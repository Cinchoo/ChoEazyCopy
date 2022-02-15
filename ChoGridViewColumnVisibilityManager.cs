using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChoEazyCopy
{
    public class ChoGridViewColumnVisibilityManager
    {
        private static Dictionary<GridViewColumn, double> _columnWidths = null;
        static void UpdateListView(ListView lv)
        {
            GridView gridview = lv.View as GridView;
            if (gridview == null || gridview.Columns == null) return;

            if (_columnWidths == null)
            {
                _columnWidths = new Dictionary<GridViewColumn, double>();
                foreach (GridViewColumn gc in gridview.Columns)
                    _columnWidths.Add(gc, gc.Width);
            }

            List<GridViewColumn> toRemove = new List<GridViewColumn>();
            foreach (GridViewColumn gc in gridview.Columns)
            {
                if (!GetIsVisible(gc))
                    gc.Width = 0;
                else
                    gc.Width = _columnWidths[gc];
            }
        }

        public static bool GetIsVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsVisibleProperty, value);
        }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(ChoGridViewColumnVisibilityManager), new UIPropertyMetadata(true, OnIsVisibleChanged));


        public static bool GetEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnabledProperty);
        }

        public static void SetEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(EnabledProperty, value);
        }

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(ChoGridViewColumnVisibilityManager), new UIPropertyMetadata(false,
                new PropertyChangedCallback(OnEnabledChanged)));

        private static void OnIsVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GridViewColumn gc = obj as GridViewColumn;
            if (gc != null)
            {
                if (!GetIsVisible(gc))
                    gc.Width = 0;
                else
                    gc.Width = _columnWidths[gc];
            }
        }
        private static void OnEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ListView view = obj as ListView;
            if (view != null)
            {
                bool enabled = (bool)e.NewValue;
                if (enabled)
                {
                    view.Loaded += (sender, e2) =>
                    {
                        UpdateListView((ListView)sender);
                    };
                    view.TargetUpdated += (sender, e2) =>
                    {
                        UpdateListView((ListView)sender);
                    };
                    view.DataContextChanged += (sender, e2) =>
                    {
                        UpdateListView((ListView)sender);
                    };
                }
            }
        }
    }
}
