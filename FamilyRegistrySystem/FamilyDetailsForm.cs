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
    public partial class FamilyDetailsForm : Form
    {
        private int householdNumber;
        private bool isEditMode;
        private DatabaseHelper dbHelper = new DatabaseHelper();

        public FamilyDetailsForm()
        {
            InitializeComponent();
            isEditMode = false;
            LoadBarangays();
        }

        public FamilyDetailsForm(int householdNumber) : this()
        {
            this.householdNumber = householdNumber;
            isEditMode = true;
            LoadFamilyData();
        }

        private void LoadBarangays()
        {
            string query = "SELECT BarangayID, BarangayName FROM Barangays";
            cmbBarangay.DataSource = dbHelper.ExecuteQuery(query);
            cmbBarangay.DisplayMember = "BarangayName";
            cmbBarangay.ValueMember = "BarangayID";
        }

        private void LoadFamilyData()
        {
            string query = @"SELECT h.*, f.* FROM Households h
                        JOIN FamilyMembers f ON h.HouseholdNumber = f.HouseholdNumber
                        WHERE h.HouseholdNumber = @HouseholdNumber AND f.IsHead = 1";

            var param = new SqlParameter("@HouseholdNumber", householdNumber);
            DataTable dt = dbHelper.ExecuteQuery(query, new SqlParameter[] { param });

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                cmbBarangay.SelectedValue = row["BarangayID"];
                txtHouseholdNumber.Text = row["HouseholdNumber"].ToString();
                txtLastName.Text = row["LastName"].ToString();
                txtFirstName.Text = row["FirstName"].ToString();
                txtMiddleName.Text = row["MiddleName"].ToString();
                dtpBirthday.Value = Convert.ToDateTime(row["Birthday"]);
                cmbSex.SelectedItem = row["Sex"].ToString();
                cmbCivilStatus.SelectedItem = row["CivilStatus"].ToString();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (ValidateForm())
            {
                if (isEditMode)
                {
                    UpdateFamily();
                }
                else
                {
                    AddFamily();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void AddFamily()
        {
            // Generate household number (you might want a better way)
            int newHouseholdNumber = GetNextHouseholdNumber();

            // Add household
            string householdQuery = @"INSERT INTO Households (HouseholdNumber, BarangayID) 
                                VALUES (@HouseholdNumber, @BarangayID)";

            SqlParameter[] householdParams = {
            new SqlParameter("@HouseholdNumber", newHouseholdNumber),
            new SqlParameter("@BarangayID", cmbBarangay.SelectedValue)
        };

            dbHelper.ExecuteNonQuery(householdQuery, householdParams);

            // Add head of family
            string memberQuery = @"INSERT INTO FamilyMembers 
                             (HouseholdNumber, IsHead, LastName, FirstName, MiddleName, 
                              Relationship, Birthday, Age, Sex, CivilStatus)
                             VALUES (@HouseholdNumber, 1, @LastName, @FirstName, @MiddleName, 
                                    '1 - Puno ng Pamilya', @Birthday, @Age, @Sex, @CivilStatus)";

            SqlParameter[] memberParams = {
            new SqlParameter("@HouseholdNumber", newHouseholdNumber),
            new SqlParameter("@LastName", txtLastName.Text),
            new SqlParameter("@FirstName", txtFirstName.Text),
            new SqlParameter("@MiddleName", txtMiddleName.Text),
            new SqlParameter("@Birthday", dtpBirthday.Value),
            new SqlParameter("@Age", CalculateAge(dtpBirthday.Value)),
            new SqlParameter("@Sex", cmbSex.SelectedItem.ToString()),
            new SqlParameter("@CivilStatus", cmbCivilStatus.SelectedItem.ToString())
        };

            dbHelper.ExecuteNonQuery(memberQuery, memberParams);
        }

        private void UpdateFamily()
        {
            // Update household
            string householdQuery = @"UPDATE Households SET BarangayID = @BarangayID 
                                WHERE HouseholdNumber = @HouseholdNumber";

            SqlParameter[] householdParams = {
            new SqlParameter("@BarangayID", cmbBarangay.SelectedValue),
            new SqlParameter("@HouseholdNumber", householdNumber)
        };

            dbHelper.ExecuteNonQuery(householdQuery, householdParams);

            // Update head of family
            string memberQuery = @"UPDATE FamilyMembers SET 
                             LastName = @LastName, FirstName = @FirstName, MiddleName = @MiddleName,
                             Birthday = @Birthday, Age = @Age, Sex = @Sex, CivilStatus = @CivilStatus
                             WHERE HouseholdNumber = @HouseholdNumber AND IsHead = 1";

            SqlParameter[] memberParams = {
            new SqlParameter("@LastName", txtLastName.Text),
            new SqlParameter("@FirstName", txtFirstName.Text),
            new SqlParameter("@MiddleName", txtMiddleName.Text),
            new SqlParameter("@Birthday", dtpBirthday.Value),
            new SqlParameter("@Age", CalculateAge(dtpBirthday.Value)),
            new SqlParameter("@Sex", cmbSex.SelectedItem.ToString()),
            new SqlParameter("@CivilStatus", cmbCivilStatus.SelectedItem.ToString()),
            new SqlParameter("@HouseholdNumber", householdNumber)
        };

            dbHelper.ExecuteNonQuery(memberQuery, memberParams);
        }

        private int CalculateAge(DateTime birthday)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthday.Year;
            if (birthday.Date > today.AddYears(-age)) age--;
            return age;
        }

        private int GetNextHouseholdNumber()
        {
            string query = "SELECT ISNULL(MAX(HouseholdNumber), 0) + 1 FROM Households";
            DataTable dt = dbHelper.ExecuteQuery(query);
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        private bool ValidateForm()
        {
            // Add validation logic
            return true;
        }
    }
}
