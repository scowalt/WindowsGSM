﻿using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsGSM.GameServer
{
    class TF2
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string ServerID;

        private string Param;
        public string Error;
        public string Notice;

        public string port = "27015";
        public string defaultmap = "cp_badlands";
        public string maxplayers = "24";
        public string additional = "";

        public TF2(string serverid)
        {
            ServerID = serverid;
        }

        public void CreateServerCFG(string hostname, string rcon_password)
        {
            string serverConfigPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles\tf\cfg\server.cfg";
            
            File.Create(serverConfigPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(serverConfigPath))
            {
                textwriter.WriteLine("hostname \"" + hostname + "\"");
                textwriter.WriteLine("rcon_password \"" + rcon_password + "\"");
                textwriter.WriteLine("sv_password \"\"");
                textwriter.WriteLine("sv_region \"255\"");
                textwriter.WriteLine("sv_lan \"0\"");
                textwriter.WriteLine("net_maxfilesize \"64\"");
                textwriter.WriteLine("sv_downloadurl \"\"");
                textwriter.WriteLine("exec banned_user.cfg");
                textwriter.WriteLine("exec banned_ip.cfg");
                textwriter.WriteLine("writeid");
                textwriter.WriteLine("writeip");
            }
        }

        public void SetParameter(string ip, string port, string map, string maxplayers, string gslt, string additional)
        {
            Param = "-console -game tf -ip " + ip + " -port " + port + " +map " + map + " -maxplayers " + maxplayers + " +sv_setsteamaccount " + gslt + " " + additional;
        }

        public Process Start()
        {
            string workingDir = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";
            string srcdsPath = workingDir + @"\srcds.exe";

            if (!File.Exists(srcdsPath))
            {
                Error = "srcds.exe not found (" + srcdsPath + ")";
                return null;
            }

            if (string.IsNullOrWhiteSpace(Param))
            {
                Error = "Start Parameter not set";
                return null;
            }

            string serverConfigPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles\tf\cfg\server.cfg";

            if (!File.Exists(serverConfigPath))
            {
                Notice = "server.cfg not found (" + serverConfigPath + ")";
            }

            Process p = new Process();
            p.StartInfo.WorkingDirectory = workingDir;
            p.StartInfo.FileName = srcdsPath;
            p.StartInfo.Arguments = Param;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            p.Start();

            return p;
        }

        public async Task<bool> Stop(Process p)
        {
            SetForegroundWindow(p.MainWindowHandle);
            SendKeys.SendWait("quit");
            SendKeys.SendWait("{ENTER}");
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            bool stopped = false;
            int attempt = 0;
            while (attempt < 10)
            {
                if (p != null)
                {
                    if (p.HasExited)
                    {
                        stopped = true;
                        break;
                    }
                }

                attempt++;

                await Task.Delay(1000);
            }

            return stopped;
        }

        public async Task<Process> Install()
        {
            string serverFilesPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";

            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, serverFilesPath, "232250", true);

            if (!await steamCMD.Download())
            {
                Error = steamCMD.GetError();
                return null;
            }

            Process pSteamCMD = steamCMD.Run();
            if (pSteamCMD == null)
            {
                Error = steamCMD.GetError();
                return null;
            }

            return pSteamCMD;
        }

        public async Task<bool> Update()
        {
            string serverFilesPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";

            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, serverFilesPath, "232250", false);

            if (!await steamCMD.Download())
            {
                Error = steamCMD.GetError();
                return false;
            }

            Process pSteamCMD = steamCMD.Run();
            if (pSteamCMD == null)
            {
                Error = steamCMD.GetError();
                return false;
            }

            await Task.Run(() => pSteamCMD.WaitForExit());

            if (pSteamCMD.ExitCode == 0)
            {
                return true;
            }

            Error = "Exit code: " + pSteamCMD.ExitCode.ToString();
            return false;
        }
    }
}