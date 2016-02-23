using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security;
using System.Windows.Forms;
using System.Threading.Tasks;
using RethinkDb.Driver;

namespace webQL
{

  
    public partial class main : Form
    {
        public main()
        {
            InitializeComponent();
        }


        

        #region Declaration

        private bool serverRunning = false;
        private string defaultBrowser = "Firefox";
        private int truncateText = 100;  // truncate Label Text in Control
        private int truncateTooltip = 256;  // truncate Tooltiptext

        #endregion

      //  public static RethinkDB r = RethinkDB.r;
      //  public void can_connect()
      //  {

      //      Console.WriteLine("CAN CONNECT");

      //      var c = r.connection();

      //      Console.WriteLine("CAN 1");

      //      c.hostname("192.168.99.100");
      //      Console.WriteLine("CAN 2");

      //      c.port(RethinkDBConstants.DEFAULT_PORT);
      //      Console.WriteLine("CAN 3");

      //      c.timeout(60);
      //      Console.WriteLine("CAN 4");

      //      c.connect();

      //      Console.WriteLine("CAN CONNECT");

      ////      int result = r.random(1, 9).add(r.random(1, 9)).run<int>(c);
      //      Console.WriteLine(7);
      //  }
      //  private bool rethinkDB()
      //  {



      //      return true;
      //  }

        #region Private Methods

        private Exception init()
        {
            
            try
            {

                Exception ex = null;
                string txt;
                string tooltiptxt;

                // Load 4 Config Files from application path
                // ---
                // WebQL.config.json 
                // WebQL.routes.json 
                // WebQL.mimeTypes.json 
                // DBQuery.config.json
                //
                Server.LoadConfig(out ex);
                if (ex != null) throw new Exception("Loading Configuration: " + ex.Message);

               
                // here you can programmatically overwrite some DbQuery Settings from Configuration File
                DbQuery.Settings.DefaultInsertValues.ForDateTime=DateTime.Now;
                //DbQuery.Settings.DefaultColumnFormat.ForDateTime = "d.M.yyyy";        
                
                //DbQuery.QueryItem i=new DbQuery.QueryItem("test");
                //i.AllowedMethods="CRUD";
                //i.Sql = "SELECT * FROM patient";                
                //DbQuery.QueryList.Add(i);

                // modify LogQL
                //LogQL.WriteToConsole = true;
                //LogQL.LogServer = ((h, m) => LogQL.Log(h, m));

                
                // try Server -> Settings IMPORTANT dont delete!
                Server.isStartable(out ex);
                if (ex != null) throw new Exception("Error starting Server: " + ex.Message);
                
                // SERVER STEP 1-7
                // 1)
                txt = "Edit Server Configuration - ";
                foreach (var pre in Server.Configuration.Parameters.Prefixes) { txt += pre + " "; }
                displayText(serverconfig, txt,"| Declare 'Rootpath', 'Prefixes', 'LogMode', 'UrlACL', 'SSLCert' here");

                // 2)
                txt= "Edit (" + Router.Routes.Count + ") Routes:";
                tooltiptxt = " | ";
                foreach (var item in Router.Routes) { tooltiptxt += item.Name + " | "; }
                displayText(routes, txt,tooltiptxt);
                          
                txt = "Edit (" + DbQuery.QueryList.Count + ") DBQuery-Items:";
                tooltiptxt = " | ";
                foreach (var item in DbQuery.QueryList.List) { tooltiptxt += item.Key + " | "; }
                displayText(dbquery, txt, tooltiptxt); 

                // 3)
                txt = "Explore Rootpath: " + Server.Configuration.Parameters.RootPath;
                tooltiptxt = " | Declare Rootpath in 'WebQl.Config.Json' -> 1) Edit Server Configuration";
                displayText(rootpath, txt, tooltiptxt); 
                
                // 4)
                txt = "Set Rights";
                txt += (string.IsNullOrWhiteSpace(Server.Configuration.Parameters.UrlACLUser) ? "" : " | UrlACL=" + Server.Configuration.Parameters.UrlACLUser);
                tooltiptxt = (string.IsNullOrWhiteSpace(Server.Configuration.Parameters.SSLCertIpport) ? "" : " | SSL-IP=" + Server.Configuration.Parameters.SSLCertIpport);
                tooltiptxt += (string.IsNullOrWhiteSpace(Server.Configuration.Parameters.SSLCertHash) ? "" : " | CertHash=" + Server.Configuration.Parameters.SSLCertHash);
                tooltiptxt += (string.IsNullOrWhiteSpace(Server.Configuration.Parameters.SSLCertAppId) ? "" : " | AppID=" + Server.Configuration.Parameters.SSLCertAppId);
                tooltiptxt += " | Valid Host IPs: " + getHostIPAdresses();
                displayText(setrights, txt, tooltiptxt); 
                
                // 6)
                               
                //txt = "Open " + defaultBrowser.ToUpper() + " with '" + Server.Configuration.DefaultUrl + "'";
                //tooltiptxt = " | This Default URL correlates with the first Item in Listener Prefixes[]";
                //displayText(browser, txt, tooltiptxt);
                displayBrowser();
                                
                // 7)
                txt = "Show Log";
                tooltiptxt=" | '" + LogQL.LogFilePath + "' - LogMode: '" + Server.Configuration.Parameters.LogMode + "'";
                displayText(showLogFile, txt, tooltiptxt);

                txt = "Clear Log...";
                tooltiptxt = " | File: '" + LogQL.LogFilePath + "'";
                displayText(clearLogFile, txt, tooltiptxt);

                txt = "Show Directory";
                tooltiptxt = " | Path: '" + Path.GetDirectoryName(LogQL.LogFilePath) +"'";
                displayText(showDirLog, txt, tooltiptxt);


               
                return ex;

            }
            catch (Exception ex)
            {
                LogQL.LogServer("ERR", "Server Init: " + ex.Message);
                displayError(ex);
                return ex;
            }
            
        }
        
