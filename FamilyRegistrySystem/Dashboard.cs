using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;

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
                // 1. Configure and show file dialog
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx";
                    openFileDialog.Title = "Select Household Data File";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 2. Extract barangay name from filename (format: "BarangayName_*.csv")
                        string fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                        string barangayName = fileName.Split('_')[0]; // Assumes format "BarangayName_restoffilename"

                        // 3. Read file based on extension
                        DataTable importedData;

                        if (openFileDialog.FileName.EndsWith(".csv"))
                        {
                            importedData = ReadCsvFile(openFileDialog.FileName);
                        }
                        else // Excel
                        {
                            importedData = ReadExcelFile(openFileDialog.FileName);
                        }

                        // 4. Process and validate data
                        if (importedData.Rows.Count > 0)
                        {
                            // Get or create barangay ID
                            int barangayId = GetOrCreateBarangay(barangayName);

                            // Process each household
                            ProcessImportedData(importedData, barangayId);

                            MessageBox.Show($"Successfully imported {importedData.Rows.Count} records for {barangayName}",
                                          "Import Successful",
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Information);

                            // Refresh the view
                            LoadFamilies();
                        }
                        else
                        {
                            MessageBox.Show("No valid data found in the selected file.",
                                          "Import Failed",
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during import: {ex.Message}",
                                "Import Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        // Helper method to read CSV files
        private DataTable ReadCsvFile(string filePath)
        {
            DataTable dt = new DataTable();

            using (StreamReader sr = new StreamReader(filePath))
            {
                // Read headers
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    dt.Columns.Add(header.Trim());
                }

                // Read data rows
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i].Trim();
                    }
                    dt.Rows.Add(dr);
                }
            }

            return dt;
        }

        // Helper method to read Excel files (requires Microsoft.Office.Interop.Excel or EPPlus)
        private DataTable ReadExcelFile(string filePath)
        {
            DataTable dt = new DataTable();

            // Using EPPlus (recommended - add NuGet package EPPlus)
            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                // Read headers
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    dt.Columns.Add(worksheet.Cells[1, col].Text);
                }

                // Read data rows
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    DataRow dr = dt.NewRow();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        dr[col - 1] = worksheet.Cells[row, col].Text;
                    }
                    dt.Rows.Add(dr);
                }
            }

            return dt;
        }

        // Helper method to get or create barangay
        private int GetOrCreateBarangay(string barangayName)
        {
            string checkQuery = "SELECT BarangayID FROM Barangays WHERE BarangayName = @Name";
            SqlParameter param = new SqlParameter("@Name", barangayName);

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

        // Helper method to process imported data
        private void ProcessImportedData(DataTable importedData, int barangayId)
        {
            foreach (DataRow row in importedData.Rows)
            {
                try
                {
                    // Extract data from row (adjust column names as needed)
                    string householdNumber = row["Household Number"].ToString();
                    string rowIndicator = row["Row indicator"].ToString();
                    string lastName = row["Last Name"].ToString();
                    string firstName = row["First Name"].ToString();
                    string middleName = row["Middle Name"].ToString();
                    string relationship = row["E"].ToString(); // Assuming "E" column is relationship
                    DateTime birthday = DateTime.Parse(row["Birthday"].ToString());
                    string sex = row["Sex"].ToString().Split(' ')[0]; // Extract "Male"/"Female"
                    string civilStatus = row["C.M."].ToString();

                    // Insert or update household and family members
                    // (Implement your specific database logic here)
                    // Example:
                    string householdQuery = @"IF NOT EXISTS (SELECT 1 FROM Households WHERE HouseholdNumber = @Number)
                                    INSERT INTO Households (HouseholdNumber, BarangayID) 
                                    VALUES (@Number, @BarangayID)";

                    SqlParameter[] householdParams = {
                new SqlParameter("@Number", householdNumber),
                new SqlParameter("@BarangayID", barangayId)
            };

                    dbHelper.ExecuteNonQuery(householdQuery, householdParams);

                    // Insert family member
                    string memberQuery = @"INSERT INTO FamilyMembers 
                                 (HouseholdNumber, IsHead, LastName, FirstName, MiddleName, 
                                  Relationship, Birthday, Age, Sex, CivilStatus)
                                 VALUES (@HouseholdNumber, @IsHead, @LastName, @FirstName, @MiddleName, 
                                        @Relationship, @Birthday, @Age, @Sex, @CivilStatus)";

                    SqlParameter[] memberParams = {
                new SqlParameter("@HouseholdNumber", householdNumber),
                new SqlParameter("@IsHead", rowIndicator == "Head"),
                new SqlParameter("@LastName", lastName),
                new SqlParameter("@FirstName", firstName),
                new SqlParameter("@MiddleName", middleName),
                new SqlParameter("@Relationship", relationship),
                new SqlParameter("@Birthday", birthday),
                new SqlParameter("@Age", CalculateAge(birthday)),
                new SqlParameter("@Sex", sex),
                new SqlParameter("@CivilStatus", civilStatus)
            };

                    dbHelper.ExecuteNonQuery(memberQuery, memberParams);
                }
                catch (Exception ex)
                {
                    // Log error but continue with next row
                    Console.WriteLine($"Error processing row: {ex.Message}");
                }
            }
        }

        private int CalculateAge(DateTime birthday)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthday.Year;
            if (birthday.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
