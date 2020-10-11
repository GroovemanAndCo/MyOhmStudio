using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MyOhmSessions.UI
{
    public class ColorFromSchemeConverter :IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool b)) throw new ArgumentException(nameof(value));
            if (!(parameter is string s)) throw new ArgumentException(nameof(parameter));
            var c = (Color)ColorConverter.ConvertFromString(s);
            
            var ic = // avoid red which is the invert of cyan if light scheme, otherwise just automatically invert color:
                (b && s == "Cyan" ) ? Colors.Coral : (!b ? c : Color.FromArgb(c.A, (byte)(255-c.R),(byte) (255-c.G), (byte) (255-c.B)));
            return new SolidColorBrush(ic);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