        private bool start()
        {

            try
            {
                Exception ex;

                ex=init();  // init server and read all files again !!
                if (ex != null) throw new Exception("Error initing Server: " + ex.Message);

                
                Server.Start(out ex);
                if (ex != null) throw new Exception("Error starting Server: " + ex.Message);

                return true;  
            }
            catch (Exception ex)
            {
                LogQL.LogServer("ERR", ex.Message);
                displayError(ex);
                serverRunning = false;
                                
                return false;
            }
            

        }
        private bool stop()
        {
            try
            {
                Exception ex;
                Server.Stop(out ex);
                if (ex != null) throw new Exception("Error stopping Server: " + ex.Message);
                return false;  
            }
            catch (Exception ex)
            {
                LogQL.LogServer("ERR", ex.Message);
                displayError(ex);
                serverRunning = false;
                return false;
            }
        }
        private bool toggleStartStop()
        {
            autoStartStop();

            serverRunning = !serverRunning;
            StatusInfoBar.ForeColor = Color.White;
            StatusInfoBar.BackColor = serverRunning ? Color.DarkGreen: Color.Maroon;            
            restarter.Text = serverRunning ? "Stop":"Start";
            displayText(StatusInfoBar, restarter.Text, " Server");

            // bool a= serverRunning ? Tools.Ganymed.Cbird.StartSynchronize() : Tools.Ganymed.Cbird.StopSynchronize();
            return serverRunning ? start():stop();
       
        }

        private void autoStartStop()
        {
            if (timerDelay.Enabled == false) return;

            timerDelay.Stop();
            
            StatusInfoBar.ForeColor = Color.White;
            StatusInfoBar.BackColor = Color.Chocolate;
            StatusInfoBar.Text = "Automatic starting canceled... Click to start the Server!";

        }
        private void displayError(Exception ex)
        {
            autoStartStop();

            StatusInfoBar.BackColor = Color.Chocolate;
            StatusInfoBar.Text = ex.Message.Truncate(truncateText);
            toolTip.SetToolTip(StatusInfoBar, ex.Message);

            // on error bring up the form from taskbar
            this.WindowState = FormWindowState.Normal;
            this.Show();

        }

        #endregion
        
        #region Form Button Events (1-7)
        // 1
        // Open Server.Config
        private void editServerConfiguration(object sender, EventArgs e)
        {
        
           procStart("explorer.exe", configuration.FullPathConfiguration);
        }
        
        // 2
        // Edit ROUTES
        private void editRoutes(object sender, EventArgs e)
        {
            procStart("explorer.exe", Server.Router.FullPathRoutes);
        }
        // Open DBQuery.Config 
        private void editDBQueryConfiguration(object sender, EventArgs e)
        {
            procStart("explorer.exe", DbQuery.QueryList.FullPathConfigJSON);
        }        
        
