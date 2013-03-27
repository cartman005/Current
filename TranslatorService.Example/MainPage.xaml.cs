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
using Windows.UI.ApplicationSettings;
using Windows.ApplicationModel.Resources;
using System.Collections.ObjectModel;
using TCD.Serialization.Xml;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;


// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace TranslatorService.Example
{
    // A basic page that provides characteristics common to most applications.
    public sealed partial class MainPage : TranslatorService.Example.Common.LayoutAwarePage
    {
        private const string DATA_FILE = "ButtonsDef";
        private const string CLIENT_ID = "UBTalker2013";
        private const string CLIENT_SECRET = "NIxPbADlIwuYYPn7xEZ43f64A96tr/h8C/FkGZSiKwY=";

        private SpeechSynthesizer speech;
        private Popup settingsPopup;

        public ObservableCollection<Button> Data { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            SettingsPane.GetForCurrentView().CommandsRequested += OnSettingsPaneRequested;
            Data = new ObservableCollection<Button>();

            /* Set up speech synthesizer */
            speech = new SpeechSynthesizer(CLIENT_ID, CLIENT_SECRET);
            speech.AudioFormat = SpeakStreamFormat.Wave;
            speech.AudioQuality = SpeakStreamQuality.MaxQuality;
            speech.AutoDetectLanguage = false;
            speech.AutomaticTranslation = false;


            /* Set data context to Button table */
            LoadData();
            this.DataContext = Data;
        }

        public async void LoadData()
        {
            var result = await GetData();

            if (result == null)
                return;

            foreach (var item in result)
            {
                Data.Add(item);
            }
        }

        public static async void SaveData(ObservableCollection<Button> data)
        {
            StorageFolder storageFolder = ApplicationData.Current.RoamingFolder;
           
            try
            {
                var file = await storageFolder.CreateFileAsync(DATA_FILE, CreationCollisionOption.ReplaceExisting);
                // serial data object to XML file specified in "DATA_FILE"
                XmlDeSerializer.SerializeToStream(await file.OpenStreamForWriteAsync(), data);
            }
            catch (Exception ex)
            {
                // handle any kind of exceptions
            }
        }

        public static async Task<ObservableCollection<Button>> GetData()
        {
            StorageFolder storageFolder = ApplicationData.Current.RoamingFolder;
            var file = await storageFolder.CreateFileAsync(DATA_FILE, CreationCollisionOption.OpenIfExists);
            try
            {
                // deserialize the collection object from "DATA_FILE" specified
                return XmlDeSerializer.DeserializeFromStream(await file.OpenStreamForReadAsync(), 
                                                     typeof(ObservableCollection<Button>)) 
                                                     as ObservableCollection<Button>;
            }
            catch (Exception ex)
            {
               // handle any kind of exception
            }
 
            return null;
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var b = e.Parameter as Button;

            if (b != null)
            {
                WaitProgressBar.Visibility = Visibility.Visible;
                Data.Add(b);
                Store_String(b.Text, Data.IndexOf(b));
                WaitProgressBar.Visibility = Visibility.Collapsed;
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

        private async void Store_String(string text, int index)
        {
            WaitProgressBar.Visibility = Visibility.Visible;

            // Gets the audio stream.
            var stream = await speech.GetSpeakStreamAsync(text, "en-us");

            // Reproduces the audio stream using a MediaElement.
            SpeechMediaElement.SetSource(stream, speech.MimeContentType);

            WaitProgressBar.Visibility = Visibility.Collapsed;

            IInputStream inputStream = stream.GetInputStreamAt(0);
            DataReader dataReader = new DataReader(inputStream);
            await dataReader.LoadAsync((uint)stream.Size);
            byte[] buffer = new byte[(int)stream.Size];
            dataReader.ReadBytes(buffer);

            String fileName = FindFilename(index, text);

            System.Diagnostics.Debug.WriteLine("Storing " + text + " to file");
            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(
                            fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            System.Diagnostics.Debug.WriteLine(file.Path);
            await Windows.Storage.FileIO.WriteBytesAsync(file, buffer);
            Data[index].FileName = fileName;
            SaveData(Data);
        }

        public static string FindFilename(int index, string text)
        {
            return index + "_" + StringExt.Truncate(text, 20) + ".wav";
        }

        private async void DeleteFile(string fileName)
        {
            try
            {
                StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

                if (file != null)
                {
                    await file.DeleteAsync();
                }
            }
            catch (Exception ex)
            { }
        }

        private async void Speak_String(int index)
        {
            WaitProgressBar.Visibility = Visibility.Visible;

            try
            {
                var myAudio = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(Data[index].FileName);
                System.Diagnostics.Debug.WriteLine("Playing " + Data[index].Text + " from memory");
                MediaElement mediaElement = new MediaElement();

                var stream = await myAudio.OpenAsync(FileAccessMode.Read);
                mediaElement.SetSource(stream, myAudio.ContentType);
                mediaElement.Play();
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is FileNotFoundException)
                {
                    WaitProgressBar.Visibility = Visibility.Collapsed;
                    Store_String(Data[index].Text, index);
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
            Speak_String(Data.IndexOf(_Item));
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewButtonPage));
        }

        private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            this.BottomAppBar.IsOpen = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button) DynamicGrid.SelectedItem;
            Data.Remove(b);
            this.BottomAppBar.IsOpen = false;
            SaveData(Data);
            if (b.FileName != null)
                DeleteFile(b.FileName);
        }

        private void EnlargeButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button) DynamicGrid.SelectedItem;
            int index = Data.IndexOf(b);
            
            if (b.ColSpan == 1 && b.RowSpan == 1)
            {
                b.ColSpan = 2;
                Data[index] = b;
            }
            else if (b.ColSpan == 2 && b.RowSpan == 1)
            {
                b.RowSpan = 2;
                Data[index] = b;
            }
            else if (b.ColSpan == 2 && b.RowSpan == 2)
            {
                /* Maximum size */
            }
            
            this.BottomAppBar.IsOpen = false;
            SaveData(Data);
        }

        private void ShrinkButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)DynamicGrid.SelectedItem;
            int index = Data.IndexOf(b);

            if (b.ColSpan == 1 && b.RowSpan == 1)
            {
                /* Minimum size */
            }
            else if (b.ColSpan == 2 && b.RowSpan == 1)
            {
                b.ColSpan = 1;
                Data[index] = b;
            }
            else if (b.ColSpan == 2 && b.RowSpan == 2)
            {
                b.RowSpan = 1;
                Data[index] = b;
            }

            this.BottomAppBar.IsOpen = false;
            SaveData(Data);
        }

        private void Item_Deselected(object sender, RoutedEventArgs e)
        {
            if (DynamicGrid.SelectedItem == null)
            {
                this.BottomAppBar.IsOpen = false;
            }
        }

        private void AppBar_Loaded(object sender, RoutedEventArgs e)
        {
            Button selection = (Button)DynamicGrid.SelectedItem;
            if (selection != null)
            {
                DeleteButton.Visibility = Visibility.Visible;

                if (selection.RowSpan != 2)
                    EnlargeButton.Visibility = Visibility.Visible;

                if (selection.ColSpan != 1)
                    ShrinkButton.Visibility = Visibility.Visible;
            }
        }

        private void AppBar_Unloaded(object sender, RoutedEventArgs e)
        {
            EnlargeButton.Visibility = Visibility.Collapsed;
            ShrinkButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
        }

        void OnSettingsPaneRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            SettingsCommand cmd = new SettingsCommand("voiceOptions",
                   "Voice Options", (x) =>
                   {
                       settingsPopup = new Popup();
                       settingsPopup.Closed += OnPopupClosed;
                       Window.Current.Activated += OnWindowActivated;
                       settingsPopup.IsLightDismissEnabled = true;
                       SimpleSettingsNarrow mypane = new SimpleSettingsNarrow();
                       mypane.Width = 100;
                       mypane.Height = 150;

                       settingsPopup.Child = mypane;
                       settingsPopup.Width = 100;
                       settingsPopup.Height = 150;
                       settingsPopup.IsOpen = true;
                   });
            args.Request.ApplicationCommands.Add(cmd);
        }

        void OnPopupClosed(object sender, object e)
        {
            Window.Current.Activated -= OnWindowActivated;
        }

        void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                settingsPopup.IsOpen = false;
            }
        }
    }

    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}