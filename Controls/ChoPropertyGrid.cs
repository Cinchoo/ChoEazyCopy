using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;
using System;

namespace ChoEazyCopy
{
    public enum PropertySort
    {
        NoSort = 0,
        Alphabetical = 1,
        Categorized = 2,
        CategorizedAlphabetical = 3
    }; 

    public class ChoPropertyGrid : Grid
    {
        #region Private fields
        private WorkflowDesigner Designer;
        private MethodInfo RefreshMethod;
        private MethodInfo OnSelectionChangedMethod;
        private MethodInfo IsInAlphaViewMethod;
        private TextBlock SelectionTypeLabel;
        private Control PropertyToolBar;
        private Border HelpText;
        private GridSplitter Splitter;
        private double HelpTextHeight = 60;
        #endregion

        #region Public properties
        /// <summary>Get or sets the selected object. Can be null.</summary>
        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }
        /// <summary>Get or sets the selected object collection. Returns empty array by default.</summary>
        public object[] SelectedObjects
        {
            get { return GetValue(SelectedObjectsProperty) as object[]; }
            set { SetValue(SelectedObjectsProperty, value); }
        }
        /// <summary>XAML information with PropertyGrid's font and color information</summary>
        /// <seealso>Documentation for WorkflowDesigner.PropertyInspectorFontAndColorData</seealso>
        public string FontAndColorData
        {
            set 
            { 
                Designer.PropertyInspectorFontAndColorData = value; 
            }
        }
        /// <summary>Shows the description area on the top of the control</summary>
        public bool HelpVisible
        {
            get { return (bool)GetValue(HelpVisibleProperty); }
            set { SetValue(HelpVisibleProperty, value); }
        }
        /// <summary>Shows the tolbar on the top of the control</summary>
        public bool ToolbarVisible
        {
            get { return (bool)GetValue(ToolbarVisibleProperty); }
            set { SetValue(ToolbarVisibleProperty, value); }
        }
        public PropertySort PropertySort
        {
            get { return (PropertySort)GetValue(PropertySortProperty); }
            set { SetValue(PropertySortProperty, value); }
        }
        #endregion