        // 3
        // Show Rootpath in Explorer and Checkbox Watch Rootpath
        private void showRootpath(object sender, EventArgs e)
        {
            string p = Server.Configuration.Parameters.RootPath;
            if (p == null) { procStart("explorer.exe", p); return; }

            procStart("explorer.exe", System.IO.Path.GetFullPath(p)); // @Server.ServerArguments.RootPath);
        } 
        private void watchEnable(object sender, EventArgs e)
        {
           //CheckBox c = (CheckBox)sender;
           //Watcher.EnableWatching = c.Checked;
           // changed 22.2.2106  before YAHOO CSS integrat   
        }

        // 4
        // Set URL-ACL (Admin Rights)
        private void setACL(object sender, EventArgs e)
        {
            autoStartStop();  // autostart TIMER abschalten            
            // SET ACL for LISTENING
            ServerIntegration.setUrlACL(Server.Configuration.Parameters.Prefixes, Server.Configuration.Parameters.UrlACLUser);
        }
        // Set SSL-Certificate from Certificate Selector (Admin Rights)
        private void setSLL(object sender, EventArgs e)
        {
            autoStartStop();  // autostart TIMER abschalten
            //  set sslcert ipport, certhash, appid  if not preset try to get default value 
            ServerIntegration.setSSLCert(Server.Configuration.Parameters.SSLCertIpport, Server.Configuration.Parameters.SSLCertHash, Server.Configuration.Parameters.SSLCertAppId);            
        }
        // Set URL-ACL with Current USER and SSL-Certificate from Certificate Selector(Admin Rights)
        private void setAllRights(object sender, MouseEventArgs e)
        {

            autoStartStop();  // autostart TIMER abschalten
            ServerIntegration.setRights(Server.Configuration.Parameters.SSLCertIpport, Server.Configuration.Parameters.SSLCertHash, Server.Configuration.Parameters.SSLCertAppId, Server.Configuration.Parameters.Prefixes, Server.Configuration.Parameters.UrlACLUser);            
        }
        // Set AUTO RUN in Registry (needs to run WebQL as Administrator)
        private void registerAutoRun(object sender, EventArgs e)
        {

            if (ServerIntegration.isCurrentAdmin() == false)
            {
                MessageBox.Show("Changing 'AutoRun' Configuration needs Administrator-Rights. Start webQL with 'Runas Administrator' to change Autostart-Configuration ", "'No Administrator Rights to Edit 'AutoRun'", MessageBoxButtons.OK);
                return;
            }
            autoStartStop();  // autostart TIMER abschalten     

            // Register TASK SCHEDULER for starting webQL on BOOT (needs to run WebQL as Administrator)
            if (ServerIntegration.RegisterInStartupAfterLoginCheck() == false) registerScheduleTask();

            // Set AUTO RUN in Registry (needs to run WebQL as Administrator)
            if (ServerIntegration.RegisterScheduledStartWebQLCheck() == false) registerAutoRun();

        }
      
        // 5
        // Toggle START/STOP
        private void StartStop(object sender, EventArgs e)
        {

            toggleStartStop();

            string t=toolTip.GetToolTip(StatusInfoBar);

            if (t.Contains("WebQL.Config:") == true) { this.editServerConfiguration(null, null); return; }
            if (t.Contains("Routes.Config:") == true) { this.editRoutes(null, null); return; }
            if (t.Contains("MimeTypes.Config:") == true) { this.editMimeTypes(null, null); return; }
            if (t.Contains("DbQuery.Config:") == true) { this.editDBQueryConfiguration(null, null); return; } 
 
        }               
        
        // 6
        // Open Browser
        private void openBrowser(object sender, EventArgs e)
        {

            Button b = (Button)sender;

            if (b.Name != "browser") defaultBrowser = b.Name;

            displayBrowser();
         
            procStart(defaultBrowser + ".exe", Server.Configuration.DefaultUrl);
         
        }                       
        
