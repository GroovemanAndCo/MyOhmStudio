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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using AdonisUI;
using MyOhmSessions.Properties;
using NLog;
using OhmstudioManager;
using OhmstudioManager.Json;
using OhmstudioManager.Utils;
using OhmstudioManager.ViewModel;
using Application = System.Windows.Forms.Application;
using Button = System.Windows.Controls.Button;
using Cursors = System.Windows.Forms.Cursors;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace MyOhmSessions.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged, IAppStateOwner
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region Crash Handling
        private void SetupUncaughtExceptionHandlers()
        {
            // Forms application exception handler
            AppDomain.CurrentDomain.UnhandledException += App_DomainUnhandledException;

            // UI thread exceptions handler.
            Application.ThreadException +=
                UIThreadException;

            // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
            Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);

        }
        private static void UIThreadException(object sender, ThreadExceptionEventArgs e) => MyExceptionHandler(e.Exception, true);
        void App_DomainUnhandledException(object sender, UnhandledExceptionEventArgs e) => MyExceptionHandler(e.ExceptionObject, e.IsTerminating);
        /// <summary>
        /// Uncaught exceptions handler implementation
        /// </summary>
        /// <param name="exceptionObject"></param>
        /// <param name="isTerminating">true if exception is not resumable</param>
        static void MyExceptionHandler(object exceptionObject, bool isTerminating)
        {
            if (!isTerminating)
            {
                Logger.Error(exceptionObject as Exception, "Intercepted resumable unhandled exception.");
                return;
            }

            // Process unhandled exception
            try
            {
                
                if (Logger != null)
                {
                    var m = "Something went wrong... The application will now close. ";
                    if (exceptionObject is Exception e)
                    {
                        Logger.Error(e,  m);
                        m += $"Exception was: {e}";

                    }
                    else
                    {
                        Logger.Error(m);
                        Logger.Info("Exception object is {0}", exceptionObject);
                    }
                    MessageBox.Show($"{m}", "Application Crash Report", MessageBoxButton.OK, MessageBoxImage.Error);
                }


                try
                {
                    // https://stackoverflow.com/questions/10492720/should-nlog-flush-all-queued-messages-in-the-asynctargetwrapper-when-flush-is
                    LogManager.Flush();
                    LogManager.Shutdown();
                }
                catch (Exception ex)
                {
					try
					{
						Logger?.Error(ex, "Failed to shutdown NLog gracefully.");
					}
                    catch (Exception)
                    {
                        // ignored
                    } // nothing to do really here but at least don't prevent critical code to continue to run below...
                }

                Environment.Exit(-1);
            }

            catch (Exception)
            {
                // if we are here, it means everything else failed, 
                // so just exit as fast as possible (if BE is present watchdog timers will then invalidate HW ASAP) ...
                Process.GetCurrentProcess().Kill();
            }
        }
        #endregion

        /// <summary>
        /// Main Application Window constructor
        /// </summary>
        public MainWindow()
        {
            SetupUncaughtExceptionHandlers();
            UiUtils.CursorStartWait();
            MainViewModel = new MainViewModel(this);
            InitializeComponent();
            SetColorScheme(MainViewModel.AppLightColorScheme==true ? AppColorScheme.Light : AppColorScheme.Dark);

            Logger.Info("Window components initialized.");
            MainViewModel.LoadJson();
            CurrentItems = PopulateListView(lvDataBinding);
            MainViewModel.CurrentItem = CurrentItems?[0];
            UiUtils.CursorStopWait();
        }

        /// <summary>
        /// AdonisUI concrete implementation for color scheme setting
        /// </summary>
        /// <param name="appColorScheme"></param>
        public void SetColorScheme(AppColorScheme appColorScheme)
        {
            switch (appColorScheme)
            {
                case AppColorScheme.Dark:
                    ResourceLocator.SetColorScheme(System.Windows.Application.Current.Resources, ResourceLocator.DarkColorScheme); 
                    break;
                case AppColorScheme.Light:
                    ResourceLocator.SetColorScheme(System.Windows.Application.Current.Resources, ResourceLocator.LightColorScheme); 
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(appColorScheme), appColorScheme, null);
            }
        }

        private Result[] PopulateListView(ListView lv, string matching = null)
        {
            var list = MainViewModel.Current?.result?.ToArray();
            if (list==null)
            {
                Logger.Trace("Nothing to populate, returning.");
                return null;
            }
            if (!string.IsNullOrEmpty(matching))
            {
                var lMatching = matching.ToLowerInvariant();
                list = list.Where(x => x.title.ToLowerInvariant().Contains(lMatching)).ToArray();
            }
            if (list.Length == 0) return null;
            lv.SelectedIndex = 0;
            return list;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) => Process.Start(e.Uri.ToString());

        private Result[] _currentItems;
        public Result[] CurrentItems
        {
            get => _currentItems;
            set
            {
                if (_currentItems != value)
                {
                    _currentItems = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _matching;
        private bool _updatingView;
        public string Matching
        {
            get => _matching;
            set
            {
                if (string.CompareOrdinal(_matching, value) == 0) return;
                _matching = value.ToLowerInvariant();
                var items = !string.IsNullOrEmpty(_matching) ? 
                    MainViewModel.Current.result.Where(
                        x => 
                               (x.title       ?.ToLowerInvariant().Contains(_matching) ?? false)
                            || (x.Owners      ?.ToLowerInvariant().Contains(_matching) ?? false)          
                            || (x.Contributors?.ToLowerInvariant().Contains(_matching) ?? false)
                            || (x.short_desc  ?.ToLowerInvariant().Contains(_matching) ?? false)
                            || (x.Styles      ?.ToLowerInvariant().Contains(_matching) ?? false)
                            || (x.Moods       ?.ToLowerInvariant().Contains(_matching) ?? false)
                                )            .ToArray() : CurrentItems;

                if (lvDataBinding?.SelectedItem is Result prevSelection)
                {
                    var item = items?.FirstOrDefault(x => x.nid == prevSelection.nid);
                    if (item != null)
                    {
                        _updatingView = true;
                        lvDataBinding.ItemsSource = items;
                        lvDataBinding.SelectedItem = item; // restore previous selection where user was if possible
                        lvDataBinding.ScrollIntoView(item);
                    }
                    else
                    {
                        lvDataBinding.ItemsSource = items;
                    }
                }
                else
                {
                    if (lvDataBinding != null) lvDataBinding.ItemsSource = items;
                }
                OnPropertyChanged();
                _updatingView = false;
            }
        }
        public MainViewModel MainViewModel { get; set; }

        private void LvDataBinding_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingView) return;
            MainViewModel.CurrentItem = lvDataBinding?.SelectedItem as Result;
            var mix = MainViewModel.CurrentItem?.ExtractMixdown();
            if (!string.IsNullOrEmpty(mix)) MainViewModel.PlayLink(mix, MainViewModel.CurrentItem);

        }

        public bool BackgroundTaskStopped
        {
            get => _backgroundTaskStopped;
            set
            {
                if (value == _backgroundTaskStopped) return;
                _backgroundTaskStopped = value;
                OnPropertyChanged();
            }
        }
        private bool _backgroundTaskStopped = true;

        CancellationTokenSource m_cancelTokenSource;

        private void UpdateDlStatus(string text) => DlStatus.Content = text;
        private bool BackgroundTaskCancel()
        {
            if (!BackgroundTaskStopped)
            {
                UpdateDlStatus("Waiting to cancel...");
                m_cancelTokenSource?.Cancel();
                BackgroundTaskStopped = true;
                return true;
            }
            return false;
        }

        private async void OnDownloadAll(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (BackgroundTaskCancel()) return;

            BackgroundTaskStopped = false;
            UpdateDlStatus("Downloading... ");

            var saveImage = ImgDlAll.Source;
            var saveTooltip = button.ToolTip;
            button.ToolTip = "Cancel downloads";
            ImgDlAll.Source = new BitmapImage(new Uri("../Images/stop.png", UriKind.Relative));

            void Report(double p) => DlProgress.Dispatcher.Invoke(() => DlProgress.Value = p);
            m_cancelTokenSource = new CancellationTokenSource();

            try
            {
                await MainViewModel.DownloadFilesAsync(MainViewModel.Current.result, Report, m_cancelTokenSource.Token);
                UpdateDlStatus("Done");
            }
            catch (OperationCanceledException)
            {
                UpdateDlStatus("Canceled");
            }
            finally
            {
                ImgDlAll.Source = saveImage;
                button.ToolTip = saveTooltip;
                BackgroundTaskStopped = true;
                m_cancelTokenSource = null;
                Report(0);
            }
        }

        private void OnDownload(object sender, RoutedEventArgs e)
        {
            var index = lvDataBinding.SelectedIndex;
            if (index < 0 || index > MainViewModel.Current.result.Length) return;
            MainViewModel.CurrentItem = MainViewModel.Current.result[index];

            try
            {
                var uri = MainViewModel.CurrentItem.mixdown[0].url_m4a;
                if (string.IsNullOrWhiteSpace(uri)) return;
                if (HttpUtils.DownloadFile(uri, MainViewModel.CurrentItem.MixdownFileName, MainViewModel.MixdownFolder))
                {
                    Logger.Info($"Successfully downloaded mixdown '{uri}' for title '{MainViewModel.CurrentItem.title}' ");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    $"Failed to download files, please check directory {MainViewModel.MixdownFolder}");
            }
            // opens the folder in explorer
            var mixdownsFolder = MainViewModel?.MixdownFolder;
            try
            {
                if (!string.IsNullOrWhiteSpace(mixdownsFolder)) Process.Start(mixdownsFolder);
            }
            catch(Exception ex)
            {
                Logger.Error(ex, $"Failed to launch mixdown folder \"{mixdownsFolder}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                MainViewModel.OnClosing();
                BackgroundTaskCancel();

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to cleanup resources while closing");
            }
            base.OnClosing(e);
        }

        private void OnOpenJson(object sender, RoutedEventArgs e)
        {
            var fileName = UiUtils.AskUserForFilename(MainViewModel.DestinationFolder, ".json");
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var path = Path.GetFullPath(fileName);

                MainViewModel.Reset();
                MainViewModel.LoadJson(path);
                CurrentItems = PopulateListView(lvDataBinding, Matching);
                MainViewModel.CurrentItem = CurrentItems?[0];

            }
        }

        private void HandleListViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var result = ((ListViewItem)sender).Content as Result; // as Result; //Casting back to the Track
            var url = result?.url_web;
            var owners = result?.Owners;
            if (string.IsNullOrWhiteSpace(owners))
            {
                DisplayInfoDialog($"No owners found for this session, launch cancelled for: {url}","Session has no owner");
                return;
            }
            if (!string.IsNullOrWhiteSpace(url)) MainViewModel.LaunchSessionWithOhmstudio(result.nid);
        }

        private void OnAbout(object sender, RoutedEventArgs e)
        {
            var About = new AboutDialog(this, MainViewModel);
            About.ShowDialog();
        }

        private void BtnDonate_Click(object sender, EventArgs e) => AboutDialog.OnDonate();

        private void OnShowMatchesFor(object sender, RoutedEventArgs e)
        {
            if (tbMatch?.Text==null) return;
            Matching = tbMatch.Text;
        }

        public void DisplayErrorDialog(string message, string title)
        {
            Logger.Error($"Error Dialog: '{message}' with title '{title}'");
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void DisplayInfoDialog(string message, string title)
        {
            Logger.Info($"Info Dialog: '{message}' with title '{title}'");
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool DisplayInfoDialogOkCancel(string message, string title)
        {
            var result = MessageBox.Show("Please Launch the OhmStudio client and authenticate then click Ok", 
                "OhmStudio is not launched", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            return result != MessageBoxResult.Cancel;
        }
        public bool DisplayWarningDialogOkCancel(string message, string title)
        {
            var result = MessageBox.Show("Please Launch the OhmStudio client and authenticate then click Ok", 
                "OhmStudio is not launched", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            return result != MessageBoxResult.Cancel;
        }

        public string AskUserForFilename(string defaultPath, string extension) => UiUtils.AskUserForFilename(defaultPath, extension);
        public string AskUserForFolder(string defaultPath, string title) => UiUtils.AskUserForFolder(defaultPath, title);

        public void OnPlaybackStopped()
        {
            if (MainViewModel.AutoPlaySetting && MainViewModel.IsStopped && lvDataBinding!= null)
            {
                var index = lvDataBinding.SelectedIndex;
                var total = lvDataBinding.Items.Count;
                var next = (index + 1) % total;
                lvDataBinding.SelectedIndex = next;
            }
        }

        public void IndicateBusy(bool indicate)
        {
            if (indicate) System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
            else System.Windows.Forms.Cursor.Current = Cursors.Default;
        }


        private void OnOpenOugFbPage(object sender, RoutedEventArgs e) =>  Process.Start(@"https://www.facebook.com/groups/ohmstudioevents");

        private void OnOpenJsonLink(object sender, RoutedEventArgs e) => Process.Start("http://www.ohmstudio.com/v3/feed/my_projects?page=0");

        private void OnTextMatchingKeydown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)  OnShowMatchesFor(btnContains, e);
        }

        private void OnPlayerForward(object sender, RoutedEventArgs e)     => MainViewModel.PlayerControl(PlayerAction.Forward);
        private void OnPlayerRewind(object sender, RoutedEventArgs e)      => MainViewModel.PlayerControl(PlayerAction.Rewind);
        private void OnPlayerStop(object sender, RoutedEventArgs e)
        {
            MainViewModel.AutoPlaySetting = false;
            MainViewModel.PlayerControl(PlayerAction.Stop);
        }

        private void OnPlayerPlayResume(object sender, RoutedEventArgs e)  => MainViewModel.PlayerControl(PlayerAction.Play);

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (MainViewModel.ShowAboutAtStartup)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action (() => new AboutDialog(this, MainViewModel).ShowDialog()) );
            }
        }

        private void OnSaveToFolder(object sender, RoutedEventArgs e) => MainViewModel.OnSaveToFolder(sender, e);
    }
}
