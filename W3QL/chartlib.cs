using Chart;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using System.Threading.Tasks;

namespace Chart
{
    public sealed class Chart
    {
        // Konstruktor
        internal Chart(string ConnectionString)
        {           
            mConnectionString = ConnectionString;
        }
        
        // privates
        private string mConnectionString { get; set; }
        private static List<string> GetEnumList<T>()
        {
            // validate that T is in fact an enum
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException();
            }

            return Enum.GetNames(typeof(T)).ToList();
        }
               
        // PUBLIC CLASSES
        // CHARTS Parameter werdenbei Anfrage übergeben 
        public class ChartParameter
        {

            public string Periode = null;
            public string Berechnung =null;
            public List<string> Jahre { get; set; }
            public List<string> Monate { get; set; }
            public List<string> Tage { get; set; }         
            public List<string> Anwender { get; set; }
            public List<string> Leistungen { get; set; }
            public List<string> Versicherungen { get; set; }
            public List<string> RechnungsGruppe { get; set; }
            public List<string> LeistungsGruppe { get; set; }

            public string Titel { get; set; }
            public string Gruppieren { get; set; }
            public string Legende { get; set; }
            public string Tabelle { get; set; }
            public string Tabellenwerte { get; set; }
            public string Ordinate { get; set; }

    
        }
        // CHARTS LIST - füllt die HTML SELECT Felder mit den Auswahlmöglichkeiten
        public class ChartList
        {

            //public List<string> Periode = new List<String>() { "Jahr", "Quartal", "Monat", "Woche", "Wochentag" };
            //public List<string> Berechnung = new List<String>() { "Umsatz_aller_Leistungen", "Umsatz_pro_Leistung", "Anzahl_aller_Leistungen", "Umsatz_pro_Patient", "Leistungen_pro_Patient", "Anzahl_aller_Patienten", "Umsatz_pro_Ordinationstag", "Leistungen_pro_Ordinationstag", "Anzahl_aller_Ordinationstage" };

            public List<string> Periode = Chart.GetEnumList<ChartQuery.enPeriode>();
            public List<string> Berechnung = Chart.GetEnumList<ChartQuery.enBerechnung>();


            public List<string> Monate = new List<String>() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
            public List<string> Tage = new List<String>() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31" };

            public List<string> Jahre { get; set; }
            public List<string> Anwender { get; set; }
            public List<string> Leistungen { get; set; }
            public List<string> Versicherungen { get; set; }
            public List<string> RechnungsGruppe { get; set; }
            public List<string> LeistungsGruppe { get; set; }

        }
        // Ergebnis der CHART QUERY ANFRAGE -> ROWLIST sind die Daten, LEGEND die Legendenwerte
        public class ChartResult
        {
            public BarRangeRowList data { get; set; }
            public List<ValuePair<string, string>> legend { get; set; }
            public string Error { get; set; }
        }

        // PUBLIC MAIN METHODS   --------------------------------------------------------------------------------------
        // get Querydata to Show Diagramm in HTML 
       
