using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public static class PDVRepository
    {
        public static List<Pdv> GetSvePdvStope(string pretraga = "")
        {
            var lista = new List<Pdv>();
            string query = @"
                SELECT id, naziv, stopa
                FROM pdv_stope
                WHERE (@pretraga = '' OR naziv LIKE '%' + @pretraga + '%')
                ORDER BY stopa";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@pretraga", pretraga ?? "");
                        using (SqlDataReader r = cmd.ExecuteReader())
                            while (r.Read()) lista.Add(MapPdv(r));
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri dohvatu PDV stopa: " + ex.Message, ex); }

            return lista;
        }

        public static Pdv GetPdvById(int id)
        {
            string query = "SELECT id, naziv, stopa FROM pdv_stope WHERE id = @id";
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader r = cmd.ExecuteReader())
                        if (r.Read()) return MapPdv(r);
                }
            }
            return null;
        }

        public static bool NazivPostoji(string naziv, int izuzetakId = 0)
        {
            string query = "SELECT COUNT(1) FROM pdv_stope WHERE naziv = @naziv AND id <> @id";
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

        public static bool Dodaj(Pdv p)
        {
            string query = "INSERT INTO pdv_stope (naziv, stopa) VALUES (@naziv, @stopa)";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@naziv", p.Naziv ?? "");
                        cmd.Parameters.AddWithValue("@stopa", p.Stopa);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri dodavanju PDV stope: " + ex.Message, ex); }
        }

        public static bool Azuriraj(Pdv p)
        {
            string query = "UPDATE pdv_stope SET naziv = @naziv, stopa = @stopa WHERE id = @id";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@naziv", p.Naziv ?? "");
                        cmd.Parameters.AddWithValue("@stopa", p.Stopa);
                        cmd.Parameters.AddWithValue("@id", p.Id);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri ažuriranju PDV stope: " + ex.Message, ex); }
        }

        public static bool Obrisi(int id)
        {
            string provjera = "SELECT COUNT(1) FROM artikli WHERE id_pdv = @id";
            string query = "DELETE FROM pdv_stope WHERE id = @id";
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
                            throw new Exception($"Nije moguće obrisati PDV stopu — koristi je {count} artikala.");
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

        private static Pdv MapPdv(SqlDataReader r) => new Pdv
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            Naziv = r.GetString(r.GetOrdinal("naziv")),
            Stopa = r.GetDecimal(r.GetOrdinal("stopa"))
        };
    }
}