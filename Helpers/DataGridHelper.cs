using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PowerBaseWpf.Helpers
{
    public sealed class DataGridHelper
    {
        private DataGrid DataGrid { get; }

        public DataGridHelper(DataGrid dataGrid)
        {
            DataGrid = dataGrid;

            dataGrid.Columns.CollectionChanged += Columns_CollectionChanged;
        }

        private void Columns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var forwardDataContext = GetForwardDataContext(DataGrid);

            if (forwardDataContext && DataGrid.DataContext != null)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (DataGridColumn column in e.NewItems)
                    {
                        column.SetValue(FrameworkElement.DataContextProperty, DataGrid.DataContext);
                    }
                }
            }
        }

        static DataGridHelper()
        {
            FrameworkElement.DataContextProperty.AddOwner(typeof(DataGridColumn));
            FrameworkElement.DataContextProperty.OverrideMetadata(typeof(DataGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        }

        public static readonly DependencyProperty ForwardDataContext =
            DependencyProperty.RegisterAttached("ForwardDataContext", typeof(bool), typeof(DataGridHelper),
                new FrameworkPropertyMetadata(true, OnForwardDataContextChanged));

        public static void OnDataContextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = obj as DataGrid;
            if (dataGrid == null) return;
            var forwardDataContext = GetForwardDataContext(dataGrid);
            if (forwardDataContext)
            {
                foreach (DataGridColumn col in dataGrid.Columns)
                {
                    col.SetValue(FrameworkElement.DataContextProperty, e.NewValue);
                }
            }
        }
        

        static void OnForwardDataContextChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var dataGrid = obj as DataGrid;
            if (dataGrid == null) return;

            new DataGridHelper(dataGrid);

            if (!(e.NewValue is bool)) return;

            if ((bool) e.NewValue && dataGrid.DataContext != null)
                OnDataContextChanged(obj, new DependencyPropertyChangedEventArgs(FrameworkElement.DataContextProperty, dataGrid.DataContext, dataGrid.DataContext));
        }

        public static bool GetForwardDataContext(DependencyObject dataGrid)
        {
            return (bool) dataGrid.GetValue(ForwardDataContext);
        }

        public static void SetForwardDataContext(DependencyObject dataGrid, bool value)
        {
            dataGrid.SetValue(ForwardDataContext, value);
        }
    }

}


