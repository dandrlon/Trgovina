using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public static class RacuniRepository
    {
        // ══════════════════════════════════════════════════════════════════════
        //  LIST / SEARCH
        // ══════════════════════════════════════════════════════════════════════

        public static List<Racun> GetSviRacuni(
            string pretraga = "",
            string status = "",
            DateTime? datumOd = null,
            DateTime? datumDo = null)
        {
            var lista = new List<Racun>();

            string sql = @"
                SELECT r.id, r.broj_racuna, r.datum_racuna, r.datum_valute,
                       r.kupac_id, p.naziv AS naziv_kupca,
                       p.adresa AS adresa_kupca, p.oib AS oib_kupca, p.grad AS grad_kupca, p.postanski_broj,
                       r.prodavac_id, pr.ime + ' ' + pr.prezime AS naziv_prodavaca,
                       r.ukupno_bez_pdv, r.ukupno_pdv, r.ukupno_sa_pdv,
                       r.placeno, r.datum_placanja,
                       r.proknjizeno, r.datum_knjizenja,r.datum_isporuke,
                       r.status, r.napomena, r.datum_kreiranja
                FROM racuni r
                INNER JOIN partneri p   ON p.id  = r.kupac_id
                LEFT  JOIN prodavaci pr ON pr.id = r.prodavac_id
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(pretraga))
                sql += " AND (r.broj_racuna LIKE @pretraga OR p.naziv LIKE @pretraga)";
            if (!string.IsNullOrWhiteSpace(status))
                sql += " AND r.status = @status";
            if (datumOd.HasValue)
                sql += " AND r.datum_racuna >= @datumOd";
            if (datumDo.HasValue)
                sql += " AND r.datum_racuna <= @datumDo";

            sql += " ORDER BY r.datum_racuna DESC, r.id DESC";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (!string.IsNullOrWhiteSpace(pretraga))
                    cmd.Parameters.AddWithValue("@pretraga", "%" + pretraga.Trim() + "%");
                if (!string.IsNullOrWhiteSpace(status))
                    cmd.Parameters.AddWithValue("@status", status);
                if (datumOd.HasValue)
                    cmd.Parameters.AddWithValue("@datumOd", datumOd.Value.Date);
                if (datumDo.HasValue)
                    cmd.Parameters.AddWithValue("@datumDo", datumDo.Value.Date);

                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        lista.Add(MapRacun(rdr));
            }

            return lista;
        }

        public static Racun GetRacunById(int id)
        {
            Racun racun = null;

            string sql = @"
                SELECT r.id, r.broj_racuna, r.datum_racuna, r.datum_valute,
                       r.kupac_id, p.naziv AS naziv_kupca,
                       p.adresa AS adresa_kupca, p.oib AS oib_kupca, p.grad AS grad_kupca, p.postanski_broj,
                       r.prodavac_id, pr.ime + ' ' + pr.prezime AS naziv_prodavaca,
                       r.ukupno_bez_pdv, r.ukupno_pdv, r.ukupno_sa_pdv,
                       r.placeno, r.datum_placanja, r.datum_isporuke,
                       r.proknjizeno, r.datum_knjizenja,
                       r.status, r.napomena, r.datum_kreiranja
                FROM racuni r
                INNER JOIN partneri p   ON p.id  = r.kupac_id
                LEFT  JOIN prodavaci pr ON pr.id = r.prodavac_id
                WHERE r.id = @id";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    if (rdr.Read())
                        racun = MapRacun(rdr);
            }

            if (racun != null)
                racun.Stavke = GetStavkeZaRacun(racun.Id);

            return racun;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  STAVKE
        // ══════════════════════════════════════════════════════════════════════

        public static List<RacunStavka> GetStavkeZaRacun(int racunId)
        {
            var lista = new List<RacunStavka>();

            string sql = @"
                SELECT rs.id, rs.racun_id, rs.artikl_id,
                       a.sifra AS sifra_artikla, a.naziv AS naziv_artikla,
                       ISNULL(jm.kratica, '') AS naziv_jm,
                       rs.rbr, rs.kolicina, rs.prodajna_cijena, rs.popust, rs.pdv_stopa,
                       rs.iznos_bez_pdv, rs.iznos_pdv, rs.iznos_sa_pdv
                FROM racun_stavke rs
                INNER JOIN artikli a ON a.id = rs.artikl_id
                LEFT  JOIN jedinice_mjere jm ON jm.id = a.jedinica_mjere_id
                WHERE rs.racun_id = @racunId
                ORDER BY rs.rbr";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@racunId", racunId);
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

        public static int DodajRacun(Racun r)
        {
            string sql = @"
                INSERT INTO racuni
                    (broj_racuna, datum_racuna, datum_valute, kupac_id, prodavac_id,
                     ukupno_bez_pdv, ukupno_pdv, ukupno_sa_pdv,
                     placeno, proknjizeno, status, napomena)
                VALUES
                    (@broj, @datum, @valuta, @kupac, @prodavac,
                     @bezPdv, @pdv, @saPdv,
                     @placeno, 0, @status, @napomena);
                SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                int newId;
                using (var cmd = new SqlCommand(sql, conn))
                {
                    DodajParametreRacuna(cmd, r);
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                foreach (var s in r.Stavke)
                {
                    s.RacunId = newId;
                    DodajStavku(conn, s);
                }
                return newId;
            }
        }

        private static void DodajStavku(SqlConnection conn, RacunStavka s)
        {
            string sql = @"
                INSERT INTO racun_stavke
                    (racun_id, artikl_id, rbr, kolicina, prodajna_cijena, popust,
                     pdv_stopa, iznos_bez_pdv, iznos_pdv, iznos_sa_pdv)
                VALUES
                    (@racunId, @artiklId, @rbr, @kolicina, @cijena, @popust,
                     @pdvStopa, @bezPdv, @pdv, @saPdv)";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@racunId", s.RacunId);
                cmd.Parameters.AddWithValue("@artiklId", s.ArtiklId);
                cmd.Parameters.AddWithValue("@rbr", s.Rbr);
                cmd.Parameters.AddWithValue("@kolicina", s.Kolicina);
                cmd.Parameters.AddWithValue("@cijena", s.ProdajnaCijena);
                cmd.Parameters.AddWithValue("@popust", s.Popust);
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

        public static void AzurirajRacun(Racun r)
        {
            string sqlUpdate = @"
                UPDATE racuni SET
                    broj_racuna    = @broj,
                    datum_racuna   = @datum,
                    datum_valute   = @valuta,
                    kupac_id       = @kupac,
                    prodavac_id    = @prodavac,
                    ukupno_bez_pdv = @bezPdv,
                    ukupno_pdv     = @pdv,
                    ukupno_sa_pdv  = @saPdv,
                    placeno        = @placeno,
                    status         = @status,
                    napomena       = @napomena
                WHERE id = @id";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sqlUpdate, conn))
                {
                    DodajParametreRacuna(cmd, r);
                    cmd.Parameters.AddWithValue("@id", r.Id);
                    cmd.ExecuteNonQuery();
                }
                using (var del = new SqlCommand("DELETE FROM racun_stavke WHERE racun_id = @id", conn))
                {
                    del.Parameters.AddWithValue("@id", r.Id);
                    del.ExecuteNonQuery();
                }
                foreach (var s in r.Stavke)
                {
                    s.RacunId = r.Id;
                    DodajStavku(conn, s);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  AKCIJE
        // ══════════════════════════════════════════════════════════════════════

        public static void OznaciPlacenim(int racunId)
        {
            string sql = @"UPDATE racuni
                           SET placeno = 1, datum_placanja = GETDATE(), status = 'PLAĆENO'
                           WHERE id = @id";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", racunId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Proknjižava račun — skida količine sa zalihe unutar transakcije.
        /// </summary>
        public static void ProkniziRacun(int racunId)
        {
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var stavke = new List<(int artiklId, decimal kolicina)>();
                        using (var cmd = new SqlCommand(
                            "SELECT artikl_id, kolicina FROM racun_stavke WHERE racun_id = @id",
                            conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", racunId);
                            using (var rdr = cmd.ExecuteReader())
                                while (rdr.Read())
                                    stavke.Add((rdr.GetInt32(0), rdr.GetDecimal(1)));
                        }

                        foreach (var (artiklId, kolicina) in stavke)
                        {
                            using (var cmd = new SqlCommand(
                                "UPDATE artikli SET zaliha = zaliha - @kol WHERE id = @id",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@kol", kolicina);
                                cmd.Parameters.AddWithValue("@id", artiklId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (var cmd = new SqlCommand(
                            "UPDATE racuni SET proknjizeno = 1, datum_knjizenja = GETDATE(), status = 'PROKNJIZENO' WHERE id = @id",
                            conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", racunId);
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

        public static void ObrisiRacun(int racunId)
        {
            string sql = "DELETE FROM racuni WHERE id = @id AND proknjizeno = 0";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", racunId);
                conn.Open();
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception("Račun je već proknjižen i ne može se obrisati.");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ŠIFARNICI
        // ══════════════════════════════════════════════════════════════════════

        public static string GeneriraBrojRacuna()
        {
            string sql = "SELECT COUNT(*) FROM racuni WHERE YEAR(datum_racuna) = @god";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@god", DateTime.Now.Year);
                conn.Open();
                int count = (int)cmd.ExecuteScalar() + 1;
                return $"R-{DateTime.Now.Year}-{count:D4}";
            }
        }

        public static bool BrojRacunaPostoji(string broj, int excludeId = 0)
        {
            string sql = "SELECT COUNT(*) FROM racuni WHERE broj_racuna = @broj AND id <> @id";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@broj", broj);
                cmd.Parameters.AddWithValue("@id", excludeId);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static List<Artikl> GetArtikliZaOdabir()
        {
            var lista = new List<Artikl>();
            string sql = @"
                SELECT a.id, a.sifra, a.naziv, a.prodajna_cijena, a.zaliha,
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
                            CijenaProdaje = rdr.GetDecimal(3),
                            Kolicina = rdr.GetDecimal(4),
                            NazivJediniceMjere = rdr["jm"].ToString(),
                            PdvStopa = rdr.GetDecimal(6)
                        });
            }
            return lista;
        }

        public static List<Partner> GetKupci()
        {
            var lista = new List<Partner>();
            string sql = @"SELECT id, naziv, ISNULL(oib,'') AS oib, ISNULL(grad,'') AS grad, ISNULL(telefon,'') AS telefon
                           FROM partneri WHERE aktivan = 1 ORDER BY naziv";
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
                            Grad = rdr["grad"].ToString(),
                            Telefon = rdr["telefon"].ToString()
                        });
            }
            return lista;
        }

        public static List<Prodavac> GetProdavaci()
        {
            var lista = new List<Prodavac>();
            string sql = "SELECT id, ime, prezime FROM prodavaci WHERE aktivan = 1 ORDER BY prezime";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        lista.Add(new Prodavac
                        {
                            Id = rdr.GetInt32(0),
                            Ime = rdr["ime"].ToString(),
                            Prezime = rdr["prezime"].ToString()
                        });
            }
            return lista;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MAPPERS
        // ══════════════════════════════════════════════════════════════════════

        private static Racun MapRacun(IDataReader rdr)
        {
            return new Racun
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                BrojRacuna = rdr["broj_racuna"].ToString(),
                DatumRacuna = rdr.GetDateTime(rdr.GetOrdinal("datum_racuna")),
                DatumValute = rdr["datum_valute"] == DBNull.Value ? (DateTime?)null
                                    : rdr.GetDateTime(rdr.GetOrdinal("datum_valute")),
                KupacId = rdr.GetInt32(rdr.GetOrdinal("kupac_id")),
                NazivKupca = rdr["naziv_kupca"].ToString(),
                ProdavacId = rdr["prodavac_id"] == DBNull.Value ? (int?)null
                                    : rdr.GetInt32(rdr.GetOrdinal("prodavac_id")),
                NazivProdavaca = rdr["naziv_prodavaca"] == DBNull.Value ? null
                                    : rdr["naziv_prodavaca"].ToString(),
                UkupnoBezPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_bez_pdv")),
                UkupnoPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_pdv")),
                UkupnoSaPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_sa_pdv")),
                Placeno = rdr.GetBoolean(rdr.GetOrdinal("placeno")),
                DatumPlacanja = rdr["datum_placanja"] == DBNull.Value ? (DateTime?)null
                                    : rdr.GetDateTime(rdr.GetOrdinal("datum_placanja")),
                Proknjizeno = rdr.GetBoolean(rdr.GetOrdinal("proknjizeno")),
                DatumKnjizenja = rdr["datum_knjizenja"] == DBNull.Value ? (DateTime?)null
                                    : rdr.GetDateTime(rdr.GetOrdinal("datum_knjizenja")),
                Status = rdr["status"].ToString(),
                Napomena = rdr["napomena"] == DBNull.Value ? null
                                    : rdr["napomena"].ToString(),
                DatumKreiranja = rdr.GetDateTime(rdr.GetOrdinal("datum_kreiranja")),
                OibKupca = rdr["oib_kupca"] == DBNull.Value ? null
                                    : rdr["oib_kupca"].ToString(),
                AdresaKupca = rdr["adresa_kupca"].ToString(),
                PdvIdKupca = "HR" + rdr["oib_kupca"].ToString(),
                DatumIsporuke = rdr["datum_isporuke"] == DBNull.Value ? (DateTime?)null
                                    : rdr.GetDateTime(rdr.GetOrdinal("datum_isporuke")),

            };
        }

        private static RacunStavka MapStavka(IDataReader rdr)
        {
            return new RacunStavka
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                RacunId = rdr.GetInt32(rdr.GetOrdinal("racun_id")),
                ArtiklId = rdr.GetInt32(rdr.GetOrdinal("artikl_id")),
                SifraArtikla = rdr["sifra_artikla"].ToString(),
                NazivArtikla = rdr["naziv_artikla"].ToString(),
                NazivJediniceMjere = rdr["naziv_jm"].ToString(),
                Rbr = rdr.GetInt32(rdr.GetOrdinal("rbr")),
                Kolicina = rdr.GetDecimal(rdr.GetOrdinal("kolicina")),
                ProdajnaCijena = rdr.GetDecimal(rdr.GetOrdinal("prodajna_cijena")),
                Popust = rdr.GetDecimal(rdr.GetOrdinal("popust")),
                PdvStopa = rdr.GetDecimal(rdr.GetOrdinal("pdv_stopa")),
                IznosBezPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_bez_pdv")),
                IznosPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_pdv")),
                IznosSaPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_sa_pdv"))
            };
        }

        private static void DodajParametreRacuna(SqlCommand cmd, Racun r)
        {
            cmd.Parameters.AddWithValue("@broj", r.BrojRacuna);
            cmd.Parameters.AddWithValue("@datum", r.DatumRacuna);
            cmd.Parameters.AddWithValue("@valuta", r.DatumValute.HasValue
                                                        ? (object)r.DatumValute.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@kupac", r.KupacId);
            cmd.Parameters.AddWithValue("@prodavac", r.ProdavacId.HasValue
                                                        ? (object)r.ProdavacId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@bezPdv", r.UkupnoBezPdv);
            cmd.Parameters.AddWithValue("@pdv", r.UkupnoPdv);
            cmd.Parameters.AddWithValue("@saPdv", r.UkupnoSaPdv);
            cmd.Parameters.AddWithValue("@placeno", r.Placeno ? 1 : 0);
            cmd.Parameters.AddWithValue("@status", r.Status ?? "KREIRAN");
            cmd.Parameters.AddWithValue("@napomena", string.IsNullOrEmpty(r.Napomena)
                                                        ? (object)DBNull.Value : r.Napomena);
        }


        // Ova metoda radi isto kao DodajRacun, ali koristi vanjski connection i
        // transakciju — potrebno za FakturirajOtpremnicu koji mora sve napraviti
        // unutar jedne transakcije.

        /// <summary>
        /// Dodaje račun unutar postojeće transakcije (koristi se pri fakturiranju otpremnice).
        /// Vraća ID novog računa.
        /// </summary>
        public static int DodajRacunUTransakciji(SqlConnection conn, SqlTransaction tx, Racun r)
        {
            string sql = @"
            INSERT INTO racuni
                (broj_racuna, datum_racuna, datum_valute, datum_isporuke,
                 kupac_id, prodavac_id,
                 ukupno_bez_pdv, ukupno_pdv, ukupno_sa_pdv,
                 placeno, proknjizeno, datum_knjizenja, status, napomena)
            VALUES
                (@broj, @datum, @valuta, @isporuka,
                 @kupac, @prodavac,
                 @bezPdv, @pdv, @saPdv,
                 @placeno, @proknjizeno, @datKnjiz, @status, @napomena);
            SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                DodajParametreRacuna(cmd, r);
                cmd.Parameters.AddWithValue("@isporuka", r.DatumIsporuke.HasValue
                                                                ? (object)r.DatumIsporuke.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@proknjizeno", r.Proknjizeno ? 1 : 0);
                cmd.Parameters.AddWithValue("@datKnjiz", r.Proknjizeno
                                                                ? (object)DateTime.Now : DBNull.Value);

                int newId = Convert.ToInt32(cmd.ExecuteScalar());

                foreach (var s in r.Stavke)
                {
                    s.RacunId = newId;
                    DodajStavkuUTransakciji(conn, tx, s);
                }

                return newId;
            }
        }

        private static void DodajStavkuUTransakciji(SqlConnection conn, SqlTransaction tx, RacunStavka s)
        {
            string sql = @"
        INSERT INTO racun_stavke
            (racun_id, artikl_id, rbr, kolicina, prodajna_cijena, popust,
             pdv_stopa, iznos_bez_pdv, iznos_pdv, iznos_sa_pdv)
        VALUES
            (@racunId, @artiklId, @rbr, @kolicina, @cijena, @popust,
             @pdvStopa, @bezPdv, @pdv, @saPdv)";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@racunId", s.RacunId);
                cmd.Parameters.AddWithValue("@artiklId", s.ArtiklId);
                cmd.Parameters.AddWithValue("@rbr", s.Rbr);
                cmd.Parameters.AddWithValue("@kolicina", s.Kolicina);
                cmd.Parameters.AddWithValue("@cijena", s.ProdajnaCijena);
                cmd.Parameters.AddWithValue("@popust", s.Popust);
                cmd.Parameters.AddWithValue("@pdvStopa", s.PdvStopa);
                cmd.Parameters.AddWithValue("@bezPdv", s.IznosBezPdv);
                cmd.Parameters.AddWithValue("@pdv", s.IznosPdv);
                cmd.Parameters.AddWithValue("@saPdv", s.IznosSaPdv);
                cmd.ExecuteNonQuery();
            }
        }
    }
}