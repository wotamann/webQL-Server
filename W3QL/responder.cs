using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using webQL.Modules;
using System.Runtime.CompilerServices;

namespace webQL
{

    public sealed class Responder
    {
        #region Declaration
        
        public HttpListenerContext CurrentContext;
        public Router.Route CurrentRoute;
        public Uri CurrentUrl;

        #endregion

        #region Constructor

        public Responder(HttpListenerContext context)
        {
            this.CurrentContext = context;
            this.CurrentUrl = context.Request.Url;
            // this.CurrentRoute will be set in the main loop in processResponseAsync
        }
        
        #endregion

        #region MAIN processing the RESPONSE to the Client ###################################### ###################################### ######################################

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task processResponseAsync()
        {

            if (LogQL.LogRequest!=null) LogQL.LogRequest(this); // LOG Request

            setDefaultHeaders(); // set DEFAULT HEADERS programmatically

    responseStart:
            
            try
            {
                // ---------------------  go through all the routes in Server.Router.Routes until matching the first route. If route.action gives a response ---------------------
                int len = Router.Routes.Count;
                for (int i = 0; i < len; i++)
                {
                    this.CurrentRoute = Router.Routes[i];
                    
                    if (this.isMatchRouteUrlSegment() == false) continue; // no Match URL continue with next in loop 
                    if (this.isMatchMethod() == false) continue; // no Match METHOD continue with next in loop 
                  
                    ModulResult modulResult = await executeAction(this.CurrentRoute);           //  invoke 'method' defined in Route.Action   

                    if (modulResult == ModulResult.Continue) continue;                          // if Continue take next Route
                    if (modulResult == ModulResult.Response) goto responseEnd;                  // if Response break and send Response
                    if (modulResult == ModulResult.ReStart) goto responseStart;                 // if ReStart start the complete Route loop again
                 
                }
                defaultNotFound(); // DEFAULT NOT FOUND - you end here if no Route fits to the request and no catching Modul_NotFound is defined
            }
            catch (Exception err)
            {
                defaultError(err); // DEFAULT ERROR RESPONSE
            }

    responseEnd:

            this.responseSend(); // Write Content to RESPONSE

            if (LogQL.LogResponse != null) LogQL.LogResponse(this);  //  LOG RESPONSE

        }

        #endregion

        #region invoke Action 
        // Async INVOKE the Modul Method defined by ACTION in Route        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<ModulResult> executeAction(Router.Route route)
        {

            // break and take next route if no ActionType and ActionMethodInfo defined
            if (route==null || route.ActionMethodInfo == null || route.ActionType == null) return ModulResult.Continue;
            // create instance of Modul Class 
            var instance = Activator.CreateInstance(route.ActionType, new object[] { this });
            // call this Instance
            return await (Task<ModulResult>)route.ActionMethodInfo.Invoke(instance, null);
           
        }

        #endregion

        #region DefaultHandler -> Headers | NotFound | Error

        // default Headers for Response  
        private void setDefaultHeaders()
        {
            
            // Set fix coded HEADER for every request Called and executed      
            //this.CurrentContext.Response.AddHeader("Cache-Control", "no-cache");
            //this.CurrentContext.Response.AddHeader("Access-Control-Allow-Origin", "*");

            this.CurrentContext.Response.AddHeader("Server", "WebQL -");
            
            // mit status 403 bringt ein LOGIN Formular
            //this.CurrentContext.Response.AddHeader("WWW-Authenticate", "Basic realm=\"insert realm\"");
            
            //this.CurrentContext.Response.AddHeader("Cache-Control", "max-age=86400, public, must-revalidate, proxy-revalidate");
            //// not allowed to be called from IFRAME (avoids clickjacking) https://developer.mozilla.org/en-US/docs/HTTP/X-Frame-Options
            //this.CurrentContext.Response.AddHeader("X-Frame-Option", "SAMEORIGIN");
            //this.CurrentContext.Response.AddHeader("Expires", DateTime.Now.ToLongDateString()); // DateTime.Now.AddDays(1).ToLongDateString()
            //// Details: https://developer.mozilla.org/en-US/docs/HTTP/Access_control_CORS?redirectlocale=en-US&redirectslug=HTTP_access_control#section_5
            //modulParameters.context.Response.AddHeader("Access-Control-Allow-Origin", "*"); 

        }

