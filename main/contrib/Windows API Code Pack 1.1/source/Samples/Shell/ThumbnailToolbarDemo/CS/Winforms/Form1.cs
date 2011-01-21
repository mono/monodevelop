//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Microsoft.WindowsAPICodePack.Samples.ImageViewerDemoWinforms
{
    public partial class Form1 : Form
    {
        private ThumbnailToolBarButton buttonPrevious;
        private ThumbnailToolBarButton buttonNext;
        private ThumbnailToolBarButton buttonFirst;
        private ThumbnailToolBarButton buttonLast;
        private List<ListViewItem> picturesList;
        private int imgListCount = 0;
        private ImageList imgList = null;

        public Form1()
        {
            InitializeComponent();
            listView1.MultiSelect = false;

            InitListView();

            this.Shown += new System.EventHandler(Form1_Shown);

            //
            toolStrip1.ImageList = imageList1;
            toolStrip1.ImageScalingSize = new Size(32, 32);
            toolStripButtonFirst.ImageIndex = 0;
            toolStripButtonPrevious.ImageIndex = 1;
            toolStripButtonNext.ImageIndex = 2;
            toolStripButtonLast.ImageIndex = 3;

        }

        void Form1_Shown(object sender, System.EventArgs e)
        {
            listView1.SelectedIndexChanged += new System.EventHandler(listView1_SelectedIndexChanged);

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

            TaskbarManager.Instance.ThumbnailToolBars.AddButtons(this.Handle, buttonFirst, buttonPrevious, buttonNext, buttonLast);

            if (listView1.Items.Count > 0)
                listView1.Items[0].Selected = true;

            //
            TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip(this.Handle, new Rectangle(pictureBox1.Location, pictureBox1.Size));
        }

        void buttonPrevious_Click(object sender, EventArgs e)
        {
            int newIndex = listView1.SelectedIndices[0] - 1;

            if (newIndex > -1)
            {
                listView1.Items[newIndex].Selected = true;
                listView1.Items[newIndex].EnsureVisible();
            }

            listView1.Focus();
        }

        void buttonNext_Click(object sender, EventArgs e)
        {
            int newIndex = listView1.SelectedIndices[0] + 1;

            if (newIndex < listView1.Items.Count)
            {
                listView1.Items[newIndex].Selected = true;
                listView1.Items[newIndex].EnsureVisible();
            }

            listView1.Focus();
        }

        void buttonFirst_Click(object sender, EventArgs e)
        {
            listView1.Items[0].Selected = true;
            listView1.Items[0].EnsureVisible();
            listView1.Focus();
        }

        void buttonLast_Click(object sender, EventArgs e)
        {
            listView1.Items[listView1.Items.Count - 1].Selected = true;
            listView1.Items[listView1.Items.Count - 1].EnsureVisible();
            listView1.Focus();
        }                

        void listView1_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            // Update the picture
            if (listView1.SelectedItems.Count > 0)
                pictureBox1.Image = Image.FromFile(((ListViewItem)listView1.SelectedItems[0]).Tag.ToString());

            // Update the button states
            if (listView1.SelectedIndices.Count > 0 && listView1.SelectedIndices[0] == 0)
            {
                buttonFirst.Enabled = false;
                toolStripButtonFirst.Enabled = false;
                buttonPrevious.Enabled = false;
                toolStripButtonPrevious.Enabled = false;
            }
            else if (listView1.SelectedIndices.Count > 0 && listView1.SelectedIndices[0] > 0)
            {
                buttonFirst.Enabled = true;
                toolStripButtonFirst.Enabled = true;
                buttonPrevious.Enabled = true;
                toolStripButtonPrevious.Enabled = true;
            }

            if (listView1.SelectedIndices.Count > 0 && listView1.SelectedIndices[0] == listView1.Items.Count - 1)
            {
                buttonLast.Enabled = false;
                toolStripButtonLast.Enabled = false;
                buttonNext.Enabled = false;
                toolStripButtonNext.Enabled = false;
            }
            else if (listView1.SelectedIndices.Count > 0 && listView1.SelectedIndices[0] < listView1.Items.Count - 1)
            {
                buttonLast.Enabled = true;
                toolStripButtonLast.Enabled = true;
                buttonNext.Enabled = true;
                toolStripButtonNext.Enabled = true;
            }
        }

        private void InitListView()
        {
            imgList = new ImageList();
            imgList.ImageSize = new Size(96, 96);
            imgList.ColorDepth = ColorDepth.Depth32Bit;

            listView1.LargeImageList = imgList;

            ShellContainer pics = (ShellContainer)KnownFolders.Pictures;

            if (ShellLibrary.IsPlatformSupported)
                pics = (ShellContainer)KnownFolders.PicturesLibrary;

            if (picturesList == null)
                picturesList = new List<ListViewItem>();
            else
                picturesList.Clear();

            // Recursively get the pictures
            GetPictures(pics);
                        
            if (picturesList.Count == 0)
            {
                if (pics is ShellLibrary)
                    TaskDialog.Show("Please add some pictures to the library", "Pictures library is empty", "No pictures found");
                else
                    TaskDialog.Show("Please add some pictures to your pictures folder", "Pictures folder is empty", "No pictures found");
            }

            listView1.Items.AddRange(picturesList.ToArray());
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
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = sf.Name;
                    item.ImageIndex = imgListCount;
                    item.Tag = sf.Path;
                    imgList.Images.Add(Image.FromFile(sf.Path));

                    picturesList.Add(item);
                    imgListCount++;
                }
            }

            // Then recurse into each subfolder
            foreach (ShellContainer subFolder in folder.OfType<ShellContainer>())
                GetPictures(subFolder);
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip(this.Handle, new Rectangle(pictureBox1.Location, pictureBox1.Size));
        }
    }
}
