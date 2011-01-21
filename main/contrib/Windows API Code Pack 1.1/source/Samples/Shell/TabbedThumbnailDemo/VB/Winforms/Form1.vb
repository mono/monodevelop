'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports Microsoft.WindowsAPICodePack.Taskbar

Namespace Microsoft.WindowsAPICodePack.Samples.TabbedThumbnailDemo
    Partial Public Class Form1
        Inherits Form
        ''' <summary>
        ''' Keeping track of the previously selected tab,
        ''' so we can capture it's bitmap when the user selects another tab.
        ''' Unfortunately, we cannot access the previously selected tab via the 
        ''' "selecting" event from TabControl or use any of its properties.
        ''' This seems to be the best way - keep track ourselves.
        ''' </summary>
        Private previousSelectedPage As TabPage = Nothing

        '
        Private thumbButtonBack As ThumbnailToolbarButton
        Private thumbButtonForward As ThumbnailToolbarButton
        Private thumbButtonRefresh As ThumbnailToolbarButton

        Private thumbButtonCut As ThumbnailToolbarButton
        Private thumbButtonCopy As ThumbnailToolbarButton
        Private thumbButtonPaste As ThumbnailToolbarButton
        Private thumbButtonSelectAll As ThumbnailToolbarButton

        ''' <summary>
        ''' Internal bool to keep track of the scroll event that is on the HTML Document's Window class.
        ''' We don't get a document or a window until we have a page loaded. This bool will be set once we 
        ''' navigate. It will be reset once we get add the scroll event...
        ''' </summary>
        Private scrollEventAdded As Boolean = False

        ''' <summary>
        ''' Reference to our window for displaying the favorite links
        ''' </summary>
        Private favsWindow As FavoritesWindow = Nothing

        Public Sub New()
            InitializeComponent()

            ' Listen for specific events on the tab control
            AddHandler tabControl1.Selecting, AddressOf tabControl1_Selecting
            AddHandler tabControl1.SelectedIndexChanged, AddressOf tabControl1_SelectedIndexChanged

            ' When the size of our form changes, invalidate the thumbnails so we can capture them again
            ' when user requests a peek or thumbnail preview.
            AddHandler SizeChanged, AddressOf Form1_SizeChanged

            ' Set our minimum size so the form will not have 0 height/width when user tries to resize it all the way
            Me.MinimumSize = New Size(500, 100)

            ' Show the Favorites window
            favsWindow = New FavoritesWindow(Me)
            favsWindow.Show()

            ' Create our Thumbnail toolbar buttons for the Browser doc
            thumbButtonBack = New ThumbnailToolbarButton(My.Resources.prevArrow, "Back")
            AddHandler thumbButtonBack.Click, AddressOf thumbButtonBack_Click

            thumbButtonForward = New ThumbnailToolbarButton(My.Resources.nextArrow, "Forward")
            AddHandler thumbButtonForward.Click, AddressOf thumbButtonForward_Click

            thumbButtonRefresh = New ThumbnailToolbarButton(My.Resources.refresh, "Refresh")
            AddHandler thumbButtonRefresh.Click, AddressOf thumbButtonRefresh_Click

            ' Create our thumbnail toolbar buttons for the RichTextBox doc
            thumbButtonCut = New ThumbnailToolbarButton(My.Resources.cut, "Cut")
            AddHandler thumbButtonCut.Click, AddressOf thumbButtonCut_Click

            thumbButtonCopy = New ThumbnailToolbarButton(My.Resources.copy, "Copy")
            AddHandler thumbButtonCopy.Click, AddressOf thumbButtonCopy_Click

            thumbButtonPaste = New ThumbnailToolbarButton(My.Resources.paste, "Paste")
            AddHandler thumbButtonPaste.Click, AddressOf thumbButtonPaste_Click

            thumbButtonSelectAll = New ThumbnailToolbarButton(My.Resources.selectAll, "SelectAll")
            AddHandler thumbButtonSelectAll.Click, AddressOf thumbButtonSelectAll_Click

            AddHandler FormClosing, AddressOf Form1_FormClosing
        End Sub

        Private cancelFormClosing As Boolean = False

        Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs)
            ' If the user is closing the app, ask them if they wish to close the current tab
            ' or all the tabs

            If tabControl1 IsNot Nothing AndAlso tabControl1.TabPages.Count > 0 Then
                If tabControl1.TabPages.Count = 1 Then
                    ' close the tab and the application
                    cancelFormClosing = False
                Else
                    ' More than 1 tab.... show the user the TaskDialog
                    Dim tdClose As New TaskDialog()
                    tdClose.Caption = "Tabbed Thumbnail demo (Winforms)"
                    tdClose.InstructionText = "Do you want to close all the tabs or the current tab?"
                    tdClose.Cancelable = True
                    tdClose.OwnerWindowHandle = Me.Handle

                    Dim closeAllTabsButton As New TaskDialogButton("closeAllTabsButton", "Close all tabs")
                    closeAllTabsButton.Default = True
                    AddHandler closeAllTabsButton.Click, AddressOf closeAllTabsButton_Click
                    tdClose.Controls.Add(closeAllTabsButton)

                    Dim closeCurrentTabButton As New TaskDialogButton("closeCurrentTabButton", "Close current tab")
                    AddHandler closeCurrentTabButton.Click, AddressOf closeCurrentTabButton_Click
                    tdClose.Controls.Add(closeCurrentTabButton)

                    tdClose.Show()
                End If
            End If

            e.Cancel = cancelFormClosing
        End Sub

        Private Sub closeCurrentTabButton_Click(ByVal sender As Object, ByVal e As EventArgs)
            button2_Click(Me, EventArgs.Empty)
            cancelFormClosing = True
        End Sub

        Private Sub closeAllTabsButton_Click(ByVal sender As Object, ByVal e As EventArgs)
            cancelFormClosing = False
        End Sub

        Private Sub Form1_SizeChanged(ByVal sender As Object, ByVal e As EventArgs)
            ' If we are in minimized state, don't invalidate the thumbnail as we want to keep the 
            ' cached image. Minimized forms can't be captured.
            If WindowState <> FormWindowState.Minimized Then
                ' Just invalidate the selected tab's thumbnail so we can recapture them when requested
                If tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                    TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tabControl1.SelectedTab).InvalidatePreview()
                End If

            End If
        End Sub

        Private Function FindTab(ByVal handle As IntPtr) As TabPage
            If handle = IntPtr.Zero Then
                Return Nothing
            End If

            For Each page As TabPage In tabControl1.TabPages
                If page.Handle = handle Then
                    Return page
                End If
            Next page

            Return Nothing
        End Function

        Private Sub thumbButtonBack_Click(ByVal sender As Object, ByVal e As ThumbnailButtonClickedEventArgs)
            Dim page As TabPage = FindTab(e.WindowHandle)

            If page IsNot Nothing AndAlso TypeOf page.Controls(0) Is WebBrowser Then
                CType(page.Controls(0), WebBrowser).GoBack()
            End If
        End Sub

        Private Sub thumbButtonForward_Click(ByVal sender As Object, ByVal e As ThumbnailButtonClickedEventArgs)
            Dim page As TabPage = FindTab(e.WindowHandle)

            If page IsNot Nothing AndAlso TypeOf page.Controls(0) Is WebBrowser Then
                CType(page.Controls(0), WebBrowser).GoForward()
            End If
        End Sub


        Private Sub thumbButtonRefresh_Click(ByVal sender As Object, ByVal e As ThumbnailButtonClickedEventArgs)
            Dim page As TabPage = FindTab(e.WindowHandle)

            If page IsNot Nothing AndAlso TypeOf page.Controls(0) Is WebBrowser Then
                CType(page.Controls(0), WebBrowser).Refresh()
            End If
        End Sub


        Private Sub thumbButtonCut_Click(ByVal sender As Object, ByVal e As ThumbnailButtonClickedEventArgs)
            Dim page As TabPage = FindTab(e.WindowHandle)

            If page IsNot Nothing AndAlso TypeOf page.Controls(0) Is RichTextBox Then
                CType(page.Controls(0), RichTextBox).Cut()

                ' If there is a selected tab, take it's screenshot
                ' invalidate the tab's thumbnail
                ' update the "preview" object with the new thumbnail
                If tabControl1.Size <> Size.Empty AndAlso tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                    UpdatePreviewBitmap(tabControl1.SelectedTab)
                End If
            End If
        End Sub

        Private Sub thumbButtonCopy_Click(ByVal sender As Object, ByVal e As ThumbnailButtonClickedEventArgs)
            Dim page As TabPage = FindTab(e.WindowHandle)

            If page IsNot Nothing AndAlso TypeOf page.Controls(0) Is RichTextBox Then
                CType(page.Controls(0), RichTextBox).Copy()

                ' If there is a selected tab, take it's screenshot
                ' invalidate the tab's thumbnail
                ' update the "preview" object with the new thumbnail
                If tabControl1.Size <> Size.Empty AndAlso tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                    UpdatePreviewBitmap(tabControl1.SelectedTab)
                End If
            End If
        End Sub

        Private Sub thumbButtonPaste_Click(ByVal sender As Object, ByVal e As ThumbnailButtonClickedEventArgs)
            Dim page As TabPage = FindTab(e.WindowHandle)

            If page IsNot Nothing AndAlso TypeOf page.Controls(0) Is RichTextBox Then
                CType(page.Controls(0), RichTextBox).Paste()

                ' If there is a selected tab, take it's screenshot
                ' invalidate the tab's thumbnail
                ' update the "preview" object with the new thumbnail
                If tabControl1.Size <> Size.Empty AndAlso tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                    UpdatePreviewBitmap(tabControl1.SelectedTab)
                End If
            End If
        End Sub

        Private Sub thumbButtonSelectAll_Click(ByVal sender As Object, ByVal e As ThumbnailButtonClickedEventArgs)
            Dim page As TabPage = FindTab(e.WindowHandle)

            If page IsNot Nothing AndAlso TypeOf page.Controls(0) Is RichTextBox Then
                CType(page.Controls(0), RichTextBox).SelectAll()
            End If
        End Sub

        Private Sub tabControl1_Selecting(ByVal sender As Object, ByVal e As TabControlCancelEventArgs)
            ' Before selecting,
            ' If there is a selected tab, take it's screenshot
            ' invalidate the tab's thumbnail
            ' update the "preview" object with the new thumbnail
            If tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                UpdatePreviewBitmap(previousSelectedPage)
            End If

            ' update our selected tab
            previousSelectedPage = tabControl1.SelectedTab
        End Sub

        ''' <summary>
        ''' Helper method to update the thumbnail preview for a given tab page.
        ''' </summary>
        ''' <param name="tabPage"></param>
        Private Sub UpdatePreviewBitmap(ByVal tabPage As TabPage)
            If tabPage IsNot Nothing Then
                Dim preview As TabbedThumbnail = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tabPage)

                If preview IsNot Nothing Then
                    Dim bitmap As Bitmap = TabbedThumbnailScreenCapture.GrabWindowBitmap(tabPage.Handle, tabPage.Size)
                    preview.SetImage(bitmap)
                End If


            End If
        End Sub

        Private Sub tabControl1_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
            ' Make sure we let the Taskbar know about the active/selected tab
            ' Tabbed thumbnails need to be updated to indicate which one is currently selected
            If tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tabControl1.SelectedTab)

                If TypeOf tabControl1.SelectedTab.Controls(0) Is RichTextBox Then
                    button4.Enabled = True
                Else
                    button4.Enabled = False
                End If
            End If
        End Sub

        Private Sub Window_Scroll(ByVal sender As Object, ByVal e As HtmlElementEventArgs)
            ' If there is a selected tab, take it's screenshot
            ' invalidate the tab's thumbnail
            ' update the "preview" object with the new thumbnail
            If tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                UpdatePreviewBitmap(tabControl1.SelectedTab)
            End If
        End Sub

        Private Sub wb_Navigated(ByVal sender As Object, ByVal e As WebBrowserNavigatedEventArgs)
            ' If there is a selected tab, take it's screenshot
            ' invalidate the tab's thumbnail
            ' update the "preview" object with the new thumbnail
            If tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                UpdatePreviewBitmap(tabControl1.SelectedTab)
            End If

            ' Update the combobox / addressbar 
            comboBox1.Text = (CType(sender, WebBrowser)).Document.Url.ToString()

            If Not scrollEventAdded Then
                AddHandler (CType(sender, WebBrowser)).Document.Window.Scroll, AddressOf Window_Scroll
                scrollEventAdded = True
            End If
        End Sub

        ''' <summary>
        ''' Create a new tab, add a webbrowser and navigate the given address/URL
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="args"></param>
        Private Sub button1_Click(ByVal sender As Object, ByVal args As System.EventArgs) Handles button1.Click
            Dim newTab As New TabPage(comboBox1.Text)
            tabControl1.TabPages.Add(newTab)
            Dim wb As New WebBrowser()
            AddHandler wb.DocumentTitleChanged, AddressOf wb_DocumentTitleChanged
            AddHandler wb.DocumentCompleted, AddressOf wb_DocumentCompleted
            AddHandler wb.Navigated, AddressOf wb_Navigated
            AddHandler wb.ProgressChanged, AddressOf wb_ProgressChanged
            wb.Dock = DockStyle.Fill
            wb.Navigate(comboBox1.Text)
            newTab.Controls.Add(wb)

            ' Add thumbnail toolbar buttons
            TaskbarManager.Instance.ThumbnailToolbars.AddButtons(newTab.Handle, thumbButtonBack, thumbButtonForward, thumbButtonRefresh)

            ' Add a new preview
            Dim preview As New TabbedThumbnail(Me.Handle, newTab.Handle)

            ' Event handlers for this preview
            AddHandler preview.TabbedThumbnailActivated, AddressOf preview_TabbedThumbnailActivated
            AddHandler preview.TabbedThumbnailClosed, AddressOf preview_TabbedThumbnailClosed
            AddHandler preview.TabbedThumbnailMaximized, AddressOf preview_TabbedThumbnailMaximized
            AddHandler preview.TabbedThumbnailMinimized, AddressOf preview_TabbedThumbnailMinimized

            TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(preview)

            ' Select the tab in the application UI as well as taskbar tabbed thumbnail list
            tabControl1.SelectedTab = newTab
            TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tabControl1.SelectedTab)

            ' set false for this new webbrowser
            scrollEventAdded = False

            '
            button2.Enabled = True
        End Sub

        Private Sub wb_ProgressChanged(ByVal sender As Object, ByVal e As WebBrowserProgressChangedEventArgs)
            ' Based on the webbrowser's progress, update our statusbar progressbar
            If e.CurrentProgress >= 0 Then
                toolStripProgressBar1.Maximum = CInt(Fix(e.MaximumProgress))
                toolStripProgressBar1.Value = CInt(Fix(e.CurrentProgress))
            End If
        End Sub

        Private Sub wb_DocumentCompleted(ByVal sender As Object, ByVal e As WebBrowserDocumentCompletedEventArgs)
            ' If there is a selected tab, take it's screenshot
            ' invalidate the tab's thumbnail
            ' update the "preview" object with the new thumbnail
            If tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                UpdatePreviewBitmap(tabControl1.SelectedTab)
            End If
        End Sub

        Private Sub wb_DocumentTitleChanged(ByVal sender As Object, ByVal e As System.EventArgs)
            ' When the webpage's title changes,
            ' update the tab's title and taskbar thumbnail's title
            Dim page As TabPage = TryCast((CType(sender, WebBrowser)).Parent, TabPage)

            If page IsNot Nothing Then
                page.Text = (CType(sender, WebBrowser)).DocumentTitle

                Dim preview As TabbedThumbnail = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(page)

                If preview IsNot Nothing Then
                    preview.Title = page.Text
                End If

            End If
        End Sub

        ''' <summary>
        ''' Close button - close the specific tab and also
        ''' remove the thumbnail preview
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub button2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles button2.Click
            If tabControl1.SelectedTab IsNot Nothing Then
                TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tabControl1.SelectedTab)
                tabControl1.TabPages.Remove(tabControl1.SelectedTab)
            End If

            If tabControl1.TabPages.Count = 0 Then
                button2.Enabled = False
            End If
        End Sub

        ''' <summary>
        ''' Open a user-specified text file in a new tab (using a RichTextBox)
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub button3_Click(ByVal sender As Object, ByVal e As EventArgs) Handles button3.Click
            ' Open text file
            Dim cfd As New CommonOpenFileDialog()

            CommonFileDialogStandardFilters.TextFiles.ShowExtensions = True
            Dim rtfFilter As New CommonFileDialogFilter("RTF Files", ".rtf")
            rtfFilter.ShowExtensions = True

            cfd.Filters.Add(CommonFileDialogStandardFilters.TextFiles)
            cfd.Filters.Add(rtfFilter)

            If cfd.ShowDialog() = CommonFileDialogResult.OK Then
                Dim newTab As New TabPage(Path.GetFileName(cfd.FileName))
                tabControl1.TabPages.Add(newTab)
                Dim rtbText As New RichTextBox()
                AddHandler rtbText.KeyDown, AddressOf rtbText_KeyDown
                AddHandler rtbText.MouseMove, AddressOf rtbText_MouseMove
                AddHandler rtbText.KeyUp, AddressOf rtbText_KeyUp
                rtbText.Dock = DockStyle.Fill

                ' Based on the extension, load the file appropriately in the RichTextBox
                If Path.GetExtension(cfd.FileName).ToLower() = ".txt" Then
                    rtbText.LoadFile(cfd.FileName, RichTextBoxStreamType.PlainText)
                ElseIf Path.GetExtension(cfd.FileName).ToLower() = ".rtf" Then
                    rtbText.LoadFile(cfd.FileName, RichTextBoxStreamType.RichText)
                End If

                ' Update the tab
                newTab.Controls.Add(rtbText)

                ' Add a new preview
                Dim preview As New TabbedThumbnail(Me.Handle, newTab.Handle)

                ' Event handlers for this preview
                AddHandler preview.TabbedThumbnailActivated, AddressOf preview_TabbedThumbnailActivated
                AddHandler preview.TabbedThumbnailClosed, AddressOf preview_TabbedThumbnailClosed
                AddHandler preview.TabbedThumbnailMaximized, AddressOf preview_TabbedThumbnailMaximized
                AddHandler preview.TabbedThumbnailMinimized, AddressOf preview_TabbedThumbnailMinimized

                preview.ClippingRectangle = GetClippingRectangle(rtbText)
                TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(preview)

                ' Add thumbnail toolbar buttons
                TaskbarManager.Instance.ThumbnailToolbars.AddButtons(newTab.Handle, thumbButtonCut, thumbButtonCopy, thumbButtonPaste, thumbButtonSelectAll)

                ' Select the tab in the application UI as well as taskbar tabbed thumbnail list
                tabControl1.SelectedTab = newTab
                TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tabControl1.SelectedTab)

                button2.Enabled = True
                button4.Enabled = True
            End If
        End Sub

        Private Sub preview_TabbedThumbnailMinimized(ByVal sender As Object, ByVal e As TabbedThumbnailEventArgs)
            ' User clicked on the minimize button on the thumbnail's context menu
            ' Minimize the app
            Me.WindowState = FormWindowState.Minimized
        End Sub

        Private Sub preview_TabbedThumbnailMaximized(ByVal sender As Object, ByVal e As TabbedThumbnailEventArgs)
            ' User clicked on the maximize button on the thumbnail's context menu
            ' Maximize the app
            Me.WindowState = FormWindowState.Maximized

            ' If there is a selected tab, take it's screenshot
            ' invalidate the tab's thumbnail
            ' update the "preview" object with the new thumbnail
            If tabControl1.Size <> Size.Empty AndAlso tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                UpdatePreviewBitmap(tabControl1.SelectedTab)
            End If
        End Sub

        Private Sub preview_TabbedThumbnailClosed(ByVal sender As Object, ByVal e As TabbedThumbnailClosedEventArgs)
            Dim pageClosed As TabPage = Nothing

            ' Find the tabpage that was "closed" by the user (via the taskbar tabbed thumbnail)
            For Each page As TabPage In tabControl1.TabPages
                If page.Handle = e.WindowHandle Then
                    pageClosed = page
                    Exit For
                End If
            Next page

            If pageClosed IsNot Nothing Then
                ' Remove the event handlers
                Dim wb As WebBrowser = TryCast(pageClosed.Controls(0), WebBrowser)

                If wb IsNot Nothing Then
                    RemoveHandler wb.DocumentTitleChanged, AddressOf wb_DocumentTitleChanged
                    RemoveHandler wb.DocumentCompleted, AddressOf wb_DocumentCompleted
                    RemoveHandler wb.Navigated, AddressOf wb_Navigated
                    RemoveHandler wb.ProgressChanged, AddressOf wb_ProgressChanged
                    RemoveHandler wb.Document.Window.Scroll, AddressOf Window_Scroll

                    wb.Dispose()
                Else
                    ' It's most likely a RichTextBox.

                    Dim rtbText As RichTextBox = TryCast(pageClosed.Controls(0), RichTextBox)

                    If rtbText IsNot Nothing Then
                        RemoveHandler rtbText.KeyDown, AddressOf rtbText_KeyDown
                        RemoveHandler rtbText.MouseMove, AddressOf rtbText_MouseMove
                        RemoveHandler rtbText.KeyUp, AddressOf rtbText_KeyUp
                    End If

                    rtbText.Dispose()
                End If

                ' Finally, remove the tab from our UI
                If pageClosed IsNot Nothing Then
                    tabControl1.TabPages.Remove(pageClosed)
                End If

                ' Dispose the tab
                pageClosed.Dispose()

                If tabControl1.TabPages.Count > 0 Then
                    button2.Enabled = True
                Else
                    button2.Enabled = False
                End If
            End If

            Dim tabbedThumbnail As TabbedThumbnail = TryCast(sender, TabbedThumbnail)
            If tabbedThumbnail IsNot Nothing Then
                ' Remove the event handlers from the tab preview
                RemoveHandler tabbedThumbnail.TabbedThumbnailActivated, AddressOf preview_TabbedThumbnailActivated
                RemoveHandler tabbedThumbnail.TabbedThumbnailClosed, AddressOf preview_TabbedThumbnailClosed
                RemoveHandler tabbedThumbnail.TabbedThumbnailMaximized, AddressOf preview_TabbedThumbnailMaximized
                RemoveHandler tabbedThumbnail.TabbedThumbnailMinimized, AddressOf preview_TabbedThumbnailMinimized
            End If
        End Sub

        Private Sub preview_TabbedThumbnailActivated(ByVal sender As Object, ByVal e As TabbedThumbnailEventArgs)
            ' User selected a tab via the thumbnail preview
            ' Select the corresponding control in our app
            For Each page As TabPage In tabControl1.TabPages
                If page.Handle = e.WindowHandle Then
                    ' Select the tab in the application UI as well as taskbar tabbed thumbnail list
                    tabControl1.SelectedTab = page
                    TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(page)
                End If
            Next page

            ' Also activate our parent form (incase we are minimized, this will restore it)
            If Me.WindowState = FormWindowState.Minimized Then
                Me.WindowState = FormWindowState.Normal
            End If
        End Sub

        Private Sub rtbText_KeyUp(ByVal sender As Object, ByVal e As KeyEventArgs)
            Dim preview As TabbedThumbnail = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(CType(sender, Control))
            If preview IsNot Nothing Then
                preview.ClippingRectangle = GetClippingRectangle(CType(sender, RichTextBox))
            End If
        End Sub

        Private Sub rtbText_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
            Dim preview As TabbedThumbnail = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(CType(sender, Control))
            If preview IsNot Nothing Then
                preview.ClippingRectangle = GetClippingRectangle(CType(sender, RichTextBox))
            End If
        End Sub

        Private Sub rtbText_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs)
            Dim preview As TabbedThumbnail = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(CType(sender, Control))
            If preview IsNot Nothing Then
                preview.ClippingRectangle = GetClippingRectangle(CType(sender, RichTextBox))
            End If
        End Sub

        Private clipText As String = "Cli&p thumbnail"
        Private showFullText As String = "F&ull thumbnail"

        Private Sub button4_Click(ByVal sender As Object, ByVal e As EventArgs) Handles button4.Click
            ' Clip the thumbnail when showing the thumbnail preview or aero peek

            ' Only supported for RTF/Text files (as an example to show that we can do thumbnail clip
            ' for specific windows if needed)

            '
            Dim preview As TabbedThumbnail = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tabControl1.SelectedTab)

            If tabControl1.SelectedTab IsNot Nothing AndAlso preview IsNot Nothing Then
                Dim rtbText As RichTextBox = TryCast(tabControl1.SelectedTab.Controls(0), RichTextBox)

                If button4.Text = clipText AndAlso rtbText IsNot Nothing Then
                    preview.ClippingRectangle = GetClippingRectangle(rtbText)
                ElseIf button4.Text = showFullText Then
                    preview.ClippingRectangle = Rectangle.Empty
                End If
            End If

            ' toggle the text
            If button4.Text = clipText Then
                button4.Text = showFullText
            Else
                button4.Text = clipText
            End If
        End Sub

        Private Function GetClippingRectangle(ByVal rtbText As RichTextBox) As Rectangle
            Dim index As Integer = rtbText.GetFirstCharIndexOfCurrentLine()
            Dim point As Point = rtbText.GetPositionFromCharIndex(index)
            Return New Rectangle(point, New Size(200, 119))
        End Function

        ''' <summary>
        ''' Navigates to the given path or URL file.
        ''' Uses the currently selected tab
        ''' </summary>
        ''' <param name="path"></param>
        Friend Sub Navigate(ByVal path As String)
            Dim lines() As String = File.ReadAllLines(path)
            Dim urlString As String = ""

            For Each line As String In lines
                If line.StartsWith("URL=") Then
                    urlString = line.Replace("URL=", "")

                    Exit For
                End If
            Next line

            If (Not String.IsNullOrEmpty(path)) AndAlso tabControl1.TabPages.Count > 0 AndAlso tabControl1.SelectedTab IsNot Nothing Then
                If TypeOf tabControl1.SelectedTab.Controls(0) Is WebBrowser Then
                    CType(tabControl1.SelectedTab.Controls(0), WebBrowser).Navigate(urlString)
                End If
            Else
                ' Simulate a click
                comboBox1.Text = urlString
                button1_Click(Me, EventArgs.Empty)
            End If
        End Sub
    End Class
End Namespace