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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using SWPatcher.General;
using SWPatcher.Helpers;
using SWPatcher.Helpers.GlobalVariables;

namespace SWPatcher.Forms
{
    internal partial class SettingsForm : Form
    {
        private bool PendingRestart;

        //private string GameClientDirectory;
        private string PatcherWorkingDirectory;

        private string ServerIp;
        private string ServerPort;
        private string CustomGamePath;
        private bool WantToPatchSoulworkerExe;
        private string GameUserId;
        private string GameUserPassword;
        private bool WantToLogin;
        private string UILanguage;

        internal SettingsForm()
        {
            InitializeComponent();
            InitializeTextComponent();
        }

        private void InitializeTextComponent()
        {
            Text = StringLoader.GetText("form_settings");
            ButtonOk.Text = StringLoader.GetText("button_ok");
            ButtonCancel.Text = StringLoader.GetText("button_cancel");
            ButtonApply.Text = StringLoader.GetText("button_apply");
            TabPageGame.Text = StringLoader.GetText("tab_game");
            GroupBoxGameDirectory.Text = StringLoader.GetText("box_game_dir");
            ButtonOpenGameDirectory.Text = StringLoader.GetText("button_open");
            CustomGamePathButton.Text = StringLoader.GetText("button_change");
            ButtonChangePatcherDirectory.Text = StringLoader.GetText("button_change");
            GroupBoxPatchExe.Text = StringLoader.GetText("box_patch_exe");
            CheckBoxPatchExe.Text = StringLoader.GetText("check_patch_exe");
            TabPageCredentials.Text = StringLoader.GetText("tab_credentials");
            GroupBoxGameUserId.Text = StringLoader.GetText("box_id");
            GroupBoxGameUserPassword.Text = StringLoader.GetText("box_pw");
            GroupBoxGameWantLogin.Text = StringLoader.GetText("box_want_login");
            CheckBoxWantToLogin.Text = StringLoader.GetText("check_want_login");
            TabPagePatcher.Text = StringLoader.GetText("tab_patcher");
            GroupBoxPatcherDirectory.Text = StringLoader.GetText("box_patcher_dir");
            GroupBoxUILanguagePicker.Text = StringLoader.GetText("box_language");
            GroupBoxGameOptions.Text = StringLoader.GetText("box_game_options");
            ButtonOpenGameOptions.Text = StringLoader.GetText("button_game_options");
            CustomSourceWarnLabel.Text = "NOTE: The translation source can be a url or a path.\nMake sure it has the correct format, otherwise it won't work.";
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            PendingRestart = false;
            TextBoxGameDirectory.Text = UserSettings.GamePath;// = this.GameClientDirectory = UserSettings.GamePath;
            ServerIpTextBox.Text = UserSettings.CustomGameIp;
            ServerPortTextBox.Text = UserSettings.CustomGamePort;
            CustomGamePathTextBox.Text = UserSettings.CustomGamePath;
            TextBoxPatcherDirectory.Text = PatcherWorkingDirectory = UserSettings.PatcherPath;
            CheckBoxPatchExe.Checked = WantToPatchSoulworkerExe = UserSettings.WantToPatchExe;
            TextBoxId.Text = GameUserId = UserSettings.GameId;
            TextBoxId.Enabled = TextBoxPassword.Enabled = CheckBoxWantToLogin.Checked = WantToLogin = UserSettings.WantToLogin;
            TranslationSourceTextBox.Text = UserSettings.CustomTranslationServer;
            TranslationSourceCheckBox.Checked = UserSettings.UseCustomTranslationServer;
            TranslationBypassDateCheckBox.Checked = UserSettings.BypassTranslationDateCheck;

            string maskedEmptyString = SHA256String(string.Empty);
            string maskedGamePw = MaskPassword(UserSettings.GamePw);
            if (maskedEmptyString == maskedGamePw)
            {
                TextBoxPassword.Text = GameUserPassword = string.Empty;
            }
            else
            {
                TextBoxPassword.Text = GameUserPassword = maskedGamePw;
            }

            var def = new ResxLanguage(StringLoader.GetText("match_windows"), "default");
            var en = new ResxLanguage("English", "en");
            var ko = new ResxLanguage("한국어", "ko");
            var vi = new ResxLanguage("Tiếng Việt", "vi");
            var ru = new ResxLanguage("Русский", "ru");
            ComboBoxUILanguage.DataSource = new ResxLanguage[] { def, en, ko, vi, ru };
            string savedCode = UILanguage = UserSettings.UILanguageCode;
            if (en.Code == savedCode)
            {
                ComboBoxUILanguage.SelectedItem = en;
            }
            else if (ko.Code == savedCode)
            {
                ComboBoxUILanguage.SelectedItem = ko;
            }
            else if (vi.Code == savedCode)
            {
                ComboBoxUILanguage.SelectedItem = vi;
            }
            else if (ru.Code == savedCode)
            {
                ComboBoxUILanguage.SelectedItem = ru;
            }
            else
            {
                ComboBoxUILanguage.SelectedItem = def;
            }

            TextBoxPatcherDirectory.TextChanged += EnableApplyButton;
            CheckBoxPatchExe.CheckedChanged += EnableApplyButton;
            TextBoxId.TextChanged += EnableApplyButton;
            TextBoxPassword.TextChanged += EnableApplyButton;
            CheckBoxWantToLogin.CheckedChanged += EnableApplyButton;
            ComboBoxUILanguage.SelectedIndexChanged += EnableApplyButton;
            CustomGamePathTextBox.TextChanged += EnableApplyButton;
            ServerIpTextBox.TextChanged += EnableApplyButton;
            ServerPortTextBox.TextChanged += EnableApplyButton;
            TranslationSourceCheckBox.CheckedChanged += EnableApplyButton;
            TranslationBypassDateCheckBox.CheckedChanged += EnableApplyButton;
            TranslationSourceTextBox.TextChanged += EnableApplyButton;
        }