        // default NotFound Response
        private const string REQUEST_NOT_FOUND = "WebQL - HTTP Status 404: Not Found - The requested Resource is not available.";
        private void defaultNotFound()
        {
            /* if no ROUTE like 
             * 
                    {
                      "Method": "GET",
                      "Action": "Modul_NotFound",
                      "BaseSegment": "/*",
                      "ActionParameter": "404NotFound.html",
                    },
             * 
             * is defined to handle a not found File, then finally handle NOT_FOUND here and
             * response hardcoded HTML_NOT_FOUND and 'HttpStatusCode.NotFound' 
             * 
             * */

            this.setResponseComplete("txt", REQUEST_NOT_FOUND, HttpStatusCode.NotFound);
            

        }

        // default Error Response
        private string ResponseErrorMessage = "WebQL - HTTP Status 500: - Unknown Internal Server Error";
        private void defaultError(Exception exc)
        {
                if (this.CurrentContext.Request.IsLocal) ResponseErrorMessage = "WebQL - HTTP Status 500: - " + exc.Message;  // on localhost bringt Message from ERROR                
                this.setResponseComplete("txt", ResponseErrorMessage, HttpStatusCode.InternalServerError);        
        }
        
        #endregion

        #region Response Helper

        #region Call Action by ActionName

