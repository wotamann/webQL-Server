using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace webQL
{

    public class Tools
    {

        
        public class Ganymed
        {

            // get connectionstring from ODBC Schillermed, smw.ini & varibale.ini 
            public static class Connection
            {

                private const string RegKeyGanymed = @"Software\SCHILLERMED\SMW";
                private const string RegKeyGanymedODBC = @"Software\ODBC\ODBC.INI\";

                private static Microsoft.Win32.RegistryKey getRegistry64_32Bit(Microsoft.Win32.RegistryHive key)
                {

                    // Wotan added  7.1.2016  f
                    Microsoft.Win32.RegistryKey registryKey;

                    // check 64/32 Bit System with WOW6432Node Differenzierung
                    if (Environment.Is64BitOperatingSystem == true)
                    {
                        registryKey = Microsoft.Win32.RegistryKey.OpenBaseKey(key, Microsoft.Win32.RegistryView.Registry64);
                    }
                    else
                    {
                        registryKey = Microsoft.Win32.RegistryKey.OpenBaseKey(key, Microsoft.Win32.RegistryView.Registry32);
                    }

                    return registryKey;

                }

                // verbindungsstring
                public static string ConnectionString { get; set; }

                // No Construktor

                // used public method
                public static string tryConnectionFromODBC(string connectionStringToAdapt)
                {
                    /*
                      
                     check if there is a ConnectionString like:
                     
                        "ConnectionString": "ODBC=Schillermed",   valid: 'odbc[=: ]ODBC-Name' 
                     
                     then convert this using Ganymed REGEDIT ODBC & SMW.INI & VARIABLE.INI to generate an ConnectionString like 
                      
                        "ConnectionString": "data source=runner;database=Schillermed;user id=sa;password=;",
			
                     */

                    
                    if (connectionStringToAdapt.Trim().ToLower().StartsWith("odbc"))
                    {

                        string[] separator = { "=", ":", " " };
                        var NameODBC = connectionStringToAdapt.Split(separator, StringSplitOptions.None)[1];

                        // get new modified connectionStringToCheck from ODBC und DATA in variable.ini
                        ConnectionString = getConnectionODBC(NameODBC);
                        return ConnectionString;
                    }
                    else
                    {                        
                        ConnectionString = connectionStringToAdapt; // unchanged
                        return ConnectionString;
                    }

                }

                public static bool IsConnectionValid()
                {

                    // prüft ob constr ein gültiger SQL verbindungsstring ist
                    try
                    {
                        SqlConnection myConnection = new SqlConnection(ConnectionString);
                        myConnection.Open();
                        switch (myConnection.State)
                        {

                            case ConnectionState.Broken:
                            case ConnectionState.Closed:
                                myConnection.Close();
                                return false;
                            default:
                                myConnection.Close();
                                return true;
                        }
                        return true;

                    }
                    catch
                    {
                        return false;

                    }

                }

                // ------------- get VPN und Connectionstring from ODBC in SMW.ini & variable.ini

                public static string getVPNummer()
                {
                    // Pfad zu Variable.ini
                    string smwIniPath = System.Environment.GetEnvironmentVariable("windir") + "\\SMW.INI";
                    string sec = "VARIABLE_INI";
                    string item = "PATH";
                    // hole VPN aus Stempel
                    string variableIniPath = ReadIniFile(smwIniPath, sec, item) + "Variable.ini";
                    string sec1 = "STEMPEL";
                    string item1 = "VPN";

                    return ReadIniFile(variableIniPath, sec1, item1);
                }

                public static string getConnectionODBC(string NameODBC = "Schillermed")
                {

                    string database = ReadRegistryODBC(ReadRegistryODBCKeys.Database, NameODBC);
                    string lastUser = ReadRegistryODBC(ReadRegistryODBCKeys.LastUser, NameODBC);
                    string server = ReadRegistryODBC(ReadRegistryODBCKeys.Server, NameODBC);
                    string trustedConnection = ReadRegistryODBC(ReadRegistryODBCKeys.Trusted_Connection, NameODBC).ToLower();

                    // Pfad zu Variable.ini
                    string smwIniPath = System.Environment.GetEnvironmentVariable("windir") + "\\SMW.INI";
                    string sec = "VARIABLE_INI";
                    string item = "PATH";
                    // hole pwd aus Section ODBC in variable.ini
                    string variableIniPath = ReadIniFile(smwIniPath, sec, item) + "Variable.ini";
                    string sec1 = "ODBC";
                    string item1 = "pwd";
                    // in Section ODBC pwd="passwort"
                    string password = ReadIniFile(variableIniPath, sec1, item1);

                    // windows authenticaiotn
                    if (trustedConnection == "yes")
                        return "Server=" + server + ";database=" + database + ";Trusted_Connection=Yes;";
                    // alternativ "Data Source=server; Initial Catalog=database; Integrated Security=True"
                    else
                        return  "data source=" + server + ";database=" + database + ";user id=" + lastUser + ";password=" + password;
                    //"data source=server; database=database; user id=user1; password=pwd1"

                }

                private static string ReadIniFile(string Fullpath, string Section, string Item)
                {

                    string st = "";
                    string[] str = null;
                    bool sec = false;

                    try
                    {
                        str = System.IO.File.ReadAllLines(Fullpath, System.Text.Encoding.Default);
                        Section = "[" + Section + "]";


                        foreach (string s in str)
                        {
                            if (sec == true)
                            {
                                int i = s.IndexOf(Item + "=", StringComparison.CurrentCultureIgnoreCase);
                                if (i == 0)
                                {
                                    st = s.Substring(Item.Length + 1);
                                }
                            }
                            //
                            if (s.IndexOf("[", StringComparison.CurrentCultureIgnoreCase) > -1)
                            {
                                sec = false;
                            }
                            if (s.IndexOf(Section, StringComparison.CurrentCultureIgnoreCase) > -1)
                            {
                                sec = true;
                            }
                        }


                    }
                    catch // (Exception ex)
                    {
                        //  MsgBox(ex.Message)
                        st = "";
                    }

                    return st;

                }


                // -----------REGISTRY METHODS -------------------------------------------


                public enum ReadRegistryODBCKeys
                {
                    Database,
                    LastUser,
                    Server,
                    Trusted_Connection
                }
                public static string ReadRegistryODBC(ReadRegistryODBCKeys Key, string NameODBC = "Schillermed")
                {

                    string regPath = RegKeyGanymedODBC + NameODBC;


                    try
                    {

                        try
                        {
                            var regKey = getRegistry64_32Bit(Microsoft.Win32.RegistryHive.CurrentUser);
                            var value = (string)regKey.OpenSubKey(regPath).GetValue(Key.ToString());
                            return value.Trim();
                        }
                        catch { }

                        try
                        {
                            var regKey = getRegistry64_32Bit(Microsoft.Win32.RegistryHive.LocalMachine);
                            var value = (string)regKey.OpenSubKey(regPath).GetValue(Key.ToString());
                            return value.Trim();
                        }
                        catch
                        {
                            Console.WriteLine("ReadRegistryODBC: Für '" + Key.ToString() + "' kein gültiger ODBC Eintrag gefunden in " + regPath);
                        }

                        return string.Empty;

                    }
                    catch (Exception er)
                    {
                        Console.WriteLine(er.Message);
                        return string.Empty;

                    }

                }


                public enum ReadRegistryGanymedKeys
                {
                    Day,
                    Month,
                    Year,
                    PatNr,
                    Username

                }
                public static string ReadRegistryGanymed(ReadRegistryGanymedKeys Key)
                {
                    // liest von REGEDIT  "HKLM\Software\SCHILLERMED\SMW"
                    try
                    {
                        // "Software\SCHILLERMED\SMW"
                        var regKey = getRegistry64_32Bit(Microsoft.Win32.RegistryHive.LocalMachine);

                        var value = (string)regKey.OpenSubKey(RegKeyGanymed).GetValue(Key.ToString());

                        return value.Trim();

                    }
                    catch // (Exception er)
                    {
                        return "";
                    }

                }


            }


            public class Cbird
            {

                // Create a Timer object that knows to call our TimerCallback method once every 2000 milliseconds.
                //http://stackoverflow.com/questions/186084/how-do-you-add-a-timer-to-a-c-sharp-console-application                
                static System.Threading.Timer sync = null;

                private const string addCbirdField = @"ALTER TABLE Rechnung ADD addedcbird varchar(10)";
                
                private const string getRechnungenFromToday = @"SELECT rechnr, bemerk FROM Rechnung WHERE rgruppe <> 'X'  AND offen = 0 AND addedcbird IS NULL AND 
                                        dat >= DATEADD(DAY, 0, DATEDIFF(DAY, 0, CURRENT_TIMESTAMP))";
          
                private const string getPositionenFromRechnungsNR_SQL = @"SELECT patient.pat_nr, patient.famname, patient.vorname, patient.geb_dat, 
                    patient.kk_kurz, Rechnung.rechnr, Rechnung.dat, leistung.std_code, leistamm.bez, leistamm.wk_ust, leistung.preis, 
                    Rechnung.betrag, Rechnung.offen, Rechnung.Prozent, Rechnung.ProzentText
                    FROM  leistamm INNER JOIN (leistung INNER JOIN (patient INNER JOIN Rechnung ON patient.pat_nr = Rechnung.pat_nr) 
                    ON leistung.rechnr = Rechnung.rechnr) ON (leistamm.kk_kurz = leistung.kk_wie) 
                    AND (leistamm.std_code = leistung.std_code) WHERE Rechnung.rechnr = ";

                private const string PATH_CBIRDWATCH = @"\cbirdWatch\";
                private const string FILENAME_LAST_INVOICE_NR = "webQL_lastinvoice.json";

                private static string lastInvoiceNrPath
                {
                    get
                    {
                        return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), FILENAME_LAST_INVOICE_NR);
                    }
                }
                private static string cbirdJsonPath
                {
                    get
                    {


                        try
                        {   // sucht den ersten Ordner im CBRID Watch Ordner im USER Ordner 
                            return Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + PATH_CBIRDWATCH)[0];
                        }
                        catch (Exception)
                        {
                            throw new Exception("Kein Ordner im CBIRD Home Direktory zur automatisierten Abbuchung gefunden!");
                        }

                    }
                }
                private static int polltimeMs = 1000;
                private static bool preventReentry = false;

                // -------- public methods -------
                public static bool StartSynchronize()
                {

                    checkAddedCbirdField();  // test if exist in TBL 'rechnung' field 'addedcbird', if not add column 'addedcbird'

                    sync = new System.Threading.Timer(TimerCallback, null, 0, polltimeMs); // start SQL polling

                    return true;             
                }
                public static bool StopSynchronize()
                {
                    sync.Dispose();
                    return false;
                }

                
                // ------- private methods ------- 
                private async static Task checkAddedCbirdField()
                {
                    // prüft ob addedcbird field available if not -> create   
                     using (SqlConnection con = new SqlConnection(DbQuery.Settings.ConnectionString))
                    {
                        con.Open();
                        using (SqlCommand cmd = new SqlCommand(addCbirdField, con))
                        {
                            try
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                            catch (Exception){}
                        }
                    }  
                }

                private async static void TimerCallback(Object o)
                {

                    if (preventReentry == true) return; 

                    preventReentry = true;
                    await getnextInvoice();
                    preventReentry = false;

                }

                private static void RetryTimes(Action action, int retries = 3)
                {
                    while (true)
                    {
                        try
                        {
                            action();
                            break; // success!
                        }
                        catch
                        {
                            if (--retries == 0) throw;
                            else Thread.Sleep(1000);
                        }
                    }
                }
               
                private static string RechnungsNummer {get;set;}
                //private static string _rechnungsummer = null;
                //private static string RechnungsNummerXXXX
                //{
                //    // Ganymed TBL:Rechnung  Feld:rechnr  -> Signatur:  Jahr + 4stelliger Index = 20160001
                //    get {
                //            try
                //            {
                //                if (string.IsNullOrEmpty(_rechnungsummer)) _rechnungsummer=File.ReadAllText(lastInvoiceNrPath).Trim();

                //                return _rechnungsummer;
                //            }
                //            catch (Exception)
                //            {
                //                return null;
                //            }
                //    }

                //    set {
                //        try
                //        {
                //            _rechnungsummer = value;
                //            RetryTimes(() => File.WriteAllText(lastInvoiceNrPath, value), 2 );                            
                //        }
                //        catch (Exception e)
                //        {
                //            throw new Exception("Datei " + lastInvoiceNrPath +" kann nicht geschrieben werden. - Fehler:" + e.Message );
                //        } 
                //    }
                //}

                private async static Task getnextInvoice()
                {

                    using (SqlConnection con = new SqlConnection(DbQuery.Settings.ConnectionString))
                    {
                        con.Open();
                        //  @"SELECT rechnr FROM Rechnung WHERE rechnr > " + RechnungsNummer + " AND rgruppe <> 'X' AND offen = 0 AND bemerk like '%bar%' ORDER BY rechnr", con))
                        using (SqlCommand cmd = new SqlCommand(getRechnungenFromToday, con))

                        {
                            var rdr = await cmd.ExecuteReaderAsync();
                            
                            if (await rdr.ReadAsync() == true)
                            {
                                // get next invoice Nr from DB
                                RechnungsNummer = rdr["rechnr"].ToString().Trim();
                                var Bemerkung = rdr["bemerk"].ToString().Trim();

                                Console.WriteLine(RechnungsNummer + "  " + Bemerkung);
                                
                                // generate JSON String for File from DB
                                var JsonResult = await generateInvoiceJSON();
                                
                                var JsonString =JsonConvert.SerializeObject(JsonResult);

                                string infoString = JsonString.Replace("{","");
                                infoString = infoString.Replace("}", "");
                                infoString = infoString.Replace("\"", "");


                             
                                Console.WriteLine(infoString);


                                var result = DialogResult.Yes;
                                
                                // teste ob BAR in Zahlungs Bemerkung eingetragen dann automatisch übernehmen !!!
                                Boolean bar = Bemerkung.ToLower().Contains("bar");

                                if (bar != true) result = MessageBox.Show(infoString, "RechnungsNr:" + RechnungsNummer + " als BAR-Zahlung in Registrierkasse eintragen", MessageBoxButtons.YesNoCancel);

                                Console.WriteLine(result.ToString() + RechnungsNummer);

                                // if CANCEL do nothing no entry in TBL rechnung or writing JSON File for CBIRD
                                if (result == DialogResult.Cancel) return;

                                rdr.Close();

                                // if YES or NO  update decison in tbl rechnung in field addedcbird  
                                cmd.CommandText = @"UPDATE Rechnung SET addedcbird = '" + result.ToString() + "' WHERE rechnr = " + RechnungsNummer;
                                await cmd.ExecuteNonQueryAsync();

                                // if YES write JSON String to File for being handled by cbird
                                if (result == DialogResult.Yes) await writeJSONToFile(JsonString);      

                            }
                        }
                    }  

                }

                private async static Task<Dictionary<string, object>> generateInvoiceJSON()
                {

                    using (SqlConnection con = new SqlConnection(DbQuery.Settings.ConnectionString))
                    {
                        con.Open();
                        using (SqlCommand cmd = new SqlCommand(getPositionenFromRechnungsNR_SQL + RechnungsNummer, con))
                        {


                            Dictionary<string, object> JsonResult = new Dictionary<string, object>(32);
                            List<Dictionary<string, object>> JsonPositionen = new List<Dictionary<string, object>>(32);

                            using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                            {
                                // add dienstleistungen[]
                                while (await rdr.ReadAsync())
                                {
                                    
                                    try
                                    {
                                        Dictionary<string, object> Positionen = new Dictionary<string, object>(6);

                                        string rechnungNr = rdr["rechnr"].ToString().Trim();
                                        int prozent = int.Parse(rdr["Prozent"].ToString().Trim());
                                        string prozentText = rdr["prozentText"].ToString().Trim();
                                        float preis = float.Parse(rdr["preis"].ToString().Trim());

                                        string bez = rechnungNr + " | " + rdr["bez"].ToString().Trim();
                                        if (prozent != 0) bez += " | " + prozent.ToString() + "% " + prozentText;
                                        preis = preis * 100 + preis * prozent;

                                        string ust = rdr["wk_ust"].ToString().Trim();
                                        if (String.IsNullOrEmpty(ust)) ust = "0";
                                        int ustn = int.Parse(ust);

                                        Positionen.Add("bezeichnung", bez);
                                        Positionen.Add("menge", 1);
                                        Positionen.Add("einzelpreis", (int)preis);  
                                        Positionen.Add("ust", ustn);

                                        JsonPositionen.Add(Positionen);

                                    }
                                    catch (Exception)
                                    {
                                        
                                        // ignore error comes from CBIRD 
                                    }
                                    
                                }

                                // add bezeichung
                                Dictionary<string, object> b = new Dictionary<string, object>(1);
                                var bezeichnung = "Ärztliche Leistung befreit USt.(§6(1) UStG) - Handelsware USt. lt Detail";
                                b.Add("bezeichnung", bezeichnung);
                                JsonPositionen.Add(b);

                                JsonResult.Add("zahlungsmittel", "Bar");
                                JsonResult.Add("positionen", JsonPositionen);


                                return JsonResult;
                               // return "{ \"zahlungsmittel\":\"Bar\", \"positionen\":" + JsonConvert.SerializeObject(JsonPositionen) + "}";

                            }
                        }
                    }

                }

                private async static Task writeJSONToFile(string jsonpart)
                {

                    var datePart = DateTime.UtcNow.ToString("yyyy-MM-dd-", CultureInfo.InvariantCulture);
                    var filepath = cbirdJsonPath + @"\" + datePart + RechnungsNummer + ".json";

                    using (var sw = new StreamWriter(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write, bufferSize: 4096, useAsync: true), Encoding.Default))
                    {
                        await sw.WriteAsync(jsonpart); 
                    }

                }

            }
            

            // convertiert Patienten Suchanfrage in Ganymed spezifische QUERY  'Mustermann Franz | Gebdat | Nummer +  #KFA  #1 >65'  
            public static string PatientFilterCRUD(string Term)
            {

                // wandelt Suchfeld(patfilter) inSQL um (Ganymed erlaubt zahl,geburtsdatum,nname+vname
                // Erstellt SQL String aus Text für Patientensuche (Ganymed)
                // PATNR oder Geburtsdatum oder Name _ Vorname mit Wildcard % *
                const string noTerm = "pat_nr $e -9999999";

                if (string.IsNullOrWhiteSpace(Term)) return noTerm;

                string[] separator = { " ", ",", "+" };
                string[] tokens = Term.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length == 0) return noTerm;

                int intResult = 0;
                DateTime dateResult;
                string ps = "";
                string compare = "and";
                int ix = 0;
                int ixn = 0;

                foreach (string t in tokens)
                {
                    ix++;

                    if (int.TryParse(t, out intResult) && intResult > 0 && intResult < int.MaxValue) { ps += " pat_nr $e " + t + " " + compare; continue; }
                    if (DateTime.TryParse(t, out dateResult)) { ps += " geb_dat $e '" + t + "' " + compare; continue; }
                    if (t.StartsWith("#") && int.TryParse(t.Substring(1), out intResult)) { ps += " warteliste $e " + intResult.ToString() + " " + compare; continue; }
                    if (t.StartsWith("#")) { ps += " kk_kurz $starts '" + t.Substring(1, Math.Min(4, t.Length - 1)) + "' " + compare; continue; }

                    if (t.StartsWith(">") && int.TryParse(t.Substring(1), out intResult)) { ps += " geb_dat $lte " + DateTime.Now.Day.ToString() + "." + DateTime.Now.Month.ToString() + "." + (DateTime.Now.Year - intResult).ToString() + " " + compare; continue; }
                    if (t.StartsWith("<") && int.TryParse(t.Substring(1), out intResult)) { ps += " geb_dat $gte " + DateTime.Now.Day.ToString() + "." + DateTime.Now.Month.ToString() + "." + (DateTime.Now.Year - intResult).ToString() + " " + compare; continue; }

                    ixn++;

                    if (t == "*") continue;
                    if (t.StartsWith("*") && ixn == 1) { ps += " famname $has '" + t.Substring(1) + "' " + compare; continue; }
                    if (t.StartsWith("*") && ixn == 2) { ps += " vorname $has '" + t.Substring(1) + "' " + compare; continue; }
                    if (!t.StartsWith("*") && ixn == 1) { ps += " famname $starts '" + t + "' " + compare; continue; }
                    if (!t.StartsWith("*") && ixn == 2) { ps += " vorname $starts '" + t + "' " + compare; continue; }

                }

                if (ps.EndsWith(compare, StringComparison.InvariantCultureIgnoreCase)) ps = ps.Substring(0, ps.Length - compare.Length);

                return ps;

            }
            

            // -- get SMW.INI 
            public static string SMW_INI(string Section, string Item)
            {
                try
                {
                    string SMW_INI_FilePath = System.Environment.GetEnvironmentVariable("windir") + @"\SMW.INI";
                    return IniFile(SMW_INI_FilePath, Section, Item);

                }
                catch
                {
                    return "";
                }

            }
            

            // -- get VARIABLE.INI 
            public static string Variable_INI(string Section, string Item)
            {


                try
                {
                    string s = "VARIABLE_INI";
                    string i = "PATH";

                    string VARIABLE_INI_FilePath = SMW_INI(s, i) + "Variable.ini";

                    return IniFile(VARIABLE_INI_FilePath, Section, Item);
                }
                catch
                {
                    return "";
                }



            }


            //-------PRIVATE------------------------------------------------------------------------------

            // PRIVATE get INI
            private static string IniFile(string FilePath, string Section, string Item)
            {

                Section = Section.StartsWith("[") ? Section : "[" + Section;
                Section = Section.EndsWith("]") ? Section : Section + "]";
                Item = Item + "=";

                bool partLines = false;

                foreach (string line in System.IO.File.ReadAllLines(FilePath, System.Text.Encoding.Default))
                {


                    // SECTION BEGINNS
                    if (line.StartsWith(Section, StringComparison.CurrentCultureIgnoreCase) || partLines == true)
                    {
                        partLines = true;

                        if (line.StartsWith(Item, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return Regex.Replace(line, Item, "", RegexOptions.IgnoreCase).Trim();
                        }
                    }


                    // SECTION FINISHED            
                    if (string.IsNullOrWhiteSpace(line) && partLines == true)
                    {
                        return "";
                    }
                }

                return "";

            }

        }


      

        public class Helpers
        {


            public class Image
            {

                public static string ImageToBase64(string FilePath, System.Drawing.Imaging.ImageFormat format)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        System.Drawing.Image image = System.Drawing.Image.FromFile(FilePath);
                        // Convert Image to byte[]
                        image.Save(ms, format);

                        byte[] imageBytes = ms.ToArray();

                        // Convert byte[] to Base64 String
                        string base64String = Convert.ToBase64String(imageBytes);
                        return base64String;
                    }
                }
                public static System.Drawing.Image Base64ToImage(string base64String)
                {
                    // Convert Base64 String to byte[]
                    byte[] imageBytes = Convert.FromBase64String(base64String);
                    MemoryStream ms = new MemoryStream(imageBytes, 0,
                      imageBytes.Length);

                    // Convert byte[] to Image
                    ms.Write(imageBytes, 0, imageBytes.Length);
                    System.Drawing.Image image = System.Drawing.Image.FromStream(ms, true);
                    return image;
                }

            }

            public class Converter
            {


                public static string RTFToPlainText(string FilePath)
                {
                    System.Windows.Forms.RichTextBox rtBox = new System.Windows.Forms.RichTextBox();
                    rtBox.Rtf = System.IO.File.ReadAllText(FilePath, Encoding.UTF7);
                    return rtBox.Text;
                }
                public static string RTFToPlainTextBase64(string FilePath)
                {
                    byte[] bytes = System.Text.Encoding.Default.GetBytes(RTFToPlainText(FilePath));
                    return System.Convert.ToBase64String(bytes);
                }

                public static string FileToBase64(string FilePath)
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(FilePath);
                    return System.Convert.ToBase64String(bytes);

                }
                public static void Base64ToFile(string FilePath, string base64)
                {
                    System.IO.File.WriteAllBytes(FilePath, System.Convert.FromBase64String(base64));
                }

                public async static Task<string> TextToImgBase64(string filePath)
                {

                    using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
                    {
                        byte[] buffer = new byte[sourceStream.Length];

                        await sourceStream.ReadAsync(buffer, 0, (int)sourceStream.Length);
                        string text = Encoding.ASCII.GetString(buffer, 0, buffer.Length);


                        return TextToImgBase64(text, "ARIAL", 8, System.Drawing.Color.FromArgb(255, 255, 255), System.Drawing.Color.FromArgb(2, 2, 2), 854, 1207); // Verhältnis A4 Seite

                    }


                }
                public static string TextToImgBase64(string txt, string fontname, int fontsize, System.Drawing.Color bgcolor, System.Drawing.Color fcolor, int width, int Height)
                {
                    Bitmap bmp = new Bitmap(width, Height);
                    using (Graphics graphics = Graphics.FromImage(bmp))
                    {

                        RectangleF r = new RectangleF();
                        System.Drawing.Font font = new System.Drawing.Font(fontname, fontsize);
                        graphics.FillRectangle(new SolidBrush(bgcolor), 0, 0, bmp.Width, bmp.Height);
                        graphics.DrawString(txt, font, new SolidBrush(fcolor), r);
                        graphics.Flush();
                        font.Dispose();
                        graphics.Dispose();
                    }

                    // Convert the image to byte[]
                    System.IO.MemoryStream stream = new System.IO.MemoryStream();
                    bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Gif);
                    byte[] imageBytes = stream.ToArray();

                    return Convert.ToBase64String(imageBytes);

                }
                
            }


            public class Sozialversichungsnummer
            {

                public static Boolean IsSVNR(String value)
                {
                    int pruefZiffer = -1;

                    //Ein Leerstring wird nicht geprüft.
                    if (String.IsNullOrEmpty(value))
                    {
                        return false;
                    }

                    //Prüfe die Länge
                    if (value.Length != 10)
                    {
                        return false;
                    }

                    Char[] aSvnr = value.ToCharArray();

                    //Prüfe ob Zahl
                    foreach (Char c in aSvnr)
                    {
                        if (!Char.IsNumber(c))
                        {
                            return false;
                        }
                    }

                    //Prüfziffer ermitteln
                    pruefZiffer = (Convert.ToInt32(aSvnr[0].ToString()) * 3 + Convert.ToInt32(aSvnr[1].ToString()) * 7 + Convert.ToInt32(aSvnr[2].ToString()) * 9 + Convert.ToInt32(aSvnr[4].ToString()) * 5 + Convert.ToInt32(aSvnr[5].ToString()) * 8 + Convert.ToInt32(aSvnr[6].ToString()) * 4 + Convert.ToInt32(aSvnr[7].ToString()) * 2 + Convert.ToInt32(aSvnr[8].ToString()) * 1 + Convert.ToInt32(aSvnr[9].ToString()) * 6) % 11;

                    return (pruefZiffer.Equals(Convert.ToInt32(aSvnr[3].ToString())));
                }

            }
        }


    }

}