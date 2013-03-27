using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace TranslatorService.Example
{
    /*
     * Database to store each button for a given page.
     */
    public class Button
    {
        public int ID { get; set; }

        /* Order on the page */
        public int Order { get; set; }

        /* Horizontal width */
        public int ColSpan { get; set; }

        /* Vertical height */
        public int RowSpan { get; set; }

        /* Text on button */
        public string Name { get; set; }

        public BitmapImage ImagePath { get; set; }

        /* Text to be spoken */
        public string Text { get; set; }

        /* Hexadecimal value of the button's color */
        public Color Color { get; set; }

        public string Description { get; set; }

        public string FileName { get; set; }
    }
}