        private const string PREPEND_MODUL = "Modul_";             
        public async Task<ModulResult> callAction(string actionName)
        {
            // exit with continue if no actionName
            if (string.IsNullOrWhiteSpace(actionName)) return ModulResult.Continue;
            
            // tune ACTION NAME -> Modul_
            actionName = actionName.Trim().StartsWith(PREPEND_MODUL, StringComparison.OrdinalIgnoreCase) ? actionName : PREPEND_MODUL + actionName;
            
            // call method ACTION defined in Routes
            return await this.executeAction(Router.Routes.Where(x => string.Equals(x.Action, actionName.ToLower(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault());
        }

        #endregion

        #region isMatch

        public bool isMatchRouteUrlSegment()
        {

       

            // If UrlSegment is undeclared, "" or null then set MATCH is true  -> needed for all unrestrikted MODULS like HEADER; REDIRECT; REWRITE etc...
            if (string.IsNullOrWhiteSpace(CurrentRoute.UrlSegment)) return true;


            // Console.WriteLine((CurrentRoute.UrlSegmentRegex.IsMatch(this.CurrentUrl.LocalPath)?"MATCH  ":"FAILED  ")+CurrentRoute.Name+"- |UrlSegmentRegex  " + CurrentRoute.UrlSegmentRegex + "| MATCH: URL| " + CurrentRoute.UrlSegment + " |CURR LOCALPATH| " + CurrentUrl.LocalPath + " |REQ LOCALPATH| " + CurrentContext.Request.Url.LocalPath);
  

            // Create a new wildcard to search for all  -  .txt files, regardless of case -> get Wildcardpattern from  current route ex 'HandledRoutes'
            // match Request.URL with the 'URLSegment' declared in CurrentRoute    
            // /+/Fi?le.*    matches /anychar/FiXle.css   and  /anychar/anychar/anychar/FiYle.js 
            // /*/Fi?le.*    matches /anychar/FiXle.css   but not  /anychar/anychar/anychar/FiYle.js             
            if (!CurrentRoute.UrlSegmentRegex.IsMatch(this.CurrentUrl.LocalPath)) return false;

            // if match=true -> set Url Parameters like /:Dir/:File  -> ResponseInfo.CurrentRoute[":Dir"] + ResponseInfo.CurrentRoute[":File"];
            setUrlParametersToRouteItems();
          
            return true;
             
        }

        // check Request.Method vs CurrentRoute["Method"] declared in RouteConfig (GET/POST etc... )
        private bool isMatchMethod()
        {
            /* RETURN
             * True     undefined or matching POST, GET, UPDATE, DELETE, HEAD ...
             * False    Request method NOT matching POST, GET, UPDATE, DELETE, HEAD ...          
             */
            // in Definition MEthod is undefined, "" or null then always true
            if (string.IsNullOrWhiteSpace(CurrentRoute["Method"])) return true;
            // Compare Method in Definition vs Method in Request

            //Console.WriteLine(this.CurrentContext.Request.HttpMethod + "CR" + CurrentRoute.Method + this.CurrentContext.Request.HttpMethod.StartsWith(this.CurrentRoute.Method, StringComparison.OrdinalIgnoreCase));
            return this.CurrentRoute["Method"].IndexOf(this.CurrentContext.Request.HttpMethod, StringComparison.OrdinalIgnoreCase) > -1;
        }
        
        #endregion

        #region Base-/RightSegment/UrlParameter
      
        /// <summary>
        /// Extracts the RightSegment /app.js from an string /js/lib/app.js?query1=111&query2=222
        /// </summary>
        /// <param name="url"></param>
        /// <returns>RightSegment</returns>
        public string getRightSegmentLocationPath(string Url)
        {
            return this.CurrentRoute.UrlRightSegmentRegex.Match(Url).Value;
        }
        public string getRightSegmentLocationPath()
        {
           return getRightSegmentLocationPath(this.CurrentUrl.LocalPath);
        }

        public MatchCollection getSegmentsLocationPath(string Url)
        {
            return this.CurrentRoute.UrlRightSegmentRegex.Matches(Url);
        }
        public MatchCollection getSegmentsLocationPath()
        {
            return getSegmentsLocationPath(this.CurrentUrl.LocalPath);
        }

        public MatchCollection getSegmentsRoutePath(string Url)
        {
            return this.CurrentRoute.UrlRightSegmentRegex.Matches(Url);
        }
        public MatchCollection getSegmentsRoutePath()
        {
            return getSegmentsRoutePath(this.CurrentRoute.UrlSegment);
        }
     
        /// <summary>
        /// "UrlSegment": "/base/:Dir/:File"  in Route-Config  
        /// works like the following UrlSegment
        /// "UrlSegment": "/base/*/*"
        /// and accepts this requested UrlPath base/mydirectory/pinkfloyd 
        /// 
        /// Call: http://localhost:8081/base/mydirectory/pinkfloyd
        /// set the Parameter ':Dir' with value 'mydirectory' and ':File'='pinkfloyd' 
        ///         
        /// use this Parameters in the Code of your own Modules: 
        /// 
        /// ...
        /// ResponseInfo.setUrlParametersToRouteItems();
        /// var valueDir = ResponseInfo.CurrentRoute[":Dir"];  //-> mydirectory
        /// var valueFile =ResponseInfo.CurrentRoute[":File"]; //-> pinkfloyd
        /// ...
        /// 
        /// </summary>
        public void setUrlParametersToRouteItems()
        {

            MatchCollection currentMatches = this.CurrentRoute.UrlRightSegmentRegex.Matches(CurrentUrl.LocalPath); // Matches Current PATH from Request
            // for (int i = Math.Min(currentMatches.Count, this.CurrentRoute.UrlSegmentMatches.Count) - 1; i >= 0; i--)

            for (int i = 0; i < Math.Min(currentMatches.Count, this.CurrentRoute.UrlSegmentMatches.Count); i++)
            {
                if (this.CurrentRoute.UrlSegmentMatches[i].Value.StartsWith(":"))
                {
                    this.CurrentRoute[this.CurrentRoute.UrlSegmentMatches[i].Value] = currentMatches[i].Value;
                    // Console.WriteLine("PARAMETER Name= " + this.CurrentRoute.UrlSegmentMatches[i].Value + "   with Value=" + currentMatches[i].Value);
                }

            }
        }

        #endregion

        #region normalizePath

        public string normalizeRootPath(string path, bool extendPath)
        {
            if (string.IsNullOrWhiteSpace(path) == true) return path;
            String delimit = Path.AltDirectorySeparatorChar.ToString();
            if (!Path.IsPathRooted(path) && extendPath) path = Server.Configuration.Parameters.RootPath + path;
            return path.EndsWith(delimit, StringComparison.Ordinal) ? path : path + delimit;
        }

        public string normalizeServerPath(string path, bool extendPath)
        {
            if (string.IsNullOrWhiteSpace(path) == true) return path;
            String delimit = Path.AltDirectorySeparatorChar.ToString();
            if (!Path.IsPathRooted(path) && extendPath) path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), path) ;
            return path.EndsWith(delimit, StringComparison.Ordinal) ? path : path + delimit;
        }

        #endregion

        #region getRequestFirstHeader/-HeaderCollection
        // REQUEST GET HEADER & QUERY Collection        
        public string getRequestFirstHeader(string headerName)
        {
            string[] t = CurrentContext.Request.Headers.GetValues(headerName);
            if (t == null || t.Length < 1) return string.Empty;
            return t[0];
        }
        public Dictionary<string, string> getRequestHeaderCollection()
        {
            Dictionary<string, string> hd = new Dictionary<string, string>(16);
            NameValueCollection hv = this.CurrentContext.Request.Headers;
            
            foreach (string name in hv)
            {
                hd.Add(name, hv[name]);
            }
            return hd;
        }

        #endregion

        #region getQuerySegment/-Collection
        // GET Query-Segment or Query-Collection from URL, set Query-Collection -> Route.Item
        public string getQuerySegment()
        {
            return getQuerySegment(this.CurrentUrl);
        }
        public string getQuerySegment(Uri url)
        {
            // ?item1=123&item2=test   -> get Query String 'item1=123&item2=test' or Dictionary with all Query-Items key:item1 value:123, key:item2 value:test  
            if (url == null || WebUtility.UrlDecode(url.Query).Length < 1) return string.Empty;
            return WebUtility.UrlDecode(url.Query).Substring(1);
        }
        public Dictionary<string, string> getQueryCollection()
        {
            return getQueryCollection(this.CurrentUrl);

        }
        public Dictionary<string, string> getQueryCollection(Uri url)
        {
            string[] p = getQuerySegment(url).Split('&');
            Dictionary<string, string> d = new Dictionary<string, string>(8);

            foreach (var item in p)
            {
                string[] i = item.Split('=');
                if (i.Length > 1) d.Add(i[0], i[1]);
            }

            return d;

        }
        public void setQueryCollectionToRouteItem()
        {
            setQueryCollectionToRouteItem(this.CurrentUrl);
        }
        public void setQueryCollectionToRouteItem(Uri url)
        {
        
            // URL?wood=oak&water=fresh   -> set ResponseInfo.CurrentRoute["wood"]="oak" and ResponseInfo.CurrentRoute["water"]="fresh";
        
            getQuerySegment(url).Split('&').ToList().ForEach(x => { 
                string[] i = x.Split('='); if (i.Length > 1) this.CurrentRoute[i[0]] = i[1]; 
            });
        }

        #endregion
        
        #region getMimeType/-ContentType

        public string getMimeType(string fileType)
        {   // text -> text/html
            return Server.MimeType.getMimeType(fileType);    // if unknown MIME then return Default siehe Definition of getMimeTyp       
        }
        public string getContentType(string fileType)
        {   // text -> text/html; charset=UTF-8 
            return Server.MimeType.getContentType(fileType);    // if unknown MIME then return Default siehe Definition of getMimeTyp + ;Charset      
        }

        #endregion

        #region setResponse
        // SET Response Cookie
        public void setResponseCookie(string name, string value)
        {

            Cookie c = new Cookie(name, value, "/");
            c.Expires = DateTime.MinValue;
            c.HttpOnly = false;
            // c.Domain
            this.CurrentContext.Response.Cookies.Add(c);

        }
        // SET Response Statuscode 
        public void setResponseStatusCode(HttpStatusCode status)
        {
            /*
             http://en.wikipedia.org/wiki/List_of_HTTP_status_codes
             */
            this.CurrentContext.Response.StatusCode = (int)status;
        }
        // SET Response Header
        public void setResponseHeader(string name, string value)
        {
            this.CurrentContext.Response.AddHeader(name, value);
        }
        public void setResponseHeaderContentType(string fileType)
        {
            this.CurrentContext.Response.AddHeader("Content-Type", getContentType(fileType)); // vom FILE-TYP den CONTENT-TYPE erraten   
        }
        public void setResponseHeaderContentTypeMediaType(string mediaType)
        {
            this.CurrentContext.Response.AddHeader("Content-Type",mediaType ); // vom FILE-TYP den CONTENT-TYPE erraten   
        }

        // SET RESPONSE Complete: Content, Filetype and Status in a single method  - not DRY but fast
        public void setResponseComplete(string fileType, byte[] content, HttpStatusCode status)
        {
            this.CurrentContext.Response.StatusCode = (int)status;
            setResponseComplete(fileType, content);
        }
        public void setResponseComplete(string fileType, string content, HttpStatusCode status)
        {
            this.CurrentContext.Response.StatusCode = (int)status;
            setResponseComplete(fileType, content);
        }
        public void setResponseComplete<T>(string fileType, T content, HttpStatusCode status)
        {
            this.CurrentContext.Response.StatusCode = (int)status;
            setResponseComplete<T>(fileType, content);
        }
        public void setResponseComplete(HttpStatusCode status) // content from route parameter
        {
            /*
                ROUTES.. {"Parameter": [filetyp]|[contentstring]"},
                ROUTES.. {"Parameter": "txt|errormessage"},
                ROUTES.. {"Parameter": "json|{"error":"errormessage"}"},
            */
            this.CurrentContext.Response.StatusCode = (int)status;
            setResponseCompleteFromRoute();

        }
        
        // SET RESPONSE Complete: Content, Filetype and Status in a single method  - not DRY but fast
        public void setResponseComplete(string fileType, byte[] content)
        {
            this.CurrentContext.Response.AddHeader("Content-Type", getContentType(fileType)); // vom FILE-TYP den CONTENT-TYPE erraten   
            responseWriteContent = content;
        }
        public void setResponseComplete(string fileType, string content)
        {
            this.CurrentContext.Response.AddHeader("Content-Type", getContentType(fileType)); // vom FILE-TYP den CONTENT-TYPE erraten               
            responseWriteContent = Encoding.UTF8.GetBytes(content);
        }
        public void setResponseComplete<T>(string fileType, T content)
        {
            this.CurrentContext.Response.AddHeader("Content-Type", getContentType(fileType)); // vom FILE-TYP den CONTENT-TYPE erraten   
            responseWriteContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content));
        }
        public void setResponseCompleteFromRoute() // get content as parameterfrom route configuration file 
        {
            /*
                ROUTES.. 
                *      {   ...,
                *          "content":"Hier steht der Response Content",
                *          "contentType":"txt",
                *      }
            */

            string contentType = this.CurrentRoute["ContentType"] ?? "txt";
            string content = this.CurrentRoute["Content"] ?? "";

            this.CurrentContext.Response.AddHeader("Content-Type", getContentType(contentType)); // vom FILE-TYP den CONTENT-TYPE erraten     
            responseWriteContent = Encoding.UTF8.GetBytes(content);
        }

