' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports Microsoft.WindowsAPICodePack.Shell.PropertySystem
Imports System.Collections.Generic
Imports System.Windows.Threading
Imports System.Windows.Media.Imaging
Imports System.Threading
Imports System.Windows.Interop
Imports System.Windows.Input

Namespace Microsoft.WindowsAPICodePack.Samples.SearchApp
	''' <summary>
	''' Interaction logic for AdvancedSearch.xaml
	''' </summary>
	Partial Public Class AdvancedSearch
		Inherits Window
		Private stockIcons As StockIcons
		Private documentsStockIcon As StockIcon
		Private picturesStockIcon As StockIcon
		Private musicStockIcon As StockIcon
		Private videosStockIcon As StockIcon

		Friend MainWindow As Window1

		' Background thread for our search
		Private backgroundSearchThread As Thread = Nothing

		Public Sub New()
			stockIcons = New StockIcons()

			documentsStockIcon = stockIcons.DocumentAssociated
			videosStockIcon = stockIcons.VideoFiles
			musicStockIcon = stockIcons.AudioFiles
			picturesStockIcon = stockIcons.ImageFiles

			InitializeComponent()

			' Set our default
			DocumentsRadioButton.IsChecked = True

			' 
			prop1prop2OperationComboBox.SelectedIndex = 0

			' Because the search can take some time, using a background thread.
			' This timer will check if that thread is still alive and accordingly update
			' the cursor
			Dim timer As New DispatcherTimer()
			timer.Interval = New TimeSpan(0, 0, 1)
			timer.IsEnabled = True
			AddHandler timer.Tick, AddressOf timer_Tick
		End Sub

		Private Sub timer_Tick(ByVal sender As Object, ByVal e As EventArgs)
			' Using a timer, check if our background search thread is still alive.
			' If not alive, update the cursor to arrow
			If backgroundSearchThread IsNot Nothing AndAlso (Not backgroundSearchThread.IsAlive) Then
				Me.Cursor = Cursors.Arrow
				MainWindow.Cursor = Cursors.Arrow

				' Also enable the search textbox again
				MainWindow.SearchBox.IsEnabled = True
				MainWindow.buttonSearchAdv.IsEnabled = True
				buttonSearch.IsEnabled = True
				buttonClear.IsEnabled = True
			End If
		End Sub

		Private Sub DocumentsRadioButton_Checked(ByVal sender As Object, ByVal e As RoutedEventArgs)
			UpdateDocumentsSearchSettings()

			documentsStockIcon.Selected = True
			DocumentsRadioButton.Content = New Image With {.Source = documentsStockIcon.BitmapSource}

			picturesStockIcon.Selected = False
			PicturesRadioButton.Content = New Image With {.Source = picturesStockIcon.BitmapSource}

			musicStockIcon.Selected = False
			MusicRadioButton.Content = New Image With {.Source = musicStockIcon.BitmapSource}

			videosStockIcon.Selected = False
			VideosRadioButton.Content = New Image With {.Source = videosStockIcon.BitmapSource}
		End Sub

		Private Sub PicturesRadioButton_Checked(ByVal sender As Object, ByVal e As RoutedEventArgs)
			UpdatePicturesSearchSettings()

			documentsStockIcon.Selected = False
			DocumentsRadioButton.Content = New Image With {.Source = documentsStockIcon.BitmapSource}

			picturesStockIcon.Selected = True
			PicturesRadioButton.Content = New Image With {.Source = picturesStockIcon.BitmapSource}

			musicStockIcon.Selected = False
			MusicRadioButton.Content = New Image With {.Source = musicStockIcon.BitmapSource}

			videosStockIcon.Selected = False
			VideosRadioButton.Content = New Image With {.Source = videosStockIcon.BitmapSource}
		End Sub

		Private Sub MusicRadioButton_Checked(ByVal sender As Object, ByVal e As RoutedEventArgs)
			UpdateMusicSearchSettings()

			documentsStockIcon.Selected = False
			DocumentsRadioButton.Content = New Image With {.Source = documentsStockIcon.BitmapSource}

			picturesStockIcon.Selected = False
			PicturesRadioButton.Content = New Image With {.Source = picturesStockIcon.BitmapSource}

			musicStockIcon.Selected = True
			MusicRadioButton.Content = New Image With {.Source = musicStockIcon.BitmapSource}

			videosStockIcon.Selected = False
			VideosRadioButton.Content = New Image With {.Source = videosStockIcon.BitmapSource}
		End Sub

		Private Sub VideosRadioButton_Checked(ByVal sender As Object, ByVal e As RoutedEventArgs)
			UpdateVideosSearchSettings()

			documentsStockIcon.Selected = False
			DocumentsRadioButton.Content = New Image With {.Source = documentsStockIcon.BitmapSource}

			picturesStockIcon.Selected = False
			PicturesRadioButton.Content = New Image With {.Source = picturesStockIcon.BitmapSource}

			musicStockIcon.Selected = False
			MusicRadioButton.Content = New Image With {.Source = musicStockIcon.BitmapSource}

			videosStockIcon.Selected = True
			VideosRadioButton.Content = New Image With {.Source = videosStockIcon.BitmapSource}
		End Sub

		Private Sub UpdateDocumentsSearchSettings()
			' We are in "documents" mode
			prop1ComboBox.Items.Clear()
			prop1ComboBox.Items.Add("Author")
			prop1ComboBox.Items.Add("Title")
			prop1ComboBox.Items.Add("Comment")
			prop1ComboBox.Items.Add("Copyright")
			prop1ComboBox.Items.Add("Pages")
			prop1ComboBox.Items.Add("Tags/Keywords")
			prop1ComboBox.SelectedIndex = 0

			prop1OperationComboBox.SelectedIndex = 9

			prop1TextBox.Text = ""

			prop2ComboBox.Items.Clear()
			prop2ComboBox.Items.Add("Author")
			prop2ComboBox.Items.Add("Title")
			prop2ComboBox.Items.Add("Comment")
			prop2ComboBox.Items.Add("Copyright")
			prop2ComboBox.Items.Add("Pages")
			prop2ComboBox.Items.Add("Tags/Keywords")
			prop2ComboBox.SelectedIndex = 5

			prop2OperationComboBox.SelectedIndex = 0

			prop2TextBox.Text = ""

			prop1prop2OperationComboBox.SelectedIndex = 0

			comboBoxDateCreated.SelectedIndex = 0

			' locations
			locationsListBox.Items.Clear()

			If ShellLibrary.IsPlatformSupported Then
				AddLocation(CType(KnownFolders.DocumentsLibrary, ShellContainer))
			Else
				AddLocation(CType(KnownFolders.Documents, ShellContainer))
			End If
		End Sub

		Private Sub UpdatePicturesSearchSettings()
			' We are in "Pictures" mode
			prop1ComboBox.Items.Clear()
			prop1ComboBox.Items.Add("Author")
			prop1ComboBox.Items.Add("Subject")
			prop1ComboBox.Items.Add("Camera maker")
			prop1ComboBox.Items.Add("Copyright")
			prop1ComboBox.Items.Add("Rating")
			prop1ComboBox.Items.Add("Tags/Keywords")
			prop1ComboBox.SelectedIndex = 0

			prop1OperationComboBox.SelectedIndex = 9

			prop1TextBox.Text = ""

			prop2ComboBox.Items.Clear()
			prop2ComboBox.Items.Add("Author")
			prop2ComboBox.Items.Add("Subject")
			prop2ComboBox.Items.Add("Camera maker")
			prop2ComboBox.Items.Add("Copyright")
			prop2ComboBox.Items.Add("Rating")
			prop2ComboBox.Items.Add("Tags/Keywords")
			prop2ComboBox.SelectedIndex = 0

			prop2OperationComboBox.SelectedIndex = 0

			prop2TextBox.Text = ""

			prop1prop2OperationComboBox.SelectedIndex = 0

			comboBoxDateCreated.SelectedIndex = 0

			' locations
			locationsListBox.Items.Clear()

			If ShellLibrary.IsPlatformSupported Then
				AddLocation(CType(KnownFolders.PicturesLibrary, ShellContainer))
			Else
				AddLocation(CType(KnownFolders.Pictures, ShellContainer))
			End If

		End Sub

		Private Sub UpdateMusicSearchSettings()
			' We are in "Music" mode
			prop1ComboBox.Items.Clear()
			prop1ComboBox.Items.Add("Album artist")
			prop1ComboBox.Items.Add("Album title")
			prop1ComboBox.Items.Add("Composer")
			prop1ComboBox.Items.Add("Rating")
			prop1ComboBox.Items.Add("Genre")
			prop1ComboBox.Items.Add("Year")
			prop1ComboBox.SelectedIndex = 1

			prop1OperationComboBox.SelectedIndex = 9

			prop1TextBox.Text = ""

			prop2ComboBox.Items.Clear()
			prop2ComboBox.Items.Add("Album artist")
			prop2ComboBox.Items.Add("Album title")
			prop2ComboBox.Items.Add("Composer")
			prop2ComboBox.Items.Add("Rating")
			prop2ComboBox.Items.Add("Genre")
			prop2ComboBox.Items.Add("Year")
			prop2ComboBox.SelectedIndex = 0

			prop2OperationComboBox.SelectedIndex = 0

			prop2TextBox.Text = ""

			prop1prop2OperationComboBox.SelectedIndex = 0

			comboBoxDateCreated.SelectedIndex = 0

			' locations
			locationsListBox.Items.Clear()

			If ShellLibrary.IsPlatformSupported Then
				AddLocation(CType(KnownFolders.MusicLibrary, ShellContainer))
			Else
				AddLocation(CType(KnownFolders.Music, ShellContainer))
			End If
		End Sub

		Private Sub UpdateVideosSearchSettings()
			' We are in "Videos" mode
			prop1ComboBox.Items.Clear()
			prop1ComboBox.Items.Add("Title")
			prop1ComboBox.Items.Add("Video length")
			prop1ComboBox.Items.Add("Comment")
			prop1ComboBox.SelectedIndex = 0

			prop1OperationComboBox.SelectedIndex = 9

			prop1TextBox.Text = ""

			prop2ComboBox.Items.Clear()
			prop1ComboBox.Items.Add("Title")
			prop1ComboBox.Items.Add("Video length")
			prop1ComboBox.Items.Add("Comment")
			prop2ComboBox.SelectedIndex = 0

			prop2OperationComboBox.SelectedIndex = 0

			prop2TextBox.Text = ""

			prop1prop2OperationComboBox.SelectedIndex = 0

			comboBoxDateCreated.SelectedIndex = 0

			' locations
			locationsListBox.Items.Clear()

			If ShellLibrary.IsPlatformSupported Then
				AddLocation(CType(KnownFolders.VideosLibrary, ShellContainer))
			Else
				AddLocation(CType(KnownFolders.Videos, ShellContainer))
			End If
		End Sub

		Private Sub addLocationButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Show CFD and let users pick a folder
			Dim cfd As New CommonOpenFileDialog()
			cfd.AllowNonFileSystemItems = True
			cfd.IsFolderPicker = True
			cfd.Multiselect = True

			If cfd.ShowDialog() = CommonFileDialogResult.OK Then
				' Loop through each "folder" and add it our list
				For Each so As ShellContainer In cfd.FilesAsShellObject
					AddLocation(so)
				Next so
			End If
		End Sub

		Private Sub AddLocation(ByVal so As ShellContainer)
			Dim sp As New StackPanel()
			sp.Orientation = Orientation.Horizontal

			' Add the thumbnail/icon
			Dim img As New Image()

			' Because we might be called from a different thread, freeze the bitmap source once
			' we get it
			Dim smallBitmapSource As BitmapSource = so.Thumbnail.SmallBitmapSource
			smallBitmapSource.Freeze()
			img.Source = smallBitmapSource

			img.Margin = New Thickness(5)
			sp.Children.Add(img)

			' Add the name/title
			Dim tb As New TextBlock()
			tb.Text = so.Name
			tb.Margin = New Thickness(5)
			sp.Children.Add(tb)

			' Set our tag as the shell container user picked...
			sp.Tag = so

			'
			locationsListBox.Items.Add(sp)
		End Sub

		Private Sub removeLocationButton_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If locationsListBox.SelectedItem IsNot Nothing Then
				locationsListBox.Items.Remove(locationsListBox.SelectedItem)
			End If
		End Sub

		Private Sub buttonClear_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			' Reset all the settings on the dialog for the selected search type
			If DocumentsRadioButton.IsChecked.Value Then
				UpdateDocumentsSearchSettings()
			ElseIf PicturesRadioButton.IsChecked.Value Then
				UpdatePicturesSearchSettings()
			ElseIf VideosRadioButton.IsChecked.Value Then
				UpdateVideosSearchSettings()
			ElseIf MusicRadioButton.IsChecked.Value Then
				UpdateMusicSearchSettings()
			End If
		End Sub

		Private Function ParseDate(ByVal toParse As String, ByVal relativeDate As DateTime) As DateTime
			If String.IsNullOrEmpty(toParse) OrElse (Not toParse.StartsWith("date:")) Then
				Throw New ArgumentException()
			End If

			Dim tmpToParse As String = toParse.ToLower().Replace("date:", "")

			Select Case tmpToParse
				Case "a long time ago"
					Dim longTimeAgo As New DateTime(relativeDate.Year - 2, 1, 1, 0, 0, 0)
					Return longTimeAgo
				Case "earlier this year"
					Dim thisYear As New DateTime(relativeDate.Year, 1, 1, 0, 0, 0)
					Return thisYear
				Case "earlier this month"
					Dim thisMonth As New DateTime(relativeDate.Year, relativeDate.Month, 1, 0, 0, 0)
					Return thisMonth
				Case "last week"
					Dim dayOfWeek As DayOfWeek = relativeDate.DayOfWeek
					Dim lastweekSunday As DateTime = relativeDate.AddDays(-1 * CInt(dayOfWeek))
					Return lastweekSunday
				Case "yesterday"
					Dim yesterday As DateTime = relativeDate.AddDays(-1)
					Return yesterday
				Case "earlier this week"
					Dim lastWeek As DateTime = relativeDate.AddDays(-7)
					Return lastWeek
				Case Else
					Throw New ArgumentException()
			End Select
		End Function

		Private Sub buttonSearch_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If MainWindow Is Nothing Then
				Return
			End If

			If backgroundSearchThread IsNot Nothing Then
				backgroundSearchThread.Abort()
			End If

			' Set our cursor to wait
			Me.Cursor = Cursors.Wait
			MainWindow.Cursor = Cursors.Wait

			' Also disable the search textbox while our search is going on
			MainWindow.SearchBox.IsEnabled = False
			MainWindow.buttonSearchAdv.IsEnabled = False
			buttonSearch.IsEnabled = False
			buttonClear.IsEnabled = False
			MainWindow.SearchBox.Clear()

			' Bring the main window in the foreground
			MainWindow.Activate()

			' Create a background thread to do the search
			backgroundSearchThread = New Thread(New ThreadStart(AddressOf DoAdvancedSearch))
			' ApartmentState.STA is required for COM
			backgroundSearchThread.SetApartmentState(ApartmentState.STA)
			backgroundSearchThread.Start()

		End Sub

		Private Sub DoAdvancedSearch()

			' This is our final searchcondition that we'll create the search folder from
			Dim finalSearchCondition As SearchCondition = Nothing

			' This is Prop1 + prop2 search condition... if the user didn't specify one of the properties,
			' we can just use the one property/value they specify...if they do, then we can do the and/or operation
			Dim combinedPropSearchCondition As SearchCondition = Nothing

			' Because we are doing the search on a background thread,
			' we can't access the UI controls from that thread.
			' Invoke from the main UI thread and get the values
			Dim prop1TextBox_Text As String = String.Empty
			Dim prop2TextBox_Text As String = String.Empty
			Dim prop1ComboBox_Value As String = String.Empty
			Dim prop2ComboBox_Value As String = String.Empty
			Dim prop1ConditionOperation As SearchConditionOperation = SearchConditionOperation.ValueContains
			Dim prop2ConditionOperation As SearchConditionOperation = SearchConditionOperation.ValueContains
			Dim prop1prop2OperationComboBox_Value As String = String.Empty
			Dim comboBoxDateCreated_Value As String = String.Empty
			Dim prop1prop2OperationComboBox_SelectedIndex As Integer = 0
			Dim dateSelected As Boolean = False
			Dim scopes As New List(Of ShellContainer)()

			Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, New Action(Function() AnonymousMethod1(prop1TextBox_Text, prop1ComboBox_Value, prop2ComboBox_Value, prop1ConditionOperation, prop2ConditionOperation, prop1prop2OperationComboBox_Value, prop1prop2OperationComboBox_SelectedIndex, comboBoxDateCreated_Value, dateSelected, scopes)))

			' If we have a valid first property/value, then create a search condition
			If Not String.IsNullOrEmpty(prop1TextBox_Text) Then
				Dim prop1Condition As SearchCondition = SearchConditionFactory.CreateLeafCondition(GetSearchProperty(prop1ComboBox_Value), prop1TextBox_Text, prop1ConditionOperation)

				' After creating the first condition, see if we need to create a second leaf condition
				If prop1prop2OperationComboBox_SelectedIndex <> 0 AndAlso Not(String.IsNullOrEmpty(prop2TextBox_Text)) Then
					Dim prop2Condition As SearchCondition = SearchConditionFactory.CreateLeafCondition(GetSearchProperty(prop2ComboBox_Value), prop2TextBox_Text, prop2ConditionOperation)

					' Create our combined search condition AND or OR
					If prop1prop2OperationComboBox.SelectedIndex = 1 Then
						combinedPropSearchCondition = SearchConditionFactory.CreateAndOrCondition(SearchConditionType.And, False, prop1Condition, prop2Condition)
					Else
						combinedPropSearchCondition = SearchConditionFactory.CreateAndOrCondition(SearchConditionType.Or, False, prop1Condition, prop2Condition)
					End If
				Else
					combinedPropSearchCondition = prop1Condition
				End If
			Else
				Return ' no search text entered
			End If

			' Get the date condition
			If dateSelected Then
				Dim dateCreatedCondition As SearchCondition = SearchConditionFactory.CreateLeafCondition(SystemProperties.System.DateCreated, ParseDate((CType(comboBoxDateCreated.SelectedItem, ComboBoxItem)).Tag.ToString(), DateTime.Now), SearchConditionOperation.GreaterThan)

				' If we have a property based search condition, create an "AND" search condition from these 2
				If combinedPropSearchCondition IsNot Nothing Then
					finalSearchCondition = SearchConditionFactory.CreateAndOrCondition(SearchConditionType.And, False, combinedPropSearchCondition, dateCreatedCondition)
				Else
					finalSearchCondition = dateCreatedCondition
				End If
			Else
				finalSearchCondition = combinedPropSearchCondition
			End If


			Dim searchFolder As New ShellSearchFolder(finalSearchCondition, scopes.ToArray())

			'
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

				Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, New Action(Function() AnonymousMethod2(items)))
			Finally
				' TODO - dispose other

				finalSearchCondition.Dispose()
				finalSearchCondition = Nothing

				searchFolder.Dispose()
				searchFolder = Nothing
			End Try
		End Sub
		
		Private Function AnonymousMethod1(ByVal prop1TextBox_Text As String, ByVal prop1ComboBox_Value As String, ByVal prop2ComboBox_Value As String, ByVal prop1ConditionOperation As SearchConditionOperation, ByVal prop2ConditionOperation As SearchConditionOperation, ByVal prop1prop2OperationComboBox_Value As String, ByVal prop1prop2OperationComboBox_SelectedIndex As Integer, ByVal comboBoxDateCreated_Value As String, ByVal dateSelected As Boolean, ByVal scopes As List(Of ShellContainer)) As Object
			prop1TextBox_Text = prop1TextBox.Text
			prop1TextBox_Text = prop1TextBox.Text
			prop1ComboBox_Value = prop1ComboBox.SelectedItem.ToString()
			prop2ComboBox_Value = prop2ComboBox.SelectedItem.ToString()
			prop1ConditionOperation = GetConditionOperation(prop1OperationComboBox)
			prop2ConditionOperation = GetConditionOperation(prop2OperationComboBox)
			prop1prop2OperationComboBox_Value = prop1prop2OperationComboBox.SelectedItem.ToString()
			prop1prop2OperationComboBox_SelectedIndex = prop1prop2OperationComboBox.SelectedIndex
			comboBoxDateCreated_Value = comboBoxDateCreated.SelectedItem.ToString()
			dateSelected = (comboBoxDateCreated.SelectedItem IsNot dateCreatedNone)
			For Each sp As StackPanel In locationsListBox.Items
				If TypeOf sp.Tag Is ShellContainer Then
					scopes.Add(CType(sp.Tag, ShellContainer))
				End If
			Next sp
			Return Nothing
		End Function
		
		Private Function AnonymousMethod2(ByVal items As List(Of SearchItem)) As Object
			MainWindow.UpdateSearchItems(items)
			Return Nothing
		End Function

		Private Function GetSearchProperty(ByVal prop As String) As PropertyKey
			Select Case prop
				Case "Author"
					Return SystemProperties.System.Author
				Case "Title"
					Return SystemProperties.System.Title
				Case "Comment"
					Return SystemProperties.System.Comment
				Case "Copyright"
					Return SystemProperties.System.Copyright
				Case "Pages"
					Return SystemProperties.System.Document.PageCount
				Case "Tags/Keywords"
					Return SystemProperties.System.Keywords
				Case "Subject"
					Return SystemProperties.System.Subject
				Case "Camera maker"
					Return SystemProperties.System.Photo.CameraManufacturer
				Case "Rating"
					Return SystemProperties.System.Rating
				Case "Album artist"
					Return SystemProperties.System.Music.AlbumArtist
				Case "Album title"
					Return SystemProperties.System.Music.AlbumTitle
				Case "Composer"
					Return SystemProperties.System.Music.Composer
				Case "Genre"
					Return SystemProperties.System.Music.Genre
				Case "Video length"
					Return SystemProperties.System.Media.Duration
				Case "Year"
					Return SystemProperties.System.Media.Year
			End Select

			Return SystemProperties.System.Null
		End Function

		Private Function GetConditionOperation(ByVal comboBox As ComboBox) As SearchConditionOperation
			Dim operation As SearchConditionOperation = CType(System.Enum.Parse(GetType(SearchConditionOperation), (CType(comboBox.Items(comboBox.SelectedIndex), ComboBoxItem)).Tag.ToString(), True), SearchConditionOperation)

			Return operation
		End Function

		Private Sub prop1prop2OperationComboBox_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)
			' Based on the And/OR operation between the two properties, enable/disable
			' the second propertie's UI
			If prop1prop2OperationComboBox.SelectedIndex = 0 Then ' (None)
				prop2ComboBox.IsEnabled = False
				prop2OperationComboBox.IsEnabled = False
				prop2TextBox.IsEnabled = False
			Else
				prop2ComboBox.IsEnabled = True
				prop2OperationComboBox.IsEnabled = True
				prop2TextBox.IsEnabled = True
			End If
		End Sub
	End Class
End Namespace
