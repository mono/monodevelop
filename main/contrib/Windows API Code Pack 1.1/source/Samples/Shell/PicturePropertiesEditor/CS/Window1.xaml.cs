// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Controls;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace Microsoft.WindowsAPICodePack.Samples.PicturePropertiesEditor
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private static string MultipleValuesText = "(Multiple values)";

        public Window1()
        {
            InitializeComponent();

            // Set the initial location for the explorer browser
            this.ExplorerBrowser1.NavigationTarget = (ShellObject)KnownFolders.SamplePictures;

            this.tabControl1.SelectionChanged += new SelectionChangedEventHandler(tabControl1_SelectionChanged);
            this.ResortBtn.Click += new RoutedEventHandler(ResortBtn_Click);
        }

        void ResortBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxThumbnails.Items.Count <= 1)
                return;

            // Anytime user tries to select an item,
            // rotate through the items...
            object obj = ListBoxThumbnails.Items.GetItemAt(ListBoxThumbnails.Items.Count - 1);

            if (obj != null)
            {
                ListBoxThumbnails.Items.RemoveAt(ListBoxThumbnails.Items.Count - 1);
                ListBoxThumbnails.Items.Insert(0, obj);
            }
        }

        void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.tabControl1.SelectedIndex == 1)
                SetupExplorerBrowser2();
        }

        private void SetupExplorerBrowser2()
        {
            ExplorerBrowser2.SingleSelection = false;
            ExplorerBrowser2.PreviewPane = PaneVisibilityState.Hide;
            ExplorerBrowser2.QueryPane = PaneVisibilityState.Hide;
            ExplorerBrowser2.NavigationPane = PaneVisibilityState.Hide;
            ExplorerBrowser2.CommandsOrganizePane = PaneVisibilityState.Hide;
            ExplorerBrowser2.CommandsViewPane = PaneVisibilityState.Hide;
            ExplorerBrowser2.DetailsPane = PaneVisibilityState.Hide;
            ExplorerBrowser2.NoHeaderInAllViews = true;
            ExplorerBrowser2.NoColumnHeader = true;
            //ExplorerBrowser2.NoSubfolders = true;
            ExplorerBrowser2.AdvancedQueryPane = PaneVisibilityState.Hide;
            ExplorerBrowser2.CommandsPane = PaneVisibilityState.Hide;
            ExplorerBrowser2.FullRowSelect = true;
            ExplorerBrowser2.ViewMode = ExplorerBrowserViewMode.Tile;

            ExplorerBrowser2.ExplorerBrowserControl.SelectionChanged += new EventHandler(ExplorerBrowserControl_SelectionChanged);

            ExplorerBrowser2.NavigationTarget = (ShellObject)KnownFolders.SamplePictures;
        }

        void ExplorerBrowserControl_SelectionChanged(object sender, EventArgs e)
        {
            // When the user selects items from the explorer browser control, update the various properties
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            if (ExplorerBrowser2.SelectedItems.Count > 1) // multiple items
            {
                // Thumbnail
                ListBoxThumbnails.Items.Clear();

                foreach (ShellObject so in ExplorerBrowser2.SelectedItems)
                {
                    Image img = new Image();
                    img.Height = 200;
                    img.Width = 200;
                    img.Source = so.Thumbnail.LargeBitmapSource;

                    ListBoxThumbnails.Items.Add(img);
                }

                #region Description properties

                // Title
                TextBoxTitle.Text = GetPropertyValue("System.Title") as string;

                // Subject
                TextBoxSubject.Text = GetPropertyValue("System.Subject") as string;

                // Rating
                UInt32? rating = GetPropertyValue("System.SimpleRating") as UInt32?;
                RatingValueControl.RatingValue = rating.HasValue ? Convert.ToInt32(rating.Value) : 0;

                // Tags / Keywords
                // TODO - We could probably loop through the tags for each file and if they are same,
                // display them. For now, just treating them all being different values
                ListBoxTags.Items.Clear();
                ListBoxTags.Items.Add(MultipleValuesText);

                // Comments
                TextBoxComments.Text = GetPropertyValue("System.Comment") as string;

                #endregion

                #region Origin Properties

                // Authors
                // TODO - We could probably loop through the tags for each file and if they are same,
                // display them. For now, just treating them all being different values
                ListBoxAuthors.ItemsSource = new string[] { MultipleValuesText };

                // Date Taken
                TextBoxDateTaken.Text = GetPropertyValue("System.Photo.DateTaken") as string;

                // Date Acquired
                TextBoxDateAcquired.Text = GetPropertyValue("System.DateAcquired") as string;

                // Copyright
                TextBoxCopyright.Text = GetPropertyValue("System.Copyright") as string;

                #endregion

                #region Image Properties

                // Dimensions
                TextBoxDimensions.Text = GetPropertyValue("System.Image.Dimensions") as string;

                // Horizontal Resolution
                TextBoxHorizontalResolution.Text = GetPropertyValue("System.Image.HorizontalResolution").ToString();

                // Vertical Resolution 
                TextBoxVerticalResolution.Text = GetPropertyValue("System.Image.VerticalResolution").ToString();

                // Bit Depth
                TextBoxBitDepth.Text = GetPropertyValue("System.Image.BitDepth").ToString();

                #endregion

            }
            else if (ExplorerBrowser2.SelectedItems.Count == 1) // only one item
            {
                ShellObject so = ExplorerBrowser2.SelectedItems[0];

                // Thumbnail
                ListBoxThumbnails.Items.Clear();
                Image img = new Image();
                img.Height = img.Width = 200;
                img.Stretch = Stretch.Fill;
                img.Source = so.Thumbnail.LargeBitmapSource;
                ListBoxThumbnails.Items.Add(img);

                #region Description properties

                // Title
                TextBoxTitle.Text = so.Properties.System.Title.Value;

                // Subject
                TextBoxSubject.Text = so.Properties.System.Subject.Value;

                // Rating
                RatingValueControl.RatingValue = so.Properties.System.SimpleRating.Value.HasValue ? (int)so.Properties.System.SimpleRating.Value : 0;

                // Tags / Keywords
                ListBoxTags.Items.Clear();
                if(so.Properties.System.Keywords.Value != null)
                    foreach (string tag in so.Properties.System.Keywords.Value)
                        ListBoxTags.Items.Add(tag);

                // Comments
                TextBoxComments.Text = so.Properties.System.Comment.Value;

                #endregion

                #region Origin Properties

                // Authors
                ListBoxAuthors.ItemsSource = so.Properties.System.Author.Value;

                // Date Taken
                TextBoxDateTaken.Text = so.Properties.System.Photo.DateTaken.Value.HasValue ? so.Properties.System.Photo.DateTaken.Value.Value.ToShortDateString() : "";

                // Date Acquired
                TextBoxDateAcquired.Text = so.Properties.System.DateAcquired.Value.HasValue ? so.Properties.System.DateAcquired.Value.Value.ToShortDateString() : "";

                // Copyright
                TextBoxCopyright.Text = so.Properties.System.Copyright.Value;

                #endregion

                #region Image Properties

                // Dimensions
                TextBoxDimensions.Text = so.Properties.System.Image.Dimensions.Value;

                // Horizontal Resolution
                TextBoxHorizontalResolution.Text = so.Properties.System.Image.HorizontalResolution.Value.ToString();

                // Vertical Resolution 
                TextBoxVerticalResolution.Text = so.Properties.System.Image.VerticalResolution.Value.ToString();

                // Bit Depth
                TextBoxBitDepth.Text = so.Properties.System.Image.BitDepth.Value.ToString();

                #endregion
            }
        }

        private object GetPropertyValue(string property)
        {
            object returnValue = null;

            foreach (ShellObject so in ExplorerBrowser2.SelectedItems)
            {
                object objValue = so.Properties.GetProperty(property).ValueAsObject;

                if (returnValue == null)
                    returnValue = objValue;
                else if (objValue == null || (returnValue.ToString() != objValue.ToString()))
                {
                    returnValue = MultipleValuesText;
                    break;  // if the values differ, than break and use the multiple values text;
                }
            }

            return returnValue;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            // reset (cancel the user's changes)
            UpdateProperties();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            // Save
            // depending on what we have selected ...
            if (ExplorerBrowser2.SelectedItems.Count >= 1)
            {
                foreach (ShellObject so in ExplorerBrowser2.SelectedItems)
                {
                    SaveProperties(so);
                }
            }
        }

        private void SaveProperties(ShellObject so)
        {
            // Get the property writer for each file
            using (ShellPropertyWriter sw = so.Properties.GetPropertyWriter())
            {
                // Write the same set of property values for each file, since the user has selected
                // multiple files.
                // ignore the ones that aren't changed...

                #region Description Properties

                // Title
                if (TextBoxTitle.Text != MultipleValuesText)
                    sw.WriteProperty(so.Properties.System.Title, !string.IsNullOrEmpty(TextBoxTitle.Text) ? TextBoxTitle.Text : null);

                // Subject
                if (TextBoxSubject.Text != MultipleValuesText)
                    sw.WriteProperty(so.Properties.System.Subject, !string.IsNullOrEmpty(TextBoxSubject.Text) ? TextBoxSubject.Text : null);

                // Rating
                if (RatingValueControl.RatingValue != 0)
                    sw.WriteProperty(so.Properties.System.SimpleRating, Convert.ToUInt32(RatingValueControl.RatingValue));

                // Tags / Keywords
                // read-only property for now

                // Comments
                if (TextBoxComments.Text != MultipleValuesText)
                    sw.WriteProperty(so.Properties.System.Comment, !string.IsNullOrEmpty(TextBoxComments.Text) ? TextBoxComments.Text : null);

                #endregion

                #region Origin Properties

                // Authors
                // read-only property for now

                // Date Taken
                // read-only property for now

                // Date Acquired
                // read-only property for now

                // Copyright
                if (TextBoxCopyright.Text != MultipleValuesText)
                    sw.WriteProperty(so.Properties.System.Copyright, !string.IsNullOrEmpty(TextBoxCopyright.Text) ? TextBoxCopyright.Text : null);

                #endregion

                #region Image Properties

                // Dimensions
                // Read-only property

                // Horizontal Resolution
                // Read-only property

                // Vertical Resolution 
                // Read-only property

                // Bit Depth
                // Read-only property

                #endregion

            }
        }
    }
}
