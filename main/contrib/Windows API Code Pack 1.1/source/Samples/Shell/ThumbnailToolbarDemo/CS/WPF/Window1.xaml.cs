//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace Microsoft.WindowsAPICodePack.Samples.ImageViewerDemo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private ThumbnailToolBarButton buttonPrevious;
        private ThumbnailToolBarButton buttonNext;
        private ThumbnailToolBarButton buttonFirst;
        private ThumbnailToolBarButton buttonLast;
        private List<ShellFile> picturesList;

        public Window1()
        {
            if (!TaskbarManager.IsPlatformSupported)
            {
                MessageBox.Show("This demo application interacts with the Windows 7 Taskbar. The current operating system does not support this feature.");
                Application.Current.Shutdown();
            }

            InitializeComponent();
            DataContext = this;

            ImageList.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(ImageList_SelectionChanged);
            this.Loaded += new RoutedEventHandler(Window1_Loaded);

            // When the LayoutUpdated event is raised, we are sure that the picturebox is rendered
            // (i.e. we'll be able to get the height and width of that control)
            pictureBox1.LayoutUpdated += new EventHandler(pictureBox1_LayoutUpdated);
        }

        void pictureBox1_LayoutUpdated(object sender, EventArgs e)
        {
            // On LayoutUpdated, get the offset of the pictureBox with repsect to its parent.
            // Form a clip rectangle (offset + size of the control) and pass it to Taskbar
            // for DWM to clip only the specific porition of the app window. 
            // This allows us to not include the "misc" controls from the app window - scroll bars, 
            // list view on the right, any toolbars, etc.

            // Get the offset for picturebox
            Vector v = VisualTreeHelper.GetOffset(pictureBox1);

            // Set the thumbnail clip
            TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip((new WindowInteropHelper(this)).Handle, new System.Drawing.Rectangle((int)v.X, (int)v.Y, (int)pictureBox1.RenderSize.Width, (int)pictureBox1.RenderSize.Height));
        }

        void ImageList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Update the button states
            if (ImageList.SelectedIndex == 0)
            {
                buttonFirst.Enabled = false;
                buttonPrevious.Enabled = false;
            }
            else if (ImageList.SelectedIndex > 0)
            {
                buttonFirst.Enabled = true;
                buttonPrevious.Enabled = true;
            }

            if (ImageList.SelectedIndex == ImageList.Items.Count - 1)
            {
                buttonLast.Enabled = false;
                buttonNext.Enabled = false;
            }
            else if (ImageList.SelectedIndex < ImageList.Items.Count - 1)
            {
                buttonLast.Enabled = true;
                buttonNext.Enabled = true;
            }
        }

        void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            buttonFirst = new ThumbnailToolBarButton(Properties.Resources.first, "First Image");
            buttonFirst.Enabled = false;
            buttonFirst.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(buttonFirst_Click);

            buttonPrevious = new ThumbnailToolBarButton(Properties.Resources.prevArrow, "Previous Image");
            buttonPrevious.Enabled = false;
            buttonPrevious.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(buttonPrevious_Click);

            buttonNext = new ThumbnailToolBarButton(Properties.Resources.nextArrow, "Next Image");
            buttonNext.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(buttonNext_Click);

            buttonLast = new ThumbnailToolBarButton(Properties.Resources.last, "Last Image");
            buttonLast.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(buttonLast_Click);

            TaskbarManager.Instance.ThumbnailToolBars.AddButtons(new WindowInteropHelper(this).Handle, buttonFirst, buttonPrevious, buttonNext, buttonLast);

            // Set our selection
            ImageList.SelectedIndex = 0;
            ImageList.Focus();

            if (ImageList.SelectedItem != null)
                ImageList.ScrollIntoView(ImageList.SelectedItem);
        }

        void buttonPrevious_Click(object sender, EventArgs e)
        {
            int newIndex = ImageList.SelectedIndex - 1;

            if (newIndex > -1)
                ImageList.SelectedIndex = newIndex;

            ImageList.Focus();

            if (ImageList.SelectedItem != null)
                ImageList.ScrollIntoView(ImageList.SelectedItem);
        }

        void buttonNext_Click(object sender, EventArgs e)
        {
            int newIndex = ImageList.SelectedIndex + 1;

            if (newIndex < ImageList.Items.Count)
                ImageList.SelectedIndex = newIndex;

            ImageList.Focus();

            if (ImageList.SelectedItem != null)
                ImageList.ScrollIntoView(ImageList.SelectedItem);
        }

        void buttonFirst_Click(object sender, EventArgs e)
        {
            ImageList.SelectedIndex = 0;
            ImageList.Focus();

            if (ImageList.SelectedItem != null)
                ImageList.ScrollIntoView(ImageList.SelectedItem);
        }

        void buttonLast_Click(object sender, EventArgs e)
        {
            ImageList.SelectedIndex = ImageList.Items.Count - 1;
            ImageList.Focus();

            if (ImageList.SelectedItem != null)
                ImageList.ScrollIntoView(ImageList.SelectedItem);
        }

        public class MyImage
        {
            public MyImage(ImageSource sourceImage, string imageName)
            {
                Image = sourceImage;
                Name = imageName;
            }

            public override string ToString()
            {
                return Name;
            }

            public ImageSource Image { get; set; }
            public string Name { get; set; }
        }

        public List<ShellFile> AllImages
        {
            get
            {
                ShellContainer pics = (ShellContainer)KnownFolders.Pictures;

                if (ShellLibrary.IsPlatformSupported)
                    pics = (ShellContainer)KnownFolders.PicturesLibrary;

                if (picturesList == null)
                    picturesList = new List<ShellFile>();
                else
                    picturesList.Clear();

                // Recursively get the pictures
                GetPictures(pics);

                if (picturesList.Count == 0)
                {
                    if (pics is ShellLibrary)
                        TaskDialog.Show("Pictures library is empty", "Please add some pictures to the library", "No pictures found");
                    else
                        TaskDialog.Show("Pictures folder is empty", "Please add some pictures to your pictures folder", "No pictures found");
                }

                return picturesList;
            }
        }

        private void GetPictures(ShellContainer folder)
        {
            // Just for demo purposes, stop at 20 pics
            if (picturesList.Count >= 20)
                return;

            // First get the pictures in this folder
            foreach (ShellFile sf in folder.OfType<ShellFile>())
            {
                string ext = Path.GetExtension(sf.Path).ToLower();

                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp")
                    picturesList.Add(sf);
            }

            // Then recurse into each subfolder
            foreach (ShellContainer subFolder in folder.OfType<ShellContainer>())
                GetPictures(subFolder);
        }
    }
}