//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace Microsoft.WindowsAPICodePack.Samples.TabbedThumbnailDemo
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Keeping track of the previously selected tab,
        /// so we can capture it's bitmap when the user selects another tab.
        /// Unfortunately, we cannot access the previously selected tab via the 
        /// "selecting" event from TabControl or use any of its properties.
        /// This seems to be the best way - keep track ourselves.
        /// </summary>
        private TabPage previousSelectedPage = null;

        //
        private ThumbnailToolBarButton thumbButtonBack;
        private ThumbnailToolBarButton thumbButtonForward;
        private ThumbnailToolBarButton thumbButtonRefresh;

        private ThumbnailToolBarButton thumbButtonCut;
        private ThumbnailToolBarButton thumbButtonCopy;
        private ThumbnailToolBarButton thumbButtonPaste;
        private ThumbnailToolBarButton thumbButtonSelectAll;

        /// <summary>
        /// Internal bool to keep track of the scroll event that is on the HTML Document's Window class.
        /// We don't get a document or a window until we have a page loaded. This bool will be set once we 
        /// navigate. It will be reset once we get add the scroll event...
        /// </summary>
        private bool scrollEventAdded = false;

        /// <summary>
        /// Reference to our window for displaying the favorite links
        /// </summary>
        private FavoritesWindow favsWindow = null;

        public Form1()
        {
            InitializeComponent();

            // Listen for specific events on the tab control
            tabControl1.Selecting += new TabControlCancelEventHandler(tabControl1_Selecting);
            tabControl1.SelectedIndexChanged += new EventHandler(tabControl1_SelectedIndexChanged);

            // When the size of our form changes, invalidate the thumbnails so we can capture them again
            // when user requests a peek or thumbnail preview.
            this.SizeChanged += new EventHandler(Form1_SizeChanged);

            // Set our minimum size so the form will not have 0 height/width when user tries to resize it all the way
            this.MinimumSize = new Size(500, 100);

            // Show the Favorites window
            favsWindow = new FavoritesWindow(this);
            favsWindow.Show();

            // Create our Thumbnail toolbar buttons for the Browser doc
            thumbButtonBack = new ThumbnailToolBarButton(Properties.Resources.prevArrow, "Back");
            thumbButtonBack.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(thumbButtonBack_Click);

            thumbButtonForward = new ThumbnailToolBarButton(Properties.Resources.nextArrow, "Forward");
            thumbButtonForward.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(thumbButtonForward_Click);

            thumbButtonRefresh = new ThumbnailToolBarButton(Properties.Resources.refresh, "Refresh");
            thumbButtonRefresh.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(thumbButtonRefresh_Click);

            // Create our thumbnail toolbar buttons for the RichTextBox doc
            thumbButtonCut = new ThumbnailToolBarButton(Properties.Resources.cut, "Cut");
            thumbButtonCut.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(thumbButtonCut_Click);

            thumbButtonCopy = new ThumbnailToolBarButton(Properties.Resources.copy, "Copy");
            thumbButtonCopy.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(thumbButtonCopy_Click);

            thumbButtonPaste = new ThumbnailToolBarButton(Properties.Resources.paste, "Paste");
            thumbButtonPaste.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(thumbButtonPaste_Click);

            thumbButtonSelectAll = new ThumbnailToolBarButton(Properties.Resources.selectAll, "SelectAll");
            thumbButtonSelectAll.Click += new EventHandler<ThumbnailButtonClickedEventArgs>(thumbButtonSelectAll_Click);

            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }

        bool cancelFormClosing = false;

        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If the user is closing the app, ask them if they wish to close the current tab
            // or all the tabs

            if (tabControl1 != null && tabControl1.TabPages.Count > 0)
            {
                if (tabControl1.TabPages.Count <= 1)
                {
                    // close the tab and the application
                    cancelFormClosing = false;
                }
                else
                {
                    // More than 1 tab.... show the user the TaskDialog
                    TaskDialog tdClose = new TaskDialog();
                    tdClose.Caption = "Tabbed Thumbnail demo (Winforms)";
                    tdClose.InstructionText = "Do you want to close all the tabs or the current tab?";
                    tdClose.Cancelable = true;
                    tdClose.OwnerWindowHandle = this.Handle;

                    TaskDialogButton closeAllTabsButton = new TaskDialogButton("closeAllTabsButton", "Close all tabs");
                    closeAllTabsButton.Default = true;
                    closeAllTabsButton.Click += new EventHandler(closeAllTabsButton_Click);
                    tdClose.Controls.Add(closeAllTabsButton);

                    TaskDialogButton closeCurrentTabButton = new TaskDialogButton("closeCurrentTabButton", "Close current tab");
                    closeCurrentTabButton.Click += new EventHandler(closeCurrentTabButton_Click);
                    tdClose.Controls.Add(closeCurrentTabButton);

                    tdClose.Show();
                }
            }

            e.Cancel = cancelFormClosing;
        }

        void closeCurrentTabButton_Click(object sender, EventArgs e)
        {
            button2_Click(this, EventArgs.Empty);
            cancelFormClosing = true;
        }

        void closeAllTabsButton_Click(object sender, EventArgs e)
        {
            cancelFormClosing = false;
        }

        void Form1_SizeChanged(object sender, EventArgs e)
        {
            // If we are in minimized state, don't invalidate the thumbnail as we want to keep the 
            // cached image. Minimized forms can't be captured.
            if (WindowState != FormWindowState.Minimized)
            {
                // Just invalidate the selected tab's thumbnail so we can recapture them when requested
                if (tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
                    TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tabControl1.SelectedTab).InvalidatePreview();

            }
        }

        private TabPage FindTab(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return null;

            foreach (TabPage page in tabControl1.TabPages)
            {
                if (page.Handle == handle)
                    return page;
            }

            return null;
        }

        void thumbButtonBack_Click(object sender, ThumbnailButtonClickedEventArgs e)
        {
            TabPage page = FindTab(e.WindowHandle);

            if (page != null && page.Controls[0] is WebBrowser)
                ((WebBrowser)page.Controls[0]).GoBack();
        }

        void thumbButtonForward_Click(object sender, ThumbnailButtonClickedEventArgs e)
        {
            TabPage page = FindTab(e.WindowHandle);

            if (page != null && page.Controls[0] is WebBrowser)
                ((WebBrowser)page.Controls[0]).GoForward();
        }


        void thumbButtonRefresh_Click(object sender, ThumbnailButtonClickedEventArgs e)
        {
            TabPage page = FindTab(e.WindowHandle);

            if (page != null && page.Controls[0] is WebBrowser)
                ((WebBrowser)page.Controls[0]).Refresh();
        }


        void thumbButtonCut_Click(object sender, ThumbnailButtonClickedEventArgs e)
        {
            TabPage page = FindTab(e.WindowHandle);

            if (page != null && page.Controls[0] is RichTextBox)
            {
                ((RichTextBox)page.Controls[0]).Cut();

                // If there is a selected tab, take it's screenshot
                // invalidate the tab's thumbnail
                // update the "preview" object with the new thumbnail
                if (tabControl1.Size != Size.Empty && tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
                    UpdatePreviewBitmap(tabControl1.SelectedTab);
            }
        }

        void thumbButtonCopy_Click(object sender, ThumbnailButtonClickedEventArgs e)
        {
            TabPage page = FindTab(e.WindowHandle);

            if (page != null && page.Controls[0] is RichTextBox)
            {
                ((RichTextBox)page.Controls[0]).Copy();

                // If there is a selected tab, take its screenshot
                // invalidate the tab's thumbnail
                // update the "preview" object with the new thumbnail
                if (tabControl1.Size != Size.Empty && tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
                    UpdatePreviewBitmap(tabControl1.SelectedTab);
            }
        }

        void thumbButtonPaste_Click(object sender, ThumbnailButtonClickedEventArgs e)
        {
            TabPage page = FindTab(e.WindowHandle);

            if (page != null && page.Controls[0] is RichTextBox)
            {
                ((RichTextBox)page.Controls[0]).Paste();

                // If there is a selected tab, take it's screenshot
                // invalidate the tab's thumbnail
                // update the "preview" object with the new thumbnail
                if (tabControl1.Size != Size.Empty && tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
                    UpdatePreviewBitmap(tabControl1.SelectedTab);
            }
        }

        void thumbButtonSelectAll_Click(object sender, ThumbnailButtonClickedEventArgs e)
        {
            TabPage page = FindTab(e.WindowHandle);

            if (page != null && page.Controls[0] is RichTextBox)
                ((RichTextBox)page.Controls[0]).SelectAll();
        }

        void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // Before selecting,
            // If there is a selected tab, take it's screenshot
            // invalidate the tab's thumbnail
            // update the "preview" object with the new thumbnail
            if (tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
                UpdatePreviewBitmap(previousSelectedPage);

            // update our selected tab
            previousSelectedPage = tabControl1.SelectedTab;
        }

        /// <summary>
        /// Helper method to update the thumbnail preview for a given tab page.
        /// </summary>
        /// <param name="tabPage"></param>
        private void UpdatePreviewBitmap(TabPage tabPage)
        {
            if (tabPage != null)
            {
                TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tabPage);

                if (preview != null)
                {
                    Bitmap bitmap = TabbedThumbnailScreenCapture.GrabWindowBitmap(tabPage.Handle, tabPage.Size);
                    preview.SetImage(bitmap);

                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                        bitmap = null;
                    }
                }
            }
        }

        void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Make sure we let the Taskbar know about the active/selected tab
            // Tabbed thumbnails need to be updated to indicate which one is currently selected
            if (tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
            {
                TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tabControl1.SelectedTab);

                if (tabControl1.SelectedTab.Controls[0] is RichTextBox)
                    button4.Enabled = true;
                else
                    button4.Enabled = false;
            }
        }

        void Window_Scroll(object sender, HtmlElementEventArgs e)
        {
            // If there is a selected tab, take it's screenshot
            // invalidate the tab's thumbnail
            // update the "preview" object with the new thumbnail
            if (tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
                UpdatePreviewBitmap(tabControl1.SelectedTab);
        }

        void wb_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            // Update the combobox / addressbar 
            comboBox1.Text = ((WebBrowser)sender).Document.Url.ToString();

            if (!scrollEventAdded)
            {
                ((WebBrowser)sender).Document.Window.Scroll += new HtmlElementEventHandler(Window_Scroll);
                scrollEventAdded = true;
            }
        }

        /// <summary>
        /// Create a new tab, add a webbrowser and navigate the given address/URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void button1_Click(object sender, System.EventArgs args)
        {
            TabPage newTab = new TabPage(comboBox1.Text);
            tabControl1.TabPages.Add(newTab);
            WebBrowser wb = new WebBrowser();
            wb.DocumentTitleChanged += new EventHandler(wb_DocumentTitleChanged);
            wb.Navigated += new WebBrowserNavigatedEventHandler(wb_Navigated);
            wb.ProgressChanged += new WebBrowserProgressChangedEventHandler(wb_ProgressChanged);
            wb.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(wb_DocumentCompleted);
            wb.Dock = DockStyle.Fill;
            wb.Navigate(comboBox1.Text);
            newTab.Controls.Add(wb);

            // Add thumbnail toolbar buttons
            TaskbarManager.Instance.ThumbnailToolBars.AddButtons(newTab.Handle, thumbButtonBack, thumbButtonForward, thumbButtonRefresh);

            // Add a new preview
            TabbedThumbnail preview = new TabbedThumbnail(this.Handle, newTab.Handle);

            // Event handlers for this preview
            preview.TabbedThumbnailActivated += preview_TabbedThumbnailActivated;
            preview.TabbedThumbnailClosed += preview_TabbedThumbnailClosed;
            preview.TabbedThumbnailMaximized += preview_TabbedThumbnailMaximized;
            preview.TabbedThumbnailMinimized += preview_TabbedThumbnailMinimized;

            TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(preview);

            // Select the tab in the application UI as well as taskbar tabbed thumbnail list
            tabControl1.SelectedTab = newTab;
            TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tabControl1.SelectedTab);

            // set false for this new webbrowser
            scrollEventAdded = false;

            //
            button2.Enabled = true;
        }

        void wb_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (tabControl1.Size != Size.Empty && tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
            {
                UpdatePreviewBitmap(tabControl1.SelectedTab);
            }
        }

        void wb_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            // Based on the webbrowser's progress, update our statusbar progressbar
            if (e.CurrentProgress >= 0)
            {
                toolStripProgressBar1.Maximum = (int)e.MaximumProgress;
                toolStripProgressBar1.Value = (int)Math.Max(e.CurrentProgress, e.MaximumProgress);
            }
        }

        void wb_DocumentTitleChanged(object sender, System.EventArgs e)
        {
            // When the webpage's title changes,
            // update the tab's title and taskbar thumbnail's title
            TabPage page = ((WebBrowser)sender).Parent as TabPage;

            if (page != null)
            {
                page.Text = ((WebBrowser)sender).DocumentTitle;

                TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(page);

                if (preview != null)
                    preview.Title = page.Text;

            }
        }

        /// <summary>
        /// Close button - close the specific tab and also
        /// remove the thumbnail preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != null)
            {
                TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tabControl1.SelectedTab);
                tabControl1.TabPages.Remove(tabControl1.SelectedTab);
            }

            if (tabControl1.TabPages.Count == 0)
                button2.Enabled = false;
        }

        /// <summary>
        /// Open a user-specified text file in a new tab (using a RichTextBox)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            // Open text file
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();

            CommonFileDialogStandardFilters.TextFiles.ShowExtensions = true;
            CommonFileDialogFilter rtfFilter = new CommonFileDialogFilter("RTF Files", ".rtf");
            rtfFilter.ShowExtensions = true;

            cfd.Filters.Add(CommonFileDialogStandardFilters.TextFiles);
            cfd.Filters.Add(rtfFilter);

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TabPage newTab = new TabPage(Path.GetFileName(cfd.FileName));
                tabControl1.TabPages.Add(newTab);
                RichTextBox rtbText = new RichTextBox();
                rtbText.KeyDown += new KeyEventHandler(rtbText_KeyDown);
                rtbText.MouseMove += new MouseEventHandler(rtbText_MouseMove);
                rtbText.KeyUp += new KeyEventHandler(rtbText_KeyUp);
                rtbText.Dock = DockStyle.Fill;

                // Based on the extension, load the file appropriately in the RichTextBox
                if (Path.GetExtension(cfd.FileName).ToLower() == ".txt")
                    rtbText.LoadFile(cfd.FileName, RichTextBoxStreamType.PlainText);
                else if (Path.GetExtension(cfd.FileName).ToLower() == ".rtf")
                    rtbText.LoadFile(cfd.FileName, RichTextBoxStreamType.RichText);

                // Update the tab
                newTab.Controls.Add(rtbText);

                // Add a new preview
                TabbedThumbnail preview = new TabbedThumbnail(this.Handle, newTab.Handle);

                // Event handlers for this preview
                preview.TabbedThumbnailActivated += preview_TabbedThumbnailActivated;
                preview.TabbedThumbnailClosed += preview_TabbedThumbnailClosed;
                preview.TabbedThumbnailMaximized += preview_TabbedThumbnailMaximized;
                preview.TabbedThumbnailMinimized += preview_TabbedThumbnailMinimized;

                preview.ClippingRectangle = GetClippingRectangle(rtbText);
                TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(preview);

                // Add thumbnail toolbar buttons
                TaskbarManager.Instance.ThumbnailToolBars.AddButtons(newTab.Handle, thumbButtonCut, thumbButtonCopy, thumbButtonPaste, thumbButtonSelectAll);

                // Select the tab in the application UI as well as taskbar tabbed thumbnail list
                tabControl1.SelectedTab = newTab;
                TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tabControl1.SelectedTab);

                button2.Enabled = true;
                button4.Enabled = true;
            }
        }

        void preview_TabbedThumbnailMinimized(object sender, TabbedThumbnailEventArgs e)
        {
            // User clicked on the minimize button on the thumbnail's context menu
            // Minimize the app
            this.WindowState = FormWindowState.Minimized;
        }

        void preview_TabbedThumbnailMaximized(object sender, TabbedThumbnailEventArgs e)
        {
            // User clicked on the maximize button on the thumbnail's context menu
            // Maximize the app
            this.WindowState = FormWindowState.Maximized;

            // If there is a selected tab, take it's screenshot
            // invalidate the tab's thumbnail
            // update the "preview" object with the new thumbnail
            if (tabControl1.Size != Size.Empty && tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
                UpdatePreviewBitmap(tabControl1.SelectedTab);
        }

        void preview_TabbedThumbnailClosed(object sender, TabbedThumbnailClosedEventArgs e)
        {

            TabPage pageClosed = null;

            // Find the tabpage that was "closed" by the user (via the taskbar tabbed thumbnail)
            foreach (TabPage page in tabControl1.TabPages)
            {
                if (page.Handle == e.WindowHandle)
                {
                    pageClosed = page;
                    break;
                }
            }

            if (pageClosed != null)
            {
                // Remove the event handlers
                WebBrowser wb = pageClosed.Controls[0] as WebBrowser;

                if (wb != null)
                {
                    wb.DocumentTitleChanged -= new EventHandler(wb_DocumentTitleChanged);
                    //wb.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(wb_DocumentCompleted);
                    wb.Navigated -= new WebBrowserNavigatedEventHandler(wb_Navigated);
                    wb.ProgressChanged -= new WebBrowserProgressChangedEventHandler(wb_ProgressChanged);
                    wb.Document.Window.Scroll -= new HtmlElementEventHandler(Window_Scroll);

                    wb.Dispose();
                }
                else
                {
                    // It's most likely a RichTextBox.

                    RichTextBox rtbText = pageClosed.Controls[0] as RichTextBox;

                    if (rtbText != null)
                    {
                        rtbText.KeyDown -= new KeyEventHandler(rtbText_KeyDown);
                        rtbText.MouseMove -= new MouseEventHandler(rtbText_MouseMove);
                        rtbText.KeyUp -= new KeyEventHandler(rtbText_KeyUp);
                    }

                    rtbText.Dispose();
                }

                // Finally, remove the tab from our UI
                if (pageClosed != null)
                    tabControl1.TabPages.Remove(pageClosed);

                // Dispose the tab
                pageClosed.Dispose();

                if (tabControl1.TabPages.Count > 0)
                    button2.Enabled = true;
                else
                    button2.Enabled = false;
            }

            TabbedThumbnail tabbedThumbnail = sender as TabbedThumbnail;
            if (tabbedThumbnail != null)
            {
                // Remove the event handlers from the tab preview
                tabbedThumbnail.TabbedThumbnailActivated -= (preview_TabbedThumbnailActivated);
                tabbedThumbnail.TabbedThumbnailClosed -= (preview_TabbedThumbnailClosed);
                tabbedThumbnail.TabbedThumbnailMaximized -= (preview_TabbedThumbnailMaximized);
                tabbedThumbnail.TabbedThumbnailMinimized -= (preview_TabbedThumbnailMinimized);
            }
        }

        void preview_TabbedThumbnailActivated(object sender, TabbedThumbnailEventArgs e)
        {
            // User selected a tab via the thumbnail preview
            // Select the corresponding control in our app
            foreach (TabPage page in tabControl1.TabPages)
            {
                if (page.Handle == e.WindowHandle)
                {
                    // Select the tab in the application UI as well as taskbar tabbed thumbnail list
                    tabControl1.SelectedTab = page;
                    TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(page);
                }
            }

            // Also activate our parent form (incase we are minimized, this will restore it)
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
        }

        void rtbText_KeyUp(object sender, KeyEventArgs e)
        {
            TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview((Control)sender);
            if (preview != null)
                preview.ClippingRectangle = GetClippingRectangle((RichTextBox)sender);
        }

        void rtbText_KeyDown(object sender, KeyEventArgs e)
        {
            TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview((Control)sender);
            if (preview != null)
                preview.ClippingRectangle = GetClippingRectangle((RichTextBox)sender);
        }

        void rtbText_MouseMove(object sender, MouseEventArgs e)
        {
            TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview((Control)sender);
            if (preview != null)
                preview.ClippingRectangle = GetClippingRectangle((RichTextBox)sender);
        }

        private string clipText = "Cli&p thumbnail";
        private string showFullText = "F&ull thumbnail";

        private void button4_Click(object sender, EventArgs e)
        {
            // Clip the thumbnail when showing the thumbnail preview or aero peek

            // Only supported for RTF/Text files (as an example to show that we can do thumbnail clip
            // for specific windows if needed)

            if (tabControl1.SelectedTab == null)
                return;

            TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tabControl1.SelectedTab);

            if (tabControl1.SelectedTab != null && preview != null)
            {
                RichTextBox rtbText = tabControl1.SelectedTab.Controls[0] as RichTextBox;

                if (button4.Text == clipText && rtbText != null)
                {
                    preview.ClippingRectangle = GetClippingRectangle(rtbText);
                }
                else if (button4.Text == showFullText)
                {
                    preview.ClippingRectangle = null;
                }
            }

            // toggle the text
            if (button4.Text == clipText)
                button4.Text = showFullText;
            else
                button4.Text = clipText;
        }

        private Rectangle GetClippingRectangle(RichTextBox rtbText)
        {
            int index = rtbText.GetFirstCharIndexOfCurrentLine();
            Point point = rtbText.GetPositionFromCharIndex(index);
            return new Rectangle(point, new Size(200, 119));
        }

        /// <summary>
        /// Navigates to the given path or URL file.
        /// Uses the currently selected tab
        /// </summary>
        /// <param name="path"></param>
        internal void Navigate(string path)
        {
            string[] lines = File.ReadAllLines(path);
            string urlString = "";

            foreach (string line in lines)
            {
                if (line.StartsWith("URL="))
                {
                    urlString = line.Replace("URL=", "");

                    break;
                }
            }

            if (!string.IsNullOrEmpty(path) && tabControl1.TabPages.Count > 0 && tabControl1.SelectedTab != null)
            {
                if (tabControl1.SelectedTab.Controls[0] is WebBrowser)
                {
                    ((WebBrowser)tabControl1.SelectedTab.Controls[0]).Navigate(urlString);
                }
            }
            else
            {
                // Simulate a click
                comboBox1.Text = urlString;
                button1_Click(this, EventArgs.Empty);
            }
        }
    }
}