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
using System.IO;
using System.Windows.Forms;
using MadMilkman.Ini;
using SWPatcher.Downloading;
using SWPatcher.General;
using SWPatcher.Helpers;
using SWPatcher.Helpers.GlobalVariables;
using SWPatcher.Launching;
using SWPatcher.Patching;
using SWPatcher.RTPatch;

namespace SWPatcher.Forms
{
    internal partial class MainForm : Form
    {
        internal enum State
        {
            Idle = 0,
            Download,
            Patch,
            Prepare,
            WaitClient,
            WaitClose,
            RTPatch,
            RegionNotInstalled
        }

        private enum NextState
        {
            None = 0,
            Download,
            Play,
            PlayRaw
        }

        private State _state;
        private NextState _nextState;
        private readonly Downloader Downloader;
        private readonly Patcher Patcher;
        private readonly RTPatcher RTPatcher;
        private readonly GameStarter GameStarter;

        internal State CurrentState
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state != value)
                {
                    switch (value)
                    {
                        case State.Idle:
                            ComboBoxLanguages.Enabled = true;
                            ComboBoxRegions.Enabled = true;
                            ButtonDownload.Enabled = true;
                            ButtonDownload.Text = StringLoader.GetText("button_download_translation");
                            ButtonPlay.Enabled = true;
                            ButtonPlay.Text = StringLoader.GetText("button_play");
                            ToolStripMenuItemStartRaw.Enabled = true;
                            ForceToolStripMenuItem.Enabled = true;
                            RefreshToolStripMenuItem.Enabled = true;
                            ToolStripStatusLabel.Text = StringLoader.GetText("form_status_idle");
                            ToolStripProgressBar.Value = ToolStripProgressBar.Minimum;
                            ToolStripProgressBar.Style = ProgressBarStyle.Blocks;

                            break;

                        case State.Download:
                            ComboBoxLanguages.Enabled = false;
                            ComboBoxRegions.Enabled = false;
                            ButtonDownload.Enabled = true;
                            ButtonDownload.Text = StringLoader.GetText("button_cancel");
                            ButtonPlay.Enabled = false;
                            ButtonPlay.Text = StringLoader.GetText("button_play");
                            ToolStripMenuItemStartRaw.Enabled = false;
                            ForceToolStripMenuItem.Enabled = false;
                            RefreshToolStripMenuItem.Enabled = false;
                            ToolStripStatusLabel.Text = StringLoader.GetText("form_status_download");
                            ToolStripProgressBar.Value = ToolStripProgressBar.Minimum;
                            ToolStripProgressBar.Style = ProgressBarStyle.Blocks;

                            break;

                        case State.Patch:
                            ComboBoxLanguages.Enabled = false;
                            ComboBoxRegions.Enabled = false;
                            ButtonDownload.Enabled = true;
                            ButtonDownload.Text = StringLoader.GetText("button_cancel");
                            ButtonPlay.Enabled = false;
                            ButtonPlay.Text = StringLoader.GetText("button_play");
                            ToolStripMenuItemStartRaw.Enabled = false;
                            ForceToolStripMenuItem.Enabled = false;
                            RefreshToolStripMenuItem.Enabled = false;
                            ToolStripStatusLabel.Text = StringLoader.GetText("form_status_patch");
                            ToolStripProgressBar.Value = ToolStripProgressBar.Minimum;
                            ToolStripProgressBar.Style = ProgressBarStyle.Blocks;

                            break;

                        case State.Prepare:
                            ComboBoxLanguages.Enabled = false;
                            ComboBoxRegions.Enabled = false;
                            ButtonDownload.Enabled = false;
                            ButtonDownload.Text = StringLoader.GetText("button_download_translation");
                            ButtonPlay.Enabled = false;
                            ButtonPlay.Text = StringLoader.GetText("button_play");
                            ToolStripMenuItemStartRaw.Enabled = false;
                            ForceToolStripMenuItem.Enabled = false;
                            RefreshToolStripMenuItem.Enabled = false;
                            ToolStripStatusLabel.Text = StringLoader.GetText("form_status_prepare");
                            ToolStripProgressBar.Value = ToolStripProgressBar.Minimum;
                            ToolStripProgressBar.Style = ProgressBarStyle.Marquee;

                            break;

                        case State.WaitClient:
                            ComboBoxLanguages.Enabled = false;
                            ComboBoxRegions.Enabled = false;
                            ButtonDownload.Enabled = false;
                            ButtonDownload.Text = StringLoader.GetText("button_download_translation");
                            ButtonPlay.Enabled = true;
                            ButtonPlay.Text = StringLoader.GetText("button_cancel");
                            ToolStripMenuItemStartRaw.Enabled = false;
                            ForceToolStripMenuItem.Enabled = false;
                            RefreshToolStripMenuItem.Enabled = false;
                            ToolStripStatusLabel.Text = StringLoader.GetText("form_status_wait_client");
                            ToolStripProgressBar.Value = ToolStripProgressBar.Minimum;
                            ToolStripProgressBar.Style = ProgressBarStyle.Blocks;

                            break;

                        case State.WaitClose:
                            ComboBoxLanguages.Enabled = false;
                            ComboBoxRegions.Enabled = false;
                            ButtonDownload.Enabled = false;
                            ButtonDownload.Text = StringLoader.GetText("button_download_translation");
                            ButtonPlay.Enabled = false;
                            ButtonPlay.Text = StringLoader.GetText("button_play");
                            ToolStripMenuItemStartRaw.Enabled = false;
                            ForceToolStripMenuItem.Enabled = false;
                            RefreshToolStripMenuItem.Enabled = false;
                            ToolStripStatusLabel.Text = StringLoader.GetText("form_status_wait_close");
                            ToolStripProgressBar.Value = ToolStripProgressBar.Minimum;
                            ToolStripProgressBar.Style = ProgressBarStyle.Marquee;
                            WindowState = FormWindowState.Minimized;

                            break;

                        case State.RTPatch:
                            ComboBoxLanguages.Enabled = false;
                            ComboBoxRegions.Enabled = false;
                            ButtonDownload.Enabled = true;
                            ButtonDownload.Text = StringLoader.GetText("button_cancel");
                            ButtonPlay.Enabled = false;
                            ButtonPlay.Text = StringLoader.GetText("button_play");
                            ToolStripMenuItemStartRaw.Enabled = false;
                            ForceToolStripMenuItem.Enabled = false;
                            RefreshToolStripMenuItem.Enabled = false;
                            ToolStripStatusLabel.Text = StringLoader.GetText("form_status_update_client");
                            ToolStripProgressBar.Value = ToolStripProgressBar.Minimum;
                            ToolStripProgressBar.Style = ProgressBarStyle.Blocks;

                            break;

                        case State.RegionNotInstalled:
                            ComboBoxLanguages.Enabled = false;
                            ButtonDownload.Enabled = false;
                            ButtonDownload.Text = StringLoader.GetText("button_download_translation");
                            ButtonPlay.Enabled = false;
                            ButtonPlay.Text = StringLoader.GetText("button_play");
                            ToolStripMenuItemStartRaw.Enabled = false;
                            ForceToolStripMenuItem.Enabled = false;
                            RefreshToolStripMenuItem.Enabled = false;
                            ToolStripStatusLabel.Text = StringLoader.GetText("form_status_idle");
                            ToolStripProgressBar.Value = ToolStripProgressBar.Minimum;
                            ToolStripProgressBar.Style = ProgressBarStyle.Blocks;

                            break;
                    }

                    Logger.Info($"State=[{value}]");
                    ComboBoxLanguages_SelectionChangeCommitted(this, EventArgs.Empty);
                    _state = value;
                }
            }
        }

        internal MainForm()
        {
            Downloader = new Downloader();
            Downloader.DownloaderProgressChanged += Downloader_DownloaderProgressChanged;
            Downloader.DownloaderCompleted += Downloader_DownloaderCompleted;

            Patcher = new Patcher();
            Patcher.PatcherProgressChanged += Patcher_PatcherProgressChanged;
            Patcher.PatcherCompleted += Patcher_PatcherCompleted;

            RTPatcher = new RTPatcher();
            RTPatcher.RTPatcherDownloadProgressChanged += RTPatcher_DownloadProgressChanged;
            RTPatcher.RTPatcherProgressChanged += RTPatcher_ProgressChanged;
            RTPatcher.RTPatcherCompleted += RTPatcher_Completed;

            GameStarter = new GameStarter();
            GameStarter.GameStarterProgressChanged += GameStarter_GameStarterProgressChanged;
            GameStarter.GameStarterCompleted += GameStarter_GameStarterCompleted;

            InitializeComponent();
            InitializeTextComponent();

            Logger.Info($"[{Text}] starting in UI Language [{UserSettings.UILanguageCode}]; Patcher Folder [{UserSettings.PatcherPath}]");
        }

        private void InitializeTextComponent()
        {
            MenuToolStripMenuItem.Text = StringLoader.GetText("form_menu");
            ForceToolStripMenuItem.Text = StringLoader.GetText("form_force_patch");
            OpenSWWebpageToolStripMenuItem.Text = StringLoader.GetText("form_open_sw_webpage");
            UploadLogToPastebinToolStripMenuItem.Text = StringLoader.GetText("form_upload_log");
            SettingsToolStripMenuItem.Text = StringLoader.GetText("form_settings");
            RefreshToolStripMenuItem.Text = StringLoader.GetText("form_refresh");
            AboutToolStripMenuItem.Text = StringLoader.GetText("form_about");
            LabelRegionPick.Text = StringLoader.GetText("form_region_pick");
            LabelLanguagePick.Text = StringLoader.GetText("form_language_pick");
            ButtonDownload.Text = StringLoader.GetText("button_download_translation");
            ButtonPlay.Text = StringLoader.GetText("button_play");
            ButtonExit.Text = StringLoader.GetText("button_exit");
            NotifyIcon.BalloonTipText = StringLoader.GetText("notify_balloon_text");
            NotifyIcon.BalloonTipTitle = StringLoader.GetText("notify_balloon_title");
            NotifyIcon.Text = StringLoader.GetText("notify_text");
            ToolStripMenuItemStartRaw.Text = StringLoader.GetText("button_play_raw");
            Text = AssemblyAccessor.Title + " " + AssemblyAccessor.Version;
        }

        private void Downloader_DownloaderProgressChanged(object sender, DownloaderProgressChangedEventArgs e)
        {
            if (CurrentState == State.Download)
            {
                ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_download")} {e.FileName} ({e.FileNumber}/{e.FileCount})";
                ToolStripProgressBar.Value = e.Progress;
            }
        }

        private void Downloader_DownloaderCompleted(object sender, DownloaderCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Logger.Debug($"{sender.ToString()} cancelled");
            }
            else if (e.Error != null)
            {
                Logger.Error(e.Error);
                MsgBox.Error(Methods.ExeptionParser(e.Error));
            }
            else
            {
                Logger.Debug($"{sender.ToString()} successfuly completed");
                CurrentState = State.Patch;
                Patcher.Run(e.Language);

                return;
            }

            CurrentState = State.Idle;
        }

        private void Patcher_PatcherProgressChanged(object sender, PatcherProgressChangedEventArgs e)
        {
            if (CurrentState == State.Patch)
            {
                switch (e.PatcherState)
                {
                    case Patcher.State.Load:
                        ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_patch")} {StringLoader.GetText("form_status_patch_load")}";
                        ToolStripProgressBar.Style = ProgressBarStyle.Marquee;

                        break;

                    case Patcher.State.Patch:
                        ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_patch")} {StringLoader.GetText("form_status_patch_patch")}";
                        ToolStripProgressBar.Style = ProgressBarStyle.Blocks;
                        if (e.Progress != -1)
                        {
                            ToolStripProgressBar.Value = e.Progress;
                        }

                        break;

                    case Patcher.State.Save:
                        ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_patch")} {StringLoader.GetText("form_status_patch_save")}";
                        ToolStripProgressBar.Style = ProgressBarStyle.Marquee;

                        break;

                    case Patcher.State.ExePatch:
                        ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_patch")} {StringLoader.GetText("form_status_patch_exe")}";
                        ToolStripProgressBar.Style = ProgressBarStyle.Marquee;

                        break;
                }
            }
        }

        private void Patcher_PatcherCompleted(object sender, PatcherCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Logger.Debug($"{sender.ToString()} cancelled");
                DeleteTmpFiles(e.Language);
            }
            else if (e.Error != null)
            {
                Logger.Error(e.Error);
                MsgBox.Error(Methods.ExeptionParser(e.Error));
                DeleteTmpFiles(e.Language);
            }
            else
            {
                Logger.Debug($"{sender.ToString()} successfuly completed");

                string clientIniPath = Path.Combine(UserSettings.GamePath, Strings.IniName.ClientVer);
                if (!Methods.LoadVerIni(out IniFile clientIni, clientIniPath))
                {
                    throw new Exception(StringLoader.GetText("exception_generic_read_error", clientIniPath));
                }
                IniSection clientVerSection = clientIni.Sections[Strings.IniName.Ver.Section];

                string translationIniPath = Path.Combine(e.Language.Path, Strings.IniName.Translation);
                var translationIni = new IniFile();

                IniKey translationDateKey = new IniKey(translationIni, Strings.IniName.Patcher.KeyDate, Methods.DateToString(e.Language.LastUpdate));
                IniSection translationPatcherSection = new IniSection(translationIni, Strings.IniName.Patcher.Section, translationDateKey);

                translationIni.Sections.Add(translationPatcherSection);
                translationIni.Sections.Add(clientVerSection.Copy(translationIni));
                Logger.Debug($"Saving translation config to [{translationIniPath}]");
                translationIni.Save(translationIniPath);
            }

            SWFileManager.DisposeFileData();
            GC.Collect();
            CurrentState = State.Idle;
        }

        private void RTPatcher_DownloadProgressChanged(object sender, RTPatcherDownloadProgressChangedEventArgs e)
        {
            if (CurrentState == State.RTPatch)
            {
                if (e.Progress == -1)
                {
                    ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_prepare")} {e.FileName}";
                    ToolStripProgressBar.Style = ProgressBarStyle.Marquee;
                }
                else
                {
                    ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_update_client")} {e.FileName} - {e.DownloadSpeed}";
                    ToolStripProgressBar.Value = e.Progress;
                }
            }
        }

        private void RTPatcher_ProgressChanged(object sender, RTPatcherProgressChangedEventArgs e)
        {
            if (CurrentState == State.RTPatch)
            {
                if (e.Progress == -1)
                {
                    ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_prepare")} {e.FileName} ({e.FileNumber}/{e.FileCount})";
                    ToolStripProgressBar.Style = ProgressBarStyle.Marquee;
                }
                else
                {
                    ToolStripStatusLabel.Text = $"{StringLoader.GetText("form_status_update_client")} {e.FileName} ({e.FileNumber}/{e.FileCount})";
                    ToolStripProgressBar.Style = ProgressBarStyle.Blocks;
                    ToolStripProgressBar.Value = e.Progress;
                }
            }
        }

        private void RTPatcher_Completed(object sender, RTPatcherCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Logger.Debug($"{sender.ToString()} cancelled");
            }
            else if (e.Error != null)
            {
                if (e.Error is ResultException ex)
                {
                    string logFileName = Path.GetFileName(ex.LogPath);
                    switch (ex.Result)
                    {
                        case 4:
                            Logger.Error(ex.Message);
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_not_exist_directory"));
                            break;

                        case 7:
                            Logger.Error($"error=[{ex.Message}]@Version=[{ex.ClientVersion.ToString()}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_error_open_patch_file"));
                            break;

                        case 9:
                            Logger.Error($"error=[{ex.Message}] file=[{Path.Combine(UserSettings.GamePath, ex.FileName)}] version=[{ex.ClientVersion}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_corrupt", $"{Path.Combine(UserSettings.GamePath, ex.FileName)}@Version=[{ex.ClientVersion}]"));
                            break;

                        case 15:
                            Logger.Error($"error=[{ex.Message}] file=[{Path.Combine(UserSettings.GamePath, ex.FileName)}] version=[{ex.ClientVersion}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_missing_file", $"{Path.Combine(UserSettings.GamePath, ex.FileName)}@Version=[{ex.ClientVersion}]"));
                            break;

                        case 18:
                            Logger.Error($"error=[{ex.Message}]@Version=[{ex.ClientVersion.ToString()}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_open_patch_file_fail"));
                            break;

                        case 20:
                            Logger.Error($"error=[{ex.Message}]@Version=[{ex.ClientVersion.ToString()}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_read_patch_file_fail"));
                            break;

                        case 22:
                            Logger.Error($"error=[{ex.Message}]@Version=[{ex.ClientVersion.ToString()}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_rename_fail"));
                            break;

                        case 29:
                            Logger.Error($"error=[{ex.Message}]@Version=[{ex.ClientVersion.ToString()}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_insufficient_storage"));
                            break;

                        case 32:
                            Logger.Error($"error=[{ex.Message}]@Version=[{ex.ClientVersion.ToString()}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_time_date_fail"));
                            break;

                        case 36:
                            Logger.Error($"error=[{ex.Message}] file=[{Path.Combine(UserSettings.GamePath, ex.FileName)}] version=[{ex.ClientVersion}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_corrupt_file", $"{Path.Combine(UserSettings.GamePath, ex.FileName)}@Version=[{ex.ClientVersion}]"));
                            break;

                        case 49:
                            Logger.Error($"error=[{ex.Message}]@Version=[{ex.ClientVersion.ToString()}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_administrator_required"));
                            break;

                        default:
