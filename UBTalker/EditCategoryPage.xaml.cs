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
    public sealed partial class EditCategoryPage : UBTalker.Common.LayoutAwarePage
    {

        Button ModButton;
        public EditCategoryPage()
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
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }

            if (srcButton.Name == "UIOpenFile1")
                ButtonImageEntry.Text = file.Name;
            else
                BGImageEntry.Text = file.Name;
        }

        private void CreateButton(object sender, RoutedEventArgs e)
        {

            WaitProgressBar.Visibility = Visibility.Visible;

            /* Get selected colors */
            if (ButtonColorChoices.SelectedIndex != -1)
            {
                ModButton.ColorHex = (ButtonColorChoices.SelectedItem as ColorChoice).Color.ToString();
            }
            else
                ModButton.ColorHex = Colors.Black.ToString();

            if (FontColorChoices.SelectedIndex != -1)
            {
                ModButton.FontColor = (FontColorChoices.SelectedItem as ColorChoice).Color.ToString();
            }
            else
                ModButton.FontColor = Colors.White.ToString();

            /* Add button to database */
            using (var db = new SQLiteConnection(Path.Combine(ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite")))
            {
                if (ModButton.Name != ButtonNameEntry.Text)
                {
                    ModButton.Name = ButtonNameEntry.Text;
                    ModButton.Text = "Category " + ButtonNameEntry.Text;
                    ModButton.FileName = null;
                }
                ModButton.ImagePath = ButtonImageEntry.Text;
                ModButton.BGImagePath = BGImageEntry.Text;
                db.Update(ModButton);
            }

            this.Frame.GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string ColorHex, FontColor;
            using (var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite")))
            {
                ModButton = db.Table<Button>().FirstOrDefault(x => x.ID == (int)e.Parameter);
                ButtonImageEntry.Text = ModButton.ImagePath;
                ButtonNameEntry.Text = ModButton.Name;
                ButtonDescEntry.Text = ModButton.Description;
                BGImageEntry.Text = ModButton.BGImagePath;
                ColorHex = ModButton.ColorHex;
                FontColor = ModButton.FontColor;
            }

            /* Hide options that can't be changed for the default category */
            if (ModButton.ID == MainPage.DEFAULT_CATEGORY)
            {
                ButtonNameEntry.IsReadOnly = true;
                ButtonDescEntry.IsReadOnly = true;
                ImageEntry.Visibility = Visibility.Collapsed;
                ButtonColorEntry.Visibility = Visibility.Collapsed;
                FontColorEntry.Visibility = Visibility.Collapsed;
            }
            else
            {
                /* Set color selections */
                if (ColorHex == Colors.White.ToString())
                    ButtonColorChoices.SelectedIndex = 1;
                else if (ColorHex == Colors.Red.ToString())
                    ButtonColorChoices.SelectedIndex = 2;
                else if (ColorHex == Colors.Blue.ToString())
                    ButtonColorChoices.SelectedIndex = 3;
                else if (ColorHex == Colors.Green.ToString())
                    ButtonColorChoices.SelectedIndex = 4;
                else if (ColorHex == Colors.Yellow.ToString())
                    ButtonColorChoices.SelectedIndex = 5;
                else if (ColorHex == Colors.Gray.ToString())
                    ButtonColorChoices.SelectedIndex = 6;
                else if (ColorHex == Colors.Purple.ToString())
                    ButtonColorChoices.SelectedIndex = 7;
                else if (ColorHex == Colors.Orange.ToString())
                    ButtonColorChoices.SelectedIndex = 8;
                else if (ColorHex == Colors.Brown.ToString())
                    ButtonColorChoices.SelectedIndex = 9;
                else
                    ButtonColorChoices.SelectedIndex = 0;               // Default to black

                if (FontColor == Colors.Black.ToString())
                    FontColorChoices.SelectedIndex = 0;
                else if (FontColor == Colors.Red.ToString())
                    FontColorChoices.SelectedIndex = 2;
                else if (FontColor == Colors.Blue.ToString())
                    FontColorChoices.SelectedIndex = 3;
                else if (FontColor == Colors.Green.ToString())
                    FontColorChoices.SelectedIndex = 4;
                else if (FontColor == Colors.Yellow.ToString())
                    FontColorChoices.SelectedIndex = 5;
                else if (FontColor == Colors.Gray.ToString())
                    FontColorChoices.SelectedIndex = 6;
                else if (FontColor == Colors.Purple.ToString())
                    FontColorChoices.SelectedIndex = 7;
                else if (FontColor == Colors.Orange.ToString())
                    FontColorChoices.SelectedIndex = 8;
                else if (FontColor == Colors.Brown.ToString())
                    FontColorChoices.SelectedIndex = 9;
                else
                    FontColorChoices.SelectedIndex = 1;                 // Default to white
            }
        }
    }
}