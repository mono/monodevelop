'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Threading
Imports Microsoft.WindowsAPICodePack.Shell
Imports System.Windows.Input
Imports Microsoft.WindowsAPICodePack.Shell.PropertySystem
Imports System.Windows.Media
Imports System.Windows.Interop
Imports System.Globalization
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports System.Diagnostics
Imports System.Threading
Imports System.Windows.Media.Imaging
Imports System.Linq

Namespace Microsoft.WindowsAPICodePack.Samples.SearchApp
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
        Private top_Renamed As Integer = 10, left_Renamed As Integer = 10 ' for glass
		Private neverRendered As Boolean = True ' for glass
		Private Const WM_DWMCOMPOSITIONCHANGED As Integer = &H031E ' for glass (when DWM / glass setting is changed)
		Friend searchFolder As ShellSearchFolder = Nothing
		Private advWindow As AdvancedSearch ' keep only one instance of the advanced window (unless user closes it)

		Private helpTaskDialog As TaskDialog

		' Background thread for our search
		Private backgroundSearchThread As Thread = Nothing

		Private selectedScope As ShellContainer = CType(KnownFolders.UsersFiles, ShellContainer)

		Public Sub New()
			InitializeComponent()
			AddHandler DragThumb.DragDelta, AddressOf OnMove

			AddHandler SourceInitialized, AddressOf Window1_SourceInitialized
			AddHandler Loaded, AddressOf Window1_Loaded

			' Because the search can take some time, using a background thread.
			' This timer will check if that thread is still alive and accordingly update
			' the cursor
			Dim timer As New DispatcherTimer()
			timer.Interval = New TimeSpan(0, 0, 1)
			timer.IsEnabled = True
			AddHandler timer.Tick, AddressOf timer_Tick

			' Update the Scopes combobox with all the known folders
			Dim sortedKnownFolders = From folder In KnownFolders.All _
			                         Where (folder.CanonicalName IsNot Nothing AndAlso folder.CanonicalName.Length > 0) _
			                         Order By folder.CanonicalName _
			                         Select folder

			' Add the Browse... item so users can select any arbitary location
			Dim browsePanel As New StackPanel()
			browsePanel.Margin = New Thickness(5, 2, 5, 2)
			browsePanel.Orientation = Orientation.Horizontal

			Dim browseImg As New Image()
			browseImg.Source = (New StockIcons()).FolderOpen.BitmapSource
			browseImg.Height = 32

			Dim browseTextBlock As New TextBlock()
			browseTextBlock.Background = Brushes.Transparent
			browseTextBlock.FontSize = 10
			browseTextBlock.Margin = New Thickness(4)
			browseTextBlock.VerticalAlignment = VerticalAlignment.Center
			browseTextBlock.Text = "Browse..."

			browsePanel.Children.Add(browseImg)
			browsePanel.Children.Add(browseTextBlock)

			SearchScopesCombo.Items.Add(browsePanel)

			For Each obj As ShellContainer In sortedKnownFolders
				Dim panel As New StackPanel()
				panel.Margin = New Thickness(5, 2, 5, 2)
				panel.Orientation = Orientation.Horizontal

				Dim img As New Image()
				img.Source = obj.Thumbnail.SmallBitmapSource
				img.Height = 32

				Dim textBlock As New TextBlock()
				textBlock.Background = Brushes.Transparent
				textBlock.FontSize = 10
				textBlock.Margin = New Thickness(4)
				textBlock.VerticalAlignment = VerticalAlignment.Center
				textBlock.Text = obj.Name

				panel.Children.Add(img)
				panel.Children.Add(textBlock)

				panel.Tag = obj

				SearchScopesCombo.Items.Add(panel)


				' Set our initial search scope.
				' If Shell Libraries are supported, search in all the libraries,
				' else, use user's profile (my documents, etc)
				If ShellLibrary.IsPlatformSupported Then
					If obj Is CType(KnownFolders.Libraries, ShellContainer) Then
						SearchScopesCombo.SelectedItem = panel
					End If
				Else
					If obj Is CType(KnownFolders.UsersFiles, ShellContainer) Then
						SearchScopesCombo.SelectedItem = panel
					End If
				End If
			Next obj

			SearchScopesCombo.ToolTip = "Change the scope of the search. Use SearchHomeFolder" & Constants.vbLf & "to search your entire search index."

			AddHandler SearchScopesCombo.SelectionChanged, AddressOf SearchScopesCombo_SelectionChanged

			' Create our help task dialog
			helpTaskDialog = New TaskDialog()
			helpTaskDialog.OwnerWindowHandle = (New WindowInteropHelper(Me)).Handle

			helpTaskDialog.Icon = TaskDialogStandardIcon.Information
			helpTaskDialog.Cancelable = True

			helpTaskDialog.Caption = "Search demo application"
			helpTaskDialog.InstructionText = "Demo application to show the usage of Search APIs"
			helpTaskDialog.Text = "This is a demo application that demonstrates the usage of Search related APIs in the Windows API Code Pack." & Constants.vbLf + Constants.vbLf
			helpTaskDialog.Text &= "The search textbox accepts any search query, including advanced query syntax (AQS) and natural query syntax (NQS)." & Constants.vbLf + Constants.vbLf
			helpTaskDialog.Text &= "Some examples:" & Constants.vbLf
			helpTaskDialog.Text += Constants.vbTab & "AQS - kind:pictures and author:corbis" & Constants.vbLf
			helpTaskDialog.Text += Constants.vbTab & "NQS - all pictures by corbis" & Constants.vbLf
			helpTaskDialog.Text += Constants.vbTab & "AQS - kind:email and from:bill" & Constants.vbLf
			helpTaskDialog.Text += Constants.vbTab & "NQS - emails by bill sent yesterday" & Constants.vbLf + Constants.vbLf
			helpTaskDialog.Text &= "The advanced search dialog shows how to search against some common properties. "
			helpTaskDialog.Text &= " Multiple conditions can be combined together for the search."
			helpTaskDialog.Text += Constants.vbLf + Constants.vbLf & "The sample also demonstrates how to use the strongly typed property system and display some properties for selected files."

			helpTaskDialog.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandContent
			helpTaskDialog.DetailsExpanded = True
			helpTaskDialog.DetailsCollapsedLabel = "Show details"
			helpTaskDialog.DetailsExpandedLabel = "Hide details"
			helpTaskDialog.DetailsExpandedText = "For more information on the Advanced Query Syntax or Natural Query Syntax, visit the following sites:" & Constants.vbLf + Constants.vbLf
			helpTaskDialog.DetailsExpandedText &= "<a href=""http://msdn.microsoft.com/en-us/library/bb266512(VS.85).aspx"">Advanced Query Syntax</a>" & Constants.vbLf
			helpTaskDialog.DetailsExpandedText &= "<a href=""http://www.microsoft.com/windows/products/winfamily/desktopsearch/technicalresources/advquery.mspx"">Windows Search Advanced Query Syntax</a>" & Constants.vbLf

			helpTaskDialog.HyperlinksEnabled = True
			AddHandler helpTaskDialog.HyperlinkClick, AddressOf helpTaskDialog_HyperlinkClick
			helpTaskDialog.FooterText = "Demo application as part of <a href=""http://code.msdn.microsoft.com/WindowsAPICodePack"">Windows API Code Pack for .NET Framework</a>"

		End Sub

		Private Sub SearchScopesCombo_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)
            Dim previousSelection As StackPanel = Nothing

            If e.RemovedItems.Count > 0 Then
                previousSelection = TryCast(e.RemovedItems(0), StackPanel)
            End If


            If SearchScopesCombo.SelectedIndex = 0 Then
                ' Show a folder selection dialog
                Dim cfd As New CommonOpenFileDialog()
                cfd.AllowNonFileSystemItems = True
                cfd.IsFolderPicker = True

                cfd.Title = "Select a folder as your search scope..."

                If cfd.ShowDialog() = CommonFileDialogResult.OK Then
                    Dim container As ShellContainer = TryCast(cfd.FileAsShellObject, ShellContainer)

                    If container IsNot Nothing Then
                        '						#Region "Add it to the bottom of our combobox"
                        Dim panel As New StackPanel()
                        panel.Margin = New Thickness(5, 2, 5, 2)
                        panel.Orientation = Orientation.Horizontal

                        Dim img As New Image()
                        img.Source = container.Thumbnail.SmallBitmapSource
                        img.Height = 32

                        Dim textBlock As New TextBlock()
                        textBlock.Background = Brushes.Transparent
                        textBlock.FontSize = 10
                        textBlock.Margin = New Thickness(4)
                        textBlock.VerticalAlignment = VerticalAlignment.Center
                        textBlock.Text = container.Name

                        panel.Children.Add(img)
                        panel.Children.Add(textBlock)

                        SearchScopesCombo.Items.Add(panel)
                        '						#End Region

                        ' Set our selected scope
                        selectedScope = container
                        SearchScopesCombo.SelectedItem = panel
                    Else
                        SearchScopesCombo.SelectedItem = previousSelection
                    End If
                Else
                    SearchScopesCombo.SelectedItem = previousSelection
                End If
            ElseIf SearchScopesCombo.SelectedItem IsNot Nothing AndAlso TypeOf SearchScopesCombo.SelectedItem Is ShellContainer Then
                selectedScope = TryCast((CType(SearchScopesCombo.SelectedItem, StackPanel)).Tag, ShellContainer)
            End If
        End Sub

		Private Sub timer_Tick(ByVal sender As Object, ByVal e As EventArgs)
			' Using a timer, check if our background search thread is still alive.
			' If not alive, update the cursor
			If backgroundSearchThread IsNot Nothing AndAlso (Not backgroundSearchThread.IsAlive) Then
				Me.Cursor = Cursors.Arrow

				' Also enable the search textbox again
				SearchBox.IsEnabled = True
				buttonSearchAdv.IsEnabled = True
			End If
		End Sub

		Private Sub helpTaskDialog_HyperlinkClick(ByVal sender As Object, ByVal e As TaskDialogHyperlinkClickedEventArgs)
			' Launch the application associated with http links
			Process.Start(e.LinkText)
		End Sub

		Private Sub Window1_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			Dim source As HwndSource = HwndSource.FromHwnd(New WindowInteropHelper(Me).Handle)
			source.AddHook(New HwndSourceHook(AddressOf WndProc))

			Dim tdr As TaskDialogResult = helpTaskDialog.Show()
		End Sub

		Friend Sub Search(ByVal searchItemsList As List(Of SearchItem))
			' Update the listview's itemsource
			listView1.ItemsSource = searchItemsList

			If listView1.Items.Count > 0 Then
				listView1.SelectedIndex = 0
			End If
		End Sub

		#Region "For the Aero glass effect"

		Private Function WndProc(ByVal hwnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr, ByRef handled As Boolean) As IntPtr
			' handle the message for DWM when the aero glass is turned on or off
			If msg = WM_DWMCOMPOSITIONCHANGED Then
				If GlassHelper.IsGlassEnabled Then
					' Extend glass
                    Dim bounds As Rect = VisualTreeHelper.GetDescendantBounds(listView1)
                    GlassHelper.ExtendGlassFrame(Me, New Thickness(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom))
				Else
					' turn off glass...
					GlassHelper.DisableGlassFrame(Me)
				End If

				handled = True
			End If

			Return IntPtr.Zero
		End Function

		Private Sub OnMove(ByVal s As Object, ByVal e As DragDeltaEventArgs)
			left_Renamed += CInt(Fix(e.HorizontalChange))
			top_Renamed += CInt(Fix(e.VerticalChange))
			Me.Left = left_Renamed
			Me.Top = top_Renamed
		End Sub

		Private Sub Window1_SourceInitialized(ByVal sender As Object, ByVal e As EventArgs)
            Dim bounds As Rect = VisualTreeHelper.GetDescendantBounds(listView1)
            GlassHelper.ExtendGlassFrame(Me, New Thickness(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom))
        End Sub

		Protected Overrides Sub OnContentRendered(ByVal e As EventArgs)
			If Me.neverRendered Then
				' The window takes the size of its content because SizeToContent
				' is set to WidthAndHeight in the markup. We then allow
				' it to be set by the user, and have the content take the size
				' of the window.
				Me.SizeToContent = SizeToContent.Manual

				Dim root As FrameworkElement = TryCast(Me.Content, FrameworkElement)
				If root IsNot Nothing Then
					root.Width = Double.NaN
					root.Height = Double.NaN
				End If

				Me.neverRendered = False
			End If

			MyBase.OnContentRendered(e)
		End Sub


		#End Region

		Private Sub SearchTextBox_Search(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If backgroundSearchThread IsNot Nothing Then
				backgroundSearchThread.Abort()
			End If

			' Set the cursor to wait
			Me.Cursor = Cursors.Wait

			' Also disable the search textbox while our search is going on
			SearchBox.IsEnabled = False
			buttonSearchAdv.IsEnabled = False

			' Search... on any letters typed
			If Not String.IsNullOrEmpty(SearchBox.Text) Then
				' Create a background thread to do the search
				backgroundSearchThread = New Thread(New ParameterizedThreadStart(AddressOf DoSimpleSearch))
				' ApartmentState.STA is required for COM
				backgroundSearchThread.SetApartmentState(ApartmentState.STA)
				backgroundSearchThread.Start(SearchBox.Text)
			Else
				listView1.ItemsSource = Nothing ' nothing was typed, or user deleted the search query (clear the list).
			End If
		End Sub

		' Helper method to do the search on a background thread
		Friend Sub DoSimpleSearch(ByVal arg As Object)
			Dim text As String = TryCast(arg, String)

			' Specify a culture for our query.
			Dim cultureInfo As New CultureInfo("en-US")

			Dim searchCondition As SearchCondition = SearchConditionFactory.ParseStructuredQuery(text, cultureInfo)

			' Create a new search folder by setting our search condition and search scope
			' KnownFolders.SearchHome - This is the same scope used by Windows search
			searchFolder = New ShellSearchFolder(searchCondition, selectedScope)

			Dim items As New List(Of SearchItem)()

			Try
				' Because we cannot pass ShellObject or IShellItem (native interface)
				' across multiple threads, creating a helper object and copying the data we need from the ShellObject
				For Each so As ShellObject In searchFolder
					' For each of our ShellObject,
					' create a SearchItem object
					' We will bind these items to the ListView
					Dim item As New SearchItem()
					item.Name = so.Name

					' We must freeze the ImageSource before passing across threads
					Dim thumbnail As BitmapSource = so.Thumbnail.MediumBitmapSource
					thumbnail.Freeze()
					item.Thumbnail = thumbnail

					item.Authors = so.Properties.System.Author.Value
					item.Title = so.Properties.System.Title.Value
					item.Keywords = so.Properties.System.Keywords.Value
					item.Copyright = so.Properties.System.Copyright.Value
					item.TotalPages = If(so.Properties.System.Document.PageCount.Value.HasValue, so.Properties.System.Document.PageCount.Value.Value, 0)
					item.Rating = If(so.Properties.System.SimpleRating.Value.HasValue, CInt(Fix(so.Properties.System.SimpleRating.Value.Value)), 0)
					item.ParsingName = so.ParsingName

					items.Add(item)
				Next so

				' Invoke the search on the main thread

				Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, New Action(Function() AnonymousMethod1(items)))
			Catch
				searchFolder.Dispose()
				searchFolder = Nothing
			End Try
		End Sub
		
		Private Function AnonymousMethod1(ByVal items As List(Of SearchItem)) As Object
			UpdateSearchItems(items)
			Return Nothing
		End Function

		' Updates the items on the listview on the main thread.
		' This method should not be called from a background thread
		Friend Sub UpdateSearchItems(ByVal items As List(Of SearchItem))
			' Update the listview's itemsource
			listView1.ItemsSource = items

			If listView1.Items.Count > 0 Then
				listView1.SelectedIndex = 0
			End If
		End Sub

		Private Sub CommandBinding_Executed(ByVal sender As Object, ByVal e As ExecutedRoutedEventArgs)
			SearchBox.Focus()
		End Sub

		Private Sub buttonSearchAdv_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If advWindow Is Nothing Then
				advWindow = New AdvancedSearch()
				advWindow.MainWindow = Me
				AddHandler advWindow.Closed, AddressOf advWindow_Closed
			End If

			If Not advWindow.IsVisible Then
				advWindow.Show()
			Else
				advWindow.Visibility = Visibility.Visible
				advWindow.Focus()
			End If
		End Sub

		Private Sub advWindow_Closed(ByVal sender As Object, ByVal e As EventArgs)
			advWindow = Nothing
		End Sub

		Private Sub Window_Closed(ByVal sender As Object, ByVal e As EventArgs)
			If advWindow IsNot Nothing Then
				advWindow.Close()
			End If
		End Sub

		Private Sub HelpButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			helpTaskDialog.Show()
		End Sub
	End Class

	''' <summary>
	''' ImageView displays image files using themselves as their icons.
	''' In order to write our own visual tree of a view, we should override its
	''' DefaultStyleKey and ItemContainerDefaultKey. DefaultStyleKey specifies
	''' the style key of ListView; ItemContainerDefaultKey specifies the style
	''' key of ListViewItem.
	''' </summary>
	Public Class ImageView
		Inherits ViewBase
		#Region "DefaultStyleKey"

		Protected Overrides ReadOnly Property DefaultStyleKey() As Object
			Get
				Return New ComponentResourceKey(Me.GetType(), "ImageView")
			End Get
		End Property

		#End Region

		#Region "ItemContainerDefaultStyleKey"

		Protected Overrides ReadOnly Property ItemContainerDefaultStyleKey() As Object
			Get
				Return New ComponentResourceKey(Me.GetType(), "ImageViewItem")
			End Get
		End Property

		#End Region
	End Class

	Public NotInheritable Class CustomCommands
		Public Shared SearchCommand As New RoutedCommand("SearchCommand", GetType(CustomCommands))
	End Class

	''' <summary>
	''' Represents a single item in the search results.
	''' This item will store the file's thumbnail, display name,
	''' and some properties (that will be displayed in the properties pane)
	''' </summary>
	Public Class SearchItem
		Private privateName As String
		Public Property Name() As String
			Get
				Return privateName
			End Get
			Set(ByVal value As String)
				privateName = value
			End Set
		End Property
		Private privateThumbnail As BitmapSource
		Public Property Thumbnail() As BitmapSource
			Get
				Return privateThumbnail
			End Get
			Set(ByVal value As BitmapSource)
				privateThumbnail = value
			End Set
		End Property
		Private privateAuthors As String()
		Public Property Authors() As String()
			Get
				Return privateAuthors
			End Get
			Set(ByVal value As String())
				privateAuthors = value
			End Set
		End Property
		Private privateRating As Integer
		Public Property Rating() As Integer
			Get
				Return privateRating
			End Get
			Set(ByVal value As Integer)
				privateRating = value
			End Set
		End Property
		Private privateCopyright As String
		Public Property Copyright() As String
			Get
				Return privateCopyright
			End Get
			Set(ByVal value As String)
				privateCopyright = value
			End Set
		End Property
		Private privateTotalPages As Integer
		Public Property TotalPages() As Integer
			Get
				Return privateTotalPages
			End Get
			Set(ByVal value As Integer)
				privateTotalPages = value
			End Set
		End Property
		Private privateKeywords As String()
		Public Property Keywords() As String()
			Get
				Return privateKeywords
			End Get
			Set(ByVal value As String())
				privateKeywords = value
			End Set
		End Property
		Private privateTitle As String
		Public Property Title() As String
			Get
				Return privateTitle
			End Get
			Set(ByVal value As String)
				privateTitle = value
			End Set
		End Property
		Private privateParsingName As String
		Public Property ParsingName() As String
			Get
				Return privateParsingName
			End Get
			Set(ByVal value As String)
				privateParsingName = value
			End Set
		End Property
	End Class
End Namespace
