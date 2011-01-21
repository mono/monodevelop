' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.Windows
Imports Microsoft.WindowsAPICodePack.Dialogs
Imports Microsoft.WindowsAPICodePack.Dialogs.Controls

Namespace Microsoft.WindowsAPICodePack.Samples.Dialogs
    ''' <summary>
    ''' Interaction logic for Window1.xaml
    ''' </summary>

    Partial Public Class Window1
        Inherits Window
        ' the currently selected dialog, used for getting controls 
        Private currentFileDialog As CommonFileDialog

        Private saveDialogGuid As New Guid("4CAC5C25-0550-45c1-969C-CE4C68A2664D")
        Private openDialogGuid As New Guid("C21EA2FA-5F70-42ad-A8AC-838266134584")

        Private openFileDialog As New CommonOpenFileDialog()

        Public Sub New()
            InitializeComponent()
        End Sub

        #Region "File Dialog Handlers and Helpers"

        Private Sub SaveFileDialogCustomizationXamlClicked(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Dim saveFileDialog As CommonSaveFileDialog = FindSaveFileDialog("CustomSaveFileDialog")
            saveFileDialog.CookieIdentifier = saveDialogGuid

            saveFileDialog.Filters.Add(New CommonFileDialogFilter("My App Type", "*.xyz"))
            saveFileDialog.DefaultExtension = "xyz"
            saveFileDialog.AlwaysAppendDefaultExtension = True

            saveFileDialog.Controls("textName").Text = Environment.UserName

            currentFileDialog = saveFileDialog

            Dim result As CommonFileDialogResult = saveFileDialog.ShowDialog()
            If result = CommonFileDialogResult.Ok Then
                Dim output As String = "File Selected: " & saveFileDialog.FileName & Environment.NewLine
                output &= Environment.NewLine & GetCustomControlValues()

                MessageBox.Show(output, "Save File Dialog Result", MessageBoxButton.OK, MessageBoxImage.Information)
            End If
        End Sub

        Private Function FindSaveFileDialog(ByVal name As String) As CommonSaveFileDialog
            Return TryCast(FindResource(name), CommonSaveFileDialog)
        End Function

        Private Sub OpenFileDialogCustomizationClicked(ByVal sender As Object, ByVal e As RoutedEventArgs)
            Dim openFileDialog As New CommonOpenFileDialog()
            currentFileDialog = openFileDialog

            ApplyOpenDialogSettings(openFileDialog)

            ' set the 'allow multi-select' flag
            openFileDialog.Multiselect = True

            openFileDialog.EnsureFileExists = True

            AddOpenFileDialogCustomControls(openFileDialog)

            Dim result As CommonFileDialogResult = openFileDialog.ShowDialog()
            If result = CommonFileDialogResult.Ok Then
                Dim output As String = ""
                For Each fileName As String In openFileDialog.FileNames
                    output &= fileName & Environment.NewLine
                Next fileName

                output &= Environment.NewLine & GetCustomControlValues()

                MessageBox.Show(output, "Files Chosen", MessageBoxButton.OK, MessageBoxImage.Information)
            End If
        End Sub

        Private Sub AddOpenFileDialogCustomControls(ByVal openDialog As CommonFileDialog)
            ' Add a RadioButtonList
            Dim list As New CommonFileDialogRadioButtonList("radioButtonOptions")
            list.Items.Add(New CommonFileDialogRadioButtonListItem("Option A"))
            list.Items.Add(New CommonFileDialogRadioButtonListItem("Option B"))
            AddHandler list.SelectedIndexChanged, AddressOf RBLOptions_SelectedIndexChanged
            list.SelectedIndex = 1
            openDialog.Controls.Add(list)

            ' Create a groupbox
            Dim groupBox As New CommonFileDialogGroupBox("Options")

            ' Create and add two check boxes to this group
            Dim checkA As New CommonFileDialogCheckBox("chkOptionA", "Option A", False)
            Dim checkB As New CommonFileDialogCheckBox("chkOptionB", "Option B", True)
            AddHandler checkA.CheckedChanged, AddressOf ChkOptionA_CheckedChanged
            AddHandler checkB.CheckedChanged, AddressOf ChkOptionB_CheckedChanged
            groupBox.Items.Add(checkA)
            groupBox.Items.Add(checkB)

            ' Create and add a separator to this group
            openDialog.Controls.Add(New CommonFileDialogSeparator())

            ' Add groupbox to dialog
            openDialog.Controls.Add(groupBox)

            ' Add a Menu
            Dim menu As New CommonFileDialogMenu("menu","Sample Menu")
            Dim itemA As New CommonFileDialogMenuItem("Menu Item 1")
            Dim itemB As New CommonFileDialogMenuItem("Menu Item 2")
            AddHandler itemA.Click, AddressOf MenuOptionA_Click
            AddHandler itemB.Click, AddressOf MenuOptionA_Click
            menu.Items.Add(itemA)
            menu.Items.Add(itemB)
            openDialog.Controls.Add(menu)

            ' Add a ComboBox
            Dim comboBox As New CommonFileDialogComboBox("comboBox")
            AddHandler comboBox.SelectedIndexChanged, AddressOf ComboEncoding_SelectedIndexChanged
            comboBox.Items.Add(New CommonFileDialogComboBoxItem("Combobox Item 1"))
            comboBox.Items.Add(New CommonFileDialogComboBoxItem("Combobox Item 2"))
            comboBox.SelectedIndex = 1
            openDialog.Controls.Add(comboBox)

            ' Create and add a separator
            openDialog.Controls.Add(New CommonFileDialogSeparator())

            ' Add a TextBox
            openDialog.Controls.Add(New CommonFileDialogLabel("Name:"))
            openDialog.Controls.Add(New CommonFileDialogTextBox("textName", Environment.UserName))

            ' Create and add a button to this group
            Dim btnCFDPushButton As New CommonFileDialogButton("Check Name")
            AddHandler btnCFDPushButton.Click, AddressOf PushButton_Click
            openDialog.Controls.Add(btnCFDPushButton)
        End Sub

        Private Sub ApplyOpenDialogSettings(ByVal openFileDialog As CommonFileDialog)
            openFileDialog.Title = "Custom Open File Dialog"

            openFileDialog.CookieIdentifier = openDialogGuid

            ' Add some standard filters.
            openFileDialog.Filters.Add(CommonFileDialogStandardFilters.TextFiles)
            openFileDialog.Filters.Add(CommonFileDialogStandardFilters.OfficeFiles)
            openFileDialog.Filters.Add(CommonFileDialogStandardFilters.PictureFiles)
        End Sub

        Private Function GetCustomControlValues() As String
            Dim values As String = "Custom Cotnrols Values:" & Environment.NewLine

            Dim list As CommonFileDialogRadioButtonList = TryCast(currentFileDialog.Controls("radioButtonOptions"), CommonFileDialogRadioButtonList)
            values &= String.Format("Radio Button List: Total Options = {0}; Selected Option = ""{1}""; Selected Option Index = {2}", list.Items.Count, list.Items(list.SelectedIndex).Text, list.SelectedIndex) & Environment.NewLine

            Dim combo As CommonFileDialogComboBox = TryCast(currentFileDialog.Controls("comboBox"), CommonFileDialogComboBox)
            values &= String.Format("Combo Box: Total Items = {0}; Selected Item = ""{1}""; Selected Item Index = {2}", combo.Items.Count, combo.Items(combo.SelectedIndex).Text, combo.SelectedIndex) & Environment.NewLine

            Dim checkBox As CommonFileDialogCheckBox = TryCast(currentFileDialog.Controls("chkOptionA"), CommonFileDialogCheckBox)
            values &= String.Format("Check Box ""{0}"" is {1}", checkBox.Text,If(checkBox.IsChecked, "Checked", "Unchecked")) & Environment.NewLine

            checkBox = TryCast(currentFileDialog.Controls("chkOptionB"), CommonFileDialogCheckBox)
            values &= String.Format("Check Box ""{0}"" is {1}", checkBox.Text,If(checkBox.IsChecked, "Checked", "Unchecked")) & Environment.NewLine

            Dim textBox As CommonFileDialogTextBox = TryCast(currentFileDialog.Controls("textName"), CommonFileDialogTextBox)
            values &= String.Format("TextBox ""Name"" = {0}", textBox.Text)

            Return values
        End Function
        #End Region

        #Region "Custom controls event handlers"

        Private Sub RBLOptions_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
            Dim list As CommonFileDialogRadioButtonList = TryCast(currentFileDialog.Controls("radioButtonOptions"), CommonFileDialogRadioButtonList)
            MessageBox.Show(String.Format("Total Options = {0}; Selected Option = {1}; Selected Option Index = {2}", list.Items.Count, list.Items(list.SelectedIndex).Text, list.SelectedIndex))
        End Sub

        Private Sub PushButton_Click(ByVal sender As Object, ByVal e As EventArgs)
            Dim textBox As CommonFileDialogTextBox = TryCast(currentFileDialog.Controls("textName"), CommonFileDialogTextBox)
            MessageBox.Show(String.Format("""Check Name"" Button Clicked; Name = {0}", textBox.Text))
        End Sub

        Private Sub ComboEncoding_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
            Dim combo As CommonFileDialogComboBox = TryCast(currentFileDialog.Controls("comboBox"), CommonFileDialogComboBox)
            MessageBox.Show(String.Format("Combo box sel index changed: Total Items = {0}; Selected Index = {1}; Selected Item = {2}", combo.Items.Count, combo.SelectedIndex, combo.Items(combo.SelectedIndex).Text))
        End Sub

        Private Sub ChkOptionA_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs)
            Dim checkBox As CommonFileDialogCheckBox = TryCast(currentFileDialog.Controls("chkOptionA"), CommonFileDialogCheckBox)
            MessageBox.Show(String.Format("Check Box ""{0}"" has been {1}", checkBox.Text,If(checkBox.IsChecked, "Checked", "Unchecked")))
        End Sub

        Private Sub ChkOptionB_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs)
            Dim checkBox As CommonFileDialogCheckBox = TryCast(currentFileDialog.Controls("chkOptionB"), CommonFileDialogCheckBox)
            MessageBox.Show(String.Format("Check Box  ""{0}""  has been {1}", checkBox.Text,If(checkBox.IsChecked, "Checked", "Unchecked")))
        End Sub

        Private Sub MenuOptionA_Click(ByVal sender As Object, ByVal e As EventArgs)
            Dim menu As CommonFileDialogMenu = TryCast(currentFileDialog.Controls("menu"), CommonFileDialogMenu)
            MessageBox.Show(String.Format("Menu ""{0}"" : Item ""{1}"" selected.", menu.Text, menu.Items(0).Text))
        End Sub

        Private Sub MenuOptionB_Click(ByVal sender As Object, ByVal e As EventArgs)
            Dim menu As CommonFileDialogMenu = TryCast(currentFileDialog.Controls("menu"), CommonFileDialogMenu)
            MessageBox.Show(String.Format("Menu ""{0}"" : Item ""{1}"" selected.", menu.Text, menu.Items(1).Text))
        End Sub

        #End Region ' Custom controls event handlers

    End Class
End Namespace
