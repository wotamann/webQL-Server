using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using webQL;


namespace webQL
{
    public sealed class configuration
    {
        #region Constructor

        public configuration()
        {
        }
        //public configuration(bool LoadConfig)
        //    : this()
        //{

        //    if (!LoadConfig) return;

        //     load WebServer Configuration from JSON File
        //    Exception ex = null;
        //    this.Load(out ex);
        //    if (ex != null) LogQL.LogServer("Error", "Server loading Configuration: " + ex.Message);

        //}

        #endregion

        #region Class

        public sealed class ConfigurationParameters
        {

            /// <summary>
            /// List of all URI (Uniform Resource Identifier)-Präfixe - Eine URI-Präfixzeichenfolge besteht aus einem Schema (HTTP oder HTTPS), einem Host, einem optionalen Anschluss und einem optionalen Pfad. 
            /// Um anzugeben, dass der HttpListener alle an einen Anschluss gesendeten Anforderungen akzeptiert, ersetzen Sie entsprechend das Hostelement durch das Zeichen "+": "https://+:8080"
            /// </summary>
            public List<string> Prefixes = new List<string>();

            /// <summary>
            /// 
            /// HTTP.SYS needs Admin-Rights for configuring with -> netsh add UrlACL 
            /// 
            /// or 
            /// 
            /// easier use the Class ServerIntegration
            /// 
            /// Set UrlACL
            /// ServerIntegration.setUrlACL(Server.ServerConfiguration.Prefixes, Server.ServerConfiguration.UrlACLUser);
            /// 
            /// Set SSL
            /// ServerIntegration.setSSLCert(Server.ServerConfiguration.SSLCertIpport, Server.ServerConfiguration.SSLCertHash, Server.ServerConfiguration.SSLCertAppId);            
            /// </summary>
            public string SSLCertIpport;
            public string SSLCertHash;
            public string SSLCertAppId;
            public string UrlACLUser;

            /// <summary>
            /// the TargetPath for the Project, which is served by WebQL Server
            /// </summary>
            public string RootPath;

            /// <summary>
            /// Define your Logging Mode 
            /// default LOG the Request to File   
            /// Option: Log to 'F'ile , 'C'onsole 
            /// Option: Log the re'Q'uest and/or re'S'ponse
            /// </summary>
            public string LogMode;

        }

        #endregion

        #region Declaration

        /// <summary>
        /// get/set the default URL using prots from prefixes[0]
        /// </summary>
        private string defaulturl = null;
        public string DefaultUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(defaulturl)) defaulturl = this.Parameters.Prefixes[0].Replace("*", "localhost").Replace("+", "localhost");
                return defaulturl;
            }
            set
            {
                Uri uriResult;
                if (Uri.TryCreate(value, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp)
                {
                    defaulturl = uriResult.OriginalString;
                }
                else
                {
                    defaulturl = this.Parameters.Prefixes[0].Replace("*", "localhost").Replace("+", "localhost");
                };


            }
        }

        public ConfigurationParameters Parameters = new ConfigurationParameters();

        #endregion

        #region Public Load Configuration   <-> JSON Object

        private string normalizeServerPath(string path, bool extendPath)
        {

            if (string.IsNullOrWhiteSpace(path) == true) return path;
            String delimit = Path.AltDirectorySeparatorChar.ToString();
            if (!Path.IsPathRooted(path) && extendPath) path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), path); 
            return path.EndsWith(delimit, StringComparison.Ordinal) ? path : path + delimit;
        }
        private string normalizeRootPath(string path, bool extendPath)
        {

            if (string.IsNullOrWhiteSpace(path) == true) return path;
            String delimit = Path.AltDirectorySeparatorChar.ToString();

            if (!Path.IsPathRooted(path) && extendPath) path = Server.Configuration.Parameters.RootPath + path;
            return path.EndsWith(delimit, StringComparison.Ordinal) ? path : path + delimit;
        }

        private const string FILENAME_WEBQL_CONFIG = "webql.config.json";
        public static string FullPathConfiguration
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), FILENAME_WEBQL_CONFIG);
            }
        }
               
        public bool Load()
        {
            Exception ex;
            return Load(out ex);
        }
        public bool Load(out Exception ex)
        {
            return Load(FullPathConfiguration, out ex);
        }
        public bool Load(string fileName, out Exception ex)
        {
            ex = null;

            try
            {


                // read defined routes for project
                string json = File.ReadAllText(fileName, Encoding.UTF8);
                json = json.Replace("\\", "/"); // ersetzt \ mit / ;

                // JSON serilaizer Settings
                JsonSerializerSettings s = new JsonSerializerSettings();
                s.MissingMemberHandling = MissingMemberHandling.Ignore;
                s.StringEscapeHandling = StringEscapeHandling.Default;

                this.Parameters = JsonConvert.DeserializeObject<ConfigurationParameters>(json);

                // if ROOTPATH in Config is NOT(!) rooted then add SERVER PATH
                this.Parameters.RootPath = normalizeServerPath(this.Parameters.RootPath, true);


                return true;

            }
            catch (Exception exc)
            {
                LogQL.LogServer("ERR", "Load Configuration: " + exc.Message);
                Server.Configuration = null;
                ex = exc;
                return false;
            }
        }

        #endregion
    }

}
