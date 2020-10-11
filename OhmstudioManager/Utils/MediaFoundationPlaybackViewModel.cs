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
using NAudio.Wave;
using NLog;
using OhmstudioManager.Json;
using OhmstudioManager.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using OhmstudioManager.Properties;

namespace OhmstudioManager.Utils
{
    // See also: https://www.pluralsight.com/guides/building-a-wpf-media-player-using-naudio

    public class MediaFoundationPlaybackViewModel : ViewModelBase, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private int _requestFloatOutput;
        private string _inputPath;
        private string _defaultDecompressionFormat;
        private IWavePlayer _wavePlayer;
        private WaveStream _wavStreamReader;

        public RelayCommand LoadCommand { get; }
        public RelayCommand PlayCommand { get; }
        public RelayCommand PauseCommand { get; }
        public RelayCommand StopCommand { get; }

        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private double _sliderPosition;
        private readonly ObservableCollection<string> inputPathHistory;
        private string lastPlayed;
        private IAppStateOwner AppStateOwner { get; }

        public MediaFoundationPlaybackViewModel(IAppStateOwner owner)
        {
            AppStateOwner = owner;
            inputPathHistory = new ObservableCollection<string>();
            LoadCommand = new RelayCommand(Load, () => IsStopped);
            PlayCommand = new RelayCommand(Play, () => !IsPlaying);
            PauseCommand = new RelayCommand(Pause, () => IsPlaying);
            StopCommand = new RelayCommand(Stop, () => !IsStopped);
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += OnTimerUpdatePositions;
        }

        public bool IsPlaying => _wavePlayer != null && _wavePlayer.PlaybackState == PlaybackState.Playing;

        public bool IsStopped => _wavePlayer == null || _wavePlayer.PlaybackState == PlaybackState.Stopped;


        private const double SliderMax = 10.0;

        private void OnTimerUpdatePositions(object sender, EventArgs eventArgs)
        {
            {
                if (_wavStreamReader != null)
                {
                    _sliderPosition = Math.Min(SliderMax, _wavStreamReader.Position * SliderMax / _wavStreamReader.Length);
                    OnPropertyChanged(nameof(SliderPosition));
                    SongCurrentPosition = _wavStreamReader.CurrentTime;
                }

            }
        }

