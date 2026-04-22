using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Trgovina.Data;
using Trgovina.Data.Models;

namespace Trgovina.Utils
{
    /// <summary>
    /// Fiskalizacija 1.0 (B2C) — komunikacija s CIS sustavom Porezne uprave.
    /// Implementira ZKI izračun i SOAP slanje sukladno Tehničkoj specifikaciji PU v2.0.
    ///
    /// PRED PRODUKCIJOM:
    ///   1. Nabaviti FINA aplikativni certifikat (fiskal.pfx)
    ///   2. Podesiti postavke u frmPostavkeFiskalizacije
    ///   3. Promijeniti FiskalTestOkolina → false
    /// </summary>
    public static class FiskalizacijaService
    {
        // ── CIS endpointi ──────────────────────────────────────────────────────────
        private const string URL_PRODUKCIJA = "https://cis.porezna-uprava.hr:8449/FiskalizacijaService";
        private const string URL_TEST = "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest";
        private const string NS = "http://www.apis-it.hr/fin/2012/types/f73";
        private const string DATE_FORMAT = "dd.MM.yyyyTHH:mm:ss";

        // ═══════════════════════════════════════════════════════════════════════════
        //  JAVNA API
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Fiskalizira račun: izračunava ZKI, šalje na CIS, vraća JIR.
        /// </summary>
        public static async Task<FiskalizacijaRezultat> FiskalizirajAsync(Racun racun)
        {
            try
            {
                var postavke = UcitajPostavke();

                if (string.IsNullOrWhiteSpace(postavke.CertPutanja))
                    return Greska("Nije konfiguriran put do FINA certifikata. Provjerite Postavke → Fiskalizacija.");

                if (!File.Exists(postavke.CertPutanja))
                    return Greska($"FINA certifikat nije pronađen: {postavke.CertPutanja}");

                // 1. Učitaj certifikat
                X509Certificate2 cert;
                try
                {
                    cert = new X509Certificate2(postavke.CertPutanja, postavke.CertLozinka,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet |
                        X509KeyStorageFlags.Exportable);
                }
                catch (Exception ex)
                {
                    return Greska($"Greška pri učitavanju certifikata: {ex.Message}");
                }

                // 2. Pripremi podatke računa
                string datumVrijeme = DateTime.Now.ToString(DATE_FORMAT);
                string brOznRac = IzvuciBrojRacuna(racun.BrojRacuna);
                string oznPosPr = postavke.OznakaPoslovnice;
                string oznNapUr = postavke.OznakaNaplatnog;
                string oib = postavke.OibTvrtke;
                string oibOper = string.IsNullOrWhiteSpace(postavke.OibOperatera)
                    ? postavke.OibTvrtke
                    : postavke.OibOperatera;
                string iznos = racun.UkupnoSaPdv.ToString("N2", CultureInfo.InvariantCulture);

                // 3. Izračunaj ZKI
                string zki = IzracunajZki(oib, datumVrijeme, brOznRac, oznPosPr, oznNapUr, iznos, cert);

                // 4. Generiraj UUID za poruku
                string uuidPoruke = Guid.NewGuid().ToString();

                // 5. Sagradi XML zahtjev
                XmlDocument xmlDoc = SagradiRacunZahtjev(
                    uuidPoruke, datumVrijeme, oib, oibOper,
                    datumVrijeme, brOznRac, oznPosPr, oznNapUr,
                    iznos, racun, zki, postavke.USustavuPDV);

                // 6. Potpiši XML
                PotpisiXml(xmlDoc, cert);

                // 7. Pošalji SOAP zahtjev
                string endpoint = postavke.TestOkolina ? URL_TEST : URL_PRODUKCIJA;
                string soapOdgovor = await PosaljiSoapAsync(xmlDoc, endpoint, cert);

                // 8. Izvuci JIR iz odgovora
                string jir = IzvuciJir(soapOdgovor);
                if (string.IsNullOrWhiteSpace(jir))
                {
                    string cisGreska = IzvuciCisGresku(soapOdgovor);
                    return Greska($"CIS nije vratio JIR. {cisGreska}");
                }

                return new FiskalizacijaRezultat
                {
                    Uspjeh = true,
                    JIR = jir,
                    ZKI = zki,
                    Poruka = $"Račun uspješno fiskaliziran.\nJIR: {jir}"
                };
            }
            catch (Exception ex)
            {
                return Greska($"Neočekivana greška fiskalizacije: {ex.Message}");
            }
        }