        private void EnableApplyButton(object sender, EventArgs e)
        {
            ButtonApply.Enabled = true;
        }

        private void ButtonOpenGameDirectory_Click(object sender, EventArgs e)
        {
            string path = UserSettings.GamePath;
            if (!string.IsNullOrWhiteSpace(path))
            {
                Process.Start(path);
            }
        }

        private void ButtonChangePatcherDirectory_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog
            {
                Description = StringLoader.GetText("dialog_folder_change_patcher_dir")
            })
            {
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    if (Methods.IsValidSwPatcherPath(folderDialog.SelectedPath))
                    {
                        TextBoxPatcherDirectory.Text = PatcherWorkingDirectory = folderDialog.SelectedPath;
                    }
                    else
                    {
                        DialogResult dialogResult = MsgBox.Question(StringLoader.GetText("question_folder_same_path_game"));

                        if (dialogResult == DialogResult.Yes)
                        {
                            TextBoxPatcherDirectory.Text = PatcherWorkingDirectory = folderDialog.SelectedPath;
                        }
                    }
                }
            }
        }

        private void CheckBoxPatchExe_CheckedChanged(object sender, EventArgs e)
        {
            WantToPatchSoulworkerExe = CheckBoxPatchExe.Checked;
        }

        private void TextBoxId_TextChanged(object sender, EventArgs e)
        {
            GameUserId = TextBoxId.Text;
        }

        private void TextBoxPassword_TextChanged(object sender, EventArgs e)
        {
            GameUserPassword = TextBoxPassword.Text;
        }

        private void CheckBoxWantToLogin_CheckedChanged(object sender, EventArgs e)
        {
            TextBoxId.Enabled = TextBoxPassword.Enabled = WantToLogin = CheckBoxWantToLogin.Checked;
        }

        private void ComboBoxUILanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            UILanguage = (ComboBoxUILanguage.SelectedItem as ResxLanguage).Code;
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            if (ButtonApply.Enabled)
            {
                ApplyChanges();
            }

            DialogResult = DialogResult.OK;
        }

        private void ButtonApply_Click(object sender, EventArgs e)
        {
            ApplyChanges();
        }

        private void ButtonOpenGameOptions_Click(object sender, EventArgs e)
        {
            string optionExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Strings.FileName.OptionExe);

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = UserSettings.GamePath,
                FileName = optionExePath
            };
            Process.Start(startInfo);
        }

        private void ApplyChanges()
        {
            try
            {
                if (UserSettings.PatcherPath != PatcherWorkingDirectory)
                {
                    MoveOldPatcherFolder(UserSettings.PatcherPath, PatcherWorkingDirectory, (Owner as MainForm).GetTranslationFolders());

                    UserSettings.PatcherPath = PatcherWorkingDirectory;
                }

                if (UserSettings.WantToPatchExe != WantToPatchSoulworkerExe)
                {
                    Region region = (Owner as MainForm).GetSelectedRegion();
                    string gameExePatchedPath = Path.Combine(UserSettings.PatcherPath, region.Folder, Methods.GetGameExeName(region.Id));
                    if (File.Exists(gameExePatchedPath))
                    {
                        File.Delete(gameExePatchedPath);
                    }

                    UserSettings.WantToPatchExe = WantToPatchSoulworkerExe;
                }

                if (UserSettings.GameId != GameUserId)
                {
                    UserSettings.GameId = GameUserId;
                }

                if (GameUserPassword != MaskPassword(UserSettings.GamePw))
                {
                    using (var secure = Methods.ToSecureString(GameUserPassword))
                    {
                        UserSettings.GamePw = Methods.EncryptString(secure);
                    }
                }

                if (UserSettings.WantToLogin != WantToLogin)
                {
                    UserSettings.WantToLogin = WantToLogin;
                }

                if (UserSettings.UILanguageCode != UILanguage)
                {
                    UserSettings.UILanguageCode = UILanguage;
                    PendingRestart = true;
                }

                if (UserSettings.CustomGamePath != CustomGamePath && CustomGamePath != null)
                {
                    UserSettings.CustomGamePath = CustomGamePath;
                    if ((Owner as MainForm).CurrentState == MainForm.State.RegionNotInstalled)
                    {
                        (Owner as MainForm).CurrentState = MainForm.State.Idle;
                    }
                    var region = (Owner as MainForm).GetSelectedRegion();
                    if (region != null && region.Id == "jpc")
                    {
                        TextBoxGameDirectory.Text = CustomGamePath;
                        UserSettings.GamePath = CustomGamePath;
                    }
                }

                if (UserSettings.CustomGameIp != ServerIp)
                {
                    UserSettings.CustomGameIp = ServerIp;
                }

                if (UserSettings.CustomGamePort != ServerPort)
                {
                    UserSettings.CustomGamePort = ServerPort;
                }

                if (UserSettings.CustomTranslationServer != TranslationSourceTextBox.Text)
                {
                    UserSettings.CustomTranslationServer = TranslationSourceTextBox.Text;
                    PendingRestart = true;
                }

                if (UserSettings.UseCustomTranslationServer != TranslationSourceCheckBox.Checked)
                {
                    UserSettings.UseCustomTranslationServer = TranslationSourceCheckBox.Checked;
                    PendingRestart = true;
                }

                if (UserSettings.BypassTranslationDateCheck != TranslationBypassDateCheckBox.Checked)
                {
                    UserSettings.BypassTranslationDateCheck = TranslationBypassDateCheckBox.Checked;
                }

                ButtonApply.Enabled = false;

                if (PendingRestart)
                {
                    MsgBox.Notice(StringLoader.GetText("notice_pending_restart"));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MsgBox.Error(Methods.ExeptionParser(ex));
            }
        }

        private static void MoveOldPatcherFolder(string oldPath, string newPath, IEnumerable<string> translationFolders)
        {
            string[] movingFolders = translationFolders.Where(s => Directory.Exists(s)).ToArray();
            string rtpLogsDirectory = Path.Combine(oldPath, Strings.FolderName.RTPatchLogs);
            string logFilePath = Path.Combine(oldPath, Strings.FileName.Log);

            foreach (var folder in movingFolders)
                MoveDirectory(Path.Combine(oldPath, folder), newPath);

            MoveDirectory(rtpLogsDirectory, newPath);

            MoveFile(logFilePath, newPath, false);
        }

        private static bool MoveDirectory(string directory, string newPath)
        {
            if (Directory.Exists(directory))
            {
                string destination = Path.Combine(newPath, Path.GetFileName(directory));
                Directory.CreateDirectory(destination);

                foreach (var dirPath in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(directory, destination));

                foreach (var filePath in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                    MoveFile(filePath, filePath.Replace(directory, destination), true);

                Directory.Delete(directory, true);
                return true;
            }

            return false;
        }

        private static bool MoveFile(string file, string newPath, bool newPathHasFileName)
        {
            if (File.Exists(file))
            {
                string newFilePath = "";
                if (newPathHasFileName)
                {
                    newFilePath = newPath;
                }
                else
                {
                    newFilePath = Path.Combine(newPath, Path.GetFileName(file));
                }

                if (File.Exists(newFilePath))
                {
                    File.Delete(newFilePath);
                }

                File.Move(file, newFilePath);

                return true;
            }

            return false;
        }

        private static string SHA256String(string str)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] result = sha256.ComputeHash(Encoding.Unicode.GetBytes(str));
                StringBuilder sb = new StringBuilder();

                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }

        private static string MaskPassword(string password)
        {
            using (SecureString secure = Methods.DecryptString(password))
            {
                return SHA256String(Methods.ToInsecureString(secure));
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CustomGamePathButton_Click_1(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                //UserSettings.CustomGamePath = fbd.SelectedPath;
                if (!Methods.IsValidSwPath(fbd.SelectedPath))
                {
                    MsgBox.Error("Please select a valid client path");
                    return;
                }
                CustomGamePathTextBox.Text = fbd.SelectedPath;
                CustomGamePath = fbd.SelectedPath;
                if (!ButtonApply.Enabled) ButtonApply.Enabled = true;
            }
        }

        private void ServerIpTextBox_TextChanged(object sender, EventArgs e)
        {
            ServerIp = ServerIpTextBox.Text;
        }

        private void ServerPortTextBox_TextChanged(object sender, EventArgs e)
        {
            ServerPort = ServerPortTextBox.Text;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            TranslationSourceCheckBox.Checked = !TranslationSourceCheckBox.Checked;
        }

        private void label2_Click(object sender, EventArgs e)
        {
            TranslationBypassDateCheckBox.Checked = !TranslationBypassDateCheckBox.Checked;
        }
    }
}