        public TimeSpan SongCurrentPosition
        {
            get => _songCurrentPosition;
            set
            {
                if (_songCurrentPosition == value) return;
                _songCurrentPosition = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan SongDuration
        {
            get => _songDuration;
            set
            {
                if (_songDuration == value) return;
                _songDuration = value;
                OnPropertyChanged();
            }
        }

        private int _initialSliderOffsetPct = Settings.Default.UserSongOffset;
        public int InitialSliderOffsetPct
        {
            get => _initialSliderOffsetPct;
            set
            {
                if (_initialSliderOffsetPct == value) return;
                _initialSliderOffsetPct = Math.Max(0, Math.Min(99, value));
                OnPropertyChanged();
            }
        }

        public double SliderPosition
        {
            get => _sliderPosition;
            set
            {
                if (Math.Abs(_sliderPosition - value) < .001) return;
                _sliderPosition = value;
                {
                    if (_wavStreamReader != null)
                    {
                        var pos = (long)(_wavStreamReader.Length * _sliderPosition / SliderMax);
                        _wavStreamReader.Position = pos; // media foundation will worry about block align for us
                        SongCurrentPosition = _wavStreamReader.CurrentTime;
                    }

                }
                OnPropertyChanged();
            }
        }

        public int RequestFloatOutput
        {
            get => _requestFloatOutput;
            set
            {
                if (_requestFloatOutput != value)
                {
                    _requestFloatOutput = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DefaultDecompressionFormat
        {
            get => _defaultDecompressionFormat;
            set
            {
                _defaultDecompressionFormat = value;
                OnPropertyChanged();
            }
        }

        public string InputPath
        {
            get => _inputPath;
            set
            {
                if (_inputPath != value)
                {
                    _inputPath = value;
                    AddToHistory(value);
                    OnPropertyChanged();
                }
            }
        }

        private void AddToHistory(string value)
        {
            if (!inputPathHistory.Contains(value))
            {
                inputPathHistory.Add(value);
            }
        }

        public void ResetPosition()
        {
            SliderPosition = 0;
        }
        protected void Rewind(long seconds = 10) => Forward(-seconds);

        protected void Forward(long seconds = 10)
        {
            {
                if (_wavStreamReader == null) return;

                double p = _wavStreamReader.Position;
                var l = _wavStreamReader.Length;
                var ls = _wavStreamReader.TotalTime.TotalSeconds;
                var ps = p / l * ls;
                p = ((ps + seconds) / ls) * l;
                _wavStreamReader.Position = seconds > 0 ?
                    Math.Min((long)p, Math.Max(l - 16, 0L)) : Math.Max((long)p, 0L);
            }
        }

        protected void Stop()
        {
            if (_timer.IsEnabled) _timer.Stop();
            _wavePlayer?.Stop();
            ResetPosition();
        }

        protected void Pause()
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.Pause();
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(IsStopped));
            }
        }

        protected void PlayResume()
        {
            if (_wavePlayer == null) CreatePlayer();
            if (_wavePlayer.PlaybackState == PlaybackState.Playing) Pause(); else Resume();
        }

        private TimeSpan _songCurrentPosition = TimeSpan.FromSeconds(0);
        private TimeSpan _songDuration = TimeSpan.FromSeconds(0);
        private bool _fromResume;
        private void Resume()
        {
            _fromResume = true;
            try
            {
                Play();

            }
            finally
            {
                _fromResume = false;
            }

        }
        protected void Play()
        {
            if (string.IsNullOrEmpty(InputPath))
            {
                Logger.Warn("Can't play anything yet, select a valid input file or URL first.");
                return;
            }

            {
                if (_wavePlayer == null) CreatePlayer();
                if (lastPlayed != InputPath && _wavStreamReader != null)
                {
                    _wavStreamReader.Dispose();
                    _wavStreamReader = null;
                }
                if (_wavStreamReader == null)
                {
                    _wavStreamReader = new MediaFoundationReader(InputPath);
                    lastPlayed = InputPath;
                    _wavePlayer.Init(_wavStreamReader);
                }
                _wavePlayer.Play();
                SongDuration = _wavStreamReader.TotalTime;
                if (!_fromResume && InitialSliderOffsetPct > 0)
                {
                    SliderPosition = 10 * ((double)InitialSliderOffsetPct) / 100;
                }
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(IsStopped));
                _timer.Start();
            }
        }

        private void CreatePlayer()
        {
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.PlaybackStopped += OnPlaybackStopped;
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            if (_wavStreamReader != null)
            {
                //SliderPosition = 0;
            }

            if (stoppedEventArgs.Exception != null)
            {
                Logger.Warn(stoppedEventArgs.Exception, "Error while stopping file.");
            }

            if (AutoPlaySetting && _wavePlayer?.PlaybackState == PlaybackState.Stopped)
            {
                CurrentlyPlaying = null;
                if (_timer.IsEnabled) _timer.Stop();

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => AppStateOwner.OnPlaybackStopped())); // inform main app that playback just stopped, maybe time to play next song?
                // StateOwner.OnPlaybackStopped(); // inform main app that playback just stopped, maybe time to play next song?
            }
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(IsStopped));
        }

        private void Load()
        {
            {
                if (_wavStreamReader != null)
                {
                    _wavStreamReader.Dispose();
                    _wavStreamReader = null;
                }

            }
        }

        public void Dispose()
        {
            {
                _wavePlayer?.Dispose();
                _wavStreamReader?.Dispose();
            }
        }

        public string MixdownFolder => Path.Combine(DestinationFolder, "Mixdowns");
        public string FullMixdownPath(string mixFileName) => string.IsNullOrEmpty(mixFileName) ? mixFileName : Path.Combine(MixdownFolder, mixFileName);

        public string CurrentlyPlaying { get; set; }
        public void PlayLink(string link, Result current = null)
        {
            AppStateOwner.IndicateBusy(true);
            try
            {
                if (_wavePlayer != null && _wavePlayer.PlaybackState == PlaybackState.Playing)
                {
                    if (link != null && current != null && CurrentlyPlaying == current.ExtractMixdown()) return;
                    Stop();
                }

                CurrentlyPlaying = link;

                if (string.IsNullOrWhiteSpace(link)) return;

                if (current == null) return;

                // use local cached version if any 
                var mixdownFileName = current.MixdownFileName;
                string cached = current.CachedPath;
                if (string.IsNullOrEmpty(cached) || !File.Exists(cached)) cached = FullMixdownPath(mixdownFileName);
                if (!File.Exists(cached))
                {
                    // Okay no way to find a cached file version so try download from web:
                    if (HttpUtils.DownloadFile(link, mixdownFileName, MixdownFolder))
                    {
                        Logger.Info($"Successfully downloaded mixdown '{link}' for title '{current.title}' ");
                        current.CachedPath = cached;
                    }
                    else
                    {
                        Logger.Info($"Unable to fetch non cached mixdown from url {link}, playback unavailable.");
                        return;
                    }

                }

                try
                {
                    InputPath = cached;
                    if (AutoPlaySetting) Play();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to play title {current.title}");
                }

            }
            finally
            {
                AppStateOwner.IndicateBusy(false);

            }
        }

        public string DestinationFolder
        {
            get => _destinationFolder;
            set 
            {
                if (string.CompareOrdinal(_destinationFolder, value) == 0) return;
                Settings.Default.UserDestinationFolder = _destinationFolder = value;
                Settings.Default.Save();
                OnPropertyChanged();
            }
        }
        private string _destinationFolder = RetrieveDefaultDestinationFolder();
        public static string RetrieveDefaultDestinationFolder()
        {
            var settingsFolder = Settings.Default.UserDestinationFolder;
            if (string.IsNullOrWhiteSpace(settingsFolder))
            {
                settingsFolder = Settings.Default.UserDestinationFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                Settings.Default.Save();
            }
            return settingsFolder;
        }


        public bool AutoPlaySetting
        {
            get => _autoPlaySetting;
            set
            {
                if (_autoPlaySetting == value) return;
                
                Settings.Default.UserAutoPlayOn = _autoPlaySetting = value;
                Settings.Default.Save();

                OnPropertyChanged();
                
                if (_autoPlaySetting)
                {
                    var mix = CurrentItem?.ExtractMixdown();
                    PlayLink(mix, CurrentItem);
                }
                else PlayLink(null);

            }
        }
        private bool _autoPlaySetting = Settings.Default.UserAutoPlayOn;

        public Result CurrentItem
        {
            get => _currentItem;
            set
            {
                if (Equals(value, _currentItem)) return;
                _currentItem = value;
                OnPropertyChanged();
            }
        }
        private Result _currentItem;
    }

}