        // 7
        // Show the LogFile
        private void showLog(object sender, EventArgs e)
        {
            bool f = System.IO.File.Exists(LogQL.LogFilePath);

            if (f==false)
            {
                MessageBox.Show("The LogFile\t\n\t\n" + LogQL.LogFilePath + "\t\n\t\ndoesn't exist! Check 'LogMod' Item in your ServerConfiguration.", "LogFile", MessageBoxButtons.OK);
                procStart("explorer.exe", LogQL.LogPath); 
                return;
            }
            procStart("explorer.exe", LogQL.LogFilePath);
        }

        private void showDirectoryLog(object sender, EventArgs e)
        {
    
            procStart("explorer.exe", Path.GetDirectoryName(LogQL.LogFilePath));
        }

        private void clearLog(object sender, EventArgs e)
        {
            autoStartStop();  // autostart TIMER abschalten
            DialogResult dialogResult = MessageBox.Show(LogQL.LogFilePath + "\t\n\t\nDelete permanent the current LogFile?", "Delete LogFile", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                LogQL.Clear();
            }
        }

        #endregion
        
        #region Some other Methods and Events
        // private methods

        private void displayText(Control ctrl, string txt, string tooltiptxt)
        {
            ctrl.Text=txt.Truncate(truncateText);

            if (tooltiptxt != null) {
                txt += tooltiptxt;
                toolTip.SetToolTip(ctrl, txt.Truncate(truncateTooltip));
            }
        }

        private string getHostIPAdresses()
        {
            string a = " ";
            System.Net.IPAddress[] IPadressList = System.Net.Dns.GetHostEntry("").AddressList;
            foreach (var item in IPadressList)
            {
                a += item.ToString() + " | ";
            }
            return a;
        }
        private void procStart(string exe, string args)
        {
            try
            {
                autoStartStop();  // autostart TIMER abschalten
                var startInfo = new ProcessStartInfo(exe, args);
                Process.Start(startInfo);
            }            
            catch (Exception err)
            {
                MessageBox.Show(err.Message + "\t\n\t\nYou can't start " + exe);
            }

        }

        // diverse Events ---------------------------------------------------------------------------------
        private void formLoading(object sender, EventArgs e)
        {
            init();
        }
        private void formClosing(object sender, FormClosingEventArgs e)
        {

            //if (MessageBox.Show("Sie wollen den WebQL Server beenden? ", "WebQL Server beenden", MessageBoxButtons.OKCancel,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2) != DialogResult.OK)
            //{
            //    e.Cancel = true;
            //    return;
            //}   
                      
            this.notifyIcon.Visible = false;
            this.notifyIcon.Dispose();
            Exception ex;
            Server.Stop(out ex);

        }
        private void activating(object sender, EventArgs e)
        {

            this.ShowInTaskbar = false;  // Removes the application from the taskbar
            this.notifyIcon.Visible = true;
            this.WindowState = FormWindowState.Normal;

        }
        private void notifyClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        private void resize(object sender, EventArgs e)
        {
            notifyIcon.BalloonTipTitle = "WebQL";
            notifyIcon.BalloonTipText = "Server has started successfully and can be found here.";
            notifyIcon.Visible = true;

            if (FormWindowState.Minimized == this.WindowState)
            {
                this.Hide();
                notifyIcon.ShowBalloonTip(1000);
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                this.Show();
                notifyIcon.Visible = true;

            }
        }
        private void tickDelay(object sender, EventArgs e)
        {
            // control time by setting timerDelay.Interval and setting StatusInfoBar.Text="|||||||||||||||"
          
            if (StatusInfoBar.Text.Length<1)
            {
                timerDelay.Stop();
                this.WindowState = FormWindowState.Minimized;
                
                // after timerDelay.Interval -> start and minimize                
                toggleStartStop();
               

                return;
            }

            //   shows on display the len-1 shrinking bar of ||||||||||||||||||   
           StatusInfoBar.Text = StatusInfoBar.Text.Substring(0,StatusInfoBar.Text.Length-1);


        }

