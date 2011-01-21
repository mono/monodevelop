//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Shell;

namespace ShellObjectDragAndDropDemo
{
    /// <summary>
    /// WPF ShellObject Drag and Drop demonstration window
    /// </summary>
    public partial class DragAndDropWindow : Window
    {
        #region implmentation data
        private Point dragStart;
        private DataObject dataObject = null;
        private bool inDragDrop = false;
        #endregion

        #region construction
        public DragAndDropWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region message handlers
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Drop += new DragEventHandler(OnDrop);
            DropSource.MouseLeftButtonDown += new MouseButtonEventHandler(OnMouseLeftButtonDown);
            DropSource.MouseLeftButtonUp += new MouseButtonEventHandler(OnMouseLeftButtonUp);
            DropDataList.MouseMove += new MouseEventHandler(OnMouseMove);
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!inDragDrop)
            {
                Point currentPos = e.GetPosition(this);

                if ((Math.Abs(currentPos.X - dragStart.X) > 5) || (Math.Abs(currentPos.Y - dragStart.Y) > 5))
                {
                    if (dataObject != null)
                    {
                        inDragDrop = true;
                        DragDropEffects de = DragDrop.DoDragDrop(this.DropSource, dataObject, DragDropEffects.Copy);
                        inDragDrop = false;
                        dataObject = null;
                    }
                }
            }
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dataObject = null;
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsMouseCaptured)
            {
                dragStart = e.GetPosition(this);
                ShellObjectCollection collection = new ShellObjectCollection();
                System.Collections.IList list =
                    (DropDataList.SelectedItems.Count > 0) ?
                        DropDataList.SelectedItems : DropDataList.Items;

                foreach (ShellObject shellObject in list)
                {
                    collection.Add(shellObject);
                }

                if (collection.Count > 0)
                {
                    // This builds a DataObject from a "Shell IDList Array" formatted memory stream.
                    // This allows drag/clipboard operations with non-file based ShellObjects (i.e., 
                    // control panel, libraries, search query results)
                    dataObject = new DataObject(
                        "Shell IDList Array",
                        collection.BuildShellIDList());

                    // Also build a file drop list
                    System.Collections.Specialized.StringCollection paths = new System.Collections.Specialized.StringCollection();
                    foreach (ShellObject shellObject in collection)
                    {
                        if (shellObject.IsFileSystemObject)
                        {
                            paths.Add(shellObject.ParsingName);
                        }
                    }
                    if (paths.Count > 0)
                        dataObject.SetFileDropList(paths);
                }
            }
        }

        void OnDrop(object sender, DragEventArgs e)
        {
            if (!inDragDrop)
            {
                string[] formats = e.Data.GetFormats();
                foreach (string format in formats)
                {
                    // Shell items are passed using the "Shell IDList Array" format. 
                    if (format == "Shell IDList Array")
                    {
                        // Retrieve the ShellObjects from the data object
                        DropDataList.ItemsSource = ShellObjectCollection.FromDataObject(
                            (System.Runtime.InteropServices.ComTypes.IDataObject)e.Data);

                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Handled = false;
        }
        #endregion
    }
}
