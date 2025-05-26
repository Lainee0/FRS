using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyRegistrySystem
{
    public partial class MainForm : Form
    {
        private DatabaseHelper dbHelper = new DatabaseHelper();

        public MainForm()
        {
            InitializeComponent();
            LoadFamilies();
        }

        private void LoadFamilies()
        {
            string query = @"SELECT h.HouseholdNumber, b.BarangayName, 
                        (SELECT COUNT(*) FROM FamilyMembers WHERE HouseholdNumber = h.HouseholdNumber) AS MemberCount
                        FROM Households h
                        JOIN Barangays b ON h.BarangayID = b.BarangayID";

            dataGridViewFamilies.DataSource = dbHelper.ExecuteQuery(query);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var form = new FamilyDetailsForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadFamilies();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridViewFamilies.SelectedRows.Count > 0)
            {
                int householdNumber = Convert.ToInt32(dataGridViewFamilies.SelectedRows[0].Cells["HouseholdNumber"].Value);
                var form = new FamilyDetailsForm(householdNumber);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadFamilies();
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridViewFamilies.SelectedRows.Count > 0)
            {
                string householdNumber = dataGridViewFamilies.SelectedRows[0].Cells["HouseholdNumber"].Value.ToString();
                var param = new SqlParameter("@HouseholdNumber", householdNumber);

                if (MessageBox.Show("Delete this family and all members?", "Confirm",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    using (var conn = dbHelper.GetConnection())
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                dbHelper.ExecuteNonQuery(
                                    "DELETE FROM FamilyMembers WHERE HouseholdNumber = @HouseholdNumber",
                                    param, transaction);

                                dbHelper.ExecuteNonQuery(
                                    "DELETE FROM Households WHERE HouseholdNumber = @HouseholdNumber",
                                    param, transaction);

                                transaction.Commit();
                                LoadFamilies();
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }
            }
        }

        private void btnViewMembers_Click(object sender, EventArgs e)
        {
            if (dataGridViewFamilies.SelectedRows.Count > 0)
            {
                int householdNumber = Convert.ToInt32(dataGridViewFamilies.SelectedRows[0].Cells["HouseholdNumber"].Value);
                var form = new FamilyMembersForm(householdNumber);
                form.ShowDialog();
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xls;*.xlsx",
                Title = "Select an Excel File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var importer = new ExcelImportHelper();
                    var allMembers = importer.ImportFromExcel(openFileDialog.FileName);

                    // Validate data before import
                    if (allMembers.Any(m => string.IsNullOrWhiteSpace(m.HouseholdNumber)))
                    {
                        // Instead of aborting, you could:
                        // 1. Filter out records with empty household numbers
                        var validMembers = allMembers.Where(m => !string.IsNullOrWhiteSpace(m.HouseholdNumber)).ToList();

                        // 2. Continue with only valid records
                        if (validMembers.Count == 0)
                        {
                            MessageBox.Show("No valid records with household numbers found.");
                            return;
                        }
                        // Proceed with validMembers instead of allMembers
                    }

                    using (var conn = dbHelper.GetConnection())
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                // First create all households
                                var distinctHouseholds = allMembers
                                    .Select(m => m.HouseholdNumber)
                                    .Distinct()
                                    .ToList();

                                foreach (var householdNumber in distinctHouseholds)
                                {
                                    EnsureHouseholdExists(conn, transaction, householdNumber);
                                }

                                // Then insert all members
                                foreach (var member in allMembers)
                                {
                                    try
                                    {
                                        var parameters = new SqlParameter[]
                                        {
                                    new SqlParameter("@HouseholdNumber", member.HouseholdNumber),
                                    new SqlParameter("@RowIndicator", member.RowIndicator ?? (object)DBNull.Value),
                                    new SqlParameter("@LastName", member.LastName),
                                    new SqlParameter("@FirstName", member.FirstName),
                                    new SqlParameter("@MiddleName", member.MiddleName ?? (object)DBNull.Value),
                                    new SqlParameter("@Relationship", member.Relationship ?? (object)DBNull.Value),
                                    new SqlParameter("@Birthday", member.Birthday),
                                    new SqlParameter("@Age", member.Age),
                                    new SqlParameter("@Sex", member.Sex ?? (object)DBNull.Value),
                                    new SqlParameter("@CivilStatus", member.CivilStatus ?? (object)DBNull.Value)
                                        };

                                        dbHelper.ExecuteNonQuery(
                                            @"INSERT INTO FamilyMembers 
                                    (HouseholdNumber, RowIndicator, LastName, FirstName, MiddleName, 
                                    Relationship, Birthday, Age, Sex, CivilStatus, IsHead)
                                    VALUES (@HouseholdNumber, @RowIndicator, @LastName, @FirstName, @MiddleName,
                                            @Relationship, @Birthday, @Age, @Sex, @CivilStatus,
                                            CASE WHEN @RowIndicator = 'Head' THEN 1 ELSE 0 END)",
                                            parameters,
                                            transaction);
                                    }
                                    catch (Exception ex)
                                    {
                                        // Store problematic record details
                                        ex.Data["Record"] = $"{member.LastName}, {member.FirstName} (HH: {member.HouseholdNumber})";
                                        throw;
                                    }
                                }

                                transaction.Commit();
                                MessageBox.Show($"Successfully imported {allMembers.Count} members!");
                                LoadFamilies();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                MessageBox.Show($"Import failed: {ex.Message}\n\nProblem record: {ex.Data["Record"]}",
                                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during import: {ex.Message}", "Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EnsureHouseholdExists(SqlConnection conn, SqlTransaction transaction, string householdNumber)
        {
            // Check if household exists
            var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Households WHERE HouseholdNumber = @HouseholdNumber",
                conn, transaction);
            checkCmd.Parameters.AddWithValue("@HouseholdNumber", householdNumber);

            int count = (int)checkCmd.ExecuteScalar();

            if (count == 0)
            {
                // Create new household with default BarangayID 1
                var insertCmd = new SqlCommand(
                    "INSERT INTO Households (HouseholdNumber, BarangayID) VALUES (@HouseholdNumber, 1)",
                    conn, transaction);
                insertCmd.Parameters.AddWithValue("@HouseholdNumber", householdNumber);
                insertCmd.ExecuteNonQuery();
            }
        }

        private string GetOrCreateHousehold(string householdNumber)
        {
            // First ensure default Barangay exists
            object defaultBarangay = dbHelper.ExecuteScalar(
                "SELECT TOP 1 BarangayID FROM Barangays");

            if (defaultBarangay == null)
            {
                dbHelper.ExecuteNonQuery(
                    "INSERT INTO Barangays (BarangayName) VALUES ('Default Barangay')");
                defaultBarangay = dbHelper.ExecuteScalar(
                    "SELECT TOP 1 BarangayID FROM Barangays");
            }

            // Check if household exists
            var householdParam = new SqlParameter("@HouseholdNumber", householdNumber);
            object existingId = dbHelper.ExecuteScalar(
                "SELECT HouseholdNumber FROM Households WHERE HouseholdNumber = @HouseholdNumber",
                householdParam);

            if (existingId != null)
            {
                return existingId.ToString(); // Return as string
            }

            // Create new household
            var barangayParam = new SqlParameter("@BarangayID", defaultBarangay);
            dbHelper.ExecuteNonQuery(
                @"INSERT INTO Households (HouseholdNumber, BarangayID) 
        VALUES (@HouseholdNumber, @BarangayID)",
                new SqlParameter[] { householdParam, barangayParam });

            return householdNumber; // Return original string
        }
    }
}
