using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyRegistrySystem
{
    public partial class Dashboard : Form
    {
        private DatabaseHelper dbHelper = new DatabaseHelper();

        public Dashboard()
        {
            InitializeComponent();
            LoadFamilies();
        }

        private void LoadFamilies()
        {
            try
            {
                string query = @"SELECT h.HouseholdNumber, b.BarangayName, 
                        (SELECT COUNT(*) FROM FamilyMembers WHERE HouseholdNumber = h.HouseholdNumber) AS MemberCount
                        FROM Households h
                        JOIN Barangays b ON h.BarangayID = b.BarangayID";

                // Get data from database
                DataTable dt = dbHelper.ExecuteQuery(query);

                // Configure DataGridView appearance
                dataGridViewFamilies.DataSource = dt;
                dataGridViewFamilies.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridViewFamilies.DefaultCellStyle.Font = new Font("Segoe UI", 9);
                dataGridViewFamilies.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                dataGridViewFamilies.EnableHeadersVisualStyles = false;
                dataGridViewFamilies.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
                dataGridViewFamilies.ColumnHeadersHeight = 35;
                dataGridViewFamilies.RowTemplate.Height = 30;

                // Customize column headers
                if (dataGridViewFamilies.Columns.Count > 0)
                {
                    dataGridViewFamilies.Columns["HouseholdNumber"].HeaderText = "Household #";
                    dataGridViewFamilies.Columns["BarangayName"].HeaderText = "Barangay";
                    dataGridViewFamilies.Columns["MemberCount"].HeaderText = "Members";

                    //// Optional: Format specific columns
                    //dataGridViewFamilies.Columns["HouseholdNumber"].DefaultCellStyle.Alignment =
                    //    DataGridViewContentAlignment.MiddleCenter;
                    //dataGridViewFamilies.Columns["MemberCount"].DefaultCellStyle.Alignment =
                    //    DataGridViewContentAlignment.MiddleCenter;
                }

                // Add alternating row colors
                dataGridViewFamilies.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading families: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                int householdNumber = Convert.ToInt32(dataGridViewFamilies.SelectedRows[0].Cells["HouseholdNumber"].Value);

                if (MessageBox.Show("Delete this family and all members?", "Confirm",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string deleteMembers = "DELETE FROM FamilyMembers WHERE HouseholdNumber = @HouseholdNumber";
                    string deleteHousehold = "DELETE FROM Households WHERE HouseholdNumber = @HouseholdNumber";

                    var param = new SqlParameter("@HouseholdNumber", householdNumber);

                    dbHelper.ExecuteNonQuery(deleteMembers, new SqlParameter[] { param });
                    dbHelper.ExecuteNonQuery(deleteHousehold, new SqlParameter[] { param });

                    LoadFamilies();
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

        private void Dashboard_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'frs_dbDataSet.Households' table. You can move, or remove it, as needed.
            this.householdsTableAdapter.Fill(this.frs_dbDataSet.Households);

        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Excel Files|*.xls;*.xlsx";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Store file path since OpenFileDialog will be disposed
                        string filePath = openFileDialog.FileName;

                        // Get barangay name from filename
                        string barangayName = Path.GetFileNameWithoutExtension(filePath).Split('_')[0];

                        // Read Excel file and process data
                        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = ExcelReaderFactory.CreateReader(stream))
                            {
                                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                                {
                                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                                    {
                                        UseHeaderRow = true
                                    }
                                });

                                // Process first sheet
                                DataTable excelData = result.Tables[0];

                                // DEBUG: Show what was read from Excel
                                StringBuilder debugInfo = new StringBuilder();
                                debugInfo.AppendLine($"Rows read: {excelData.Rows.Count}");
                                debugInfo.AppendLine("First 5 rows:");

                                for (int i = 0; i < Math.Min(5, excelData.Rows.Count); i++)
                                {
                                    debugInfo.AppendLine($"Row {i}: {string.Join("|", excelData.Rows[i].ItemArray)}");
                                }

                                MessageBox.Show(debugInfo.ToString(), "Debug - Excel Data Read");

                                // Process data after verification
                                ProcessExcelData(excelData, barangayName);
                            }
                        }

                        MessageBox.Show("Import completed successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}");
            }
        }

        private void ProcessExcelData(DataTable excelData, string barangayName)
        {
            string connectionString = GetConnectionString();

            // Debug: Show incoming parameters
            MessageBox.Show($"Starting import for {barangayName}, {excelData.Rows.Count} rows", "Debug - Start Import");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int barangayId = GetOrCreateBarangay(barangayName, connection, transaction);
                        int savedRows = 0;

                        foreach (DataRow row in excelData.Rows)
                        {
                            if (SaveHouseholdMember(row, barangayId, connection, transaction))
                                savedRows++;
                        }

                        transaction.Commit();
                        MessageBox.Show($"Successfully saved {savedRows}/{excelData.Rows.Count} rows", "Debug - Import Complete");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Error during import: {ex.Message}\n\nNo changes were saved.", "Import Error");
                    }
                }
            }
        }

        private int GetOrCreateBarangay(string barangayName, SqlConnection connection, SqlTransaction transaction)
        {
            string checkQuery = "SELECT BarangayID FROM Barangays WHERE BarangayName = @Name";
            var param = new SqlParameter("@Name", barangayName);

            object result = dbHelper.ExecuteScalar(checkQuery, new SqlParameter[] { param });

            if (result != null)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                string insertQuery = "INSERT INTO Barangays (BarangayName) OUTPUT INSERTED.BarangayID VALUES (@Name)";
                return Convert.ToInt32(dbHelper.ExecuteScalar(insertQuery, new SqlParameter[] { param }));
            }
        }

        private bool SaveHouseholdMember(DataRow row, int barangayId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Required field validation
                if (row["Household Number"] == DBNull.Value || row["Last Name"] == DBNull.Value)
                {
                    Debug.WriteLine("Skipping row - missing required fields");
                    return false;
                }

                string householdNumber = row["Household Number"].ToString();
                string lastName = row["Last Name"].ToString();
                string firstName = row["First Name"]?.ToString() ?? "";

                // Debug: Show row being processed
                Debug.WriteLine($"Processing: {householdNumber} - {lastName}, {firstName}");

                // Insert household
                string householdQuery = @"IF NOT EXISTS (SELECT 1 FROM Households WHERE HouseholdNumber = @Number)
                               INSERT INTO Households (HouseholdNumber, BarangayID) 
                               VALUES (@Number, @BarangayID)";

                using (SqlCommand cmd = new SqlCommand(householdQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Number", householdNumber);
                    cmd.Parameters.AddWithValue("@BarangayID", barangayId);
                    cmd.ExecuteNonQuery();
                }

                // Insert member
                string memberQuery = @"INSERT INTO FamilyMembers 
                             (HouseholdNumber, IsHead, LastName, FirstName, MiddleName, 
                              Relationship, Birthday, Age, Sex, CivilStatus)
                             VALUES (@HouseholdNumber, @IsHead, @LastName, @FirstName, @MiddleName, 
                                    @Relationship, @Birthday, @Age, @Sex, @CivilStatus)";

                using (SqlCommand cmd = new SqlCommand(memberQuery, connection, transaction))
                {
                    // Add all parameters with null checks
                    cmd.Parameters.AddWithValue("@HouseholdNumber", householdNumber);
                    cmd.Parameters.AddWithValue("@IsHead", row["Row indicator"].ToString() == "Head");
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@MiddleName", row["Middle Name"]?.ToString() ?? "");
                    cmd.Parameters.AddWithValue("@Relationship", row["E"]?.ToString() ?? "");
                    cmd.Parameters.AddWithValue("@Birthday", Convert.ToDateTime(row["Birthday"]));
                    cmd.Parameters.AddWithValue("@Age", CalculateAge(Convert.ToDateTime(row["Birthday"])));
                    cmd.Parameters.AddWithValue("@Sex", row["Sex"]?.ToString()?.Split(' ')[0] ?? "");
                    cmd.Parameters.AddWithValue("@CivilStatus", row["C.M."]?.ToString() ?? "");

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving row: {ex.Message}");
                return false;
            }
        }

        private int CalculateAge(DateTime birthday)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthday.Year;
            if (birthday.Date > today.AddYears(-age)) age--;
            return age;
        }

        private string GetConnectionString()
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["FamilyRegistrySystem.Properties.Settings.frs_dbConnectionString"]?.ConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new Exception("Connection string not found in config file");

                // Verify the connection string format
                new SqlConnectionStringBuilder(connectionString);

                return connectionString;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid connection string: {ex.Message}");
                throw;
            }
        }
    }
}
