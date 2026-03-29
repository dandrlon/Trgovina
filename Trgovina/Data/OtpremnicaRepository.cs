using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public static class OtpremnicaRepository
    {
        // ══════════════════════════════════════════════════════════════════
        //  LIST / SEARCH
        // ══════════════════════════════════════════════════════════════════

        public static List<Otpremnica> GetSveOtpremnice(
            string pretraga = "",
            string status = "",
            DateTime? datumOd = null,
            DateTime? datumDo = null)
        {
            var lista = new List<Otpremnica>();

            string sql = @"
                SELECT o.id, o.broj_otpremnice,
                       o.datum_otpremnice, o.datum_isporuke, o.datum_isporuke_real, o.datum_fakturiranja,
                       o.kupac_id,   p.naziv  AS naziv_kupca,
                       p.adresa AS adresa_kupca, p.oib AS oib_kupca, p.grad AS grad_kupca,
                       o.prodavac_id, ISNULL(pr.ime + ' ' + pr.prezime, '') AS naziv_prodavaca,
                       o.racun_id,   r.broj_racuna,
                       o.ukupno_vrijednost,
                       o.isporuceno, o.fakturirano, o.status, o.napomena, o.datum_kreiranja
                FROM otpremnice o
                INNER JOIN partneri  p  ON p.id  = o.kupac_id
                LEFT  JOIN prodavaci pr ON pr.id = o.prodavac_id
                LEFT  JOIN racuni    r  ON r.id  = o.racun_id
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(pretraga))
                sql += " AND (o.broj_otpremnice LIKE @pretraga OR p.naziv LIKE @pretraga)";
            if (!string.IsNullOrWhiteSpace(status))
                sql += " AND o.status = @status";
            if (datumOd.HasValue)
                sql += " AND o.datum_otpremnice >= @datumOd";
            if (datumDo.HasValue)
                sql += " AND o.datum_otpremnice <= @datumDo";

            sql += " ORDER BY o.datum_otpremnice DESC, o.id DESC";

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
                        lista.Add(MapOtpremnica(rdr));
            }
            return lista;
        }

        public static Otpremnica GetOtpremnicaById(int id)
        {
            Otpremnica o = null;
            string sql = @"
                SELECT o.id, o.broj_otpremnice,
                       o.datum_otpremnice, o.datum_isporuke, o.datum_isporuke_real, o.datum_fakturiranja,
                       o.kupac_id,   p.naziv  AS naziv_kupca,
                       p.adresa AS adresa_kupca, p.oib AS oib_kupca, p.grad AS grad_kupca,
                       o.prodavac_id, ISNULL(pr.ime + ' ' + pr.prezime, '') AS naziv_prodavaca,
                       o.racun_id,   r.broj_racuna,
                       o.ukupno_vrijednost,
                       o.isporuceno, o.fakturirano, o.status, o.napomena, o.datum_kreiranja
                FROM otpremnice o
                INNER JOIN partneri  p  ON p.id  = o.kupac_id
                LEFT  JOIN prodavaci pr ON pr.id = o.prodavac_id
                LEFT  JOIN racuni    r  ON r.id  = o.racun_id
                WHERE o.id = @id";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    if (rdr.Read()) o = MapOtpremnica(rdr);
            }
            if (o != null)
                o.Stavke = GetStavkeZaOtpremnicu(o.Id);
            return o;
        }

        // ══════════════════════════════════════════════════════════════════
        //  STAVKE
        // ══════════════════════════════════════════════════════════════════

        public static List<OtpremnicaStavka> GetStavkeZaOtpremnicu(int otpremnicaId)
        {
            var lista = new List<OtpremnicaStavka>();
            string sql = @"
                SELECT os.id, os.otpremnica_id, os.artikl_id,
                       a.sifra AS sifra_artikla, a.naziv AS naziv_artikla,
                       ISNULL(jm.kratica, '') AS naziv_jm,
                       os.rbr, os.kolicina, os.cijena_bez_pdv, os.iznos_bez_pdv, os.pdv_stopa,
                       os.napomena
                FROM otpremnica_stavke os
                INNER JOIN artikli       a  ON a.id  = os.artikl_id
                LEFT  JOIN jedinice_mjere jm ON jm.id = a.jedinica_mjere_id
                WHERE os.otpremnica_id = @id
                ORDER BY os.rbr";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", otpremnicaId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        lista.Add(MapStavka(rdr));
            }
            return lista;
        }

        // ══════════════════════════════════════════════════════════════════
        //  INSERT
        // ══════════════════════════════════════════════════════════════════

        public static int DodajOtpremnicu(Otpremnica o)
        {
            string sql = @"
                INSERT INTO otpremnice
                    (broj_otpremnice, datum_otpremnice, datum_isporuke, kupac_id, prodavac_id,
                     ukupno_vrijednost, isporuceno, fakturirano, status, napomena)
                VALUES
                    (@broj, @datum, @isporuka, @kupac, @prodavac,
                     @ukupno, 0, 0, @status, @napomena);
                SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                int newId;
                using (var cmd = new SqlCommand(sql, conn))
                {
                    DodajParametreOtpremnice(cmd, o);
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                foreach (var s in o.Stavke)
                {
                    s.OtpremnicaId = newId;
                    DodajStavku(conn, s);
                }
                return newId;
            }
        }

        // Ova metoda radi isto kao DodajOtpremnicu, ali koristi vanjski connection i
        // transakciju — potrebno za PretvoriUOtpremnicu u PonudaRepository.

        /// <summary>
        /// Dodaje otpremnicu unutar postojeće transakcije (koristi se pri pretvorbi ponude).
        /// Vraća ID nove otpremnice.
        /// </summary>
        public static int DodajOtpremnicuUTransakciji(SqlConnection conn, SqlTransaction tx, Otpremnica o)
        {
            string sql = @"
        INSERT INTO otpremnice
            (broj_otpremnice, datum_otpremnice, datum_isporuke, kupac_id, prodavac_id,
             ukupno_vrijednost, isporuceno, fakturirano, status, napomena)
        VALUES
            (@broj, @datum, @isporuka, @kupac, @prodavac,
             @ukupno, 0, 0, @status, @napomena);
        SELECT SCOPE_IDENTITY();";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@broj", o.BrojOtpremnice);
                cmd.Parameters.AddWithValue("@datum", o.DatumOtpremnice);
                cmd.Parameters.AddWithValue("@isporuka", o.DatumIsporuke.HasValue
                                                            ? (object)o.DatumIsporuke.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@kupac", o.KupacId);
                cmd.Parameters.AddWithValue("@prodavac", o.ProdavacId.HasValue
                                                            ? (object)o.ProdavacId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ukupno", o.UkupnoVrijednost);
                cmd.Parameters.AddWithValue("@status", o.Status ?? "KREIRANA");
                cmd.Parameters.AddWithValue("@napomena", string.IsNullOrEmpty(o.Napomena)
                                                            ? (object)DBNull.Value : o.Napomena);

                int newId = Convert.ToInt32(cmd.ExecuteScalar());

                foreach (var s in o.Stavke)
                {
                    s.OtpremnicaId = newId;
                    DodajStavkuUTransakciji(conn, tx, s);
                }
                return newId;
            }
        }

        private static void DodajStavkuUTransakciji(SqlConnection conn, SqlTransaction tx, OtpremnicaStavka s)
        {
            string sql = @"
        INSERT INTO otpremnica_stavke
            (otpremnica_id, artikl_id, rbr, kolicina,
             cijena_bez_pdv, iznos_bez_pdv, pdv_stopa, napomena)
        VALUES
            (@oId, @artiklId, @rbr, @kolicina,
             @cijena, @iznos, @pdvStopa, @napomena)";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@oId", s.OtpremnicaId);
                cmd.Parameters.AddWithValue("@artiklId", s.ArtiklId);
                cmd.Parameters.AddWithValue("@rbr", s.Rbr);
                cmd.Parameters.AddWithValue("@kolicina", s.Kolicina);
                cmd.Parameters.AddWithValue("@cijena", s.CijenaBezPdv);
                cmd.Parameters.AddWithValue("@iznos", s.IznosBezPdv);
                cmd.Parameters.AddWithValue("@pdvStopa", s.PdvStopa);
                cmd.Parameters.AddWithValue("@napomena", string.IsNullOrEmpty(s.Napomena)
                                                            ? (object)DBNull.Value : s.Napomena);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DodajStavku(SqlConnection conn, OtpremnicaStavka s)
        {
            string sql = @"
                INSERT INTO otpremnica_stavke
                    (otpremnica_id, artikl_id, rbr, kolicina,
                     cijena_bez_pdv, iznos_bez_pdv, pdv_stopa, napomena)
                VALUES
                    (@oId, @artiklId, @rbr, @kolicina,
                     @cijena, @iznos, @pdvStopa, @napomena)";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@oId", s.OtpremnicaId);
                cmd.Parameters.AddWithValue("@artiklId", s.ArtiklId);
                cmd.Parameters.AddWithValue("@rbr", s.Rbr);
                cmd.Parameters.AddWithValue("@kolicina", s.Kolicina);
                cmd.Parameters.AddWithValue("@cijena", s.CijenaBezPdv);
                cmd.Parameters.AddWithValue("@iznos", s.IznosBezPdv);
                cmd.Parameters.AddWithValue("@pdvStopa", s.PdvStopa);
                cmd.Parameters.AddWithValue("@napomena", string.IsNullOrEmpty(s.Napomena)
                                                            ? (object)DBNull.Value : s.Napomena);
                cmd.ExecuteNonQuery();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════════

        public static void AzurirajOtpremnicu(Otpremnica o)
        {
            string sqlUpdate = @"
                UPDATE otpremnice SET
                    broj_otpremnice  = @broj,
                    datum_otpremnice = @datum,
                    datum_isporuke   = @isporuka,
                    kupac_id         = @kupac,
                    prodavac_id      = @prodavac,
                    ukupno_vrijednost = @ukupno,
                    status           = @status,
                    napomena         = @napomena
                WHERE id = @id AND isporuceno = 0";   // ne možemo mijenjati isporučenu

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sqlUpdate, conn))
                {
                    DodajParametreOtpremnice(cmd, o);
                    cmd.Parameters.AddWithValue("@id", o.Id);
                    int affected = cmd.ExecuteNonQuery();
                    if (affected == 0)
                        throw new Exception("Otpremnica je već isporučena i ne može se mijenjati.");
                }
                using (var del = new SqlCommand(
                    "DELETE FROM otpremnica_stavke WHERE otpremnica_id = @id", conn))
                {
                    del.Parameters.AddWithValue("@id", o.Id);
                    del.ExecuteNonQuery();
                }
                foreach (var s in o.Stavke)
                {
                    s.OtpremnicaId = o.Id;
                    DodajStavku(conn, s);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  AKCIJE
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Isporučuje otpremnicu — smanjuje zalihu artikala unutar transakcije.
        /// </summary>
        public static void IsporuciOtpremnicu(int otpremnicaId)
        {
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Provjeri da nije već isporučena
                        using (var chk = new SqlCommand(
                            "SELECT isporuceno FROM otpremnice WHERE id = @id", conn, tx))
                        {
                            chk.Parameters.AddWithValue("@id", otpremnicaId);
                            var val = chk.ExecuteScalar();
                            if (val != null && (bool)val)
                                throw new Exception("Otpremnica je već isporučena.");
                        }

                        // Dohvati stavke
                        var stavke = new List<(int artiklId, decimal kolicina)>();
                        using (var cmd = new SqlCommand(
                            "SELECT artikl_id, kolicina FROM otpremnica_stavke WHERE otpremnica_id = @id",
                            conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", otpremnicaId);
                            using (var rdr = cmd.ExecuteReader())
                                while (rdr.Read())
                                    stavke.Add((rdr.GetInt32(0), rdr.GetDecimal(1)));
                        }

                        // Provjeri zalihu i skini
                        foreach (var (artiklId, kolicina) in stavke)
                        {
                            // Provjera negativne zalihe (opcionalno — makni ako ne želiš blokadu)
                            using (var chkZ = new SqlCommand(
                                "SELECT zaliha FROM artikli WHERE id = @id", conn, tx))
                            {
                                chkZ.Parameters.AddWithValue("@id", artiklId);
                                decimal zaliha = (decimal)chkZ.ExecuteScalar();
                                if (zaliha < kolicina)
                                    throw new Exception(
                                        $"Nedovoljno zalihe za artikl ID {artiklId}. " +
                                        $"Na stanju: {zaliha:N3}, potrebno: {kolicina:N3}.");
                            }

                            using (var upd = new SqlCommand(
                                "UPDATE artikli SET zaliha = zaliha - @kol WHERE id = @id",
                                conn, tx))
                            {
                                upd.Parameters.AddWithValue("@kol", kolicina);
                                upd.Parameters.AddWithValue("@id", artiklId);
                                upd.ExecuteNonQuery();
                            }
                        }

                        // Označi isporučenom
                        using (var upd = new SqlCommand(@"
                            UPDATE otpremnice SET
                                isporuceno          = 1,
                                datum_isporuke_real = GETDATE(),
                                status              = 'ISPORUČENA'
                            WHERE id = @id", conn, tx))
                        {
                            upd.Parameters.AddWithValue("@id", otpremnicaId);
                            upd.ExecuteNonQuery();
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

        /// <summary>
        /// Fakturira otpremnicu — kreira račun i veže ga za otpremnicu.
        /// Vraća ID novog računa.
        /// </summary>
        public static int FakturirajOtpremnicu(int otpremnicaId, DateTime? datumValute = null)
        {
            var otp = GetOtpremnicaById(otpremnicaId);
            if (otp == null)
                throw new Exception("Otpremnica nije pronađena.");
            if (!otp.Isporuceno)
                throw new Exception("Otpremnica mora biti isporučena prije fakturiranja.");
            if (otp.Fakturirano)
                throw new Exception("Otpremnica je već fakturirana.");

            // Kreiraj Racun objekt iz otpremnice
            var stavkeRacuna = new System.Collections.Generic.List<RacunStavka>();
            foreach (var s in otp.Stavke)
            {
                decimal cijenaSaPdv = s.CijenaSaPdv();
                decimal iznosBezPdv = s.IznosBezPdv;
                decimal iznosSaPdv = s.IznosSaPdv();
                decimal iznosPdv = iznosSaPdv - iznosBezPdv;

                stavkeRacuna.Add(new RacunStavka
                {
                    ArtiklId = s.ArtiklId,
                    SifraArtikla = s.SifraArtikla,
                    NazivArtikla = s.NazivArtikla,
                    NazivJediniceMjere = s.NazivJediniceMjere,
                    Rbr = s.Rbr,
                    Kolicina = s.Kolicina,
                    ProdajnaCijena = cijenaSaPdv,
                    Popust = 0,
                    PdvStopa = s.PdvStopa,
                    IznosBezPdv = iznosBezPdv,
                    IznosPdv = iznosPdv,
                    IznosSaPdv = iznosSaPdv
                });
            }

            var racun = new Racun
            {
                BrojRacuna = RacuniRepository.GeneriraBrojRacuna(),
                DatumRacuna = DateTime.Today,
                DatumValute = datumValute ?? DateTime.Today.AddDays(30),
                DatumIsporuke = otp.DatumIsporukeReal ?? otp.DatumIsporuke ?? DateTime.Today,
                KupacId = otp.KupacId,
                NazivKupca = otp.NazivKupca,
                AdresaKupca = otp.AdresaKupca,
                OibKupca = otp.OibKupca,
                PdvIdKupca = "HR" + otp.OibKupca,
                ProdavacId = otp.ProdavacId,
                UkupnoBezPdv = stavkeRacuna.Sum(s => s.IznosBezPdv),
                UkupnoPdv = stavkeRacuna.Sum(s => s.IznosPdv),
                UkupnoSaPdv = stavkeRacuna.Sum(s => s.IznosSaPdv),
                Placeno = false,
                Proknjizeno = true,    // račun iz otpremnice je automatski proknjižen (zaliha već skinuta)
                Status = "PROKNJIZENO",
                Napomena = $"Fakturirano na osnovi otpremnice {otp.BrojOtpremnice}",
                Stavke = stavkeRacuna
            };

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Insert račun + stavke
                        int racunId = RacuniRepository.DodajRacunUTransakciji(conn, tx, racun);

                        // Ažuriraj otpremnicu
                        using (var upd = new SqlCommand(@"
                            UPDATE otpremnice SET
                                fakturirano        = 1,
                                datum_fakturiranja = GETDATE(),
                                racun_id           = @racunId,
                                status             = 'FAKTURIRANA'
                            WHERE id = @id", conn, tx))
                        {
                            upd.Parameters.AddWithValue("@racunId", racunId);
                            upd.Parameters.AddWithValue("@id", otpremnicaId);
                            upd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return racunId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static void ObrisiOtpremnicu(int id)
        {
            string sql = "DELETE FROM otpremnice WHERE id = @id AND isporuceno = 0 AND fakturirano = 0";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new Exception("Otpremnica je isporučena ili fakturirana i ne može se obrisati.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  ŠIFARNICI
        // ══════════════════════════════════════════════════════════════════

        public static string GeneriraBrojOtpremnice()
        {
            string sql = "SELECT COUNT(*) FROM otpremnice WHERE YEAR(datum_otpremnice) = @god";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@god", DateTime.Now.Year);
                conn.Open();
                int count = (int)cmd.ExecuteScalar() + 1;
                return $"OTP-{DateTime.Now.Year}-{count:D4}";
            }
        }

        public static bool BrojOtpremnicaPostoji(string broj, int excludeId = 0)
        {
            string sql = "SELECT COUNT(*) FROM otpremnice WHERE broj_otpremnice = @broj AND id <> @id";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@broj", broj);
                cmd.Parameters.AddWithValue("@id", excludeId);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  MAPPERS
        // ══════════════════════════════════════════════════════════════════

        private static Otpremnica MapOtpremnica(IDataReader rdr)
        {
            return new Otpremnica
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                BrojOtpremnice = rdr["broj_otpremnice"].ToString(),
                DatumOtpremnice = rdr.GetDateTime(rdr.GetOrdinal("datum_otpremnice")),
                DatumIsporuke = rdr["datum_isporuke"] == DBNull.Value ? (DateTime?)null
                                        : rdr.GetDateTime(rdr.GetOrdinal("datum_isporuke")),
                DatumIsporukeReal = rdr["datum_isporuke_real"] == DBNull.Value ? (DateTime?)null
                                        : rdr.GetDateTime(rdr.GetOrdinal("datum_isporuke_real")),
                DatumFakturiranja = rdr["datum_fakturiranja"] == DBNull.Value ? (DateTime?)null
                                        : rdr.GetDateTime(rdr.GetOrdinal("datum_fakturiranja")),
                KupacId = rdr.GetInt32(rdr.GetOrdinal("kupac_id")),
                NazivKupca = rdr["naziv_kupca"].ToString(),
                AdresaKupca = rdr["adresa_kupca"].ToString(),
                OibKupca = rdr["oib_kupca"] == DBNull.Value ? null : rdr["oib_kupca"].ToString(),
                GradKupca = rdr["grad_kupca"].ToString(),
                ProdavacId = rdr["prodavac_id"] == DBNull.Value ? (int?)null
                                        : rdr.GetInt32(rdr.GetOrdinal("prodavac_id")),
                NazivProdavaca = rdr["naziv_prodavaca"].ToString(),
                RacunId = rdr["racun_id"] == DBNull.Value ? (int?)null
                                        : rdr.GetInt32(rdr.GetOrdinal("racun_id")),
                BrojRacuna = rdr["broj_racuna"] == DBNull.Value ? null
                                        : rdr["broj_racuna"].ToString(),
                UkupnoVrijednost = rdr.GetDecimal(rdr.GetOrdinal("ukupno_vrijednost")),
                Isporuceno = rdr.GetBoolean(rdr.GetOrdinal("isporuceno")),
                Fakturirano = rdr.GetBoolean(rdr.GetOrdinal("fakturirano")),
                Status = rdr["status"].ToString(),
                Napomena = rdr["napomena"] == DBNull.Value ? null : rdr["napomena"].ToString(),
                DatumKreiranja = rdr.GetDateTime(rdr.GetOrdinal("datum_kreiranja"))
            };
        }

        private static OtpremnicaStavka MapStavka(IDataReader rdr)
        {
            return new OtpremnicaStavka
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                OtpremnicaId = rdr.GetInt32(rdr.GetOrdinal("otpremnica_id")),
                ArtiklId = rdr.GetInt32(rdr.GetOrdinal("artikl_id")),
                SifraArtikla = rdr["sifra_artikla"].ToString(),
                NazivArtikla = rdr["naziv_artikla"].ToString(),
                NazivJediniceMjere = rdr["naziv_jm"].ToString(),
                Rbr = rdr.GetInt32(rdr.GetOrdinal("rbr")),
                Kolicina = rdr.GetDecimal(rdr.GetOrdinal("kolicina")),
                CijenaBezPdv = rdr.GetDecimal(rdr.GetOrdinal("cijena_bez_pdv")),
                IznosBezPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_bez_pdv")),
                PdvStopa = rdr.GetDecimal(rdr.GetOrdinal("pdv_stopa")),
                Napomena = rdr["napomena"] == DBNull.Value ? null : rdr["napomena"].ToString()
            };
        }

        private static void DodajParametreOtpremnice(SqlCommand cmd, Otpremnica o)
        {
            cmd.Parameters.AddWithValue("@broj", o.BrojOtpremnice);
            cmd.Parameters.AddWithValue("@datum", o.DatumOtpremnice);
            cmd.Parameters.AddWithValue("@isporuka", o.DatumIsporuke.HasValue
                                                        ? (object)o.DatumIsporuke.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@kupac", o.KupacId);
            cmd.Parameters.AddWithValue("@prodavac", o.ProdavacId.HasValue
                                                        ? (object)o.ProdavacId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ukupno", o.UkupnoVrijednost);
            cmd.Parameters.AddWithValue("@status", o.Status ?? "KREIRANA");
            cmd.Parameters.AddWithValue("@napomena", string.IsNullOrEmpty(o.Napomena)
                                                        ? (object)DBNull.Value : o.Napomena);
        }
    }
}