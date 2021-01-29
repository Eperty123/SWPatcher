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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml;
using SWPatcher.General;
using SWPatcher.Helpers;
using SWPatcher.Helpers.GlobalVariables;
using SWPatcher.Helpers.Steam;

namespace SWPatcher.Forms
{
    internal partial class MainForm
    {
        internal void RestoreFromTray()
        {
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Show();

            NotifyIcon.Visible = false;
        }

        private static void StartupBackupCheck(Language language)
        {
            if (Directory.Exists(language.BackupPath))
            {
                if (Directory.GetFiles(language.BackupPath, "*", SearchOption.AllDirectories).Length > 0)
                {
                    DialogResult result = MsgBox.Question(StringLoader.GetText("question_backup_files_found", language.ToString()));

                    if (result == DialogResult.Yes)
                    {
                        RestoreBackup(language);
                    }
                    else
                    {
                        string[] filePaths = Directory.GetFiles(language.BackupPath, "*", SearchOption.AllDirectories);

                        foreach (var file in filePaths)
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(language.BackupPath);
            }
        }

        private static void RestoreBackup(Language language)
        {
            if (!Directory.Exists(language.BackupPath))
            {
                return;
            }

            string regionId = language.ApplyingRegionId;
            string regionFolder = language.ApplyingRegionFolder;
            string backupFilePath = Path.Combine(language.BackupPath, Methods.GetGameExeName(regionId));
            if (File.Exists(backupFilePath))
            {
                string gameExePath = Path.Combine(UserSettings.GamePath, Methods.GetGameExeName(regionId));
                string gameExePatchedPath = Path.Combine(UserSettings.PatcherPath, regionFolder, Methods.GetGameExeName(regionId));
                Logger.Info($"Restoring .exe original=[{gameExePath}] backup=[{gameExePatchedPath}]");

                if (File.Exists(gameExePath))
                {
                    if (File.Exists(gameExePatchedPath))
                    {
                        File.Delete(gameExePatchedPath);
                    }

                    File.Move(gameExePath, gameExePatchedPath);
                }

                File.Move(backupFilePath, gameExePath);
            }

            string[] filePaths = Directory.GetFiles(language.BackupPath, "*", SearchOption.AllDirectories);

            foreach (var file in filePaths)
            {
                string path = Path.Combine(UserSettings.GamePath, file.Substring(language.BackupPath.Length + 1));
                Logger.Info($"Restoring file original=[{path}] backup=[{file}]");

                if (File.Exists(path))
                {
                    string langPath = Path.Combine(language.Path, path.Substring(UserSettings.GamePath.Length + 1));

                    if (File.Exists(langPath))
                    {
                        File.Delete(langPath);
                    }

                    File.Move(path, langPath);
                }

                try
                {
                    File.Move(file, path);
                }
                catch (DirectoryNotFoundException)
                {
                    MsgBox.Error(StringLoader.GetText("exception_cannot_restore_file", Path.GetFullPath(file)));
                    Logger.Error($"Cannot restore file=[{file}]");
                    File.Delete(file);
                }
            }
        }

        private static void DeleteTmpFiles(Language language)
        {
            string[] tmpFilePaths = Directory.GetFiles(language.Path, "*.tmp", SearchOption.AllDirectories);

            foreach (var tmpFile in tmpFilePaths)
            {
                File.Delete(tmpFile);
                Logger.Info($"Deleting tmp file=[{tmpFile}]");
            }
        }

        private static string GetJPSwPathFromRegistry()
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                string value = Methods.GetRegistryValue(Strings.Registry.JP.RegistryKey, Strings.Registry.JP.Key32Path, Strings.Registry.JP.FolderName);

                return value;
            }
            else
            {
                string value = Methods.GetRegistryValue(Strings.Registry.JP.RegistryKey, Strings.Registry.JP.Key64Path, Strings.Registry.JP.FolderName);

                if (value != string.Empty)
                {
                    return value;
                }
                else
                {
                    value = Methods.GetRegistryValue(Strings.Registry.JP.RegistryKey, Strings.Registry.JP.Key32Path, Strings.Registry.JP.FolderName);

                    return value;
                }
            }
        }

        private static string GetKRSwPathFromRegistry()
        {
            string value = Methods.GetRegistryValue(Strings.Registry.KR.RegistryKey, Strings.Registry.KR.Key32Path, Strings.Registry.KR.FolderName);

            return value;
        }

