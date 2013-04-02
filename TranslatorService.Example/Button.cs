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
     * Database to store each button for a given page.
     */
    public class Button
    {
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

        public string ImagePath { get; set; }

        /* Text to be spoken */
        public string Text { get; set; }

        /* Hexadecimal value of the button's color */
        public string ColorHex { get; set; }

        public string Description { get; set; }

        public string FileName { get; set; }

        public int Category { get; set; }

        public Boolean isFolder { get; set; }

        public string BGImagePath { get; set; }
    }
}