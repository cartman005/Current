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

    public sealed partial class VoiceSettings : UserControl
    {
        public VoiceSettings()
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

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
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

                ApplicationData.Current.LocalSettings.Values["lang"] = MainPage.SpeakingLanguage;
            }
            catch (Exception ex)
            { }

        }
    }
}