#if !DEBUG
                            string logFileText = File.ReadAllText(ex.LogPath);

                            try
                            {
                                UploadToPasteBin(logFileName, logFileText, PasteBinExpiration.OneWeek, true, "text");
                            }
                            catch (PasteBinApiException)
                            {
                            }

                            Logger.Error($"See {logFileName} for details. Error Code=[{ex.Result}]");
                            MsgBox.Error(StringLoader.GetText("exception_rtpatch_result", ex.Result, logFileName));
#endif
                            break;
                    }

                    Methods.RTPatchCleanup(true);
                }
                else
                {
                    Methods.RTPatchCleanup(false);
                    Logger.Error(e.Error);
                    MsgBox.Error(Methods.ExeptionParser(e.Error));
                }
            }
            else
            {
                Methods.RTPatchCleanup(true);
                Logger.Debug($"{sender.ToString()} successfuly completed");
                switch (_nextState)
                {
                    case NextState.Download:
                        CurrentState = State.Download;
                        Downloader.Run(e.Language);

                        break;

                    case NextState.Play:
                        GameStarter.Run(e.Language, true);

                        break;

                    case NextState.PlayRaw:
                        GameStarter.Run(e.Language, false);

                        break;
                }

                _nextState = 0;
                return;
            }

            CurrentState = State.Idle;
        }

        private void GameStarter_GameStarterProgressChanged(object sender, GameStarterProgressChangedEventArgs e)
        {
            CurrentState = e.State;
        }

        private void GameStarter_GameStarterCompleted(object sender, GameStarterCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Logger.Debug($"{sender.ToString()} cancelled.");
            }
            else if (e.Error != null)
            {
                Logger.Error(e.Error);
                MsgBox.Error(e.Error.Message);
            }
            else if (e.NeedsForcePatch)
            {
                MsgBox.Notice(StringLoader.GetText("notice_outdated_translation"));
                ResetTranslation(e.Language);

                CurrentState = State.RTPatch;
                _nextState = NextState.Download;
                RTPatcher.Run(e.Language);

                return;
            }
            else
            {
                Logger.Debug($"{sender.ToString()} successfuly completed");
                RestoreFromTray();
            }

            try
            {
                RestoreBackup(e.Language);
            }
            finally
            {
                CurrentState = State.Idle;
            }
        }
    }
}