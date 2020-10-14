/* 
 * This file is part of the MyOhmSessions distribution (https://github.com/GroovemanAndCo/MyOhmStudio).
 * Copyright (c) 2020 Fabien (https://github.com/fab672000)
 * 
 * This program is free software: you can redistribute it and/or modify  
 * it under the terms of the GNU General Public License as published by  
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NLog;

namespace OhmstudioManager.Utils
{
    /// <summary>
    /// Decoding utilities
    /// </summary>
    public static class EncodeUtils
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static DataProtectionScope Scope = DataProtectionScope.LocalMachine;

        /// Encodes a given string
        public static string Encode(string text)
        {
            if (text == null) throw new ArgumentNullException("text");

            //encrypt data
            var data = Encoding.Unicode.GetBytes(text);
            byte[] data1 = data.Select(x => x += AppCustomSeed).ToArray();
            byte[] data2 = System.Web.Security.MachineKey.Protect(data1, null);
            return Convert.ToBase64String(data2);
        }

        /// Decodes given string.
        public static string Decode(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            //parse base64 string
            try
            {
                byte[] data1 = Convert.FromBase64String(text);
                byte[] data2 =  System.Web.Security.MachineKey.Unprotect(data1, null);
                byte[] result = data2.Select(x => x -= AppCustomSeed).ToArray();
                return Encoding.Unicode.GetString(result);

            }
            catch (Exception ex)
            {

                Logger.Warn(ex, "Failed to use the github api, auto-updates won't be available.");
                return null;
            }
        }

        private const byte AppCustomSeed = 0x42;
    }
}
