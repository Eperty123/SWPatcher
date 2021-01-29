﻿/*
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

using SWPatcher.Properties;
using System;
using System.IO;
using System.Reflection;

namespace SWPatcher.Helpers.GlobalVariables
{
    internal sealed class UserSettings
    {
        internal static string PatcherPath
        {
            get
            {
                if (!Directory.Exists(Settings.Default.PatcherWorkingDirectory))
                {
                    return PatcherPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Assembly.GetExecutingAssembly().GetName().Name);
                }
                else
                {
                    return Settings.Default.PatcherWorkingDirectory.Replace("\\\\", "\\");
                }
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    value = value.Replace("\\\\", "\\");
                    Directory.CreateDirectory(value);
                    Directory.SetCurrentDirectory(value);
                }

                Settings.Default.PatcherWorkingDirectory = value;
                Settings.Default.Save();
                Logger.Info($"Patcher path set to [{value}]");
            }
        }

        internal static string GamePath
        {
            get
            {
                return Settings.Default.GameDirectory.Replace("\\\\", "\\");
            }
            set
            {
                value = value.Replace("\\\\", "\\");

                Methods.EnsureDirectoryRights(value);
                Settings.Default.GameDirectory = value;
                Settings.Default.Save();
                Logger.Info($"Game path set to [{value}]");
            }
        }

        internal static string CustomGamePath
        {
            get
            {
                return string.IsNullOrWhiteSpace(Settings.Default.CustomGameDirectory) ? "" : Settings.Default.CustomGameDirectory.Replace("\\\\", "\\");
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    value = value.Replace("\\\\", "\\");

                    Methods.EnsureDirectoryRights(value);
                    Settings.Default.CustomGameDirectory = value;
                    Settings.Default.Save();
                    Logger.Info($"Custom game path set to [{value}]");
                }
            }
        }

        internal static string CustomGameIp
        {
            get
            {
                return string.IsNullOrWhiteSpace(Settings.Default.CustomGameIp) ? "127.0.0.1" : Settings.Default.CustomGameIp;
            }
            set
            {
                Settings.Default.CustomGameIp = value;
                Settings.Default.Save();
                Logger.Info($"Custom game ip set to [{value}]");
            }
        }

        internal static string CustomGamePort
        {
            get
            {
                return string.IsNullOrWhiteSpace(Settings.Default.CustomGamePort) ? "10000" : Settings.Default.CustomGamePort;
            }
            set
            {
                Settings.Default.CustomGamePort = value;
                Settings.Default.Save();
                Logger.Info($"Custom game port set to [{value}]");
            }
        }

        internal static bool WantToPatchExe
        {
            get
            {
                return Settings.Default.WantToPatchSoulworkerExe;
            }
            set
            {
                Settings.Default.WantToPatchSoulworkerExe = value;
                Settings.Default.Save();
                Logger.Info($"Patch .exe choice set to [{value}]");
            }
        }

        internal static string GameId
        {
            get
            {
                return Settings.Default.GameUserId;
            }
            set
            {
                Settings.Default.GameUserId = value;
                Settings.Default.Save();
            }
        }

        internal static string GamePw
        {
            get
            {
                return Settings.Default.GameUserPassword;
            }
            set
            {
                Settings.Default.GameUserPassword = value;
                Settings.Default.Save();
            }
        }

        internal static bool WantToLogin
        {
            get
            {
                return Settings.Default.WantToLoginWithPatcher;
            }
            set
            {
                Settings.Default.WantToLoginWithPatcher = value;
                Settings.Default.Save();
                Logger.Info($"Direct login choice set to [{value}]");
            }
        }

        internal static string UILanguageCode
        {
            get
            {
                return Settings.Default.UILanguage;
            }
            set
            {
                Settings.Default.UILanguage = value;
                Settings.Default.Save();
                Logger.Info($"UI Language set to [{value}]");
            }
        }

        internal static string LanguageId
        {
            get
            {
                return Settings.Default.LanguageId;
            }
            set
            {
                Settings.Default.LanguageId = value;
                Settings.Default.Save();
            }
        }

        internal static string RegionId
        {
            get
            {
                return Settings.Default.RegionId;
            }
            set
            {
                Settings.Default.RegionId = value;
                Settings.Default.Save();
            }
        }

        internal static bool UpdateSettings
        {
            get
            {
                return Settings.Default.UpdateSettings;
            }
            set
            {
                Settings.Default.UpdateSettings = value;
                Settings.Default.Save();
            }
        }

        internal static bool UseCustomTranslationServer
        {
            get
            {
                return Settings.Default.UseCustomTranslationServer;
            }
            set
            {
                Settings.Default.UseCustomTranslationServer = value;
                Settings.Default.Save();
            }
        }

        internal static bool BypassTranslationDateCheck
        {
            get
            {
                return Settings.Default.BypassTranslationDateCheck;
            }
            set
            {
                Settings.Default.BypassTranslationDateCheck = value;
                Settings.Default.Save();
            }
        }

        internal static string CustomTranslationServer
        {
            get
            {
                return Settings.Default.CustomTranslationServer ?? "";
            }
            set
            {
                Settings.Default.CustomTranslationServer = value;
                Settings.Default.Save();
            }
        }
    }
}
