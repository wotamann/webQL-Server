using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using webQL.Modules.Ganyweb;

namespace webQL
{
    
    namespace Modules
    {

        /// Description
        /// 
        /// namespace Modules contains the Classes which are defined in 
        /// 
        /// Route.Config.Json 
        /// 
        /// "RouteName":
        /// {
        ///     ...
        ///     "Action":"MyModulName777",
        ///     "Param1":"valueOfParam1"
        ///     ...
        /// }
        /// 
        /// 
        /// namespace Modules
        /// 
        /// [ModulMapping("MyModulName777")]
        /// public class AnyName : IModul
        /// {   ...
        ///     Task<ModulResult> Action()
        ///     {
        ///         string valueOfParam1 = ResponseInfo.CurrentRoute["Param1"];
        ///         ...  
        ///         return ModulResult.Response 
        ///     };
        ///     ...
        /// }
        /// 
        /// 
        /// Summary:
        /// Action":"MyModulName777"  -> calls the async Method 'Action()' in Class AnyName with Attribute [ModulMapping("MyModulName777")] 
        /// 
        /// 
        /// 

        #region Attributes

        [AttributeUsage(AttributeTargets.Class)]
        public sealed class ModulNameAttribute : Attribute
        {
            public string Map;
            public ModulNameAttribute(string s)
            {
                Map = s;
            }
        }

        #endregion

        #region Declarations

        public enum ModulResult
        {

            ReStart,   // Restart Route loop 
            Response,   // break loop of routes and send response
            Continue,   // simply go on...
        }
        public interface IModul
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Task<ModulResult> Action();
        }

        #endregion
        
            
        [ModulName("Modul_File")]
        public class Modul_File : IModul 
        {

           
            private Responder ResponseInfo;
            public Modul_File(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }

            public async Task<ModulResult> Action()
            {


               
                string currentPath = ResponseInfo.CurrentRoute["Path"]; // "Path":"C:\absolute\path\"  or "Path":"relative\pathtoroot\"  
                string currentUrlReplacement = ResponseInfo.CurrentRoute["UrlReplacement"]; // "Path":"C:\absolute\path\"  or "Path":"relative\pathtoroot\"  

                
                if (string.IsNullOrWhiteSpace(currentPath))
                {
                        currentPath = Server.Configuration.Parameters.RootPath + ResponseInfo.CurrentUrl.LocalPath;    // rootpath + url.localpath  ->  "C:/root/path/" + "test/js/app.js"                     
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(currentUrlReplacement))
                    {
                        // "Path":"C:\absolute\path\" + url http://server/test/js/app.js ->  "C:\absolute\path\" + "test/js/app.js"
                        currentPath = ResponseInfo.normalizeRootPath(currentPath, true) + ResponseInfo.CurrentUrl.LocalPath; 
                    }
                    else
                    {
                        string[] r = currentUrlReplacement.Split('|');
                        // "Path":"C:\absolute\path\" + url http://server:80/test/replace/app.js + 'UrlReplacement':'xxx|test/replace/' ->  "C:\absolute\path\" + "xxx/app.js"
                        currentPath = ResponseInfo.normalizeRootPath(currentPath, true) + Regex.Replace(ResponseInfo.CurrentUrl.LocalPath,r[0], r[1]);   
                        
                        //// "Path":"C:\absolute\path\" + url http://server/test/js/app.js ->  "C:\absolute\path\" + "app.js"
                        //currentPath = ResponseInfo.normalizeRootPath(currentPath, true) + ResponseInfo.getRightSegmentLocationPath(ResponseInfo.CurrentUrl.LocalPath);   
                    }
                }

                Console.WriteLine("current path "+currentPath);

                if (File.Exists(currentPath))
                {

      

                    byte[] content = null;

                    using (FileStream SourceStream = new FileStream(currentPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        content = new byte[SourceStream.Length];
                        await SourceStream.ReadAsync(content, 0, (int)SourceStream.Length);
                    }

                    // unique Hash from: lastmodify File + path + content -> if ny of this 3 things are modified then send new content to the browser
                    var md5 = System.Security.Cryptography.MD5.Create();
                    string mixHash = File.GetLastWriteTimeUtc(currentPath) + currentPath + md5.ComputeHash(content);
                    string ETagResponse = Crypto.hashToBase64_MD5(mixHash);
                    string ETagRequest = ResponseInfo.getRequestFirstHeader("If-None-Match");
                    
                    //  If-None-Match:gmaFSLlRdjE8AYXTDcc+GCPg5QX/nvoc/8fKQYJGmGk=   
                    //  ETagResponse  gmaFSLlRdjE8AYXTDcc+GCPg5QX/nvoc/8fKQYJGmGk=
                    if (ETagRequest == ETagResponse )
                    {
                        ResponseInfo.setResponseNull();
                        ResponseInfo.setResponseStatusCode(HttpStatusCode.NotModified);
                    }
                    else
                    {

                        if (ResponseInfo.CurrentContext.Request.HttpMethod == "HEAD") // send no content only message
                            ResponseInfo.setResponseComplete(currentPath, string.Empty, HttpStatusCode.NoContent);

                        if (ResponseInfo.CurrentContext.Request.HttpMethod == "GET")  // send file
                        {
                            // set ETAG Header
                            ResponseInfo.setResponseHeader("ETag", ETagResponse);
                           
                            ResponseInfo.setResponseComplete(currentPath, content, HttpStatusCode.OK);

                        }
              
                    }

                    // finish response and send file
                    return ModulResult.Response;

                }
                

                // FILE NOT found continue... continue and take next routes;

                // no success -> continue with next route                
                return ModulResult.Continue;
            
            }

        }
        
        [ModulNameAttribute("Modul_NotFound")]                        
        public class Modul_NotFound : IModul
        {
            private Responder ResponseInfo;


            public Modul_NotFound(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }
            
            public async Task<ModulResult> Action()
            {
                /* 
                 * PARAMETERS:
                 * ----------
                 * FileName404
                 * Type404
                 * Message404
                 * 
                 */

                // if 'FileName404' is defined then respond the file 
                string fileName404 = !string.IsNullOrWhiteSpace(ResponseInfo.CurrentRoute["FileName404"]) ? ResponseInfo.CurrentRoute["FileName404"] : "404.hmtl";
                fileName404 = Server.Configuration.Parameters.RootPath + fileName404;  
              
                if (File.Exists(fileName404))
                {

                    HttpListenerResponse response = ResponseInfo.CurrentContext.Response;
                    byte[] content = null;
                    //string contentType = null;   

                    using (FileStream SourceStream = File.OpenRead(fileName404))
                    {
                        content = new byte[SourceStream.Length];
                        await SourceStream.ReadAsync(content, 0, (int)SourceStream.Length);
                    }

                    ResponseInfo.setResponseComplete(fileName404, content, HttpStatusCode.OK);

                    return ModulResult.Response;
                }

                // if 'FileName404' is not defined and Parameter 'Message404' is defined then respond the Message in Value
                if (!string.IsNullOrWhiteSpace(ResponseInfo.CurrentRoute["Message404"]))
                {
                    // if any ContentType in 'Type404' is defined then respond the Message with Contentype from 'Type404' otherwise use 'Plain/Txt'
                    string type = !string.IsNullOrWhiteSpace(ResponseInfo.CurrentRoute["Type404"]) ?  ResponseInfo.CurrentRoute["Type404"] : "txt";                    
                    ResponseInfo.setResponseComplete(type, ResponseInfo.CurrentRoute["Message404"], HttpStatusCode.OK);

                    return ModulResult.Response;
                }

                return ModulResult.Continue;

            }
        }

