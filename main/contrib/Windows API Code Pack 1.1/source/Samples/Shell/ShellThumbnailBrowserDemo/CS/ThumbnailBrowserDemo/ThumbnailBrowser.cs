//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Controls;

namespace Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo
{
    public partial class ThumbnailBrowser : Form
    {
        /// <summary>
        /// Different views the picture browser supports
        /// </summary>
        private enum Views
        {
            Small,
            Medium,
            Large,
            ExtraLarge,
        }

        /// <summary>
        /// Preview mode (thumbnails or icons)
        /// </summary>
        private enum Mode
        {
            ThumbnailOrIcon,
            ThumbnailOnly,
            IconOnly,
        }

        /// <summary>
        /// Our current view (defaults to Views.Large)
        /// </summary>
        private Views currentView = Views.Large;

        /// <summary>
        /// Our current mode (defaults to Thumbnail view)
        /// </summary>
        private Mode currentMode = Mode.ThumbnailOrIcon;

        /// <summary>
        /// Our current ShellObject.
        /// </summary>
        private ShellObject currentItem = null;

        /// <summary>
        /// If the user checks the "do not show.." checkbox, then don't display
        /// the error dialog again.
        /// </summary>
        private bool showErrorTaskDialog = true;

        /// <summary>
        /// This is the state what we should be doing if the user gets the error.
        /// By default change the mode.
        /// </summary>
        private bool onErrorChangeMode = true;

        /// <summary>
        /// Task dialog to be shown to the user when error occurs.
        /// </summary>
        private TaskDialog td = null;

        public ThumbnailBrowser()
        {
            InitializeComponent();

            // Set some ExplorerBrowser properties
            explorerBrowser1.ContentOptions.SingleSelection = true;
            explorerBrowser1.ContentOptions.ViewMode = ExplorerBrowserViewMode.List;
            explorerBrowser1.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.CommandsView = PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.CommandsOrganize = PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.Commands = PaneVisibilityState.Hide;

            // set our initial state CurrentView == large
            toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.large;
            smallToolStripMenuItem.Checked = false;
            mediumToolStripMenuItem.Checked = false;
            largeToolStripMenuItem.Checked = true;
            extraLargeToolStripMenuItem.Checked = false;

            //
            comboBox1.SelectedIndex = 0;

            //
            explorerBrowser1.SelectionChanged += new EventHandler(explorerBrowser1_SelectionChanged);

            // Create our Task Dialog for displaying the error to the user
            // when they are asking for Thumbnail Only and the selected item doesn't have a thumbnail.
            td = new TaskDialog();
            td.OwnerWindowHandle = this.Handle;
            td.InstructionText = "Error displaying thumbnail";
            td.Text = "The selected item does not have a thumbnail and you have selected the viewing mode to be thumbnail only. Please select one of the following options:";
            td.StartupLocation = TaskDialogStartupLocation.CenterOwner;
            td.Icon = TaskDialogStandardIcon.Error;
            td.Cancelable = true;
            td.FooterCheckBoxText = "Do not show this dialog again";
            td.FooterCheckBoxChecked = false;

            TaskDialogCommandLink button1 = new TaskDialogCommandLink("changeModeButton", "Change mode to Thumbnail Or Icon",
                "Change the viewing mode to Thumbnail or Icon. If the selected item does not have a thumbnail, it's associated icon will be displayed.");
            button1.Click += new EventHandler(button1_Click);

            TaskDialogCommandLink button2 = new TaskDialogCommandLink("noChangeButton", "Keep the current mode",
                                    "Keep the currently selected mode (Thumbnail Only). If the current mode is Thumbnail Only and the selected item does not have a thumbnail, nothing will be shown in the preview panel.");
            button2.Click += new EventHandler(button2_Click);

            td.Controls.Add(button1);
            td.Controls.Add(button2);

        }

        void button1_Click(object sender, EventArgs e)
        {
            onErrorChangeMode = true;
            td.Close(TaskDialogResult.Ok);
        }

        void button2_Click(object sender, EventArgs e)
        {
            onErrorChangeMode = false;
            td.Close(TaskDialogResult.Ok);
        }

        void explorerBrowser1_SelectionChanged(object sender, EventArgs e)
        {
            if (this.explorerBrowser1.SelectedItems != null && this.explorerBrowser1.SelectedItems.Count == 1)
            {
                // Set our new current item
                currentItem = explorerBrowser1.SelectedItems[0];

                // Update preview
                UpdatePreview();
            }
        }

