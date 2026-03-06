using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public static class GrupeArtikalaRepository
    {
        public static List<GrupaArtikala> GetSveGrupe(string pretraga = "")
        {
            var lista = new List<GrupaArtikala>();
            string query = @"
                SELECT id, naziv, opis
                FROM grupe_artikala
                WHERE (@pretraga = '' OR naziv LIKE '%' + @pretraga + '%')
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
                            while (r.Read()) lista.Add(MapGrupa(r));
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri dohvatu grupa: " + ex.Message, ex); }

            return lista;
        }

        public static GrupaArtikala GetGrupaById(int id)
        {
            string query = "SELECT id, naziv, opis FROM grupe_artikala WHERE id = @id";
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader r = cmd.ExecuteReader())
                        if (r.Read()) return MapGrupa(r);
                }
            }
            return null;
        }

        public static bool NazivPostoji(string naziv, int izuzetakId = 0)
        {
            string query = "SELECT COUNT(1) FROM grupe_artikala WHERE naziv = @naziv AND id <> @id";
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

        public static bool Dodaj(GrupaArtikala g)
        {
            string query = "INSERT INTO grupe_artikala (naziv, opis) VALUES (@naziv, @opis)";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@naziv", g.Naziv ?? "");
                        cmd.Parameters.AddWithValue("@opis", g.Opis ?? "");
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri dodavanju grupe: " + ex.Message, ex); }
        }

        public static bool Azuriraj(GrupaArtikala g)
        {
            string query = "UPDATE grupe_artikala SET naziv = @naziv, opis = @opis WHERE id = @id";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@naziv", g.Naziv ?? "");
                        cmd.Parameters.AddWithValue("@opis", g.Opis ?? "");
                        cmd.Parameters.AddWithValue("@id", g.Id);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Greška pri ažuriranju grupe: " + ex.Message, ex); }
        }

        public static bool Obrisi(int id)
        {
            // Provjeri ima li artikala u grupi
            string provjera = "SELECT COUNT(1) FROM artikli WHERE id_grupe = @id";
            string query = "DELETE FROM grupe_artikala WHERE id = @id";
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
                            throw new Exception($"Nije moguće obrisati grupu — koristi je {count} artikala.");
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

        private static GrupaArtikala MapGrupa(SqlDataReader r) => new GrupaArtikala
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            Naziv = r.GetString(r.GetOrdinal("naziv")),
            Opis = r.IsDBNull(r.GetOrdinal("opis")) ? "" : r.GetString(r.GetOrdinal("opis"))
        };
    }
}