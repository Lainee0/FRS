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
    public partial class MemberDetailsForm : Form
    {
        private int householdNumber;
        private string originalLastName, originalFirstName;
        private bool isEditMode;
        private DatabaseHelper dbHelper = new DatabaseHelper();

        public MemberDetailsForm(int householdNumber)
        {
            InitializeComponent();
            this.householdNumber = householdNumber;
            isEditMode = false;
        }

        public MemberDetailsForm(int householdNumber, string lastName, string firstName) : this(householdNumber)
        {
            originalLastName = lastName;
            originalFirstName = firstName;
            isEditMode = true;
            LoadMemberData();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (ValidateForm())
            {
                if (isEditMode)
                {
                    UpdateMember();
                }
                else
                {
                    AddMember();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void LoadMemberData()
        {
            string query = @"SELECT * FROM FamilyMembers 
                       WHERE HouseholdNumber = @HouseholdNumber
                       AND LastName = @LastName AND FirstName = @FirstName";

            SqlParameter[] parameters = {
            new SqlParameter("@HouseholdNumber", householdNumber),
            new SqlParameter("@LastName", originalLastName),
            new SqlParameter("@FirstName", originalFirstName)
        };

            DataTable dt = dbHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                txtLastName.Text = row["LastName"].ToString();
                txtFirstName.Text = row["FirstName"].ToString();
                txtMiddleName.Text = row["MiddleName"].ToString();
                cmbRelationship.SelectedItem = row["Relationship"].ToString();
                dtpBirthday.Value = Convert.ToDateTime(row["Birthday"]);
                cmbSex.SelectedItem = row["Sex"].ToString();
                cmbCivilStatus.SelectedItem = row["CivilStatus"].ToString();
            }
        }

        private void AddMember()
        {
            string query = @"INSERT INTO FamilyMembers 
                       (HouseholdNumber, IsHead, LastName, FirstName, MiddleName, 
                        Relationship, Birthday, Age, Sex, CivilStatus)
                       VALUES (@HouseholdNumber, 0, @LastName, @FirstName, @MiddleName, 
                              @Relationship, @Birthday, @Age, @Sex, @CivilStatus)";

            SqlParameter[] parameters = {
            new SqlParameter("@HouseholdNumber", householdNumber),
            new SqlParameter("@LastName", txtLastName.Text),
            new SqlParameter("@FirstName", txtFirstName.Text),
            new SqlParameter("@MiddleName", txtMiddleName.Text),
            new SqlParameter("@Relationship", cmbRelationship.SelectedItem.ToString()),
            new SqlParameter("@Birthday", dtpBirthday.Value),
            new SqlParameter("@Age", CalculateAge(dtpBirthday.Value)),
            new SqlParameter("@Sex", cmbSex.SelectedItem.ToString()),
            new SqlParameter("@CivilStatus", cmbCivilStatus.SelectedItem.ToString())
        };

            dbHelper.ExecuteNonQuery(query, parameters);
        }

        private void UpdateMember()
        {
            string query = @"UPDATE FamilyMembers SET 
                       LastName = @LastName, FirstName = @FirstName, MiddleName = @MiddleName,
                       Relationship = @Relationship, Birthday = @Birthday, Age = @Age,
                       Sex = @Sex, CivilStatus = @CivilStatus
                       WHERE HouseholdNumber = @HouseholdNumber
                       AND LastName = @OriginalLastName AND FirstName = @OriginalFirstName";

            SqlParameter[] parameters = {
            new SqlParameter("@LastName", txtLastName.Text),
            new SqlParameter("@FirstName", txtFirstName.Text),
            new SqlParameter("@MiddleName", txtMiddleName.Text),
            new SqlParameter("@Relationship", cmbRelationship.SelectedItem.ToString()),
            new SqlParameter("@Birthday", dtpBirthday.Value),
            new SqlParameter("@Age", CalculateAge(dtpBirthday.Value)),
            new SqlParameter("@Sex", cmbSex.SelectedItem.ToString()),
            new SqlParameter("@CivilStatus", cmbCivilStatus.SelectedItem.ToString()),
            new SqlParameter("@HouseholdNumber", householdNumber),
            new SqlParameter("@OriginalLastName", originalLastName),
            new SqlParameter("@OriginalFirstName", originalFirstName)
        };

            dbHelper.ExecuteNonQuery(query, parameters);
        }

        private int CalculateAge(DateTime birthday)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthday.Year;
            if (birthday.Date > today.AddYears(-age)) age--;
            return age;
        }

        private bool ValidateForm()
        {
            // Add validation logic
            return true;
        }
    }
}
