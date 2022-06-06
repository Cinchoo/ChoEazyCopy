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
        private static Dictionary<GridView, Dictionary<GridViewColumn, double>> _columns = null;

        public static void ResizeAllColumnsToFit(DependencyObject obj)
        {
            ListView lv = obj as ListView;
            if (lv == null) return;

            GridView gridview = lv.View as GridView;
            if (gridview == null || gridview.Columns == null) return;

            var columns = _columns;
            if (columns == null || !_columns.ContainsKey(gridview))
                return;
            if (_columns[gridview] == null)
                return;

            foreach (GridViewColumn gc in _columns[gridview].Keys.ToArray())
            {
                gc.Width = 0;
                gc.Width = Double.NaN;
                columns[gridview][gc] = gc.Width;
            }

        }
        public static bool Contains(GridViewColumn col)
        {
            Dictionary<GridViewColumn, double> dict = null;
            return Contains(col, out dict);
        }

        public static bool Contains(GridViewColumn col, out Dictionary<GridViewColumn, double> dict)
        {
            dict = null;
            if (col == null)
                return false;
            if (_columns == null)
                return false;

            foreach (var gv in _columns.Keys.ToArray())
            {
                if (_columns[gv].ContainsKey(col))
                {
                    dict = _columns[gv];
                    return true;
                }
            }

            return false;
        }

        public static void SetGridColumnWidth(GridViewColumnHeader colHeader)
        {
            SetGridColumnWidth(colHeader.Column);
        }

        public static void SetGridColumnWidth(GridViewColumn col)
        {
            Dictionary<GridViewColumn, double> dict = null;
            if (Contains(col, out dict) && (col.Width > 0 || col.Width == double.NaN))
                dict[col] = col.Width;
        }

        static void UpdateListView(ListView lv)
        {
            GridView gridview = lv.View as GridView;
            if (gridview == null || gridview.Columns == null) return;

            if (_columns == null)
                _columns = new Dictionary<GridView, Dictionary<GridViewColumn, double>>();

            if (!_columns.ContainsKey(gridview))
            {
                _columns.Add(gridview, new Dictionary<GridViewColumn, double>());
                foreach (GridViewColumn gc in gridview.Columns)
                    _columns[gridview].Add(gc, gc.Width);
            }

            List<GridViewColumn> toRemove = new List<GridViewColumn>();
            foreach (GridViewColumn gc in gridview.Columns)
            {
                if (!GetIsVisible(gc))
                {
                    gc.Width = 0;
                    ((GridViewColumnHeader)gc.Header).IsHitTestVisible = false;
                }
                else
                {
                    Dictionary<GridViewColumn, double> dict = null;
                    if (Contains(gc, out dict))
                    {
                        gc.Width = dict[gc];
                        ((GridViewColumnHeader)gc.Header).IsHitTestVisible = true;
                    }
                }
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
                {
                    gc.Width = 0;
                    ((GridViewColumnHeader)gc.Header).IsHitTestVisible = false;
                }
                else
                {
                    Dictionary<GridViewColumn, double> dict = null;
                    if (Contains(gc, out dict))
                    {
                        gc.Width = dict[gc];
                        ((GridViewColumnHeader)gc.Header).IsHitTestVisible = true;
                    }
                }
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
