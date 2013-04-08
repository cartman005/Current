using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace UBTalker
{
    /*
     * Database to store buttons/categories for pages.
     */
    public class Button
    {
        /* Uniquely identifies each element */
        [SQLite.AutoIncrement, SQLite.PrimaryKey]
        public int ID { get; set; }

        /* Order on the page */
        public int Order { get; set; }

        /* Horizontal width */
        public int ColSpan { get; set; }

        /* Vertical height */
        public int RowSpan { get; set; }

        /* Text on button */
        public string Name { get; set; }

        /* Filename of cover image */
        public string ImagePath { get; set; }

        /* Text to be spoken */
        public string Text { get; set; }

        /* Hexadecimal value of the button's color */
        public string ColorHex { get; set; }

        /* Audio file filename */
        public string FileName { get; set; }

        /* ID of category this element belongs to */
        public int Category { get; set; }

        /* Category Specific Attributes */
        /* True if element is a category */
        public Boolean isFolder { get; set; }

        /* Description of category */
        public string Description { get; set; }

        /* If element is a category, filename of background image for category */
        public string BGImagePath { get; set; }
    }
}