        // Set AUTO RUN in Registry (needs to run WebQL as Administrator)
        private void registerAutoRun()
        {



            try
            {

                if (ServerIntegration.RegisterInStartupAfterLoginCheck() == false)
                {
                    DialogResult dialogResult = MessageBox.Show("Register 'WebQL' to 'AutoRun'. A Login to an User-Account is required to autostart 'webQL'", "'AutoRun'-Registration", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        ServerIntegration.RegisterInStartupAfterLogin(true); // Add AutoRun
                        MessageBox.Show("The 'AutoRun'-Registration was successful created.");
                    }
                }
                else
                {
                    DialogResult dialogResult = MessageBox.Show("UnRegister WebQL 'AutoRun'", "'AutoRun'-UnRegistration", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        ServerIntegration.RegisterInStartupAfterLogin(false); // Delete AutoRun
                        MessageBox.Show("The 'AutoRun'-Registration was successful deleted.");
                    }
                }

            }
            catch (SecurityException err)
            {
                MessageBox.Show(err.Message + "\t\n\t\nThe 'AutoRun'-Registration needs sufficient Rights. Try to restart WebQL and runAs Administrator.", "'AutoRun' Registration");
            }
            catch (UnauthorizedAccessException err)
            {
                MessageBox.Show(err.Message + "\t\n\t\nThe 'AutoRun'-Registration needs sufficient Rights. Try to restart WebQL and runAs Administrator.", "'AutoRun' Registration");
            }
            catch (Exception err)
            {

                MessageBox.Show(err.Message + "\t\n\t\nPlease inform your Administrator! Try to restart WebQL and runAs Administrator.", "'AutoRun' Registration");

            }

        }
        // Register TASK SCHEDULER for starting webQL on BOOT (needs to run WebQL as Administrator)
        private void registerScheduleTask()
        {

            try
            {

                if (ServerIntegration.RegisterScheduledStartWebQLCheck() == false)
                {
                    DialogResult dialogResult = MessageBox.Show("Create a autostarting Task to run 'WebQL' after Booting. No Login for starting 'webQL' is required", "'Starter Task'-Registration", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        ServerIntegration.RegisterScheduledStartWebQL(true); // Add AutoRun
                        MessageBox.Show("The 'Starter Task'-Registration was successful created.");
                    }
                }
                else
                {
                    DialogResult dialogResult = MessageBox.Show("Delete Task starting 'WebQL' after Boot. No Login required", "'Starter Task'-UnRegistration", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        ServerIntegration.RegisterScheduledStartWebQL(false); // Delete AutoRun
                        MessageBox.Show("The 'Starter Task'-Registration was successful deleted.");
                    }
                }

            }
            catch (SecurityException err)
            {
                MessageBox.Show(err.Message + "\t\n\t\nThe 'Starter Task'-Registration needs sufficient Rights. Try to restart WebQL and runAs Administrator.", "'Starter Task' Registration");
            }
            catch (UnauthorizedAccessException err)
            {
                MessageBox.Show(err.Message + "\t\n\t\nThe 'Starter Task'-Registration needs sufficient Rights. Try to restart WebQL and runAs Administrator.", "'Starter Task' Registration");
            }
            catch (Exception err)
            {

                MessageBox.Show(err.Message + "\t\n\t\nPlease inform your Administrator! Try to restart WebQL and runAs Administrator", "'Starter Task' Registration");

            }

        }
       

        // Edit WebQL.mimeTypes.json
        private void editMimeTypes(object sender, EventArgs e)
        {
            procStart("explorer.exe", MimeType.FullPathMimeTypes);
        }

        private void displayBrowser()
        {
            string txt = "Open " + defaultBrowser.ToUpper() + " with '" + Server.Configuration.DefaultUrl + "'";
            string tooltiptxt = " | This Default URL correlates with the first Item in Listener Prefixes[]";
            displayText(browser, txt, tooltiptxt);
        }



