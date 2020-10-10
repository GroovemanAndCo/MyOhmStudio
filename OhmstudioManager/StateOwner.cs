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

namespace OhmstudioManager
{
    public interface StateOwner
    {
        string DestinationFolder { get; }
        bool AutoPlaySetting {get;}
        void DisplayErrorDialog(string message, string title);
        void DisplayInfoDialog(string message, string title);
        bool DisplayInfoDialogOkCancel(string message, string title);
        bool DisplayWarningDialogOkCancel(string message, string title);
        string AskUserForFilename(string defaultPath, string extension);
        string AskUserForFolder(string defaultPath, string title);
        void OnPlaybackStopped();
        void IndicateBusy(bool indicate);
    }
}