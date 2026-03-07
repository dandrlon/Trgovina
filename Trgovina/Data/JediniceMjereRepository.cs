using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public static class JediniceMjereRepository
    {
        public static List<JedinicaMjere> GetSveJedinice(string pretraga = "")
        {
            var lista = new List<JedinicaMjere>();
            string query = @"
                SELECT id, naziv, kratica
                FROM jedinice_mjere
                WHERE (@pretraga = '' OR naziv LIKE '%' + @pretraga + '%' OR kratica LIKE '%' + @pretraga + '%')
                ORDER BY naziv";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@pretraga", pretraga ?? "");
                        using (SqlDataReader r = cmd.ExecuteReader())
                            while (r.Read()) lista.Add(MapJedinica(r));
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri dohvatu jedinica mjere: " + ex.Message, ex); }

            return lista;
        }

        public static JedinicaMjere GetJedinicaById(int id)
        {
            string query = "SELECT id, naziv, kratica FROM jedinice_mjere WHERE id = @id";
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader r = cmd.ExecuteReader())
                        if (r.Read()) return MapJedinica(r);
                }
            }
            return null;
        }

        public static bool NazivPostoji(string naziv, int izuzetakId = 0)
        {
            string query = "SELECT COUNT(1) FROM jedinice_mjere WHERE naziv = @naziv AND id <> @id";
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@naziv", naziv);
                    cmd.Parameters.AddWithValue("@id", izuzetakId);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public static bool Dodaj(JedinicaMjere j)
        {
            string query = "INSERT INTO jedinice_mjere (naziv, kratica) VALUES (@naziv, @kratica)";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@naziv", j.Naziv ?? "");
                        cmd.Parameters.AddWithValue("@kratica", j.Skracenica ?? "");
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri dodavanju jedinice mjere: " + ex.Message, ex); }
        }

        public static bool Azuriraj(JedinicaMjere j)
        {
            string query = "UPDATE jedinice_mjere SET naziv = @naziv, kratica = @kratica WHERE id = @id";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@naziv", j.Naziv ?? "");
                        cmd.Parameters.AddWithValue("@kratica", j.Skracenica ?? "");
                        cmd.Parameters.AddWithValue("@id", j.Id);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri ažuriranju jedinice mjere: " + ex.Message, ex); }
        }

        public static bool Obrisi(int id)
        {
            string provjera = "SELECT COUNT(1) FROM artikli WHERE id_jedinice_mjere = @id";
            string query = "DELETE FROM jedinice_mjere WHERE id = @id";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(provjera, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                            throw new Exception($"Nije moguće obrisati jedinicu mjere — koristi je {count} artikala.");
                    }
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex) { throw new Exception(ex.Message, ex); }
        }

        private static JedinicaMjere MapJedinica(SqlDataReader r) => new JedinicaMjere
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            Naziv = r.GetString(r.GetOrdinal("naziv")),
            Skracenica = r.IsDBNull(r.GetOrdinal("kratica")) ? "" : r.GetString(r.GetOrdinal("kratica"))
        };
    }
}