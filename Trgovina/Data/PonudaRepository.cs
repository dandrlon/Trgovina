using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public static class PonudaRepository
    {
        // ══════════════════════════════════════════════════════════════════
        //  LIST / SEARCH
        // ══════════════════════════════════════════════════════════════════

        public static List<Ponuda> GetSvePonude(
            string pretraga = "",
            string status = "",
            DateTime? datumOd = null,
            DateTime? datumDo = null)
        {
            var lista = new List<Ponuda>();

            string sql = @"
                SELECT po.id, po.broj_ponude,
                       po.datum_ponude, po.datum_vazenja, po.datum_slanja, po.datum_odgovora,
                       po.kupac_id,   p.naziv  AS naziv_kupca,
                       p.adresa AS adresa_kupca, p.oib AS oib_kupca, p.grad AS grad_kupca,
                       po.prodavac_id, ISNULL(pr.ime + ' ' + pr.prezime, '') AS naziv_prodavaca,
                       po.otpremnica_id, o.broj_otpremnice,
                       po.racun_id,      r.broj_racuna,
                       po.ukupno_bez_pdv, po.ukupno_pdv, po.ukupno_sa_pdv,
                       po.status, po.napomena, po.uvjeti_placanja, po.rok_isporuke,
                       po.datum_kreiranja
                FROM ponude po
                INNER JOIN partneri   p  ON p.id  = po.kupac_id
                LEFT  JOIN prodavaci  pr ON pr.id = po.prodavac_id
                LEFT  JOIN otpremnice o  ON o.id  = po.otpremnica_id
                LEFT  JOIN racuni     r  ON r.id  = po.racun_id
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(pretraga))
                sql += " AND (po.broj_ponude LIKE @pretraga OR p.naziv LIKE @pretraga)";
            if (!string.IsNullOrWhiteSpace(status))
                sql += " AND po.status = @status";
            if (datumOd.HasValue)
                sql += " AND po.datum_ponude >= @datumOd";
            if (datumDo.HasValue)
                sql += " AND po.datum_ponude <= @datumDo";

            sql += " ORDER BY po.datum_ponude DESC, po.id DESC";

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
                        lista.Add(MapPonuda(rdr));
            }
            return lista;
        }

        public static Ponuda GetPonudaById(int id)
        {
            Ponuda ponuda = null;
            string sql = @"
                SELECT po.id, po.broj_ponude,
                       po.datum_ponude, po.datum_vazenja, po.datum_slanja, po.datum_odgovora,
                       po.kupac_id,   p.naziv  AS naziv_kupca,
                       p.adresa AS adresa_kupca, p.oib AS oib_kupca, p.grad AS grad_kupca,
                       po.prodavac_id, ISNULL(pr.ime + ' ' + pr.prezime, '') AS naziv_prodavaca,
                       po.otpremnica_id, o.broj_otpremnice,
                       po.racun_id,      r.broj_racuna,
                       po.ukupno_bez_pdv, po.ukupno_pdv, po.ukupno_sa_pdv,
                       po.status, po.napomena, po.uvjeti_placanja, po.rok_isporuke,
                       po.datum_kreiranja
                FROM ponude po
                INNER JOIN partneri   p  ON p.id  = po.kupac_id
                LEFT  JOIN prodavaci  pr ON pr.id = po.prodavac_id
                LEFT  JOIN otpremnice o  ON o.id  = po.otpremnica_id
                LEFT  JOIN racuni     r  ON r.id  = po.racun_id
                WHERE po.id = @id";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    if (rdr.Read()) ponuda = MapPonuda(rdr);
            }
            if (ponuda != null)
                ponuda.Stavke = GetStavkeZaPonudu(ponuda.Id);
            return ponuda;
        }

        // ══════════════════════════════════════════════════════════════════
        //  STAVKE
        // ══════════════════════════════════════════════════════════════════

        public static List<PonudaStavka> GetStavkeZaPonudu(int ponudaId)
        {
            var lista = new List<PonudaStavka>();
            string sql = @"
                SELECT ps.id, ps.ponuda_id, ps.artikl_id,
                       a.sifra AS sifra_artikla, a.naziv AS naziv_artikla,
                       ISNULL(jm.kratica, '') AS naziv_jm,
                       ps.rbr, ps.kolicina, ps.cijena_bez_pdv, ps.popust, ps.pdv_stopa,
                       ps.iznos_bez_pdv, ps.iznos_pdv, ps.iznos_sa_pdv, ps.napomena
                FROM ponuda_stavke ps
                INNER JOIN artikli        a  ON a.id  = ps.artikl_id
                LEFT  JOIN jedinice_mjere jm ON jm.id = a.jedinica_mjere_id
                WHERE ps.ponuda_id = @id
                ORDER BY ps.rbr";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", ponudaId);
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

        public static int DodajPonudu(Ponuda p)
        {
            string sql = @"
                INSERT INTO ponude
                    (broj_ponude, datum_ponude, datum_vazenja, kupac_id, prodavac_id,
                     ukupno_bez_pdv, ukupno_pdv, ukupno_sa_pdv,
                     status, napomena, uvjeti_placanja, rok_isporuke)
                VALUES
                    (@broj, @datum, @vazenje, @kupac, @prodavac,
                     @bezPdv, @pdv, @saPdv,
                     @status, @napomena, @uvjeti, @rok);
                SELECT SCOPE_IDENTITY();";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                int newId;
                using (var cmd = new SqlCommand(sql, conn))
                {
                    DodajParametre(cmd, p);
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                foreach (var s in p.Stavke)
                {
                    s.PonudaId = newId;
                    DodajStavku(conn, s);
                }
                return newId;
            }
        }

        private static void DodajStavku(SqlConnection conn, PonudaStavka s)
        {
            string sql = @"
                INSERT INTO ponuda_stavke
                    (ponuda_id, artikl_id, rbr, kolicina,
                     cijena_bez_pdv, popust, pdv_stopa,
                     iznos_bez_pdv, iznos_pdv, iznos_sa_pdv, napomena)
                VALUES
                    (@pId, @artiklId, @rbr, @kolicina,
                     @cijena, @popust, @pdvStopa,
                     @bezPdv, @pdv, @saPdv, @napomena)";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@pId", s.PonudaId);
                cmd.Parameters.AddWithValue("@artiklId", s.ArtiklId);
                cmd.Parameters.AddWithValue("@rbr", s.Rbr);
                cmd.Parameters.AddWithValue("@kolicina", s.Kolicina);
                cmd.Parameters.AddWithValue("@cijena", s.CijenaBezPdv);
                cmd.Parameters.AddWithValue("@popust", s.Popust);
                cmd.Parameters.AddWithValue("@pdvStopa", s.PdvStopa);
                cmd.Parameters.AddWithValue("@bezPdv", s.IznosBezPdv);
                cmd.Parameters.AddWithValue("@pdv", s.IznosPdv);
                cmd.Parameters.AddWithValue("@saPdv", s.IznosSaPdv);
                cmd.Parameters.AddWithValue("@napomena", string.IsNullOrEmpty(s.Napomena)
                                                            ? (object)DBNull.Value : s.Napomena);
                cmd.ExecuteNonQuery();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════════

        public static void AzurirajPonudu(Ponuda p)
        {
            string sqlUpdate = @"
                UPDATE ponude SET
                    broj_ponude      = @broj,
                    datum_ponude     = @datum,
                    datum_vazenja    = @vazenje,
                    kupac_id         = @kupac,
                    prodavac_id      = @prodavac,
                    ukupno_bez_pdv   = @bezPdv,
                    ukupno_pdv       = @pdv,
                    ukupno_sa_pdv    = @saPdv,
                    status           = @status,
                    napomena         = @napomena,
                    uvjeti_placanja  = @uvjeti,
                    rok_isporuke     = @rok
                WHERE id = @id AND status NOT IN ('PRETVORENA')";

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sqlUpdate, conn))
                {
                    DodajParametre(cmd, p);
                    cmd.Parameters.AddWithValue("@id", p.Id);
                    int affected = cmd.ExecuteNonQuery();
                    if (affected == 0)
                        throw new Exception("Ponuda je već pretvorena u dokument i ne može se mijenjati.");
                }
                using (var del = new SqlCommand(
                    "DELETE FROM ponuda_stavke WHERE ponuda_id = @id", conn))
                {
                    del.Parameters.AddWithValue("@id", p.Id);
                    del.ExecuteNonQuery();
                }
                foreach (var s in p.Stavke)
                {
                    s.PonudaId = p.Id;
                    DodajStavku(conn, s);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  AKCIJE — STATUS
        // ══════════════════════════════════════════════════════════════════

        public static void OznaciPoslano(int ponudaId)
        {
            string sql = @"UPDATE ponude
                           SET status = 'POSLANA', datum_slanja = GETDATE()
                           WHERE id = @id AND status = 'KREIRANA'";
            IzvrsiStatusUpdate(sql, ponudaId, "Ponuda nije u statusu KREIRANA.");
        }

        public static void OznaciPrihvaceno(int ponudaId)
        {
            string sql = @"UPDATE ponude
                           SET status = 'PRIHVAĆENA', datum_odgovora = GETDATE()
                           WHERE id = @id AND status IN ('KREIRANA','POSLANA')";
            IzvrsiStatusUpdate(sql, ponudaId, "Ponuda ne može biti prihvaćena iz trenutnog statusa.");
        }

        public static void OznaciOdbijeno(int ponudaId)
        {
            string sql = @"UPDATE ponude
                           SET status = 'ODBIJENA', datum_odgovora = GETDATE()
                           WHERE id = @id AND status IN ('KREIRANA','POSLANA')";
            IzvrsiStatusUpdate(sql, ponudaId, "Ponuda ne može biti odbijena iz trenutnog statusa.");
        }

        private static void IzvrsiStatusUpdate(string sql, int id, string greska)
        {
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0)
                    throw new Exception(greska);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  PRETVORBA — u Otpremnicu ili Račun
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Pretvara ponudu u otpremnicu. Vraća ID nove otpremnice.
        /// </summary>
        public static int PretvoriUOtpremnicu(int ponudaId)
        {
            var ponuda = GetPonudaById(ponudaId);
            if (ponuda == null)
                throw new Exception("Ponuda nije pronađena.");
            if (ponuda.JePretvorena)
                throw new Exception("Ponuda je već pretvorena u dokument.");

            var stavke = new System.Collections.Generic.List<OtpremnicaStavka>();
            foreach (var s in ponuda.Stavke)
                stavke.Add(s.ToOtpremnicaStavka());

            var otpremnica = new Otpremnica
            {
                BrojOtpremnice = OtpremnicaRepository.GeneriraBrojOtpremnice(),
                DatumOtpremnice = DateTime.Today,
                DatumIsporuke = DateTime.Today.AddDays(7),
                KupacId = ponuda.KupacId,
                NazivKupca = ponuda.NazivKupca,
                AdresaKupca = ponuda.AdresaKupca,
                OibKupca = ponuda.OibKupca,
                ProdavacId = ponuda.ProdavacId,
                Napomena = $"Kreirano iz ponude {ponuda.BrojPonude}",
                Status = "KREIRANA",
                Stavke = stavke
            };
            otpremnica.UkupnoVrijednost = otpremnica.IzracunajUkupno();

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        int otpId = OtpremnicaRepository.DodajOtpremnicuUTransakciji(conn, tx, otpremnica);

                        using (var upd = new SqlCommand(@"
                            UPDATE ponude SET
                                status        = 'PRETVORENA',
                                otpremnica_id = @oid
                            WHERE id = @id", conn, tx))
                        {
                            upd.Parameters.AddWithValue("@oid", otpId);
                            upd.Parameters.AddWithValue("@id", ponudaId);
                            upd.ExecuteNonQuery();
                        }
                        tx.Commit();
                        return otpId;
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        /// <summary>
        /// Pretvara ponudu direktno u račun (bez otpremnice). Vraća ID novog računa.
        /// </summary>
        public static int PretvoriURacun(int ponudaId, DateTime? datumValute = null)
        {
            var ponuda = GetPonudaById(ponudaId);
            if (ponuda == null)
                throw new Exception("Ponuda nije pronađena.");
            if (ponuda.JePretvorena)
                throw new Exception("Ponuda je već pretvorena u dokument.");

            var stavkeRacuna = new System.Collections.Generic.List<RacunStavka>();
            foreach (var s in ponuda.Stavke)
                stavkeRacuna.Add(s.ToRacunStavka());

            var racun = new Racun
            {
                BrojRacuna = RacuniRepository.GeneriraBrojRacuna(),
                DatumRacuna = DateTime.Today,
                DatumValute = datumValute ?? DateTime.Today.AddDays(30),
                KupacId = ponuda.KupacId,
                NazivKupca = ponuda.NazivKupca,
                AdresaKupca = ponuda.AdresaKupca,
                OibKupca = ponuda.OibKupca,
                PdvIdKupca = "HR" + ponuda.OibKupca,
                ProdavacId = ponuda.ProdavacId,
                UkupnoBezPdv = stavkeRacuna.Sum(s => s.IznosBezPdv),
                UkupnoPdv = stavkeRacuna.Sum(s => s.IznosPdv),
                UkupnoSaPdv = stavkeRacuna.Sum(s => s.IznosSaPdv),
                Placeno = false,
                Proknjizeno = false,
                Status = "KREIRAN",
                Napomena = $"Kreirano iz ponude {ponuda.BrojPonude}",
                Stavke = stavkeRacuna
            };

            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        int racunId = RacuniRepository.DodajRacunUTransakciji(conn, tx, racun);

                        using (var upd = new SqlCommand(@"
                            UPDATE ponude SET
                                status   = 'PRETVORENA',
                                racun_id = @rid
                            WHERE id = @id", conn, tx))
                        {
                            upd.Parameters.AddWithValue("@rid", racunId);
                            upd.Parameters.AddWithValue("@id", ponudaId);
                            upd.ExecuteNonQuery();
                        }
                        tx.Commit();
                        return racunId;
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public static void ObrisiPonudu(int id)
        {
            string sql = "DELETE FROM ponude WHERE id = @id AND status NOT IN ('PRETVORENA')";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                if (cmd.ExecuteNonQuery() == 0)
                    throw new Exception("Ponuda je pretvorena u dokument i ne može se obrisati.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  ŠIFARNICI
        // ══════════════════════════════════════════════════════════════════

        public static string GeneriraBrojPonude()
        {
            string sql = "SELECT COUNT(*) FROM ponude WHERE YEAR(datum_ponude) = @god";
            using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@god", DateTime.Now.Year);
                conn.Open();
                int count = (int)cmd.ExecuteScalar() + 1;
                return $"PON-{DateTime.Now.Year}-{count:D4}";
            }
        }

        public static bool BrojPonudePostoji(string broj, int excludeId = 0)
        {
            string sql = "SELECT COUNT(*) FROM ponude WHERE broj_ponude = @broj AND id <> @id";
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

        private static Ponuda MapPonuda(IDataReader rdr)
        {
            return new Ponuda
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                BrojPonude = rdr["broj_ponude"].ToString(),
                DatumPonude = rdr.GetDateTime(rdr.GetOrdinal("datum_ponude")),
                DatumVazenja = rdr["datum_vazenja"] == DBNull.Value ? (DateTime?)null
                                       : rdr.GetDateTime(rdr.GetOrdinal("datum_vazenja")),
                DatumSlanja = rdr["datum_slanja"] == DBNull.Value ? (DateTime?)null
                                       : rdr.GetDateTime(rdr.GetOrdinal("datum_slanja")),
                DatumOdgovora = rdr["datum_odgovora"] == DBNull.Value ? (DateTime?)null
                                       : rdr.GetDateTime(rdr.GetOrdinal("datum_odgovora")),
                DatumKreiranja = rdr.GetDateTime(rdr.GetOrdinal("datum_kreiranja")),
                KupacId = rdr.GetInt32(rdr.GetOrdinal("kupac_id")),
                NazivKupca = rdr["naziv_kupca"].ToString(),
                AdresaKupca = rdr["adresa_kupca"].ToString(),
                OibKupca = rdr["oib_kupca"] == DBNull.Value ? null : rdr["oib_kupca"].ToString(),
                GradKupca = rdr["grad_kupca"].ToString(),
                ProdavacId = rdr["prodavac_id"] == DBNull.Value ? (int?)null
                                       : rdr.GetInt32(rdr.GetOrdinal("prodavac_id")),
                NazivProdavaca = rdr["naziv_prodavaca"].ToString(),
                OtpremnicaId = rdr["otpremnica_id"] == DBNull.Value ? (int?)null
                                       : rdr.GetInt32(rdr.GetOrdinal("otpremnica_id")),
                BrojOtpremnice = rdr["broj_otpremnice"] == DBNull.Value ? null
                                       : rdr["broj_otpremnice"].ToString(),
                RacunId = rdr["racun_id"] == DBNull.Value ? (int?)null
                                       : rdr.GetInt32(rdr.GetOrdinal("racun_id")),
                BrojRacuna = rdr["broj_racuna"] == DBNull.Value ? null
                                       : rdr["broj_racuna"].ToString(),
                UkupnoBezPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_bez_pdv")),
                UkupnoPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_pdv")),
                UkupnoSaPdv = rdr.GetDecimal(rdr.GetOrdinal("ukupno_sa_pdv")),
                Status = rdr["status"].ToString(),
                Napomena = rdr["napomena"] == DBNull.Value ? null : rdr["napomena"].ToString(),
                UvjetiPlacanja = rdr["uvjeti_placanja"] == DBNull.Value ? null : rdr["uvjeti_placanja"].ToString(),
                RokIsporuke = rdr["rok_isporuke"] == DBNull.Value ? null : rdr["rok_isporuke"].ToString()
            };
        }

        private static PonudaStavka MapStavka(IDataReader rdr)
        {
            return new PonudaStavka
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                PonudaId = rdr.GetInt32(rdr.GetOrdinal("ponuda_id")),
                ArtiklId = rdr.GetInt32(rdr.GetOrdinal("artikl_id")),
                SifraArtikla = rdr["sifra_artikla"].ToString(),
                NazivArtikla = rdr["naziv_artikla"].ToString(),
                NazivJediniceMjere = rdr["naziv_jm"].ToString(),
                Rbr = rdr.GetInt32(rdr.GetOrdinal("rbr")),
                Kolicina = rdr.GetDecimal(rdr.GetOrdinal("kolicina")),
                CijenaBezPdv = rdr.GetDecimal(rdr.GetOrdinal("cijena_bez_pdv")),
                Popust = rdr.GetDecimal(rdr.GetOrdinal("popust")),
                PdvStopa = rdr.GetDecimal(rdr.GetOrdinal("pdv_stopa")),
                IznosBezPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_bez_pdv")),
                IznosPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_pdv")),
                IznosSaPdv = rdr.GetDecimal(rdr.GetOrdinal("iznos_sa_pdv")),
                Napomena = rdr["napomena"] == DBNull.Value ? null : rdr["napomena"].ToString()
            };
        }

        private static void DodajParametre(SqlCommand cmd, Ponuda p)
        {
            cmd.Parameters.AddWithValue("@broj", p.BrojPonude);
            cmd.Parameters.AddWithValue("@datum", p.DatumPonude);
            cmd.Parameters.AddWithValue("@vazenje", p.DatumVazenja.HasValue
                                                        ? (object)p.DatumVazenja.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@kupac", p.KupacId);
            cmd.Parameters.AddWithValue("@prodavac", p.ProdavacId.HasValue
                                                        ? (object)p.ProdavacId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@bezPdv", p.UkupnoBezPdv);
            cmd.Parameters.AddWithValue("@pdv", p.UkupnoPdv);
            cmd.Parameters.AddWithValue("@saPdv", p.UkupnoSaPdv);
            cmd.Parameters.AddWithValue("@status", p.Status ?? "KREIRANA");
            cmd.Parameters.AddWithValue("@napomena", string.IsNullOrEmpty(p.Napomena)
                                                        ? (object)DBNull.Value : p.Napomena);
            cmd.Parameters.AddWithValue("@uvjeti", string.IsNullOrEmpty(p.UvjetiPlacanja)
                                                        ? (object)DBNull.Value : p.UvjetiPlacanja);
            cmd.Parameters.AddWithValue("@rok", string.IsNullOrEmpty(p.RokIsporuke)
                                                        ? (object)DBNull.Value : p.RokIsporuke);
        }
    }
}