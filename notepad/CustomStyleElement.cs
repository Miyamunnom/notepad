using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace notepad
{
    public class CustomStyleElement
    {
        public string name { get; set; }
        public Thickness thickness { get; set; }
        public Brush textColor { get; set; }
        public Brush backgroundColor { get; set; }
        public double fontSize { get; set; }
        public FontFamily font { get; set; }

        public CustomStyleElement()
        { 
        }

        public CustomStyleElement(string name, Thickness thickness, Brush textColor, Brush backgroundColor, FontFamily font, double fontSize)
        {
            this.name = name;
            this.thickness = thickness;
            this.textColor = textColor;
            this.backgroundColor = backgroundColor;
            this.fontSize = fontSize;
            this.font = font;
        }

    }
}
