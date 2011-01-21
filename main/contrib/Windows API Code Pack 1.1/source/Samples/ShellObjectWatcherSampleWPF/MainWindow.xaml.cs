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
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;

namespace ShellObjectWatcherSampleWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private ShellObjectWatcher _watcher = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.AllowNonFileSystemItems = true;
            cfd.EnsureReadOnly = true;
            cfd.IsFolderPicker = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                StartWatcher(cfd.FileAsShellObject);                
            }
        }

        private void btnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.AllowNonFileSystemItems = true;
            cfd.EnsureReadOnly = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                StartWatcher(cfd.FileAsShellObject);
            }
        }

        private void StartWatcher(ShellObject shellObject)
        {
            if (_watcher != null) { _watcher.Dispose(); }
            eventStack.Children.Clear();

            txtPath.Text = shellObject.ParsingName;

            _watcher = new ShellObjectWatcher(shellObject, chkRecursive.IsChecked ?? true);
            _watcher.AllEvents += AllEventsHandler;

            _watcher.Start();
        }

        void AllEventsHandler(object sender, ShellObjectNotificationEventArgs e)
        {
            eventStack.Children.Add(
                new Label()
                {
                    Content = FormatEvent(e.ChangeType, e)
                });
        }

        private string FormatEvent(ShellObjectChangeTypes changeType, ShellObjectNotificationEventArgs args)
        {
            ShellObjectChangedEventArgs changeArgs;
            ShellObjectRenamedEventArgs renameArgs;
            SystemImageUpdatedEventArgs imageArgs;

            string msg;
            if ((renameArgs = args as ShellObjectRenamedEventArgs) != null)
            {
                msg = string.Format("{0}: {1} ==> {2}", changeType,
                    renameArgs.Path,
                    System.IO.Path.GetFileName(renameArgs.NewPath));

            }
            else if ((changeArgs = args as ShellObjectChangedEventArgs) != null)
            {
                msg = string.Format("{0}: {1}", changeType, changeArgs.Path);
            }
            else if ((imageArgs = args as SystemImageUpdatedEventArgs) != null)
            {
                msg = string.Format("{0}: ImageUpdated ==> {1}", changeType, imageArgs.ImageIndex);
            }
            else
            {
                msg = args.ChangeType.ToString();
            }

            return msg;
        }

        private void chkRecursive_Checked(object sender, RoutedEventArgs e)
        {
            if (_watcher != null && _watcher.Running)
            {
                StartWatcher(ShellObject.FromParsingName(txtPath.Text));
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
            }
        }

        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_watcher != null) { _watcher.Dispose(); }                        
        }


    }
}
