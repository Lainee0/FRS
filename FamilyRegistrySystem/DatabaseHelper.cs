using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

public class DatabaseHelper
{
    private string connectionString = ConfigurationManager.ConnectionStrings["FamilyRegistrySystem.Properties.Settings.frs_dbConnectionString"].ConnectionString;

    public DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                return dt;
            }
        }
    }

    public int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteNonQuery();
            }
        }
    }

    public object ExecuteScalar(string query, SqlParameter[] parameters = null)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }
    }
}