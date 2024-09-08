using Cinchoo.Core.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace ChoEazyCopy
{
    /*
    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class CopyFlagsEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoCopyFlags>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class FileAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileAttributes>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class FileSelectionAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileSelectionAttributes>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    public class FileMoveSelectionAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoFileMoveComboBox cmb = new ChoFileMoveComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoFileMoveComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileMoveAttributes>(propertyItem.Value.ToNString()).Select(c => c.Title);
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => cmb.SelectedItem = propertyItem.Value.ToNString()));
            return cmb;
        }
    }

    public class ChoFileMoveComboBox : ComboBox
    {
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                if (e.AddedItems != null && e.AddedItems.Count > 0)
                {
                    var value = (ChoFileMoveAttributes)Enum.Parse(typeof(ChoFileMoveAttributes), e.AddedItems.OfType<string>().FirstOrDefault());
                    if (value == ChoFileMoveAttributes.MoveFilesOnly)
                    {
                        if (MessageBox.Show("Would like to delete the original file(s) after transferring the copies to the new location?", MainWindow.Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (value == ChoFileMoveAttributes.MoveDirectoriesAndFiles)
                    {
                        if (MessageBox.Show("Would like to delete the original file(s) / folder(s) after transferring the copies to the new location?", MainWindow.Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
            base.OnSelectionChanged(e);
        }
    }
    public class ChoMultilineTextBoxEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            System.Windows.Controls.TextBox textBox = new System.Windows.Controls.TextBox();
            textBox.AcceptsReturn = true;
            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(textBox, System.Windows.Controls.TextBox.TextProperty, _binding);
            return textBox;
        }
    }
    */
}
