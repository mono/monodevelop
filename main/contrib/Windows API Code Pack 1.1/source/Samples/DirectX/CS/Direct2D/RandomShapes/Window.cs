// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

using System.Windows.Forms;

namespace RandomShapes
{
    public partial class Window : Form
    {
        public Window()
        {
            InitializeComponent();
        }

        private void Window_Load(object sender, EventArgs e)
        {
            d2DShapesControlWithButtons1.Initialize();
        }
    }
}
