using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UBTalker.Speech;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace UBTalker
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class NewButtonPage : UBTalker.Common.LayoutAwarePage
    {

        int category;
        public NewButtonPage()
        {

            this.InitializeComponent();

            // Find better way to do this?
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Black", Color = Colors.Black });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "White", Color = Colors.White });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Red", Color = Colors.Red });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Blue", Color = Colors.Blue });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Green", Color = Colors.Green });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Yellow", Color = Colors.Yellow });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Gray", Color = Colors.Gray });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Purple", Color = Colors.Purple });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Orange", Color = Colors.Orange });
            ButtonColorChoices.Items.Add(new ColorChoice { Name = "Brown", Color = Colors.Brown });
            ButtonColorChoices.SelectedIndex = 0;

            FontColorChoices.Items.Add(new ColorChoice { Name = "Black", Color = Colors.Black });
            FontColorChoices.Items.Add(new ColorChoice { Name = "White", Color = Colors.White });
            FontColorChoices.Items.Add(new ColorChoice { Name = "Red", Color = Colors.Red });
            FontColorChoices.Items.Add(new ColorChoice { Name = "Blue", Color = Colors.Blue });
            FontColorChoices.Items.Add(new ColorChoice { Name = "Green", Color = Colors.Green });
            FontColorChoices.Items.Add(new ColorChoice { Name = "Yellow", Color = Colors.Yellow });
            FontColorChoices.Items.Add(new ColorChoice { Name = "Gray", Color = Colors.Gray });
            FontColorChoices.Items.Add(new ColorChoice { Name = "Purple", Color = Colors.Purple });
            FontColorChoices.Items.Add(new ColorChoice { Name = "Orange", Color = Colors.Orange });
            FontColorChoices.Items.Add(new ColorChoice { Name = "Brown", Color = Colors.Brown });
            FontColorChoices.SelectedIndex = 1;
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
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
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

        // Source: http://danlb.blogspot.com/2011/10/windows-8-metro-file-picker.html
        private async void UIOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();

            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");


            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            try
            {
                await file.CopyAsync(ApplicationData.Current.LocalFolder, file.Name);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }

            ButtonImageEntry.Text = file.Name;
        }

        private async void UIOpenSoundFile_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();

            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".m4a");


            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            try
            {
                await file.CopyAsync(ApplicationData.Current.LocalFolder, file.Name);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }

            SoundFileEntry.Text = file.Name;
        } 

        private async void CreateButton(object sender, RoutedEventArgs e)
        {

            WaitProgressBar.Visibility = Visibility.Visible;

            // Check that button text or sound file is entered
            // Name should be allowed to be left blank
            if ((SoundFileToggle.IsOn && string.IsNullOrWhiteSpace(SoundFileEntry.Text)) || (!SoundFileToggle.IsOn && string.IsNullOrWhiteSpace(ButtonTextEntry.Text))) {
                WaitProgressBar.Visibility = Visibility.Collapsed;
                MessageDialog dialog = new MessageDialog("You must enter a string to be spoken or provide your own sound file", "UB Talker");
                await dialog.ShowAsync();
                return;
            }

            Button newButton = new Button
            {
                Name = ButtonNameEntry.Text,
                ImagePath = ButtonImageEntry.Text,
                Category = category,
                isFolder = false,
                Language = MainPage.SpeakingLanguage
            };

            /* Get selected colors */
            if (ButtonColorChoices.SelectedIndex != -1)
            {
                newButton.ColorHex = (ButtonColorChoices.SelectedItem as ColorChoice).Color.ToString();
            }
            else
                newButton.ColorHex = Colors.Black.ToString();

            if (FontColorChoices.SelectedIndex != -1)
            {
                newButton.FontColor = (FontColorChoices.SelectedItem as ColorChoice).Color.ToString();
            }
            else
                newButton.FontColor = Colors.White.ToString();

            /* Add button to database */
            using (var db = new SQLiteConnection(Path.Combine(ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite")))
            {
                Button temp = null;
                try
                {
                    temp = db.Table<Button>().OrderBy(x => x.ID).Last();
                }
                catch (Exception) { }

                // Set the row ID based on the last entered rowID
                int rowid = 1;
                if (temp != null)
                    rowid = temp.ID + 1;

                newButton.Order = rowid;

                if (SoundFileToggle.IsOn)
                    newButton.FileName = SoundFileEntry.Text;
                else
                    newButton.Text = ButtonTextEntry.Text;

                System.Diagnostics.Debug.WriteLine("New Button Color: " + newButton.ColorHex);
                System.Diagnostics.Debug.WriteLine("New font Color: " + newButton.FontColor);

                db.Insert(newButton);
                System.Diagnostics.Debug.WriteLine("The category for the new button is " + category);
            }

            this.Frame.GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) 
        {
            base.OnNavigatedTo(e);
            category = (int) e.Parameter;
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (SoundFileToggle.IsOn)
            {
                SoundClipGrid.Visibility = Visibility.Visible;
                ButtonTextGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                SoundClipGrid.Visibility = Visibility.Collapsed;
                ButtonTextGrid.Visibility = Visibility.Visible;
            }

        }
    }

    public class ColorChoice
    {
        public string Name { get; set; }
        public Color Color { get; set; }
    }
}