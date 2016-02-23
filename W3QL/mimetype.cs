using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using webQL;

namespace webQL
{

    public sealed class MimeType
    {

        #region Constructor

        public MimeType()
        {
        }
        public MimeType(bool LoadMimeType)
            : this()
        {

            if (!LoadMimeType) return;

            // load LoadMimeTypes from JSON File
            Exception ex = null;
            this.Load(out ex);
            if (ex != null) throw new Exception("Error loading MimeTypes:" + ex.Message);

        }

        #endregion

        #region Declaration & Methods

        [ThreadStatic]
        public Dictionary<string, string> MimeTypes = new Dictionary<string, string>(128);

        private const string getMimeTypDefault = "application/octet-stream";
        private const string charsetDefault = ";charset=utf-8";
        
        
        
        public string getContentType(string Extension)
        {
            return getMimeType(Extension) + charsetDefault;
        }
        
        public string getMimeType(string Extension)
        {
            // Extension allowed -> Filename.Ext | .ext | ext | Ext | EXT
            // if EXT unknown, then return getMimeTypDefault + charsetDefault
            if (string.IsNullOrWhiteSpace(Extension)) return getMimeTypDefault;

            string value=null;
            // wenn PATH extension != null 
            if (!string.IsNullOrWhiteSpace(Path.GetExtension(Extension))) Extension = Path.GetExtension(Extension);
            Extension = Extension.Replace(".", "").ToLower();
            MimeTypes.TryGetValue(Extension, out value);
            
            if (string.IsNullOrWhiteSpace(value)) return getMimeTypDefault;            
            return value;
        }

        #endregion

        #region Public Load MimeTypes from JSON-File

        private const string FILENAME_WEBQL_MIMETYPES = "webql.mimetypes.json";
        public static string FullPathMimeTypes
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), FILENAME_WEBQL_MIMETYPES);
            }
        }
        public bool Load()
        {
            Exception ex;
            return Load(out ex);
        }
        public bool Load(out Exception ex)
        {
            return Load(FullPathMimeTypes, out ex);
        }
        public bool Load(string fileName, out Exception ex)
        {

            ex = null;
            try
            {

                string json = File.ReadAllText(fileName, Encoding.UTF8);
                json = json.ToLower();

                JsonSerializerSettings s = new JsonSerializerSettings();
                s.MissingMemberHandling = MissingMemberHandling.Ignore;
                s.StringEscapeHandling = StringEscapeHandling.Default;
                this.MimeTypes = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                return true;

            }
            catch (Exception exc)
            {
                LogQL.LogServer("WS.LoadMimeTypes", exc.Message);

                this.MimeTypes = null;
                ex = exc;
                return false;
            }

        }

        #endregion

    }

}
