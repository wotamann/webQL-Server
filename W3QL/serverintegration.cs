using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using webQL;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace webQL
{
    

    /// <summary>
    /// Static Loging CLass writes to Console and LogFile
    /// </summary>
    public static class LogQL
    {
        #region private

        private static DateTime lastDate=DateTime.Now;

        public static readonly string LogPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "log") + "\\";
        
        private static readonly string LogFilename = "logWebQL.txt";
        private static readonly string HourDelimit = "NEW HOUR ".PadRight(32, '-');

        private static bool firstLogPathTry = true;
     
        #endregion

        public static string LogFilePath
        {
            get
            {

                if (firstLogPathTry)
                {
                    Directory.CreateDirectory(LogPath);
                    firstLogPathTry = false;
                }
                return LogPath + DateTime.Now.ToShortDateString() + " " + LogFilename;
            }
        }

        public static bool WriteToConsole = true;
        public static bool WriteToLogFile = true;  
        
        private static bool logToRequest = true;
        public static bool LogToRequest
        {
            get { return logToRequest; }
            set
            {
                logToRequest = value;
                if (logToRequest == false && LogRequest != null) { logRequestTmp = LogRequest; LogRequest = null; }
                if (logToRequest == true && LogRequest==null) LogRequest = logRequestTmp;
                

            }
        }
        
        private static bool logToResponse=true;
        public static bool LogToResponse
        {
            get { return logToResponse; }
            set {
                logToResponse = value;
                if (logToResponse == false && LogResponse != null) { logResponseTmp = LogResponse; LogResponse = null; } 
                if (logToResponse == true && LogResponse == null) LogResponse = logResponseTmp;
                
                
               }
        }

        private static Action<Responder> logRequestTmp;
        private static Action<Responder> logResponseTmp;        

        // Define LogServerDefault FN 
        public static Action<string, string> LogServer = new Action<string, string>(
            (h, m) => LogQL.Log(h, m));

        // Define LogRequestDefault FN
        public static Action<Responder> LogRequest = new Action<Responder>(
            s =>{
                    var c=s.CurrentContext.Request;
                    LogQL.Log(s.CurrentContext.GetHashCode().ToString("00000000  REQ " + (c.IsSecureConnection ? "S" : "") + (c.IsLocal ? "L" : "")), c.HttpMethod.PadRight(4) + " |" + c.RemoteEndPoint + "|" + c.LocalEndPoint + "|" + c.Url.OriginalString + " |" + c.UserAgent);
            });
        
        // Define LogResponseDefault FN
        public static Action<Responder> LogResponse = new Action<Responder>(
            s =>{
                    LogQL.Log(s.CurrentContext.GetHashCode().ToString("00000000 -RSP"), s.CurrentContext.Response.StatusCode + " |" + s.CurrentRoute.Action + " |" + s.CurrentRoute.UrlSegment);        
            });


        static Queue<string> LogQueue = new Queue<string>(128);

        // main Log Method
        [MethodImpl(MethodImplOptions.AggressiveInlining)]                
        public static void Log(string section, string logMessage)
        {

            if (LogQL.WriteToConsole) Console.WriteLine(String.Format("{0}\t{1,-16}\t{2}", DateTime.Now, section, logMessage));

            if (LogQL.WriteToLogFile)
            {
                try
                {
                    using (StreamWriter w = File.AppendText(LogFilePath))
                    {
                        if (lastDate.Hour < DateTime.Now.Hour) { var t = w.WriteLineAsync(HourDelimit); }

                        var t1 = w.WriteLineAsync(String.Format("{0}\t{1,-16}\t{2}", DateTime.Now, section, logMessage));

                    }
                }
                catch { }
            }

            lastDate = DateTime.Now;
        }

        // clear Log File        
        public static void Clear()
        {
            File.WriteAllText(LogFilePath, "");            
        }
             
    }


    /// <summary>
    /// Class ServerIntegration URLACL, SSLCERT, Get APPID, CURRENTUSER, CERTHASH
    /// </summary>
    public static class ServerIntegration
    {
        // Documentation
        /* 
        * 
        * INFO: HTTP.SYS muß bei USER ohne ADMIN Rechte mittels netsh add UrlACL konfiguriert werden: 
        * 
        * 
        *  http://msdn.microsoft.com/de-de/library/ms733768.aspx
        *  Config HTTP HTTPS für beliebige USER
        *  netsh http add urlacl url=http://+:80/MyUri user=DOMAIN\user
        *  
        * 
        * Konfigurieren eines SSL-Zertifikats
        * http://msdn.microsoft.com/de-de/library/ms733791.aspx
        * http://stackoverflow.com/questions/11403333/httplistener-with-https-support/11457719#11457719
        * 
        * Das Secure Sockets Layer (SSL)-Protokoll verwendet Zertifikate auf Client und Server, um Verschlüsselungsschlüssel 
        * zu speichern. Der Server muss ein SSL-Zertifikat bereitstellen, wenn eine Verbindung hergestellt wird, damit der Client die Identität des Servers überprüfen kann. 
        * Der Server kann ebenfalls ein Zertifikat vom Client anfordern, so dass eine wechselseitige Authentifizierung auf beiden Seiten der Verbindung möglich wird.
        * Zertifikate sind in einem zentralen Speicher entsprechend der IP-Adresse und der Anschlussnummer der Verbindung gespeichert. 
        * Die spezielle IP-Adresse 0.0.0.0 stimmt mit jeder IP-Adresse für den lokalen Computer überein. Beachten Sie, dass der Zertifikatspeicher URLs nicht anhand der Pfade unterscheidet. 
        * Dienste mit der gleichen Kombination von IP-Adresse und Anschlussnummer müssen die gleichen Zertifikate verwenden, auch wenn sich der Pfad in der URL dieser Dienste unterscheidet.
        * Schrittweise Anweisungen finden Sie unter Vorgehensweise: Konfigurieren eines Anschlusses mit einem SSL-Zertifikat.
        * 
        * 
        * ie manually get your Certhash from 'server.cer' OPEN -> Details -> fingerprint -> ‎7c 8b d5 b9 34 b6 21 95 fa bc b6 c3 80 0a 45 ae 9a 3b 58 b6
        * entferne alle Leerzeichen und das Resultat ist der CertHash -> 7c8bd5b934b62195fabcb6c3800a45ae9a3b58b6
        * 
        * Get Application ID from Webprojekt Eigenschaften -> Anwednung -> Assemblyinformation -> GUID dieser Wert ist deine APPID -> 00112233-4455-6677-8899-121236722343
        * 
        * run CMD as Administrator and execute:
        * 
        * netsh http add sslcert ipport=0.0.0.0:8081 certhash=<your certhash> appid={<your APP ID>}
        * 
        * example:
        * 
        * CMD
        * netsh http add sslcert ipport=0.0.0.0:8081 certhash=7c8bd5b934b62195fabcb6c3800a45ae9a3b58b6 appid={00112233-4455-6677-8899-121236722343}
        * 
        * Powershell 
        * netsh http add sslcert ipport=0.0.0.0:8081 certhash=7c8bd5b934b62195fabcb6c3800a45ae9a3b58b6 appid='{00112233-4455-6677-8899-121236722343}'
        * 
        * Important difference is the apostroph('):  using CMD ->  ...appid={...}  -  using Powershell -> ...appid= '{...}' 
        * 
        * CMD CONSOLE
        *      netsh http show sslcert
        *      netsh http delete sslcert ipport=0.0.0.0:443 
        * 
        * PROGRAMMATICALLY
        *      ws.setUrlACL(ws.WebServerArguments.Prefixes);
        *      ws.setSSLCert(ws.WebServerArguments.SSLCertIpport, ws.WebServerArguments.SSLCertHash, ws.WebServerArguments.SSLCertAppId);
        * 
        * 
        * IMPORTANT: IF you want access WebQL Server from external, dont forget 
        * 
        * 1) Config your Firewall to accept incoming requests on the listening Ports         
        * 
        * 2) Config your Router to allow PORT FORWARDING to your WebQL running Computer's IP and the listening Ports
        * 
        * 
        */

        #region Declaration

        private const int SSL_DEFAULT_PORT = 443;

        #endregion

        #region Public Methods

        public static void setRights(string IP_Port, List<string> Prefixes)
        {
            setRights(IP_Port, "", "", Prefixes, "");
        }
        public static void setRights(string IP_Port, string CertHASH, string AppID, List<string> Prefixes, String UrlACLUser)
        {
            try
            {
                StringCollection args = new StringCollection();
                StringCollection argsUrlACL = new StringCollection();
                StringCollection argsCertSSL = new StringCollection();

                // set URLACL for defined prefixes and user  ie. "DOMAIN/user"
                argsUrlACL = generateFromPrefixesUrlACL(Prefixes, UrlACLUser);
                if (argsUrlACL != null)
                {
                    foreach (var arg in argsUrlACL)
                    {
                        args.Add(arg);
                    }
                }

                /// setze für HTTPS  den PORT, CERTHASH und APPID des eigenen Programmes   
                argsCertSSL = generateSSLCert(IP_Port, CertHASH, AppID);
                if (argsCertSSL != null)
                {
                    foreach (var arg in argsCertSSL)
                    {
                        args.Add(arg);
                    }
                }

                runasCMD(args);

            }
            catch (Exception e)
            {
                Console.WriteLine("Error in SetRights: " + e.Message);
            }

        }

        public static void setUrlACL(List<string> prefixes)
        {
            // set URLACL for defined prefixes and current user
            string currentuser = getCurrentUser();
            setUrlACL(prefixes, currentuser);
        }
        public static void setUrlACL(List<string> prefixes, string urlACLUser)
        {
            // set URLACL for defined prefixes and user  ie. "DOMAIN/user"  or "jeder" or "system" etc...
            try
            {

                StringCollection argsUrlACL = new StringCollection();

                argsUrlACL = generateFromPrefixesUrlACL(prefixes, urlACLUser);

                if (argsUrlACL != null) runasCMD(argsUrlACL);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public static void setSSLCert()
        {
            // use default settings  - const int SSL_PORT = 443
            setSSLCert(getLocal_IP_WithPort(SSL_DEFAULT_PORT), selectCertHash(), getAppid());
        }
        public static void setSSLCert(string ipport)
        {
            // ipport ie.: 192.168.0.137:4403 
            setSSLCert(ipport, selectCertHash(), getAppid());
        }
        public static void setSSLCert(string ipport, string certhash)
        {
            // ipport ie.: 192.168.0.137:4403
            setSSLCert(ipport, certhash, getAppid());
        }
        public static void setSSLCert(string ipport, string certhash, string appid)
        {
            /// setze für HTTPS  den PORT, CERTHASH und [APPID]= APPID des eigenen Programmes   
            try
            {

                StringCollection argsSSLCert = new StringCollection();

                argsSSLCert = generateSSLCert(ipport, certhash, appid);
                if (argsSSLCert != null) runasCMD(argsSSLCert);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public static void RegisterInStartupAfterLogin(bool doRegistration)
        {

            var regKey = getRegistry64_32Bit(Microsoft.Win32.RegistryHive.LocalMachine);


            Microsoft.Win32.RegistryKey AutostartKey = regKey.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (doRegistration)
            {
                AutostartKey.SetValue("webQL.exe", Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "webQL.exe"));

            }
            else
            {
                AutostartKey.DeleteValue("webQL.exe");
            }
            AutostartKey.Close();

        }
        public static Boolean RegisterInStartupAfterLoginCheck()
        {

            var regKey = getRegistry64_32Bit(Microsoft.Win32.RegistryHive.LocalMachine);



            using (Microsoft.Win32.RegistryKey AutostartKey = regKey.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", false))
           {
               var r= AutostartKey.GetValue("webQL.exe");           
               if (r==null) return false;
               string rs=(string)r;
               return rs.Length > 0;
           };

        }

        private static string scheduledStartWebQLName = "webQL";
        private static string scheduledStartWebQLFile = Process.GetCurrentProcess().MainModule.FileName;
        public static void RegisterScheduledStartWebQL(bool doRegistration)
        {


            /*
             * 
             * Copyright (c) 2003-2010 David Hall
             * 
             * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
             * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
             * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
             * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
             * 
             http://taskscheduler.codeplex.com/
             * 
             * using Microsoft.Win32.TaskScheduler -> (warning conflicts with Task threading)
             * 
             */


            // Get the service on the local machine
            using (Microsoft.Win32.TaskScheduler.TaskService ts = new Microsoft.Win32.TaskScheduler.TaskService())
            {

                if (doRegistration==true)
                { 

                    // Create a new task definition and assign properties
                    Microsoft.Win32.TaskScheduler.TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Start 'webQL' when booting. You can manage this Task from within 'WebQL' on Item 'Start(Boot)' ";

                    // Create a trigger that will fire on Start Windows without login
                    td.Triggers.Add(new Microsoft.Win32.TaskScheduler.BootTrigger { });

                    // Create an action that will launch WEBQL  whenever the trigger fires
                    string action = scheduledStartWebQLFile;

                    td.Actions.Add(new Microsoft.Win32.TaskScheduler.ExecAction(action, null, null));

                    td.Principal.UserId = "SYSTEM";
                    td.Principal.LogonType = Microsoft.Win32.TaskScheduler.TaskLogonType.ServiceAccount;
                    td.Settings.StopIfGoingOnBatteries = false;
                    td.Settings.DisallowStartIfOnBatteries = false;
                    td.Settings.Priority = ProcessPriorityClass.High;
                    td.Settings.Hidden = true;
                    td.Settings.Enabled = true;                    
                    td.Principal.RunLevel = Microsoft.Win32.TaskScheduler.TaskRunLevel.Highest;
                    // if new Instance startet ignore new one
                    td.Settings.MultipleInstances = Microsoft.Win32.TaskScheduler.TaskInstancesPolicy.IgnoreNew;

                    ts.RootFolder.RegisterTaskDefinition(scheduledStartWebQLName, td); 

                }
                else
                {
                    ts.RootFolder.DeleteTask(scheduledStartWebQLName);   // Remove the task we just created
                }
               
            }

        }
        public static Boolean RegisterScheduledStartWebQLCheck()
        {
            using (Microsoft.Win32.TaskScheduler.TaskService ts = new Microsoft.Win32.TaskScheduler.TaskService())
            {
                return ts.RootFolder.GetTasks(new Regex(scheduledStartWebQLName)).Count > 0;
            }

        }

        public static Boolean isCurrentAdmin()
        {

            try
            {
                var identity = WindowsIdentity.GetCurrent();
                if (identity == null) throw new InvalidOperationException("Couldn't get the current user identity");
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);

            }
            catch (Exception)
            {
                return false;
            }
        }


        #endregion

        #region Private Methods

        private static StringCollection generateFromPrefixesUrlACL(List<string> prefixes)
        {
            // set URLACL for defined prefixes and current user
            string currentuser = getCurrentUser();
            return generateFromPrefixesUrlACL(prefixes, currentuser);
        }
        private static StringCollection generateFromPrefixesUrlACL(List<string> prefixes, string UrlACLUser)
        {
            // set URLACL for defined prefixes and user  ie. "DOMAIN/user"
            try
            {


                //  get sslcert ipport, certhash, appid  if certhash or appid is empty or null then try to get default values
                if (string.IsNullOrWhiteSpace(UrlACLUser)) UrlACLUser = getCurrentUser();  // if empty -> get CertHASH from CERTIFICATES SELECTOR
                if (string.IsNullOrWhiteSpace(UrlACLUser)) return null;
                UrlACLUser=UrlACLUser.Replace("/","\\");
           
                StringCollection args = new StringCollection();

                // Add the server bindings to ACL :
                foreach (var prefix in prefixes)
                {
                    // delete pre existing urlacl...
                    args.Add("netsh http delete urlacl url=" + prefix);
                    // netsh http add urlacl url=http://+:80/MyUri user=DOMAIN\user
                    args.Add("netsh http add urlacl url=" + prefix + " user=" + UrlACLUser);
                }

                return args;

            }
            catch 
            {
                return null;
            }

        }

        private static StringCollection generateSSLCert(string ipport)
        {
            // use the appid from my application!!
            return generateSSLCert(ipport, selectCertHash(), getAppid());
        }
        private static StringCollection generateSSLCert(string ipport, string certhash)
        {
            return generateSSLCert(ipport, certhash, getAppid());
        }
        private static StringCollection generateSSLCert(string ipport, string certhash, string appid)
        {
            // setze für HTTPS  den PORT, CERTHASH und [APPID]= APPID des eigenen Programmes   
            //  get sslcert ipport, certhash, appid  if certhash or appid is empty or null then try to get default values
            if (string.IsNullOrWhiteSpace(certhash)) certhash = selectCertHash();  // if empty -> get CertHASH from CERTIFICATES SELECTOR
            if (string.IsNullOrWhiteSpace(certhash)) return null;

            if (string.IsNullOrWhiteSpace(appid)) appid = getAppid();     // if Empty -> get APP-ID from Server Application
            if (string.IsNullOrWhiteSpace(appid)) return null;

            if (string.IsNullOrWhiteSpace(ipport)) ipport = localIPAddress().ToString() + ":443";     // if Empty -> get APP-ID from Server Application
            if (string.IsNullOrWhiteSpace(ipport)) return null;

            //string arg = "/c  netsh http add sslcert ipport=0.0.0.0:8081 certhash=7c8bd5b934b62195fabcb6c3800a45ae9a3b58b6 appid={00112233-4455-6677-8899-121236722343}";
            StringCollection args = new StringCollection();

            // delete pre existing urlacl...
            args.Add("netsh http delete sslcert ipport=" + ipport);
            // add SSL Certificate
            args.Add("netsh http add sslcert ipport=" + ipport + " certhash=" + certhash + " appid={" + appid + "}");
            return args;
        }

        private static string selectCertHash()
        {
            StoreName storeName = new StoreName();
            storeName = StoreName.My;

            StoreLocation storeLocation = new StoreLocation();
            storeLocation = StoreLocation.LocalMachine;

            return selectCertHash(storeName, storeLocation, true);
        }
        private static string selectCertHash(StoreName storeName, StoreLocation storeLocation, Boolean OnlyTimeValid)
        {
            /* Code adapted from 
             * http://msdn.microsoft.com/de-de/library/system.security.cryptography.x509certificates.x509certificate2ui.aspx
            */
            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            X509Certificate2Collection collection = store.Certificates as X509Certificate2Collection;
            if (OnlyTimeValid == true)
            {
                X509Certificate2Collection fcollection = collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false) as X509Certificate2Collection;
            }
            X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(collection, "Zertifikate", "Wählen Sie ein Zertifikat aus der Liste", X509SelectionFlag.SingleSelection);

            foreach (X509Certificate2 x509 in scollection)
            {
                try
                {
                    byte[] rawdata = x509.RawData;
                    string x509hash = x509.GetCertHashString();
                    x509.Reset();
                    return x509hash;                // return Cert
                }
                catch (CryptographicException)
                {
                    return "Information für dieses Zertifikat kann nicht gelesen werden.";
                }
            }
            store.Close();

            return string.Empty;  //  "no Cert found/selected";

        }

        private static void runasCMD(StringCollection args)
        {
            string arg = "/c ";
            foreach (string a in args)
            {
                arg += a + " & ";
            }

            try
            {
                ProcessStartInfo p = new ProcessStartInfo("cmd.exe");
                p.UseShellExecute = true;
                p.CreateNoWindow = true;
                p.Verb = "runas";
                p.Arguments = arg;
                
                if (Process.Start(p) != null) return;    
            }
            catch (Exception ex)
            {
                Console.WriteLine("ServerIntegration - runasCMD ERROR:" + ex.Message);
            }


        }
        private static string convertHash(byte[] cert)
        {
            StringBuilder builder = new StringBuilder();

            foreach (byte b in cert)
            {
                builder.AppendFormat("{0:x2}", b);
            }

            return builder.ToString();
        }
        private static string getLocal_IP_WithPort(int port)
        {
            try
            {
                return localIPAddress().ToString() + ":" + port.ToString(); ;
            }
            catch
            {
                return string.Empty;
            }

        }
        private static string getAppid()
        {

            try
            {
                var assembly = typeof(webQL.program).Assembly;
                var attribute = assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0] as GuidAttribute;
                return attribute.Value;
            }
            catch
            {
                return string.Empty;
            }

        }
        private static string getCurrentUser()
        {

            // get current USER NAME 
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name;

        }
        private static IPAddress localIPAddress()
        {

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }


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


        #endregion




    }

    /// <summary>
    /// SHA256/SHA512 Hash Functions from String to Base64, Generator for Random Numbers and Random Strings of Numbers 
    /// </summary>
    public static class Crypto
    {
        
        /// <summary>
        /// HASH to Based64 Functions
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string hashToBase64_SHA256(string value)
        {
            return hashToBase64_SHA256(Encoding.UTF8.GetBytes(value));
        }
        public static string hashToBase64_SHA256(byte[] value)
        {
             try
            {

                if (value == null || value.Length==0) return string.Empty;
                SHA256Managed SHA256 = new System.Security.Cryptography.SHA256Managed();
                return Convert.ToBase64String(SHA256.ComputeHash(value));

            }
            catch
            {
                return string.Empty;
            }
        }
    
        public static string hashToBase64_SHA512(string value)
        {
            return hashToBase64_SHA512(Encoding.UTF8.GetBytes(value));
        }
        public static string hashToBase64_SHA512(byte[] value)
        {
            try
            {
                if (value == null || value.Length == 0) return string.Empty;
            
                //SHA512 Hash aus dem String berechnen. Dazu muss der string in ein Byte[]
                //zerlegt werden. Danach muss das Resultat wieder zurück in ein string.
                SHA512 sha512 = new SHA512CryptoServiceProvider();
                return Convert.ToBase64String(sha512.ComputeHash(value));

            }
            catch
            {
                return string.Empty;
            }

        }

        public static string hashToBase64_MD5(string value)
        {
            return hashToBase64_MD5(Encoding.UTF8.GetBytes(value));
        }
        public static string hashToBase64_MD5(byte[] value)
        {
            try
            {

                if (value == null || value.Length == 0) return string.Empty;
                MD5CryptoServiceProvider md5= new System.Security.Cryptography.MD5CryptoServiceProvider();
                return Convert.ToBase64String(md5.ComputeHash(value));

            }
            catch
            {
                return string.Empty;
            }
        }
    
        /// <summary>
        /// Generate a random Number or RandomString 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static string GenerateRandomNumberString(Int64 min, Int64 max)
        {
            return GenerateRandomNumber(min,max).ToString();
        }
        public static Int64 GenerateRandomNumber(Int64 min, Int64 max)
        {
            /*
             * Code from 
             * http://www.hofmann-robert.info/?p=360
             
             */

            RNGCryptoServiceProvider c = new RNGCryptoServiceProvider();
            // Ein integer64 benötigt 8 Byte
            byte[] randomNumber = new byte[8];
            // dann füllen wir den Array mit zufälligen Bytes
            c.GetBytes(randomNumber);
            // schließlich wandeln wir den Byte-Array in einen Integer um
            Int64 result = Math.Abs(BitConverter.ToInt64(randomNumber, 0));
            // da bis jetzt noch keine Begrenzung der Zahlen vorgenommen wurde,
            // wird diese Begrenzung mit einer einfachen Modulo-Rechnung hinzu-
            // gefügt
            return result % max + min;
        }       
       
    }


}