        [ModulName("Modul_SetHeader")]
        public class Modul_SetHeader : IModul
        {

            private Responder ResponseInfo;
            public Modul_SetHeader(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }
            
            public async Task<ModulResult> Action()
            {
                //Set Headers defined in Parameters

                //      "Set Headers":{ /* first in routes */
                //          "Action": "Modul_SetHeader",
                //          "Header1: "HeaderName|HeaderValue",
                //          "Header2: "HeaderName|HeaderValue",
                //          "Header3: "HeaderName|HeaderValue",
                //          }
                //      },

                string[] p1;
                if (!string.IsNullOrWhiteSpace(ResponseInfo.CurrentRoute["Header1"]))
                {
                    p1 = ResponseInfo.CurrentRoute["Header1"].Split('|');
                    ResponseInfo.CurrentContext.Response.AddHeader(p1[0], p1[1]);
                }
                if (!string.IsNullOrWhiteSpace(ResponseInfo.CurrentRoute["Header2"]))
                {
                    p1 = ResponseInfo.CurrentRoute["Header2"].Split('|');
                    ResponseInfo.CurrentContext.Response.AddHeader(p1[0], p1[1]);
                }
                if (!string.IsNullOrWhiteSpace(ResponseInfo.CurrentRoute["Header3"]))                
                {
                    p1 = ResponseInfo.CurrentRoute["Header3"].Split('|');
                ResponseInfo.CurrentContext.Response.AddHeader(p1[0], p1[1]);
                }
            
                return ModulResult.Continue; // continue looping list of routes because this Modul doesnt provide a response

            }

            }
        
        [ModulName("Modul_DB")]
        public class Modul_DB : IModul
        {

            private Responder ResponseInfo;
            private Ganyweb.AccessHelper accessHelper;
            public Modul_DB(Responder ResponseInfo)
            {
                this.ResponseInfo = ResponseInfo;
                this.accessHelper = new Ganyweb.AccessHelper(ResponseInfo);
            }


            public async Task<ModulResult> Action()
            {


                ////Get POST Data from Request   
                //// WARNING: THIS NEXT DOESNT WORK!!!   if  SET or GET is JSON OBJECT from CLIENT    
                //DbQuery.Arguments arg = await ResponseInfo.getPost<DbQuery.Arguments>();


                //Get POST Data from Request            
                Dictionary<string, dynamic> argJons = await ResponseInfo.getPost<Dictionary<string, dynamic>>();

                // convert Dictionary -> DbQuery.Arguments // keep a nested JSON in ie. ARGS.SET      
                DbQuery.Arguments arg = argJons.ToObject<DbQuery.Arguments>();

                // IMPORTANT: get TOKEN fromincoming request and get out USER-RIGHTS
                // this token can't be manipulated, because data is only serverside!!!
                //Ganyweb.Token.TokenInfo token = accessHelper.getAccessToken().UserRights; //  
                //arg.Rights = token.UserRights;
                // take ie. token.UserRights='ABC' and compare this rights with rights defined in DBQuery.json Setting 'Rights':'CDE' -> PASS
                // take ie. token.UserRights='ABC' and compare this rights with rights defined in DBQuery.json Setting 'Rights':'DEFGH' -> NO PASS
                //if one char compares then you have the right to access   
                // otherwise dbquery.crud will respond an 'Not sufficient Rights' Error             
                arg.Rights = accessHelper.getAccessToken().UserRights;


                // GANYMED !!
                if (arg.Message == ("GanymedSelection")) arg.Filter = Tools.Ganymed.PatientFilterCRUD(arg.Filter);

                // get JSON String as Query Result in Format of DbQuery.Result
                string jsonResponse = (string)await DbQuery.ExecuteAsync(arg);  //  arguments for DB Request  
                
                // respond the JSON String 
                ResponseInfo.setResponseComplete("json", jsonResponse, HttpStatusCode.OK);
                
                // ResponseInfo.addResponseSecureJsonPrefix();
                return ModulResult.Response;

            }

        }
        
        [ModulName("Modul_Document")]
        public class Modul_Document : IModul
        {
            private class DOCUMENT
            {
                public string fileName { get; set; }
            }

            private Responder ResponseInfo;
            //   private AccessHelper accessHelper;
            public Modul_Document(Responder ResponseInfo)
            {
                this.ResponseInfo = ResponseInfo;
                //       this.accessHelper = new AccessHelper(ResponseInfo);
            }

            public async Task<ModulResult> Action()
            {
                /*
                 * Install Ghostscript32 Bit used for converting PDF
                 * Install NUGET MagickImage https://magick.codeplex.com/
                 * 
                */

                DOCUMENT doc = await ResponseInfo.getPost<DOCUMENT>();  // get generic POST data 


                string path = null;
                string ext = null;
                string allowedExtensions = ".PDF"; // becomes overwritten with Parameter ResponseInfo.CurrentRoute["AllowedExtensions"]  from route.config


                try
                {
                    // read config parameters
                    path = ResponseInfo.CurrentRoute["Path"];
                    allowedExtensions = ResponseInfo.CurrentRoute["AllowedExtensions"] ?? allowedExtensions; // if NULL then .PDF

                    // check exist on wrong configuration
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        ResponseInfo.setResponseComplete("json", "{\"Success\":false,\"Error\":\"No Path defined.\",\"Data\":null}", HttpStatusCode.OK);
                        return ModulResult.Response;

                    }

                    if (string.IsNullOrWhiteSpace(doc.fileName))
                    {
                        ResponseInfo.setResponseComplete("json", "{\"Success\":false,\"Error\":\"No Filename defined.\",\"Data\":null}", HttpStatusCode.OK);
                        return ModulResult.Response;

                    }

                    path = Path.Combine(path, doc.fileName);
                    ext = Path.GetExtension(doc.fileName).ToUpper();
                    if (allowedExtensions.Contains("|" + ext + "|") == false)
                    {
                        ResponseInfo.setResponseComplete("json", "{\"Success\":false,\"Error\":\"Extension " + Path.GetExtension(doc.fileName).ToUpper() + " not supported\",\"Ext\":\"" + ext + "\",\"Data\":null}", HttpStatusCode.OK);
                        return ModulResult.Response;

                    }


                    if (File.Exists(path))
                    {

                        string base64 = null;

                        //Tools.Helpers.Converter.FileToBase64(path);

                        if (".RTF" == ext)
                        {
                            base64 = Tools.Helpers.Converter.RTFToPlainTextBase64(path);
                        }
                        else
                        {
                            base64 = Tools.Helpers.Converter.FileToBase64(path);
                        }

                        if (string.IsNullOrWhiteSpace(base64))
                        {
                            ResponseInfo.setResponseComplete("json", "{\"Success\":false,\"Error\":\"File no Access or no Data\",\"Ext\":\"" + ext + "\",\"Data\":null}", HttpStatusCode.OK);
                            return ModulResult.Response;
                        }

                        ResponseInfo.setResponseComplete("json", "{\"Success\":true,\"Error\":null,\"Ext\":\"" + ext + "\",\"Data\":\"" + base64 + "\"}", HttpStatusCode.OK);
                        return ModulResult.Response;
                    };

                    ResponseInfo.setResponseComplete("json", "{\"Success\":false,\"Error\":\"File not found\",\"Ext\":\"" + ext + "\",\"Data\": null }", HttpStatusCode.OK);

                }
                catch (Exception ex)
                {
                    ResponseInfo.setResponseComplete("json", "{\"Success\":false,\"Error\":\"Modul_Document:" + ex.Message.Replace("\\", "/").Replace("\"", "'") + "\",\"Data\":null}", HttpStatusCode.OK);
                }



