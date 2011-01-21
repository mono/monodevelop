//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Linq;

using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Controls;

namespace Microsoft.WindowsAPICodePack.Samples
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ExplorerBrowserTestWindow : Window
    {
        public ExplorerBrowserTestWindow()
        {
            InitializeComponent();

            var sortedKnownFolders =
                from folder in KnownFolders.All
                where (folder.CanonicalName != null &&
                    folder.CanonicalName.Length > 0 &&
                    ((ShellObject)folder).Thumbnail.BitmapSource != null &&
                    folder.CanonicalName.CompareTo("Network") != 0 &&
                    folder.CanonicalName.CompareTo("NetHood") != 0)
                orderby folder.CanonicalName
                select folder;
            knownFoldersCombo.ItemsSource = sortedKnownFolders;

            var viewModes =
                from mode in Enums.Get<ExplorerBrowserViewMode>()
                orderby mode.ToString()
                select mode;
            ViewModeCombo.ItemsSource = viewModes;
            ViewModeCombo.Text = "Auto";

            this.Loaded += new RoutedEventHandler(ExplorerBrowserTestWindow_Loaded);
        }

        void ExplorerBrowserTestWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Navigate to initial folder
            eb.ExplorerBrowserControl.Navigate((ShellObject)KnownFolders.Desktop);
        }

        private void navigateFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShellFile sf = ShellFile.FromFilePath(navigateFileTextBox.Text);
                eb.ExplorerBrowserControl.Navigate(sf);
            }
            catch
            {
                MessageBox.Show("Navigation not possible!");
            }
        }

        private void navigateFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShellFileSystemFolder sf = ShellFileSystemFolder.FromFolderPath(navigateFolderTextBox.Text);
                eb.ExplorerBrowserControl.Navigate(sf);
            }
            catch
            {
                MessageBox.Show("Navigation not possible!");
            }
        }

        private void navigateKnownFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IKnownFolder kf = knownFoldersCombo.Items[knownFoldersCombo.SelectedIndex] as IKnownFolder;
                eb.ExplorerBrowserControl.Navigate((ShellObject)kf);
            }
            catch
            {
                MessageBox.Show("Navigation not possible!");
            }
        }

        private void ClearNavigationLog_Click(object sender, RoutedEventArgs e)
        {
            eb.ExplorerBrowserControl.NavigationLog.ClearLog();
        }
    }

    public static class Enums
    {
        public static IEnumerable<T> Get<T>()
        {
            return System.Enum.GetValues(typeof(T)).Cast<T>();
        }
    }

    public class TriCheckToPaneVisibilityState : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return PaneVisibilityState.DoNotCare;
            else if ((bool)value == true)
                return PaneVisibilityState.Show;
            else
                return PaneVisibilityState.Hide;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((PaneVisibilityState)value == PaneVisibilityState.DoNotCare)
                return null;
            else if ((PaneVisibilityState)value == PaneVisibilityState.Show)
                return true;
            else
                return false;
        }

        #endregion
    }

}
