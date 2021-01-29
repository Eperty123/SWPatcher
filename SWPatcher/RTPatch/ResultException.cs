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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace SWPatcher.RTPatch
{
    [Serializable]
    internal class ResultException : Exception
    {
        internal ulong Result { get; private set; }
        internal string LogPath { get; private set; }
        internal string FileName { get; private set; }
        internal Version ClientVersion { get; private set; }

        internal ResultException()
        {

        }

        internal ResultException(string message, ulong result, string logPath, string fileName, Version version) : base(message)
        {
            Result = result;
            LogPath = logPath;
            FileName = fileName;
            ClientVersion = version;
        }

        internal ResultException(string message, ulong result, string logPath, string fileName, Version version, Exception innerException) : base(message, innerException)
        {
            Result = result;
            LogPath = logPath;
            FileName = fileName;
            ClientVersion = version;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected ResultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Result = info.GetUInt64("Result");
            LogPath = info.GetString("LogPath");
            FileName = info.GetString("FileName");
            ClientVersion = (Version)info.GetValue("ClientVersion", typeof(Version));
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("Result", Result, typeof(ulong));
            info.AddValue("LogPath", LogPath, typeof(string));
            info.AddValue("FileName", FileName, typeof(string));
            info.AddValue("ClientVersion", ClientVersion, typeof(Version));

            base.GetObjectData(info, context);
        }
    }
}
