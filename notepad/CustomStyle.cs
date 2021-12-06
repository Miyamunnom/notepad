using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace notepad
{
    public class CustomStyle
    {
        public string name { get; set; }
        public List<CustomStyleElement> elements = new List<CustomStyleElement>();

        public CustomStyle()
        {

        }

        public CustomStyleElement getElementByName(string name)
        {
            foreach (CustomStyleElement item in elements)
            {
                if(item.name.Equals(name))
                {
                    return item;
                }
            }

            return null;
        }

    }
}
