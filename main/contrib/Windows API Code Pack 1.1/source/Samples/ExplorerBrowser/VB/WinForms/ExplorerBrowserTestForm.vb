'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Shell
Imports System.Windows.Forms.Integration
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.Controls
Imports System.Threading

Namespace Microsoft.WindowsAPICodePack.Samples
	Partial Public Class ExplorerBrowserTestForm
        Inherits Form
        Dim uiDecoupleTimer As New System.Windows.Forms.Timer
        Dim selectionChanged As New AutoResetEvent(False)
        Dim itemsChanged As New AutoResetEvent(False)

		Public Sub New()
			InitializeComponent()

			' initialize known folder combo box
			Dim knownFolderList As New List(Of String)()
			For Each folder As IKnownFolder In KnownFolders.All
				knownFolderList.Add(folder.CanonicalName)
			Next folder
			knownFolderList.Sort()
			knownFolderCombo.Items.AddRange(knownFolderList.ToArray())

			' initial property grids
			navigationPropertyGrid.SelectedObject = explorerBrowser.NavigationOptions
			visibilityPropertyGrid.SelectedObject = explorerBrowser.NavigationOptions.PaneVisibility
			contentPropertyGrid.SelectedObject = explorerBrowser.ContentOptions

			' setup ExplorerBrowser navigation events
			AddHandler explorerBrowser.NavigationPending, AddressOf explorerBrowser_NavigationPending
			AddHandler explorerBrowser.NavigationFailed, AddressOf explorerBrowser_NavigationFailed
			AddHandler explorerBrowser.NavigationComplete, AddressOf explorerBrowser_NavigationComplete
			AddHandler explorerBrowser.ItemsChanged, AddressOf explorerBrowser_ItemsChanged
            AddHandler explorerBrowser.SelectionChanged, AddressOf explorerBrowser_SelectionChanged
            AddHandler explorerBrowser.ViewEnumerationComplete, AddressOf explorerBrowser_ViewEnumerationComplete


			' set up Navigation log event and button state
			AddHandler explorerBrowser.NavigationLog.NavigationLogChanged, AddressOf NavigationLog_NavigationLogChanged
			Me.backButton.Enabled = False
            Me.forwardButton.Enabled = False

            AddHandler uiDecoupleTimer.Tick, AddressOf uiDecoupleTimer_Tick
            Me.uiDecoupleTimer.Interval = 100
            uiDecoupleTimer.Start()

		End Sub

        Private Sub uiDecoupleTimer_Tick( ByVal sender As Object, ByVal args As EventArgs )
            If (selectionChanged.WaitOne(1)) Then
                Dim itemsText As New StringBuilder()
                For Each item As ShellObject In explorerBrowser.SelectedItems
                    If item IsNot Nothing Then
                        itemsText.AppendLine(Constants.vbTab & "Item = " & item.GetDisplayName(DisplayNameType.Default))
                    End If
                Next item
                Me.selectedItemsTextBox.Text = itemsText.ToString()
                Me.itemsTabControl.TabPages(1).Text = "Selected Items (Count=" & explorerBrowser.SelectedItems.Count.ToString() & ")"
            End If

            If (itemsChanged.WaitOne(1)) Then
                Dim itemsText As New StringBuilder()
                For Each item As ShellObject In explorerBrowser.Items
                    If item IsNot Nothing Then
                        itemsText.AppendLine(Constants.vbTab & "Item = " & item.GetDisplayName(DisplayNameType.Default))
                    End If
                Next item
                Me.itemsTextBox.Text = itemsText.ToString()
                Me.itemsTabControl.TabPages(0).Text = "Items (Count=" & explorerBrowser.Items.Count.ToString() & ")"
            End If
        End Sub

        Private Sub explorerBrowser_ViewEnumerationComplete(ByVal sender As Object, ByVal args As EventArgs)
            ' This event is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            BeginInvoke(New MethodInvoker(Function() AnonymousMethod2()))

            selectionChanged.Set()
            itemsChanged.Set()
        End Sub
        Private Function AnonymousMethod2() As Object
            Me.eventHistoryTextBox.Text = Me.eventHistoryTextBox.Text & "View enumeration complete." & Constants.vbLf
            Return Nothing
        End Function


        Protected Overrides Sub OnShown(ByVal e As EventArgs)
            MyBase.OnShown(e)
            explorerBrowser.Navigate(CType(KnownFolders.Desktop, ShellObject))
        End Sub

        Private Sub NavigationLog_NavigationLogChanged(ByVal sender As Object, ByVal args As NavigationLogEventArgs)
            ' This event is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            ' calculate button states
            ' update history combo box
            BeginInvoke(New MethodInvoker(Function() AnonymousMethod1(args)))
        End Sub

        Private Function AnonymousMethod1(ByVal args As NavigationLogEventArgs) As Object
            If args.CanNavigateBackwardChanged Then
                Me.backButton.Enabled = explorerBrowser.NavigationLog.CanNavigateBackward
            End If
            If args.CanNavigateForwardChanged Then
                Me.forwardButton.Enabled = explorerBrowser.NavigationLog.CanNavigateForward
            End If
            If args.LocationsChanged Then
                Me.navigationHistoryCombo.Items.Clear()
                For Each shobj As ShellObject In Me.explorerBrowser.NavigationLog.Locations
                    Me.navigationHistoryCombo.Items.Add(shobj.Name)
                Next shobj
            End If
            If Me.explorerBrowser.NavigationLog.CurrentLocationIndex = -1 Then
                Me.navigationHistoryCombo.Text = ""
            Else
                Me.navigationHistoryCombo.SelectedIndex = Me.explorerBrowser.NavigationLog.CurrentLocationIndex
            End If
            Return Nothing
        End Function

        Private Sub explorerBrowser_SelectionChanged(ByVal sender As Object, ByVal e As EventArgs)
            selectionChanged.Set()
        End Sub

        Private Sub explorerBrowser_ItemsChanged(ByVal sender As Object, ByVal e As EventArgs)
            itemsChanged.Set()
        End Sub

        Private Sub explorerBrowser_NavigationComplete(ByVal sender As Object, ByVal args As NavigationCompleteEventArgs)
            ' This event is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            ' update event history text box
            BeginInvoke(New MethodInvoker(Function() AnonymousMethod4(args)))
        End Sub

        Private Function AnonymousMethod4(ByVal args As NavigationCompleteEventArgs) As Object
            Dim location As String = If((args.NewLocation Is Nothing), "(unknown)", args.NewLocation.Name)
            Me.eventHistoryTextBox.Text = Me.eventHistoryTextBox.Text & "Navigation completed. New Location = " & location & Constants.vbLf
            Return Nothing
        End Function

        Private Sub explorerBrowser_NavigationFailed(ByVal sender As Object, ByVal args As NavigationFailedEventArgs)
            ' This event is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            ' update event history text box
            BeginInvoke(New MethodInvoker(Function() AnonymousMethod5(args)))
        End Sub

        Private Function AnonymousMethod5(ByVal args As NavigationFailedEventArgs) As Object
            Dim location As String = If((args.FailedLocation Is Nothing), "(unknown)", args.FailedLocation.Name)
            Me.eventHistoryTextBox.Text = Me.eventHistoryTextBox.Text & "Navigation failed. Failed Location = " & location & Constants.vbLf
            If Me.explorerBrowser.NavigationLog.CurrentLocationIndex = -1 Then
                Me.navigationHistoryCombo.Text = ""
            Else
                Me.navigationHistoryCombo.SelectedIndex = Me.explorerBrowser.NavigationLog.CurrentLocationIndex
            End If
            Return Nothing
        End Function

        Private Sub explorerBrowser_NavigationPending(ByVal sender As Object, ByVal args As NavigationPendingEventArgs)
            ' fail navigation if check selected (this must be synchronous)
            args.Cancel = Me.failNavigationCheckBox.Checked


            ' This portion is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            ' update event history text box
            BeginInvoke(New MethodInvoker(Function() AnonymousMethod6(args)))
        End Sub

        Private Function AnonymousMethod6(ByVal args As NavigationPendingEventArgs) As Object
            Dim message As String = ""
            Dim location As String = If((args.PendingLocation Is Nothing), "(unknown)", args.PendingLocation.Name)
            If args.Cancel Then
                message = "Navigation Failing. Pending Location = " & location
            Else
                message = "Navigation Pending. Pending Location = " & location
            End If
            Me.eventHistoryTextBox.Text = Me.eventHistoryTextBox.Text & message & Constants.vbLf
            Return Nothing
        End Function

        Private Sub navigateButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles navigateButton.Click
            Try
                ' navigate to specific folder
                explorerBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(pathEdit.Text))
            Catch e1 As COMException
                MessageBox.Show("Navigation not possible.")
            End Try
        End Sub

        Private Sub filePathNavigate_Click(ByVal sender As Object, ByVal e As EventArgs) Handles filePathNavigate.Click
            Try
                ' Navigates to a specified file (must be a container file to work, i.e., ZIP, CAB)         
                Me.explorerBrowser.Navigate(ShellFile.FromFilePath(Me.filePathEdit.Text))
            Catch e1 As COMException
                MessageBox.Show("Navigation not possible.")
            End Try
        End Sub

        Private Sub knownFolderNavigate_Click(ByVal sender As Object, ByVal e As EventArgs) Handles knownFolderNavigate.Click
            Try
                ' Navigate to a known folder
                Dim kf As IKnownFolder = KnownFolderHelper.FromCanonicalName(Me.knownFolderCombo.Items(knownFolderCombo.SelectedIndex).ToString())

                Me.explorerBrowser.Navigate(CType(kf, ShellObject))
            Catch e1 As COMException
                MessageBox.Show("Navigation not possible.")
            End Try
        End Sub

        Private Sub forwardButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles forwardButton.Click
            ' Move forwards through navigation log
            explorerBrowser.NavigateLogLocation(NavigationLogDirection.Forward)
        End Sub

        Private Sub backButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles backButton.Click
            ' Move backwards through navigation log
            explorerBrowser.NavigateLogLocation(NavigationLogDirection.Backward)
        End Sub

        Private Sub navigationHistoryCombo_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles navigationHistoryCombo.SelectedIndexChanged
            ' navigating to specific index in navigation log
            explorerBrowser.NavigateLogLocation(Me.navigationHistoryCombo.SelectedIndex)
        End Sub

        Private Sub clearHistoryButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles clearHistoryButton.Click
            ' clear navigation log
            explorerBrowser.NavigationLog.ClearLog()
        End Sub

        Private Sub filePathEdit_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles filePathEdit.TextChanged
            filePathNavigate.Enabled = (filePathEdit.Text.Length > 0)
        End Sub

        Private Sub pathEdit_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles pathEdit.TextChanged
            navigateButton.Enabled = (pathEdit.Text.Length > 0)
        End Sub

        Private Sub knownFolderCombo_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles knownFolderCombo.TextChanged
            knownFolderNavigate.Enabled = (knownFolderCombo.Text.Length > 0)
        End Sub

    End Class
End Namespace
