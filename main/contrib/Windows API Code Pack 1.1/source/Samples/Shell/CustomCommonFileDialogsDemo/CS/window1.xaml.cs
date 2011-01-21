// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;

namespace Microsoft.WindowsAPICodePack.Samples.Dialogs
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class Window1 : Window
    {
        // the currently selected dialog, used for getting controls 
        private CommonFileDialog currentFileDialog;

        private Guid saveDialogGuid = new Guid("4CAC5C25-0550-45c1-969C-CE4C68A2664D");
        private Guid openDialogGuid = new Guid("C21EA2FA-5F70-42ad-A8AC-838266134584");

        private CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();

        public Window1()
        {
            InitializeComponent();
        }

        #region File Dialog Handlers and Helpers

        private void SaveFileDialogCustomizationXamlClicked(object sender, RoutedEventArgs e)
        {
            CommonSaveFileDialog saveFileDialog = FindSaveFileDialog("CustomSaveFileDialog");
            saveFileDialog.CookieIdentifier = saveDialogGuid;

            saveFileDialog.Filters.Add(new CommonFileDialogFilter("My App Type", "*.xyz"));
            saveFileDialog.DefaultExtension = "xyz";
            saveFileDialog.AlwaysAppendDefaultExtension = true;

            saveFileDialog.Controls["textName"].Text = Environment.UserName;

            currentFileDialog = saveFileDialog;

            CommonFileDialogResult result = saveFileDialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                string output = "File Selected: " + saveFileDialog.FileName + Environment.NewLine;
                output += Environment.NewLine + GetCustomControlValues();

                MessageBox.Show(output, "Save File Dialog Result", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private CommonSaveFileDialog FindSaveFileDialog(string name)
        {
            return FindResource(name) as CommonSaveFileDialog;
        }

        private void OpenFileDialogCustomizationClicked(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog();
            currentFileDialog = openFileDialog;

            ApplyOpenDialogSettings(openFileDialog);
            
            // set the 'allow multi-select' flag
            openFileDialog.Multiselect = true;

            openFileDialog.EnsureFileExists = true;

            AddOpenFileDialogCustomControls(openFileDialog);

            CommonFileDialogResult result = openFileDialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                string output = "";
                foreach (string fileName in openFileDialog.FileNames)
                {
                    output += fileName + Environment.NewLine;
                }
                
                output += Environment.NewLine + GetCustomControlValues();

                MessageBox.Show(output, "Files Chosen", MessageBoxButton.OK, MessageBoxImage.Information );
            }
        }

        private void AddOpenFileDialogCustomControls(CommonFileDialog openDialog)
        {
            // Add a RadioButtonList
            CommonFileDialogRadioButtonList list = new CommonFileDialogRadioButtonList("radioButtonOptions");
            list.Items.Add(new CommonFileDialogRadioButtonListItem("Option A"));
            list.Items.Add(new CommonFileDialogRadioButtonListItem("Option B"));
            list.SelectedIndexChanged += RBLOptions_SelectedIndexChanged;
            list.SelectedIndex = 1;
            openDialog.Controls.Add(list);

            // Create a groupbox
            CommonFileDialogGroupBox groupBox = new CommonFileDialogGroupBox("Options");

            // Create and add two check boxes to this group
            CommonFileDialogCheckBox checkA = new CommonFileDialogCheckBox("chkOptionA", "Option A", false);
            CommonFileDialogCheckBox checkB = new CommonFileDialogCheckBox("chkOptionB", "Option B", true);
            checkA.CheckedChanged += ChkOptionA_CheckedChanged;
            checkB.CheckedChanged += ChkOptionB_CheckedChanged;
            groupBox.Items.Add(checkA);
            groupBox.Items.Add(checkB);

            // Create and add a separator to this group
            openDialog.Controls.Add(new CommonFileDialogSeparator());

            // Add groupbox to dialog
            openDialog.Controls.Add(groupBox);

            // Add a Menu
            CommonFileDialogMenu menu = new CommonFileDialogMenu("menu","Sample Menu");
            CommonFileDialogMenuItem itemA = new CommonFileDialogMenuItem("Menu Item 1");
            CommonFileDialogMenuItem itemB = new CommonFileDialogMenuItem("Menu Item 2");
            itemA.Click += MenuOptionA_Click;
            itemB.Click += MenuOptionA_Click;
            menu.Items.Add(itemA);
            menu.Items.Add(itemB);
            openDialog.Controls.Add(menu);

            // Add a ComboBox
            CommonFileDialogComboBox comboBox = new CommonFileDialogComboBox("comboBox");
            comboBox.SelectedIndexChanged += ComboEncoding_SelectedIndexChanged;
            comboBox.Items.Add(new CommonFileDialogComboBoxItem("Combobox Item 1"));
            comboBox.Items.Add(new CommonFileDialogComboBoxItem("Combobox Item 2"));
            comboBox.SelectedIndex = 1;
            openDialog.Controls.Add(comboBox);

            // Create and add a separator
            openDialog.Controls.Add(new CommonFileDialogSeparator());

            // Add a TextBox
            openDialog.Controls.Add(new CommonFileDialogLabel("Name:"));
            openDialog.Controls.Add(new CommonFileDialogTextBox("textName", Environment.UserName));

            // Create and add a button to this group
            CommonFileDialogButton btnCFDPushButton = new CommonFileDialogButton("Check Name");
            btnCFDPushButton.Click += PushButton_Click;
            openDialog.Controls.Add(btnCFDPushButton);
        }

        private void ApplyOpenDialogSettings(CommonFileDialog openFileDialog)
        {
            openFileDialog.Title = "Custom Open File Dialog";

            openFileDialog.CookieIdentifier = openDialogGuid;
            
            // Add some standard filters.
            openFileDialog.Filters.Add(CommonFileDialogStandardFilters.TextFiles);
            openFileDialog.Filters.Add(CommonFileDialogStandardFilters.OfficeFiles);
            openFileDialog.Filters.Add(CommonFileDialogStandardFilters.PictureFiles);
        }

        private string GetCustomControlValues()
        {
            string values = "Custom Cotnrols Values:" + Environment.NewLine;

            CommonFileDialogRadioButtonList list = currentFileDialog.Controls["radioButtonOptions"] as CommonFileDialogRadioButtonList;
            values += String.Format("Radio Button List: Total Options = {0}; Selected Option = \"{1}\"; Selected Option Index = {2}", list.Items.Count, list.Items[list.SelectedIndex].Text, list.SelectedIndex) + Environment.NewLine;

            CommonFileDialogComboBox combo = currentFileDialog.Controls["comboBox"] as CommonFileDialogComboBox;
            values += String.Format("Combo Box: Total Items = {0}; Selected Item = \"{1}\"; Selected Item Index = {2}", combo.Items.Count, combo.Items[combo.SelectedIndex].Text, combo.SelectedIndex) + Environment.NewLine;

            CommonFileDialogCheckBox checkBox = currentFileDialog.Controls["chkOptionA"] as CommonFileDialogCheckBox;
            values += String.Format("Check Box \"{0}\" is {1}", checkBox.Text, checkBox.IsChecked ? "Checked" : "Unchecked") + Environment.NewLine;

            checkBox = currentFileDialog.Controls["chkOptionB"] as CommonFileDialogCheckBox;
            values += String.Format("Check Box \"{0}\" is {1}", checkBox.Text, checkBox.IsChecked ? "Checked" : "Unchecked") + Environment.NewLine;

            CommonFileDialogTextBox textBox = currentFileDialog.Controls["textName"] as CommonFileDialogTextBox;
            values += String.Format("TextBox \"Name\" = {0}", textBox.Text);

            return values;
        }
        #endregion

        #region Custom controls event handlers

        private void RBLOptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            CommonFileDialogRadioButtonList list = currentFileDialog.Controls["radioButtonOptions"] as CommonFileDialogRadioButtonList;
            MessageBox.Show(String.Format("Total Options = {0}; Selected Option = {1}; Selected Option Index = {2}", list.Items.Count, list.Items[list.SelectedIndex].Text, list.SelectedIndex));
        }

        private void PushButton_Click(object sender, EventArgs e)
        {
            CommonFileDialogTextBox textBox = currentFileDialog.Controls["textName"] as CommonFileDialogTextBox;
            MessageBox.Show(String.Format("\"Check Name\" Button Clicked; Name = {0}", textBox.Text));
        }

        private void ComboEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            CommonFileDialogComboBox combo = currentFileDialog.Controls["comboBox"] as CommonFileDialogComboBox;
            MessageBox.Show(String.Format("Combo box sel index changed: Total Items = {0}; Selected Index = {1}; Selected Item = {2}", combo.Items.Count, combo.SelectedIndex, combo.Items[combo.SelectedIndex].Text));
        }

        private void ChkOptionA_CheckedChanged(object sender, EventArgs e)
        {
            CommonFileDialogCheckBox checkBox = currentFileDialog.Controls["chkOptionA"] as CommonFileDialogCheckBox;
            MessageBox.Show(String.Format("Check Box \"{0}\" has been {1}", checkBox.Text, checkBox.IsChecked ? "Checked" : "Unchecked"));
        }

        private void ChkOptionB_CheckedChanged(object sender, EventArgs e)
        {
            CommonFileDialogCheckBox checkBox = currentFileDialog.Controls["chkOptionB"] as CommonFileDialogCheckBox;
            MessageBox.Show(String.Format("Check Box  \"{0}\"  has been {1}", checkBox.Text, checkBox.IsChecked ? "Checked" : "Unchecked"));
        }

        private void MenuOptionA_Click(object sender, EventArgs e)
        {
            CommonFileDialogMenu menu = currentFileDialog.Controls["menu"] as CommonFileDialogMenu;
            MessageBox.Show(String.Format("Menu \"{0}\" : Item \"{1}\" selected.", menu.Text, menu.Items[0].Text));
        }

        private void MenuOptionB_Click(object sender, EventArgs e)
        {
            CommonFileDialogMenu menu = currentFileDialog.Controls["menu"] as CommonFileDialogMenu;
            MessageBox.Show(String.Format("Menu \"{0}\" : Item \"{1}\" selected.", menu.Text, menu.Items[1].Text));
        }

        #endregion Custom controls event handlers

    }
}
