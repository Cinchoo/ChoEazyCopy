using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Cinchoo.Core;

namespace ChoEazyCopy
{
    /// <summary>
    /// Interaction logic for ChoDonateButton.xaml
    /// </summary>
    public partial class ChoDonateButton : UserControl
    {
        public static readonly DependencyProperty PaypalAccountEmailProperty = DependencyProperty.Register("PaypalAccountEmail", typeof(string), typeof(ChoDonateButton), new PropertyMetadata(""));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(ChoDonateButton), new PropertyMetadata("Donation"));
        public static readonly DependencyProperty CountryProperty = DependencyProperty.Register("Country", typeof(string), typeof(ChoDonateButton), new PropertyMetadata("US"));
        public static readonly DependencyProperty CurrencyProperty = DependencyProperty.Register("Currency", typeof(string), typeof(ChoDonateButton), new PropertyMetadata("USD"));

        public ChoDonateButton()
        {
            InitializeComponent();
        }

        public string PaypalAccountEmail
        {
            get { return (string)GetValue(PaypalAccountEmailProperty); }
            set { SetValue(PaypalAccountEmailProperty, value); }
        }

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public string Country
        {
            get { return (string)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        public string Currency
        {
            get { return (string)GetValue(CurrencyProperty); }
            set { SetValue(CurrencyProperty, value); }
        }

        private void btnDonate_Click(object sender, RoutedEventArgs e)
        {
            ChoGuard.ArgumentNotNullOrEmpty(PaypalAccountEmail, "PaypalAccountEmail");

            string url = "";

            url += "https://www.paypal.com/cgi-bin/webscr" +
                "?cmd=" + "_donations" +
                "&business=" + PaypalAccountEmail +
                "&lc=" + Country +
                "&item_name=" + Description +
                "&currency_code=" + Currency +
                "&bn=" + "PP%2dDonationsBF";

            System.Diagnostics.Process.Start(url);

        }
    }
}
