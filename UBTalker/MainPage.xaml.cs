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
        public const int DEFAULT_CATEGORY = 1;
        private int category;
        public static string SpeakingLanguage;
        public static bool SingleSwitch;
        public static int Whisper;
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
            category = DEFAULT_CATEGORY;

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
                        Whisper = (int)settings.Values["whisper"];
                }

                /* Interval */
                if (settings.Values.ContainsKey("timer_interval"))
                    Timer.Interval = (TimeSpan)settings.Values["timer_interval"];
            }

            SetSwitch(SingleSwitch);

            /* Set up database */
            var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite"));
            db.CreateTable<Button>();
            var b = db.Table<Button>().FirstOrDefault(x => x.ID == category);
            if (b == null && category == DEFAULT_CATEGORY)
            {
                /* Create default category */
                db.Insert(new Button
                {
                    Name = "Default",
                    Text = "Category Default",
                    Description = "Starting Category",
                    ImagePath = "",
                    Order = 0,
                    ColorHex = Colors.Black.ToString(),
                    Category = 999999,
                    isFolder = true,
                    BGImagePath = "",
                    Language = MainPage.SpeakingLanguage
                });
            }

            /* Set background image */
            else if (b != null && b.BGImagePath != null)
                SetBackground(b.BGImagePath);

            Load_Buttons(db, false);
        }

        /*
         * Used for single switch mode.
         * Increments the selected item by one index.
         * If Whisper mode is on, plays the text associated with that item.
         * 
         * If the bottom AppBar is open, does nothing to avoid getting in the way of options.
         */
        private void timer_Ticker(object sender, object e)
        {
            if (Current.DynamicGrid.Items.Count > 0 && Current.BottomAppBar.IsOpen == false)
            {
                if (Current.DynamicGrid.SelectedIndex == Current.DynamicGrid.Items.Count - 1)
                    Current.DynamicGrid.SelectedIndex = 0;
                else
                    Current.DynamicGrid.SelectedIndex++;

                if (Whisper > 0)
                {
                    Button selection = Current.DynamicGrid.Items[Current.DynamicGrid.SelectedIndex] as Button;
                    Speak_String(selection.Text, selection.FileName, selection.ID, selection.Language, true);
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
            catch (Exception) { }
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
                MessageDialog dialog = new MessageDialog("You must obtain a Client ID and Secret in order to use this application. Please visit Azure DataMarket at https://datamarket.azure.com/developer/applications to get one.\r\nThen, go to https://datamarket.azure.com/dataset/1899a118-d202-492c-aa16-ba21c33c06cb and subscribe the Microsoft Translator Service.\n", "UB Talker");
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
        private async void Speak_String(string text, string filename, int id, string lang, bool speak_whisper)
        {
            WaitProgressBar.Visibility = Visibility.Visible;

            /* Set up Media Element */
            if (speak_whisper)
            {
                switch (Whisper)
                {
                    /* Whisper */
                    case 1:
                        WhisperMediaElement.Volume = 0.15;
                        WhisperMediaElement.Balance = 0;
                        break;

                    /* Right-Only */
                    case 2:
                        WhisperMediaElement.Volume = 1;
                        WhisperMediaElement.Balance = 1;
                        break;

                    /* Both */
                    case 3:
                        WhisperMediaElement.Volume = 0.15;
                        WhisperMediaElement.Balance = 1;
                        break;

                    /* Silent */
                    default:
                        speak_whisper = false;      // Should not occur
                        break;
                }
            }

            /* Try to play the file */
            try
            {
                System.Diagnostics.Debug.WriteLine(filename);
                var myAudio = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                System.Diagnostics.Debug.WriteLine("Playing " + text + " from memory");
                var stream = await myAudio.OpenAsync(FileAccessMode.Read);

                if (speak_whisper)
                    WhisperMediaElement.SetSource(stream, speech.MimeContentType);
                else
                    SpeechMediaElement.SetSource(stream, speech.MimeContentType);
            }
            /* Create the file */
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Issue playing the file");
                if (ex is ArgumentNullException || ex is FileNotFoundException)
                {
                    WaitProgressBar.Visibility = Visibility.Collapsed;
                    Store_String(text, id, lang, speak_whisper);
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

        /* Gets the audio stream and stores it as a file */
        private async void Store_String(string text, int index, string lang, bool speak_whisper)
        {
            WaitProgressBar.Visibility = Visibility.Visible;

            // Gets the audio stream.
            var stream = await speech.GetSpeakStreamAsync(text, lang);

            // Reproduces the audio stream using a MediaElement.
            if(speak_whisper)
                WhisperMediaElement.SetSource(stream, speech.MimeContentType);
            else
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
                var b = Col.FirstOrDefault(x => x.ID == index);
                if (b != null)
                {
                    b.FileName = fileName;
                    db.Update(b);
                }
            }

        }

        /*
         * Helper function for when items selected with single-switch.
         */
        private void SingleSwitchPress(CoreWindow sender, KeyEventArgs args)
        {
            if (this.Frame.CurrentSourcePageType.Equals(typeof(MainPage))) {
                if (args.VirtualKey == VirtualKey.A)
                {
                    try
                    {
                        HandleClick((Button)Current.DynamicGrid.Items[Current.DynamicGrid.SelectedIndex]);
                    }
                    catch (Exception) { }
                }
            }
        }

        /*
         * Helper function for when items pressed/clicked with touch/mouse.
         */
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            HandleClick((Button)e.ClickedItem);
        }

        /*
        * Handles clicks on Grid items.
        * If a category, navigate to a new page.
        * If a button, try to speak associated text.
        */
        void HandleClick(Button item)
        {
            if (item.isFolder)
                Frame.Navigate(typeof(MainPage), item.ID);
            else
                Speak_String(item.Text, item.FileName, item.ID, item.Language, false);
        }

        /* Switches to the new button screen */
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewButtonPage), category);
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

        /*
         * Deletes an item by removing it from the database and Grid.
         * Also tries to delete associated image and sound files.
         * If it is a category, recursively deletes all times contained in category
         */
        private async void DeleteItem(Button button)
        {
            StorageFile file;

            /* Delete image file */
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(button.ImagePath);
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

            /* Delete sound file */
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(button.FileName);
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

            /* Category */
            if (button.isFolder)
            {
                /* Delete background image file */
                try
                {
                    file = await ApplicationData.Current.LocalFolder.GetFileAsync(button.BGImagePath);
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
            }

            /* Delete item and sub items from database */
            using (var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite")))
            {
                /* Recursive for sub items */
                foreach (Button b in db.Table<Button>().Where(x => x.Category == button.ID).ToList())
                {
                    DeleteItem(b);
                }
                db.Delete(DynamicGrid.SelectedItem);
                Col.Remove(DynamicGrid.SelectedItem as Button);
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

        /*
         * Turns single-switch functionality on/off.
         * Sets up the associated Timer and event handler
         */
        public void SetSwitch(bool Switch)
        {
            SingleSwitch = Switch;

            /* Turn on */
            if (SingleSwitch)
            {
                if (!Timer.IsEnabled)
                {
                    Timer.Start();
                    if (Current.DynamicGrid.Items.Count > 0)
                        Current.DynamicGrid.SelectedIndex = 0;
                    Window.Current.CoreWindow.KeyDown += SingleSwitchPress;
                }
            }

            /* Turn off */
            else {
                if (Timer.IsEnabled)
                {
                    Timer.Stop();
                    Current.DynamicGrid.SelectedItem = null;
                }
                Window.Current.CoreWindow.KeyDown -= SingleSwitchPress;
            }
        }

        /* 
         * Called when a page is first created and when it is navigated to.
         * Sets the current page and category.
         * Loads the buttons for the category into the Grid and sets the background image.
         */
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
                    /* Decide if the new button page should be shown if the category is empty*/
                    bool newPage = (e.SourcePageType == typeof(MainPage)) && (e.NavigationMode == NavigationMode.New);

                    Load_Buttons(db, newPage);
                    var b = db.Table<Button>().FirstOrDefault(x => x.ID == category);

                    if (b != null && b.BGImagePath != null)
                    {
                        try
                        {
                            SetBackground(b.BGImagePath);
                        }
                        catch (Exception) { };
                    }

                    /* Set background image */
                    if (b != null && b.BGImagePath != null)
                        SetBackground(b.BGImagePath);
                }
            }
        }

        /* Shows or hides the Delete button from the bottom AppBar depending on if an item is selected */
        private void AppBar_Loaded(object sender, RoutedEventArgs e)
        {
            //EditButton.Visibility = Visibility.Visible;

            Button selection = (Button)DynamicGrid.SelectedItem;
            if (selection != null)
            {
                ClearButton.Visibility = Visibility.Visible;
                DeleteButton.Visibility = Visibility.Visible;
            }
        }

        /* Resets the visibility of items in the bottom AppBar when it is closed */
        private void AppBar_Unloaded(object sender, RoutedEventArgs e)
        {
            ClearButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
            //EditButton.Visibility = Visibility.Collapsed;
        }

        /*
         * Handles EditButton clicks from the bottom AppBar.
         * If a Grid item is selected, navigates to the edit page for that item.
         * If no item is selected, navigates to the edit page for the current category.
         */
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Button selection = (Button)DynamicGrid.SelectedItem;
            if (selection != null)
            {
                if (selection.isFolder)
                    Frame.Navigate(typeof(EditCategoryPage), ((Button)DynamicGrid.SelectedItem).ID);
                else
                    Frame.Navigate(typeof(EditButtonPage), ((Button)DynamicGrid.SelectedItem).ID);
            }
            else
                Frame.Navigate(typeof(EditCategoryPage), category);
        }

        /*
         * Loads Buttons from the given database, adds them to the Button collection and sets them to the Grid.
         * If there are no buttons to load, automatically opens the New Button creation page if the showNewButton option is true.
         */
        private async void Load_Buttons(SQLiteConnection db, bool showNewButton)
        {
            Current.Col.Clear();
            var list = db.Table<Button>().Where(x => x.Category == category).OrderBy(x => x.Order).ToList();

            if (list.Count == 0 && showNewButton)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Frame.Navigate(typeof(NewButtonPage), Current.category));
            }
            else
            {
                foreach (var i in list)
                {
                    Current.Col.Add(i);
                }
                this.DataContext = Current.Col;
            }
        }

        /*
         * Called when the grid of Buttons is changed, specifically the order.
         * Loads the order of the Buttons and updates them in the database.
         */
        private void Col_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
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

        private void DeselectButton_Click(object sender, RoutedEventArgs e)
        {
            Current.DynamicGrid.SelectedItem = null;
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
        /* Shortens the given string to the maximum string length */
        public static string Truncate(this string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    /* Returns the image associated with the file name for use in binding */
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
            catch (FileNotFoundException) { }
        }
    }
}