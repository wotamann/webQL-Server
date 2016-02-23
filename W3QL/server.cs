using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;


namespace webQL
{

    public static class Server
    {
        
        #region Declaration

        private static HttpListener listener;
        private static Boolean listening = true;
        private readonly static int TaskListLengthToGC = 64;

        [ThreadStatic]
        public static configuration Configuration = null; // True -> Load JSON-Configuration 
        [ThreadStatic]
        public static MimeType MimeType = null; // True -> Load JSON-MimeTypes 
        [ThreadStatic]
        public static Router Router = null; // True -> Load JSON-Routes 
        
       #endregion

        #region Public Methods

        public static void LoadConfig(out Exception ex)
        {
            ex = null;
            try
            {
                // load Configuration from JSON File            
                Configuration = new configuration();
                Configuration.Load(out ex);
                if (ex != null) throw new Exception("WebQL.Config: " + configuration.FullPathConfiguration + " - Error:" + ex.Message);

                if (Server.Configuration.Parameters.LogMode!=null)
                {
                    // Set Logging Flags 
                    LogQL.WriteToLogFile = Server.Configuration.Parameters.LogMode.Contains("F");
                    LogQL.WriteToConsole = Server.Configuration.Parameters.LogMode.Contains("C");
                    LogQL.LogToRequest = Server.Configuration.Parameters.LogMode.Contains("Q");
                    LogQL.LogToResponse = Server.Configuration.Parameters.LogMode.Contains("S");

                }
                // load Routes Configuration from JSON File
                Router = new Router();
                Router.Load(out ex);

                if (ex != null) throw new Exception("Loading Routes.Config: " + Router.FullPathRoutes + " - Error:" + ex.Message);

                // load LoadMimeTypes  from JSON File
                MimeType = new MimeType();
                MimeType.Load(out ex);
                if (ex != null) throw new Exception("Loading MimeTypes.Config: " + MimeType.FullPathMimeTypes + " - Error:" + ex.Message);


                DbQuery.QueryList.LoadConfig(out ex);
                if (ex != null) throw new Exception("Loading DbQuery.Config: " + DbQuery.QueryList.FullPathConfigJSON + " - Error:" + ex.Message);
         

            }
            catch (Exception e)
            {
                ex = e;               
            }

        }

        // Check if HTTPListener can start
        public static Boolean isStartable(out Exception ex)
        {

            ex = null;
            try
            {
                // check OS 
                if (!HttpListener.IsSupported) throw new Exception("Minimal Windows 7 or Server 2003 is required.");

                // URI prefixes are required, for example "http://+:8080/index/".
                if (Server.Configuration.Parameters.Prefixes == null || Server.Configuration.Parameters.Prefixes.Count == 0) throw new Exception("No Prefixes found.");

                // Server.Arguments.Routes Defined Routes are required for example: JSON-OBJECT ->
                if (Router.Routes == null || Router.Routes.Count == 0) throw new Exception("No Routes found.");
                return true;
            }
            catch (Exception exc)
            {
                LogQL.LogServer("Server Error", "Can't start:" + exc.Message);
                ex = exc;
                return false;
            }

        }
        // Check if HTTPListener is listening
        public static bool isListening()
        {
            return listener!=null ? listener.IsListening : false; 
        }
        // Start Webserver       
        public static void Start(out Exception ex)
        {
            ex = null;
            try
            {
              
                LogQL.LogServer("Server", "Server starting...");

                isStartable(out ex);
                if (ex != null) throw ex;

                Task t=processRequestsAsync();  // MAIN LOOP normally you never get out here
            
                // Check for sufficient rights for Listening
                if (listening == false)
                {
                    string e = "Error: Maybe 1)WebQL already running e.g. after 'AutoStart on Boot' - 2)Bogus Declaration of Prefixes[] - 3)Insufficient Rights to listen. Try to 'Set Rights'";

                    LogQL.LogServer("Server Error", "Server listening: " + e);
                    throw new Exception(e);
                
                }

            }
            catch (Exception exc)
            {
                LogQL.LogServer("Server Error", "Server starting: " + exc.Message);
                ex = exc;
            }
            
        }
        // Stop Webserver   
        public static void Stop(out Exception ex)
        {

            ex = null;
            try
            {
                LogQL.LogServer("Server", "Server stopping... ");
                listening = false;


                LogQL.LogServer("Server", "Release Requests... ");
                HttpClient htc = new HttpClient();
                Task t = htc.GetStringAsync(Server.Configuration.DefaultUrl);
                Thread.Sleep(250);
            }
            catch (Exception e)
            {
                LogQL.LogServer("Server Error", "Server stopping: " + e.Message);
                ex = e;
            }
        }

        #endregion

        #region Main Loop processing the Requests 

        // MAIN LOOP Listening...     
        private static async Task processRequestsAsync()
        {
     
            // Listener START/CREATE
            if (listener == null)
            {
                try
                {
                    listener = new HttpListener();
                    foreach (var prefix in Server.Configuration.Parameters.Prefixes) listener.Prefixes.Add(prefix);
                    listener.IgnoreWriteExceptions = true;
                    listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                }
                catch 
                {
                    listener = null;
                    LogQL.LogServer("Server", "Check your Prefixes in Server-Config! The Declaration should look like this \"http://+:8081/\", \"http://*:8082/\" or \"https://+:4403/\"");                
                }
                
            }
            // Listener EXISTS but not STARTED
            if (listener != null && listener.IsListening == false)
            {
                try
                {
                    listener.Start();
                    listening = true;

                    LogQL.LogServer("Server", "Start listening...");

                }
                catch (HttpListenerException ex)
                {

                    listener.Abort();
                    listener = null;
                    listening = false;

                    LogQL.LogServer("Server Error", "Error to Start listening: " + ex.Message);

                }

            }
            // Listener EXISTS and is STARTED
            else
            {
                listening = true;
                LogQL.LogServer("Server", "I'm Listening...");

            }

            // -----------------------------------------------------------------------------------------------------------------------

            HttpListenerContext context = null;
            List<Task> TaskList = new List<Task>(128);  // holds all Tasks  

            try
            {


                // ----------------------------- MAIN LOOP WAITING FOR REQUEST and HANDLING RESPONSE ------------------------------
                while (listening == true)
                {
                    
                    // here your are waiting for an incoming Request andif you get the Request Context 
                    context = await listener.GetContextAsync();
                    
                    // get Responder and process Response
                    Responder resp = new Responder(context);

                    // do some garbage collection on your TaskList and remove all finished or canceled or faulted TASKS 
                    if (TaskList.Count > TaskListLengthToGC) TaskList.RemoveAll(t => (t.IsCanceled || t.IsCompleted || t.IsFaulted));
                    
                    // add processResponseAsync Result TASK to TASKLIST
                    TaskList.Add(resp.processResponseAsync());
                }

            }
            catch (Exception e)
            {
                LogQL.LogServer("Server Error","Request Loop: "+ e.Message);
                listening = false;
                listener = null;
            }

            // wait for completing all Tasks in Tasklist
            await Task.WhenAll(TaskList);
            // no pending Response - now you can save stop.listening
            listener.Stop();

            LogQL.LogServer("Server", "Server stopped...");

        }
          
       #endregion
       
    }

}
