using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using FireRadioPlayer.ViewModel;
using Coding4Fun.Phone.Controls;
using ScottIsAFool.WindowsPhone.IsolatedStorage;
using System.IO.IsolatedStorage;
using System.Windows.Resources;

namespace FireRadioPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Shows the message.
        /// </summary>
        /// <param name="Message">The message.</param>
        /// <param name="Title">The title.</param>
        public static void ShowMessage(string Message, string Title)
        {
            ShowMessage(Message, Title, null);
        }

        /// <summary>
        /// Shows the message.
        /// </summary>
        /// <param name="Message">The message.</param>
        /// <param name="Title">The title.</param>
        /// <param name="TapEvent">The tap event.</param>
        public static void ShowMessage(string Message, string Title, Action TapEvent)
        {
            ToastPrompt _prompt = new ToastPrompt
            {
                Title = Title,
                Message = Message
            };
            if (TapEvent != null)
                _prompt.Tap += (s, e) => { TapEvent(); };
            _prompt.Show();
        }

        // Easy access to the root frame
        public PhoneApplicationFrame RootFrame
        {
            get;
            private set;
        }

        // Constructor
        public App()
        {
            CheckAndCopyLogo();
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            ThemeManager.ToDarkTheme();

            // Required to make the marketplace put the right capabilities in!!
            Microsoft.Xna.Framework.Media.MediaLibrary library = new Microsoft.Xna.Framework.Media.MediaLibrary();
            library.Dispose();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disable user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        private void CheckAndCopyLogo()
        {
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string file = "Images/FireRadio.jpg";
                string isoFile = "Shared/Media/FireRadio.jpg";
                if (storage.FileExists(isoFile))
                    return;
                StreamResourceInfo sri = Application.GetResourceStream(new Uri(file, UriKind.Relative));

                if (sri != null)
                {
                    using (IsolatedStorageFileStream stream = storage.CreateFile(isoFile))
                    {
                        int chunkSize = 4096;
                        byte[] bytes = new byte[chunkSize];
                        int byteCount;

                        while ((byteCount = sri.Stream.Read(bytes, 0, chunkSize)) > 0)
                        {
                            stream.Write(bytes, 0, byteCount);
                        }
                    }
                }
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            ((ViewModelLocator)App.Current.Resources["Locator"]).Settings = ISettings.GetKeyValue<SettingsViewModel>("Settings");
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (!e.IsApplicationInstancePreserved)
            {
                ((ViewModelLocator)App.Current.Resources["Locator"]).Settings = ISettings.GetKeyValue<SettingsViewModel>("Settings");
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            ISettings.Set("Settings", ((ViewModelLocator)App.Current.Resources["Locator"]).Settings);
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            ISettings.Set("Settings", ((ViewModelLocator)App.Current.Resources["Locator"]).Settings);
            ViewModelLocator.Cleanup();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}
