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
    public static class ArtikliRepository
    {
        // ─────────────────────────────────────────
        //  ARTIKLI
        // ─────────────────────────────────────────

        public static List<Artikl> GetSviArtikli(string pretraga = "", int idGrupe = 0, bool? aktivan = null)
        {
            var lista = new List<Artikl>();

            string query = @"
                SELECT a.id, a.sifra, a.naziv, a.opis,
                       a.nabavna_cijena, a.prodajna_cijena, a.kolicina,
                       a.grupa_id, g.naziv AS naziv_grupe,
                       a.jedinica_mjere_id, j.naziv AS naziv_jedinice,j.kratica,
                       a.pdv_id, p.stopa AS pdv_stopa, p.stopa AS pdv_naziv,
                       a.aktivan
                FROM artikli a
                LEFT JOIN grupe_artikala g ON a.grupa_id = g.id
                LEFT JOIN jedinice_mjere j ON a.jedinica_mjere_id = j.id
                LEFT JOIN pdv p ON a.pdv_id = p.id
                WHERE (@pretraga = '' OR a.naziv LIKE '%' + @pretraga + '%' OR a.sifra LIKE '%' + @pretraga + '%')
                  AND (@idGrupe = 0 OR a.grupa_id = @idGrupe)
                  AND (@aktivan IS NULL OR a.aktivan = @aktivan)
                ORDER BY a.naziv";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@pretraga", pretraga ?? "");
                        cmd.Parameters.AddWithValue("@idGrupe", idGrupe);

                        if (aktivan.HasValue)
                            cmd.Parameters.Add("@aktivan", SqlDbType.Bit).Value = aktivan.Value;
                        else
                            cmd.Parameters.Add("@aktivan", SqlDbType.Bit).Value = DBNull.Value;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(MapArtikl(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Greška pri dohvatu artikala: " + ex.Message, ex);
            }

            return lista;
        }

        public static Artikl GetArtikalById(int id)
        {
            string query = @"
                SELECT a.id, a.sifra, a.naziv, a.opis,
                       a.nabavna_cijena, a.prodajna_cijena, a.kolicina,
                       a.grupa_id, g.naziv AS naziv_grupe,
                       a.jedinica_mjere_id, j.naziv AS naziv_jedinice,j.kratica,
                       a.pdv_id, p.stopa AS pdv_stopa, p.stopa AS pdv_naziv,
                       a.aktivan
                FROM artikli a
                LEFT JOIN grupe_artikala g ON a.grupa_id = g.id
                LEFT JOIN jedinice_mjere j ON a.jedinica_mjere_id = j.id
                LEFT JOIN pdv p ON a.pdv_id = p.id
                WHERE a.id = @id";

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return MapArtikl(reader);
                    }
                }
            }
            return null;
        }

        public static bool DodajArtikl(Artikl a)
        {
            string query = @"
                INSERT INTO artikli (sifra, naziv, opis, nabavna_cijena, prodajna_cijena, 
                                     kolicina, grupa_id, jedinica_mjere_id, pdv_id, aktivan)
                VALUES (@sifra, @naziv, @opis, @nabavna_cijena, @prodajna_cijena,
                        @kolicina, @grupa_id, @jedinica_mjere_id, @pdv_id, @aktivan)";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        PopuniParametre(cmd, a);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Greška pri dodavanju artikla: " + ex.Message, ex);
            }
        }

        public static bool AzurirajArtikl(Artikl a)
        {
            string query = @"
                UPDATE artikli SET
                    sifra = @sifra, naziv = @naziv, opis = @opis,
                    nabavna_cijena = @nabavna_cijena, prodajna_cijena = @prodajna_cijena,
                    kolicina = @kolicina, grupa_id = @grupa_id,
                    jedinica_mjere_id = @jedinica_mjere_id, pdv_id = @pdv_id,
                    aktivan = @aktivan
                WHERE id = @id";

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        PopuniParametre(cmd, a);
                        cmd.Parameters.AddWithValue("@id", a.Id);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Greška pri ažuriranju artikla: " + ex.Message, ex);
            }
        }

        public static bool ObrisiArtikl(int id)
        {
            // Soft delete - samo deaktivira
            string query = "UPDATE artikli SET aktivan = 0 WHERE id = @id";

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
                throw new Exception("Greška pri brisanju artikla: " + ex.Message, ex);
            }
        }

        public static bool SifraPostoji(string sifra, int excludeId = 0)
        {
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                string q = "SELECT COUNT(1) FROM artikli WHERE sifra = @sifra AND id <> @excludeId";
                using (SqlCommand cmd = new SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@sifra", sifra);
                    cmd.Parameters.AddWithValue("@excludeId", excludeId);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        //  ŠIFARNICI

        public static List<GrupaArtikala> GetGrupe()
        {
            var lista = new List<GrupaArtikala>();
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id, naziv FROM grupe_artikala ORDER BY naziv", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        lista.Add(new GrupaArtikala { Id = r.GetInt32(0), Naziv = r.GetString(1) });
            }
            return lista;
        }

        public static List<JedinicaMjere> GetJediniceMjere()
        {
            var lista = new List<JedinicaMjere>();
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id, naziv, kratica FROM jedinice_mjere ORDER BY naziv", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        lista.Add(new JedinicaMjere
                        {
                            Id = r.GetInt32(0),
                            Naziv = r.GetString(1),
                            Skracenica = r.IsDBNull(2) ? "" : r.GetString(2)
                        });
            }
            return lista;
        }

        public static List<Pdv> GetPdvStope()
        {
            var lista = new List<Pdv>();
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id, opis, stopa FROM pdv ORDER BY stopa", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        lista.Add(new Pdv
                        {
                            Id = r.GetInt32(0),
                            Naziv = r.GetString(1),
                            Stopa = r.GetDecimal(2)
                        });
            }
            return lista;
        }

        //  HELPERI

        private static Artikl MapArtikl(SqlDataReader r)
        {
            return new Artikl
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Sifra = r.IsDBNull(r.GetOrdinal("sifra")) ? "" : r.GetString(r.GetOrdinal("sifra")),
                Naziv = r.GetString(r.GetOrdinal("naziv")),
                Opis = r.IsDBNull(r.GetOrdinal("opis")) ? "" : r.GetString(r.GetOrdinal("opis")),
                CijenaNabave = r.IsDBNull(r.GetOrdinal("nabavna_cijena")) ? 0 : r.GetDecimal(r.GetOrdinal("nabavna_cijena")),
                CijenaProdaje = r.IsDBNull(r.GetOrdinal("prodajna_cijena")) ? 0 : r.GetDecimal(r.GetOrdinal("prodajna_cijena")),
                Kolicina = r.IsDBNull(r.GetOrdinal("kolicina")) ? 0 : r.GetDecimal(r.GetOrdinal("kolicina")),
                IdGrupe = r.IsDBNull(r.GetOrdinal("grupa_id")) ? 0 : r.GetInt32(r.GetOrdinal("grupa_id")),
                NazivGrupe = r.IsDBNull(r.GetOrdinal("naziv_grupe")) ? "" : r.GetString(r.GetOrdinal("naziv_grupe")),
                IdJediniceMjere = r.IsDBNull(r.GetOrdinal("jedinica_mjere_id")) ? 0 : r.GetInt32(r.GetOrdinal("jedinica_mjere_id")),
                NazivJediniceMjere = r.IsDBNull(r.GetOrdinal("naziv_jedinice")) ? "" : r.GetString(r.GetOrdinal("naziv_jedinice")),
                IdPdv = r.IsDBNull(r.GetOrdinal("pdv_id")) ? 0 : r.GetInt32(r.GetOrdinal("pdv_id")),
                PdvStopa = r.IsDBNull(r.GetOrdinal("pdv_stopa")) ? 0 : r.GetDecimal(r.GetOrdinal("pdv_stopa")),
                Aktivan = !r.IsDBNull(r.GetOrdinal("aktivan")) && r.GetBoolean(r.GetOrdinal("aktivan"))
            };
        }

        private static void PopuniParametre(SqlCommand cmd, Artikl a)
        {
            cmd.Parameters.AddWithValue("@sifra", a.Sifra ?? "");
            cmd.Parameters.AddWithValue("@naziv", a.Naziv ?? "");
            cmd.Parameters.AddWithValue("@opis", a.Opis ?? "");
            cmd.Parameters.AddWithValue("@nabavna_cijena", a.CijenaNabave);
            cmd.Parameters.AddWithValue("@prodajna_cijena", a.CijenaProdaje);
            cmd.Parameters.AddWithValue("@kolicina", a.Kolicina);
            cmd.Parameters.AddWithValue("@grupa_id", a.IdGrupe > 0 ? (object)a.IdGrupe : DBNull.Value);
            cmd.Parameters.AddWithValue("@jedinica_mjere_id", a.IdJediniceMjere > 0 ? (object)a.IdJediniceMjere : DBNull.Value);
            cmd.Parameters.AddWithValue("@pdv_id", a.IdPdv > 0 ? (object)a.IdPdv : DBNull.Value);
            cmd.Parameters.AddWithValue("@aktivan", a.Aktivan);
        }
    }
}
