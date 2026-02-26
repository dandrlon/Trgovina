using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public static class PartneriRepository
    {
        // ─────────────────────────────────────────
        //  ARTIKLI
        // ─────────────────────────────────────────

        public static List<Partner> GetSviPartneri(string pretraga = "", bool? aktivan = null)
        {
            var lista = new List<Partner>();

            string query = @"
                SELECT id, naziv, adresa, grad, postanski_broj, drzava, oib, telefon, email, kontakt_osoba, aktivan, napomena
                FROM partneri
                WHERE (@pretraga = '' OR naziv LIKE '%' + @pretraga + '%' OR id LIKE '%' + @pretraga + '%')
                  AND (@aktivan IS NULL OR a.aktivan = @aktivan)
                ORDER BY naziv";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@pretraga", pretraga ?? "");

                        if (aktivan.HasValue)
                            cmd.Parameters.Add("@aktivan", SqlDbType.Bit).Value = aktivan.Value;
                        else
                            cmd.Parameters.Add("@aktivan", SqlDbType.Bit).Value = DBNull.Value;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(MapPartner(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Greška pri dohvatu partnera: " + ex.Message, ex);
            }

            return lista;
        }

        public static Partner GetPartnerById(int id)
        {
            string query = @"
                SELECT id, naziv, adresa, grad, postanski_broj, drzava, oib, telefon, email, kontakt_osoba, aktivan, napomena
                FROM partneri
                WHERE a.id = @id";

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return MapPartner(reader);
                    }
                }
            }
            return null;
        }

        public static bool DodajPartnera(Partner p)
        {
            string query = @"
                INSERT INTO partneri (naziv, adresa, grad, postanski_broj, 
                                     drzava, oib, telefon, email, kontakt_osoba, aktivan, napomena)
                VALUES ( @naziv, @adresa, @grad, @postanski_broj,@drzava,
                        @oib, @telefon, @email, @kontakt_osoba, @aktivan, @napomena)";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        PopuniParametre(cmd, p);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Greška pri dodavanju partnera: " + ex.Message, ex);
            }
        }

        public static bool AzurirajPartnera(Partner p)
        {
            string query = @"
                UPDATE partneri SET
                    naziv = @naziv, adresa = @adresa,postanski_broj = @postanski_broj
                    drzava = @drzava, oib = @oib, telefon = @telefon, email = @email
                    kontakt_osoba = @kontakt_osoba, aktivan = @aktivan, napomena = @napomena
                WHERE id = @id";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        PopuniParametre(cmd, p);
                        cmd.Parameters.AddWithValue("@id", p.Id);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Greška pri ažuriranju partnera: " + ex.Message, ex);
            }
        }

        public static bool ObrisiArtikl(int id)
        {
            // Soft delete - samo deaktivira
            string query = "UPDATE partneri SET aktivan = 0 WHERE id = @id";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Greška pri brisanju partnera: " + ex.Message, ex);
            }
        }
  

        //  HELPERI

        private static Partner MapPartner(SqlDataReader r)
        {
            return new Partner
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Naziv = r.GetString(r.GetOrdinal("naziv")),
                Adresa = r.IsDBNull(r.GetOrdinal("adresa")) ? "" : r.GetString(r.GetOrdinal("adresa")),
                Grad = r.IsDBNull(r.GetOrdinal("grad")) ? "" : r.GetString(r.GetOrdinal("grad")),
                PostanskiBroj = r.IsDBNull(r.GetOrdinal("postanski_broj")) ? "" : r.GetString(r.GetOrdinal("postanski_broj")),
                Drzava = r.IsDBNull(r.GetOrdinal("drzava")) ? "" : r.GetString(r.GetOrdinal("drzava")),
                OIB = r.IsDBNull(r.GetOrdinal("oib")) ? "" : r.GetString(r.GetOrdinal("oib")),
                Telefon = r.IsDBNull(r.GetOrdinal("telefon")) ? "" : r.GetString(r.GetOrdinal("telefon")),
                Email = r.IsDBNull(r.GetOrdinal("email")) ? "" : r.GetString(r.GetOrdinal("email")),
                KontaktOsoba = r.IsDBNull(r.GetOrdinal("kontakt_osoba")) ? "" : r.GetString(r.GetOrdinal("kontakt_osoba")),
                Aktivan = !r.IsDBNull(r.GetOrdinal("aktivan")) && r.GetBoolean(r.GetOrdinal("aktivan")),
                Napomena = r.IsDBNull(r.GetOrdinal("napomena")) ? "" : r.GetString(r.GetOrdinal("napomena")),
            };
        }

        private static void PopuniParametre(SqlCommand cmd, Partner p)
        {
            cmd.Parameters.AddWithValue("@naziv", p.Naziv ?? "");
            cmd.Parameters.AddWithValue("@adresa", p.Adresa ?? "");
            cmd.Parameters.AddWithValue("@grad", p.Grad ?? "");
            cmd.Parameters.AddWithValue("@postanski_broj", p.PostanskiBroj ?? "");
            cmd.Parameters.AddWithValue("@drzava", p.Drzava ?? "");
            cmd.Parameters.AddWithValue("@oib", p.OIB ?? "");
            cmd.Parameters.AddWithValue("@telefon", p.Telefon ?? "");
            cmd.Parameters.AddWithValue("@email", p.Email ?? "");
            cmd.Parameters.AddWithValue("@kontakt_osoba", p.KontaktOsoba ?? "");
            cmd.Parameters.AddWithValue("@aktivan", p.Aktivan);
            cmd.Parameters.AddWithValue("@napomena", p.Napomena ?? "");
        }
    }
}
