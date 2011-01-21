' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Diagnostics
Imports Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
Imports Microsoft.WindowsAPICodePack.Shell
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.Dialogs

Namespace Transliterator
    Partial Friend Class Transliterator
        Inherits Form
        Public Const SB_LINEUP As Integer = 0
        Public Const SB_LINEDOWN As Integer = 1
        Public Const SB_PAGEUP As Integer = 2
        Public Const SB_PAGEDOWN As Integer = 3
        Public Const SB_THUMBPOSITION As Integer = 4
        Public Const SB_THUMBTRACK As Integer = 5
        Public Const SB_TOP As Integer = 6
        Public Const SB_BOTTOM As Integer = 7
        Public Const SB_ENDSCROLL As Integer = 8
        Private Const WM_HSCROLL As Integer = &H114
        Private Const WM_VSCROLL As Integer = &H115

        Private Const categoryTransliteration As String = "Transliteration"
        Private transliterationServices() As MappingService = Nothing
        Private guidService? As Guid = Nothing

        <DllImport("user32.dll")> _
        Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
        End Function

        Public Sub New()
            InitializeComponent()

            MinimumSize = Me.Size

            Dim fileName As String = Path.GetFullPath("sample.txt")
            If (File.Exists(fileName)) Then
                textBoxSourceFile.Text = fileName
                textBoxSource.Text = File.ReadAllText(fileName)
            End If

            transliterationServices = GetSpecifiedMappingServices(categoryTransliteration)
            If (transliterationServices IsNot Nothing) AndAlso (transliterationServices.Count() > 0) Then
                For Each ms As MappingService In transliterationServices
                    comboBoxServices.Items.Add(New DataItem() With {.Name = ms.Description, .guid = ms.Guid})
                Next ms
                comboBoxServices.SelectedIndex = 0
            End If

        End Sub

        Private Function GetSpecifiedMappingServices(ByVal CategoryName As String) As MappingService()
            Dim transliterationServices() As MappingService = Nothing
            Try
                Dim enumOptions As New MappingEnumOptions() With {.Category = CategoryName}
                transliterationServices = MappingService.GetServices(enumOptions)
            Catch exc As LinguisticException
                ShowErrorMessage(String.Format("Error calling ELS: {0}, HResult: {1}", exc.ResultState.ErrorMessage, exc.ResultState.HResult))
            End Try
            Return transliterationServices
        End Function

        Private Function LanguageConverter(ByVal serviceGuid As Guid, ByVal sourceContent As String) As String
            Dim transliterated As String = Nothing
            If (sourceContent IsNot Nothing) AndAlso (sourceContent.Length > 0) Then
                Try
                    Dim mapService As New MappingService(serviceGuid)
                    Using bag As MappingPropertyBag = mapService.RecognizeText(sourceContent, Nothing)
                        transliterated = bag.GetResultRanges()(0).FormatData(New StringFormatter())
                    End Using
                Catch exc As LinguisticException
                    ShowErrorMessage(String.Format("Error calling ELS: {0}, HResult: {1}", exc.ResultState.ErrorMessage, exc.ResultState.HResult))
                End Try
            End If
            Return transliterated
        End Function

        Private Sub ShowErrorMessage(ByVal msg As String)
            Dim td As New TaskDialog() With {.StandardButtons = TaskDialogStandardButtons.Close, .Caption = "Error", .InstructionText = msg, .Icon = TaskDialogStandardIcon.Error}

            Dim res As TaskDialogResult = td.Show()
        End Sub


        Private Sub btnBrowse_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnBrowse.Click
            Dim openFileDialog As New CommonOpenFileDialog()
            openFileDialog.AllowNonFileSystemItems = True
            openFileDialog.Title = "Select source file"
            openFileDialog.InitialDirectory = Application.StartupPath
            openFileDialog.Filters.Add(New CommonFileDialogFilter("Text files (*.txt)", "*.txt"))
            openFileDialog.RestoreDirectory = True

            If openFileDialog.ShowDialog() <> CommonFileDialogResult.Cancel Then
                Try
                    textBoxSourceFile.Text = openFileDialog.FileAsShellObject.ParsingName
                    textBoxSource.Text = File.ReadAllText(textBoxSourceFile.Text)
                    textBoxResult.Text = ""
                Catch ex As Exception
                    ShowErrorMessage(ex.Message)
                End Try
            End If
        End Sub

        Private Sub btnConvert_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnConvert.Click
            Try
                Debug.Assert(guidService.HasValue)
                Dim result As String = LanguageConverter(guidService.GetValueOrDefault(), textBoxSource.Text)
                If (result IsNot Nothing) AndAlso (result.Length > 0) Then
                    textBoxResult.Text = result
                End If
            Catch ex As Exception
                ShowErrorMessage(ex.Message)
            End Try
        End Sub

        Private Sub btnHelp_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnHelp.Click
            Dim taskHelp As New TaskDialog()
            taskHelp.Caption = "Help"
            taskHelp.Text = "Steps to use the tool:" & Constants.vbLf + Constants.vbLf
            taskHelp.Text &= "1) Use the Browse button to load the unicode text from a "
            taskHelp.Text &= "text file or copy and paste text "
            taskHelp.Text &= "directly to the text box under the 'Text for conversion:' label," & Constants.vbLf
            taskHelp.Text &= "2) Choose a tranliteration service from the drop down list," & Constants.vbLf
            taskHelp.Text &= "3) Click the Convert button." & Constants.vbLf + Constants.vbLf
            taskHelp.Text &= "This demo uses the Extended Linguistic Services API in the Windows API Code "
            taskHelp.Text &= "Pack for Microsoft .NET Framework."
            taskHelp.DetailsExpandedText = "<a href=""http://code.msdn.microsoft.com/WindowsAPICodePack"">Windows API Code Pack for .NET Framework</a>"

            ' Enable the hyperlinks
            taskHelp.HyperlinksEnabled = True
            AddHandler taskHelp.HyperlinkClick, AddressOf taskHelp_HyperlinkClick

            taskHelp.Cancelable = True

            taskHelp.Icon = TaskDialogStandardIcon.Information
            taskHelp.Show()
        End Sub

        Private Sub btnClose_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnClose.Click
            Me.Close()
        End Sub

        Private Shared Sub taskHelp_HyperlinkClick(ByVal sender As Object, ByVal e As TaskDialogHyperlinkClickedEventArgs)
            ' Launch the application associated with http links
            Process.Start(e.LinkText)
        End Sub

        Private Sub textBoxSource_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles textBoxSource.MouseDown
            If textBoxSource.Capture Then
                textBoxResult.SelectionLength = 0
            End If
        End Sub

        Private Sub textBoxSource_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles textBoxSource.MouseMove
            If (textBoxSource.Capture) AndAlso (textBoxSource.SelectionLength > 0) Then
                textBoxResult.SelectionStart = textBoxSource.SelectionStart
                textBoxResult.SelectionLength = textBoxSource.SelectionLength
            End If
        End Sub

        Private Sub textBoxResult_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles textBoxResult.MouseDown
            If textBoxResult.Capture Then
                textBoxSource.SelectionLength = 0
            End If
        End Sub

        Private Sub textBoxResult_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles textBoxResult.MouseMove
            If (textBoxResult.Capture) AndAlso (textBoxResult.SelectionLength > 0) Then
                textBoxSource.SelectionStart = textBoxResult.SelectionStart
                textBoxSource.SelectionLength = textBoxResult.SelectionLength
            End If
        End Sub

        Private Sub textBoxSource_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles textBoxSource.TextChanged
            ' Enable the "Convert" button only when source text is not empty 
            btnConvert.Enabled = textBoxSource.Text.Length > 0
        End Sub

        Private Sub textBoxSourceFile_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles textBoxSourceFile.KeyDown
            If e.KeyCode = Keys.Enter Then
                Try
                    textBoxSource.Text = File.ReadAllText(textBoxSourceFile.Text)
                Catch ex As Exception
                    ShowErrorMessage(ex.Message)
                End Try
            End If
        End Sub

        Private Sub comboBoxServices_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles comboBoxServices.SelectedIndexChanged
            Dim cb As ComboBox = CType(sender, ComboBox)
            Dim di As DataItem = CType(comboBoxServices.Items(cb.SelectedIndex), DataItem)
            guidService = di.guid
        End Sub

        Private Sub textBoxSource_OnVerticalScroll(ByVal sender As Object, ByVal e As ScrollEventArgs) Handles textBoxSource.OnVerticalScroll
            'Look all the Type you want to control. I've put some here :
            'This type is when you click on the scrollbar buttons up/down.
            If textBoxSource.Capture = True Then
                If e.Type = ScrollEventType.SmallIncrement Then
                    SendMessage(textBoxResult.Handle, WM_VSCROLL, SB_LINEDOWN, 0)
                ElseIf e.Type = ScrollEventType.SmallDecrement Then
                    SendMessage(textBoxResult.Handle, WM_VSCROLL, SB_LINEUP, 0)
                ElseIf e.Type = ScrollEventType.ThumbTrack Then
                    SendMessage(textBoxResult.Handle, WM_VSCROLL, (SB_THUMBTRACK Or (e.NewValue << 16)), 0)
                End If
            End If
        End Sub

        Private Sub textBoxResult_OnVerticalScroll(ByVal sender As Object, ByVal e As ScrollEventArgs) Handles textBoxResult.OnVerticalScroll
            'Look all the Type you want to control. I've put some here :
            'This type is when you click on the scrollbar buttons up/down.
            If textBoxResult.Capture = True Then
                If e.Type = ScrollEventType.SmallIncrement Then
                    SendMessage(textBoxSource.Handle, WM_VSCROLL, SB_LINEDOWN, 0)
                ElseIf e.Type = ScrollEventType.SmallDecrement Then
                    SendMessage(textBoxSource.Handle, WM_VSCROLL, SB_LINEUP, 0)
                ElseIf e.Type = ScrollEventType.ThumbTrack Then
                    SendMessage(textBoxSource.Handle, WM_VSCROLL, (SB_THUMBTRACK Or (e.NewValue << 16)), 0)
                End If
            End If
        End Sub

    End Class

	Friend Class DataItem
		Inherits System.Object
		Private privateguid As Guid
		Public Property guid() As Guid
			Get
				Return privateguid
			End Get
			Set(ByVal value As Guid)
				privateguid = value
			End Set
		End Property
		Private privateName As String
		Public Property Name() As String
			Get
				Return privateName
			End Get
			Set(ByVal value As String)
				privateName = value
			End Set
		End Property

		Public Overrides Function ToString() As String
			Return Name
		End Function
	End Class

End Namespace
