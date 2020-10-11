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
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MyOhmSessions
{
    public static class UiUtils
    {
        public static string AskUserForFilename(string defaultPath, string extension)
        {
            var theDialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Open Text File",
                Filter = $"TXT files|*{extension}",
                DefaultExt = ".json",
                InitialDirectory = defaultPath,
                CheckFileExists = true
            };

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    return theDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
            return null;
        }

        internal static string AskUserForFolder(string defaultPath, string title)
        {
            string ret=null;

            var dlg = new CommonOpenFileDialog
            {
                Title = title,
                IsFolderPicker = true,
                InitialDirectory = defaultPath,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = defaultPath,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };


            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ret = dlg.FileName;
            }

            return ret;
        }

        private static Cursor _previousCursor;

        public static void CursorStartWait()
        {
            _previousCursor = Cursor.Current;
            // Set cursor as hourglass
            Cursor.Current = Cursors.WaitCursor;
        }
        public static void CursorStopWait()
        {
            _previousCursor = Cursor.Current;
            Cursor.Current = _previousCursor;
        }
    }
}
