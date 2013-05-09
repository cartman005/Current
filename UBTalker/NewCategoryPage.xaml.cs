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
    public sealed partial class NewCategoryPage : UBTalker.Common.LayoutAwarePage
    {

        int category;
        public NewCategoryPage()
        {

            this.InitializeComponent();

            // Find a better way to do this?
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
            Windows.UI.Xaml.Controls.Button srcButton = e.OriginalSource as Windows.UI.Xaml.Controls.Button;

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
            catch (Exception)
            {
            }

            if (srcButton.Name == "UIOpenFile1")
                ButtonImageEntry.Text = file.Name;
            else
                BGImageEntry.Text = file.Name;
        }

        private void CreateButton(object sender, RoutedEventArgs e)
        {

            WaitProgressBar.Visibility = Visibility.Visible;

            /* Speak string */
            if (string.IsNullOrWhiteSpace(ButtonNameEntry.Text)) {
                WaitProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            Button newButton = new Button
            {
                Name = ButtonNameEntry.Text,
                Text = "Category " + ButtonNameEntry.Text,
                Description = ButtonDescEntry.Text,
                ImagePath = ButtonImageEntry.Text,
                Category = category,
                isFolder = true,
                BGImagePath = BGImageEntry.Text,
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

                int rowid = 1;
                if (temp != null)
                    rowid = temp.ID + 1;

                newButton.Order = rowid;

                db.Insert(newButton);
            }

            this.Frame.GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) 
        {
            base.OnNavigatedTo(e);
            category = (int) e.Parameter;   
        } 
    }
}