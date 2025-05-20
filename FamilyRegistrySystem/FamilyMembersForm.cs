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
    public partial class FamilyMembersForm : Form
    {
        private int householdNumber;
        private DatabaseHelper dbHelper = new DatabaseHelper();

        public FamilyMembersForm(int householdNumber)
        {
            InitializeComponent();
            this.householdNumber = householdNumber;
            LoadFamilyMembers();
        }

        private void LoadFamilyMembers()
        {
            string query = @"SELECT 
                        CASE WHEN IsHead = 1 THEN 'Head' ELSE 'Member' END AS MemberType,
                        LastName, FirstName, MiddleName, Relationship, 
                        CONVERT(VARCHAR, Birthday, 101) AS Birthday,
                        Age, Sex, CivilStatus
                        FROM FamilyMembers
                        WHERE HouseholdNumber = @HouseholdNumber
                        ORDER BY IsHead DESC, MemberID";

            var param = new SqlParameter("@HouseholdNumber", householdNumber);
            dataGridViewMembers.DataSource = dbHelper.ExecuteQuery(query, new SqlParameter[] { param });
        }

        private void btnAddMember_Click(object sender, EventArgs e)
        {
            var form = new MemberDetailsForm(householdNumber);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadFamilyMembers();
            }
        }

        private void btnEditMember_Click(object sender, EventArgs e)
        {
            if (dataGridViewMembers.SelectedRows.Count > 0)
            {
                string lastName = dataGridViewMembers.SelectedRows[0].Cells["LastName"].Value.ToString();
                string firstName = dataGridViewMembers.SelectedRows[0].Cells["FirstName"].Value.ToString();

                var form = new MemberDetailsForm(householdNumber, lastName, firstName);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadFamilyMembers();
                }
            }
        }

        private void btnDeleteMember_Click(object sender, EventArgs e)
        {
            if (dataGridViewMembers.SelectedRows.Count > 0)
            {
                string lastName = dataGridViewMembers.SelectedRows[0].Cells["LastName"].Value.ToString();
                string firstName = dataGridViewMembers.SelectedRows[0].Cells["FirstName"].Value.ToString();

                if (MessageBox.Show($"Delete {firstName} {lastName}?", "Confirm",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string query = @"DELETE FROM FamilyMembers 
                               WHERE HouseholdNumber = @HouseholdNumber
                               AND LastName = @LastName AND FirstName = @FirstName";

                    SqlParameter[] parameters = {
                    new SqlParameter("@HouseholdNumber", householdNumber),
                    new SqlParameter("@LastName", lastName),
                    new SqlParameter("@FirstName", firstName)
                };

                    dbHelper.ExecuteNonQuery(query, parameters);
                    LoadFamilyMembers();
                }
            }
        }
    }
}