        // Browser Test xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        private void testBrowser(string browser, string defaultUrl)
        {

            List<string> urlList = getUrlList();
            foreach (var url in urlList)
            {
                procStart(browser, url);
            }


        }
        // List of URLS used by testBrowser xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        private List<string> getUrlList()
        {
            string du = Server.Configuration.DefaultUrl;
            List<string> urlList = new List<string>();
            urlList.Add(du + "js/app.js");
            urlList.Add(du + "js/js/app.js");
            urlList.Add(du + "js/js/app.js");
            urlList.Add(du + "app.js");
            urlList.Add(du + "js/js/js/app.js");
            urlList.Add(du + "js/js/app.js");
            urlList.Add(du + "js/js/app.js");
            urlList.Add(du + "app.js");
            urlList.Add(du + "js/js/js/app.js");


            //urlList.Add(du + "mod/showfiles?path=c:\\windows\\&color=74E");
            //urlList.Add(du + "mod/showfiles?path=c:\\windows\\System32\\&color=89F");
            //urlList.Add(du + "mod/showfiles?path=c:\\windows\\&color=37F");
            //urlList.Add(du + "rewrite/path=c:\\windows\\system\\&color=6F2");
            //urlList.Add(du + "rewrite/path=c:\\windows\\&color=7D5");
            //urlList.Add(du + "m/showfiles?path=c:\\windows\\&color=F82");
            //urlList.Add(du + "m/showfiles?path=c:\\windows\\&color=e53");
            //urlList.Add(du + "virtual/wüste.jpg");
            //urlList.Add(du + "virtual/wörter.pdf");
            //urlList.Add(du + "virtual/wüste/test.pdf");
            //urlList.Add(du + "virtualnoroot/wüste.jpg");
            //urlList.Add(du + "virtualnoroot/wörter.pdf");
            //urlList.Add(du + "virtualnoroot/wüste/test.pdf");
            //urlList.Add(du + "mod/basic?header=1&Wörter=33&Länge=20");
            //urlList.Add(du + "mod/basic?header=2&Wörter=33&Länge=20");
            //urlList.Add(du + "mod/lösung/66/basimax?header=3&Wörter=Basimax&Länge=20");
            //urlList.Add(du + "mod/showfiles?path=c:\\windows\\&color=3a5");
            //urlList.Add(du + "m/basic?header=3&Wörter=33&Länge=20");
            //urlList.Add(du + "m/lösung/66/basimax?header=4&Wörter=Basimax&Länge=20");
            //urlList.Add(du + "m/basic?rewrite=mtomod&header=5&Wörter=33&Länge=10");
            //urlList.Add(du + "m/showfiles?path=c:\\users\\&color=33a");
            //urlList.Add(du + "redirect/#q=redirect");
            //urlList.Add(du + "redirect/#q=url%20test");

            //urlList.Add(du +"basicwefwef/rhrth?header=2&col=12");
            //urlList.Add(du +"basicwefwef/rhrth?header=4&col=12");
            //urlList.Add(du +"basicwefwef/rhrth?header=1&col=12");
            //urlList.Add(du +"basicwefwef/rhrth?header=2&col=12");
            //urlList.Add(du +"basicwefwef/rhrth?header=3&col=12");
            //urlList.Add(du +"basicwefwef/rhrth?header=5&col=12");
            //urlList.Add(du +"basicwefwef/rhrth?header=3&col=12");

            //urlList.Add(du + "/charts");
            //urlList.Add(du + "/charts");
            //urlList.Add(du + "/charts");
            //urlList.Add(du + "/charts");
            //urlList.Add(du + "/charts");
            //urlList.Add(du + "/charts");
            //urlList.Add(du + "/charts");
                      
            //urlList.Add(du + "befund/basicxxy");
            //urlList.Add(du + "js/rüge#!</yyy.xxxx");
            //urlList.Add(du + "html/yyy.xxxx");
            //urlList.Add(du + "/newsticker//meldung/NSA-Ueberwachungsskandal-Von-NSA-GCHQ-BND-PRISM-Tempora-XKeyScore-und-dem-Supergrundrecht-was-bisher-geschah-1958399.html");  // with quwery
            //urlList.Add(du + "?/test.pdf?testh%C3%B6%C3%BCber=123");  // with quwery
            //urlList.Add(du + "shut/down/180759xxx");
            //urlList.Add(du + "css/app.css");
            //urlList.Add(du + "js/app.js");
            //urlList.Add(du + "img/wörter.pdf");
            //urlList.Add(du + "wüste/wüste.jpg");
            //urlList.Add(du + "wüste/test.pdf?testh%C3%B6%C3%BCber=123");  // with quwery
            //urlList.Add(du + "befund/32895_040213144055.pdf");
            //urlList.Add(du + "befund/00030_230102075743.jpg");
            //urlList.Add(du + "befund/00001_270199161429.gif");

            //urlList.Add("http://localhost/");
            //urlList.Add("http://localhost:8081");
            //urlList.Add("http://localhost:8081/");
            //urlList.Add("http://localhost:8081/");
            //urlList.Add("http://localhost:8082/");

            return urlList;
        }
        
        #endregion

      
   
      
        
    }
}
