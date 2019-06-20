using MFE.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace MFE.WPF.Converters
{
    public class StatusIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (Status)value;
            var icon = "Skipped";
            switch (status)
            {
                case Status.Skipped:
                    icon = "Skipped";
                    break;
                case Status.Pending:
                    icon = "Pending";
                    break;
                case Status.Succeeded:
                    icon = "Succeeded";
                    break;
                case Status.Warning:
                    icon = "Warning";
                    break;
                case Status.Failed:
                    icon = "Failed";
                    break;
                default:
                    break;
            }

            return Application.Current.FindResource(icon);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