        /// <summary>
        /// Updates the thumbnail preview for currently selected item and current view
        /// </summary>
        private void UpdatePreview()
        {
            if (currentItem != null)
            {
                // Set the appropiate FormatOption
                if (currentMode == Mode.ThumbnailOrIcon)
                    currentItem.Thumbnail.FormatOption = ShellThumbnailFormatOption.Default;
                else if (currentMode == Mode.ThumbnailOnly)
                    currentItem.Thumbnail.FormatOption = ShellThumbnailFormatOption.ThumbnailOnly;
                else
                    currentItem.Thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;

                // Get the correct bitmap
                try
                {
                    if (currentView == Views.Small)
                        pictureBox1.Image = currentItem.Thumbnail.SmallBitmap;
                    else if (currentView == Views.Medium)
                        pictureBox1.Image = currentItem.Thumbnail.MediumBitmap;
                    else if (currentView == Views.Large)
                        pictureBox1.Image = currentItem.Thumbnail.LargeBitmap;
                    else if (currentView == Views.ExtraLarge)
                        pictureBox1.Image = currentItem.Thumbnail.ExtraLargeBitmap;
                }
                catch (NotSupportedException)
                {
                    TaskDialog tdThumbnailHandlerError = new TaskDialog();
                    tdThumbnailHandlerError.Caption = "Error getting the thumbnail";
                    tdThumbnailHandlerError.InstructionText = "The selected file does not have a valid thumbnail or thumbnail handler.";
                    tdThumbnailHandlerError.Icon = TaskDialogStandardIcon.Error;
                    tdThumbnailHandlerError.StandardButtons = TaskDialogStandardButtons.Ok;
                    tdThumbnailHandlerError.Show();
                }
                catch (InvalidOperationException)
                {
                    if (currentMode == Mode.ThumbnailOnly)
                    {
                        // If we get an InvalidOperationException and our mode is Mode.ThumbnailOnly,
                        // then we have a ShellItem that doesn't have a thumbnail (icon only).
                        // Let the user know this and if they want, change the mode.
                        if (showErrorTaskDialog)
                        {
                            TaskDialogResult tdr = td.Show();

                            showErrorTaskDialog = !td.FooterCheckBoxChecked.Value;
                        }

                        // If the user picked the first option, change the mode...
                        if (onErrorChangeMode)
                        {
                            // Change the mode to ThumbnailOrIcon
                            comboBox1.SelectedIndex = 0;
                            UpdatePreview();
                        }
                        else // else, ignore and display nothing.
                            pictureBox1.Image = null;
                    }
                    else
                        pictureBox1.Image = null;
                }
            }
            else
                pictureBox1.Image = null;
        }

        private void browseLocationButton_Click(object sender, EventArgs e)
        {
            // Create a new CommonOpenFileDialog to allow users to select a folder/library
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();

            // Set options to allow libraries and non filesystem items to be selected
            cfd.IsFolderPicker = true;
            cfd.AllowNonFileSystemItems = true;

            // Show the dialog
            CommonFileDialogResult result = cfd.ShowDialog();

            // if the user didn't cancel
            if (result == CommonFileDialogResult.Ok)
            {
                // Update the location on the ExplorerBrowser
                ShellObject resultItem = cfd.FileAsShellObject;
                explorerBrowser1.Navigate(resultItem);
            }
        }

        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            ToggleViews();
        }

        /// <summary>
        /// Toggle the different views for the thumbnail image.
        /// Includes: Small, Medium, Large (default), and Extra Large.
        /// </summary>
        private void ToggleViews()
        {
            // Toggle the views
            // Update our current view, as well as the image shown
            // on the "Views" menu.

            if (currentView == Views.Small)
            {
                currentView = Views.Medium;
                toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.medium;
                smallToolStripMenuItem.Checked = false;
                mediumToolStripMenuItem.Checked = true;
                largeToolStripMenuItem.Checked = false;
                extraLargeToolStripMenuItem.Checked = false;
            }
            else if (currentView == Views.Medium)
            {
                currentView = Views.Large;
                toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.large;
                smallToolStripMenuItem.Checked = false;
                mediumToolStripMenuItem.Checked = false;
                largeToolStripMenuItem.Checked = true;
                extraLargeToolStripMenuItem.Checked = false;
            }
            else if (currentView == Views.Large)
            {
                currentView = Views.ExtraLarge;
                toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.extralarge;
                smallToolStripMenuItem.Checked = false;
                mediumToolStripMenuItem.Checked = false;
                largeToolStripMenuItem.Checked = false;
                extraLargeToolStripMenuItem.Checked = true;
            }
            else if (currentView == Views.ExtraLarge)
            {
                currentView = Views.Small;
                toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.small;
                smallToolStripMenuItem.Checked = true;
                mediumToolStripMenuItem.Checked = false;
                largeToolStripMenuItem.Checked = false;
                extraLargeToolStripMenuItem.Checked = false;
            }

            // Update the image
            UpdatePreview();
        }

        private void smallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Update current view
            currentView = Views.Small;

            // Update the menu item states
            smallToolStripMenuItem.Checked = true;
            mediumToolStripMenuItem.Checked = false;
            largeToolStripMenuItem.Checked = false;
            extraLargeToolStripMenuItem.Checked = false;

            // Update the main splitbutton image
            toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.small;

            // Update the image
            UpdatePreview();
        }

        private void mediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Update current view
            currentView = Views.Medium;

            // Update the menu item states
            smallToolStripMenuItem.Checked = false;
            mediumToolStripMenuItem.Checked = true;
            largeToolStripMenuItem.Checked = false;
            extraLargeToolStripMenuItem.Checked = false;

            // Update the main splitbutton image
            toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.medium;

            // Update the image
            UpdatePreview();
        }

        private void largeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Update current view
            currentView = Views.Large;

            // Update the menu item states
            smallToolStripMenuItem.Checked = false;
            mediumToolStripMenuItem.Checked = false;
            largeToolStripMenuItem.Checked = true;
            extraLargeToolStripMenuItem.Checked = false;

            // Update the main splitbutton image
            toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.large;

            // Update the image
            UpdatePreview();
        }

        private void extraLargeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Update current view
            currentView = Views.ExtraLarge;

            // Update the menu item states
            smallToolStripMenuItem.Checked = false;
            mediumToolStripMenuItem.Checked = false;
            largeToolStripMenuItem.Checked = false;
            extraLargeToolStripMenuItem.Checked = true;

            // Update the main splitbutton image
            toolStripSplitButton1.Image = Microsoft.WindowsAPICodePack.Samples.ThumbnailBrowserDemo.Properties.Resources.extralarge;

            // Update the image
            UpdatePreview();

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
                currentMode = Mode.ThumbnailOrIcon;
            else if (comboBox1.SelectedIndex == 1)
                currentMode = Mode.ThumbnailOnly;
            else
                currentMode = Mode.IconOnly;

            UpdatePreview();
        }
    }
}
