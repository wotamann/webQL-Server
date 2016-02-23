namespace webQL
{
    partial class main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(main));
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.timerDelay = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.StatusInfoBar = new System.Windows.Forms.Label();
            this.browser = new System.Windows.Forms.Button();
            this.serverconfig = new System.Windows.Forms.Button();
            this.restarter = new System.Windows.Forms.Button();
            this.setrights = new System.Windows.Forms.Button();
            this.dbquery = new System.Windows.Forms.Button();
            this.rootpath = new System.Windows.Forms.Button();
            this.safari = new System.Windows.Forms.Button();
            this.opera = new System.Windows.Forms.Button();
            this.iexplore = new System.Windows.Forms.Button();
            this.chrome = new System.Windows.Forms.Button();
            this.firefox = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.showLogFile = new System.Windows.Forms.Button();
            this.clearLogFile = new System.Windows.Forms.Button();
            this.routes = new System.Windows.Forms.Button();
            this.showDirLog = new System.Windows.Forms.Button();
            this.watchFlag = new System.Windows.Forms.CheckBox();
            this.minifyJS = new System.Windows.Forms.CheckBox();
            this.minifyCSS = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "WebSQL-Server";
            this.notifyIcon.Visible = true;
            this.notifyIcon.Click += new System.EventHandler(this.notifyClick);
            // 
            // timerDelay
            // 
            this.timerDelay.Enabled = true;
            this.timerDelay.Tick += new System.EventHandler(this.tickDelay);
            // 
            // StatusInfoBar
            // 
            this.StatusInfoBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.StatusInfoBar.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.StatusInfoBar.ForeColor = System.Drawing.Color.DimGray;
            this.StatusInfoBar.Location = new System.Drawing.Point(19, 111);
            this.StatusInfoBar.Name = "StatusInfoBar";
            this.StatusInfoBar.Padding = new System.Windows.Forms.Padding(3);
            this.StatusInfoBar.Size = new System.Drawing.Size(497, 25);
            this.StatusInfoBar.TabIndex = 20;
            this.StatusInfoBar.Text = "|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||" +
    "||||||||||||||||||||||||||||||||||||||";
            this.StatusInfoBar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.StatusInfoBar, "Start/Stop Server");
            this.StatusInfoBar.Click += new System.EventHandler(this.StartStop);
            // 
            // browser
            // 
            this.browser.BackColor = System.Drawing.Color.WhiteSmoke;
            this.browser.FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
            this.browser.FlatAppearance.BorderSize = 0;
            this.browser.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.browser.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.browser.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.browser.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browser.ForeColor = System.Drawing.Color.DimGray;
            this.browser.Location = new System.Drawing.Point(19, 138);
            this.browser.Name = "browser";
            this.browser.Size = new System.Drawing.Size(288, 25);
            this.browser.TabIndex = 5;
            this.browser.Text = "http://default.url";
            this.browser.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.browser, "Open the first in Prefixes[] listening Port with the Default-Browser");
            this.browser.UseVisualStyleBackColor = false;
            this.browser.Click += new System.EventHandler(this.openBrowser);
            // 
            // serverconfig
            // 
            this.serverconfig.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.serverconfig.FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
            this.serverconfig.FlatAppearance.BorderSize = 0;
            this.serverconfig.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.serverconfig.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.serverconfig.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.serverconfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.serverconfig.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.serverconfig.ForeColor = System.Drawing.Color.DimGray;
            this.serverconfig.Location = new System.Drawing.Point(19, 3);
            this.serverconfig.Name = "serverconfig";
            this.serverconfig.Size = new System.Drawing.Size(547, 25);
            this.serverconfig.TabIndex = 12;
            this.serverconfig.Text = "Edit Server Configuration...";
            this.serverconfig.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.serverconfig, "Edit Server-Configuration (Prefixes, LogMode, UrlACL, SSL, Rootpath)");
            this.serverconfig.UseVisualStyleBackColor = false;
            this.serverconfig.Click += new System.EventHandler(this.editServerConfiguration);
            // 
            // restarter
            // 
            this.restarter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.restarter.FlatAppearance.BorderColor = System.Drawing.Color.LightGray;
            this.restarter.FlatAppearance.BorderSize = 0;
            this.restarter.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.restarter.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.restarter.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.restarter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.restarter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.restarter.ForeColor = System.Drawing.Color.DimGray;
            this.restarter.Location = new System.Drawing.Point(516, 111);
            this.restarter.Name = "restarter";
            this.restarter.Size = new System.Drawing.Size(50, 25);
            this.restarter.TabIndex = 18;
            this.restarter.Text = "Start";
            this.restarter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.restarter, "Start/Stop Server");
            this.restarter.UseVisualStyleBackColor = false;
            this.restarter.Click += new System.EventHandler(this.StartStop);
            // 
            // setrights
            // 
            this.setrights.BackColor = System.Drawing.Color.WhiteSmoke;
            this.setrights.FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
            this.setrights.FlatAppearance.BorderSize = 0;
            this.setrights.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.setrights.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.setrights.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.setrights.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.setrights.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.setrights.ForeColor = System.Drawing.Color.DimGray;
            this.setrights.Location = new System.Drawing.Point(19, 84);
            this.setrights.Name = "setrights";
            this.setrights.Size = new System.Drawing.Size(457, 25);
            this.setrights.TabIndex = 24;
            this.setrights.Text = "Set Rights";
            this.setrights.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.setrights, "Set UrlACL and SSL Rights, which are defined in the Server-Configuration File.  ");
            this.setrights.UseVisualStyleBackColor = false;
            this.setrights.MouseClick += new System.Windows.Forms.MouseEventHandler(this.setAllRights);
            // 
            // dbquery
            // 
            this.dbquery.BackColor = System.Drawing.Color.WhiteSmoke;
            this.dbquery.FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
            this.dbquery.FlatAppearance.BorderSize = 0;
            this.dbquery.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.dbquery.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.dbquery.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.dbquery.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dbquery.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dbquery.ForeColor = System.Drawing.Color.DimGray;
            this.dbquery.Location = new System.Drawing.Point(293, 30);
            this.dbquery.Name = "dbquery";
            this.dbquery.Size = new System.Drawing.Size(273, 25);
            this.dbquery.TabIndex = 25;
            this.dbquery.Text = "Edit DBQuery Configuration ...";
            this.dbquery.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.dbquery, "Edit the DBQuery File, which defines the Access to your MS-SQL Database");
            this.dbquery.UseVisualStyleBackColor = false;
            this.dbquery.Click += new System.EventHandler(this.editDBQueryConfiguration);
            // 
            // rootpath
            // 
            this.rootpath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.rootpath.FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
            this.rootpath.FlatAppearance.BorderSize = 0;
            this.rootpath.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.rootpath.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.rootpath.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.rootpath.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rootpath.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rootpath.ForeColor = System.Drawing.Color.DimGray;
            this.rootpath.Location = new System.Drawing.Point(19, 57);
            this.rootpath.Name = "rootpath";
            this.rootpath.Size = new System.Drawing.Size(547, 25);
            this.rootpath.TabIndex = 30;
            this.rootpath.Text = "Explore Rootpath...";
            this.rootpath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.rootpath, "Explore the \'Rootpath\' of your Webproject ");
            this.rootpath.UseVisualStyleBackColor = false;
            this.rootpath.Click += new System.EventHandler(this.showRootpath);
            // 
            // safari
            // 
            this.safari.BackColor = System.Drawing.Color.WhiteSmoke;
            this.safari.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.safari.FlatAppearance.BorderSize = 0;
            this.safari.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.safari.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.safari.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.safari.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.safari.ForeColor = System.Drawing.Color.DimGray;
            this.safari.Location = new System.Drawing.Point(411, 138);
            this.safari.Name = "safari";
            this.safari.Size = new System.Drawing.Size(53, 25);
            this.safari.TabIndex = 31;
            this.safari.Text = "Safari";
            this.toolTip.SetToolTip(this.safari, "Open the Default URL with Safari.  Refresh with \'Click+CTRL\' ");
            this.safari.UseVisualStyleBackColor = false;
            this.safari.Click += new System.EventHandler(this.openBrowser);
            // 
            // opera
            // 
            this.opera.BackColor = System.Drawing.Color.WhiteSmoke;
            this.opera.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.opera.FlatAppearance.BorderSize = 0;
            this.opera.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.opera.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.opera.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.opera.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.opera.ForeColor = System.Drawing.Color.DimGray;
            this.opera.Location = new System.Drawing.Point(462, 138);
            this.opera.Name = "opera";
            this.opera.Size = new System.Drawing.Size(53, 25);
            this.opera.TabIndex = 32;
            this.opera.Text = "Opera";
            this.toolTip.SetToolTip(this.opera, "Open the Default URL with Opera.  Refresh with \'Click+CTRL\' ");
            this.opera.UseVisualStyleBackColor = false;
            this.opera.Click += new System.EventHandler(this.openBrowser);
            // 
            // iexplore
            // 
            this.iexplore.BackColor = System.Drawing.Color.WhiteSmoke;
            this.iexplore.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.iexplore.FlatAppearance.BorderSize = 0;
            this.iexplore.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.iexplore.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.iexplore.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.iexplore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.iexplore.ForeColor = System.Drawing.Color.DimGray;
            this.iexplore.Location = new System.Drawing.Point(513, 138);
            this.iexplore.Name = "iexplore";
            this.iexplore.Size = new System.Drawing.Size(53, 25);
            this.iexplore.TabIndex = 33;
            this.iexplore.Text = "IE";
            this.toolTip.SetToolTip(this.iexplore, "Open the Default URL with Internet Explorer.  Refresh with \'Click+CTRL\' ");
            this.iexplore.UseVisualStyleBackColor = false;
            this.iexplore.Click += new System.EventHandler(this.openBrowser);
            // 
            // chrome
            // 
            this.chrome.BackColor = System.Drawing.Color.WhiteSmoke;
            this.chrome.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.chrome.FlatAppearance.BorderSize = 0;
            this.chrome.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.chrome.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.chrome.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.chrome.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chrome.ForeColor = System.Drawing.Color.DimGray;
            this.chrome.Location = new System.Drawing.Point(359, 138);
            this.chrome.Name = "chrome";
            this.chrome.Size = new System.Drawing.Size(53, 25);
            this.chrome.TabIndex = 34;
            this.chrome.Text = "Chrome";
            this.toolTip.SetToolTip(this.chrome, "Open the Default URL with Chrome. Refresh with \'Click+CTRL\' ");
            this.chrome.UseVisualStyleBackColor = false;
            this.chrome.Click += new System.EventHandler(this.openBrowser);
            // 
            // firefox
            // 
            this.firefox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.firefox.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.firefox.FlatAppearance.BorderSize = 0;
            this.firefox.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.firefox.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.firefox.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.firefox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.firefox.ForeColor = System.Drawing.Color.DimGray;
            this.firefox.Location = new System.Drawing.Point(307, 138);
            this.firefox.Name = "firefox";
            this.firefox.Size = new System.Drawing.Size(53, 25);
            this.firefox.TabIndex = 35;
            this.firefox.Text = "Firefox";
            this.toolTip.SetToolTip(this.firefox, "Open the Default URL with Firefox. Refresh with \'Click+CTRL\' ");
            this.firefox.UseVisualStyleBackColor = false;
            this.firefox.Click += new System.EventHandler(this.openBrowser);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.button1.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.button1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.button1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.ForeColor = System.Drawing.Color.DimGray;
            this.button1.Location = new System.Drawing.Point(476, 84);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 25);
            this.button1.TabIndex = 36;
            this.button1.Text = "Set AutoStart";
            this.toolTip.SetToolTip(this.button1, "You have 2 Options: 1) Set an Autostart Task (No Login required) or 2) Create Aut" +
        "ostart in \'Registry ...\\CurrentVersion\\Run\' (Login is required and you need Admi" +
        "n-Rights for set this Autostart). ");
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.registerAutoRun);
            // 
            // showLogFile
            // 
            this.showLogFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.showLogFile.FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
            this.showLogFile.FlatAppearance.BorderSize = 0;
            this.showLogFile.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.showLogFile.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.showLogFile.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.showLogFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.showLogFile.ForeColor = System.Drawing.Color.DimGray;
            this.showLogFile.Location = new System.Drawing.Point(19, 165);
            this.showLogFile.Name = "showLogFile";
            this.showLogFile.Size = new System.Drawing.Size(386, 25);
            this.showLogFile.TabIndex = 37;
            this.showLogFile.Text = "Show Log";
            this.showLogFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.showLogFile, "Show the Log-File");
            this.showLogFile.UseVisualStyleBackColor = false;
            this.showLogFile.Click += new System.EventHandler(this.showLog);
            // 
            // clearLogFile
            // 
            this.clearLogFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.clearLogFile.FlatAppearance.BorderColor = System.Drawing.Color.LightGray;
            this.clearLogFile.FlatAppearance.BorderSize = 0;
            this.clearLogFile.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.clearLogFile.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.clearLogFile.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.clearLogFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearLogFile.ForeColor = System.Drawing.Color.DimGray;
            this.clearLogFile.Location = new System.Drawing.Point(404, 165);
            this.clearLogFile.Name = "clearLogFile";
            this.clearLogFile.Size = new System.Drawing.Size(72, 25);
            this.clearLogFile.TabIndex = 38;
            this.clearLogFile.Text = "Clear Log...";
            this.toolTip.SetToolTip(this.clearLogFile, "Clear the Log-File...");
            this.clearLogFile.UseVisualStyleBackColor = false;
            this.clearLogFile.Click += new System.EventHandler(this.clearLog);
            // 
            // routes
            // 
            this.routes.BackColor = System.Drawing.Color.WhiteSmoke;
            this.routes.FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
            this.routes.FlatAppearance.BorderSize = 0;
            this.routes.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.routes.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.routes.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.routes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.routes.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.routes.ForeColor = System.Drawing.Color.DimGray;
            this.routes.Location = new System.Drawing.Point(19, 30);
            this.routes.Name = "routes";
            this.routes.Size = new System.Drawing.Size(276, 25);
            this.routes.TabIndex = 40;
            this.routes.Text = "Edit Routes Configuration...";
            this.routes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.routes, "Edit Routes-Configuration File, which holds all by the Server handled Routes");
            this.routes.UseVisualStyleBackColor = false;
            this.routes.Click += new System.EventHandler(this.editRoutes);
            // 
            // showDirLog
            // 
            this.showDirLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.showDirLog.FlatAppearance.BorderColor = System.Drawing.Color.LightGray;
            this.showDirLog.FlatAppearance.BorderSize = 0;
            this.showDirLog.FlatAppearance.CheckedBackColor = System.Drawing.Color.White;
            this.showDirLog.FlatAppearance.MouseDownBackColor = System.Drawing.Color.LightSteelBlue;
            this.showDirLog.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gainsboro;
            this.showDirLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.showDirLog.ForeColor = System.Drawing.Color.DimGray;
            this.showDirLog.Location = new System.Drawing.Point(476, 165);
            this.showDirLog.Name = "showDirLog";
            this.showDirLog.Size = new System.Drawing.Size(90, 25);
            this.showDirLog.TabIndex = 48;
            this.showDirLog.Text = "Show Directory";
            this.toolTip.SetToolTip(this.showDirLog, "Show Directory of Log-Files");
            this.showDirLog.UseVisualStyleBackColor = false;
            this.showDirLog.Click += new System.EventHandler(this.showDirectoryLog);
            // 
            // watchFlag
            // 
            this.watchFlag.AutoSize = true;
            this.watchFlag.BackColor = System.Drawing.Color.WhiteSmoke;
            this.watchFlag.ForeColor = System.Drawing.Color.DimGray;
            this.watchFlag.Location = new System.Drawing.Point(114, 198);
            this.watchFlag.Name = "watchFlag";
            this.watchFlag.Size = new System.Drawing.Size(113, 17);
            this.watchFlag.TabIndex = 49;
            this.watchFlag.Text = "Observe Rootpath";
            this.toolTip.SetToolTip(this.watchFlag, "Watch Rootpath and Refresh the current Browser in case of changed Files");
            this.watchFlag.UseVisualStyleBackColor = false;
            this.watchFlag.CheckedChanged += new System.EventHandler(this.watchEnable);
            // 
            // minifyJS
            // 
            this.minifyJS.AutoSize = true;
            this.minifyJS.BackColor = System.Drawing.Color.WhiteSmoke;
            this.minifyJS.Enabled = false;
            this.minifyJS.ForeColor = System.Drawing.Color.Silver;
            this.minifyJS.Location = new System.Drawing.Point(233, 198);
            this.minifyJS.Name = "minifyJS";
            this.minifyJS.Size = new System.Drawing.Size(68, 17);
            this.minifyJS.TabIndex = 59;
            this.minifyJS.Text = "Minify JS";
            this.toolTip.SetToolTip(this.minifyJS, "Minify all JS Files in Rootpath on the Fly");
            this.minifyJS.UseVisualStyleBackColor = false;
            // 
            // minifyCSS
            // 
            this.minifyCSS.AutoSize = true;
            this.minifyCSS.BackColor = System.Drawing.Color.WhiteSmoke;
            this.minifyCSS.Enabled = false;
            this.minifyCSS.ForeColor = System.Drawing.Color.Silver;
            this.minifyCSS.Location = new System.Drawing.Point(307, 198);
            this.minifyCSS.Name = "minifyCSS";
            this.minifyCSS.Size = new System.Drawing.Size(77, 17);
            this.minifyCSS.TabIndex = 60;
            this.minifyCSS.Text = "Minify CSS";
            this.toolTip.SetToolTip(this.minifyCSS, "Minify all CSS Files in Rootpath on the Fly");
            this.minifyCSS.UseVisualStyleBackColor = false;
            // 
            // label8
            // 
            this.label8.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.label8.ForeColor = System.Drawing.Color.DimGray;
            this.label8.Location = new System.Drawing.Point(19, 193);
            this.label8.Name = "label8";
            this.label8.Padding = new System.Windows.Forms.Padding(3);
            this.label8.Size = new System.Drawing.Size(547, 25);
            this.label8.TabIndex = 61;
            this.label8.Text = "Development";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.label8, "Settings for Developer");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.DarkGray;
            this.label1.Location = new System.Drawing.Point(1, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(15, 16);
            this.label1.TabIndex = 41;
            this.label1.Text = "1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(1, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(15, 16);
            this.label2.TabIndex = 42;
            this.label2.Text = "2";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(1, 142);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(15, 16);
            this.label3.TabIndex = 43;
            this.label3.Text = "6";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.DarkGray;
            this.label4.Location = new System.Drawing.Point(1, 115);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(15, 16);
            this.label4.TabIndex = 44;
            this.label4.Text = "5";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(1, 88);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(15, 16);
            this.label5.TabIndex = 45;
            this.label5.Text = "4";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.DarkGray;
            this.label6.Location = new System.Drawing.Point(1, 61);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(15, 16);
            this.label6.TabIndex = 46;
            this.label6.Text = "3";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.DarkGray;
            this.label7.Location = new System.Drawing.Point(1, 169);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(15, 16);
            this.label7.TabIndex = 47;
            this.label7.Text = "7";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(1, 200);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(15, 16);
            this.label9.TabIndex = 52;
            this.label9.Text = "8";
            // 
            // main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gainsboro;
            this.ClientSize = new System.Drawing.Size(569, 193);
            this.Controls.Add(this.minifyCSS);
            this.Controls.Add(this.minifyJS);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.watchFlag);
            this.Controls.Add(this.showDirLog);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.routes);
            this.Controls.Add(this.clearLogFile);
            this.Controls.Add(this.showLogFile);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.firefox);
            this.Controls.Add(this.chrome);
            this.Controls.Add(this.iexplore);
            this.Controls.Add(this.opera);
            this.Controls.Add(this.safari);
            this.Controls.Add(this.rootpath);
            this.Controls.Add(this.dbquery);
            this.Controls.Add(this.setrights);
            this.Controls.Add(this.restarter);
            this.Controls.Add(this.serverconfig);
            this.Controls.Add(this.browser);
            this.Controls.Add(this.StatusInfoBar);
            this.Controls.Add(this.label8);
            this.ForeColor = System.Drawing.Color.RoyalBlue;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(32, 128);
            this.MaximizeBox = false;
            this.Name = "main";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "webQL - Server";
            this.TopMost = true;
            this.Activated += new System.EventHandler(this.activating);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.formClosing);
            this.Load += new System.EventHandler(this.formLoading);
            this.Resize += new System.EventHandler(this.resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.Timer timerDelay;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label StatusInfoBar;
        private System.Windows.Forms.Button browser;
        private System.Windows.Forms.Button serverconfig;
        private System.Windows.Forms.Button restarter;
        private System.Windows.Forms.Button setrights;
        private System.Windows.Forms.Button dbquery;
        private System.Windows.Forms.Button rootpath;
        private System.Windows.Forms.Button safari;
        private System.Windows.Forms.Button opera;
        private System.Windows.Forms.Button iexplore;
        private System.Windows.Forms.Button chrome;
        private System.Windows.Forms.Button firefox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button showLogFile;
        private System.Windows.Forms.Button clearLogFile;
        private System.Windows.Forms.Button routes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button showDirLog;
        private System.Windows.Forms.CheckBox watchFlag;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox minifyJS;
        private System.Windows.Forms.CheckBox minifyCSS;
        private System.Windows.Forms.Label label8;


    }
}

