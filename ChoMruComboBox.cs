using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ChoEazyCopy
{
    public class ChoMruComboBox : Control
    {
        
        #region Dependency Properties

        public static readonly DependencyProperty MruSourceProperty =
            DependencyProperty.Register("MruSource", typeof(ChoObservableMruList<string>), typeof(ChoMruComboBox));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ChoMruComboBox), new PropertyMetadata(OnTextChanged));

        #endregion

        #region Constructors

        static ChoMruComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChoMruComboBox), new FrameworkPropertyMetadata(typeof(ChoMruComboBox)));
        }

        public ChoMruComboBox()
        {

            this.Focusable = false;
            this.IsTabStop = false;
            
        }

        #endregion

        #region Properties

        public ChoObservableMruList<string> MruSource
        {
            get { return (ChoObservableMruList<string>)GetValue(MruSourceProperty); }
            set { SetValue(MruSourceProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        #endregion

        #region Event Handlers

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
                        
            ChoMruComboBox srcControl = d as ChoMruComboBox;
            if (srcControl != null)
            {
                if (srcControl.MruSource != null)
                {
                    var dir = e.NewValue.ToString();

                    if (!dir.IsNullOrWhiteSpace() && Directory.Exists(dir))
                        srcControl.MruSource.Add(dir);
                }
            }

        }
        private static void OnTextChanged1(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChoMruComboBox srcControl = d as ChoMruComboBox;
            if (srcControl != null)
            {
                if (srcControl.MruSource != null)
                {
                    System.Threading.ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(false),
                    (state, bTimeout) => srcControl.HandleMruUpdate(e.NewValue.ToString()), "", TimeSpan.FromSeconds(1), true);
                }
            }
        }
        private void HandleMruUpdate(string listVal)
        {
            if (this.Dispatcher.CheckAccess()) 
            {
                if (Directory.Exists(listVal))
                    this.MruSource.Add(listVal); 
            }
            else
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<string>(HandleMruUpdate), listVal);
            }
        }
        #endregion

    }

}
