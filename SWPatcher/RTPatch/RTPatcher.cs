/*
 * This file is part of Soulworker Patcher.
 * Copyright (C) 2016-2017 Miyu, Dramiel Leayal
 *
 * Soulworker Patcher is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Soulworker Patcher is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Soulworker Patcher. If not, see <http://www.gnu.org/licenses/>.
 */

using MadMilkman.Ini;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWPatcher.General;
using SWPatcher.Helpers;
using SWPatcher.Helpers.GlobalVariables;
using SWPatcher.Helpers.Steam;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace SWPatcher.RTPatch
{
    internal delegate void RTPatcherDownloadProgressChangedEventHandler(object sender, RTPatcherDownloadProgressChangedEventArgs e);

    internal delegate void RTPatcherProgressChangedEventHandler(object sender, RTPatcherProgressChangedEventArgs e);

    internal delegate void RTPatcherCompletedEventHandler(object sender, RTPatcherCompletedEventArgs e);

    internal delegate string RTPatchCallback(uint id, IntPtr ptr);

    internal class RTPatcher
    {
        private const int DiffBytes = 10; // how many bytes to redownload on resume, just to be safe, why not?
        private readonly BackgroundWorker Worker;
        private string CurrentLogFilePath;
        private string FileName;
        private string LastMessage;
        private string Url;
        private Version ClientNextVersion;
        private Version ClientVersion;
        private Version ServerVersion;
        private int FileCount;
        private int FileNumber;
        private Language Language;

        internal event RTPatcherDownloadProgressChangedEventHandler RTPatcherDownloadProgressChanged;

        internal event RTPatcherProgressChangedEventHandler RTPatcherProgressChanged;

        internal event RTPatcherCompletedEventHandler RTPatcherCompleted;

        internal RTPatcher()
        {
            Worker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            Worker.DoWork += Worker_DoWork;
            Worker.ProgressChanged += Worker_ProgressChanged;
            Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string regionId = Language.ApplyingRegionId;
            Methods.CheckRunningGame(regionId);
            switch (regionId)
            {
                case "jp":
                case "gjp":
                    LoadVersions();
                    Logger.Debug(Methods.MethodFullName("RTPatch", Thread.CurrentThread.ManagedThreadId.ToString(), ClientNextVersion.ToString()));

                    break;

                case "kr":
                    CheckKRVersion();
                    //Logger.Debug(Methods.MethodFullName("RTPatch", Thread.CurrentThread.ManagedThreadId.ToString()));
                    return;

                //break;
                case "nkr":
                    CheckNaverKRVersion();

                    return;

                case "gf":
                    CheckGFVersion();
                    return;

                case "jpc":
                    return;

                default:
                    throw new Exception(StringLoader.GetText("exception_region_unknown", regionId));
            }

            while (ClientNextVersion < ServerVersion)
            {
                if (Worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                FileCount = 0;
                FileNumber = 0;
                FileName = "";
                LastMessage = "";
                ClientVersion = ClientNextVersion;
                ClientNextVersion = GetNextVersion(ClientNextVersion);
                string RTPFileName = VersionToRTP(ClientNextVersion);
                string url = Url + RTPFileName;
                string destination = Path.Combine(UserSettings.GamePath, RTPFileName);
                string gamePath = UserSettings.GamePath;
                string diffFilePath = Path.Combine(gamePath, VersionToRTP(ClientNextVersion));
                CurrentLogFilePath = Path.Combine(Strings.FolderName.RTPatchLogs, Path.GetFileName(diffFilePath) + ".log");
                string logDirectory = Path.GetDirectoryName(CurrentLogFilePath);
                Directory.CreateDirectory(logDirectory);

                Logger.Info($"Downloading url=[{url}] path=[{destination}]");

                #region Download Resumable File

                using (FileStream fs = File.OpenWrite(destination))
                {
                    long fileLength = fs.Length < DiffBytes ? 0 : fs.Length - DiffBytes;
                    fs.Position = fileLength;
                    HttpWebRequest request = WebRequest.CreateHttp(url);
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0;)";
                    request.Credentials = new NetworkCredential();
                    request.AddRange(fileLength);

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        long bytesToReceive = response.ContentLength;
                        using (Stream stream = response.GetResponseStream())
                        {
                            byte[] buffer = new byte[1000 * 1000]; // 1MB buffer
                            int bytesRead;
                            long totalFileBytes = fileLength + bytesToReceive;
                            long fileBytes = fileLength;
                            long receivedBytes = 0;

                            Worker.ReportProgress(0, 0L);
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                if (Worker.CancellationPending)
                                {
                                    e.Cancel = true;
                                    request.Abort();
                                    return;
                                }

                                fs.Write(buffer, 0, bytesRead);

                                fileBytes += bytesRead;
                                receivedBytes += bytesRead;
                                int progress = fileBytes >= totalFileBytes ? int.MaxValue : Convert.ToInt32(((double)fileBytes / totalFileBytes) * int.MaxValue);
                                double elapsedTicks = sw.ElapsedTicks;
                                long frequency = Stopwatch.Frequency;
                                long elapsedSeconds = Convert.ToInt64(elapsedTicks / frequency);
                                long bytesPerSecond;
                                if (elapsedSeconds > 0)
                                {
                                    bytesPerSecond = receivedBytes / elapsedSeconds;
                                }
                                else
                                {
                                    bytesPerSecond = receivedBytes;
                                }

                                Worker.ReportProgress(progress, bytesPerSecond);
                            }
                            sw.Stop();
                        }
                    }
                }

                #endregion Download Resumable File

                Methods.CheckRunningProcesses(regionId);
                Logger.Info($"RTPatchApply diffFile=[{diffFilePath}] path=[{gamePath}]");
                Worker.ReportProgress(-1, 0L);

                #region Apply RTPatch

                File.Delete(CurrentLogFilePath);
                string command = $"/u /nos \"{gamePath}\" \"{diffFilePath}\"";
                ulong result = Environment.Is64BitProcess ? NativeMethods.RTPatchApply64(command, new RTPatchCallback(RTPatchMessage), true) : NativeMethods.RTPatchApply32(command, new RTPatchCallback(RTPatchMessage), true);
                Logger.Debug($"RTPatchApply finished with result=[{result}]");
                File.AppendAllText(CurrentLogFilePath, $"Result=[{result}]");

                if (result != 0)
                {
                    if (result > 10000)
                    {
                        Logger.Debug($"RTPatchApply cancelled Result=[{result}] IsNormal=[{result == 32769}]");
                        e.Cancel = true;

                        return; // RTPatch cancelled
                    }

                    throw new ResultException(LastMessage, result, CurrentLogFilePath, FileName, ClientVersion);
                }
                File.Delete(diffFilePath);

                IniFile ini = new IniFile(new IniOptions
                {
                    KeyDuplicate = IniDuplication.Ignored,
                    SectionDuplicate = IniDuplication.Ignored
                });
                string iniPath = Path.Combine(UserSettings.GamePath, Strings.IniName.ClientVer);
                ini.Load(iniPath);
                string serverVer = ini.Sections[Strings.IniName.Ver.Section].Keys[Strings.IniName.Ver.Key].Value;
                string clientVer = ClientNextVersion.ToString();
                if (serverVer != clientVer)
                {
                    ini.Sections[Strings.IniName.Ver.Section].Keys[Strings.IniName.Ver.Key].Value = clientVer;
                    ini.Save(iniPath);
                }

                #endregion Apply RTPatch
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (FileCount == 0)
            {
                RTPatcherDownloadProgressChanged?.Invoke(sender, new RTPatcherDownloadProgressChangedEventArgs(VersionToRTP(ClientNextVersion), e.ProgressPercentage, (long)e.UserState));
            }
            else
            {
                RTPatcherProgressChanged?.Invoke(this, new RTPatcherProgressChangedEventArgs(FileNumber, FileCount, FileName, e.ProgressPercentage));
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RTPatcherCompleted?.Invoke(this, new RTPatcherCompletedEventArgs(e.Cancelled, e.Error, Language));
        }

        private void LoadVersions()
        {
            IniFile serverIni = Methods.GetJPServerIni();
            ServerVersion = new Version(serverIni.Sections[Strings.IniName.Ver.Section].Keys[Strings.IniName.Ver.Key].Value);
            string address = serverIni.Sections[Strings.IniName.ServerRepository.Section].Keys[Strings.IniName.ServerRepository.Key].Value;
            Url = address + Strings.IniName.ServerRepository.UpdateRepository;

            IniFile clientIni = new IniFile();
            clientIni.Load(Path.Combine(UserSettings.GamePath, Strings.IniName.ClientVer));
            ClientNextVersion = new Version(clientIni.Sections[Strings.IniName.Ver.Section].Keys[Strings.IniName.Ver.Key].Value);
        }

        private void CheckKRVersion()
        {
            int serverVersion = Methods.GetKRServerVersion();
            //int clientVersion = Convert.ToInt32(Methods.GetRegistryValue(Strings.Registry.KR.RegistryKey, Strings.Registry.KR.Key32Path, Strings.Registry.KR.Version, 0));
            string stovePath = Methods.GetRegistryValue(Strings.Registry.KR.RegistryKey, Strings.Registry.KR.StoveKeyPath, Strings.Registry.KR.StoveWorkingDir, string.Empty);
            string smilegatePath = Path.GetDirectoryName(stovePath);
            string soulworkerManifestPath = Path.Combine(smilegatePath, @"WebLauncher\gamemanifest\gamemanifest_11_live.upf");
            int clientVersion1 = -1;
            string soulworkerPath = Methods.GetRegistryValue(Strings.Registry.KR.RegistryKey, Strings.Registry.KR.Key32Path, Strings.Registry.KR.FolderName, string.Empty);
            string soulworkerManifest2Path = Path.Combine(soulworkerPath, @"combinedata_manifest\gamemanifest_11_live.upf");
            int clientVersion2 = -1;

            if (File.Exists(soulworkerManifestPath))
            {
                var manifestJson = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(soulworkerManifestPath));
                clientVersion1 = Convert.ToInt32(manifestJson["gameinfo"]["version"].Value<string>());
            }

            if (File.Exists(soulworkerManifest2Path))
            {
                var manifestJson = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(soulworkerManifest2Path));
                clientVersion2 = Convert.ToInt32(manifestJson["gameinfo"]["version"].Value<string>());
            }

            if (clientVersion1 == -1 && clientVersion2 == -1)
            {
                throw new Exception($"Could not find manifest file. Please contact Miyu#9404 on discord.\nManifest1 Path: \"{soulworkerManifestPath}\"\nManifest2 Path: \"{soulworkerManifest2Path}\"");
            }

            int clientVersion = clientVersion1 > clientVersion2 ? clientVersion1 : clientVersion2;

            if (clientVersion != serverVersion)
            {
                throw new Exception(StringLoader.GetText("exception_game_not_latest"));
            }
        }

        private void CheckNaverKRVersion()
        {
            int serverVersion = Methods.GetKRServerVersion();
            int clientVersion = Convert.ToInt32(Methods.GetRegistryValue(Strings.Registry.NaverKR.RegistryKey, Strings.Registry.NaverKR.Key32Path, Strings.Registry.NaverKR.Version, 0));

            if (clientVersion != serverVersion)
            {
                throw new Exception(StringLoader.GetText("exception_game_not_latest"));
            }
        }

        private void CheckGFVersion()
        {
            const string SteamGameId = "630100";
            string steamInstallPath;
            bool success = false;
            if (!Environment.Is64BitOperatingSystem)
            {
                steamInstallPath = Methods.GetRegistryValue(Strings.Registry.Steam.RegistryKey, Strings.Registry.Steam.Key32Path, Strings.Registry.Steam.InstallPath);
            }
            else
            {
                steamInstallPath = Methods.GetRegistryValue(Strings.Registry.Steam.RegistryKey, Strings.Registry.Steam.Key64Path, Strings.Registry.Steam.InstallPath);

                if (steamInstallPath == string.Empty)
                {
                    steamInstallPath = Methods.GetRegistryValue(Strings.Registry.Steam.RegistryKey, Strings.Registry.Steam.Key32Path, Strings.Registry.Steam.InstallPath);
                }
            }

            if (steamInstallPath != string.Empty)
            {
                List<string> libraryPaths = new List<string>();
                string mainSteamLibrary = Path.Combine(steamInstallPath, "steamapps");
                libraryPaths.Add(mainSteamLibrary);
                string libraryFoldersFile = Path.Combine(mainSteamLibrary, "libraryfolders.vdf");

                if (File.Exists(libraryFoldersFile))
                {
                    var libraryManifest = SteamManifest.Load(libraryFoldersFile);
                    int i = 1;
                    while (libraryManifest.Elements.TryGetValue((i++).ToString(), out SteamManifestElement sme))
                    {
                        libraryPaths.Add(Path.Combine(((SteamManifestEntry)sme).Value, "steamapps"));
                    }

                    foreach (string libraryPath in libraryPaths)
                    {
                        string acf = Path.Combine(libraryPath, $"appmanifest_{SteamGameId}.acf");
                        if (File.Exists(acf))
                        {
                            var smacf = SteamManifest.Load(acf);
                            if (smacf.Elements.TryGetValue("StateFlags", out SteamManifestElement sme))
                            {
                                if (int.TryParse(((SteamManifestEntry)sme).Value, out int stateFlagInt))
                                {
                                    var appState = (AppState)stateFlagInt;
                                    if (appState == AppState.StateFullyInstalled)
                                    {
                                        success = true;
                                    }
                                    else
                                    {
                                        success = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!success)
            {
                // if no valid soulworker found on steam, look on gameforge's launcher
                CheckGFLauncherVersion();
            }
        }

        private void CheckGFLauncherVersion()
        {
            string gameforgeInstallPath;
            string metadataGameState;
            if (!Environment.Is64BitOperatingSystem)
            {
                gameforgeInstallPath = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Key32Path, Strings.Registry.Gameforge.InstallPath);
                metadataGameState = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Metadata32Path, Strings.Registry.Gameforge.GameState);
            }
            else
            {
                gameforgeInstallPath = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Key64Path, Strings.Registry.Gameforge.InstallPath);
                metadataGameState = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Metadata64Path, Strings.Registry.Gameforge.GameState);

                if (gameforgeInstallPath == string.Empty)
                {
                    gameforgeInstallPath = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Key32Path, Strings.Registry.Gameforge.InstallPath);
                    metadataGameState = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Metadata32Path, Strings.Registry.Gameforge.GameState);
                }
            }

            if (gameforgeInstallPath == string.Empty)
            {
                throw new Exception(StringLoader.GetText("exception_game_not_latest"));
            }

            if (metadataGameState == string.Empty)
            {
                throw new Exception(StringLoader.GetText("exception_game_not_latest"));
            }

            if (metadataGameState != "1")
            {
                throw new Exception(StringLoader.GetText("exception_game_not_latest"));
            }
        }

        private static bool WebFileExists(string uri)
        {
            if (WebRequest.CreateHttp(uri) is HttpWebRequest request)
            {
                request.Method = "HEAD";

                try
                {
                    if (request.GetResponse() is HttpWebResponse response)
                    {
                        using (response)
                        {
                            return response.StatusCode == HttpStatusCode.OK;
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse exResponse)
                    {
                        HttpStatusCode statusCode = exResponse.StatusCode;
                        if (statusCode != HttpStatusCode.NotFound)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                request.Abort();
            }

            return false;
        }

        private Version GetNextVersion(Version clientVer)
        {
            var result = new Version(clientVer.Major, clientVer.Minor, clientVer.Build, clientVer.Revision + 1);

            if (!WebFileExists(Url + VersionToRTP(result)))
            {
                result = new Version(clientVer.Major, clientVer.Minor, clientVer.Build + 1, 0);

                if (!WebFileExists(Url + VersionToRTP(result)))
                {
                    result = new Version(clientVer.Major, clientVer.Minor + 1, 0, 0);

                    if (!WebFileExists(Url + VersionToRTP(result)))
                    {
                        result = new Version(clientVer.Major + 1, 0, 0, 0);
                    }
                }
            }

            return result;
        }

        internal static string VersionToRTP(Version version)
        {
            return $"{version.Major}_{version.Minor}_{version.Build}_{version.Revision}.RTP";
        }

        private string RTPatchMessage(uint id, IntPtr ptr)
        {
            switch (id)
            {
                case 1u:
                case 2u:
                case 3u:
                case 4u:
                case 8u:
                case 9u:
                case 10u:
                case 11u:
                case 12u: // outputs
                    string text = Marshal.PtrToStringAnsi(ptr);
                    LastMessage = text;
                    File.AppendAllText(CurrentLogFilePath, text);

                    break;

                case 14u:
                case 17u:
                case 18u: // abort on error

                    break;//return null;
                case 5u: // completion percentage
                    int readInt = Marshal.ReadInt32(ptr);
                    int percentage = readInt > short.MaxValue ? int.MaxValue : readInt * 0x10000;
                    Worker.ReportProgress(percentage);
                    percentage = readInt * 100 / 0x8000;
                    File.AppendAllText(CurrentLogFilePath, $"[{percentage}%]");

                    break;

                case 6u: // number of files in patch
                    int fileCount = Marshal.ReadInt32(ptr);
                    FileCount = fileCount;
                    File.AppendAllText(CurrentLogFilePath, $"File Count=[{fileCount}]\n");

                    break;

                case 7u: // current file
                    string fileName = Marshal.PtrToStringAnsi(ptr);
                    FileNumber++;
                    FileName = fileName;
                    Worker.ReportProgress(-1);
                    File.AppendAllText(CurrentLogFilePath, $"Patching=[{fileName}]\n");

                    break;

                default:
                    break; // ignore rest
            }

            if (Worker.CancellationPending)
            {
                return null;
            }

            return "";
        }

        internal void Cancel()
        {
            Worker.CancelAsync();
        }

        internal void Run(Language language)
        {
            if (Worker.IsBusy)
            {
                return;
            }

            Language = language;
            Worker.RunWorkerAsync();
        }
    }
}