        /// <summary>
        /// Samo izračunava ZKI bez slanja (za offline/fallback slučaj).
        /// </summary>
        public static string IzracunajZkiSamo(Racun racun)
        {
            try
            {
                var postavke = UcitajPostavke();
                if (!File.Exists(postavke.CertPutanja)) return null;

                var cert = new X509Certificate2(postavke.CertPutanja, postavke.CertLozinka,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

                string datumVrijeme = DateTime.Now.ToString(DATE_FORMAT);
                string brOznRac = IzvuciBrojRacuna(racun.BrojRacuna);
                string iznos = racun.UkupnoSaPdv.ToString("N2", CultureInfo.InvariantCulture);

                return IzracunajZki(postavke.OibTvrtke, datumVrijeme, brOznRac,
                    postavke.OznakaPoslovnice, postavke.OznakaNaplatnog, iznos, cert);
            }
            catch { return null; }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ZKI ALGORITAM
        //  Sukladno poglavlju 12 Tehničke specifikacije PU
        //  ZKI = MD5(RSA_SHA1(OIB + datum + brRac + oznPosPr + oznNapUr + iznos))
        // ═══════════════════════════════════════════════════════════════════════════

        private static string IzracunajZki(string oib, string datumVrijeme, string brOznRac,
            string oznPosPr, string oznNapUr, string iznos, X509Certificate2 cert)
        {
            // Ulazni string za potpis
            string ulaz = $"{oib}{datumVrijeme}{brOznRac}{oznPosPr}{oznNapUr}{iznos}";

            // RSA potpis s privatnim ključem certifikata
            using (RSA rsa = cert.GetRSAPrivateKey())
            {
                byte[] podaci = Encoding.ASCII.GetBytes(ulaz);
                byte[] potpis = rsa.SignData(podaci, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

                // MD5 hash potpisa → ZKI (hex lowercase, 32 znaka)
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(potpis);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  XML GRADNJA
        // ═══════════════════════════════════════════════════════════════════════════

        private static XmlDocument SagradiRacunZahtjev(
            string uuidPoruke, string datumVrijemePoruke,
            string oib, string oibOper,
            string datumVrijemeRacuna, string brOznRac, string oznPosPr, string oznNapUr,
            string iznos, Racun racun, string zki, bool uSustavuPdv)
        {
            var doc = new XmlDocument { PreserveWhitespace = false };

            // SOAP envelope
            XmlElement envelope = doc.CreateElement("soapenv", "Envelope",
                "http://schemas.xmlsoap.org/soap/envelope/");
            doc.AppendChild(envelope);

            XmlElement body = doc.CreateElement("soapenv", "Body",
                "http://schemas.xmlsoap.org/soap/envelope/");
            envelope.AppendChild(body);

            // RacunZahtjev — element koji se potpisuje
            XmlElement zahtjev = doc.CreateElement("tns", "RacunZahtjev", NS);
            zahtjev.SetAttribute("Id", "signXmlId");
            zahtjev.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            body.AppendChild(zahtjev);

            // Zaglavlje
            XmlElement zaglavlje = doc.CreateElement("tns", "Zaglavlje", NS);
            zahtjev.AppendChild(zaglavlje);
            Dodaj(doc, zaglavlje, "tns", "IdPoruke", NS, uuidPoruke);
            Dodaj(doc, zaglavlje, "tns", "DatumVrijeme", NS, datumVrijemePoruke);

            // Racun
            XmlElement racunEl = doc.CreateElement("tns", "Racun", NS);
            zahtjev.AppendChild(racunEl);

            Dodaj(doc, racunEl, "tns", "Oib", NS, oib);
            Dodaj(doc, racunEl, "tns", "USustPdv", NS, uSustavuPdv ? "true" : "false");
            Dodaj(doc, racunEl, "tns", "DatVrijeme", NS, datumVrijemeRacuna);
            Dodaj(doc, racunEl, "tns", "OznSlijed", NS, "N"); // N=naplatni uređaj

            // Broj računa (tri dijela)
            XmlElement brRac = doc.CreateElement("tns", "BrRac", NS);
            racunEl.AppendChild(brRac);
            Dodaj(doc, brRac, "tns", "BrOznRac", NS, brOznRac);
            Dodaj(doc, brRac, "tns", "OznPosPr", NS, oznPosPr);
            Dodaj(doc, brRac, "tns", "OznNapUr", NS, oznNapUr);

            // PDV stavke — grupirati po stopi
            if (uSustavuPdv && racun.Stavke != null && racun.Stavke.Count > 0)
            {
                var grupePoStopi = new Dictionary<decimal, (decimal osnovica, decimal iznos_)>();
                foreach (var s in racun.Stavke)
                {
                    if (!grupePoStopi.ContainsKey(s.PdvStopa))
                        grupePoStopi[s.PdvStopa] = (0m, 0m);
                    var g = grupePoStopi[s.PdvStopa];
                    grupePoStopi[s.PdvStopa] = (g.osnovica + s.IznosBezPdv, g.iznos_ + s.IznosPdv);
                }

                foreach (var kvp in grupePoStopi)
                {
                    XmlElement pdv = doc.CreateElement("tns", "Pdv", NS);
                    racunEl.AppendChild(pdv);
                    XmlElement porez = doc.CreateElement("tns", "Porez", NS);
                    pdv.AppendChild(porez);
                    Dodaj(doc, porez, "tns", "Stopa", NS,
                        kvp.Key.ToString("N2", CultureInfo.InvariantCulture));
                    Dodaj(doc, porez, "tns", "Osnovica", NS,
                        kvp.Value.osnovica.ToString("N2", CultureInfo.InvariantCulture));
                    Dodaj(doc, porez, "tns", "Iznos", NS,
                        kvp.Value.iznos_.ToString("N2", CultureInfo.InvariantCulture));
                }
            }

            Dodaj(doc, racunEl, "tns", "IznosUkupno", NS, iznos);
            Dodaj(doc, racunEl, "tns", "NacinPlac", NS, MapNacinPlacanja(racun.NacinPlacanja));
            Dodaj(doc, racunEl, "tns", "OibOper", NS, oibOper);
            Dodaj(doc, racunEl, "tns", "ZastKod", NS, zki);
            Dodaj(doc, racunEl, "tns", "NakDost", NS, "false");

            return doc;
        }

        private static void PotpisiXml(XmlDocument doc, X509Certificate2 cert)
        {
            XmlElement elementZaPotpis = doc.GetElementsByTagName("tns:RacunZahtjev")[0] as XmlElement;
            if (elementZaPotpis == null) return;

            // Koristi custom SignedXml koji zna pronaći element po Id
            var signed = new FiskalSignedXml(doc)
            {
                SigningKey = cert.GetRSAPrivateKey()
            };
            signed.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            signed.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

            var reference = new Reference("#signXmlId");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigExcC14NTransform());
            signed.AddReference(reference);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signed.KeyInfo = keyInfo;

            signed.ComputeSignature();
            elementZaPotpis.AppendChild(doc.ImportNode(signed.GetXml(), true));
        }

        private class FiskalSignedXml : SignedXml
        {
            public FiskalSignedXml(XmlDocument doc) : base(doc) { }

            public override XmlElement GetIdElement(XmlDocument doc, string id)
            {
                // Standardna pretraga
                XmlElement el = base.GetIdElement(doc, id);
                if (el != null) return el;

                // Traži po Id atributu (bez namespace-a) — CIS koristi ovo
                XmlNodeList nodes = doc.GetElementsByTagName("*");
                foreach (XmlElement node in nodes)
                {
                    if (node.GetAttribute("Id") == id)
                        return node;
                }
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  SOAP KOMUNIKACIJA
        // ═══════════════════════════════════════════════════════════════════════════

        private static async Task<string> PosaljiSoapAsync(XmlDocument xmlDoc, string endpoint,
            X509Certificate2 cert)
        {
            // SOAP omotač
            string xmlSadrzaj = xmlDoc.OuterXml;
            string soapBody = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
              <soapenv:Body>
                {xmlDoc.DocumentElement?.InnerXml ?? xmlSadrzaj}
              </soapenv:Body>
            </soapenv:Envelope>";

            // Ako smo već izgradili kompletni envelope u SagradiRacunZahtjev, koristi direktno
            string payload = xmlDoc.OuterXml.Contains("Envelope")
                ? xmlDoc.OuterXml
                : soapBody;

            byte[] podaci = Encoding.UTF8.GetBytes(payload);

            var request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=UTF-8";
            request.Headers.Add("SOAPAction",
                "http://e-porezna.porezna-uprava.hr/fiskalizacija/2012/services/FiskalizacijaService/racuni");
            request.ContentLength = podaci.Length;
            request.Timeout = 10_000; // 10s

            // Mutual TLS — dodaj klijentski certifikat
            request.ClientCertificates.Add(cert);

            // Trust CIS server certifikata u test okolini (samo za razvoj!)
            ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, e) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var stream = await request.GetRequestStreamAsync())
                await stream.WriteAsync(podaci, 0, podaci.Length);

            try
            {
                using (var response = await request.GetResponseAsync())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    return await reader.ReadToEndAsync();
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var reader = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                        return await reader.ReadToEndAsync();
                }
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  PARSIRANJE ODGOVORA
        // ═══════════════════════════════════════════════════════════════════════════

        private static string IzvuciJir(string soapOdgovor)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(soapOdgovor);
                var mgr = new XmlNamespaceManager(doc.NameTable);
                mgr.AddNamespace("tns", NS);

                var node = doc.SelectSingleNode("//tns:Jir", mgr);
                return node?.InnerText?.Trim();
            }
            catch { return null; }
        }

        private static string IzvuciCisGresku(string soapOdgovor)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(soapOdgovor);
                var mgr = new XmlNamespaceManager(doc.NameTable);
                mgr.AddNamespace("tns", NS);

                var sifra = doc.SelectSingleNode("//tns:SifraGreske", mgr)?.InnerText ?? "";
                var poruka = doc.SelectSingleNode("//tns:PorukaGreske", mgr)?.InnerText ?? "";
                if (!string.IsNullOrEmpty(sifra))
                    return $"CIS greška [{sifra}]: {poruka}";

                // SOAP Fault
                var fault = doc.SelectSingleNode("//faultstring")?.InnerText;
                if (!string.IsNullOrEmpty(fault)) return $"SOAP Fault: {fault}";

                return "";
            }
            catch { return ""; }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  HELPERI
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Iz broja računa "1/1/1" ili "2024-001" izvlači prvi dio (sekvencijalni broj).
        /// PU zahtijeva samo numerički dio bez separatora.
        /// </summary>
        private static string IzvuciBrojRacuna(string brojRacuna)
        {
            if (string.IsNullOrEmpty(brojRacuna)) return "1";
            // Uzmi sve do prvog "/" ili "-" ili cijeli string ako nema separatora
            var parts = brojRacuna.Split('/', '-');
            return parts[2].Trim();
        }

        /// <summary>
        /// Mapira interni kod načina plaćanja na CIS kod.
        /// G=Gotovina, K=Kartica, T=Transakcijski račun, O=Ostalo
        /// </summary>
        private static string MapNacinPlacanja(string nacinPlacanja)
        {
            switch (nacinPlacanja?.ToUpper())
            {
                case "G": return "G"; // Gotovina
                case "K": return "K"; // Kartica
                case "T": return "T"; // Transakcijski račun
                case "C": return "C"; // Ček
                default: return "O";  // Ostalo
            }
        }

        private static void Dodaj(XmlDocument doc, XmlElement parent, string prefix,
            string localName, string ns, string value)
        {
            XmlElement el = doc.CreateElement(prefix, localName, ns);
            el.InnerText = value ?? "";
            parent.AppendChild(el);
        }

        private static FiskalizacijaRezultat Greska(string poruka) =>
            new FiskalizacijaRezultat { Uspjeh = false, Poruka = poruka };

        private static FiskalPostavke UcitajPostavke()
        {
            var p = DatabaseHelper.GetSvePostavke();

            string Get(string k, string def = "") =>
                p.ContainsKey(k) && !string.IsNullOrEmpty(p[k]) ? p[k] : def;

            return new FiskalPostavke
            {
                OibTvrtke = Get("OIB"),
                OibOperatera = Get("FiskalOibOperatera"),
                CertPutanja = Get("FiskalCertifikatPutanja"),
                CertLozinka = Get("FiskalCertifikatLozinka"),
                OznakaPoslovnice = Get("FiskalOznakaPoslovnice", "1"),
                OznakaNaplatnog = Get("FiskalOznakaNaplatnog", "1"),
                TestOkolina = Get("FiskalTestOkolina", "true").ToLower() == "true",
                USustavuPDV = Get("USustavuPDV", "true").ToLower() == "true"
            };
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  ERACUN STUB (Fiskalizacija 2.0 — B2B)
        //  Implementirati kada odaberete middleware (Moj-eRačun, Mikroe, FINA REST)
        // ═══════════════════════════════════════════════════════════════════════════

        public static async Task<FiskalizacijaRezultat> PosaljiEracunAsync(Racun racun)
        {
            // TODO: Implementirati integraciju s odabranim eRačun middleware-om
            // Primjer za Moj-eRačun REST API:
            //
            // var p = DatabaseHelper.GetSvePostavke();
            // string apiKljuc = p.ContainsKey("EracunApiKljuc") ? p["EracunApiKljuc"] : "";
            // string apiUrl = p.ContainsKey("EracunApiUrl") ? p["EracunApiUrl"] : "";
            //
            // var ubl = EracunUblGenerator.Generiraj(racun);
            // var response = await HttpClient.PostAsync(apiUrl + "/invoices",
            //     new StringContent(ubl, Encoding.UTF8, "application/xml"),
            //     headers: { "Authorization": $"Bearer {apiKljuc}" });
            //
            // string referenca = ParseReferenca(await response.Content.ReadAsStringAsync());
            // return new FiskalizacijaRezultat { Uspjeh = true, EracunReferenca = referenca };

            await Task.Delay(1); // placeholder
            return new FiskalizacijaRezultat
            {
                Uspjeh = false,
                Poruka = "eRačun (Fiskalizacija 2.0) još nije konfiguriran.\n" +
                         "Molimo odaberite middleware (Moj-eRačun, Mikroe i sl.) i " +
                         "unesite API ključ u Postavke → Fiskalizacija → eRačun."
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  POMOĆNE KLASE
    // ═══════════════════════════════════════════════════════════════════════════

    public class FiskalizacijaRezultat
    {
        public bool Uspjeh { get; set; }
        public string JIR { get; set; }
        public string ZKI { get; set; }
        public string EracunReferenca { get; set; }
        public string Poruka { get; set; }
    }

    internal class FiskalPostavke
    {
        public string OibTvrtke { get; set; }
        public string OibOperatera { get; set; }
        public string CertPutanja { get; set; }
        public string CertLozinka { get; set; }
        public string OznakaPoslovnice { get; set; }
        public string OznakaNaplatnog { get; set; }
        public bool TestOkolina { get; set; }
        public bool USustavuPDV { get; set; }
    }
}