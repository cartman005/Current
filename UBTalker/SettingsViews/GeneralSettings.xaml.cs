using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace UBTalker.SettingsViews
{

    public sealed partial class GeneralSettings : UserControl
    {
        MainPage rootPage = MainPage.Current;

        public GeneralSettings()
        {
            this.InitializeComponent();
            switch (MainPage.SpeakingLanguage)
            {
                case "en-us":
                    LanguageSelection.SelectedIndex = 0;
                    break;
                case "es":
                    LanguageSelection.SelectedIndex = 1;
                    break;
                case "gr":
                    LanguageSelection.SelectedIndex = 2;
                    break;
                case "fr":
                    LanguageSelection.SelectedIndex = 3;
                    break;
            }

            if (MainPage.Timer.Interval == TimeSpan.FromSeconds(1))
                IntervalSelection.SelectedIndex = 0;
            else if (MainPage.Timer.Interval == TimeSpan.FromSeconds(3))
                IntervalSelection.SelectedIndex = 1;
            else if (MainPage.Timer.Interval == TimeSpan.FromSeconds(5))
                IntervalSelection.SelectedIndex = 2;
            else
                IntervalSelection.SelectedIndex = 3;

            WhisperSelection.SelectedIndex = MainPage.Whisper;

            SingleSwitchToggle.IsOn = MainPage.SingleSwitch;
            if (MainPage.SingleSwitch)
            {
                IntervalLabel.Visibility = Visibility.Visible;
                IntervalSelection.Visibility = Visibility.Visible;
                WhisperLabel.Visibility = Visibility.Visible;
                WhisperSelection.Visibility = Visibility.Visible;
            }
        }

        private void LanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                /* Update language */
                switch (LanguageSelection.SelectedIndex)
                {
                    case 0:
                        MainPage.SpeakingLanguage = "en-us";
                        break;
                    case 1:
                        MainPage.SpeakingLanguage = "es";
                        break;
                    case 2:
                        MainPage.SpeakingLanguage = "gr";
                        break;
                    case 3:
                        MainPage.SpeakingLanguage = "fr";
                        break;
                }

                /* Store setting */
                var settings = ApplicationData.Current.LocalSettings;
                if (settings != null)
                {
                    if (settings.Values.ContainsKey("lang"))
                        settings.Values["lang"] = MainPage.SpeakingLanguage;
                    else
                        settings.Values.Add("lang", MainPage.SpeakingLanguage);
                }

            }
            catch (Exception) { }

        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            MainPage.Current.SetSwitch(SingleSwitchToggle.IsOn);
            if (MainPage.SingleSwitch)
            {
                IntervalLabel.Visibility = Visibility.Visible;
                IntervalSelection.Visibility = Visibility.Visible;
                WhisperLabel.Visibility = Visibility.Visible;
                WhisperSelection.Visibility = Visibility.Visible;
            }
            else
            {
                IntervalLabel.Visibility = Visibility.Collapsed;
                IntervalSelection.Visibility = Visibility.Collapsed;
                WhisperLabel.Visibility = Visibility.Collapsed;
                WhisperSelection.Visibility = Visibility.Collapsed;
            }

            var settings = ApplicationData.Current.LocalSettings;
            if (settings != null)
            {
                if (settings.Values.ContainsKey("single_switch"))
                    settings.Values["single_switch"] = MainPage.SingleSwitch;
                else
                    settings.Values.Add("single_switch", MainPage.SingleSwitch);
            }
        }

        private void IntervalSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                /* Update Interval */
                switch (IntervalSelection.SelectedIndex)
                {
                    case 0:
                        MainPage.Timer.Interval = TimeSpan.FromSeconds(1);
                        break;
                    case 1:
                        MainPage.Timer.Interval = TimeSpan.FromSeconds(3);
                        break;
                    case 2:
                        MainPage.Timer.Interval = TimeSpan.FromSeconds(5);
                        break;
                    case 3:
                        MainPage.Timer.Interval = TimeSpan.FromSeconds(10);
                        break;
                    default:
                        break;
                }

                /* Store setting */
                var settings = ApplicationData.Current.LocalSettings;
                if (settings != null)
                {
                    if (settings.Values.ContainsKey("timer_interval"))
                        settings.Values["timer_interval"] = MainPage.Timer.Interval;
                    else
                        settings.Values.Add("timer_interval", MainPage.Timer.Interval);
                }
            }
            catch (Exception) { }
        }

        private void WhisperSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                MainPage.Whisper = WhisperSelection.SelectedIndex;

                /* Store setting */
                var settings = ApplicationData.Current.LocalSettings;
                if (settings != null)
                {
                    if (settings.Values.ContainsKey("whisper"))
                        settings.Values["whisper"] = MainPage.Whisper;
                    else
                        settings.Values.Add("whisper", MainPage.Whisper);
                }
            }
            catch (Exception) { }
        }
    }
}
