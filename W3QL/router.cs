using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;


namespace webQL
{

    public sealed class Router
    {
                
        public sealed class Route
        {
            public string Name { get; set; }

            public string Action { get; set; }
            public Type ActionType { get; set; }
            public MethodInfo ActionMethodInfo { get; set; }

            public string UrlSegment { get; set; }
            public Regex UrlSegmentRegex { get; set; }
            public Regex UrlRightSegmentRegex { get; set; }
            public MatchCollection UrlSegmentMatches { get; set; }
                
            /* 
             
            Route-Configuration File
              
               "FileHandlerJS":    -> a remarkable Name descriping the Route
                {
                  "Action":"Modul_File",        // -> calls the Method Action in a Class with Attribute '[ModulName("Modul_File")]'
                  "UrlSegment":"/js/*.js",      //-> URL used for matching with Request URL
                  
                  "Path":"C:/Data/"
                                
                },
             
            Writing your Modul, you can access this Properties in the Code of 
            your Class with Attribute '[ModulName("Modul_File")]' 
              
                [ModulName("Modul_File")]
                public class Modul_File : IModul 
                {
                    ...
              
                    public async Task<ModulResult> Action()
                    {     
                        // here you access the Properties from the Route-Configuration 
                        string currentPath = ResponseInfo.CurrentRoute["Path"];  
                    ....
                    }
                ...
                }
              
              
            
             */
            public Dictionary<string, string> Items = new Dictionary<string, string>(16);
            public string this[string i]
            {
                get
                {
                    string itemName = null;
                    Items.TryGetValue(i,out itemName);
                    return itemName;

                }
                set
                {
                    Items[i] = value;
                }
            }                             
            
        }

        #region Constructor

        public Router()
        {
        }
        public Router(bool LoadRoutes): this()
        {
            if (!LoadRoutes) return;

            // load WebServer Configuration from JSON File
            Exception ex = null;
            this.Load(out ex);
            if (ex != null) throw new Exception("Error loading Routes: " + ex.Message);

        }

        #endregion

        #region Declaration

        public static List<Route> Routes;

        #endregion

        #region Load Routes   