        // SET Response Content as Byte[], String or Generic Class
        public void setResponseContent(byte[] content)
        {
            responseWriteContent = content;
        }
        public void setResponseContent(string content)
        {
            responseWriteContent = Encoding.UTF8.GetBytes(content);
        }
        public void setResponseContent<T>(T content)
        {
            responseWriteContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content));
        }

        // add a prepending secureJsonPrefix used by AngularJS to the Content
        // Declaration of secureJsonPrefix
        public const string secureJsonPrefix = ")]}',\n";
        /* secureJsonPrefix will be prefixed to all JSON Strings. Default ")]}',\n" used by Angular 
            * if you dont want a prefix set 'secureJsonPrefix' to string.Empty
            * 
            * A JSON vulnerability allows third party website to turn your JSON resource URL into JSONP request under some conditions. 
            * To counter this your server can prefix all JSON requests with following string ")]}',\n". 
            * Angular will automatically strip the prefix before processing it as JSON. -> http://docs.angularjs.org/api/ng.$http  JSON Vulnerability Protection
            * 
            * Detailsto Vulnerability -> http://haacked.com/archive/2008/11/20/anatomy-of-a-subtle-json-vulnerability.aspx
        */
        public void addResponseSecureJsonPrefix()
        {
            //  prepend the response byte[] with the secureJsonPrefix
            //  setResponseComplete("json",responseString, HttpStatusCode.OK);
            //  responseAddSecureJsonPrefix();
            //  return ServerModules.ModulResult.Response; 

            byte[] SecureJsonPrefixBytes = Encoding.UTF8.GetBytes(secureJsonPrefix);

            byte[] secureResponseWriteContent = new byte[SecureJsonPrefixBytes.Length + responseWriteContent.Length];
            System.Buffer.BlockCopy(SecureJsonPrefixBytes, 0, secureResponseWriteContent, 0, SecureJsonPrefixBytes.Length);
            System.Buffer.BlockCopy(responseWriteContent, 0, secureResponseWriteContent, SecureJsonPrefixBytes.Length, responseWriteContent.Length);

            responseWriteContent = secureResponseWriteContent;

        }


        // Declaration of responseWriteContent
        public byte[] responseWriteContent = null;
        // Clear Content  -> responseWriteContent=null
        public void setResponseNull()
        {
            // set buffervariable null
            responseWriteContent = null;
        }
        // Send Content
        public async Task responseSend()
        {

            HttpListenerResponse response = this.CurrentContext.Response;
   
            if (responseWriteContent != null) {
                response.ContentLength64 = responseWriteContent.Length;
                await response.OutputStream.WriteAsync(responseWriteContent, 0, responseWriteContent.Length);
            }

            setResponseNull();
            response.Close();
            return;
       
        }
        
        // Send CHUNK
        public void responseBeginChunked()
        {
            this.CurrentContext.Response.SendChunked = true;
        }
        public void responseBeginChunked(string fileType)
        {
            this.CurrentContext.Response.SendChunked = true;
            this.CurrentContext.Response.AddHeader("Content-Type", getContentType(fileType)); // vom FILE-TYP den CONTENT-TYPE erraten               
        }
        public void responseBeginChunked(string fileType, HttpStatusCode status)
        {
            this.CurrentContext.Response.SendChunked = true;
            this.CurrentContext.Response.StatusCode = (int)status;
            this.CurrentContext.Response.AddHeader("Content-Type", getContentType(fileType)); // vom FILE-TYP den CONTENT-TYPE erraten               
        }

        public async Task responseSendChunked(byte[] content)
        {
                       
            if (content == null) return;
            try
            {
                await this.CurrentContext.Response.OutputStream.WriteAsync(content, 0, content.Length);
                // await this.CurrentContext.Response.OutputStream.FlushAsync();   
            }
            catch {}

       
        }
        public async Task responseSendChunked(string content)
        {

            byte[] cont = Encoding.UTF8.GetBytes(content);
            await responseSendChunked(cont);

        }
        
        public async Task responseEndChunked()
        {
            await this.CurrentContext.Response.OutputStream.FlushAsync();
        }
        

        #endregion
        
        #region getPOST

        // REQUEST GET POST        
        public async Task<string> getPost(string key)
        {
            /* read POST-DATA from Request and try to get the Value from the KeyParam as String
            * 
            * sample:  string s = await getPost("Filter");
            *
            */
            string requestPostData = await readPostInputStream();
            
            if (string.IsNullOrWhiteSpace(key)) return requestPostData;

            Dictionary<string, string> reqDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestPostData); // new Dictionary<string, object>();
            string outStr = null;
            if (reqDic.TryGetValue(key, out outStr)) return outStr;
            return String.Empty;
            
        }
        
        public async Task<T> getPost<T>()
        {
            /* 
             * read POST-DATA from Request and try to convert it to generic Type
             * 
             * sample:  string x = await getPost<string>(request);
             *          Dictionary<string, dynamic> x = await getPost<Dictionary<string, dynamic>>();
             */

            string requestPostData = await readPostInputStream();

            //// ALTERNATIV JsonConvert using System.Web.Script.Serialization;
            //return (T)JsonConvert.DeserializeObject(requestPostData, typeof(T));
            
            //// ALTERNATIV using System.Web.Script.Serialization;
            var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            return (T)jss.Deserialize<T>(requestPostData);



        }

        public async Task<T> getPost<T>(string key)
        {
            /* 
             * read POST-DATA from Request and convert to Dictionary and then try to convert one DictItem with the KeyParam to deserialize to the generic Type
             * 
             * sample:  string s = await getPost<string>(request);
             *
             */
            string requestPostData = await readPostInputStream();
            Dictionary<string, Object> reqDic = JsonConvert.DeserializeObject<Dictionary<string, Object>>(requestPostData); // new Dictionary<string, object>();
            Object keyObj = null;
            if (reqDic.TryGetValue(key, out keyObj))
            {
                string js = JsonConvert.SerializeObject(keyObj);
                return (T)JsonConvert.DeserializeObject(js, typeof(T));
            }
            return default(T);
        }

        // .....
        string postInputStream = null;
        public async Task<string> readPostInputStream()
        {
            //  if not first call use the readed postInputStream 
            if (!string.IsNullOrWhiteSpace(postInputStream)) return postInputStream;
            // otherwise read Request.InputStream

            //  header must be set 'Content-Type': 'application/json;charset=UTF-8' sonst vertümmeln die UMLAUTE !!!
            using (StreamReader requestInputstream = new StreamReader(this.CurrentContext.Request.InputStream, this.CurrentContext.Request.ContentEncoding))
            {
                postInputStream = await requestInputstream.ReadToEndAsync();
                return postInputStream;
            }

        }


        #endregion

        #endregion


    }

}
