using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UBTalker.Speech;
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
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Callisto.Controls;
using Windows.System;
using Windows.UI.Core;
using System.Collections.ObjectModel;


// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace UBTalker
{
    // A basic page that provides characteristics common to most applications.
    public sealed partial class MainPage : UBTalker.Common.LayoutAwarePage
    {
        public const string CLIENT_ID = "UBTalker2013";
        public const string CLIENT_SECRET = "NIxPbADlIwuYYPn7xEZ43f64A96tr/h8C/FkGZSiKwY=";
        public const int DEFAULT_CATEGORY = 999999;
        private int category;
        public static string SpeakingLanguage;
        public static bool SingleSwitch;
        public static bool Whisper;
        public static bool RightOnly;
        public static DispatcherTimer Timer;
        public static MainPage Current;
        private ObservableCollection<Button> Col;

        private SpeechSynthesizer speech;

        public MainPage()
        {
            this.InitializeComponent();

            Current = this;

            /* Set up timer */
            if (Timer == null)      // Ensure that there's only one Timer
            {
                Timer = new DispatcherTimer();
                Timer.Interval = TimeSpan.FromSeconds(3);
                Timer.Tick += timer_Ticker;
            }

            Current.Col = new ObservableCollection<Button>();
            Current.Col.CollectionChanged += Current.Col_CollectionChanged;

            /* Set up speech synthesizer */
            speech = new SpeechSynthesizer(CLIENT_ID, CLIENT_SECRET);
            speech.AudioFormat = SpeakStreamFormat.MP3;
            speech.AudioQuality = SpeakStreamQuality.MaxQuality;
            speech.AutoDetectLanguage = false;
            speech.AutomaticTranslation = false;

            // Load settings
            SpeakingLanguage = "en-us";
            SingleSwitch = false;
            var settings = ApplicationData.Current.LocalSettings;
            if (settings != null)
            {
                /* Language */
                if (settings.Values.ContainsKey("lang"))
                    SpeakingLanguage = settings.Values["lang"].ToString();
                
                /* Mode */
                if (settings.Values.ContainsKey("single_switch"))
                {
                    SingleSwitch = (bool)settings.Values["single_switch"];
                    if (settings.Values.ContainsKey("whisper"))
                        Whisper = (bool)settings.Values["whisper"];
                    if (settings.Values.ContainsKey("right_only"))
                        RightOnly = (bool)settings.Values["right_only"];
                }

                /* Interval */
                if (settings.Values.ContainsKey("timer_interval"))
                    Timer.Interval = (TimeSpan)settings.Values["timer_interval"];
            }

            SetSwitch(SingleSwitch);

            /* Set up database */
            var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite"));
            db.CreateTable<Button>();
            Load_Buttons(db);
            var b = db.Table<Button>().FirstOrDefault(x => x.ID == category);

            if (b != null && b.BGImagePath != null)
            {
                try
                {
                    SetBackground(b.BGImagePath);
                }
                catch (Exception ex) { };
            }

            /* Set background image */
            if (b != null && b.BGImagePath != null)
                SetBackground(b.BGImagePath);
        }


        private void timer_Ticker(object sender, object e)
        {
            if (Current.DynamicGrid.Items.Count > 0)
            {
                if (Current.DynamicGrid.SelectedIndex == Current.DynamicGrid.Items.Count - 1)
                    Current.DynamicGrid.SelectedIndex = 0;
                else
                    Current.DynamicGrid.SelectedIndex++;

                if (Whisper || RightOnly)
                {
                    Button selection = Current.DynamicGrid.Items[Current.DynamicGrid.SelectedIndex] as Button;
                    Speak_String(selection.Text, selection.FileName, selection.ID, selection.Language, Whisper, RightOnly);
                }
            }
        }

        /* Sets the background image for the current page */
        public async void SetBackground(string path)
        {
            ImageBrush brush = new ImageBrush();
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(path);
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                BitmapImage image = new BitmapImage();
                image.SetSource(stream);
                brush.ImageSource = image;
                brush.Stretch = Stretch.UniformToFill;
                DynamicGrid.Background = brush;
            }
            catch (Exception ex) { }
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

        /* Creates a filename for the given file */
        public static string FindFilename(int index, string text, string ext)
        {
            return index + "_" + StringExt.Truncate(text, 20) + ext;
        }


        /* Plays the given file. If the file does not exist, creates it */
        private async void Speak_String(string text, string filename, int id, string lang, bool whisper, bool right_only)
        {
            WaitProgressBar.Visibility = Visibility.Visible;

            /* Set up Media Element */
            if (whisper)
                SpeechMediaElement.Volume = 0.2;
            else
                SpeechMediaElement.Volume = 1;
            if (right_only)
                SpeechMediaElement.Balance = 1;
            else
                SpeechMediaElement.Balance = 0;

            /* Try to play the file */
            try
            {
                System.Diagnostics.Debug.WriteLine(filename);
                var myAudio = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                System.Diagnostics.Debug.WriteLine("Playing " + text + " from memory");
                var stream = await myAudio.OpenAsync(FileAccessMode.Read);
                SpeechMediaElement.SetSource(stream, speech.MimeContentType);
            }
            /* Create the file */
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Issue playing the file");
                if (ex is ArgumentNullException || ex is FileNotFoundException)
                {
                    WaitProgressBar.Visibility = Visibility.Collapsed;
                    Store_String(text, id, lang);
                    WaitProgressBar.Visibility = Visibility.Visible;
                }
                else
                    throw;
            }
            finally
            {
                WaitProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        /* Gets the audio file and stores it */
        private async void Store_String(string text, int index, string lang)
        {
            WaitProgressBar.Visibility = Visibility.Visible;

            // Gets the audio stream.
            var stream = await speech.GetSpeakStreamAsync(text, lang);

            // Reproduces the audio stream using a MediaElement.
            SpeechMediaElement.SetSource(stream, speech.MimeContentType);

            WaitProgressBar.Visibility = Visibility.Collapsed;

            /* Get audio stream of text */
            IInputStream inputStream = stream.GetInputStreamAt(0);
            DataReader dataReader = new DataReader(inputStream);
            await dataReader.LoadAsync((uint)stream.Size);
            byte[] buffer = new byte[(int)stream.Size];
            dataReader.ReadBytes(buffer);

            /* Get a filename */
            String fileName = FindFilename(index, text, ".wav");

            System.Diagnostics.Debug.WriteLine("Storing " + text + " to file");
            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(
                            fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            /* Save stream as file */
            System.Diagnostics.Debug.WriteLine(file.Path);
            await Windows.Storage.FileIO.WriteBytesAsync(file, buffer);

            /* Add filename to button's attributes */
            using (var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite")))
            {
                var b = db.Table<Button>().FirstOrDefault(x => x.ID == index);
                b.FileName = fileName;
                db.Update(b);
                Load_Buttons(db);
            }

        }


        private void ItemClick(CoreWindow sender, KeyEventArgs args)
        {
            if (this.Frame.CurrentSourcePageType.Equals(typeof(MainPage))) {
                if (args.VirtualKey == VirtualKey.A)
                {
                    try
                    {
                        Button _Item = (Button)Current.DynamicGrid.Items[Current.DynamicGrid.SelectedIndex];
                        if (_Item.isFolder)
                            Frame.Navigate(typeof(MainPage), _Item.ID);
                        else
                            Speak_String(_Item.Text, _Item.FileName, _Item.ID, _Item.Language, false, false);
                    }
                    catch (Exception ex) { }
                }
            }
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
            Button _Item = (Button)e.ClickedItem;
            if (_Item.isFolder)
                Frame.Navigate(typeof(MainPage), _Item.ID);
            else
                Speak_String(_Item.Text, _Item.FileName, _Item.ID, _Item.Language, false, false);
        }

        /* Switches to the new button screen */
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // If is a button
            Frame.Navigate(typeof(NewButtonPage), category);
            //else
            //Frame.Navigate(typeof(MainPage), b.Category);
        }

        /* Switches to the new category screen */
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewCategoryPage), category);
        }

        private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            this.BottomAppBar.IsOpen = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteItem((Button)DynamicGrid.SelectedItem);
        }

        private async void DeleteItem(Button button)
        {
            StorageFile file;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync((button).ImagePath);
                await file.DeleteAsync();
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException ||
                    ex is ArgumentNullException)
                { }
                else
                {
                    throw;
                }
            }
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync((button).FileName);
                await file.DeleteAsync();
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException ||
                    ex is ArgumentNullException)
                { }
                else
                {
                    throw;
                }
            }

            using (var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite")))
            {
                foreach (Button b in db.Table<Button>().Where(x => x.Category == button.ID).ToList())
                {
                    DeleteItem(b);
                }
                db.Delete(DynamicGrid.SelectedItem);
                Load_Buttons(db);
                this.BottomAppBar.IsOpen = false;
            }
        }

        private void Item_Deselected(object sender, RoutedEventArgs e)
        {
            if (DynamicGrid.SelectedItem == null)
            {
                this.BottomAppBar.IsOpen = false;
            }
        }

        public void SetSwitch(bool Switch)
        {
            SingleSwitch = Switch;
            if (SingleSwitch)
            {
                if (!Timer.IsEnabled)
                {
                    Timer.Start();
                    if (Current.DynamicGrid.Items.Count > 0)
                        Current.DynamicGrid.SelectedIndex = 0;
                    Window.Current.CoreWindow.KeyDown += ItemClick;
                }
            }
            else {
                if (Timer.IsEnabled)
                {
                    Timer.Stop();
                    Current.DynamicGrid.SelectedItem = null;
                }
                Window.Current.CoreWindow.KeyDown -= ItemClick;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Current = this;

            // Set the input focus to ensure that keyboard events are raised.
            this.Loaded += delegate { this.Focus(FocusState.Programmatic); };

            // Set the category
            if (e.Parameter != null)
                category = (int)e.Parameter;
            else
                category = DEFAULT_CATEGORY;

            using (var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite")))
            {
                ObservableCollection<Button> temp = new ObservableCollection<Button>(db.Table<Button>().Where(x => x.Category == category).OrderBy(x => x.Order));
                
                if (this.DataContext != temp)
                {
                    Load_Buttons(db);
                    var b = db.Table<Button>().FirstOrDefault(x => x.ID == category);

                    if (b != null && b.BGImagePath != null)
                    {
                        try
                        {
                            SetBackground(b.BGImagePath);
                        }
                        catch (Exception ex) { };
                    }

                    /* Set background image */
                    if (b != null && b.BGImagePath != null)
                        SetBackground(b.BGImagePath);
                }
            }
        }

        private void AppBar_Loaded(object sender, RoutedEventArgs e)
        {
            Button selection = (Button)DynamicGrid.SelectedItem;
            if (selection != null)
            {
                DeleteButton.Visibility = Visibility.Visible;

                if (!selection.isFolder)
                    EditButton.Visibility = Visibility.Visible;
            }
        }

        private void AppBar_Unloaded(object sender, RoutedEventArgs e)
        {
            DeleteButton.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EditButtonPage), ((Button)DynamicGrid.SelectedItem).ID);
        }

        private void Load_Buttons(SQLiteConnection db)
        {
            Current.Col.Clear();
            var list = db.Table<Button>().Where(x => x.Category == category).OrderBy(x => x.Order).ToList();
            foreach (var i in list)
            {
                Current.Col.Add(i);
            }
            this.DataContext = Current.Col;
        }

        private void Col_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Changed------");
            var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite"));
            ObservableCollection<Button> list = Current.DynamicGrid.DataContext as ObservableCollection<Button>;
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Order = i;
            }
            db.UpdateAll(list);
        }
    }

    /* XAML helper class to convert a hexadecimal color value to a color */
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, String language)
        {
            if ((Boolean)value)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, String language)
        {
            throw new NotImplementedException();
        }
    }

    /* XAML helper class to convert a hexadecimal color value to a color */
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, String language)
        {
            return ColorHelper.GetColorFromHexa((String)value);
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

    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    public class StringToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, String language)
        {
            BitmapImage image = new BitmapImage();
            GetImage((string)value, image);
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, String language)
        {
            throw new NotImplementedException();
        }

        public async void GetImage(string path, BitmapImage image)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(path);
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                image.SetSource(stream);
            }
            catch (FileNotFoundException ex) { }
        }
    }
}