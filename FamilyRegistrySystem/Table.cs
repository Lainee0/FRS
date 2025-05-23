using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyRegistrySystem
{
    public partial class Table : Form
    {
        // SQL connection string - update with your actual credentials
        private string sqlConnectionString = "Server=KENNETH-PC\\SQLEXPRESS;Database=frs_db;Integrated Security=True;";

        public Table()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Sheet(*.xlsx)|*.xlsx|All Files(*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string filepath = ofd.FileName;
                textBox1.Text = filepath;
                LoadDataFromExceltoDataGridView(filepath, ".xlsx", "yes");
            }
        }

        public void LoadDataFromExceltoDataGridView(string fpath, string ext, string hdr)
        {
            string con = "Provider=Microsoft.Ace.OLEDB.12.0; Data Source={0}; Extended Properties='Excel 8.0; HDR={1}'";
            con = String.Format(con, fpath, hdr);
            OleDbConnection excelcon = new OleDbConnection(con);
            excelcon.Open();
            DataTable exceldata = excelcon.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            string exsheetname = exceldata.Rows[0]["TABLE_NAME"].ToString();
            OleDbCommand com = new OleDbCommand("Select * from [" + exsheetname + "]", excelcon);
            OleDbDataAdapter oda = new OleDbDataAdapter(com);
            DataTable dt = new DataTable();
            oda.Fill(dt);
            excelcon.Close();
            dataGridView1.DataSource = dt;
        }

        private int GetOrCreateBarangay(SqlConnection connection, SqlTransaction transaction, string barangayName)
        {
            // Check if barangay exists
            string checkSql = "SELECT BarangayID FROM Barangays WHERE BarangayName = @BarangayName";
            using (SqlCommand cmd = new SqlCommand(checkSql, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@BarangayName", barangayName);
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
            }

            // If not exists, create new barangay
            string insertSql = "INSERT INTO Barangays (BarangayName) OUTPUT INSERTED.BarangayID VALUES (@BarangayName)";
            using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@BarangayName", barangayName);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void CreateHouseholdIfNotExists(SqlConnection connection, SqlTransaction transaction,
                                              int householdNumber, int barangayId)
        {
            // Check if household exists
            string checkSql = "SELECT 1 FROM Households WHERE HouseholdNumber = @HouseholdNumber";
            using (SqlCommand cmd = new SqlCommand(checkSql, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@HouseholdNumber", householdNumber);
                if (cmd.ExecuteScalar() == null)
                {
                    // Create new household
                    string insertSql = @"INSERT INTO Households (HouseholdNumber, BarangayID) 
                                      VALUES (@HouseholdNumber, @BarangayID)";
                    using (SqlCommand insertCmd = new SqlCommand(insertSql, connection, transaction))
                    {
                        insertCmd.Parameters.AddWithValue("@HouseholdNumber", householdNumber);
                        insertCmd.Parameters.AddWithValue("@BarangayID", barangayId);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void InsertFamilyMember(SqlConnection connection, SqlTransaction transaction,
                                      int householdNumber, DataRow memberData)
        {
            string insertSql = @"
            INSERT INTO FamilyMembers (
                HouseholdNumber, IsHead, RowIndicator, LastName, FirstName, MiddleName,
                Relationship, Birthday, Age, Sex, CivilStatus
            ) VALUES (
                @HouseholdNumber, @IsHead, @RowIndicator, @LastName, @FirstName, @MiddleName,
                @Relationship, @Birthday, @Age, @Sex, @CivilStatus
            )";

            using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
            {
                // Add parameters with null checks
                cmd.Parameters.AddWithValue("@HouseholdNumber", householdNumber);
                cmd.Parameters.AddWithValue("@IsHead", memberData["IsHead"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@RowIndicator", memberData["RowIndicator"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@LastName", memberData["LastName"]);
                cmd.Parameters.AddWithValue("@FirstName", memberData["FirstName"]);
                cmd.Parameters.AddWithValue("@MiddleName", memberData["MiddleName"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Relationship", memberData["Relationship"] ?? (object)DBNull.Value);

                // Handle date conversion
                if (DateTime.TryParse(memberData["Birthday"]?.ToString(), out DateTime birthday))
                    cmd.Parameters.AddWithValue("@Birthday", birthday);
                else
                    cmd.Parameters.AddWithValue("@Birthday", DBNull.Value);

                cmd.Parameters.AddWithValue("@Age", memberData["Age"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Sex", memberData["Sex"] ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CivilStatus", memberData["CivilStatus"] ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (dataGridView1.DataSource == null)
            {
                MessageBox.Show("No data loaded to save!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                DataTable familyData = (DataTable)dataGridView1.DataSource;

                using (SqlConnection sqlConn = new SqlConnection(sqlConnectionString))
                {
                    sqlConn.Open();

                    // Begin transaction for atomic operations
                    using (SqlTransaction transaction = sqlConn.BeginTransaction())
                    {
                        try
                        {
                            // Process each row in the Excel data
                            foreach (DataRow row in familyData.Rows)
                            {
                                // 1. Handle Barangay
                                int barangayId = GetOrCreateBarangay(sqlConn, transaction, row["BarangayName"].ToString());

                                // 2. Handle Household
                                int householdNumber = Convert.ToInt32(row["HouseholdNumber"]);
                                CreateHouseholdIfNotExists(sqlConn, transaction, householdNumber, barangayId);

                                // 3. Handle Family Member
                                InsertFamilyMember(sqlConn, transaction, householdNumber, row);
                            }

                            transaction.Commit();
                            MessageBox.Show("Data successfully saved to database!", "Success",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Error saving data: {ex.Message}", "Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
