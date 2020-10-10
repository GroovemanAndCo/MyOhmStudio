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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MyOhmSessions.Properties;
using OhmstudioManager;

namespace MyOhmSessions.UI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AboutDialog
    {
        StateOwner StateOwner { get; }

        public AboutDialog(MainWindow owner)
        {
            this.Owner = owner;
            StateOwner = owner;
            InitializeComponent();
            ShowAtStartup.IsChecked = Settings.Default.StartupShowAbout; 

        }

        public static Version Version => typeof(AboutDialog).Assembly.GetName().Version;
        public static string AppName => Assembly.GetExecutingAssembly().GetName().Name;
        public static string AppLocation => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public string OnTitle => $"About {AppName} {Version.Major}.{Version.Minor}.{Version.Build}";

        private void OnOK(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSpecialThanks(object sender, RoutedEventArgs e)
        {
            var msg = "A very special thanks to: irockus, Flavio67, jazzcrime, jamie57lp for their outstanding support!";
            StateOwner.DisplayInfoDialog(msg, "Special Thanks!");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) => Process.Start(e.Uri.ToString());

        private void OnOpenOugFbPage(object sender, RoutedEventArgs e) => Process.Start(@"https://www.facebook.com/groups/ohmstudioevents");
        private void OnDiscordRefugePage(object sender, RoutedEventArgs e) => Process.Start(@"https://discord.com/channels/407428324101455884");
        private void OnLicense(object sender, RoutedEventArgs e) => Process.Start(Path.Combine(AppLocation, "License.txt"));

        public static void OnDonate()
        {
            {
                var url = "";

                const string business = "fabien@onepost.net"; // your paypal email
                const string description = "Support my development efforts and pay me a coffee :) Thank you!"; // '%20' represents a space. remember HTML!
                const string country = "CAN"; // AU, US, etc.
                const string currency = "CAD"; // AUD, USD, etc.

                url += "https://www.paypal.com/cgi-bin/webscr" +
                       "?cmd=" + "_donations" + "&business=" + business + "&lc=" + country + "&item_name=" + description + "&currency_code=" + currency + "&bn=" + "PP%2dDonationsBF";

                Process.Start(url);
            }

        }

        private void OnDonateBtn(object sender, RoutedEventArgs e) => OnDonate();

        private void OnShowAtStartup(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox c)) return;
            Settings.Default.StartupShowAbout = c.IsChecked ?? false;
            Settings.Default.Save();
        }
    }
}