        #region Dependency properties registration
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(ChoPropertyGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedObjectPropertyChanged));

        public static readonly DependencyProperty SelectedObjectsProperty =
            DependencyProperty.Register("SelectedObjects", typeof(object[]), typeof(ChoPropertyGrid),
            new FrameworkPropertyMetadata(new object[0], FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedObjectsPropertyChanged, CoerceSelectedObjects));

        public static readonly DependencyProperty HelpVisibleProperty =
            DependencyProperty.Register("HelpVisible", typeof(bool), typeof(ChoPropertyGrid),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, HelpVisiblePropertyChanged));
        public static readonly DependencyProperty ToolbarVisibleProperty =
            DependencyProperty.Register("ToolbarVisible", typeof(bool), typeof(ChoPropertyGrid),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ToolbarVisiblePropertyChanged));
        public static readonly DependencyProperty PropertySortProperty =
            DependencyProperty.Register("PropertySort", typeof(PropertySort), typeof(ChoPropertyGrid),
            new FrameworkPropertyMetadata(PropertySort.CategorizedAlphabetical, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PropertySortPropertyChanged));
        #endregion

        #region Dependency properties events
        private static object CoerceSelectedObject(DependencyObject d, object value)
        {
            ChoPropertyGrid pg = d as ChoPropertyGrid;

            object[] collection = pg.GetValue(SelectedObjectsProperty) as object[];

            return collection.Length == 0 ? null : value;
        }
        private static object CoerceSelectedObjects(DependencyObject d, object value)
        {
            ChoPropertyGrid pg = d as ChoPropertyGrid;

            object single = pg.GetValue(SelectedObjectsProperty);

            return single == null ? new object[0] : value;
        }

        private static void SelectedObjectPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ChoPropertyGrid pg = source as ChoPropertyGrid;
            pg.CoerceValue(SelectedObjectsProperty);

            if (e.NewValue == null)
            {
                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { null });
                pg.SelectionTypeLabel.Text = string.Empty;
            }
            else
            {
                var context = new EditingContext();
                var mtm = new ModelTreeManager(context);
                mtm.Load(e.NewValue);
                Selection selection = Selection.Select(context, mtm.Root);

                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { selection });
                pg.SelectionTypeLabel.Text = e.NewValue.GetType().Name;
            }

            pg.ChangeHelpText(string.Empty, string.Empty);
        }
        private static void SelectedObjectsPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ChoPropertyGrid pg = source as ChoPropertyGrid;
            pg.CoerceValue(SelectedObjectsProperty);

            object[] collection = e.NewValue as object[];

            if (collection.Length == 0)
            {
                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { null });
                pg.SelectionTypeLabel.Text = string.Empty;
            }
            else
            {
                bool same = true;
                Type first = null;

                var context = new EditingContext();
                var mtm = new ModelTreeManager(context);
                Selection selection = null;

                // Accumulates the selection and determines the type to be shown in the top of the PG
                for (int i = 0; i < collection.Length; i++)
                {
                    mtm.Load(collection[i]);
                    if (i == 0)
                    {
                        selection = Selection.Select(context, mtm.Root);
                        first = collection[0].GetType();
                    }
                    else
                    {
                        selection = Selection.Union(context, mtm.Root);
                        if (!collection[i].GetType().Equals(first))
                            same = false;
                    }
                }

                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { selection });
                pg.SelectionTypeLabel.Text = same ? first.Name + " <multiple>" : "Object <multiple>";
            }

            pg.ChangeHelpText(string.Empty, string.Empty);
        }
        private static void HelpVisiblePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ChoPropertyGrid pg = source as ChoPropertyGrid;

            if (e.NewValue != e.OldValue)
            {
                if (e.NewValue.Equals(true))
                {
                    pg.RowDefinitions[1].Height = new GridLength(5);
                    pg.RowDefinitions[2].Height = new GridLength(pg.HelpTextHeight);
                }
                else
                {
                    pg.HelpTextHeight = pg.RowDefinitions[2].Height.Value;
                    pg.RowDefinitions[1].Height = new GridLength(0);
                    pg.RowDefinitions[2].Height = new GridLength(0);
                }
            }        
        }
        private static void ToolbarVisiblePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ChoPropertyGrid pg = source as ChoPropertyGrid;
            pg.PropertyToolBar.Visibility = e.NewValue.Equals(true) ? Visibility.Visible : Visibility.Collapsed;
        }
        private static void PropertySortPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ChoPropertyGrid pg = source as ChoPropertyGrid;
            PropertySort sort = (PropertySort)e.NewValue;

            bool isAlpha = (sort == PropertySort.Alphabetical || sort == PropertySort.NoSort);
            pg.IsInAlphaViewMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { isAlpha });
        }
        #endregion

        /// <summary>Default constructor, creates the UIElements including a PropertyInspector</summary>
        public ChoPropertyGrid()
        {
            this.ColumnDefinitions.Add(new ColumnDefinition());
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });

            this.Designer = new WorkflowDesigner();
            TextBlock title = new TextBlock()
            {
                Visibility = Visibility.Visible,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight = FontWeights.Bold
            };
            TextBlock descrip = new TextBlock()
            {
                Visibility = Visibility.Visible,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            DockPanel dock = new DockPanel()
            {
                Visibility = Visibility.Visible,
                LastChildFill = true,
                Margin = new Thickness(3,0,3,0)
            };

            title.SetValue(DockPanel.DockProperty, Dock.Top);
            dock.Children.Add(title);
            dock.Children.Add(descrip);
            this.HelpText = new Border()
            {
                Visibility = Visibility.Visible,
                BorderBrush = SystemColors.ActiveBorderBrush,
                Background = SystemColors.ControlBrush,
                BorderThickness = new Thickness(1),
                Child = dock
            };
            this.Splitter = new GridSplitter() 
            { 
                Visibility = Visibility.Visible,
                ResizeDirection = GridResizeDirection.Rows, 
                Height=1, 
                HorizontalAlignment= HorizontalAlignment.Stretch
            };

            var inspector = Designer.PropertyInspectorView;
            inspector.Visibility = Visibility.Visible;
            inspector.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);

            this.Splitter.SetValue(Grid.RowProperty, 1);
            this.Splitter.SetValue(Grid.ColumnProperty, 0);

            this.HelpText.SetValue(Grid.RowProperty, 2);
            this.HelpText.SetValue(Grid.ColumnProperty, 0);

            Binding binding = new Binding("Parent.Background");
            title.SetBinding(BackgroundProperty, binding);
            descrip.SetBinding(BackgroundProperty, binding);

            this.Children.Add(inspector);
            this.Children.Add(this.Splitter);
            this.Children.Add(this.HelpText);

            Type inspectorType = inspector.GetType();
            var props = inspectorType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

            var methods = inspectorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

            this.RefreshMethod = inspectorType.GetMethod("RefreshPropertyList",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            this.IsInAlphaViewMethod = inspectorType.GetMethod("set_IsInAlphaView",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            this.OnSelectionChangedMethod = inspectorType.GetMethod("OnSelectionChanged", 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            this.SelectionTypeLabel = inspectorType.GetMethod("get_SelectionTypeLabel",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Invoke(inspector, new object[0]) as TextBlock;
            this.PropertyToolBar = inspectorType.GetMethod("get_PropertyToolBar",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Invoke(inspector, new object[0]) as Control;
            inspectorType.GetEvent("GotFocus").AddEventHandler(this,
                Delegate.CreateDelegate(typeof(RoutedEventHandler), this, "GotFocusHandler", false));

            this.SelectionTypeLabel.Text = string.Empty;
        }
        /// <summary>Updates the PropertyGrid's properties</summary>
        public void RefreshPropertyList()
        {
            RefreshMethod.Invoke(Designer.PropertyInspectorView, new object[] { false });
        }

        /// <summary>Traps the change of focused property and updates the help text</summary>
        /// <param name="sender">Not used</param>
        /// <param name="args">Points to the source control containing the selected property</param>
        private void GotFocusHandler(object sender, RoutedEventArgs args)
        {
            //if (args.OriginalSource is TextBlock)
            //{
                string title = string.Empty;
                string descrip = string.Empty;
                var theSelectedObjects = this.GetValue(SelectedObjectsProperty) as object[];

                if (theSelectedObjects != null && theSelectedObjects.Length > 0)
                {
                    Type first = theSelectedObjects[0].GetType();
                    for (int i = 1; i < theSelectedObjects.Length; i++)
                    {
                        if (!theSelectedObjects[i].GetType().Equals(first))
                        {
                            ChangeHelpText(title, descrip);
                            return;
                        }
                    }

                    object data = (args.OriginalSource as FrameworkElement).DataContext;
                    PropertyInfo propEntry = data != null ? data.GetType().GetProperty("PropertyEntry") : null;
                    if (propEntry == null)
                    {
                        propEntry = data != null ? data.GetType().GetProperty("ParentProperty") : null;
                    }

                    if (propEntry != null)
                    {
                        object propEntryValue = propEntry.GetValue(data, null);
                        string propName = propEntryValue.GetType().GetProperty("PropertyName").GetValue(propEntryValue, null) as string;
                        title = propEntryValue.GetType().GetProperty("DisplayName").GetValue(propEntryValue, null) as string;
                        PropertyInfo property = theSelectedObjects[0].GetType().GetProperty(propName);
                        object[] attrs = property == null ? null : property.GetCustomAttributes(typeof(DescriptionAttribute), true);

                        if (attrs != null && attrs.Length > 0)
                            descrip = (attrs[0] as DescriptionAttribute).Description;
                    }
                    ChangeHelpText(title, descrip);
                }
            //}
        }
        /// <summary>Changes the text help area contents</summary>
        /// <param name="title">Title in bold</param>
        /// <param name="descrip">Description with ellipsis</param>
        private void ChangeHelpText(string title, string descrip)
        {
            DockPanel dock = this.HelpText.Child as DockPanel;
            (dock.Children[0] as TextBlock).Text = title;
            (dock.Children[1] as TextBlock).Text = descrip;
        }
    }
}
