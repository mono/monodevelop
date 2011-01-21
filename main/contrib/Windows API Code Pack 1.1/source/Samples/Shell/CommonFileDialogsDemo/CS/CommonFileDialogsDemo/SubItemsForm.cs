// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.WindowsAPICodePack.Samples.ShellObjectCFDBrowser
{
    public partial class SubItemsForm : Form
    {
        private int itemsCount = 0;
        public SubItemsForm()
        {
            InitializeComponent();

            listView1.LargeImageList = imageList1;
        }

        public void AddItem(string name, Image image)
        {
            imageList1.Images.Add(image);
            listView1.Items.Add(new ListViewItem(name, itemsCount));
            itemsCount++;
        }
    }
}
