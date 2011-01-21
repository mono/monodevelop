' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Controls
Imports Microsoft.WindowsAPICodePack.Shell.PropertySystem

Namespace Microsoft.WindowsAPICodePack.Samples.PicturePropertiesEditor
    ''' <summary>
    ''' Interaction logic for Window1.xaml
    ''' </summary>
    Partial Public Class Window1
        Inherits Window
        Private Shared MultipleValuesText As String = "(Multiple values)"

        Public Sub New()
            InitializeComponent()

            ' Set the initial location for the explorer browser
            Me.ExplorerBrowser1.NavigationTarget = CType(KnownFolders.SamplePictures, ShellObject)

            AddHandler tabControl1.SelectionChanged, AddressOf tabControl1_SelectionChanged
            AddHandler ResortBtn.Click, AddressOf ResortBtn_Click
        End Sub

        Private Sub ResortBtn_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            If ListBoxThumbnails.Items.Count <= 1 Then
                Return
            End If

            ' Anytime user tries to select an item,
            ' rotate through the items...
            Dim obj As Object = ListBoxThumbnails.Items.GetItemAt(ListBoxThumbnails.Items.Count - 1)

            If obj IsNot Nothing Then
                ListBoxThumbnails.Items.RemoveAt(ListBoxThumbnails.Items.Count - 1)
                ListBoxThumbnails.Items.Insert(0, obj)
            End If
        End Sub

        Private Sub tabControl1_SelectionChanged(ByVal sender As Object, ByVal e As SelectionChangedEventArgs)
            If Me.tabControl1.SelectedIndex = 1 Then
                SetupExplorerBrowser2()
            End If
        End Sub

        Private Sub SetupExplorerBrowser2()
            ExplorerBrowser2.SingleSelection = False
            ExplorerBrowser2.PreviewPane = PaneVisibilityState.Hide
            ExplorerBrowser2.QueryPane = PaneVisibilityState.Hide
            ExplorerBrowser2.NavigationPane = PaneVisibilityState.Hide
            ExplorerBrowser2.CommandsOrganizePane = PaneVisibilityState.Hide
            ExplorerBrowser2.CommandsViewPane = PaneVisibilityState.Hide
            ExplorerBrowser2.DetailsPane = PaneVisibilityState.Hide
            ExplorerBrowser2.NoHeaderInAllViews = True
            ExplorerBrowser2.NoColumnHeader = True
            'ExplorerBrowser2.NoSubfolders = true;
            ExplorerBrowser2.AdvancedQueryPane = PaneVisibilityState.Hide
            ExplorerBrowser2.CommandsPane = PaneVisibilityState.Hide
            ExplorerBrowser2.FullRowSelect = True
            ExplorerBrowser2.ViewMode = ExplorerBrowserViewMode.Tile

            AddHandler ExplorerBrowser2.ExplorerBrowserControl.SelectionChanged, AddressOf ExplorerBrowserControl_SelectionChanged

            ExplorerBrowser2.NavigationTarget = CType(KnownFolders.SamplePictures, ShellObject)
        End Sub

        Private Sub ExplorerBrowserControl_SelectionChanged(ByVal sender As Object, ByVal e As EventArgs)
            ' When the user selects items from the explorer browser control, update the various properties
            UpdateProperties()
        End Sub

        Private Sub UpdateProperties()
            If ExplorerBrowser2.SelectedItems.Count > 1 Then ' multiple items
                ' Thumbnail
                ListBoxThumbnails.Items.Clear()

                For Each so As ShellObject In ExplorerBrowser2.SelectedItems
                    Dim img As New Image()
                    img.Height = 200
                    img.Width = 200
                    img.Source = so.Thumbnail.LargeBitmapSource

                    ListBoxThumbnails.Items.Add(img)
                Next so

                '				#Region "Description properties"

                ' Title
                TextBoxTitle.Text = TryCast(GetPropertyValue("System.Title"), String)

                ' Subject
                TextBoxSubject.Text = TryCast(GetPropertyValue("System.Subject"), String)

                ' Rating
                Dim ratingObj As Object = Nothing

                ratingObj = GetPropertyValue("System.SimpleRating")

                If ratingObj Is Nothing OrElse ratingObj.ToString() = MultipleValuesText Then
                    RatingValueControl.RatingValue = 0
                Else
                    Dim rating? As UInt32 = CType(ratingObj, UInt32?)
                    RatingValueControl.RatingValue = If(rating.HasValue, Convert.ToInt32(rating.Value), 0)
                End If

                ' Tags / Keywords
                ' TODO - We could probably loop through the tags for each file and if they are same,
                ' display them. For now, just treating them all being different values
                ListBoxTags.Items.Clear()
                ListBoxTags.Items.Add(MultipleValuesText)

                ' Comments
                TextBoxComments.Text = TryCast(GetPropertyValue("System.Comment"), String)

                '				#End Region

                '				#Region "Origin Properties"

                ' Authors
                ' TODO - We could probably loop through the tags for each file and if they are same,
                ' display them. For now, just treating them all being different values
                ListBoxAuthors.Items.Clear()
                ListBoxAuthors.Items.Add(MultipleValuesText)

                ' Date Taken
                TextBoxDateTaken.Text = TryCast(GetPropertyValue("System.Photo.DateTaken"), String)

                ' Date Acquired
                TextBoxDateAcquired.Text = TryCast(GetPropertyValue("System.DateAcquired"), String)

                ' Copyright
                TextBoxCopyright.Text = TryCast(GetPropertyValue("System.Copyright"), String)

                '				#End Region

                '				#Region "Image Properties"

                ' Dimensions
                TextBoxDimensions.Text = TryCast(GetPropertyValue("System.Image.Dimensions"), String)

                ' Horizontal Resolution
                TextBoxHorizontalResolution.Text = GetPropertyValue("System.Image.HorizontalResolution").ToString()

                ' Vertical Resolution 
                TextBoxVerticalResolution.Text = GetPropertyValue("System.Image.VerticalResolution").ToString()

                ' Bit Depth
                TextBoxBitDepth.Text = GetPropertyValue("System.Image.BitDepth").ToString()

                '				#End Region

            ElseIf ExplorerBrowser2.SelectedItems.Count = 1 Then ' only one item
                Dim so As ShellObject = ExplorerBrowser2.SelectedItems(0)

                ' Thumbnail
                ListBoxThumbnails.Items.Clear()
                Dim img As New Image()
                img.Width = 200
                img.Height = img.Width
                img.Stretch = Stretch.Fill
                img.Source = so.Thumbnail.LargeBitmapSource
                ListBoxThumbnails.Items.Add(img)

                '				#Region "Description properties"

                ' Title
                TextBoxTitle.Text = so.Properties.System.Title.Value

                ' Subject
                TextBoxSubject.Text = so.Properties.System.Subject.Value

                ' Rating
                RatingValueControl.RatingValue = If(so.Properties.System.SimpleRating.Value.HasValue, CInt(Fix(so.Properties.System.SimpleRating.Value)), 0)

                ' Tags / Keywords
                ListBoxTags.Items.Clear()
                If (so.Properties.System.Keywords.Value Is Nothing) Then
                Else
                    For Each tag As String In so.Properties.System.Keywords.Value
                        ListBoxTags.Items.Add(tag)
                    Next
                End If

                ' Comments
                TextBoxComments.Text = so.Properties.System.Comment.Value

                '				#End Region

                '				#Region "Origin Properties"

                ' Authors
                ListBoxAuthors.Items.Clear()
                If so.Properties.System.Author.Value Is Nothing Then
                Else
                    For Each author As String In so.Properties.System.Author.Value
                        ListBoxAuthors.Items.Add(author)
                    Next
                End If


                ' Date Taken
                TextBoxDateTaken.Text = If(so.Properties.System.Photo.DateTaken.Value.HasValue, so.Properties.System.Photo.DateTaken.Value.Value.ToShortDateString(), "")

                ' Date Acquired
                TextBoxDateAcquired.Text = If(so.Properties.System.DateAcquired.Value.HasValue, so.Properties.System.DateAcquired.Value.Value.ToShortDateString(), "")

                ' Copyright
                TextBoxCopyright.Text = so.Properties.System.Copyright.Value

                '				#End Region

                '				#Region "Image Properties"

                ' Dimensions
                TextBoxDimensions.Text = so.Properties.System.Image.Dimensions.Value

                ' Horizontal Resolution
                TextBoxHorizontalResolution.Text = so.Properties.System.Image.HorizontalResolution.Value.ToString()

                ' Vertical Resolution 
                TextBoxVerticalResolution.Text = so.Properties.System.Image.VerticalResolution.Value.ToString()

                ' Bit Depth
                TextBoxBitDepth.Text = so.Properties.System.Image.BitDepth.Value.ToString()

                '				#End Region
                End If
        End Sub

        Private Function GetPropertyValue(ByVal [property] As String) As Object
            Dim returnValue As Object = Nothing

            For Each so As ShellObject In ExplorerBrowser2.SelectedItems
                Dim objValue As Object = so.Properties.GetProperty([property]).ValueAsObject

                If returnValue Is Nothing Then
                    returnValue = objValue
                ElseIf objValue Is Nothing OrElse (returnValue.ToString() <> objValue.ToString()) Then
                    returnValue = MultipleValuesText
                    Exit For ' if the values differ, than break and use the multiple values text;
                End If
            Next so

            Return returnValue
        End Function

        Private Sub buttonCancel_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            ' reset (cancel the user's changes)
            UpdateProperties()
        End Sub

        Private Sub buttonSave_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
            ' Save
            ' depending on what we have selected ...
            If ExplorerBrowser2.SelectedItems.Count = 1 Then

            ElseIf ExplorerBrowser2.SelectedItems.Count > 1 Then
                For Each so As ShellObject In ExplorerBrowser2.SelectedItems
                    ' Get the property writer for each file
                    Using sw As ShellPropertyWriter = so.Properties.GetPropertyWriter()
                        ' Write the same set of property values for each file, since the user has selected
                        ' multiple files.
                        ' ignore the ones that aren't changed...

                        '						#Region "Description Properties"

                        ' Title
                        If TextBoxTitle.Text <> MultipleValuesText Then
                            sw.WriteProperty(so.Properties.System.Title, If((Not String.IsNullOrEmpty(TextBoxTitle.Text)), TextBoxTitle.Text, Nothing))
                        End If

                        ' Subject
                        If TextBoxSubject.Text <> MultipleValuesText Then
                            sw.WriteProperty(so.Properties.System.Subject, If((Not String.IsNullOrEmpty(TextBoxSubject.Text)), TextBoxSubject.Text, Nothing))
                        End If

                        ' Rating
                        If RatingValueControl.RatingValue <> 0 Then
                            sw.WriteProperty(so.Properties.System.SimpleRating, Convert.ToUInt32(RatingValueControl.RatingValue))
                        End If

                        ' Tags / Keywords
                        ' read-only property for now

                        ' Comments
                        If TextBoxComments.Text <> MultipleValuesText Then
                            sw.WriteProperty(so.Properties.System.Comment, If((Not String.IsNullOrEmpty(TextBoxComments.Text)), TextBoxComments.Text, Nothing))
                        End If

                        '						#End Region


                        '						#Region "Origin Properties"

                        ' Authors
                        ' read-only property for now

                        ' Date Taken
                        ' read-only property for now

                        ' Date Acquired
                        ' read-only property for now

                        ' Copyright
                        If TextBoxCopyright.Text <> MultipleValuesText Then
                            sw.WriteProperty(so.Properties.System.Copyright, If((Not String.IsNullOrEmpty(TextBoxCopyright.Text)), TextBoxCopyright.Text, Nothing))
                        End If

                        '						#End Region

                        '						#Region "Image Properties"

                        ' Dimensions
                        ' Read-only property

                        ' Horizontal Resolution
                        ' Read-only property

                        ' Vertical Resolution 
                        ' Read-only property

                        ' Bit Depth
                        ' Read-only property

                        '						#End Region

                    End Using
                Next so
            End If
        End Sub
    End Class
End Namespace
