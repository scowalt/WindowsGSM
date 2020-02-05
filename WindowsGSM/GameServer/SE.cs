﻿using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Note:
    /// I personally don't play Space engineers and I have no experience on this game, even on the game server.
    /// If anyone is the specialist or having a experience on Space Engineers server. Feel feel to edit this and pull request in Github.
    /// 
    /// </summary>
    class SE
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Space Engineers Dedicated Server";
        public string StartPath = @"DedicatedServer64\SpaceEngineersDedicated.exe";
        public bool ToggleConsole = false;

        public string port = "27016";
        public string defaultmap = "";
        public string maxplayers = "4";
        public string additional = "";

        public SE(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            /*
             * The configs is created under %APPDATA% points to C:\Users\(Username)\AppData\Roaming\ 
             */
        }

        public async Task<Process> Start()
        {
            string configFile = "SpaceEngineers-Dedicated.cfg";
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, "Config", configFile);
            if (!File.Exists(configPath))
            {
                Notice = $"{configFile} not found ({configPath})";
            }

            string param = (ToggleConsole ? "-console" : "-noconsole") + " -ignorelastsession";
            param += (string.IsNullOrEmpty(_serverData.ServerIP)) ? "" : $" -ip {_serverData.ServerIP}";
            param += (string.IsNullOrEmpty(_serverData.ServerPort)) ? "" : $" -port {_serverData.ServerPort}";
            param += $" {_serverData.ServerParam} -config server.cfg";

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = Functions.Path.GetServerFiles(_serverData.ServerID, StartPath),
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            /*
             * I had tried to change APPDATA value but fail... seems there is no way to change config dir...
             */
            //p.StartInfo.EnvironmentVariables["APPDATA"] = Functions.Path.GetServerFiles(_serverData.ServerID, "Config");
            //p.StartInfo.Environment["APPDATA"] = Functions.Path.GetServerFiles(_serverData.ServerID, "Config");
            var serverConsole = new Functions.ServerConsole(_serverData.ServerID);
            p.OutputDataReceived += serverConsole.AddOutput;
            p.ErrorDataReceived += serverConsole.AddOutput;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (ToggleConsole)
                {
                    SetForegroundWindow(p.MainWindowHandle);
                    SendKeys.SendWait("^(c)");
                    SendKeys.SendWait("^(c)");
                    SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                }
                else
                {
                    /*  Base on https://www.spaceengineersgame.com/dedicated-servers.html
                     * 
                     *  C:\WINDOWS\system32 > TASKKILL /pid 26500
                     *  SUCCESS: Sent termination signal to the process with PID 26500.
                     * 
                     *  But the process still exist.... Therefore, p.Kill(); is used
                     * 

                    Process taskkill = new Process
                    {
                        StartInfo =
                        {
                            FileName = "TASKKILL",
                            Arguments = $"/PID {p.Id}",
                            Verb = "runas",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    taskkill.Start();
                    */

                    p.Kill();
                }
            });
        }

        public async Task<Process> Install()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            Process p = await srcds.Install("298740");
            Error = srcds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            bool success = await srcds.Update("298740");
            Error = srcds.Error;

            return success;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.Path.GetServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string importPath = Path.Combine(path, StartPath);
            Error = $"Invalid Path! Fail to find {Path.GetFileName(StartPath)}";
            return File.Exists(importPath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, "298740");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("298740");
        }

        public string GetQueryPort()
        {
            return _serverData.ServerPort;
        }
    }
}