using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranslatorService.Example
{
    class Category
    {
        [SQLite.PrimaryKey]
        public int Name { get; set; }
    }
}
