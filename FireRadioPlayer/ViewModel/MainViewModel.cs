using GalaSoft.MvvmLight;
using System;
using System.Net;
using Newtonsoft.Json;
using FireRadioPlayer.Model;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Command;
using Microsoft.Phone.Net.NetworkInformation;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.BackgroundAudio;
using System.Windows.Threading;
using Microsoft.Phone.Tasks;
using AudioSharedLibrary;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;

namespace FireRadioPlayer.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private static readonly string firePlayerUrl = "http://www.fireradio.co.uk/now.json?nocache={0}";
        private static readonly string FireRadioReminderName = "ScottIsAFool.FireRadio.Reminder";

        private DispatcherTimer playTimer;
        private DispatcherTimer refreshTimer;

        private bool _gettingNowPlaying;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                PlayButtonIconUri = new Uri("/Icons/appbar.transport.play.rest.png", UriKind.Relative);
                PlayButtonText = "play";
            }
            else
            {
                WireCommands();
                playTimer = new DispatcherTimer();
                playTimer.Interval = new TimeSpan(1000);
                playTimer.Tick += new EventHandler(playTimer_Tick);
                playTimer.Start();

                refreshTimer = new DispatcherTimer();
                refreshTimer.Tick += new EventHandler(refreshTimer_Tick);
                refreshTimer.Interval = new TimeSpan(0, 2, 0);
                refreshTimer.Start();

                BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(Instance_PlayStateChanged);
                SetPlayButton();
                IsIndeterminate = true;
            }
        }

        void refreshTimer_Tick(object sender, EventArgs e)
        {
            var settings = ((ViewModelLocator)App.Current.Resources["Locator"]).Settings;
            if (settings.AutoRefresh)
            {
                GetNowPlayingInfo();
            }            
        }

        void Instance_PlayStateChanged(object sender, EventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                PlayButtonText = "pause";
                PlayButtonIconUri = new Uri("/Icons/appbar.transport.pause.rest.png", UriKind.Relative);
            }
            else
            {
                PlayButtonIconUri = new Uri("/Icons/appbar.transport.play.rest.png", UriKind.Relative);
                PlayButtonText = "play";                
            }
        }

        void playTimer_Tick(object sender, EventArgs e)
        {
            string errorString = BackgroundErrorNotifier.GetError();
            if (errorString != null)
            {
                playTimer.Stop();
                App.ShowMessage(errorString, "Error");                
            }
            var track = BackgroundAudioPlayer.Instance.Track;
            if (track != null)
            {
                if (track.Tag == null)
                {
                    IsProgressVisible = true;
                    ProgressText = "Finding stream...";
                }
                else
                {
                    switch (track.Tag)
                    {
                        case "Buffering":
                            IsProgressVisible = IsIndeterminate = true;
                            ProgressText = "Buffering...";
                            break;
                        case "Stopped":
                            IsProgressVisible = false;
                            ProgressText = "";
                            break;
                        case "Playing":
                            if (!_gettingNowPlaying)
                            {
                                IsIndeterminate = _gettingNowPlaying;
                                ProgressText = track.Tag;
                            }
                            break;
                        default:                            
                            IsProgressVisible = IsIndeterminate = true;
                            ProgressText = track.Tag;
                            break;
                    }
                }
            }
            //SetPlayButton();
        }

        private void SetPlayButton()
        {
            if (BackgroundAudioPlayer.Instance.PlayerState != PlayState.Playing)
            {
                PlayButtonIconUri = new Uri("/Icons/appbar.transport.play.rest.png", UriKind.Relative);
                PlayButtonText = "play";
            }
            else
            {
                PlayButtonText = "pause";
                PlayButtonIconUri = new Uri("/Icons/appbar.transport.pause.rest.png", UriKind.Relative);
            }
        }

        private void WireCommands()
        {
            MainPageLoaded = new RelayCommand(() =>
            {
                GetNowPlayingInfo();
                if (((ViewModelLocator)App.Current.Resources["Locator"]).Settings.AutoPlay)
                {
                    if (BackgroundAudioPlayer.Instance.PlayerState != PlayState.Playing)
                    {
                        if (IsNetworkAvailable)
                        {
                            BackgroundAudioPlayer.Instance.Play();
                        }
                    }
                    SetPlayButton();
                }                
            });

            PlayStopCommand = new RelayCommand(() =>
            {
                if (!playTimer.IsEnabled)
                    playTimer.Start();
                if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
                {
                    BackgroundAudioPlayer.Instance.Stop();
                    IsProgressVisible = true;
                    ProgressText = "Stopping...";
                }
                else
                {
                    if (IsNetworkAvailable)
                    {
                        IsProgressVisible = true;
                        ProgressText = "Finding stream...";
                        BackgroundAudioPlayer.Instance.Play();
                    }
                }
                SetPlayButton();
            });

            SetReminderCommand = new RelayCommand<string>(time =>
            {
                string showStartTime = time.Substring(0, time.Length - time.IndexOf(" - ") - 3);
                if (showStartTime.ToLower().Equals("noon"))
                {
                    showStartTime = "12:00PM";
                }
                if (showStartTime.ToLower().Equals("midnight"))
                {
                    showStartTime = "12:00AM";
                }
                DateTime startDate = DateTime.Parse(showStartTime);
                if (startDate < DateTime.Now) // ie, the show is actually on tomorrow
                {
                    startDate = startDate.AddDays(1);
                }
                ScheduledAction _existingReminder = ScheduledActionService.Find(FireRadioReminderName);
                
                if (_existingReminder != null)
                    ScheduledActionService.Remove(FireRadioReminderName);

                Reminder _reminder = new Reminder(FireRadioReminderName)
                {
#if DEBUG
                    BeginTime = DateTime.Now.AddSeconds(10),
#else
                    BeginTime = ConvertToTimeZone(startDate),
#endif
                    Title = "Fire Radio Player Reminder",
                    Content = string.Format("{0} is just about to start on Fire Radio.", OnAirNext.Name),
                    NavigationUri = new Uri("/Views/MainPage.xaml", UriKind.Relative)
                };
                ScheduledActionService.Add(_reminder);
                App.ShowMessage("Reminder has been set.", "Success");
            });

            SearchMarketPlaceCommand = new RelayCommand<Track>(track =>
            {                
                new MarketplaceSearchTask()
                {
                    ContentType = MarketplaceContentType.Music,
                    SearchTerms = track.Title
                }.Show();
                
            });

            OpenPageCommand = new RelayCommand<string>(link =>
            {
                new WebBrowserTask()
                {
                    Uri = new Uri(link, UriKind.Absolute)
                }.Show();
            });

            OpenEmailCommand = new RelayCommand<string>(email =>
            {
                new EmailComposeTask()
                {
                    To = email,
                }.Show();
            });

            RefreshCommand = new RelayCommand(GetNowPlayingInfo);
            NavigateToCommand = new RelayCommand<string>(NavigateTo);
        }

        private void GetNowPlayingInfo()
        {
            if (IsNetworkAvailable)
            {
                string fire = string.Format(firePlayerUrl, GetTime());
                IsProgressVisible = IsIndeterminate = _gettingNowPlaying = true;
                ProgressText = "Getting now playing details...";
                WebClient client = new WebClient();
                client.DownloadStringCompleted += (s, e) =>
                {
                    if (e.Error == null)
                    {
                        if (e.Result != null)
                        {
                            FirePlayer playerDetails = JsonConvert.DeserializeObject<FirePlayer>(e.Result);
                            playerDetails.Last10Tracks.ForEach(item =>
                            {
                                item.Artist = HttpUtility.HtmlDecode(item.Artist);
                                item.Title = HttpUtility.HtmlDecode(item.Title);
                            });
                            NowPlaying = playerDetails.Last10Tracks[0];
                            playerDetails.Last10Tracks.Remove(NowPlaying);
                            OnAirNow = playerDetails.OnAirNow;
                            OnAirNext = playerDetails.OnAirNext;
                            RecentlyPlayed = new ObservableCollection<Track>(playerDetails.Last10Tracks);
                        }
                    }
                    else
                    {
                        App.ShowMessage("There was an error getting the info", "Error");
                    }
                    IsProgressVisible = _gettingNowPlaying = false;
                };
                client.DownloadStringAsync(new Uri(fire, UriKind.Absolute));
            }
        }

        private void NavigateTo(string pageUrl)
        {
            if (!string.IsNullOrEmpty(pageUrl))
            {
                var root = Application.Current.RootVisual as PhoneApplicationFrame;
                root.Navigate(new Uri(pageUrl, UriKind.Relative));
            }
        }

        private DateTime ConvertToTimeZone(DateTime dateTime)
        {
            TimeZoneInfo info = TimeZoneInfo.Local;
            DateTime result = dateTime.AddHours(info.BaseUtcOffset.Hours);
            return result;
        }

        public bool IsNetworkAvailable
        {
            get
            {                
                var result = NetworkInterface.GetIsNetworkAvailable();
                if (!result)
                {
                    App.ShowMessage("No network connections available", "Warning");
                }
                else
                {
                    if (((ViewModelLocator)App.Current.Resources["Locator"]).Settings.UseOnlyWifi)
                    {
                        if (NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet ||
                            NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                        {
                            App.ShowMessage("Sorry, you need to turn your wifi on", "Warning");
                            playTimer.Stop();
                        }
                    }
                }
                if (!playTimer.IsEnabled)
                    playTimer.Start();
                return result;
            }
        }

        public Track NowPlaying { get; set; }
        public ObservableCollection<Track> RecentlyPlayed { get; set; }
        public OnAirNow OnAirNow { get; set; }
        public OnAirNext OnAirNext { get; set; }
        public bool IsProgressVisible { get; set; }
        public bool IsIndeterminate { get; set; }
        public string ProgressText { get; set; }
        public Uri PlayButtonIconUri { get; set; }
        public string PlayButtonText { get; set; }

        public RelayCommand MainPageLoaded { get; set; }
        public RelayCommand PlayStopCommand { get; set; }
        public RelayCommand RefreshCommand { get; set; }
        public RelayCommand<string> NavigateToCommand { get; set; }
        public RelayCommand<string> SetReminderCommand { get; set; }
        public RelayCommand<Track> SearchMarketPlaceCommand { get; set; }
        public RelayCommand<string> OpenPageCommand { get; set; }
        public RelayCommand<string> OpenEmailCommand { get; set; }

        private Int64 GetTime()
        {
            Int64 retval = 0;
            DateTime st = new DateTime(1970, 1, 1);
            TimeSpan t = (DateTime.Now.ToUniversalTime() - st);
            retval = (Int64)(t.TotalMilliseconds + 0.5);
            return retval;
        }
    }
}