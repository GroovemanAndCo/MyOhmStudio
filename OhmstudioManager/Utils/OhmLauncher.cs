﻿/* 
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
using OhmstudioManager;
using System.Diagnostics;

namespace MyOhmSessions
{

    public class OhmLauncher
    {
        private const string OhmStudioGuest = "ohm_studio_guest";
        private const string OhmStudioNetMp = "ohm_studio_net_mp_host";

        private static bool checkProcesses()
        {
            var pName = Process.GetProcessesByName(OhmStudioGuest);
            var pName2 = Process.GetProcessesByName(OhmStudioNetMp);
            return pName.Length != 0 && pName2.Length != 0;
        }
        public static bool CheckIfLaunched(StateOwner parent)
        {

            while (!checkProcesses())
            {
                var result = parent.DisplayWarningDialogOkCancel("Please Launch the OhmStudio client and authenticate then click Ok", "OhmStudio is not launched");
                if (!result ) return false;
            }

            return true;
        }
    }
}
