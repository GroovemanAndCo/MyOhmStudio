/* 
 * This file is part of the MyOhmSessions distribution (https://github.com/GroovemanAndCo/MyOhmStudio).
 * Copyright (c) 2020 Fabien (https://github.com/fab672000)
 * 
 * This program is free software: you can redistribute it and/or modify  
 * it under the terms of the GNU General Public License as published by  
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
