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
    }
}
