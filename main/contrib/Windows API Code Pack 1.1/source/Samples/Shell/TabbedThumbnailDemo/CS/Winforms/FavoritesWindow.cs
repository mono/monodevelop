// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.IO;
using Microsoft.WindowsAPICodePack.Controls;

namespace Microsoft.WindowsAPICodePack.Samples.TabbedThumbnailDemo
{
    public partial class FavoritesWindow : Form
    {
        private Form1 parentForm = null;

        public FavoritesWindow(Form1 parent)
        {
            parentForm = parent;

            InitializeComponent();

            explorerBrowser1.NavigationOptions.PaneVisibility.AdvancedQuery = PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.Commands= PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.CommandsOrganize= PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.CommandsView= PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.Details = PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.Navigation= PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide;
            explorerBrowser1.NavigationOptions.PaneVisibility.Query= PaneVisibilityState.Hide;

            explorerBrowser1.ContentOptions.NoSubfolders = true;
            explorerBrowser1.ContentOptions.NoColumnHeader = true;
            explorerBrowser1.ContentOptions.NoHeaderInAllViews = true;

            explorerBrowser1.SelectionChanged += new EventHandler(explorerBrowser1_SelectionChanged);
            this.Load += new EventHandler(FavoritesWindow_Load);
        }

        void explorerBrowser1_SelectionChanged(object sender, EventArgs e)
        {
            if (explorerBrowser1.SelectedItems.Count > 0 && explorerBrowser1.SelectedItems[0] is ShellFile)
            {
                string path = ((ShellFile)explorerBrowser1.SelectedItems[0]).Path;

                if (Path.GetExtension(path).ToLower() == ".url")
                {
                    if (parentForm != null)
                        parentForm.Navigate(path);
                }
            }
        }

        void FavoritesWindow_Load(object sender, EventArgs e)
        {
            explorerBrowser1.ContentOptions.ViewMode = ExplorerBrowserViewMode.List;

            explorerBrowser1.Navigate((ShellObject)KnownFolders.Favorites);
        }
    }
}
