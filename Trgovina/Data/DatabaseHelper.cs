using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trgovina.Data
{
    public static class DatabaseHelper
    {
        // Connection string iz App.config
        public static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["TrgovinaConnectionString"].ConnectionString;
            }
        }

        // Test konekcije
        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Dohvati postavku
        public static string GetPostavka(string kljuc, string defaultValue = "")
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT vrijednost FROM postavke WHERE kljuc = @kljuc";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kljuc", kljuc);
                        object result = cmd.ExecuteScalar();
                        return result?.ToString() ?? defaultValue;
                    }
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        // Spremi postavku
        public static bool SpremiPostavku(string kljuc, string vrijednost)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"IF EXISTS (SELECT 1 FROM postavke WHERE kljuc = @kljuc)
                                    UPDATE postavke SET vrijednost = @vrijednost, datum_izmjene = GETDATE() WHERE kljuc = @kljuc
                                    ELSE
                                    INSERT INTO postavke (kljuc, vrijednost) VALUES (@kljuc, @vrijednost)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kljuc", kljuc);
                        cmd.Parameters.AddWithValue("@vrijednost", vrijednost);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
