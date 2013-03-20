using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TranslatorService.Speech;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Reflection;
using SQLite;


// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace TranslatorService.Example
{
    // A basic page that provides characteristics common to most applications.
    public sealed partial class MainPage : TranslatorService.Example.Common.LayoutAwarePage
    {
        private const string CLIENT_ID = "UBTalker2013";
        private const string CLIENT_SECRET = "NIxPbADlIwuYYPn7xEZ43f64A96tr/h8C/FkGZSiKwY=";

        private SpeechSynthesizer speech;

        public MainPage()
        {
            this.InitializeComponent();

            /* Set up speech synthesizer */
            speech = new SpeechSynthesizer(CLIENT_ID, CLIENT_SECRET);
            speech.AudioFormat = SpeakStreamFormat.MP3;
            speech.AudioQuality = SpeakStreamQuality.MaxQuality;
            speech.AutoDetectLanguage = false;
            speech.AutomaticTranslation = false;

            /* Database transactions */
            var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "mydb.sqlite"));
            db.CreateTable<Button>();

            /* Set up combobox */
            // Source: http://social.msdn.microsoft.com/Forums/en-US/winappswithcsharp/thread/1cb9c5b9-3ef6-4c88-b747-ae222c38c922/
            var colorChoices = typeof(Colors).GetTypeInfo().DeclaredProperties;
            foreach (var item in colorChoices)
            {
                ColorChoices.Items.Add(item);
            }
            ColorChoices.DataContext = colorChoices;

            /* Get and insert sample buttons from system colors */
            /*var _Colors = typeof(Colors)
                .GetRuntimeProperties()
                .Select((x, i) => new
                {
                    Color = (Color)x.GetValue(null),
                    Name = x.Name,
                    Index = i,
                    ColSpan = 1,
                    RowSpan = 1
                });

            foreach (var c in _Colors)
            {
                db.Insert(new Button { Text = c.Name, ColSpan = c.ColSpan, RowSpan = c.RowSpan, Order = c.Index, ColorHex = c.Color.ToString() });
            } */
            
            /* Set data context to Button table */
            this.DataContext = db.Table<Button>().ToList();
        }

        private int ColSpan(int i)
        {
            return 1;
        }

        private int RowSpan(int i)
        {
            return 1;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (string.IsNullOrWhiteSpace(CLIENT_ID) || string.IsNullOrWhiteSpace(CLIENT_SECRET))
            {
                MessageDialog dialog = new MessageDialog("You must obtain a Client ID and Secret in order to use this application. Please visit Azure DataMarket at https://datamarket.azure.com/developer/applications to get one.\r\nThen, go to https://datamarket.azure.com/dataset/1899a118-d202-492c-aa16-ba21c33c06cb and subscribe the Microsoft Translator Service.\n", "Translator Service Example");
                await dialog.ShowAsync();
                return;
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private async void Speak_String(string text)
        {
            WaitProgressBar.Visibility = Visibility.Visible;

            // Gets the audio stream.
            var stream = await speech.GetSpeakStreamAsync(text, "en-us");

            // Reproduces the audio stream using a MediaElement.
            SpeechMediaElement.SetSource(stream, speech.MimeContentType);

            WaitProgressBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Invoked when an item within a group is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            dynamic _Item = e.ClickedItem;
            Speak_String(_Item.Text);
        }

        private async void CreateDatabase()
        {
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection("button");
            await conn.CreateTableAsync<Button>();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            /* Speak string */
            if (string.IsNullOrWhiteSpace(SpeechText.Text))
                return;
            Speak_String(SpeechText.Text);

            Color selection;
            /* Get selected color */
            if (ColorChoices.SelectedIndex != -1)
            {
                var pi = ColorChoices.SelectedItem as PropertyInfo;
                selection = (Color)pi.GetValue(null);
            }
            else
                selection = Colors.Black;

            /* Add button to database */
            using (var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "mydb.sqlite")))
            {
                db.Insert(new Button { Text = SpeechText.Text, ColSpan = ColSpan(1), RowSpan = RowSpan(1), Order = 0, ColorHex = selection.ToString() });
                this.DataContext = db.Table<Button>().ToList();
            }

            /* Clear textbox */
            SpeechText.Text = "";
        }

        private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            AppBar.IsOpen = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "mydb.sqlite")))
            {
                db.Delete(DynamicGrid.SelectedItem);
                this.DataContext = db.Table<Button>().ToList();
                AppBar.IsOpen = false;
            }
        }

        private void Item_Deselected(object sender, RoutedEventArgs e)
        {
            if (DynamicGrid.SelectedItem == null)
            {
                AppBar.IsOpen = false;
            }
        }
    }

    /* XAML helper class to convert a hexadecimal color value to a color */
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, String language)
        {
            return ColorHelper.GetColorFromHexa((String) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, String language)
        {
            throw new NotImplementedException();
        }
    }

    /* Get the color value from the hexadecimal string */
    public static class ColorHelper
    {
        public static Color GetColorFromHexa(string hexaColor)
        {
            return Color.FromArgb(
                    Convert.ToByte(hexaColor.Substring(1, 2), 16),
                    Convert.ToByte(hexaColor.Substring(3, 2), 16),
                    Convert.ToByte(hexaColor.Substring(5, 2), 16),
                    Convert.ToByte(hexaColor.Substring(7, 2), 16)
            );
        }
    }
}