using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamilyRegistrySystem
{
    public partial class Table : Form
    {
        public Table()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Sheet(*.xlsx)|*.xlsx|All Files(*.*)|*.*";
            if(ofd.ShowDialog() == DialogResult.OK )
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
    }
}