        public async Task<string> getChartDataAsync(ChartParameter chartParam)
        {
            // get CHART DATA from PARAM
            ChartQuery cl = new ChartQuery(mConnectionString);
            return await cl.getQueryDataJSON(chartParam);  

        }
        // get Lists to Fill SELECT in HTML
        public async Task<string> getChartListAsync()
        {
                // Chart LIST...
                ChartQuery cl = new ChartQuery(mConnectionString);
                return await cl.WhereCriteria.GetListsJSON();            
        }

    }
    
    public sealed class ChartQuery
    {

       
        public enum enPeriode : int
        {
            Jahr,
            Quartal,
            Monat,
            Woche,
            Wochentag
        }
        public enum enBerechnung : int
        {
            Umsatz_aller_Leistungen,
            Umsatz_pro_Leistung,
            Umsatz_pro_Patient,
            Umsatz_pro_Ordinationstag,
            Anzahl_aller_Leistungen,
            Leistungen_pro_Patient,
            Leistungen_pro_Ordinationstag,
            Anzahl_aller_Patienten,
            Anzahl_aller_Ordinationstage
        }
        public enum enLegendLine : int
        {
            Filter_Zeitraum = 0,
            Filter_Sonstige = 1,
            Periode = 2,
            Berechnung = 3
        }
        
        private string ErrorWriterJSON(Exception ex)
        {

            Dictionary<string, object> json = new Dictionary<string, object>();
            json.Add("Error", ex.Message);  //  + "|" + ex.Source + "|" + ex.StackTrace);
            var jsonSerialiser = new JavaScriptSerializer();
            return jsonSerialiser.Serialize(json);

        }
        
        #region Klasse QueryWhereCriteria
        // -------------------------------------------------------------------------------------------#################################################################
        public class QueryWhereCriteria
        {
            #region Konstanten und Felder
            private const int CommandTimeout = 3600;
            private SqlCommand mCommand = new SqlCommand();
            private ChartQuery mQuery = null;
            #endregion

            #region interner Konstruktor
            internal QueryWhereCriteria(ChartQuery query)
            {
                mCommand.CommandTimeout = CommandTimeout;
                mQuery = query;
            }
            #endregion

            #region öffentliche Methoden : GetListsJSON
            //  MEthode für WEB Controller zum Aufruf RESULT=JSON aller LISTEN des CHART Filters
            public async Task<string> GetListsJSON()
            {

                ChartQuery chartQ = new ChartQuery(mQuery.Connectionstring);

                try
                {
                    Chart.ChartList chartList = await chartQ.WhereCriteria.TryGetLists();

                    var jsonSerialiser = new JavaScriptSerializer();
                    return jsonSerialiser.Serialize(chartList);
                }
                catch(Exception ex)
                {
                    return chartQ.ErrorWriterJSON(ex);
                }

            }
            public async Task<Chart.ChartList> GetLists()
            {

                ChartQuery chartQ = new ChartQuery(mQuery.Connectionstring);
                return await chartQ.WhereCriteria.TryGetLists();


            }


            /// <param name="jahre">
            /// Liste mit allen Jahreszahlen vom Datum der ersten Leistung bis zum aktuellen Jahr im Format "0". Die Liste 
            /// enthält keine Lücken und ist absteigend sortiert. NULL-Verweis, wenn die Abfrage nicht erfolgreich ist.
            /// </param>
            /// <param name="monate">Liste mit den Monaten 1 - 12 im Format "0". Die Liste ist aufsteigend sortiert. NULL-Verweis bei Fehler.</param>
            /// <param name="tage">Liste mit den Tagen 1 - 31 im Format "0". Die Liste ist aufsteigend sortiert. NULL-Verweis bei Fehler.</param>
            /// <param name="anwender">Liste mit den Namen aller Anwender. Die Liste ist aufsteigend sortiert. NULL-Verweis, wenn die Abfrage nicht erfolgreich ist.</param>
            /// <param name="leistungen">Liste mit den Bezeichnungen aller Leistungen. Die Liste ist aufsteigend sortiert. NULL-Verweis, wenn die Abfrage nicht erfolgreich ist.</param>
            /// <param name="kassen">Liste mit den Bezeichnungen aller Kassen. Die Liste ist aufsteigend sortiert. NULL-Verweis, wenn die Abfrage nicht erfolgreich ist.</param>
            /// <param name="lGruppen">Liste mit den Bezeichnungen aller L-Gruppen. Die Liste ist aufsteigend sortiert. NULL-Verweis, wenn die Abfrage nicht erfolgreich ist.</param>
            /// <param name="rGruppen">Liste mit den Bezeichnungen aller R-Gruppen. Die Liste ist aufsteigend sortiert. NULL-Verweis, wenn die Abfrage nicht erfolgreich ist.</param>
            /// <param name="ex">Enthält einen NULL-Verweis wenn alle Listen erfolgreich gefüllt wurden, sonst ein Exception Objekt.</param>
            /// <returns>True wenn alle Listen erfolgreich gefüllt wurden, sonst False.</returns>
            public async Task<Chart.ChartList> TryGetLists()
            {
                Chart.ChartList cf = new Chart.ChartList();

                    using (SqlConnection connection = new SqlConnection(mQuery.Connectionstring))
                    {
                        connection.Open();
                        cf.Jahre = await SetYears(connection);
                        cf.Monate= SetMonths();
                        cf.Tage=await SetDays();
                        cf.Anwender = await SetAnwender(connection);
                        cf.Leistungen = await SetLeistungen(connection);
                        cf.Versicherungen= await SetKassen(connection);
                        cf.LeistungsGruppe = await SetLGruppen(connection);
                        cf.RechnungsGruppe= await SetRGruppen(connection);
                    }

                    return cf;
            }


            #region private Methoden : SetXXX
            private async Task<List<string>> SetYears(SqlConnection connection)
            {
                List<string> jahre = new List<string>(35);
                mCommand.Connection = connection;
                mCommand.CommandText = "SELECT YEAR(GETDATE())";

                int maxYear = Convert.ToInt32(await mCommand.ExecuteScalarAsync());
                int minYear = maxYear;

                mCommand.CommandText = "SELECT MIN(dat) FROM leistung";
                object minDate = await mCommand.ExecuteScalarAsync();

                if (minDate is DateTime)
                    minYear = Convert.ToDateTime(minDate).Year;

                if (minYear > maxYear)
                {
                    int temp = minYear;
                    minYear = maxYear;
                    maxYear = temp;
                }

                while (maxYear >= minYear)
                {
                    jahre.Add(maxYear.ToString("0000", mCulture));
                    maxYear--;
                }
                return jahre;
            }
            private List<string> SetMonths()
            {
                List<string> monate = new List<string>(12);

                for (int i = 1; i <= 12; i++)
                    monate.Add(i.ToString("00", mCulture));
                //monate.Add(new DateTime(2000, i, 1).ToString("MMM", mCulture));

                return monate;

            }
            private async Task<List<string>> SetDays()
            {
                List<string> tage = new List<string>(31);

                for (int i = 1; i <= 31; i++)
                    tage.Add(i.ToString("00", mCulture));
                return tage;
            }
            private async Task<List<string>> SetAnwender(SqlConnection connection)
            {
                List<string> anwender = new List<string>(30);
                mCommand.Connection = connection;
                mCommand.CommandText =
                    "SELECT DISTINCT LTRIM(RTRIM(Anwender)) FROM Benutzer " +
                    "WHERE Anwender IS NOT NULL ORDER BY LTRIM(RTRIM(Anwender))";

                using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
                {
                    while (rdr.Read())
                        anwender.Add(rdr[0].ToString());
                }
                return anwender;
            }
            private async Task<List<string>> SetLeistungen(SqlConnection connection)
            {
                List<string> leistungen = new List<string>(450);
                mCommand.Connection = connection;
                mCommand.CommandText =
                    "SELECT DISTINCT LTRIM(RTRIM(std_code)) FROM leistamm " +
                    "WHERE std_code IS NOT NULL ORDER BY LTRIM(RTRIM(std_code))";

                using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
                {
                    while (rdr.Read())
                        leistungen.Add(rdr[0].ToString());
                }
                return leistungen;
            }
            private async Task<List<string>> SetKassen(SqlConnection connection)
            {
                List<string> kassen = new List<string>(50);
                mCommand.Connection = connection;
                mCommand.CommandText =
                    "SELECT LTRIM(RTRIM(kk_kurz)) FROM kasse " +
                    "WHERE kk_kurz IS NOT NULL ORDER BY LTRIM(RTRIM(kk_kurz))";

                using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
                {
                    while (rdr.Read())
                        kassen.Add(rdr[0].ToString());
                }
                return kassen;
            }
            private async Task<List<string>> SetLGruppen(SqlConnection connection)
            {
                List<string> lGruppen = new List<string>(15);
                mCommand.Connection = connection;
                mCommand.CommandText =
                    "SELECT DISTINCT LTRIM(RTRIM(lgruppe)) FROM leistamm WHERE " +
                    "lgruppe IS NOT NULL ORDER BY LTRIM(RTRIM(lgruppe))";

                using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
                {
                    while (rdr.Read())
                        lGruppen.Add(rdr[0].ToString());
                }
                return lGruppen;
            }
            private async Task<List<string>> SetRGruppen(SqlConnection connection)
            {
                List<string> rGruppen = new List<string>(10);
                mCommand.Connection = connection;
                mCommand.CommandText =
                    "SELECT DISTINCT LTRIM(RTRIM(rgruppe)) FROM leistamm WHERE " +
                    "rgruppe IS NOT NULL ORDER BY LTRIM(RTRIM(rgruppe))";

                using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
                {
                    while (rdr.Read())
                        rGruppen.Add(rdr[0].ToString());
                }
                return rGruppen;
            }
            #endregion
        }
        // -------------------------------------------------------------------------------------------#################################################################

        #endregion

        #endregion

        #region Konstanten und readonly Felder
        private const int CommandTimeout = 3600;
        private static readonly CultureInfo mCulture = new CultureInfo("de-AT");
        #endregion

        #region Felder zu Command und QueryWhereCriteria
        private SqlCommand mCommand = new SqlCommand();
        private string mConnectionString = null;
        private QueryWhereCriteria mWhereCriteria = null;
        #endregion

        #region Felder zu Listen
        //
        private List<int> mYears = new List<int>(64);
        private List<int> mMonths = new List<int>(12);
        private List<int> mDays = new List<int>(31);
        List<ValuePair<string, string>> mLegendLines = new List<ValuePair<string, string>>(4);
        //
        #endregion

        #region Felder zu Delegates
     
        private Func<SqlConnection, Task<BarRangeRowList>> TryGetRangeRowList = null;

        #endregion

        #region Eigenschaften
        public string Connectionstring
        {
            get { return mConnectionString; }
            set { mConnectionString = value; }
        }
        public QueryWhereCriteria WhereCriteria
        {
            get { return mWhereCriteria; }
        }
        #endregion

        #region Konstruktoren
        internal ChartQuery(SqlCommand command, string connectionString)
        {
            mCommand = command;
            mConnectionString = connectionString;
            mWhereCriteria = new QueryWhereCriteria(this);
            mCommand.CommandTimeout = CommandTimeout;
            Clear();
        }
        public ChartQuery(string connectionString) : this(new SqlCommand(), connectionString) { }
        public ChartQuery() : this(new SqlCommand(), null) { }
        #endregion

        #region Öffentliche Methoden : getQueryData + getQueryDataJSON

        /// <param name="periode">Die zeitliche Gruppierung zur Berechnung der Daten. Es wird immer auch nach Jahr gruppiert.</param>
        /// <param name="berechnung">Die Art der über die gruppierten Daten ausgeführten Berechnung.</param>
        /// <param name="jahre">Einschränkung nach Jahren. Bei NULL-Verweis oder leerer Liste erfolgt keine Einschränkung.</param>
        /// <param name="monate">Einschränkung nach Monaten. Bei NULL-Verweis oder leerer Liste erfolgt keine Einschränkung.</param>
        /// <param name="tage">Einschränkung nach Tagen. Bei NULL-Verweis oder leerer Liste erfolgt keine Einschränkung.</param>
        /// <param name="anwender">Einschränkung nach Anwendern. Bei NULL-Verweis oder leerer Liste erfolgt keine Einschränkung.</param>
        /// <param name="leistungen">Einschränkung nach Leistungen. Bei NULL-Verweis oder leerer Liste erfolgt keine Einschränkung.</param>
        /// <param name="kassen">Einschränkung nach Kassen. Bei NULL-Verweis oder leerer Liste erfolgt keine Einschränkung.</param>
        /// <param name="lGruppen">Einschränkung nach L-Gruppen. Bei NULL-Verweis oder leerer Liste erfolgt keine Einschränkung.</param>
        /// <param name="rGruppen">Einschränkung nach R-Gruppen. Bei NULL-Verweis oder leerer Liste erfolgt keine Einschränkung.</param>
        /// <param name="painter">Enthält ein Objekt vom Typ ChartPainter bei erfolgreich ausgeführter Abfrage, sonst einen NULL-Verweis.</param>
        /// <param name="ex">Enthält einen NULL-Verweis bei erfolgreich ausgeführter Abfrage, sonst ein Exception Objekt.</param>
        /// <returns>True bei erfolgreich ausgeführter Abfrage, sonst False.</returns>
        public async Task<Chart.ChartResult> getQueryData(Chart.ChartParameter chartParam)
        {
            ChartQuery chartQ = new ChartQuery(mConnectionString);
            return  await chartQ.tryGetQueryData(chartParam);

        }
        public async Task<string> getQueryDataJSON(Chart.ChartParameter chartParam)
        {
            ChartQuery chartQ = new ChartQuery(mConnectionString);

            try
            {
                Chart.ChartResult chartRes = await chartQ.tryGetQueryData(chartParam);

                var jsonSerialiser = new JavaScriptSerializer();
                return jsonSerialiser.Serialize(chartRes);
            }
            catch (Exception ex)
            {
                return chartQ.ErrorWriterJSON(ex);
            }
        }
     
        
        public async Task<Chart.ChartResult> tryGetQueryData(Chart.ChartParameter chartParam)
        {

            ChartQuery.enPeriode periode = (ChartQuery.enPeriode)Enum.Parse(typeof(ChartQuery.enPeriode), chartParam.Periode, true);
            ChartQuery.enBerechnung berechnung = (ChartQuery.enBerechnung)Enum.Parse(typeof(ChartQuery.enBerechnung), chartParam.Berechnung, true);

            Chart.ChartResult res = new Chart.ChartResult();

            Clear();

            string sqlWhere = string.Empty;
            string strBerechnung;
            string strPeriode;
            string strJahre = "Jahre: ";
            string strMonate = "Monate: ";
            string strTage = "Tage: ";
            string strAnwender = "Anwender: ";
            string strLeistungen = "Positionen: ";
            string strKassen = "Versicherung: ";
            string strLGruppen = "L-Gruppen: ";
            string strRGruppen = "R-Gruppen: ";
            string format = "N2";
            int minStepWidth = 0;

            bool ignoreForWhere;
            List<SqlParameter> parameters = new List<SqlParameter>(25);

            try
            {
                
                strBerechnung = berechnung.ToString().Replace('_',' ');
                //switch (berechnung)
                //{


                //    case enBerechnung.Umsatz_aller_Leistungen:
                //        strBerechnung = "Umsatz der Leistungen";
                //        break;
                //    case enBerechnung.Umsatz_pro_Leistung:
                //        strBerechnung = "Umsatz pro Leistung";
                //        break;
                //    case enBerechnung.Anzahl_aller_Leistungen:
                //        strBerechnung = "Anzahl der Leistungen";
                //        break;
                //    case enBerechnung.Umsatz_pro_Patient:
                //        strBerechnung = "Umsatz pro Patient";
                //        break;
                //    case enBerechnung.Leistungen_pro_Patient:
                //        strBerechnung = "Leistungen pro Patient";
                //        break;
                //    case enBerechnung.Anzahl_aller_Patienten:
                //        strBerechnung = "Anzahl der Patienten";
                //        break;
                //    case enBerechnung.Umsatz_pro_Ordinationstag:
                //        strBerechnung = "Umsatz pro Ordinationstag";
                //        break;
                //    case enBerechnung.Leistungen_pro_Ordinationstag:
                //        strBerechnung = "Leistungen pro Ordinationstag";
                //        break;
                //    case enBerechnung.Anzahl_aller_Ordinationstage:
                //        strBerechnung = "Anzahl der Ordinationstage";
                //        break;
                //    default:
                //        throw new Exception("Es wurde kein gültiges Aggregat ausgewählt");
                //}

                using (SqlConnection connection = new SqlConnection(mConnectionString))
                {
                    connection.Open();

                    // Jahre
                    ignoreForWhere = await SetYears(connection,chartParam.Jahre);
                    if (!ignoreForWhere)
                    {
                        string where = " AND ";
                        SqlParameter[] psJahre = new SqlParameter[mYears.Count];

                        for (int i = 0; i < mYears.Count; i++)
                        {
                            strJahre += i == 0 ? mYears[i].ToString("0", mCulture) : ", " + mYears[i].ToString("0", mCulture);

                            SqlParameter p = new SqlParameter("@y" + i.ToString("0"), SqlDbType.Int);
                            p.Value = mYears[i];
                            psJahre[i] = p;

                            where += (i == 0 ? "DATEPART(YY,l.dat) IN (" : ",") + p.ParameterName + (i == mYears.Count - 1 ? ")" : "");
                        }

                        parameters.AddRange(psJahre);
                        sqlWhere += where;
                    }
                    else
                        strJahre += "[Alle]";
                    //

                    // Monate
                    ignoreForWhere = SetMonths(chartParam.Monate);
                    if (!ignoreForWhere)
                    {
                        string where = " AND ";
                        SqlParameter[] psMonate = new SqlParameter[mMonths.Count];

                        for (int i = 0; i < mMonths.Count; i++)
                        {
                            strMonate += i == 0 ? mMonths[i].ToString("00", mCulture) : ", " + mMonths[i].ToString("00", mCulture);

                            SqlParameter p = new SqlParameter("@m" + i.ToString("0"), SqlDbType.Int);
                            p.Value = mMonths[i];
                            psMonate[i] = p;

                            where += (i == 0 ? "DATEPART(M,l.dat) IN (" : ",") + p.ParameterName + (i == mMonths.Count - 1 ? ")" : "");
                        }

                        parameters.AddRange(psMonate);
                        sqlWhere += where;
                    }
                    else
                        strMonate += "[Alle]";
                    //

                    // Tage
                    SetDays(chartParam.Tage);
                    if (mDays != null && mDays.Count > 0)
                    {
                        string where = " AND ";
                        SqlParameter[] psTage = new SqlParameter[mDays.Count];

                        for (int i = 0; i < mDays.Count; i++)
                        {
                            strTage += i == 0 ? mDays[i].ToString("00", mCulture) : ", " + mDays[i].ToString("00", mCulture);

                            SqlParameter p = new SqlParameter("@d" + i.ToString("0"), SqlDbType.Int);
                            p.Value = mDays[i];
                            psTage[i] = p;

                            where += (i == 0 ? "DATEPART(D,l.dat) IN (" : ",") + p.ParameterName + (i == mDays.Count - 1 ? ")" : "");
                        }

                        parameters.AddRange(psTage);
                        sqlWhere += where;
                    }
                    else
                        strTage += "[Alle]";
                    //

                    // Anwender
                    if (chartParam.Anwender != null && chartParam.Anwender.Count > 0)
                    {
                        string where = " AND ";
                        SqlParameter[] psAnwender = new SqlParameter[chartParam.Anwender.Count];

                        for (int i = 0; i < chartParam.Anwender.Count; i++)
                        {
                            string anw = chartParam.Anwender[i] == null ? string.Empty : chartParam.Anwender[i].Trim();
                            strAnwender += i == 0 ? anw : ", " + anw;

                            SqlParameter p = new SqlParameter("@a" + i.ToString("0"), SqlDbType.Char, 30);
                            p.Value = anw;
                            psAnwender[i] = p;

                            where += (i == 0 ? "b.Anwender IN (" : ",") + p.ParameterName + (i == chartParam.Anwender.Count - 1 ? ")" : "");
                        }

                        parameters.AddRange(psAnwender);
                        sqlWhere += where;
                    }
                    else
                        strAnwender += "[Alle]";
                    //

                    // Leistungen
                    if (chartParam.Leistungen != null && chartParam.Leistungen.Count > 0)
                    {
                        string where = " AND ";
                        SqlParameter[] psLeistungen = new SqlParameter[chartParam.Leistungen.Count];

                        for (int i = 0; i < chartParam.Leistungen.Count; i++)
                        {
                            bool like = false;
                            string leistung = chartParam.Leistungen[i] == null ? string.Empty : chartParam.Leistungen[i].Trim();
                            strLeistungen += i == 0 ? leistung : ", " + leistung;

                            if (leistung.EndsWith("*") || leistung.EndsWith("%"))
                            {
                                leistung = leistung.Substring(0, leistung.Length - 1);
                                like = true;
                            }

                            SqlParameter p = new SqlParameter("@lst" + i.ToString("0"), SqlDbType.Char, 8);
                            p.Value = leistung;
                            psLeistungen[i] = p;

                            if (like)
                                where += (i == 0 ? "(" : " OR ") + "ls.std_code LIKE " + p.ParameterName + (i == chartParam.Leistungen.Count - 1 ? "+'%')" : "+'%'");
                            else
                                where += (i == 0 ? "(" : " OR ") + "ls.std_code=" + p.ParameterName + (i == chartParam.Leistungen.Count - 1 ? ")" : "");
                        }

                        parameters.AddRange(psLeistungen);
                        sqlWhere += where;
                    }
                    else
                        strLeistungen += "[Alle]";
                    //

                    // Kassen
                    if (chartParam.Versicherungen != null && chartParam.Versicherungen.Count > 0)
                    {
                        string where = " AND ";
                        SqlParameter[] psKassen = new SqlParameter[chartParam.Versicherungen.Count];

                        for (int i = 0; i < chartParam.Versicherungen.Count; i++)
                        {
                            string kasse = chartParam.Versicherungen[i] == null ? string.Empty : chartParam.Versicherungen[i].Trim();
                            strKassen += i == 0 ? kasse : ", " + kasse;
                            
                            SqlParameter p = new SqlParameter("@k" + i.ToString("0"), SqlDbType.Char, 4);
                            p.Value = kasse;
                            psKassen[i] = p;

                            //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx woko 12/2015 aurelia KFAG BUG
//                            where += (i == 0 ? "ls.kk_kurz IN (" : ",") + p.ParameterName + (i == chartParam.Versicherungen.Count - 1 ? ")" : "");
                            where += (i == 0 ? "l.kk_kurz IN (" : ",") + p.ParameterName + (i == chartParam.Versicherungen.Count - 1 ? ")" : "");
                        }

                        parameters.AddRange(psKassen);
                        sqlWhere += where;
                    }
                    else
                        strKassen += "[Alle]";
                    //

                    // LGruppen
                    if (chartParam.LeistungsGruppe != null && chartParam.LeistungsGruppe.Count > 0)
                    {
                        string where = " AND ";
                        SqlParameter[] psLGruppen = new SqlParameter[chartParam.LeistungsGruppe.Count];

                        for (int i = 0; i < chartParam.LeistungsGruppe.Count; i++)
                        {
                            string lGruppe = chartParam.LeistungsGruppe[i] == null ? string.Empty : chartParam.LeistungsGruppe[i].Trim();
                            strLGruppen += i == 0 ? lGruppe : ", " + lGruppe;

                            SqlParameter p = new SqlParameter("@lg" + i.ToString("0"), SqlDbType.Char, 1);
                            p.Value = lGruppe;
                            psLGruppen[i] = p;

                            where += (i == 0 ? "l.lgruppe IN (" : ",") + p.ParameterName + (i == chartParam.LeistungsGruppe.Count - 1 ? ")" : "");
                        }

                        parameters.AddRange(psLGruppen);
                        sqlWhere += where;
                    }
                    else
                        strLGruppen += "[Alle]";
                    //

                    // RGruppen
                    if (chartParam.RechnungsGruppe != null && chartParam.RechnungsGruppe.Count > 0)
                    {
                        string where = " AND ";
                        SqlParameter[] psRGruppen = new SqlParameter[chartParam.RechnungsGruppe.Count];

                        for (int i = 0; i < chartParam.RechnungsGruppe.Count; i++)
                        {
                            string rGruppe = chartParam.RechnungsGruppe[i] == null ? string.Empty : chartParam.RechnungsGruppe[i].Trim();
                            strRGruppen += i == 0 ? rGruppe : ", " + rGruppe;


                            SqlParameter p = new SqlParameter("@rg" + i.ToString("0"), SqlDbType.Char, 1);
                            p.Value = rGruppe;
                            psRGruppen[i] = p;

                            where += (i == 0 ? "l.rgruppe IN (" : ",") + p.ParameterName + (i == chartParam.RechnungsGruppe.Count - 1 ? ")" : "");
                        }

                        parameters.AddRange(psRGruppen);
                        sqlWhere += where;
                    }
                    else
                        strRGruppen += "[Alle]";
                    //

                    mCommand.Parameters.AddRange(parameters.ToArray());

                    switch (periode)
                    {
                        case enPeriode.Jahr:

                            strPeriode = "Jahr";

                            if (berechnung == enBerechnung.Umsatz_aller_Leistungen)
                            {
                                SetCommandText_Periode_Jahr_Umsatz_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Leistung)
                            {
                                SetCommandText_Periode_Jahr_Umsatz_pro_Leistung(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Leistungen)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Jahr_Anzahl_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Patient)
                            {
                                SetCommandText_Periode_Jahr_Umsatz_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Patient)
                            {
                                SetCommandText_Periode_Jahr_Leistungen_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Patienten)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Jahr_Anzahl_der_Patienten(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Ordinationstag)
                            {
                                SetCommandText_Periode_Jahr_Umsatz_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Ordinationstag)
                            {
                                SetCommandText_Periode_Jahr_Leistungen_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Ordinationstage)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Jahr_Anzahl_der_Ordinationstage(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Jahr;
                            }

                            break;

                        case enPeriode.Quartal:

                            strPeriode = "Quartal";

                            if (berechnung == enBerechnung.Umsatz_aller_Leistungen)
                            {
                                SetCommandText_Periode_Quartal_Umsatz_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Leistung)
                            {
                                SetCommandText_Periode_Quartal_Umsatz_pro_Leistung(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Leistungen)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Quartal_Anzahl_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Patient)
                            {
                                SetCommandText_Periode_Quartal_Umsatz_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Patient)
                            {
                                SetCommandText_Periode_Quartal_Leistungen_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Patienten)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Quartal_Anzahl_der_Patienten(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Ordinationstag)
                            {
                                SetCommandText_Periode_Quartal_Umsatz_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Ordinationstag)
                            {
                                SetCommandText_Periode_Quartal_Leistungen_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Ordinationstage)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Quartal_Anzahl_der_Ordinationstage(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Quartal;
                            }

                            break;

                        case enPeriode.Monat:

                            strPeriode = "Monat";

                            if (berechnung == enBerechnung.Umsatz_aller_Leistungen)
                            {
                                SetCommandText_Periode_Monat_Umsatz_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Leistung)
                            {
                                SetCommandText_Periode_Monat_Umsatz_pro_Leistung(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Leistungen)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Monat_Anzahl_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Patient)
                            {
                                SetCommandText_Periode_Monat_Umsatz_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Patient)
                            {
                                SetCommandText_Periode_Monat_Leistungen_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Patienten)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Monat_Anzahl_der_Patienten(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Ordinationstag)
                            {
                                SetCommandText_Periode_Monat_Umsatz_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Ordinationstag)
                            {
                                SetCommandText_Periode_Monat_Leistungen_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Ordinationstage)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Monat_Anzahl_der_Ordinationstage(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Monat;
                            }

                            break;

                        case enPeriode.Woche:

                            strPeriode = "Kalenderwoche";

                            if (berechnung == enBerechnung.Umsatz_aller_Leistungen)
                            {
                                SetCommandText_Periode_Woche_Umsatz_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Umsatz_der_Leistungen_Anzahl;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Leistung)
                            {
                                SetCommandText_Periode_Woche_Umsatz_pro_Leistung(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Umsatz_pro_Leistung;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Leistungen)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Woche_Anzahl_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Umsatz_der_Leistungen_Anzahl;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Patient)
                            {
                                SetCommandText_Periode_Woche_Umsatz_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Patient_Umsatz_Leistungen;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Patient)
                            {
                                SetCommandText_Periode_Woche_Leistungen_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Patient_Umsatz_Leistungen;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Patienten)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Woche_Anzahl_der_Patienten(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Patient_Anzahl;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Ordinationstag)
                            {
                                SetCommandText_Periode_Woche_Umsatz_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Tag_Umsatz_Leistungen;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Ordinationstag)
                            {
                                SetCommandText_Periode_Woche_Leistungen_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Tag_Umsatz_Leistungen;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Ordinationstage)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Woche_Anzahl_der_Ordinationstage(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Woche_Tag_Anzahl;
                            }

                            break;

                        case enPeriode.Wochentag:

                            strPeriode = "Wochentag(Ø)";

                            if (berechnung == enBerechnung.Umsatz_aller_Leistungen)
                            {
                                SetCommandText_Periode_Wochentag_Umsatz_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Leistung)
                            {
                                SetCommandText_Periode_Wochentag_Umsatz_pro_Leistung(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Leistungen)
                            {
                                SetCommandText_Periode_Wochentag_Anzahl_der_Leistungen(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Patient)
                            {
                                SetCommandText_Periode_Wochentag_Umsatz_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Patient)
                            {
                                SetCommandText_Periode_Wochentag_Leistungen_pro_Patient(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Patienten)
                            {
                                SetCommandText_Periode_Wochentag_Anzahl_der_Patienten(ref sqlWhere); //
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }
                            else if (berechnung == enBerechnung.Umsatz_pro_Ordinationstag)//
                            {
                                SetCommandText_Periode_Wochentag_Umsatz_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }
                            else if (berechnung == enBerechnung.Leistungen_pro_Ordinationstag) // 
                            {
                                SetCommandText_Periode_Wochentag_Leistungen_pro_Ordinationstag(ref sqlWhere);
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }
                            else if (berechnung == enBerechnung.Anzahl_aller_Ordinationstage)
                            {
                                format = "N0";
                                minStepWidth = 1;
                                SetCommandText_Periode_Wochentag_Anzahl_der_Ordinationstage(ref sqlWhere);//
                                TryGetRangeRowList = TryGetRangeRowList_Periode_Wochentag;
                            }

                            break;

                        default:
                            throw new Exception("Es wurde keine gültige Periode ausgewählt");
                    }

                    mLegendLines.Add(new ValuePair<string, string>("Filter Zeitraum:", strTage + "; " + strMonate + "; " + strJahre));
                    mLegendLines.Add(new ValuePair<string, string>("Sonstige Filter:", strAnwender + "; " + strKassen + "; " + strLGruppen + "; " + strRGruppen + "; " + strLeistungen));
                    mLegendLines.Add(new ValuePair<string, string>("Periode:", strPeriode));
                    mLegendLines.Add(new ValuePair<string, string>("Berechnung:", strBerechnung));


                    res.legend = mLegendLines;
                    
                    // xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
                    //Console.WriteLine(mCommand.CommandText);

                    res.data = await TryGetRangeRowList(connection);
                }

                return res;
            }
            catch 
            {               
                return null;
            }
        }

        #endregion

        #region Private Methoden: Clear, SetYears, SetMonths, SetDays, GetMaxWeeksOfYears
        private void Clear()
        {
            mCommand.CommandText = null;
            mCommand.Parameters.Clear();
            TryGetRangeRowList = null;
            mYears.Clear();
            mMonths.Clear();
            mDays.Clear();
            mLegendLines.Clear();
        }
        private async Task<bool> SetYears(SqlConnection connection, IList<string> years)
        {
            mYears.Clear();

            if (years != null && years.Count > 0)
            {
                for (int i = 0; i < years.Count; i++)
                {
                    int year;

                    if (!int.TryParse(years[i], NumberStyles.Integer, mCulture, out year))
                        continue;

                    if (year < 1800 || year > 2200)
                        continue;

                    mYears.Add(year);
                }

                mYears.Sort(delegate(int x, int y) { return y.CompareTo(x); });

                for (int i = 1; i < mYears.Count; i++)
                {
                    if (mYears[i - 1] == mYears[i])
                        mYears.RemoveAt(i--);
                }
            }

            if (mYears.Count > 0)
            {
                return false;
            }

           bool ignoreForWhere = true;
            mCommand.Connection = connection;
            mCommand.CommandText = "SELECT YEAR(GETDATE())";

            int maxYear = Convert.ToInt32(await mCommand.ExecuteScalarAsync());
            int minYear = maxYear;

            //mCommand.CommandText = "SELECT MIN(AU_FAHRT_TEILBEREICH.Fahrt_teilbereich_beladung_datum)FROM AU_FAHRT_TEILBEREICH";
            mCommand.CommandText = "SELECT MIN(dat)FROM leistung";
            object minDate = await mCommand.ExecuteScalarAsync();

            if (minDate is DateTime)
                minYear = Convert.ToDateTime(minDate).Year;

            if (minYear > maxYear)
            {
                int temp = minYear;
                minYear = maxYear;
                maxYear = temp;
            }

            while (maxYear >= minYear)
            {
                mYears.Add(maxYear);
                maxYear--;
            }
            return ignoreForWhere;
        }

        private bool SetMonths(IList<string> months)
        {
            mMonths.Clear();

            if (months != null && months.Count > 0)
            {
                for (int i = 0; i < months.Count; i++)
                {
                    int month;

                    if (!int.TryParse(months[i], NumberStyles.Integer, mCulture, out month))
                        continue;

                    if (month < 1 || month > 12)
                        continue;

                    mMonths.Add(month);
                }

                mMonths.Sort(delegate(int x, int y) { return x.CompareTo(y); });

                for (int i = 1; i < mMonths.Count; i++)
                {
                    if (mMonths[i - 1] == mMonths[i])
                        mMonths.RemoveAt(i--);
                }
            }

            if (mMonths.Count == 0)
            {
                return  true;

            }
            else
                return false;
        }
        
        private void SetDays(IList<string> days)
        {
            mDays.Clear();

            if (days != null && days.Count > 0)
            {
                for (int i = 0; i < days.Count; i++)
                {
                    int day;

                    if (!int.TryParse(days[i], NumberStyles.Integer, mCulture, out day))
                        continue;

                    if (day < 1 || day > 31)
                        continue;

                    mDays.Add(day);
                }

                mDays.Sort(delegate(int x, int y) { return x.CompareTo(y); });

                for (int i = 1; i < mDays.Count; i++)
                {
                    if (mDays[i - 1] == mDays[i])
                        mDays.RemoveAt(i--);
                }
            }
        }
        
        private int GetMaxWeeksOfYears()
        {
            // mYears kann nicht leer sein
            // ein Jahr hat 52 oder 53 Europ.ISO-Wochen
            int weeks = 52;
            for (int i = 0; i < mYears.Count; i++)
            {
                EuropeanIsoWeek.TryGetWeeksCount(mYears[i], out weeks);
                if (weeks > 52)
                    break;
            }
            return weeks;
        }
        #endregion

        #region Private Methoden: Targets zu TryGetRangeRowListDelegate
        //
        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Jahr(SqlConnection connection)
        {
            int rangeCount = 1;
            BarRangeRowList rowList = new BarRangeRowList(rangeCount, 20);
            rowList.RangeCaptions[0] = "Jahr";

            Dictionary<int, BarRangeRow> yearRowDict = new Dictionary<int, BarRangeRow>(rangeCount);

            for (int i = 0; i < mYears.Count; i++)
            {
                int year = mYears[i];
                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[rangeCount]);
                rowList.Add(row);
                yearRowDict.Add(mYears[i], row);
            }

            mCommand.Connection = connection;

            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    BarRangeRow row;
                    int year = Convert.ToInt32(rdr[0]);

                    if (yearRowDict.TryGetValue(year, out row))
                    {
                        if (rdr[1] != DBNull.Value)
                            row.y[0] = Convert.ToDouble(rdr[1]);
                    }
                }
            }

            return rowList;
        }

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Quartal(SqlConnection connection)
        {
            int rangeCount = 4;
            BarRangeRowList rowList = new BarRangeRowList(rangeCount, 20);

            //for (int i = 0; i < rowList.RangeCount; i++)
            //rowList.RangeCaptions[i] = "Quartal " + (i + 1).ToString("0");
            rowList.RangeCaptions[0] = "Quartal";

            Dictionary<int, BarRangeRow> yearRowDict = new Dictionary<int, BarRangeRow>(rangeCount);

            for (int i = 0; i < mYears.Count; i++)
            {
                int year = mYears[i];
                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[rangeCount]);
                rowList.Add(row);
                yearRowDict.Add(mYears[i], row);
            }

            mCommand.Connection = connection;

            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    BarRangeRow row;
                    int year = Convert.ToInt32(rdr[0]);

                    if (yearRowDict.TryGetValue(year, out row))
                    {
                        if (rdr[2] != DBNull.Value)
                            row.y[Convert.ToInt32(rdr[1]) - 1] = Convert.ToDouble(rdr[2]);
                    }
                }
            }
            return rowList;
        }

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Monat(SqlConnection connection)
        {
            BarRangeRowList rowList = null;
            int rangeCount = 12; // mMonths.Count;
            rowList = new BarRangeRowList(rangeCount, 20);
            Dictionary<int, int> monthValueIndexDict = new Dictionary<int, int>(rangeCount);
            Dictionary<int, BarRangeRow> yearRowDict = new Dictionary<int, BarRangeRow>(rangeCount);

            for (int i = 0; i < rowList.RangeCount; i++)
            {
                //   rowList.RangeCaptions[i] = new DateTime(2000, mMonths[i], 1).ToString("MMM", mCulture);
                //     monthValueIndexDict.Add(mMonths[i], i);

                //rowList.RangeCaptions[i] = new DateTime(2000, i + 1, 1).ToString("MMM", mCulture);
                monthValueIndexDict.Add(i + 1, i);
            }
            rowList.RangeCaptions[0] = "Monat";


            for (int i = 0; i < mYears.Count; i++)
            {
                int year = mYears[i];
                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[rangeCount]);
                rowList.Add(row);
                yearRowDict.Add(mYears[i], row);
            }

            mCommand.Connection = connection;

            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    BarRangeRow row;
                    int year = Convert.ToInt32(rdr[0]);

                    if (yearRowDict.TryGetValue(year, out row))
                    {
                        if (rdr[2] != DBNull.Value)
                        {
                            int month = Convert.ToInt32(rdr[1]), monthIndex;

                            if (monthValueIndexDict.TryGetValue(month, out monthIndex))
                                row.y[monthIndex] = Convert.ToDouble(rdr[2]);
                            //monthValueIndexDict.TryGetValue(month, out monthIndex);
                            //row.y[monthcounter] = Convert.ToDouble(rdr[2]);
                            //monthcounter++;

                        }
                    }
                }
            }
            return rowList;
        }

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Woche_Umsatz_der_Leistungen_Anzahl(SqlConnection connection)
        {
            SortedDictionary<int, SortedDictionary<int, double>> dictYear = new SortedDictionary<int, SortedDictionary<int, double>>();

            for (int i = 0; i < mYears.Count; i++)
                dictYear.Add(mYears[i], new SortedDictionary<int, double>());

            mCommand.Connection = connection;
            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    // DBNull Werte sind ausgeschlossen
                    DateTime date = rdr.GetDateTime(0);
                    double value = Convert.ToDouble(rdr[1]);
                    int year = date.Year;

                    int week;
                    EuropeanIsoWeek.GetWeek(date, out year, out week);

                    SortedDictionary<int, double> dictWeek;

                    // Die Wo des Datums kann auch in ein Jahr fallen,
                    // das nicht in mYears enthalten ist
                    if (!dictYear.TryGetValue(year, out dictWeek))
                        continue;

                    if (!dictWeek.ContainsKey(week))
                        dictWeek.Add(week, value);
                    else
                        dictWeek[week] += value;
                }
            }

            int weeks = GetMaxWeeksOfYears();
            BarRangeRowList rowList = new BarRangeRowList(weeks, dictYear.Count);

            //for (int i = 1; i <= weeks; i++)
            //rowList.RangeCaptions[i - 1] = i.ToString("0") + ". Wo";
            rowList.RangeCaptions[0] = "Woche";

            for (int i = dictYear.Count - 1; i >= 0; i--)
            {
                int year = dictYear.ElementAt(i).Key;
                SortedDictionary<int, double> dictWeek = dictYear.ElementAt(i).Value;

                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[weeks]);

                for (int j = 0; j < dictWeek.Count; j++)
                {
                    int week = dictWeek.ElementAt(j).Key;
                    double value = dictWeek.ElementAt(j).Value;
                    row.y[week - 1] = value;
                }

                rowList.Add(row);
            }
            return rowList;
        }

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Woche_Umsatz_pro_Leistung(SqlConnection connection)
        {
            SortedDictionary<int, SortedDictionary<int, ValuePair<HashSet<string>, double>>> dictYear =
                new SortedDictionary<int, SortedDictionary<int, ValuePair<HashSet<string>, double>>>();

            for (int i = 0; i < mYears.Count; i++)
                dictYear.Add(mYears[i], new SortedDictionary<int, ValuePair<HashSet<string>, double>>());

            mCommand.Connection = connection;
            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    // DBNull Werte sind ausgeschlossen
                    DateTime date = rdr.GetDateTime(0);
                    string code = rdr.GetString(1);
                    double sum = Convert.ToDouble(rdr[2]);
                    int year = date.Year;

                    int week;
                    EuropeanIsoWeek.GetWeek(date, out year, out week);

                    SortedDictionary<int, ValuePair<HashSet<string>, double>> dictWeek;

                    // Die Wo des Datums kann auch in ein Jahr fallen,
                    // das nicht in mYears enthalten ist
                    if (!dictYear.TryGetValue(year, out dictWeek))
                        continue;

                    if (!dictWeek.ContainsKey(week))
                    {
                        HashSet<string> set = new HashSet<string>();
                        set.Add(code);
                        dictWeek.Add(week, new ValuePair<HashSet<string>, double>(set, sum));
                    }
                    else
                    {
                        ValuePair<HashSet<string>, double> pair = dictWeek[week];
                        pair.Value1.Add(code);
                        pair.Value2 += sum;
                    }
                }
            }

            int weeks = GetMaxWeeksOfYears();
            BarRangeRowList rowList = new BarRangeRowList(weeks, dictYear.Count);

            //for (int i = 1; i <= weeks; i++)
            //    rowList.RangeCaptions[i - 1] = i.ToString("0") + ". Wo";
            rowList.RangeCaptions[0] = "Woche";


            for (int i = dictYear.Count - 1; i >= 0; i--)
            {
                int year = dictYear.ElementAt(i).Key;
                SortedDictionary<int, ValuePair<HashSet<string>, double>> dictWeek = dictYear.ElementAt(i).Value;

                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[weeks]);

                for (int j = 0; j < dictWeek.Count; j++)
                {
                    int week = dictWeek.ElementAt(j).Key;
                    ValuePair<HashSet<string>, double> pair = dictWeek.ElementAt(j).Value;
                    row.y[week - 1] = pair.Value2 / pair.Value1.Count;
                }

                rowList.Add(row);
            }
            return rowList;
        }

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Woche_Patient_Umsatz_Leistungen(SqlConnection connection)
        {
            SortedDictionary<int, SortedDictionary<int, ValuePair<HashSet<int>, double>>> dictYear =
                new SortedDictionary<int, SortedDictionary<int, ValuePair<HashSet<int>, double>>>();

            for (int i = 0; i < mYears.Count; i++)
                dictYear.Add(mYears[i], new SortedDictionary<int, ValuePair<HashSet<int>, double>>());

            mCommand.Connection = connection;
            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    // DBNull Werte sind ausgeschlossen
                    DateTime date = rdr.GetDateTime(0);
                    int patNr = rdr.GetInt32(1);
                    double sum = Convert.ToDouble(rdr[2]);
                    int year = date.Year;

                    int week;
                    EuropeanIsoWeek.GetWeek(date, out year, out week);

                    SortedDictionary<int, ValuePair<HashSet<int>, double>> dictWeek;

                    // Die Wo des Datums kann auch in ein Jahr fallen,
                    // das nicht in mYears enthalten ist
                    if (!dictYear.TryGetValue(year, out dictWeek))
                        continue;

                    if (!dictWeek.ContainsKey(week))
                    {
                        HashSet<int> set = new HashSet<int>();
                        set.Add(patNr);
                        dictWeek.Add(week, new ValuePair<HashSet<int>, double>(set, sum));
                    }
                    else
                    {
                        ValuePair<HashSet<int>, double> pair = dictWeek[week];
                        pair.Value1.Add(patNr);
                        pair.Value2 += sum;
                    }
                }
            }

            int weeks = GetMaxWeeksOfYears();
            BarRangeRowList rowList = new BarRangeRowList(weeks, dictYear.Count);

            //for (int i = 1; i <= weeks; i++)
            //    rowList.RangeCaptions[i - 1] = i.ToString("0") + ". Wo";
            rowList.RangeCaptions[0] = "Woche";

            for (int i = dictYear.Count - 1; i >= 0; i--)
            {
                int year = dictYear.ElementAt(i).Key;
                SortedDictionary<int, ValuePair<HashSet<int>, double>> dictWeek = dictYear.ElementAt(i).Value;

                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[weeks]);

                for (int j = 0; j < dictWeek.Count; j++)
                {
                    int week = dictWeek.ElementAt(j).Key;
                    ValuePair<HashSet<int>, double> pair = dictWeek.ElementAt(j).Value;
                    row.y[week - 1] = pair.Value2 / pair.Value1.Count;
                }

                rowList.Add(row);
            }
            return rowList;
        }

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Woche_Patient_Anzahl(SqlConnection connection)
        {
            SortedDictionary<int, SortedDictionary<int, HashSet<int>>> dictYear =
                new SortedDictionary<int, SortedDictionary<int, HashSet<int>>>();

            for (int i = 0; i < mYears.Count; i++)
                dictYear.Add(mYears[i], new SortedDictionary<int, HashSet<int>>());

            mCommand.Connection = connection;
            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    // DBNull Werte sind ausgeschlossen
                    DateTime date = rdr.GetDateTime(0);
                    int patNr = rdr.GetInt32(1);
                    int year = date.Year;

                    int week;
                    EuropeanIsoWeek.GetWeek(date, out year, out week);

                    SortedDictionary<int, HashSet<int>> dictWeek;

                    // Die Wo des Datums kann auch in ein Jahr fallen,
                    // das nicht in mYears enthalten ist
                    if (!dictYear.TryGetValue(year, out dictWeek))
                        continue;

                    if (!dictWeek.ContainsKey(week))
                    {
                        HashSet<int> set = new HashSet<int>();
                        set.Add(patNr);
                        dictWeek.Add(week, set);
                    }
                    else
                        dictWeek[week].Add(patNr);
                }
            }

            int weeks = GetMaxWeeksOfYears();
            BarRangeRowList rowList = new BarRangeRowList(weeks, dictYear.Count);

            //for (int i = 1; i <= weeks; i++)
            //    rowList.RangeCaptions[i - 1] = i.ToString("0") + ". Wo";
            rowList.RangeCaptions[0] = "Woche";


            for (int i = dictYear.Count - 1; i >= 0; i--)
            {
                int year = dictYear.ElementAt(i).Key;
                SortedDictionary<int, HashSet<int>> dictWeek = dictYear.ElementAt(i).Value;

                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[weeks]);

                for (int j = 0; j < dictWeek.Count; j++)
                {
                    int week = dictWeek.ElementAt(j).Key;
                    row.y[week - 1] = dictWeek.ElementAt(j).Value.Count;
                }

                rowList.Add(row);
            }
            return rowList;
        }

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Woche_Tag_Umsatz_Leistungen(SqlConnection connection)
        {
            SortedDictionary<int, SortedDictionary<int, ValuePair<HashSet<DateTime>, double>>> dictYear =
                new SortedDictionary<int, SortedDictionary<int, ValuePair<HashSet<DateTime>, double>>>();

            for (int i = 0; i < mYears.Count; i++)
                dictYear.Add(mYears[i], new SortedDictionary<int, ValuePair<HashSet<DateTime>, double>>());

            mCommand.Connection = connection;
            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    // DBNull Werte sind ausgeschlossen
                    DateTime date = rdr.GetDateTime(0);
                    double value = Convert.ToDouble(rdr[1]);
                    int year = date.Year;

                    int week;
                    EuropeanIsoWeek.GetWeek(date, out year, out week);

                    SortedDictionary<int, ValuePair<HashSet<DateTime>, double>> dictWeek;

                    // Die Wo des Datums kann auch in ein Jahr fallen,
                    // das nicht in mYears enthalten ist
                    if (!dictYear.TryGetValue(year, out dictWeek))
                        continue;

                    if (!dictWeek.ContainsKey(week))
                    {
                        HashSet<DateTime> set = new HashSet<DateTime>();
                        set.Add(date);
                        dictWeek.Add(week, new ValuePair<HashSet<DateTime>, double>(set, value));
                    }
                    else
                    {
                        ValuePair<HashSet<DateTime>, double> pair = dictWeek[week];
                        pair.Value1.Add(date);
                        pair.Value2 += value;
                    }
                }
            }

            int weeks = GetMaxWeeksOfYears();
            BarRangeRowList rowList = new BarRangeRowList(weeks, dictYear.Count);

            //for (int i = 1; i <= weeks; i++)
            //    rowList.RangeCaptions[i - 1] = i.ToString("0") + ". Wo";
            rowList.RangeCaptions[0] = "Woche";


            for (int i = dictYear.Count - 1; i >= 0; i--)
            {
                int year = dictYear.ElementAt(i).Key;
                SortedDictionary<int, ValuePair<HashSet<DateTime>, double>> dictWeek = dictYear.ElementAt(i).Value;

                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[weeks]);

                for (int j = 0; j < dictWeek.Count; j++)
                {
                    int week = dictWeek.ElementAt(j).Key;
                    ValuePair<HashSet<DateTime>, double> pair = dictWeek.ElementAt(j).Value;
                    int count = pair.Value1.Count;
                    double value = pair.Value2;
                    row.y[week - 1] = value / count;
                }

                rowList.Add(row);
            }
            return rowList;
        }

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Woche_Tag_Anzahl(SqlConnection connection)
        {
            SortedDictionary<int, SortedDictionary<int, int>> dictYear = new SortedDictionary<int, SortedDictionary<int, int>>();

            for (int i = 0; i < mYears.Count; i++)
                dictYear.Add(mYears[i], new SortedDictionary<int, int>());
            
            mCommand.Connection = connection;
            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    // DBNull Werte sind ausgeschlossen
                    DateTime date = rdr.GetDateTime(0);
                    int year = date.Year;

                    int week;
                    EuropeanIsoWeek.GetWeek(date, out year, out week);

                    SortedDictionary<int, int> dictWeek;

                    // Die Wo des Datums kann auch in ein Jahr fallen,
                    // das nicht in mYears enthalten ist
                    if (!dictYear.TryGetValue(year, out dictWeek))
                        continue;

                    if (!dictWeek.ContainsKey(week))
                        dictWeek.Add(week, 1);
                    else
                        dictWeek[week] += 1;
                }
            }

            int weeks = GetMaxWeeksOfYears();
            BarRangeRowList rowList = new BarRangeRowList(weeks, dictYear.Count);

            //for (int i = 1; i <= weeks; i++)
            //    rowList.RangeCaptions[i - 1] = i.ToString("0") + ". Wo";
            rowList.RangeCaptions[0] = "Woche";


            for (int i = dictYear.Count - 1; i >= 0; i--)
            {
                int year = dictYear.ElementAt(i).Key;
                SortedDictionary<int, int> dictWeek = dictYear.ElementAt(i).Value;

                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[weeks]);

                for (int j = 0; j < dictWeek.Count; j++)
                {
                    int week = dictWeek.ElementAt(j).Key;
                    double value = dictWeek.ElementAt(j).Value;
                    row.y[week - 1] = value;
                }

                rowList.Add(row);
            }
            return rowList;
        }
        //

        private async Task<BarRangeRowList> TryGetRangeRowList_Periode_Wochentag(SqlConnection connection)
        {
            SortedDictionary<int, SortedDictionary<int, double>> dictYear =
                new SortedDictionary<int, SortedDictionary<int, double>>();

            for (int i = 0; i < mYears.Count; i++)
                dictYear.Add(mYears[i], new SortedDictionary<int, double>());

            mCommand.Connection = connection;
            using (SqlDataReader rdr = await mCommand.ExecuteReaderAsync())
            {
                while (rdr.Read())
                {
                    // DBNull Werte sind ausgeschlossen
                    int year = rdr.GetInt32(0);
                    int weekDay = rdr.GetInt32(1);
                    double value = Convert.ToDouble(rdr[2]);

                    SortedDictionary<int, double> dictWeek;

                    // Das darf nie passieren, da alle Jahre in mYears als
                    // Parameter in der Where Klausel der SQL Anweisung gesetzt sind
                    if (!dictYear.TryGetValue(year, out dictWeek))
                        continue;

                    if (!dictWeek.ContainsKey(weekDay))
                        dictWeek.Add(weekDay, value);
                }
            }

            BarRangeRowList rowList = new BarRangeRowList(7, dictYear.Count);
            
            
            //for (int i = 0; i < rowList.RangeCount; i++)
            //    rowList.RangeCaptions[i] = monday.AddDays(i).ToString("ddd", mCulture) + " Ø";
            rowList.RangeCaptions[0] = "Wochentag";


            for (int i = dictYear.Count - 1; i >= 0; i--)
            {
                int year = dictYear.ElementAt(i).Key;
                SortedDictionary<int, double> dictWeek = dictYear.ElementAt(i).Value;

                BarRangeRow row = new BarRangeRow(year.ToString("0000"), new double?[rowList.RangeCount]);

                for (int j = 0; j < dictWeek.Count; j++)
                {
                    int weekDay = dictWeek.ElementAt(j).Key;
                    row.y[weekDay - 1] = dictWeek.ElementAt(j).Value;
                }

                rowList.Add(row);
            }
            return rowList;
        }
        //
        #endregion

        #region Private Methoden: SetCommandText_Periode_Jahr_XXX
        //
        private void SetCommandText_Periode_Jahr_Umsatz_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,l.dat),SUM(ls.preis)FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b " +
                "ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere +
                " GROUP BY DATEPART(yy,l.dat)ORDER BY DATEPART(yy,l.dat)DESC";
        }
        private void SetCommandText_Periode_Jahr_Umsatz_pro_Leistung(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,AVG(aggr)FROM(SELECT DATEPART(yy,l.dat)yy,SUM(ls.preis)aggr " +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat)," +
                "ls.std_code)TBL GROUP BY yy ORDER BY yy DESC";
        }
        private void SetCommandText_Periode_Jahr_Anzahl_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,l.dat),COUNT(ls.preis)FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b " +
                "ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere +
                " GROUP BY DATEPART(yy,l.dat)ORDER BY DATEPART(yy,l.dat)DESC";
        }
        private void SetCommandText_Periode_Jahr_Umsatz_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,AVG(aggr)FROM(SELECT DATEPART(yy,l.dat)yy,SUM(ls.preis)aggr FROM " +
                "leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
                "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT " +
                "NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),l.pat_nr)TBL GROUP BY yy ORDER BY yy DESC";
        }
        private void SetCommandText_Periode_Jahr_Leistungen_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,AVG(CONVERT(FLOAT,aggr))FROM(SELECT DATEPART(yy,l.dat)yy,COUNT(ls.preis)aggr FROM " +
                "leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
                "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT " +
                "NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),l.pat_nr)TBL GROUP BY yy ORDER BY yy DESC";

        }
        private void SetCommandText_Periode_Jahr_Anzahl_der_Patienten(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,COUNT(pat_nr)FROM (SELECT DATEPART(yy,l.dat)yy,l.pat_nr FROM leistamm " +
                "ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT " +
                "OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT " +
                "NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),pat_nr)TBL GROUP BY yy ORDER BY yy DESC";
        }
        private void SetCommandText_Periode_Jahr_Umsatz_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,dat),SUM(aggr)/COUNT(dat)FROM(SELECT l.dat," +
                "SUM(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=" +
                "l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON " +
                "l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere +
                " GROUP BY l.dat)TBL GROUP BY DATEPART(yy,dat)ORDER BY DATEPART(yy,dat)DESC";
        }
        private void SetCommandText_Periode_Jahr_Leistungen_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,dat),CONVERT(FLOAT,SUM(aggr))/COUNT(dat)FROM " +
                "(SELECT l.dat,COUNT(ls.std_code)aggr FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b " +
                "ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere +
                " GROUP BY l.dat)TBL GROUP BY DATEPART(yy,dat)ORDER BY DATEPART(yy,dat)DESC";
        }
        private void SetCommandText_Periode_Jahr_Anzahl_der_Ordinationstage(ref string sqlWhere)
        {
            mCommand.CommandText =
               "SELECT DATEPART(yy,dat),COUNT(dat)FROM(SELECT l.dat,COUNT(l.dat)aggr FROM " +
               "leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
               "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS " +
               "NOT NULL " + sqlWhere + " GROUP BY l.dat)TBL GROUP BY DATEPART(yy,dat)" +
               "ORDER BY DATEPART(yy,dat)DESC";
        }
        //
        #endregion

        #region Private Methoden: SetCommandText_Periode_Quartal_XXX
        //
        private void SetCommandText_Periode_Quartal_Umsatz_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,l.dat),DATEPART(q,l.dat),SUM(ls.preis)FROM leistamm ls " +
                "INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
                "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat " +
                "IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),DATEPART(q,l.dat)" +
                "ORDER BY DATEPART(yy,l.dat)DESC,DATEPART(q,l.dat) ASC";
        }
        private void SetCommandText_Periode_Quartal_Umsatz_pro_Leistung(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,q,AVG(aggr)FROM(SELECT DATEPART(yy,l.dat)yy,DATEPART(q,l.dat)q," +
                "SUM(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code " +
                "AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat)," +
                "DATEPART(q,l.dat),ls.std_code)TBL GROUP BY yy,q ORDER BY yy DESC,q ASC";
        }
        private void SetCommandText_Periode_Quartal_Anzahl_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,l.dat),DATEPART(q,l.dat),COUNT(ls.preis)FROM leistamm ls " +
                "INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
                "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS " +
                "NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),DATEPART(q,l.dat)" +
                "ORDER BY DATEPART(yy,l.dat)DESC,DATEPART(q,l.dat)ASC";
        }
        private void SetCommandText_Periode_Quartal_Umsatz_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,q,AVG(aggr)FROM(SELECT DATEPART(yy,l.dat)yy,DATEPART(q,l.dat)q," +
                "SUM(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code " +
                "AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat)," +
                "DATEPART(q,l.dat),l.pat_nr)TBL GROUP BY yy,q ORDER BY yy DESC,q ASC";
        }
        private void SetCommandText_Periode_Quartal_Leistungen_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
              "SELECT yy,q,AVG(CONVERT(FLOAT,aggr))FROM(SELECT DATEPART(yy,l.dat)yy,DATEPART(q,l.dat)q," +
              "COUNT(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code " +
              "AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
              "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat)," +
              "DATEPART(q,l.dat),l.pat_nr)TBL GROUP BY yy,q ORDER BY yy DESC,q ASC";
        }
        private void SetCommandText_Periode_Quartal_Anzahl_der_Patienten(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,q,COUNT(pat_nr)FROM (SELECT DATEPART(yy,l.dat)yy,DATEPART(q,l.dat)q," +
                "l.pat_nr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 " +
                "AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),DATEPART(q,l.dat)," +
                "pat_nr)TBL GROUP BY yy,q ORDER BY yy DESC,q ASC";
        }
        private void SetCommandText_Periode_Quartal_Umsatz_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,dat),DATEPART(q,dat),CONVERT(FLOAT,SUM(aggr))/COUNT(dat)FROM " +
                "(SELECT l.dat,SUM(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b " +
                "ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere +
                " GROUP BY l.dat)TBL GROUP BY DATEPART(yy,dat),DATEPART(q,dat)" +
                "ORDER BY DATEPART(yy,dat)DESC,DATEPART(q,dat)ASC";
        }
        private void SetCommandText_Periode_Quartal_Leistungen_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,dat),DATEPART(q,dat),CONVERT(FLOAT,SUM(aggr))/COUNT(dat)FROM " +
                "(SELECT l.dat,COUNT(ls.std_code)aggr FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b " +
                "ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY " +
                "l.dat)TBL GROUP BY DATEPART(yy,dat),DATEPART(q,dat)ORDER BY " +
                "DATEPART(yy,dat)DESC,DATEPART(q,dat)ASC";
        }
        private void SetCommandText_Periode_Quartal_Anzahl_der_Ordinationstage(ref string sqlWhere)
        {
            mCommand.CommandText =
               "SELECT DATEPART(yy,dat),DATEPART(q,dat),COUNT(dat)FROM(SELECT l.dat,COUNT(l.dat)" +
               "aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=" +
               "l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS " +
               "NOT NULL " + sqlWhere + " GROUP BY l.dat)TBL GROUP BY DATEPART(yy,dat),DATEPART(q,dat)" +
               "ORDER BY DATEPART(yy,dat)DESC,DATEPART(q,dat)ASC";

            //mCommand.CommandText = // !!!!!!!!!!
            //    "SELECT DATEPART(yy,l.dat),DATEPART(q,l.dat)," +
            //    "COUNT(DISTINCT(l.dat)) Gesamt " +
            //    "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
            //    "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id " +
            //    "WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere +
            //    " GROUP BY DATEPART(yy,l.dat),DATEPART(q,l.dat) ORDER BY DATEPART(yy,l.dat) DESC,DATEPART(q,l.dat)ASC";
        }
        //
        #endregion

        #region Private Methoden: SetCommandText_Periode_Monat_XXX
        //
        private void SetCommandText_Periode_Monat_Umsatz_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,l.dat),DATEPART(mm,l.dat),SUM(ls.preis)FROM leistamm ls " +
                "INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
                "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat " +
                "IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),DATEPART(mm,l.dat)" +
                "ORDER BY DATEPART(yy,l.dat)DESC,DATEPART(mm,l.dat) ASC";
        }
        private void SetCommandText_Periode_Monat_Umsatz_pro_Leistung(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,mm,AVG(aggr)FROM(SELECT DATEPART(yy,l.dat)yy,DATEPART(mm,l.dat)mm," +
                "SUM(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code " +
                "AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat)," +
                "DATEPART(mm,l.dat),ls.std_code)TBL GROUP BY yy,mm ORDER BY yy DESC,mm ASC";
        }
        private void SetCommandText_Periode_Monat_Anzahl_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,l.dat),DATEPART(mm,l.dat),COUNT(ls.preis)FROM leistamm ls " +
                "INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
                "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS " +
                "NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),DATEPART(mm,l.dat)" +
                "ORDER BY DATEPART(yy,l.dat)DESC,DATEPART(mm,l.dat)ASC";
        }
        private void SetCommandText_Periode_Monat_Umsatz_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,mm,AVG(aggr)FROM(SELECT DATEPART(yy,l.dat)yy,DATEPART(mm,l.dat)mm," +
                "SUM(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code " +
                "AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat)," +
                "DATEPART(mm,l.dat),l.pat_nr)TBL GROUP BY yy,mm ORDER BY yy DESC,mm ASC";
        }
        private void SetCommandText_Periode_Monat_Leistungen_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
            "SELECT yy,mm,AVG(CONVERT(FLOAT,aggr))FROM(SELECT DATEPART(yy,l.dat)yy,DATEPART(mm,l.dat)mm," +
            "COUNT(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code " +
            "AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
            "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat)," +
            "DATEPART(mm,l.dat),l.pat_nr)TBL GROUP BY yy,mm ORDER BY yy DESC,mm ASC";
        }
        private void SetCommandText_Periode_Monat_Anzahl_der_Patienten(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT yy,mm,COUNT(pat_nr)FROM (SELECT DATEPART(yy,l.dat)yy,DATEPART(mm,l.dat)mm," +
                "l.pat_nr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 " +
                "AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),DATEPART(mm,l.dat)," +
                "pat_nr)TBL GROUP BY yy,mm ORDER BY yy DESC,mm ASC";
        }
        private void SetCommandText_Periode_Monat_Umsatz_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,dat),DATEPART(mm,dat),CONVERT(FLOAT,SUM(aggr))/COUNT(dat)FROM " +
                "(SELECT l.dat,SUM(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b " +
                "ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere +
                " GROUP BY l.dat)TBL GROUP BY DATEPART(yy,dat),DATEPART(mm,dat)" +
                "ORDER BY DATEPART(yy,dat)DESC,DATEPART(mm,dat)ASC";
        }
        private void SetCommandText_Periode_Monat_Leistungen_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DATEPART(yy,dat),DATEPART(mm,dat),CONVERT(FLOAT,SUM(aggr))/COUNT(dat)FROM " +
                "(SELECT l.dat,COUNT(ls.std_code)aggr FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b " +
                "ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY " +
                "l.dat)TBL GROUP BY DATEPART(yy,dat),DATEPART(mm,dat)ORDER BY " +
                "DATEPART(yy,dat)DESC,DATEPART(mm,dat)ASC";
        }
        private void SetCommandText_Periode_Monat_Anzahl_der_Ordinationstage(ref string sqlWhere)
        {
            mCommand.CommandText =
               "SELECT DATEPART(yy,dat),DATEPART(mm,dat),COUNT(dat)FROM(SELECT l.dat,COUNT(l.dat)" +
               "aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=" +
               "l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS " +
               "NOT NULL " + sqlWhere + " GROUP BY l.dat)TBL GROUP BY DATEPART(yy,dat),DATEPART(mm,dat)" +
               "ORDER BY DATEPART(yy,dat)DESC,DATEPART(mm,dat)ASC";
        }
        //
        #endregion

        #region Private Methoden: SetCommandText_Periode_Woche_XXX
        //
        private void SetCommandText_Periode_Woche_Umsatz_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT l.dat,SUM(ls.preis)" +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat";
        }
        private void SetCommandText_Periode_Woche_Umsatz_pro_Leistung(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT l.dat,ls.std_code,SUM(ls.preis)" +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL AND ls.std_code IS NOT NULL "
                + sqlWhere + " GROUP BY l.dat,ls.std_code";
        }
        private void SetCommandText_Periode_Woche_Anzahl_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT l.dat,COUNT(ls.std_code)" +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat";
        }
        private void SetCommandText_Periode_Woche_Umsatz_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT l.dat,l.pat_nr,SUM(ls.preis)" +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat,l.pat_nr";
        }
        private void SetCommandText_Periode_Woche_Leistungen_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT l.dat,l.pat_nr,COUNT(ls.preis)" +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat,l.pat_nr";
        }
        private void SetCommandText_Periode_Woche_Anzahl_der_Patienten(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT l.dat,l.pat_nr " +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat,l.pat_nr";
        }
        private void SetCommandText_Periode_Woche_Umsatz_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT l.dat,SUM(ls.preis)" +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat";
        }
        private void SetCommandText_Periode_Woche_Leistungen_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT l.dat,COUNT(ls.preis)" +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat";
        }
        private void SetCommandText_Periode_Woche_Anzahl_der_Ordinationstage(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SELECT DISTINCT(l.dat)" +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere;
        }
        //
        #endregion

        #region Private Methoden: SetCommandText_Periode_Wochentag_XXX
        //
        private void SetCommandText_Periode_Wochentag_Umsatz_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SET DATEFIRST 1;" +
                "SELECT DATEPART(yy,l.dat),DATEPART(dw,l.dat),SUM(ls.preis)FROM leistamm " +
                "ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
                "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat " +
                "IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),DATEPART(dw,l.dat)";
        }
        private void SetCommandText_Periode_Wochentag_Umsatz_pro_Leistung(ref string sqlWhere)
        {
            mCommand.CommandText =
               "SET DATEFIRST 1;" +
               "SELECT DATEPART(yy,dat),DATEPART(dw,dat),AVG(aggr)FROM(SELECT l.dat,SUM(ls.preis)" +
               "aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=" +
               "l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat " +
               "IS NOT NULL " + sqlWhere + " GROUP BY l.dat,ls.std_code)TBL GROUP BY DATEPART(yy,dat)," +
               "DATEPART(dw,dat)";
        }
        private void SetCommandText_Periode_Wochentag_Anzahl_der_Leistungen(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SET DATEFIRST 1;" +
                "SELECT yy,dw,CONVERT(FLOAT,SUM(aggr))/COUNT(aggr)FROM (SELECT DATEPART(yy,l.dat)" +
                "yy,DATEPART(dw,l.dat)dw,COUNT(l.dat)aggr FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON " +
                "l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP " +
                "BY l.dat)TBL GROUP BY yy,dw";
        }
        private void SetCommandText_Periode_Wochentag_Umsatz_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
               "SET DATEFIRST 1;" +
               "SELECT DATEPART(yy,dat),DATEPART(dw,dat),AVG(aggr)FROM(SELECT l.dat,SUM(ls.preis)aggr " +
               "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=" +
               "l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT " +
               "NULL " + sqlWhere + " GROUP BY l.dat,l.pat_nr)TBL GROUP BY DATEPART(yy,dat),DATEPART(dw,dat)";
        }
        private void SetCommandText_Periode_Wochentag_Leistungen_pro_Patient(ref string sqlWhere)
        {
            mCommand.CommandText =
               "SET DATEFIRST 1;" +
               "SELECT DATEPART(yy,dat),DATEPART(dw,dat),AVG(CONVERT(FLOAT,aggr))FROM(SELECT l.dat," +
               "COUNT(ls.preis)aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code " +
               "AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 " +
               "AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat,l.pat_nr)TBL " +
               "GROUP BY DATEPART(yy,dat),DATEPART(dw,dat)";
        }
        private void SetCommandText_Periode_Wochentag_Anzahl_der_Patienten(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SET DATEFIRST 1;" +
                "SELECT DATEPART(yy,dat),DATEPART(dw,dat),AVG(CONVERT(FLOAT,aggr))FROM(SELECT l.dat," +
                "COUNT(DISTINCT(l.pat_nr))aggr FROM leistamm ls INNER JOIN leistung l ON ls.std_code=" +
                "l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id " +
                "WHERE ls.preis>0AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat)TBL GROUP BY " +
                "DATEPART(yy,dat),DATEPART(dw,dat)";
        }
        private void SetCommandText_Periode_Wochentag_Umsatz_pro_Ordinationstag(ref string sqlWhere)
        {
            //mCommand.CommandText = "SET DATEFIRST 1;" +
            //    "SELECT DATEPART(yy,AU_FAHRT_TEILBEREICH.Fahrt_teilbereich_beladung_datum)," +
            //    "DATEPART(dw,AU_FAHRT_TEILBEREICH.Fahrt_teilbereich_beladung_datum)," +
            //    "SUM(AU_FAHRT_TEILBEREICH_PRODUKT.Fahrt_teilbereich_produkt_preis_pro_ladeeinheit_in_FREMDWAEHRUNG)/COUNT(DISTINCT(AU_FAHRT_TEILBEREICH.Fahrt_teilbereich_nummer))" +
            //    "FROM AU_FAHRT_TEILBEREICH INNER JOIN AU_FAHRT_TEILBEREICH_PRODUKT ON AU_FAHRT_TEILBEREICH.Fahrt_teilbereich_nummer=" +
            //    "AU_FAHRT_TEILBEREICH_PRODUKT.Fahrt_teilbereich_produkt_fahrt_nummer WHERE " +
            //    "AU_FAHRT_TEILBEREICH_PRODUKT.Fahrt_teilbereich_produkt_preis_pro_ladeeinheit_in_FREMDWAEHRUNG > 0 AND " +
            //    "AU_FAHRT_TEILBEREICH.Fahrt_teilbereich_beladung_datum IS NOT NULL " +
            //    "GROUP BY DATEPART(yy,AU_FAHRT_TEILBEREICH.Fahrt_teilbereich_beladung_datum)," +
            //    "DATEPART(dw,AU_FAHRT_TEILBEREICH.Fahrt_teilbereich_beladung_datum)";

            mCommand.CommandText =
                "SET DATEFIRST 1;" +
                "SELECT yy,dw,CONVERT(FLOAT,SUM(aggr2))/COUNT(aggr1)FROM " +
                "(SELECT DATEPART(yy,l.dat)yy,DATEPART(dw,l.dat)dw,COUNT(l.dat)aggr1,SUM(ls.preis)aggr2 " +
                "FROM leistamm ls INNER JOIN leistung l ON ls.std_code=l.std_code AND " +
                "ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE " +
                "ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere + " GROUP BY l.dat)TBL GROUP BY yy,dw";
        }
        private void SetCommandText_Periode_Wochentag_Leistungen_pro_Ordinationstag(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SET DATEFIRST 1;" +
                "SELECT yy,dw,AVG(CONVERT(FLOAT,aggr))FROM(SELECT DATEPART(yy,l.dat)yy," +
                "DATEPART(dw,l.dat)dw,COUNT(l.dat)aggr FROM leistamm ls INNER JOIN leistung l " +
                "ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie LEFT OUTER JOIN Benutzer b " +
                "ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat IS NOT NULL " + sqlWhere +
                " GROUP BY l.dat)TBL GROUP BY yy,dw";
        }
        private void SetCommandText_Periode_Wochentag_Anzahl_der_Ordinationstage(ref string sqlWhere)
        {
            mCommand.CommandText =
                "SET DATEFIRST 1;" +
                "SELECT DATEPART(yy,l.dat),DATEPART(dw,l.dat),1 FROM leistamm ls " +
                "INNER JOIN leistung l ON ls.std_code=l.std_code AND ls.kk_kurz=l.kk_wie " +
                "LEFT OUTER JOIN Benutzer b ON l.BenutzerID=b.Id WHERE ls.preis>0 AND l.dat " +
                "IS NOT NULL " + sqlWhere + " GROUP BY DATEPART(yy,l.dat),DATEPART(dw,l.dat)";
        }
        //
        #endregion
    }

    public sealed class BarRangeRow
    {
        #region private Felder
        private string mCaption;
        private double?[] mValues;
        #endregion

        #region öffentliche readonly Eigenschaften
        public string label
        {
            get { return mCaption; }
        }
        public double?[] y
        {
            get { return mValues; }
        }
        #endregion

        #region Konstruktor
        public BarRangeRow(string caption, params double?[] values)
        {
            mCaption = caption;
            mValues = values;
        }
        #endregion
    }

    public sealed class BarRangeRowList : IList<BarRangeRow>
    {
        #region private Felder
        private List<BarRangeRow> mList;
        private int mRangeCount;
        private string[] mRangeCaptions;
        private bool mLastRangeTableMirrored = false;
        #endregion

        #region öffentliche readonly Eigenschaften
        public int RangeCount
        {
            get { return mRangeCount; }
        }
        public string[] RangeCaptions
        {
            get { return mRangeCaptions; }
            set { mRangeCaptions = value; }
        }
        public bool LastRangeTableMirrored
        {
            get { return mLastRangeTableMirrored; }
        }
        #endregion

        #region Konstruktor
        /// <summary>
        /// Wenn rangeCount nicht positiv ist, wird eine Aushahme ausgelöst
        /// </summary>
        public BarRangeRowList(int rangeCount, int capacity)
        {
            if (rangeCount < 1)
                throw new ArgumentException(this.GetType().ToString() + ": rangeCount muss eine positive Zahl sein.", "rangeCount");

            mRangeCount = rangeCount;
            mRangeCaptions = new string[rangeCount];
            mList = new List<BarRangeRow>(Math.Max(3, capacity));
        }
        #endregion
        
        #region private Methoden
        /// <summary>
        /// Wenn item null ist, oder wenn die Länge der Eigenschaft Values von item nicht mit mRangeCount übereinstimmt, wird eine Ausnahme ausgelöst
        /// </summary>
        private void CheckRange(BarRangeRow item)
        {
            if (item == null)
                throw new ArgumentNullException("item", this.GetType().ToString() + ": item darf nicht null sein.");

            if (item.y.Length != mRangeCount)
                throw new ArgumentException(this.GetType().ToString() + ": das Array Values muss die Länge " + mRangeCount + " haben.", "item");
        }
        #endregion

        #region IList<BarRangeRow> Member
        public int IndexOf(BarRangeRow item)
        {
            return mList.IndexOf(item);
        }
        /// <summary>
        /// Wenn item null ist, oder wenn die Länge der Eigenschaft Values von item nicht mit RangeCount übereinstimmt, wird eine Ausnahme ausgelöst
        /// </summary>
        public void Insert(int index, BarRangeRow item)
        {
            CheckRange(item);
            mList.Insert(index, item);
        }
        public void RemoveAt(int index)
        {
            mList.RemoveAt(index);
        }
        /// <summary>
        /// Wenn der Wert null ist, oder wenn die Länge der Eigenschaft Values des Wertes nicht mit RangeCount übereinstimmt, wird eine Ausnahme ausgelöst
        /// </summary>
        public BarRangeRow this[int index]
        {
            get
            {
                return mList[index];
            }
            set
            {
                CheckRange(value);
                mList[index] = value;
            }
        }
        #endregion

        #region ICollection<BarRangeRow> Member
        /// <summary>
        /// Wenn item null ist, oder wenn die Länge der Eigenschaft Values von item nicht mit RangeCount übereinstimmt, wird eine Ausnahme ausgelöst
        /// </summary>
        public void Add(BarRangeRow item)
        {
            CheckRange(item);
            mList.Add(item);
        }
        public void Clear()
        {
            mList.Clear();
        }
        public bool Contains(BarRangeRow item)
        {
            return mList.Contains(item);
        }
        public void CopyTo(BarRangeRow[] array, int arrayIndex)
        {
            mList.CopyTo(array, arrayIndex);
        }
        public int Count
        {
            get { return mList.Count; }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public bool Remove(BarRangeRow item)
        {
            return mList.Remove(item);
        }
        #endregion

        #region IEnumerable<BarRangeRow> Member
        public IEnumerator<BarRangeRow> GetEnumerator()
        {
            return mList.GetEnumerator();
        }
        #endregion

        #region IEnumerable Member
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return mList.GetEnumerator();
        }
        #endregion
    }

    public sealed class BarRangeTable
    {
        #region öffentliche readonly Felder
        public readonly int RangeCount;
        public readonly string[] RangeCaptions;
        public readonly BarRangeRow[] RangeRows;
        public readonly int BarsOfRangeCount;
        #endregion

        #region Konstruktor
        public BarRangeTable(string[] rangeCaptions, BarRangeRow[] rangeRows)
        {
            this.RangeCount = rangeCaptions.Length;
            this.RangeCaptions = rangeCaptions;
            this.RangeRows = rangeRows;
            this.BarsOfRangeCount = rangeRows.Length;
        }
        #endregion

        #region öffentliche Methoden
        public IEnumerable<double> GetNumericBarValuesEnumerator()
        {
            foreach (BarRangeRow row in RangeRows)
            {
                foreach (double? value in row.y)
                {
                    if (value != null)
                        yield return value.Value;
                }
            }
        }
        #endregion
    }

    public sealed class ValuePair<T1, T2>
    {
        #region öffentliche Felder
        public T1 Value1;
        public T2 Value2;
        #endregion

        #region Konstruktor
        public ValuePair(T1 value1, T2 value2)
        {
            this.Value1 = value1;
            this.Value2 = value2;
        }
        #endregion
    }

    public static class EuropeanIsoWeek
    {
        /// <summary>
        /// Methoden zur Berechnung der EU-Kalenderwoche nach ISO-Norm 8601.
        /// </summary>

        #region Jahr und Wo zu einem Datum
        public static void GetWeek(DateTime date, out int year, out int week)
        {
            DateTime start = GetWeekStartDate(new DateTime(date.Year, 1, 4));

            if (start > date)
            {
                year = start.Year - 1;
                TryGetWeeksCount(year, out week);
                return;
            }

            if (date.Month == 12 && date.Day > 28)
            {
                if (date.Subtract(GetWeekStartDate(new DateTime(date.Year, 12, 28))).Days < 7)
                {
                    year = date.Year;
                    TryGetWeeksCount(year, out week);
                }
                else
                {
                    year = date.Year + 1;
                    week = 1;
                }
                return;
            }

            year = date.Year;
            week = (date.Subtract(start).Days / 7) + 1;
        }
        internal static void GetWeek_1(DateTime date, out int year, out int week)
        {
            // Erzeugt dasselbe Resultat wie GetWeek, ist eleganter aber deutlich langsamer

            DateTime start = GetWeekStartDate(new DateTime(date.Year, 1, 4));

            if (start > date)
            {
                year = start.Year - 1;
                TryGetWeeksCount(year, out week);
                return;
            }

            DateTime end = GetWeekEndDate(new DateTime(date.Year, 12, 28));

            if (end < date)
            {
                year = date.Year + 1;
                week = 1;
                return;
            }

            year = date.Year;
            week = (date.Subtract(start).Days / 7) + 1;
        }
        internal static void GetWeek_2(DateTime date, out int year, out int week)
        {
            // Erzeugt dasselbe Resultat wie GetWeek, ist aber deutlich langsamer

            double a = Math.Floor((14 - (date.Month)) / 12D);
            double y = date.Year + 4800 - a;
            double m = (date.Month) + (12 * a) - 3;

            double jd = date.Day + Math.Floor(((153 * m) + 2) / 5) +
                (365 * y) + Math.Floor(y / 4) - Math.Floor(y / 100) +
                Math.Floor(y / 400) - 32045;

            double d4 = (jd + 31741 - (jd % 7)) % 146097 % 36524 % 1461;

            double L = Math.Floor(d4 / 1460);
            double d1 = ((d4 - L) % 365) + L;

            week = (int)Math.Floor(d1 / 7) + 1;

            year = date.Year;
            if (week == 1 && date.Month == 12)
                year++;
            if (week >= 52 && date.Month == 1)
                year--;
        }
        #endregion

        #region Anzahl der Kalenderwochen eines Jahres
        public static bool TryGetWeeksCount(int year, out int weeks)
        {
            weeks = 0;
            DateTime start;

            if (!TryGetWeekStartDate(year, 1, out start))
                return false;

            // 28.Dezember liegt immer in der letzen KW (Berechnung: 29.Dezember)
            DateTime end = new DateTime(year, 12, 29);
            weeks = (int)Math.Ceiling((end.Subtract(start).Days / 7D));

            return true;
        }
        #endregion

        #region Start- und Enddatum einer Kalenderwoche
        public static DateTime GetWeekStartDate(DateTime date)
        {
            return date.AddDays(1 - (((int)date.DayOfWeek + 6) % 7 + 1));
        }
        public static DateTime GetWeekEndDate(DateTime date)
        {
            DateTime start = GetWeekStartDate(date);

            if (date.Year == DateTime.MaxValue.Year)
                return start.AddDays(Math.Min(6, new DateTime(date.Year, 12, 31).DayOfYear - start.DayOfYear));
            else
                return start.AddDays(6);
        }
        public static void GetWeekBounds(DateTime date, out DateTime monday, out DateTime end)
        {
            monday = GetWeekStartDate(date);
            end = GetWeekEndDate(date);
        }
        public static bool TryGetWeekStartDate(int year, int week, out DateTime monday)
        {
            monday = new DateTime();

            try
            {
                // 4.Jänner liegt immer in der ersten KW
                monday = GetWeekStartDate(new DateTime(year, 1, 4)).AddDays((week - 1) * 7);
                return true;
            }
            catch { return false; }
        }
        public static bool TryGetWeekEndDate(int year, int week, out DateTime end)
        {
            if (!TryGetWeekStartDate(year, week, out end))
                return false;

            end = GetWeekEndDate(end);
            return true;
        }
        public static bool TryGetWeekBounds(int year, int week, out DateTime monday, out DateTime end)
        {
            end = new DateTime();

            if (!TryGetWeekStartDate(year, week, out monday))
                return false;

            end = GetWeekEndDate(monday);
            return true;
        }
        #endregion

        #region Datum des Wochentages einer Kalenderwoche
        public static bool TryGetWeekDayDate(int year, int week, DayOfWeek weekDay, out DateTime date)
        {
            date = new DateTime();

            try
            {
                if (!TryGetWeekStartDate(year, week, out date))
                    return false;

                date = date.AddDays((int)(weekDay + 6) % 7);
                return true;
            }
            catch { return false; }
        }
        #endregion

        #region Tag innerhalb der Wo (1=Mo,..,7=So)
        public static int GetDayInWeek(DateTime date)
        {
            return (int)(date.DayOfWeek + 6) % 7 + 1;
        }
        #endregion
    }


}