        private static string GetCustomGamePath()
        {
            return UserSettings.CustomGamePath ?? "";
        }

        private static string GetNaverKRSwPathFromRegistry()
        {
            string value = Methods.GetRegistryValue(Strings.Registry.NaverKR.RegistryKey, Strings.Registry.NaverKR.Key32Path, Strings.Registry.NaverKR.FolderName);

            return value;
        }

        private static string GetGameforgeSwPath()
        {
            const string SteamGameId = "630100";
            string steamInstallPath;
            string swPath = string.Empty;
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
                            if (smacf.Elements.TryGetValue("installdir", out SteamManifestElement sme))
                            {
                                string path = Path.Combine(libraryPath, "common", ((SteamManifestEntry)sme).Value);
                                if (Directory.Exists(path) &&
                                    Directory.GetFiles(path).Length > 0)
                                {
                                    swPath = path;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (swPath == string.Empty)
            {
                swPath = GetGameforgeLauncherSwPath();
            }

            return swPath;
        }

        private static string GetGameforgeLauncherSwPath()
        {
            string gameforgeInstallPath;
            if (!Environment.Is64BitOperatingSystem)
            {
                gameforgeInstallPath = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Key32Path, Strings.Registry.Gameforge.InstallPath);
            }
            else
            {
                gameforgeInstallPath = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Key64Path, Strings.Registry.Gameforge.InstallPath);

                if (gameforgeInstallPath == string.Empty)
                {
                    gameforgeInstallPath = Methods.GetRegistryValue(Strings.Registry.Gameforge.RegistryKey, Strings.Registry.Gameforge.Key32Path, Strings.Registry.Gameforge.InstallPath);
                }
            }

            if (gameforgeInstallPath == string.Empty)
            {
                return string.Empty;
            }

            return Path.Combine(gameforgeInstallPath, "Client");
        }

        internal void ResetTranslation(Language language)
        {
            DeleteTranslationIni(language);
            LabelNewTranslations.Text = StringLoader.GetText("form_label_new_translation", language.ToString(), Methods.DateToLocalString(language.LastUpdate));
        }

        private static void DeleteTranslationIni(Language language)
        {
            string iniPath = Path.Combine(language.Path, Strings.IniName.Translation);

            if (File.Exists(iniPath))
            {
                File.Delete(iniPath);
            }
        }

