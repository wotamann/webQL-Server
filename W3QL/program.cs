using System;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace webQL
{




    static class program
    {

        static int restartMax = 3;              // Try max N Restarts 
        static int restartInterval = 5000;     //  wait for Restart N milliseconds
        static string restartSetting = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "webQL.setting");

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            
            try 
            {

                /* SINGLETON APPLICATION - RUN ONLY SINGLE INSTANCE OF THIS PROGRAMM                  */
                var currentProcess = Process.GetCurrentProcess();
                foreach (Process p in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (p.Id != currentProcess.Id)
                    {
                        SetForegroundWindow(p.MainWindowHandle);
                        return;
                    }
                }

                LogQL.LogServer("WebQL", "Program WebQL has been started!");
         

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new main());
               
            }
	        catch (Exception ex)
	        {
                
                restarter(ex); // Try 'restartMax' Times to restart the webQL-Application, waitng for 'restartInterval' milliseconds
	        
            }
           
        }
        
        static void restarter(Exception ex)
        {
            /*   Try 'restartMax' Times to restart the webQL-Application, waitng for 'restartInterval' milliseconds   */
            int restartCounter;
            int.TryParse(get(), out restartCounter);
            restartCounter++;
            set(restartCounter.ToString());

            if (restartCounter > restartMax)
            {
                // if more than restartMax failed restarts then App.Exit !!!
                set("0"); // reset restartCounter on file 
                LogQL.LogServer("WebQL Error", "### PROGRAM EXITED ### |" + ex.Message + " |Modul:" + ex.TargetSite.Module.Name + " |Stack:" + ex.StackTrace + " |Source:" + ex.Source);
                Application.Exit();
            }
            else
            {
                // if error wait sec and try RESTART     
                Thread.Sleep(restartInterval);
                LogQL.LogServer("WebQL Error", "### PROG " + restartCounter + ". AUTORESTART ### |" + ex.Message + " |Modul:" + ex.TargetSite.Module.Name + " |Stack:" + ex.StackTrace + " |Source:" + ex.Source);
                Application.Restart();
            }

        }
        // primitive set/get restartCounter on File 
        static void set(string content)
        {
        
            try
            {
                System.IO.File.WriteAllText(restartSetting, content);
            }
            catch 
            {
            }

        }
        static string get()
        {

            try
            {
                return System.IO.File.ReadAllText(restartSetting);
            }
            catch
            {
                return string.Empty;
            }

        }
        
    }
}
