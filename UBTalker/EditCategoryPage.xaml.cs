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

            ColorChoices.Items.Add(new ColorChoice { Name = "Black", Color = Colors.Black });
            ColorChoices.Items.Add(new ColorChoice { Name = "Red", Color = Colors.Red });
            ColorChoices.Items.Add(new ColorChoice { Name = "Blue", Color = Colors.Blue });
            ColorChoices.Items.Add(new ColorChoice { Name = "Green", Color = Colors.Green });
            ColorChoices.Items.Add(new ColorChoice { Name = "Yellow", Color = Colors.Yellow });
            ColorChoices.Items.Add(new ColorChoice { Name = "Gray", Color = Colors.Gray });
            ColorChoices.Items.Add(new ColorChoice { Name = "Purple", Color = Colors.Purple });
            ColorChoices.Items.Add(new ColorChoice { Name = "Orange", Color = Colors.Orange });
            ColorChoices.Items.Add(new ColorChoice { Name = "Brown", Color = Colors.Brown });
            ColorChoices.SelectedIndex = 0;
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

            Color selection;
            /* Get selected color */
            if (ColorChoices.SelectedIndex != -1)
            {
                var pi = ColorChoices.SelectedItem as ColorChoice;
                selection = pi.Color;
            }
            else
                selection = Colors.Black;

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
                ModButton.ColorHex = selection.ToString();
                db.Update(ModButton);
            }

            this.Frame.GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string ColorHex;
            using (var db = new SQLiteConnection(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "TalkerDB.sqlite")))
            {
                ModButton = db.Table<Button>().FirstOrDefault(x => x.ID == (int)e.Parameter);
                ButtonImageEntry.Text = ModButton.ImagePath;
                ButtonNameEntry.Text = ModButton.Name;
                ButtonDescEntry.Text = ModButton.Description;
                BGImageEntry.Text = ModButton.BGImagePath;
                ColorHex = ModButton.ColorHex;
            }
            if (ModButton.ID == MainPage.DEFAULT_CATEGORY)
            {
                ButtonNameEntry.IsReadOnly = true;
                ButtonDescEntry.IsReadOnly = true;
                ImageEntry.Visibility = Visibility.Collapsed;
                ColorEntry.Visibility = Visibility.Collapsed;
            }
            else
            {
                /* Set color selectien */
                if (ColorHex == Colors.Red.ToString())
                    ColorChoices.SelectedIndex = 1;
                else if (ColorHex == Colors.Blue.ToString())
                    ColorChoices.SelectedIndex = 2;
                else if (ColorHex == Colors.Green.ToString())
                    ColorChoices.SelectedIndex = 3;
                else if (ColorHex == Colors.Yellow.ToString())
                    ColorChoices.SelectedIndex = 4;
                else if (ColorHex == Colors.Gray.ToString())
                    ColorChoices.SelectedIndex = 5;
                else if (ColorHex == Colors.Purple.ToString())
                    ColorChoices.SelectedIndex = 6;
                else if (ColorHex == Colors.Orange.ToString())
                    ColorChoices.SelectedIndex = 7;
                else if (ColorHex == Colors.Brown.ToString())
                    ColorChoices.SelectedIndex = 8;
                else
                    ColorChoices.SelectedIndex = 0;
            }
        }
    }
}