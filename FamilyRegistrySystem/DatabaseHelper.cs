using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

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

    public int ExecuteNonQuery(string query, SqlParameter[] parameters = null, SqlTransaction transaction = null)
    {
        using (SqlCommand cmd = new SqlCommand(query, transaction?.Connection ?? new SqlConnection(connectionString), transaction))
        {
            if (transaction == null) cmd.Connection.Open();

            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }

            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                // Log the exact SQL being executed
                Debug.WriteLine($"SQL Error executing: {query}");
                Debug.WriteLine($"Parameters: {string.Join(", ", parameters?.Select(p => $"{p.ParameterName}={p.Value}") ?? new string[0])}");
                throw;
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

                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
        }
    }

    public SqlConnection GetConnection()
    {
        return new SqlConnection(connectionString);
    }

    // Add these methods to your existing DatabaseHelper class
    public object ExecuteScalar(string query, SqlParameter parameter)
    {
        return ExecuteScalar(query, new SqlParameter[] { parameter });
    }

    public int ExecuteNonQuery(string query, SqlParameter parameter)
    {
        return ExecuteNonQuery(query, new SqlParameter[] { parameter });
    }

    public int ExecuteNonQuery(string query, SqlParameter parameter, SqlTransaction transaction)
    {
        return ExecuteNonQuery(query, new SqlParameter[] { parameter }, transaction);
    }
}