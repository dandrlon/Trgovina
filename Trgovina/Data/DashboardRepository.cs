using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trgovina.Data.Models;

namespace Trgovina.Data
{
    public class DashboardRepository
    {
        private readonly string _connStr = Data.DatabaseHelper.ConnectionString;
        public DashboardRepository(string connStr) => _connStr = connStr;

        public DashboardStats DohvatiStats()
        {
            var stats = new DashboardStats();
            var conn = new SqlConnection(_connStr);
            conn.Open();

            // Prodaja ovaj mjesec
            using (var cmd = new SqlCommand(@"
            SELECT 
                ISNULL(SUM(r.ukupno_sa_pdv), 0)
            FROM racuni r
            WHERE MONTH(r.datum_racuna) = MONTH(GETDATE())
              AND YEAR(r.datum_racuna)  = YEAR(GETDATE())", conn))
            {
                stats.UkupnaProdajaMjesec = (decimal)cmd.ExecuteScalar();
            }

            // Računi danas
            using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM racuni
            WHERE CAST(datum_racuna AS DATE) = CAST(GETDATE() AS DATE)", conn))
            {
                stats.BrojRacunaDanas = (int)cmd.ExecuteScalar();
            }

            // Vrijednost robe na stanju
            using (var cmd = new SqlCommand(@"
            SELECT ISNULL(SUM(a.kolicina * a.nabavna_cijena), 0)
            FROM artikli a
            WHERE a.aktivan = 1", conn))
            {
                stats.VrijednostRobe = (decimal)cmd.ExecuteScalar();
            }

            // Aktivni partneri
            using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM partneri WHERE aktivan = 1", conn))
            {
                stats.AktivniPartneri = (int)cmd.ExecuteScalar();
            }

            return stats;
        }

        public Dictionary<DateTime, decimal> DohvatiProdajuPo30Dana()
        {
            var dict = new Dictionary<DateTime, decimal>();
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                SELECT 
                    CAST(datum_racuna AS DATE) AS dan,
                    ISNULL(SUM(ukupno_sa_pdv), 0) AS promet
                FROM racuni
                WHERE datum_racuna >= DATEADD(DAY, -29, CAST(GETDATE() AS DATE))
                GROUP BY CAST(datum_racuna AS DATE)
                ORDER BY dan ASC", conn))
                {
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                            dict[(DateTime)rdr[0]] = (decimal)rdr[1];
                }
            }
            return dict;
        }

        public List<NedavniRacunRow> DohvatiNedavneRacune(int top = 10)
        {
            var lista = new List<NedavniRacunRow>();
            var conn = new SqlConnection(_connStr);
            conn.Open();

            var cmd = new SqlCommand($@"
            SELECT TOP {top}
                r.broj_racuna,
                p.naziv,
                r.ukupno_sa_pdv,
                r.status
            FROM racuni r
            JOIN partneri p ON p.id = r.kupac_id
            ORDER BY r.datum_racuna DESC", conn);

            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new NedavniRacunRow
                {
                    BrojRacuna = reader.GetString(0),
                    Partner = reader.GetString(1),
                    Iznos = reader.GetDecimal(2),
                    Status = reader.GetString(3)
                });
            }
            return lista;
        }
    }
}