                // no success -> continue with next route
                return ModulResult.Response;

            }

        }

        [ModulName("Modul_Upload")]
        public class Modul_Upload : IModul
        {

            class Modul_UploadInfo
            {
                public string fileName { get; set; }
                public string filePath { get; set; }
                public string lastModifiedDate { get; set; }
                public string dataUrl { get; set; }
            }

            private Responder ResponseInfo;
            //    private AccessHelper accessHelper;
            public Modul_Upload(Responder ResponseInfo)
            {
                this.ResponseInfo = ResponseInfo;
                //       this.accessHelper = new AccessHelper(ResponseInfo);
            }

            public async Task<ModulResult> Action()
            {
                /*
                 * 
                 * 
                 * 
                */


                Modul_UploadInfo uploaddata = await ResponseInfo.getPost<Modul_UploadInfo>();  // get generic POST data 

                string base64Data = Regex.Match(uploaddata.dataUrl, @"data:(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                byte[] binData = Convert.FromBase64String(base64Data);

                // generate uploaddata.filePath
                if (string.IsNullOrWhiteSpace(uploaddata.filePath)) uploaddata.filePath = ResponseInfo.CurrentRoute["Param1"];  // get uploadpath from configfile!!
                if (string.IsNullOrWhiteSpace(uploaddata.filePath)) uploaddata.filePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); // if no uploadpath then take path of exe 

                uploaddata.filePath = ResponseInfo.normalizeRootPath(uploaddata.filePath, true);
                uploaddata.filePath += uploaddata.fileName;


                try
                {
                    using (FileStream SourceStream = File.OpenWrite(uploaddata.filePath))
                    {
                        await SourceStream.WriteAsync(binData, 0, (int)binData.Length);
                    }

                    ResponseInfo.setResponseComplete("json", "{\"Success\":\"Upload Info: OK\",\"Error\":null}", HttpStatusCode.OK);
                }
                catch (Exception ex)
                {
                    ResponseInfo.setResponseComplete("json", "{\"Success\":null,\"Error\":\"Upload Info:" + ex.Message.Replace("\\", "/").Replace("\"", "'") + "\"}", HttpStatusCode.OK);
                }


                // no success -> continue with next route
                return ModulResult.Response;
            }


        }
        
        [ModulName("Modul_ChartList")]
        public class Modul_ChartList : IModul
        {

            private Responder ResponseInfo;
            public Modul_ChartList(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }
            
            public async Task<ModulResult> Action()
            {


                Chart.Chart c = new Chart.Chart(DbQuery.Settings.ConnectionString);
                // query ChartList
                string responseString = await c.getChartListAsync();

                ResponseInfo.setResponseHeaderContentType(".json");
                ResponseInfo.setResponseStatusCode(HttpStatusCode.OK);
                if (responseString.Contains("\"Error\":")) ResponseInfo.setResponseStatusCode(HttpStatusCode.InternalServerError);
                ResponseInfo.setResponseContent(responseString);
                //ResponseInfo.addResponseSecureJsonPrefix();

                return ModulResult.Response;
            }

        }

        [ModulName("Chart")]
        public class Modul_Chart : IModul
        {

            private Responder ResponseInfo;
            public Modul_Chart(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }
            
            public async Task<ModulResult> Action()
            {
                // get parameter by POST
                Chart.Chart.ChartParameter chartParameter = await ResponseInfo.getPost<Chart.Chart.ChartParameter>("chartParameter");

                // construct CHART and set CONN String
                Chart.Chart c = new Chart.Chart(DbQuery.Settings.ConnectionString);

                // get ChartData with parameter in JSON Format
                string responseString = await c.getChartDataAsync(chartParameter);
                if (responseString.Contains("\"Error\":")) ResponseInfo.setResponseStatusCode(HttpStatusCode.InternalServerError);

                // write Response 
                ResponseInfo.setResponseComplete(".json", responseString, HttpStatusCode.OK);
                //ResponseInfo.addResponseSecureJsonPrefix();

                return ModulResult.Response;
            }

        }
        
        [ModulName("Basic")]
        public class Modul_Basic : IModul
        {

            private Responder ResponseInfo;
            public Modul_Basic(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }
            
            public async Task<ModulResult> Action()
            {

                HttpListenerRequest request = ResponseInfo.CurrentContext.Request;
                HttpListenerResponse response = ResponseInfo.CurrentContext.Response;


                ResponseInfo.setUrlParametersToRouteItems();


                var a=ResponseInfo.CurrentRoute[":Dir"] ;
                var f = ResponseInfo.CurrentRoute[":File"] ;
                var t = ResponseInfo.CurrentRoute["Test"];


              //  // getRequestHeaderCollection
              //  string qr = "";
              //  string v = "H5";
              //  Dictionary<string, string> query = ResponseInfo.getQueryCollection(ResponseInfo.CurrentUrl);

              //  if (query.TryGetValue("header", out v)) v = "H" + v;

              //  foreach (var q in query)
              //  {
              //      qr += "<" + v + ">" + q.Key + " = " + q.Value + "</" + v + ">";
              //  }
              //  qr += "<p>------------------------------------------</p>";
              //  Dictionary<string, string> header = ResponseInfo.getRequestHeaderCollection();
              //  foreach (var q in header)
              //  {
              //      qr += "<strong>" + q.Key + "</strong> = " + q.Value + "</br> ";
              //  }
              //string responseString = string.Format("<HTML><BODY><" + v + ">SERVER BASIC MODUL - " + DateTime.Now.ToLongTimeString() + "</" + v + "><" + v + "> Parameter: </" + v + ">" + qr + "</BODY></HTML>");

              // response.AddHeader("Content-Type", "text/html; charset=utf-8");
              //ResponseInfo.setResponseHeaderContentType(".txt");
              //ResponseInfo.setResponseStatusCode(HttpStatusCode.OK);

              ResponseInfo.setResponseComplete("txt","Basic Test send back URL Parameters /base/dir/file !!   DIR:"+a+"  FILE:"+f+"  Test:"+t,HttpStatusCode.OK);

              return ModulResult.Response;

            }
        }

        
        namespace AccessDotHealth
        {
            #region AccessHelper

            public sealed class AccessHelper
            {

                private Responder ResponseInfo;
                public AccessHelper(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }

                private const double ACCESS_TOKEN_EXPIRES_IN_SECONDS = 3600;  // =60min      token is only valid for VALUE seconds
                private const string COOKIE_ACCESS_TOKEN_NAME = "XSRF-TOKEN";
                private const string HEADER_ACCESS_TOKEN_NAME = "x-xsrf-token";
                private const string HEADER_FINGERPRINT_NAME = "x-fingerprint";

                public string generateAccessTokenRandomKey()
                {

                    //Console.WriteLine("RANDOM TOKEN GENERATED:  " + getSHA512Hash(GenerateRandomNumber(1000000000, 9999999999).ToString()));
                    return Crypto.hashToBase64_SHA512(Crypto.GenerateRandomNumberString(100000000000, 999999999999));

                }

                public void generateAccessToken(Token.TokenInfo token)
                {


                    // create ID for TOKENS KEY 
                    string TokenKey = generateAccessTokenRandomKey();

                    token.Key = TokenKey;
                    // get Fingerprint from HEADER            
                    token.Fingerprint = ResponseInfo.getRequestFirstHeader(HEADER_FINGERPRINT_NAME);

                    //  setze Gültigkeitsdauer von TOKEN
                    token.Expires = DateTime.Now.AddSeconds(ACCESS_TOKEN_EXPIRES_IN_SECONDS);

                    // setze USER AGENT
                    token.UserAgent = ResponseInfo.CurrentContext.Request.UserAgent;

                    // create XRSF-COOKIE with Value = TOKENKEY   Details to Securitiy Cookies - Token - XSRF under http://docs.angularjs.org/api/ng.$http              
                    ResponseInfo.setResponseCookie(COOKIE_ACCESS_TOKEN_NAME, TokenKey);

                    // add new created Token to the Tokens-Collection
                    Token.Add(TokenKey, token);

                }
                public void clearAccessToken()
                {
                    // key = value of HEADER_ACCESS_TOKEN_NAME -> remove key from TOKENS                
                    // Requestheader with HEADER_ACCESS_TOKEN_NAME as KEy to Remove  
                    Token.Remove(ResponseInfo.getRequestFirstHeader(HEADER_ACCESS_TOKEN_NAME));

                    // DELETE COOKIE
                    Cookie c = new Cookie(COOKIE_ACCESS_TOKEN_NAME, "", "/");
                    c.Expires = DateTime.Now.AddDays(-1D);  //  Delete COOKIE
                    ResponseInfo.CurrentContext.Response.Cookies.Add(c);

                }
                public Boolean isMatchAccessToken()
                {
                    
                    // get HEADER_ACCESS_TOKEN_NAME as KEY 
                    // getValue TokenInfo from Key with HEADER_ACCESS_TOKEN_NAME -> must in Collection exist  
                    Token.TokenInfo token = getAccessToken(); //  Token.GetValue(ResponseInfo.getRequestFirstHeader(HEADER_ACCESS_TOKEN_NAME)); //  new Token.TokenInfo();

                    // if tokinfo in Tokens is null then no access
                    if (token == null) goto noAccess;
                                       
                    
                    // Header-Fingerprint must be not empty and must match with Token-Fingerprint 
                    if (string.IsNullOrWhiteSpace(token.Fingerprint) || ResponseInfo.getRequestFirstHeader(HEADER_FINGERPRINT_NAME) != token.Fingerprint) goto noAccess;

                    // if DateTime.Now is younger then Token is expired
                    if (DateTime.Now.CompareTo(token.Expires) > 0) goto noAccess;

                    // cookies dont need to be explicit checked - because if no cookie - angularjs sends no token -> no token -> no auth

                    // if here all Checks are passed and you are allowed to go on!   
                    return true;


                // if you come here one Check has failed!    
                noAccess:

                    clearAccessToken();
                    return false;

                }
                public Token.TokenInfo getAccessToken()
                {

                    // Key with HEADER_TOKEN_NAME must in Collection exist  
                    return Token.GetValue(ResponseInfo.getRequestFirstHeader(HEADER_ACCESS_TOKEN_NAME));

                }

                public async Task<Boolean> isMatchLogin(Token.TokenInfo tokenInfo)
                {

                    // get CREDENTIAL {"credential":"secretcredential" } from current REQUEST Inputstream = 'POST' Data  
                    tokenInfo.Credential = await ResponseInfo.getPost("credential");

                    try
                    {
                        string credential = tokenInfo.Credential;
                        // split credential in SALT + HASH
                        string salt = credential.Substring(0, credential.Length / 2);
                        string hash = credential.Substring(credential.Length / 2);

                        // Read USER COLLECTION 
                        DbQuery.Arguments args = new DbQuery.Arguments();
                        args.Name = "TableForAuthentication";
                        args.Get = "User,PasswordHash, Role";

                        string json = await DbQuery.ReadAsync(args);

                        // wenn Fehler aufgetreten - Result.Error -> check failed
                        if (!string.IsNullOrWhiteSpace(DbQuery.DeserializeErrorToString(json))) return false;


                        //    return crypto.hashSHA256(salt + user + password);  // concat salt + hash(salt + user+ password) to credential

                        List<Dictionary<string, string>> users = DbQuery.DeserializeDataToListOfDict(json);
                        foreach (System.Collections.Generic.Dictionary<string, string> u in users)
                        {

                            string us, k, b;
                            u.TryGetValue("User", out us);
                            u.TryGetValue("PasswordHash", out k);
                            u.TryGetValue("Role", out b);

                            //  match requested hash with hash created from DB                
                            if (hash == Crypto.hashToBase64_SHA256(salt + us + k))
                            {
                                    tokenInfo.User = us;
                                    tokenInfo.Authorization = b;
                                    return true;   // nur hier OKAY 
                            }
                        }
                        return false;
                    }
                    catch
                    {
                        return false;
                    }
              
                }
               
                public async Task<Boolean> LogLogin(Token.TokenInfo tokenInfo, string mode)
                {
                    try
                    {
                       
                        DbQuery.Arguments args = new DbQuery.Arguments();
                        args.Name = "TableForAuthenticationLog";
                        args.Set = "{User:'" + tokenInfo.User + "', IP:'" + ResponseInfo.CurrentContext.Request.RemoteEndPoint + "', Useragent:'" + ResponseInfo.CurrentContext.Request.UserAgent + "', Mode:'" + mode + "' }";
                        string json = await DbQuery.CreateAsync(args);
                  
                        // wenn Fehler aufgetreten - Result.Error -> check failed
                        if (!string.IsNullOrWhiteSpace(DbQuery.DeserializeErrorToString(json))) return false;
                        return true;
                    }
                    catch
                    {
                        return false;
                    }

                }
               
            }

            #endregion
            #region Token

            public static class Token
            {
                /// <summary>
                /// Information to the authenticated Token, which will be checked with details(fingerprint, ) from incomming request of the Token in the Tokens Collection. 
                /// </summary>
                public class TokenInfo
                {
                    public string Key;
                    public string IPAdress;
                    public string Fingerprint;
                    public string Credential;
                    public string User;
                    public string Authorization;
                    public string UserAgent;
                    public DateTime Expires;
                }

                // Tokens holds for all incomig requests valid Dictionary of TokenInfos for AUTH-Requests 
                [ThreadStatic]
                public static System.Collections.Concurrent.ConcurrentDictionary<string, TokenInfo> Tokens = new ConcurrentDictionary<string, TokenInfo>(4, 64);

                // Methods
                public static TokenInfo GetValue(string key)
                {

                    // exit with Null
                    if (string.IsNullOrWhiteSpace(key)) return null;

                    TokenInfo value = null;
                    Token.Tokens.TryGetValue(key, out value);  // not found returns defined value=null
                    return value;

                }
                public static void Add(string key, TokenInfo token)
                {
                    if (string.IsNullOrWhiteSpace(key) == false) Token.Tokens.TryAdd(key, token);
                }
                public static void Remove(string key)
                {
                    TokenInfo value = null;
                    if (string.IsNullOrWhiteSpace(key) == false) Token.Tokens.TryRemove(key, out value);
                }
                public static void Clear()
                {
                    Tokens.Clear();
                    // Console.WriteLine(Tokens.Count.ToString() + "TOKENS CLEARED");
                }
                public static void ClearAutomatic()
                {
                    Random r = new Random();
                    if (Tokens.Count > 32 || r.NextDouble()<0.001) Token.ClearExpired();  // delete expired TOKENS from Dictionary only called by random 0.1%           
                }
                public static void ClearExpired()
                {
                    DateTime now = DateTime.Now;
                    int l = Tokens.Count;

                    /* CAVE REMOVE IN FOREACH !!
                        * You can change 
                        * foreach(BruteforceEntry be in Entries.Values) 
                        * to 
                        * foreach(BruteforceEntry be in new List<BruteforceEntry>(Entries.Values))*/
                    foreach (KeyValuePair<string, TokenInfo> token in new List<KeyValuePair<string, TokenInfo>>(Tokens))
                    {
                        if (now.CompareTo(token.Value.Expires) > 0) Remove(token.Key);
                    }
                    //Debug.WriteLine(Tokens.Count.ToString() + " Len - TOKENS REMOVED:" + (l - Tokens.Count).ToString());
                }

            }

            #endregion

            [ModulName("AccessDotHealth")]
            public class Modul_Access : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_Access(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {

                    // Match AccessToken coming from Request with Values in Tokens
                    if (this.accessHelper.isMatchAccessToken() == true)
                        return ModulResult.Continue; // access OK -> continue with your response...

                    // IF NO ACCESS Response here what you want / HttpStatusCode.Unauthorized -> could be used by Angular.js http interceptor 
                    ResponseInfo.setResponseComplete("txt", "Modul_Access: No Access", HttpStatusCode.Unauthorized);  // NO access - break, write response and send response
                    return ModulResult.Response;

                }

            }

            [ModulName("RegisterDotHealth")]
            public class Modul_Register : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_Register(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {
                    GanyAuthResult a = new GanyAuthResult();

                    a.Route = "Register";
                    a.Authorized = false;

                    try
                    {

                        Dictionary<string, string> dict = await ResponseInfo.getPost<Dictionary<string, string>>();



                        string user, passwordHash, role;
                            dict.TryGetValue("user", out user);
                            dict.TryGetValue("passwordHash", out passwordHash);
                            dict.TryGetValue("role", out role);


                            // split credential in SALT + HASH
                       
                        // Read USER COLLECTION 
                            DbQuery.Arguments args = new DbQuery.Arguments();
                            args.Name = "TableForAuthentication";
                            args.Set = "{User:'" + user + "',PasswordHash:'" + passwordHash + "', Role:'" + role + "'}";
                            string json = await DbQuery.CreateAsync(args);

                            // wenn Fehler aufgetreten - Result.Error -> check failed
                            string e = DbQuery.DeserializeErrorToString(json); // fehlermelgung beim registrieren..
                            if (string.IsNullOrWhiteSpace(e))
                            {a.Message = GanyAuthResult.INFO_REGISTER_TRUE;}
                            else
                            {
                                a.Message = GanyAuthResult.INFO_REGISTER_FALSE;
                                a.Error= e;
                            }

                            ResponseInfo.setResponseStatusCode(HttpStatusCode.OK);
                            ResponseInfo.setResponseComplete<GanyAuthResult>("json", a);
                      
                    }
                    catch (Exception ex)
                    {
                        a.Message = GanyAuthResult.ERR_REGISTER;
                        a.Error = ex.Message;
                        ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.InternalServerError);
                    }

                    return ModulResult.Response;

                }
            }
            
            [ModulName("LoginDotHealth")]
            public class Modul_Login : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_Login(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {
                    Token.TokenInfo tokenInfo = new Token.TokenInfo();
                    HttpListenerRequest request = ResponseInfo.CurrentContext.Request;
                    GanyAuthResult a = new GanyAuthResult();
                    a.Route = "Login";

                    // wenn user + password checked
                    if (await this.accessHelper.isMatchLogin(tokenInfo) == true)
                    {
                        try
                        {
                            // if matchClientServerToken == false then setAccessToken  
                            if (this.accessHelper.isMatchAccessToken() != true)
                            {

                                await this.accessHelper.LogLogin(tokenInfo, "Login");

                                this.accessHelper.generateAccessToken(tokenInfo);
                                a.Authorized = true;
                                a.Rights = tokenInfo.Authorization;
                                a.Message = GanyAuthResult.INFO_LOGIN_TRUE;
                                ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.OK);
                            }
                            else
                            {
                                a.Authorized = true;
                                a.Message = GanyAuthResult.INFO_LOGIN_NO;
                                ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.OK);
                            }

                        }
                        catch
                        {
                            a.Authorized = false;
                            a.Message = GanyAuthResult.ERR_LOGIN;
                            ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.Unauthorized);
                        }
                    }
                    else
                    {
                        a.Authorized = false;
                        a.Message = GanyAuthResult.INFO_LOGIN_FALSE;
                        ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.Unauthorized);
                    }

                    return ModulResult.Response; // musst be true to execute loop

                }
            }

            [ModulName("LogoutDotHealth")]
            public class Modul_Logout : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_Logout(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {
                    GanyAuthResult a = new GanyAuthResult();

                    a.Route = "Logout";
                    a.Authorized = false;

                    try
                    {
                        if (this.accessHelper.isMatchAccessToken() == true)
                        {

                            this.accessHelper.clearAccessToken();
                            a.Message = GanyAuthResult.INFO_LOGOUT;
                            ResponseInfo.setResponseStatusCode(HttpStatusCode.Unauthorized);
                        }
                        else
                        {
                            a.Message = GanyAuthResult.INFO_LOGOUT_NO;
                            ResponseInfo.setResponseStatusCode(HttpStatusCode.Unauthorized);
                        }
                        ResponseInfo.setResponseComplete<GanyAuthResult>("json", a);
                    }
                    catch (Exception ex)
                    {
                        a.Message = GanyAuthResult.ERR_REQUEST;
                        a.Error = ex.Message;
                        ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.InternalServerError);
                    }

                    return ModulResult.Response;

                }
            }

            [ModulName("StatusDotHealth")]
            public class Modul_LogStatus : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_LogStatus(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {
                    GanyAuthResult a = new GanyAuthResult();
                    a.Route = "LogStatus";
                    if (this.accessHelper.isMatchAccessToken() == true)
                    {
                        a.Authorized = true;
                        a.Message = GanyAuthResult.INFO_STATUS_TRUE;
                        ResponseInfo.setResponseStatusCode(HttpStatusCode.OK);
                    }
                    else
                    {
                        a.Authorized = false;
                        a.Message = GanyAuthResult.INFO_STATUS_FALSE;
                        ResponseInfo.setResponseStatusCode(HttpStatusCode.Unauthorized);
                    }

                    ResponseInfo.setResponseComplete<GanyAuthResult>("json", a);
                    return ModulResult.Response;

                }
            }

        }
        
        
        namespace Ganyweb
        {

            #region AccessHelper

            public sealed class AccessHelper
            {

                private Responder ResponseInfo;
                public AccessHelper(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }
                
                private const double ACCESS_TOKEN_EXPIRES_IN_SECONDS = 3600;  // =60min      token is only valid for VALUE seconds
                private const string COOKIE_ACCESS_TOKEN_NAME = "XSRF-TOKEN";
                private const string HEADER_ACCESS_TOKEN_NAME = "X-XSRF-Token";
                private const string HEADER_FINGERPRINT_NAME = "X-Fingerprint";
                
                public string generateAccessTokenRandomKey()
                {

                    //Console.WriteLine("RANDOM TOKEN GENERATED:  " + getSHA512Hash(GenerateRandomNumber(1000000000, 9999999999).ToString()));
                    return Crypto.hashToBase64_SHA512(Crypto.GenerateRandomNumberString(100000000000, 999999999999));

                }

                public void generateAccessToken(Token.TokenInfo token)
                {


                    // create ID for TOKENS KEY 
                    string TokenKey = generateAccessTokenRandomKey();
                    
                    // store generated KEY 
                    token.Key = TokenKey;

                    // stroe IP Adress from Endpoint
                    token.IPAdress = ResponseInfo.CurrentContext.Request.LocalEndPoint.Address.ToString();

                    // get Fingerprint from HEADER            
                    token.Fingerprint = ResponseInfo.getRequestFirstHeader(HEADER_FINGERPRINT_NAME);

                    //  setze Gültigkeitsdauer von TOKEN
                    token.Expires = DateTime.Now.AddSeconds(ACCESS_TOKEN_EXPIRES_IN_SECONDS);

                    // setze USER AGENT
                    token.UserAgent = ResponseInfo.CurrentContext.Request.UserAgent;

                    // create XRSF-COOKIE with Value = TOKENKEY   Details to Securitiy Cookies - Token - XSRF under http://docs.angularjs.org/api/ng.$http              
                    ResponseInfo.setResponseCookie(COOKIE_ACCESS_TOKEN_NAME, TokenKey);

                    // add new created Token to the Tokens-Collection
                    Token.Add(TokenKey, token);

                }

                public void clearAccessToken(string tokenKey = null)
                {
                    // key = value of HEADER_ACCESS_TOKEN_NAME -> remove key from TOKENS                
                    // Requestheader with HEADER_ACCESS_TOKEN_NAME as KEy to Remove  

                    if (string.IsNullOrWhiteSpace(tokenKey)) Token.Remove(ResponseInfo.getRequestFirstHeader(HEADER_ACCESS_TOKEN_NAME));
                    if (!string.IsNullOrWhiteSpace(tokenKey)) Token.Remove(tokenKey);

                    // DELETE COOKIE
                    Cookie c = new Cookie(COOKIE_ACCESS_TOKEN_NAME, "", "/");
                    c.Expires = DateTime.Now.AddDays(-1D);  //  Delete COOKIE
                    ResponseInfo.CurrentContext.Response.Cookies.Add(c);

                }

                public string[] extractInfoFromURl()
                {
                    // return param string[] - [0] = Token Key, [1] = fingerprint
                    try
                    {
                        // get url query without '?'
                        string urlToken = this.ResponseInfo.CurrentUrl.Query.Substring(1);
                        string[] tokens = urlToken.Split('=');

                        // decode URL 
                        // 0 = Token Key 1 = fingerprint
                        tokens[0] = WebUtility.UrlDecode(tokens[0]);
                        tokens[1] = WebUtility.UrlDecode(tokens[1]);

                 
                        return tokens;
                    }
                    catch 
                    {
                        return null;
                    }
    
                }
                              
                // get and check TOKEN from URL or from HEADER in request
                public Boolean matchClientServerToken()
                {


                    string tokenKey = null;
                    string fingerPrint = null;

                    try
                    {

                        var urlInfos = extractInfoFromURl();

                        // if token infos in URL then set tokenKey & fingerPrint
                        if (urlInfos != null)
                        {
                            // try to evaluate tokenKey
                            tokenKey = urlInfos[0];
                            // try to evaluate fingerPrint
                            fingerPrint = urlInfos[1];
                        }
                        

                        // get TokenKey as parameter or via request Header HEADER_ACCESS_TOKEN_NAME  
                        if (string.IsNullOrWhiteSpace(tokenKey)) tokenKey = ResponseInfo.getRequestFirstHeader(HEADER_ACCESS_TOKEN_NAME);
                        

                        // getValue TokenInfo from Key with HEADER_ACCESS_TOKEN_NAME -> must in Collection exist  
                        Token.TokenInfo token = Token.GetValue(tokenKey); //  new Token.TokenInfo();

                        // if Token is null and not exist then no access
                        if (token == null) goto noAccess;


                        // if compare if IP from Endpoint is valid with IP storesd in TOken
                        var IP = ResponseInfo.CurrentContext.Request.LocalEndPoint.Address.ToString();
                        if (string.IsNullOrWhiteSpace(token.IPAdress) || token.IPAdress != IP) goto noAccess;


                        // URL oder Header-Fingerprint must be not empty and must match with Token-Fingerprint 
                        if (string.IsNullOrWhiteSpace(fingerPrint)) fingerPrint = ResponseInfo.getRequestFirstHeader(HEADER_FINGERPRINT_NAME);
                        if (string.IsNullOrWhiteSpace(token.Fingerprint) || fingerPrint != token.Fingerprint) goto noAccess;

                        // Cookies check 
                        CookieCollection reqCookies = ResponseInfo.CurrentContext.Request.Cookies;
                        if (reqCookies[COOKIE_ACCESS_TOKEN_NAME] == null || tokenKey != reqCookies[COOKIE_ACCESS_TOKEN_NAME].Value) goto noAccess;

                        // if DateTime.Now is younger then Token is expired
                        if (DateTime.Now.CompareTo(token.Expires) > 0) goto noAccess;


                        // Success here, if all checks have been passed!   
                        return true;

                    }
                    catch (Exception)
                    {
                        // error should never occurr, if else clear token with tokenkey in incoming request
                        clearAccessToken(tokenKey);
                        return false;
                    }


                // if you come here one check has failed and you cant pass!    
                noAccess:

                    // for security reasons clear access token in container
                    clearAccessToken(tokenKey);
                    return false;

                }

                public Token.TokenInfo getAccessToken()
                {

                    // Key with HEADER_TOKEN_NAME must in Collection exist  
                    return Token.GetValue(ResponseInfo.getRequestFirstHeader(HEADER_ACCESS_TOKEN_NAME));

                }

                public async Task<bool> isMatchLogin(Token.TokenInfo tokenInfo)
                {

                    // get CREDENTIAL {"credential":"secretcredential" } from current REQUEST Inputstream = 'POST' Data  
                    tokenInfo.Credential = await ResponseInfo.getPost("credential");

                    return await this.isMatchGanyLogin(tokenInfo);
                    // ------ GANYMED SOLUTION HOOKED HERE -------------------

                    //    // if here all Checks are passed!   
                    //    return true;

                    //// if you come here one Check has failed!    
                    //noAccess:
                    //    return false;
                }

                public async Task<bool> isMatchGanyLogin(Token.TokenInfo tokenInfo)
                {

                    // get CREDENTIAL from POST REQUEST
                    tokenInfo.Credential = await ResponseInfo.getPost("credential");

                    // credential is a base64 coded SHA256 Hashcode concated from SALT and Concat from USER and PASSWORD
                    // prüft user und passwort in Schillermed DB
                    // valid     -> TRUE  + set CurrentUser + set ModParam.currentRights
                    // not valid -> FALSE

                    const int BenutzerBerechtigung = 2048;


                    try
                    {
                        string credential = tokenInfo.Credential;
                        // split credential in SALT + HASH
                        string salt = credential.Substring(0, credential.Length / 2);
                        string hash = credential.Substring(credential.Length / 2);

                        // Read USER COLLECTION 
                        DbQuery.Arguments args = new DbQuery.Arguments();
                        args.Name = "Benutzer";
                        args.Get = "ID, Anwender, Kennwort, Berechtigungen, wk_Rechte";

                        string json = await DbQuery.ReadAsync(args);

                        // wenn Fehler aufgetreten - Result.Error -> check failed
                        if (!string.IsNullOrWhiteSpace(DbQuery.DeserializeErrorToString(json))) return false;

                        List<Dictionary<string, string>> users = DbQuery.DeserializeDataToListOfDict(json);
                        foreach (System.Collections.Generic.Dictionary<string, string> user in users)
                        {

                            string i, u, k, b, r;
                            user.TryGetValue("ID", out i);
                            user.TryGetValue("Anwender", out u);
                            user.TryGetValue("Kennwort", out k);
                            user.TryGetValue("Berechtigungen", out b);
                            user.TryGetValue("wk_Rechte", out r);

                            
                            //  match requested hash with hash created from DB                
                            if (hash == Crypto.hashToBase64_SHA256(salt + u + k))
                            {
                                // benutzerberechtigung in Schillermed Benutzer/Berechtigungen ausreichend ?
                                // dann benutzer retour
                                int bi = Convert.ToInt32(b);
                                //  rights for login
                                if ((bi & BenutzerBerechtigung) > 0)  // if ((bi & BenutzerBerechtigung)== BenutzerBerechtigung)
                                {
                                    tokenInfo.UserID = i;
                                    tokenInfo.User = u;
                                    tokenInfo.Authorization = b;
                                    tokenInfo.UserRights = r;

                                    return true;   // nur hier erfolgreicher login
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        return false;
                    }
                    catch
                    {
                        return false;
                    }

                }

            }


            #endregion
         
            #region Token Manager

            public static class Token
            {
                /// <summary>
                /// Information to the authenticated Token, which will be checked with details(fingerprint, ) from incomming request of the Token in the Tokens Collection. 
                /// </summary>
                public class TokenInfo
                {
                    public string Key;
                    public string IPAdress;
                    public string Fingerprint;
                    public string Credential;
                    public string UserID;                    
                    public string User;
                    public string UserRights;
                    public string Authorization;
                    public string UserAgent;
                    public DateTime Expires;
                }

                // Tokens holds for all incomig requests valid Dictionary of TokenInfos for AUTH-Requests 
                [ThreadStatic]
                public static System.Collections.Concurrent.ConcurrentDictionary<string, TokenInfo> Tokens = new ConcurrentDictionary<string, TokenInfo>(4, 64);

                // Methods
                public static TokenInfo GetValue(string key)
                {

                    // exit with Null
                    if (string.IsNullOrWhiteSpace(key)) return null;

                    TokenInfo value = null;
                    Token.Tokens.TryGetValue(key, out value);  // not found returns defined value=null
                    return value;

                }
                public static void Add(string key, TokenInfo token)
                {
                    if (string.IsNullOrWhiteSpace(key) == false) Token.Tokens.TryAdd(key, token);
                }
                public static void Remove(string key)
                {
                    TokenInfo value = null;
                    if (string.IsNullOrWhiteSpace(key) == false) Token.Tokens.TryRemove(key, out value);
                }
                public static void Clear()
                {
                    Tokens.Clear();
                    // Console.WriteLine(Tokens.Count.ToString() + "TOKENS CLEARED");
                }
                public static void ClearAutomatic()
                {
                    Random r = new Random();
                    if (Tokens.Count > 32) Token.ClearExpired();  // delete expired TOKENS from Dictionary only called by random 0.5%           
                }
                public static void ClearExpired()
                {
                    DateTime now = DateTime.Now;
                    int l = Tokens.Count;

                    /* CAVE REMOVE IN FOREACH !!
                        * You can change 
                        * foreach(BruteforceEntry be in Entries.Values) 
                        * to 
                        * foreach(BruteforceEntry be in new List<BruteforceEntry>(Entries.Values))*/
                    foreach (KeyValuePair<string, TokenInfo> token in new List<KeyValuePair<string, TokenInfo>>(Tokens))
                    {
                        if (now.CompareTo(token.Value.Expires) > 0) Remove(token.Key);
                    }
                    //Debug.WriteLine(Tokens.Count.ToString() + " Len - TOKENS REMOVED:" + (l - Tokens.Count).ToString());
                }

            }

            #endregion
            


            [ModulName("Access")]
            public class Modul_Access : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_Access(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {

                    
                    string redirectPath = ResponseInfo.CurrentRoute["Redirect"];
                    // Match AccessToken coming from Request with Values in Tokens
                    if (this.accessHelper.matchClientServerToken() == true)
                        return ModulResult.Continue; // access OK -> continue with your response...


                    // Here no access allowed - if redirect in route is set then redirect to redirectPath
                    if (!string.IsNullOrWhiteSpace(redirectPath))
                    {
                        ResponseInfo.CurrentContext.Response.Redirect(redirectPath);
                            return ModulResult.Response;
                    }

                    // Here no access allowed / HttpStatusCode.Unauthorized -> could be used by Angular.js http interceptor 
                    ResponseInfo.setResponseComplete("html", WebUtility.HtmlDecode("<h3 style=\"color:#999\"> Keine Zugangsberechtigung, vielleicht müssen Sie sich anmelden!</h3>"), HttpStatusCode.Unauthorized);  // NO access - break, write response and send response
                        return ModulResult.Response;
                }

            }

            #region GanyAuthResult

            public sealed class GanyAuthResult
            {

                public string User;
                public string UserID;
                public string Route;
                public bool Authorized;
                public string Message;
                public string Rights;
                public string Error;
                public string Token;

                public const string ERR_REGISTER = "Fehler bei der Registrierung";
                public const string ERR_LOGIN = "Fehler bei der Anmeldung";
                public const string ERR_LOGOUT = "Fehler bei der Abmeldung";
                public const string ERR_NOT_AUTHORIZED = "Diese Anfrage ist nicht authorisiert";
                public const string ERR_REQUEST = "Fehler bei der Anfrage";
                public const string ERR_SERVER = "Unbekannter Serverfehler";

                public const string INFO_REGISTER_TRUE = "Registrierung war erfolgreich";
                public const string INFO_REGISTER_FALSE = "Registrierung war nicht erfolgreich";
                public const string INFO_LOGIN_TRUE = "Anmeldung war erfolgreich";
                public const string INFO_LOGIN_NO = "Keine nochmalige Anmeldung möglich";
                public const string INFO_LOGIN_FALSE = "Überprüfen Sie 'Benutzer' und 'Passwort'";
                public const string INFO_LOGOUT = "Abmeldung war erfolgreich";
                public const string INFO_LOGOUT_NO = "Keine Abmeldung möglich";
                public const string INFO_STATUS_TRUE = "Anfrage ist authorisiert";
                public const string INFO_STATUS_FALSE = "Anfrage ist NICHT authorisiert";


            }

            #endregion

            [ModulName("GanyLogin")]
            public class Modul_GanyLogin : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_GanyLogin(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {
                    Token.TokenInfo tokenInfo = new Token.TokenInfo();
                    HttpListenerRequest request = ResponseInfo.CurrentContext.Request;
                    GanyAuthResult a = new GanyAuthResult();

                    a.Route = "Login";

                    // wenn user + password checked
                    if (await this.accessHelper.isMatchGanyLogin(tokenInfo) == true)
                    {
                        try
                        {
                            
                            // if matchClientServerToken == false then setAccessToken  
                            if (this.accessHelper.matchClientServerToken() != true)
                            {   

                                this.accessHelper.generateAccessToken(tokenInfo);
                                a.Authorized = true;
                                a.User = tokenInfo.User;        // tokeninfo new generated here 
                                a.UserID = tokenInfo.UserID;    // tokeninfo new generated here 
                                a.Rights = tokenInfo.UserRights;

                                // LogQL.Log("# Fingerprint #", tokenInfo.Fingerprint);

                                
                                a.Message = GanyAuthResult.INFO_LOGIN_TRUE;
                                ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.OK);
                            }
                            else
                            {
                                a.Authorized = true;
                                a.User = tokenInfo.User;        // available tokeninfo here 
                                a.UserID = tokenInfo.UserID;    // available tokeninfo here                                 
                                a.Rights = tokenInfo.UserRights;
                                a.Message = GanyAuthResult.INFO_LOGIN_NO;
                                ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.OK);
                            }

                        }
                        catch
                        {
                            a.Authorized = false;
                            a.Message = GanyAuthResult.ERR_LOGIN;
                            ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.Unauthorized);
                        }
                    }
                    else
                    {
                        a.Authorized = false;
                        a.Message = GanyAuthResult.INFO_LOGIN_FALSE;
                        ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.Unauthorized);
                    }

                    LogQL.Log("LOGIN", a.User + "|" + a.Authorized + "|" + a.Message + "|" + a.Error);

                    return ModulResult.Response; // musst be true to execute loop

                }
            }

            [ModulName("GanyLogout")]
            public class Modul_GanyLogout : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_GanyLogout(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {
                    GanyAuthResult a = new GanyAuthResult();

                    a.Route = "Logout";
                    a.Authorized = false;

                    try
                    {
                        if (this.accessHelper.matchClientServerToken() == true)
                        {
                            this.accessHelper.clearAccessToken();
                            a.Message = GanyAuthResult.INFO_LOGOUT;
                            ResponseInfo.setResponseStatusCode(HttpStatusCode.OK);
                        }
                        else
                        {
                            a.Message = GanyAuthResult.INFO_LOGOUT_NO;
                            ResponseInfo.setResponseStatusCode(HttpStatusCode.Unauthorized);
                        }
                        ResponseInfo.setResponseComplete<GanyAuthResult>("json", a);
                    }
                    catch (Exception ex)
                    {
                        a.Message = GanyAuthResult.ERR_REQUEST;
                        a.Error = ex.Message;
                        ResponseInfo.setResponseComplete<GanyAuthResult>("json", a, HttpStatusCode.InternalServerError);
                    }

                    LogQL.Log("LOGOUT", a.User + "|" + a.Authorized + "|" + a.Message + "|" + a.Error);
                    
                    return ModulResult.Response;

                }
            }

            [ModulName("GanyLogstatus")]
            public class Modul_GanyLogStatus : IModul
            {

                private Responder ResponseInfo;
                private AccessHelper accessHelper;
                public Modul_GanyLogStatus(Responder ResponseInfo)
                {
                    this.ResponseInfo = ResponseInfo;
                    this.accessHelper = new AccessHelper(ResponseInfo);
                }

                public async Task<ModulResult> Action()
                {
                    GanyAuthResult a = new GanyAuthResult();
                    a.Route = "LogStatus";
                    if (this.accessHelper.matchClientServerToken() == true)
                    {
                        a.Authorized = true;
                        a.Message = GanyAuthResult.INFO_STATUS_TRUE;
                        ResponseInfo.setResponseStatusCode(HttpStatusCode.OK);
                    }
                    else
                    {
                        a.Authorized = false;
                        a.Message = GanyAuthResult.INFO_STATUS_FALSE;
                        ResponseInfo.setResponseStatusCode(HttpStatusCode.Unauthorized);
                    }

                    ResponseInfo.setResponseComplete<GanyAuthResult>("json", a);
                    return ModulResult.Response;

                }
            }

            [ModulName("Modul_Gany_SMW_INI")]
            public class Modul_Gany_SMW_INI : IModul
            {
                private class INI
                {
                    public string Section { get; set; }
                    public string Item { get; set; }
                    public string Value { get; set; }
                    public string Error { get; set; }
                }

                private Responder ResponseInfo;
                public Modul_Gany_SMW_INI(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }

                public async Task<ModulResult> Action()
                {

                    HttpListenerRequest request = ResponseInfo.CurrentContext.Request;
                    HttpListenerResponse response = ResponseInfo.CurrentContext.Response;

                    INI ini = await ResponseInfo.getPost<INI>();  // get generic POST data 


                    try
                    {
                        string[] ItemArray = Regex.Split(ini.Item, @"\W+");
                        ini.Value = "";

                        for (int i = 0; i < ItemArray.Length; i++)
                        {
                            ini.Value += Tools.Ganymed.SMW_INI(ini.Section, ItemArray[i]) + "|";
                        }
                        ini.Value = ini.Value.Remove(ini.Value.Length - 1);
                    }
                    catch (Exception e)
                    {
                        ini.Error = "Error in Modul SMW.INI";
                    }

                    ResponseInfo.setResponseComplete("json", ini, HttpStatusCode.OK);
                    return ModulResult.Response;

                }
            }

            [ModulName("Modul_Gany_VAR_INI")]
            public class Modul_Gany_VAR_INI : IModul
            {
                private class INI
                {
                    public string Section { get; set; }
                    public string Item { get; set; }
                    public string Value { get; set; }
                    public string Error { get; set; }
                }

                private Responder ResponseInfo;
                public Modul_Gany_VAR_INI(Responder ResponseInfo) { this.ResponseInfo = ResponseInfo; }

                public async Task<ModulResult> Action()
                {

                    HttpListenerRequest request = ResponseInfo.CurrentContext.Request;
                    HttpListenerResponse response = ResponseInfo.CurrentContext.Response;

                    INI ini = await ResponseInfo.getPost<INI>();  // get generic POST data 

                    try
                    {
                        string[] ItemArray = Regex.Split(ini.Item, @"\W+");                        
                        ini.Value = "";

                        for (int i = 0; i < ItemArray.Length; i++)
                        {
                            ini.Value += Tools.Ganymed.Variable_INI(ini.Section, ItemArray[i])+"|";
                        }
                        ini.Value = ini.Value.Remove(ini.Value.Length - 1);
                    }
                    catch (Exception e)
                    {
                        ini.Error = "Error in Modul VAR.INI";
                    }

                    ResponseInfo.setResponseComplete("json", ini, HttpStatusCode.OK);
                    return ModulResult.Response;

                }
            }
        }
        
    }

}