        private void InitRegionsConfigData()
        {
            var doc = new XmlDocument();
            string xmlPath = Urls.TranslationGitHubHome + Strings.IniName.LanguagePack;
            Logger.Debug(Methods.MethodFullName(System.Reflection.MethodBase.GetCurrentMethod(), xmlPath));
            try
            {
                doc.Load(xmlPath);
            }
            catch (Exception e)
            {
                MsgBox.Error("Unable to find LanguagePacks.xml at the specified translations source, change the source and try again.");
                return;
            }

            XmlElement configRoot = doc.DocumentElement;
            XmlElement xmlRegions = configRoot[Strings.Xml.Regions];
            int regionCount = xmlRegions.ChildNodes.Count;
            Logger.Debug($"Got {regionCount} regions");
            List<Region> regions = new List<Region>();

            for (int i = 0; i < regionCount; i++)
            {
                XmlNode regionNode = xmlRegions.ChildNodes[i];

                string regionId = regionNode.Name;
                string regionName = StringLoader.GetText(regionNode.Attributes[Strings.Xml.Attributes.Name].Value);
                string regionFolder = regionNode.Attributes[Strings.Xml.Attributes.Folder]?.Value;
                string languagesLinkId = regionNode.Attributes[Strings.Xml.Attributes.LanguagesLinkId]?.Value;

                if (string.IsNullOrWhiteSpace(regionFolder))
                {
                    regionFolder = regionId;
                }

                if (string.IsNullOrWhiteSpace(languagesLinkId))
                {
                    XmlElement xmlLanguages = regionNode[Strings.Xml.Languages];
                    int languageCount = xmlLanguages.ChildNodes.Count;
                    Language[] regionLanguages = new Language[languageCount];

                    for (int j = 0; j < languageCount; j++)
                    {
                        XmlNode languageNode = xmlLanguages.ChildNodes[j];

                        string languageId = languageNode.Name;
                        string languageName = languageNode.Attributes[Strings.Xml.Attributes.Name].Value;
                        string languageDateString = languageNode[Strings.Xml.Value].InnerText;
                        DateTime languageDate = Methods.ParseDate(languageDateString);

                        regionLanguages[j] = new Language(languageId, languageName, languageDate, regionId, regionFolder);
                    }

                    regions.Add(new Region(regionId, regionName, regionFolder, regionLanguages));
                    if (regionId == "jp")
                    {
                        Language[] lngs = new Language[regionLanguages.Length];
                        for (int c = 0; c < regionLanguages.Length; c++)
                        {
                            lngs[c] = new Language(regionLanguages[c].Id, regionLanguages[c].Name, regionLanguages[c].LastUpdate, "jpc", "jpc");
                        }
                        regions.Add(new Region("jpc", StringLoader.GetText("form_region_jpc"), "jpc", lngs));
                    }
                }
                else
                {
                    Region linkedRegion = regions.Where(r => r.Id == languagesLinkId).FirstOrDefault();
                    var regionLanguages = new List<Language>();
                    if (linkedRegion != null)
                    {
                        foreach (var l in linkedRegion.AppliedLanguages)
                        {
                            regionLanguages.Add(new Language(l.Id, l.Name, l.LastUpdate, regionId, regionFolder));
                        }
                    }
                    var regionLanguagesArr = regionLanguages.ToArray();
                    regions.Add(new Region(regionId, regionName, regionFolder, regionLanguagesArr));
                    if (regionId == "jp")
                    {
                        Language[] lngs = new Language[regionLanguagesArr.Length];
                        for (int c = 0; c < regionLanguagesArr.Length; c++)
                        {
                            lngs[c] = new Language(regionLanguagesArr[c].Id, regionLanguagesArr[c].Name, regionLanguagesArr[c].LastUpdate, "jpc", "jpc");
                        }
                        regions.Add(new Region("jpc", StringLoader.GetText("form_region_jpc"), "jpc", lngs));
                    }
                }
            }

            ComboBoxRegions.DataSource = regions.Count > 0 ? regions : null;

            if (ComboBoxRegions.DataSource != null)
            {
                if (string.IsNullOrEmpty(UserSettings.RegionId))
                {
                    if (ComboBoxRegions.SelectedItem == null && ComboBoxRegions.Items.Count > 0)
                    {
                        ComboBoxRegions.SelectedItem = ComboBoxRegions.Items[0];
                        UserSettings.RegionId = (ComboBoxRegions.SelectedItem as Region).Id;
                    }
                    else
                    {
                        UserSettings.RegionId = (ComboBoxRegions?.SelectedItem as Region)?.Id ?? "jpc";
                    }
                }
                else
                {
                    int index = ComboBoxRegions.Items.IndexOf(new Region(UserSettings.RegionId));
                    ComboBoxRegions.SelectedIndex = index == -1 ? 0 : index;
                }

                ComboBoxRegions_SelectionChangeCommitted(this, EventArgs.Empty);
            }
        }

        private static string GetSHA256(string filename)
        {
            using (var sha256 = SHA256.Create())
            using (FileStream fs = File.OpenRead(filename))
            {
                return BitConverter.ToString(sha256.ComputeHash(fs)).Replace("-", "");
            }
        }

        private string UploadToPasteBin(string title, string text, PasteBinExpiration expiration, bool isPrivate, string format)
        {
            var client = new PasteBinClient(Strings.PasteBin.DevKey);

            try
            {
                client.Login(Strings.PasteBin.Username, Strings.PasteBin.Password);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            var entry = new PasteBinEntry
            {
                Title = title,
                Text = text,
                Expiration = expiration,
                Private = isPrivate,
                Format = format
            };

            try
            {
                return client.Paste(entry);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MsgBox.Error(StringLoader.GetText("exception_log_file_failed"));
            }
            finally
            {
                client.Logout();
            }

            return null;
        }

        private static byte[] TrimArrayIfNecessary(byte[] array)
        {
            int limit = 512000 / 2;

            if (array.Length > limit)
            {
                byte[] trimmedArray = new byte[limit];
                Array.Copy(array, array.Length - limit, trimmedArray, 0, limit);

                return trimmedArray;
            }

            return array;
        }

        internal IEnumerable<string> GetTranslationFolders()
        {
            return ComboBoxRegions.Items.Cast<Region>().Select(s => s.Folder);
        }

        internal Region GetSelectedRegion()
        {
            return ComboBoxRegions.SelectedItem as Region;
        }
    }
}