        private const string FILENAME_WEBQL_ROUTES = "webql.routes.json";
        public string FullPathRoutes
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), FILENAME_WEBQL_ROUTES);
            }
        }

        public bool Load()
        {
            Exception ex;
            return Load(out ex);
        }
        public bool Load(out Exception ex)
        {
            return Load(FullPathRoutes, out ex);
        }
        public bool Load(string fileName, out Exception ex)
        {
            ex = null;

            Routes = new List<Route>(32);
      
            try
            {

                // read routes from file
                string json = File.ReadAllText(fileName, Encoding.UTF8);
                json = json.Replace("\\", "/"); 

                // JSON serializer and settings
                JsonSerializerSettings s = new JsonSerializerSettings();
                s.MissingMemberHandling = MissingMemberHandling.Ignore;
                s.StringEscapeHandling = StringEscapeHandling.Default;
                Dictionary<string, Dictionary<string, string>> loadedRoutes = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json, s);

                 
                /* DESCRIPTION
                 * 
                 * Name of Method in ModulClass which handles the response of the request -> defined in Interface IModul
                 *                 
                 * Get first Method which is in Interface IModul defined Method - this method excutes for the defined 'RouteAction' the request  
                                         
                "HTML":{
                    "Action":"Modul_File",
                    "UrlSegment": "/html/*.html"
                },
                    
                Route named HTML has the Property 'Action' with Value 'Modul_File'
                    
                All classes in namespace WebQl.Modules with interface IModul can use the Attribute 'ModulMappingAttribute('Modul_File3')'
                    
                The class in namespace WebQl.Modules using interface IModul with the corresponding class Attribute 'ModulMappingAttribute('ActionName')' which is in Route-Config.Json file defined for example in a Routedefinition
                     
                * Route-Config.Json 
                "HTML":{
                    "Action":"ActionName",
                    "UrlSegment": "/html/*.html"
                },
                    
                 * Class in namespace WebQl.Modules with ModulMappingAttribute 
                     
                [ModulMapping("ActionName")]
                public class Modul_Foo : IModul
                {
                    public async Task<ModulResult> Action()
                    {...}
                }
                     
                 */
                string ActionMethodName = Assembly.GetExecutingAssembly().GetTypes().Where(x => String.Equals(x.FullName, "webQL.Modules.IModul", StringComparison.Ordinal)).First().GetMethods().FirstOrDefault().Name;
            

                //foreach (var route in new Dictionary<string, Dictionary<string, string>>(routedict))
                foreach (var route in loadedRoutes)
                    {
                    // convert all parameters in dictionary (loaded from JSON) -> into Route properties
                    Route r = convertingHelper(route);
                        
                    // tune some Properties
                    r.Action = tuneModulName(r.Action);
                    r.UrlSegment = prependSeparatorPath(r.UrlSegment);           
                    r.Items= route.Value;
                    r.Name = route.Key;

                    calculateRegex(r);
                    
                    //  Precalculate Type and MethodInfo for each ACTION in ROUTE
                    Type type = Assembly.GetExecutingAssembly().GetTypes().
                    FirstOrDefault(x => x.GetCustomAttribute<Modules.ModulNameAttribute>() != null && tuneModulName(x.GetCustomAttribute<Modules.ModulNameAttribute>().Map) == r.Action);
                    if (type == null)
                    {
                        LogQL.LogServer("Route WARNING", "Ignore Route: Modul Definition: 'Action':'" + r.Action + "' in Route '" + r.Name + "' (File: " + FILENAME_WEBQL_ROUTES + "), has no corresponding Class with  Attribute [ModulName(\"" + r.Action + "\")].");
                        continue;
                    }
                    else
                    {   
                        r.ActionMethodInfo = type.GetMethod(ActionMethodName, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
                        r.ActionType = type;
                    }

                    if (route.Key.StartsWith("-") == true)
                    {
                        LogQL.LogServer("Route INFO", "Inactive Route '" + r.Name + "': To enable this Route delete the leading '-' from '" + r.Name + "' in your RouteConfig-File: " + FILENAME_WEBQL_ROUTES);
                        continue;           // IGNORE Routes starting with 'minus'  ie. "-Route1":{"k1":"v1","k2":"v2",...}              
                    }
                    else
                    {
                        LogQL.LogServer("Route INFO", "Active Route '" + r.Name + "': If incoming Request.Url matches '" + r.UrlSegment + "' then execute " + r.Action);
                    }
                    // if you go here add Route r to the List of Routes 
                    Routes.Add(r);                   

                }

                return true;

            }
            catch (Exception exc)
            {
                LogQL.LogServer("ERR", "Load Route-Configuration: " + exc.Message);
                Server.Configuration = null;
                ex = exc;
                return false;
            }
        }

        void calculateRegex(Route r){

        // Precalculate REGEX PAttern for URL MAtching here, used in responder - isMatchRouteUrlSegment
                    if (!string.IsNullOrWhiteSpace(r.UrlSegment))
                    {
                        //     /css*/*.(html|css|js)                matches     /cssdir1/anyfile1.css | /cssdir2/anyfile2.js | /css/anyfile3.html
                        //     /js+/*.(html|css|js)                 matches     /jsdir/subdir1/subdir2/anyfile.js
                        //     /js+/f?le.(html|css|js)              matches     /jsdir/subdir1/subdir2/file.js
                        //     /(js/css/html/lib)+/*.(html|css|js)  matches     /lib/subdir1/subdir2/anyfile.js
                        //     /-css/*.*                            matches     /lib/subdir1/subdir2/anyfile.js but NOT /css/anyfile.js

                        //     /base/:Param/:Id                     matches     like /base/*/*  but gets the Parameter :Param & :Id from Url 
                        //     /base/*/*                            matches     /lib/subdir1/subdir2/anyfile.js but NOT /css/anyfile.js



                        //                wordbeginn                mask .     wildcard:   * all until /         + all to the end   ? only one char     ! only null or one char j!s -> js and jxs but not jxxs 
                        var regexpattern = "^" + r.UrlSegment.Replace(".", @"\.").Replace("*", "[^/]*").Replace("+", ".*").Replace("?", @"[\w]").Replace("!", @"[\w]{0,1}");
              
                        // find Parameters between : and /  ie.: /:test/ 
                        var regexParam = new Regex(":[^/]*", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);
                        // replace Param /:test/  with wildcard *
                        regexpattern = regexParam.Replace(regexpattern, "[^/]*") + "$";  // ersetzt Parmeter /:param1/:param2 ->  /*/* so ulr can match
                        
                        
                        // extracts minus preceeding words like '-expression' from urlsegment  /dir/-expression/
                        var regexNegativMatch = new Regex(@"-(\w|\.)*[+\*)/|]{0}", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);
                        MatchCollection negativMatches = regexNegativMatch.Matches(regexpattern);
                        foreach (Match item in negativMatches)
                        {
                            string pattern = @"(?!#/).*";  
                    
                            pattern = pattern.Replace("#", item.Value.Substring(1));
                         //    Console.WriteLine(regexpattern+ "   Pattern:  " + pattern + "  ITEM Value: " + item.Value);
                       
                            regexpattern = regexpattern.Replace(item.Value, pattern);

                       //     Console.WriteLine(regexpattern + "   ITEM Value: " + item.Value);
                       
                        }
                     //   Console.WriteLine("### RegexPattern: " + regexpattern);
                        
                        //regexpattern = regexNegativMatch.Replace(regexpattern, "[^/]*") + "$";  // ersetzt Parmeter /:param1/:param2 ->  /*/* so ulr can match
                        //Console.WriteLine(r.UrlSegment+ "  REGEX MATCH: " + regexpattern);
                        r.UrlSegmentRegex = new Regex(regexpattern, RegexOptions.IgnoreCase | RegexOptions.Compiled |RegexOptions.RightToLeft);
                        
                        
                        //// \/^((?!wort).|(?:wort).).*$
                        //string ss = @"\/^((?!##).|(?:##).).*$";
                        //string word = "test";
                        //ss = ss.Replace("##", word);
                        
                        // URL SPlitting /root/path/dir/file.ext ->  splits in   root,path,dir,file.ext
                        r.UrlRightSegmentRegex = new Regex("[^\\/]?\\w[^/]*", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);
                        r.UrlSegmentMatches = r.UrlRightSegmentRegex.Matches(r.UrlSegment);

                        
                    }
        }

        #endregion

        #region Private Methods 

        private Route convertingHelper(KeyValuePair<string, Dictionary<string, string>> route)
        {
            return getRouteFromDictionary<Route>(route);

        }
        private T getRouteFromDictionary<T>(KeyValuePair<string, Dictionary<string, string>> routeDict)
        {
            // 
            Type type = typeof(T);
            var obj = Activator.CreateInstance(type);
            Dictionary<string, string> p = new Dictionary<string, string>(32);

            System.Reflection.PropertyInfo[] propArr = type.GetProperties();

            foreach (System.Reflection.PropertyInfo prop in propArr) // gehe alle Properties in Class ROUTE durch
            {
                //
                KeyValuePair<string, string> rdv = routeDict.Value.FirstOrDefault(n => n.Key.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                var v = rdv.Value;
                if (string.IsNullOrWhiteSpace(v)) v = null; // "key1":"", "key2":" ", "key2":null  -> null
                if (prop.Name!="Item") prop.SetValue(obj, v); // setze in Property den Wert aus DICT

            }
         
            return (T)obj;
        }

        private const string PREPEND_MODUL = "Modul_";
        private static string tuneModulName(string modul)
        {
            if (string.IsNullOrWhiteSpace(modul)) return null;
            string m= modul.Trim().StartsWith(PREPEND_MODUL, StringComparison.OrdinalIgnoreCase) ? modul : PREPEND_MODUL + modul;
            return m.ToLower();
        }
        
        private static string prependSeparatorPath(string path)
        {
            String delimit = Path.AltDirectorySeparatorChar.ToString();
            if (string.IsNullOrWhiteSpace(path) == true) return null;
            return path.StartsWith(delimit, StringComparison.Ordinal) ? path : delimit + path;
        }
        //public static string normalizeServerPath(string path, bool extendPath)
        //{

        //    if (string.IsNullOrWhiteSpace(path) == true) return path;
        //    String delimit = Path.AltDirectorySeparatorChar.ToString();
        //    if (!Path.IsPathRooted(path) && extendPath) path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + path;
        //    return path.EndsWith(delimit, StringComparison.Ordinal) ? path : path + delimit;
        //}
        //public static string normalizeRootPath(string path, bool extendPath)
        //{

        //    if (string.IsNullOrWhiteSpace(path) == true) return path;
        //    String delimit = Path.AltDirectorySeparatorChar.ToString();

        //    if (!Path.IsPathRooted(path) && extendPath) path = Server.Configuration.Parameters.RootPath + path;
        //    return path.EndsWith(delimit, StringComparison.Ordinal) ? path : path + delimit;
        //}

        #endregion

    }

}
