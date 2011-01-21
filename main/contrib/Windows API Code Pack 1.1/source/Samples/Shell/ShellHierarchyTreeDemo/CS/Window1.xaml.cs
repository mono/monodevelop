//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Shell;

namespace ShellHierarchyTreeDemo
{
    /// <summary>
    /// This application demonstrates how to navigate the Shell namespace 
    /// starting from the Desktop folder (Shell.Desktop).
    /// </summary>
    public partial class Window1 : Window
    {
        static public IKnownFolder DesktopKnownFolder = KnownFolders.Desktop;

        public Window1()
        {
            InitializeComponent();

            // After everything is initialized, selected the header (Desktop)
            treeViewHeader.IsSelected = true;
            treeViewHeader.Focus();
        }

        void treeViewHeader_Selected(object sender, RoutedEventArgs e)
        {
            // Whenever the user selects this header, show the Desktop data
            if (treeViewHeader.IsSelected)
                ShowDesktopData();
        }

        void ShowDesktopData()
        {
            DesktopCollection.ShowObjectData(this, DesktopKnownFolder as ShellObject);
            DesktopCollection.ShowThumbnail(this, DesktopKnownFolder as ShellObject);
            DesktopCollection.ShowProperties(this, DesktopKnownFolder as ShellObject);
        }

        private void MenuItemRefresh_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem selectedItem = ExplorerTreeView.SelectedItem as TreeViewItem;

