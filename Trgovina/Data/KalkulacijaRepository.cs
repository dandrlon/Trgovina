using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public static class KalkulacijeRepository
    {
        // ══════════════════════════════════════════════════════════════════════
        //  LIST / SEARCH
        // ══════════════════════════════════════════════════════════════════════

        public static List<Kalkulacija> GetSveKalkulacije(
            string pretraga = "",
            DateTime? datumOd = null,
            DateTime? datumDo = null)
        {
            var lista = new List<Kalkulacija>();

            string sql = @"
                SELECT k.id, k.broj_kalkulacije, k.datum_kalkulacije,
                       k.dobavljac_id, ISNULL(p.naziv, '—') AS naziv_dobavljaca,
                       p.oib AS oib_dobavljaca, p.adresa AS adresa_dobavljaca,
                       ISNULL(k.broj_dobavljacevog_racuna, '') AS broj_dob_racuna,
                       k.ukupno_bez_pdv, k.ukupno_pdv, k.ukupno_sa_pdv,
                       k.proknjizeno, k.datum_knjizenja,
                       ISNULL(k.napomena, '') AS napomena,
                       k.datum_kreiranja
                FROM kalkulacije k
                LEFT JOIN partneri p ON p.id = k.dobavljac_id
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(pretraga))
                sql += " AND (k.broj_kalkulacije LIKE @pretraga OR p.naziv LIKE @pretraga OR k.broj_dobavljacevog_racuna LIKE @pretraga)";
            if (datumOd.HasValue)
                sql += " AND k.datum_kalkulacije >= @datumOd";
            if (datumDo.HasValue)
                sql += " AND k.datum_kalkulacije <= @datumDo";

            sql += " ORDER BY k.datum_kalkulacije DESC, k.id DESC";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (!string.IsNullOrWhiteSpace(pretraga))
                    cmd.Parameters.AddWithValue("@pretraga", "%" + pretraga.Trim() + "%");
                if (datumOd.HasValue)
                    cmd.Parameters.AddWithValue("@datumOd", datumOd.Value.Date);
                if (datumDo.HasValue)
                    cmd.Parameters.AddWithValue("@datumDo", datumDo.Value.Date);

                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        lista.Add(MapKalkulacija(rdr));
            }

            return lista;
        }

        public static Kalkulacija GetKalkulacijaById(int id)
        {
            Kalkulacija kalkulacija = null;

            string sql = @"
                SELECT k.id, k.broj_kalkulacije, k.datum_kalkulacije,
                       k.dobavljac_id, ISNULL(p.naziv, '—') AS naziv_dobavljaca,
                       p.oib AS oib_dobavljaca, p.adresa AS adresa_dobavljaca,
                       ISNULL(k.broj_dobavljacevog_racuna, '') AS broj_dob_racuna,
                       k.ukupno_bez_pdv, k.ukupno_pdv, k.ukupno_sa_pdv,
                       k.proknjizeno, k.datum_knjizenja,
                       ISNULL(k.napomena, '') AS napomena,
                       k.datum_kreiranja
                FROM kalkulacije k
                LEFT JOIN partneri p ON p.id = k.dobavljac_id
                WHERE k.id = @id";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    if (rdr.Read())
                        kalkulacija = MapKalkulacija(rdr);
            }

            if (kalkulacija != null)
                kalkulacija.Stavke = GetStavkeZaKalkulaciju(kalkulacija.Id);

            return kalkulacija;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  STAVKE
        // ══════════════════════════════════════════════════════════════════════

        public static List<KalkulacijaStavka> GetStavkeZaKalkulaciju(int kalkulacijaId)
        {
            var lista = new List<KalkulacijaStavka>();

            string sql = @"
                SELECT ks.id, ks.kalkulacija_id, ks.artikl_id,
                       a.sifra AS sifra_artikla, a.naziv AS naziv_artikla,
                       ISNULL(jm.kratica, 'kom') AS naziv_jm,
                       ks.rbr, ks.kolicina, ks.nabavna_cijena, ks.pdv_stopa,
                       ks.iznos_bez_pdv, ks.iznos_pdv, ks.iznos_sa_pdv
                FROM kalkulacija_stavke ks
                INNER JOIN artikli a ON a.id = ks.artikl_id
                LEFT  JOIN jedinice_mjere jm ON jm.id = a.jedinica_mjere_id
                WHERE ks.kalkulacija_id = @kalkulacijaId
                ORDER BY ks.rbr";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@kalkulacijaId", kalkulacijaId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        lista.Add(MapStavka(rdr));
            }

            return lista;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INSERT
        // ══════════════════════════════════════════════════════════════════════

        public static int DodajKalkulaciju(Kalkulacija k)
        {
            string sql = @"
                INSERT INTO kalkulacije
                    (broj_kalkulacije, datum_kalkulacije, dobavljac_id,
                     broj_dobavljacevog_racuna, ukupno_bez_pdv, ukupno_pdv,
                     ukupno_sa_pdv, proknjizeno, napomena)
                VALUES
                    (@broj, @datum, @dobavljac,
                     @brDobRacuna, @bezPdv, @pdv,
                     @saPdv, 0, @napomena);
                SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                int newId;
                using (var cmd = new SqlCommand(sql, conn))
                {
                    DodajParametreKalkulacije(cmd, k);
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                int rbr = 1;
                foreach (var s in k.Stavke)
                {
                    s.KalkulacijaId = newId;
                    s.Rbr = rbr++;
                    DodajStavku(conn, s);
                }
                return newId;
            }
        }

        private static void DodajStavku(SqlConnection conn, KalkulacijaStavka s,
            SqlTransaction tx = null)
        {
            string sql = @"
                INSERT INTO kalkulacija_stavke
                    (kalkulacija_id, artikl_id, rbr, kolicina, nabavna_cijena,
                     pdv_stopa, iznos_bez_pdv, iznos_pdv, iznos_sa_pdv)
                VALUES
                    (@kalkulacijaId, @artiklId, @rbr, @kolicina, @nabavnaCijena,
                     @pdvStopa, @bezPdv, @pdv, @saPdv)";

            using (var cmd = tx != null
                ? new SqlCommand(sql, conn, tx)
                : new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@kalkulacijaId", s.KalkulacijaId);
                cmd.Parameters.AddWithValue("@artiklId", s.ArtiklId);
                cmd.Parameters.AddWithValue("@rbr", s.Rbr);
                cmd.Parameters.AddWithValue("@kolicina", s.Kolicina);
                cmd.Parameters.AddWithValue("@nabavnaCijena", s.NabavnaCijena);
                cmd.Parameters.AddWithValue("@pdvStopa", s.PdvStopa);
                cmd.Parameters.AddWithValue("@bezPdv", s.IznosBezPdv);
                cmd.Parameters.AddWithValue("@pdv", s.IznosPdv);
                cmd.Parameters.AddWithValue("@saPdv", s.IznosSaPdv);
                cmd.ExecuteNonQuery();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════════════

        public static void AzurirajKalkulaciju(Kalkulacija k)
        {
            string sqlUpdate = @"
                UPDATE kalkulacije SET
                    broj_kalkulacije          = @broj,
                    datum_kalkulacije         = @datum,
                    dobavljac_id              = @dobavljac,
                    broj_dobavljacevog_racuna = @brDobRacuna,
                    ukupno_bez_pdv            = @bezPdv,
                    ukupno_pdv                = @pdv,
                    ukupno_sa_pdv             = @saPdv,
                    napomena                  = @napomena
                WHERE id = @id AND proknjizeno = 0";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sqlUpdate, conn))
                {
                    DodajParametreKalkulacije(cmd, k);
                    cmd.Parameters.AddWithValue("@id", k.Id);
                    int affected = cmd.ExecuteNonQuery();
                    if (affected == 0)
                        throw new Exception("Kalkulacija je proknjižena ili ne postoji — nije moguće urediti.");
                }

                using (var del = new SqlCommand(
                    "DELETE FROM kalkulacija_stavke WHERE kalkulacija_id = @id", conn))
                {
                    del.Parameters.AddWithValue("@id", k.Id);
                    del.ExecuteNonQuery();
                }

                int rbr = 1;
                foreach (var s in k.Stavke)
                {
                    s.KalkulacijaId = k.Id;
                    s.Rbr = rbr++;
                    DodajStavku(conn, s);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PROKNJIZAVANJE — povećava zalihe artikala (unutar transakcije)
        // ══════════════════════════════════════════════════════════════════════

        public static void ProknjiziKalkulaciju(int kalkulacijaId)
        {
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Provjeri nije li već proknjižena
                        using (var chk = new SqlCommand(
                            "SELECT proknjizeno FROM kalkulacije WHERE id = @id", conn, tx))
                        {
                            chk.Parameters.AddWithValue("@id", kalkulacijaId);
                            var val = chk.ExecuteScalar();
                            if (val == null)
                                throw new Exception("Kalkulacija nije pronađena.");
                            if (Convert.ToBoolean(val))
                                throw new Exception("Kalkulacija je već proknjižena.");
                        }

                        // Dohvati stavke
                        var stavke = new List<(int artiklId, decimal kolicina, decimal nabavnaCijena)>();
                        using (var cmd = new SqlCommand(
                            "SELECT artikl_id, kolicina, nabavna_cijena FROM kalkulacija_stavke WHERE kalkulacija_id = @id",
                            conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", kalkulacijaId);
                            using (var rdr = cmd.ExecuteReader())
                                while (rdr.Read())
                                    stavke.Add((rdr.GetInt32(0), rdr.GetDecimal(1), rdr.GetDecimal(2)));
                        }

                        // Povećaj zalihe i ažuriraj nabavnu cijenu
                        foreach (var (artiklId, kolicina, nabavnaCijena) in stavke)
                        {
                            using (var cmd = new SqlCommand(@"
                                UPDATE artikli
                                SET zaliha          = ISNULL(zaliha, 0) + @kol,
                                    nabavna_cijena  = @nc
                                WHERE id = @id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@kol", kolicina);
                                cmd.Parameters.AddWithValue("@nc", nabavnaCijena);
                                cmd.Parameters.AddWithValue("@id", artiklId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Označi kalkulaciju proknjiženom
                        using (var cmd = new SqlCommand(@"
                            UPDATE kalkulacije
                            SET proknjizeno    = 1,
                                datum_knjizenja = GETDATE()
                            WHERE id = @id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", kalkulacijaId);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DELETE
        // ══════════════════════════════════════════════════════════════════════

        public static void ObrisiKalkulaciju(int kalkulacijaId)
        {
            string sql = "DELETE FROM kalkulacije WHERE id = @id AND proknjizeno = 0";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", kalkulacijaId);
                conn.Open();
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception("Kalkulacija je proknjižena ili ne postoji — nije moguće obrisati.");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ŠIFARNICI
        // ══════════════════════════════════════════════════════════════════════

        public static string GeneriraBrojKalkulacije()
        {
            string sql = "SELECT COUNT(*) FROM kalkulacije WHERE YEAR(datum_kalkulacije) = @god";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@god", DateTime.Now.Year);
                conn.Open();
                int count = (int)cmd.ExecuteScalar() + 1;
                return $"K-{DateTime.Now.Year}-{count:D4}";
            }
        }

        public static bool BrojKalkulacijePostoji(string broj, int excludeId = 0)
        {
            string sql = "SELECT COUNT(*) FROM kalkulacije WHERE broj_kalkulacije = @broj AND id <> @id";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@broj", broj);
                cmd.Parameters.AddWithValue("@id", excludeId);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static List<Partner> GetDobavljaci()
        {
            var lista = new List<Partner>();
            string sql = @"
                SELECT id, naziv, ISNULL(oib,'') AS oib,
                       ISNULL(adresa,'') AS adresa,
                       ISNULL(grad,'') AS grad,
                       ISNULL(telefon,'') AS telefon
                FROM partneri
                WHERE aktivan = 1
                ORDER BY naziv";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        lista.Add(new Partner
                        {
                            Id = rdr.GetInt32(0),
                            Naziv = rdr["naziv"].ToString(),
                            OIB = rdr["oib"].ToString(),
                            Adresa = rdr["adresa"].ToString(),
                            Grad = rdr["grad"].ToString(),
                            Telefon = rdr["telefon"].ToString()
                        });
            }
            return lista;
        }

        public static List<Artikl> GetArtikliZaOdabir()
        {
            var lista = new List<Artikl>();
            string sql = @"
                SELECT a.id, a.sifra, a.naziv, a.nabavna_cijena, a.zaliha,
                       ISNULL(jm.kratica, '') AS jm,
                       ISNULL(p.stopa, 0)    AS pdv_stopa
                FROM artikli a
                LEFT JOIN jedinice_mjere jm ON jm.id = a.jedinica_mjere_id
                LEFT JOIN pdv p             ON p.id  = a.pdv_id
                WHERE a.aktivan = 1
                ORDER BY a.naziv";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        lista.Add(new Artikl
                        {
                            Id = rdr.GetInt32(0),
                            Sifra = rdr["sifra"].ToString(),
                            Naziv = rdr["naziv"].ToString(),
                            CijenaNabave = rdr.GetDecimal(3),
                            Kolicina = rdr.GetDecimal(4),
                            NazivJediniceMjere = rdr["jm"].ToString(),
                            PdvStopa = rdr.GetDecimal(6)
                        });
            }
            return lista;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MAPPERS
        // ══════════════════════════════════════════════════════════════════════

        private static Kalkulacija MapKalkulacija(IDataReader rdr)
        {
            return new Kalkulacija
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                BrojKalkulacije = rdr["broj_kalkulacije"].ToString(),
                DatumKalkulacije = rdr.GetDateTime(rdr.GetOrdinal("datum_kalkulacije")),
                DobavljacId = rdr["dobavljac_id"] == DBNull.Value ? (int?)null
                                            : rdr.GetInt32(rdr.GetOrdinal("dobavljac_id")),
                NazivDobavljaca = rdr["naziv_dobavljaca"].ToString(),
                BrojDobavljacevogRacuna = rdr["broj_dob_racuna"].ToString(),
                UkupnoBezPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_bez_pdv")),
                UkupnoPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_pdv")),
                UkupnoSaPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_sa_pdv")),
                Proknjizeno = rdr.GetBoolean(rdr.GetOrdinal("proknjizeno")),
                DatumKnjizenja = rdr["datum_knjizenja"] == DBNull.Value ? (DateTime?)null
                                            : rdr.GetDateTime(rdr.GetOrdinal("datum_knjizenja")),
                Napomena = rdr["napomena"].ToString(),
                DatumKreiranja = rdr.GetDateTime(rdr.GetOrdinal("datum_kreiranja")),
                OibDobavljaca = rdr["oib_dobavljaca"].ToString(),
                AdresaDobavljaca = rdr["adresa_dobavljaca"].ToString()
            };
        }

        private static KalkulacijaStavka MapStavka(IDataReader rdr)
        {
            return new KalkulacijaStavka
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                KalkulacijaId = rdr.GetInt32(rdr.GetOrdinal("kalkulacija_id")),
                ArtiklId = rdr.GetInt32(rdr.GetOrdinal("artikl_id")),
                SifraArtikla = rdr["sifra_artikla"].ToString(),
                NazivArtikla = rdr["naziv_artikla"].ToString(),
                NazivJM = rdr["naziv_jm"].ToString(),
                Rbr = rdr.GetInt32(rdr.GetOrdinal("rbr")),
                Kolicina = rdr.GetDecimal(rdr.GetOrdinal("kolicina")),
                NabavnaCijena = rdr.GetDecimal(rdr.GetOrdinal("nabavna_cijena")),
                PdvStopa = rdr.GetDecimal(rdr.GetOrdinal("pdv_stopa")),
                IznosBezPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_bez_pdv")),
                IznosPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_pdv")),
                IznosSaPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_sa_pdv"))
            };
        }

        private static void DodajParametreKalkulacije(SqlCommand cmd, Kalkulacija k)
        {
            cmd.Parameters.AddWithValue("@broj", k.BrojKalkulacije);
            cmd.Parameters.AddWithValue("@datum", k.DatumKalkulacije);
            cmd.Parameters.AddWithValue("@dobavljac", k.DobavljacId.HasValue
                                                            ? (object)k.DobavljacId.Value
                                                            : DBNull.Value);
            cmd.Parameters.AddWithValue("@brDobRacuna", string.IsNullOrEmpty(k.BrojDobavljacevogRacuna)
                                                            ? (object)DBNull.Value
                                                            : k.BrojDobavljacevogRacuna);
            cmd.Parameters.AddWithValue("@bezPdv", k.UkupnoBezPdv);
            cmd.Parameters.AddWithValue("@pdv", k.UkupnoPdv);
            cmd.Parameters.AddWithValue("@saPdv", k.UkupnoSaPdv);
            cmd.Parameters.AddWithValue("@napomena", string.IsNullOrEmpty(k.Napomena)
                                                            ? (object)DBNull.Value
                                                            : k.Napomena);
        }
    }
}