            if (selectedItem.ItemsSource is DesktopCollection)
            {
                selectedItem.ItemsSource = new DesktopCollection();
            }
            else if (selectedItem.ItemsSource is ShellContainer)
            {
                selectedItem.IsExpanded = false;
                selectedItem.Items.Clear();
                selectedItem.Items.Add(":::");
                selectedItem.IsExpanded = true;
            }
        }
    }

    public class DesktopCollection : Collection<object>
    {
        public DesktopCollection()
        {
            foreach (ShellObject obj in Window1.DesktopKnownFolder)
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = obj;
                if (obj is ShellContainer)
                {
                    item.Items.Add(":::");
                    item.Expanded += ExplorerTreeView_Expanded;
                }

                item.Selected += new RoutedEventHandler(item_Selected);
                Add(item);
            }
        }

        internal void ExplorerTreeView_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem sourceItem = sender as TreeViewItem;
            ShellContainer shellContainer = sourceItem.Header as ShellContainer;

            if (sourceItem.Items.Count > 0 && sourceItem.Items[0].Equals(":::"))
            {
                sourceItem.Items.Clear();
                try
                {
                    foreach (ShellObject obj in shellContainer)
                    {
                        TreeViewItem item = new TreeViewItem();
                        item.Header = obj;
                        if (obj is ShellContainer)
                        {
                            item.Items.Add(":::");
                            item.Expanded += ExplorerTreeView_Expanded;
                        }
                        item.Selected += new RoutedEventHandler(item_Selected);
                        sourceItem.Items.Add(item);
                    }
                }
                catch (FileNotFoundException)
                {
                    // Device might not be ready
                    MessageBox.Show("The device or directory is not ready.", "Shell Hierarchy Tree Demo");
                }
                catch (ArgumentException)
                {
                    // Device might not be ready
                    MessageBox.Show("The directory is currently not accessible.", "Shell Hierarchy Tree Demo");
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("You don't currently have permission to access this folder.", "Shell Hierarchy Tree Demo");
                }

            }
        }

        internal void item_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem)
            {

                Window1 wnd = (Window1)Application.Current.MainWindow;

                TreeViewItem sourceItem = wnd.ExplorerTreeView.SelectedItem as TreeViewItem;
                if (sourceItem == null)
                    return;

                ShellObject shellObj = sourceItem.Header as ShellObject;

                if (shellObj == null)
                    return;

                ShowObjectData(wnd, shellObj);
                ShowThumbnail(wnd, shellObj);
                try
                {
                    ShowProperties(wnd, shellObj);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Cannot show properties for \"{0}\": {1}", shellObj, ex.Message));
                    wnd.PropertiesListBox.ItemsSource = null;
                    wnd.FolderPropsListBox.Visibility = Visibility.Hidden;
                    wnd.PropertiesGrid.RowDefinitions[1].Height = new GridLength(0);
                }

            }
        }

        internal static void ShowProperties(Window1 wnd, ShellObject shellObj)
        {
            wnd.PropertiesListBox.ItemsSource = shellObj.Properties.DefaultPropertyCollection;

            if (shellObj is IKnownFolder)
            {
                ShowKnownFolderProperties(wnd, shellObj as IKnownFolder);
            }
            else if (shellObj is ShellLibrary)
            {
                ShowLibraryProperties(wnd, shellObj as ShellLibrary);
            }
            else
            {
                wnd.FolderPropsListBox.Visibility = Visibility.Hidden;
                wnd.PropertiesGrid.RowDefinitions[1].Height = new GridLength(0);
            }
        }

        internal static void ShowKnownFolderProperties(Window1 wnd, IKnownFolder kf)
        {
            wnd.FolderPropsListBox.Visibility = Visibility.Visible;
            wnd.PropertiesGrid.RowDefinitions[1].Height = new GridLength(150);

            Collection<KnownFolderProperty> properties = new Collection<KnownFolderProperty>();

            properties.Add(new KnownFolderProperty { Property = "Canonical Name", Value = kf.CanonicalName });
            properties.Add(new KnownFolderProperty { Property = "Category", Value = kf.Category });
            properties.Add(new KnownFolderProperty { Property = "Definition Options", Value = kf.DefinitionOptions });
            properties.Add(new KnownFolderProperty { Property = "Description", Value = kf.Description });
            properties.Add(new KnownFolderProperty { Property = "File Attributes", Value = kf.FileAttributes });
            properties.Add(new KnownFolderProperty { Property = "Folder Id", Value = kf.FolderId });
            properties.Add(new KnownFolderProperty { Property = "Folder Type", Value = kf.FolderType });
            properties.Add(new KnownFolderProperty { Property = "Folder Type Id", Value = kf.FolderTypeId });
            properties.Add(new KnownFolderProperty { Property = "Path", Value = kf.Path });
            properties.Add(new KnownFolderProperty { Property = "Relative Path", Value = kf.RelativePath });
            properties.Add(new KnownFolderProperty { Property = "Security", Value = kf.Security });
            properties.Add(new KnownFolderProperty { Property = "Tooltip", Value = kf.Tooltip });

            wnd.FolderPropsListBox.ItemsSource = properties;
        }

        internal static void ShowLibraryProperties(Window1 wnd, ShellLibrary lib)
        {
            wnd.FolderPropsListBox.Visibility = Visibility.Visible;
            wnd.PropertiesGrid.RowDefinitions[1].Height = new GridLength(150);

            Collection<KnownFolderProperty> properties = new Collection<KnownFolderProperty>();

            properties.Add(new KnownFolderProperty { Property = "Name", Value = lib.Name });
            object value = null;

            try
            {
                value = lib.LibraryType;
            }
            catch
            { }
            properties.Add(new KnownFolderProperty { Property = "Library Type", Value = value });


            try
            {
                value = lib.LibraryTypeId;
            }
            catch
            { }
            properties.Add(new KnownFolderProperty { Property = "Library Type Id", Value = value });

            properties.Add(new KnownFolderProperty { Property = "Path", Value = lib.ParsingName });
            properties.Add(new KnownFolderProperty { Property = "Is Pinned To NavigationPane", Value = lib.IsPinnedToNavigationPane });

            wnd.FolderPropsListBox.ItemsSource = properties;
        }

        internal static void ShowObjectData(Window1 wnd, ShellObject shellObj)
        {
            wnd.PropertiesTextBox.Text =
                String.Format(
                    "Name = {0}{1}Path/ParsingName = {2}{1}Type = {3}{4} ({5}File System)",
                    shellObj.Name,
                    Environment.NewLine,
                    shellObj.ParsingName,
                    shellObj.GetType().Name,
                    shellObj.IsLink ? " (Shortcut)" : "",
                    shellObj.IsFileSystemObject ? "" : "Non ");
        }

        internal static void ShowThumbnail(Window1 wnd, ShellObject shellObj)
        {
            try
            {
                wnd.ThumbnailPreview.Source = shellObj.Thumbnail.LargeBitmapSource;
            }
            catch
            {
                wnd.ThumbnailPreview.Source = null;
            }
        }

    }

    struct KnownFolderProperty
    {
        public string Property { set; get; }
        public object Value { set; get; }
    }
}
