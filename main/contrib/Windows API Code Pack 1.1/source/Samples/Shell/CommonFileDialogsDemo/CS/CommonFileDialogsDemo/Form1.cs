//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Reflection;

namespace Microsoft.WindowsAPICodePack.Samples.ShellObjectCFDBrowser
{
    public partial class Form1 : Form
    {
        private ShellObject currentlySelected = null;

        public Form1()
        {
            InitializeComponent();

            LoadKnownFolders();
            LoadSavedSearches();

            if (ShellLibrary.IsPlatformSupported)
                LoadKnownLibraries();
            else
                label2.Enabled = cfdLibraryButton.Enabled = librariesComboBox.Enabled = false;

            if (ShellSearchConnector.IsPlatformSupported)
                LoadSearchConnectors();
            else
                label7.Enabled = searchConnectorButton.Enabled = searchConnectorComboBox.Enabled = false;
        }

        /// <summary>
        /// Load all the Saved Searches on the current system
        /// </summary>
        private void LoadSavedSearches()
        {
            foreach (ShellObject so in KnownFolders.SavedSearches)
            {
                if (so is ShellSavedSearchCollection)
                    savedSearchComboBox.Items.Add(Path.GetFileName(so.ParsingName));
            }

            if (savedSearchComboBox.Items.Count > 0)
                savedSearchComboBox.SelectedIndex = 0;
            else
                savedSearchButton.Enabled = false;
        }

        /// <summary>
        /// Load all the Search Connectors on the current system
        /// </summary>
        private void LoadSearchConnectors()
        {
            foreach (ShellObject so in KnownFolders.SavedSearches)
            {
                if (so is ShellSearchConnector)
                    searchConnectorComboBox.Items.Add(Path.GetFileName(so.ParsingName));
            }

            if (searchConnectorComboBox.Items.Count > 0)
                searchConnectorComboBox.SelectedIndex = 0;
            else
                searchConnectorButton.Enabled = false;
        }

        /// <summary>
        /// Load the known Shell Libraries
        /// </summary>
        private void LoadKnownLibraries()
        {
            // Load all the known libraries.
            // (There's currently no easy way to get all the known libraries in the system, 
            // so hard-code them here.)

            // Make sure we are clear
            librariesComboBox.Items.Clear();

            // 
            librariesComboBox.Items.Add("Documents");
            librariesComboBox.Items.Add("Music");
            librariesComboBox.Items.Add("Pictures");
            librariesComboBox.Items.Add("Videos");

            // Set initial selection
            librariesComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Load all the knownfolders into the combobox
        /// </summary>
        private void LoadKnownFolders()
        {
            // Make sure we are clear
            knownFoldersComboBox.Items.Clear();

            // Get a list of all the known folders
            foreach (IKnownFolder kf in KnownFolders.All)
                if (kf != null && kf.CanonicalName != null)
                    knownFoldersComboBox.Items.Add(kf.CanonicalName);

            // Set our initial selection
            if (knownFoldersComboBox.Items.Count > 0)
                knownFoldersComboBox.SelectedIndex = 0;
        }

        private void cfdKFButton_Click(object sender, EventArgs e)
        {
            // Initialize
            detailsListView.Items.Clear();
            pictureBox1.Image = null;

            // Create a new CFD
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();

            // Allow users to select non-filesystem objects
            cfd.AllowNonFileSystemItems = true;

            // Get the known folder selected
            string kfString = knownFoldersComboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(kfString))
            {
                try
                {
                    // Try to get a known folder using the selected item (string).
                    IKnownFolder kf = KnownFolderHelper.FromCanonicalName(kfString);

                    // Set the knownfolder in the CFD.
                    cfd.InitialDirectoryShellContainer = kf as ShellContainer;

                    if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        ShellObject selectedSO = null;

                        try
                        {
                            // Get the selection from the user.
                            selectedSO = cfd.FileAsShellObject;
                        }
                        catch
                        {
                            // In some cases the user might select an object that cannot be wrapped
                            // by ShellObject.
                            MessageBox.Show("Could not create a ShellObject from the selected item.");
                        }


                        currentlySelected = selectedSO;

                        DisplayProperties(selectedSO);

                        showChildItemsButton.Enabled = selectedSO is ShellContainer ? true : false;
                    }
                }
                catch
                {
                    MessageBox.Show("Could not create a KnownFolder object for the selected item");
                }
            }
            else
            {
                MessageBox.Show("Invalid KnownFolder set.");
            }

            // Dispose our dialog in the end
            cfd.Dispose();
        }

        private void cfdLibraryButton_Click(object sender, EventArgs e)
        {
            // Initialize
            detailsListView.Items.Clear();
            pictureBox1.Image = null;

            // If the user has selected a library,
            // try to get the known folder path (Libraries are also known folders, so this will work)
            if (librariesComboBox.SelectedIndex > -1)
            {
                string selection = librariesComboBox.SelectedItem as string;
                ShellContainer selectedFolder = null;

                switch (selection)
                {
                    case "Documents":
                        selectedFolder = KnownFolders.DocumentsLibrary as ShellContainer;
                        break;
                    case "Music":
                        selectedFolder = KnownFolders.MusicLibrary as ShellContainer;
                        break;
                    case "Pictures":
                        selectedFolder = KnownFolders.PicturesLibrary as ShellContainer;
                        break;
                    case "Videos":
                        selectedFolder = KnownFolders.VideosLibrary as ShellContainer;
                        break;
                };

                // Create a CommonOpenFileDialog
                CommonOpenFileDialog cfd = new CommonOpenFileDialog();
                cfd.EnsureReadOnly = true;

                // Set the initial location as the path of the library
                cfd.InitialDirectoryShellContainer = selectedFolder;

                if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    // Get the selection from the user.
                    ShellObject so = cfd.FileAsShellObject;

                    currentlySelected = so;

                    showChildItemsButton.Enabled = so is ShellContainer ? true : false;

                    DisplayProperties(so);
                }
            }
        }

        private void cfdFileButton_Click(object sender, EventArgs e)
        {
            // Initialize
            detailsListView.Items.Clear();
            pictureBox1.Image = null;

            // Create a CommonOpenFileDialog to select files
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.AllowNonFileSystemItems = true;
            cfd.EnsureReadOnly = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ShellObject selectedSO = null;

                try
                {
                    // Try to get the selected item
                    selectedSO = cfd.FileAsShellObject;
                }
                catch
                {
                    MessageBox.Show("Could not create a ShellObject from the selected item");
                }

                currentlySelected = selectedSO;

                // Set the path in our filename textbox
                selectedFileTextBox.Text = selectedSO.ParsingName;

                DisplayProperties(selectedSO);

                showChildItemsButton.Enabled = selectedSO is ShellContainer ? true : false;

            }
        }

        private void cfdFolderButton_Click(object sender, EventArgs e)
        {
            // Initialize
            detailsListView.Items.Clear();
            pictureBox1.Image = null;

            // Display a CommonOpenFileDialog to select only folders 
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.EnsureReadOnly = true;
            cfd.IsFolderPicker = true;
            cfd.AllowNonFileSystemItems = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ShellContainer selectedSO = null;

                try
                {
                    // Try to get a valid selected item
                    selectedSO = cfd.FileAsShellObject as ShellContainer;
                }
                catch
                {
                    MessageBox.Show("Could not create a ShellObject from the selected item");
                }

                currentlySelected = selectedSO;

                // Set the path in our filename textbox
                selectedFolderTextBox.Text = selectedSO.ParsingName;

                DisplayProperties(selectedSO);

                showChildItemsButton.Enabled = selectedSO is ShellContainer ? true : false;
            }
        }

        private void savedSearchButton_Click(object sender, EventArgs e)
        {
            // Initialize
            detailsListView.Items.Clear();
            pictureBox1.Image = null;

            // Display a CommonOpenFileDialog to select only folders 
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.InitialDirectory = Path.Combine(KnownFolders.SavedSearches.Path, savedSearchComboBox.SelectedItem.ToString());
            cfd.EnsureReadOnly = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ShellObject selectedSO = null;

                try
                {
                    // Try to get a valid selected item
                    selectedSO = cfd.FileAsShellObject;
                }
                catch
                {
                    MessageBox.Show("Could not create a ShellObject from the selected item");
                }

                currentlySelected = selectedSO;

                DisplayProperties(selectedSO);

                showChildItemsButton.Enabled = selectedSO is ShellContainer ? true : false;

            }
        }

        private void DisplayProperties(ShellObject selectedSO)
        {
            // Display some basic properties 
            if (selectedSO != null)
            {
                // display properties for this folder, as well as a thumbnail image.
                selectedSO.Thumbnail.CurrentSize = new System.Windows.Size(128, 128);
                pictureBox1.Image = selectedSO.Thumbnail.Bitmap;

                // show the properties
                AddProperty("Name", selectedSO.Name);
                AddProperty("Path", selectedSO.ParsingName);
                AddProperty("Type of ShellObject", selectedSO.GetType().Name);

                foreach (IShellProperty prop in selectedSO.Properties.DefaultPropertyCollection)
                {
                    if (prop.ValueAsObject != null)
                    {
                        try
                        {
                            if (prop.ValueType == typeof(string[]))
                            {
                                string[] arr = (string[])prop.ValueAsObject;
                                string value = "";
                                if (arr != null && arr.Length > 0)
                                {
                                    foreach (string s in arr)
                                        value = value + s + "; ";

                                    if (value.EndsWith("; "))
                                        value = value.Remove(value.Length - 2);
                                }

                                AddProperty(prop.CanonicalName, value);
                            }
                            else
                                AddProperty(prop.CanonicalName, prop.ValueAsObject.ToString());
                        }
                        catch
                        {
                            // Ignore
                            // Accessing some properties might throw exception.
                        }
                    }
                }
            }

        }

        private void AddProperty(string property, string value)
        {
            if (!string.IsNullOrEmpty(property))
            {
                // Create the property ListViewItem
                ListViewItem lvi = new ListViewItem(property);

                // Add a subitem for the value
                ListViewItem.ListViewSubItem subItemValue = new ListViewItem.ListViewSubItem(lvi, value);
                lvi.SubItems.Add(subItemValue);

                // Add the ListViewItem to our list
                detailsListView.Items.Add(lvi);
            }
        }

        private void searchConnectorButton_Click(object sender, EventArgs e)
        {
            // Initialize
            detailsListView.Items.Clear();
            pictureBox1.Image = null;

            // Display a CommonOpenFileDialog to select only folders 
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.InitialDirectory = Path.Combine(KnownFolders.SavedSearches.Path, searchConnectorComboBox.SelectedItem.ToString());
            cfd.EnsureReadOnly = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ShellObject selectedSO = null;

                try
                {
                    // Try to get a valid selected item
                    selectedSO = cfd.FileAsShellObject;
                }
                catch
                {
                    MessageBox.Show("Could not create a ShellObject from the selected item");
                }

                currentlySelected = selectedSO;

                DisplayProperties(selectedSO);

                showChildItemsButton.Enabled = selectedSO is ShellContainer ? true : false;

            }
        }

        private void showChildItemsButton_Click(object sender, EventArgs e)
        {
            ShellContainer container = currentlySelected as ShellContainer;

            if (container == null)
                return;

            SubItemsForm subItems = new SubItemsForm();

            // Populate
            foreach (ShellObject so in container)
                subItems.AddItem(so.Name, so.Thumbnail.SmallBitmap);

            subItems.ShowDialog();
        }

        private void saveFileButton_Click(object sender, EventArgs e)
        {
            // Initialize
            detailsListView.Items.Clear();
            pictureBox1.Image = null;

            // Show a CommonSaveFileDialog with couple of file filters.
            // Also show some properties (specific to the filter selected) 
            // that the user can update from the dialog itself.
            CommonSaveFileDialog saveCFD = new CommonSaveFileDialog();
            saveCFD.AlwaysAppendDefaultExtension = true;
            saveCFD.DefaultExtension = ".docx";

            // When the file type changes, we will add the specific properties
            // to be collected from the dialog (refer to the saveCFD_FileTypeChanged event handler)
            saveCFD.FileTypeChanged += new EventHandler(saveCFD_FileTypeChanged);

            saveCFD.Filters.Add(new CommonFileDialogFilter("Word Documents", "*.docx"));
            saveCFD.Filters.Add(new CommonFileDialogFilter("JPEG Files", "*.jpg"));

            if (saveCFD.ShowDialog() == CommonFileDialogResult.Ok)
            {
                // Get the selected file (this is what we'll save...)
                // Save it to disk, so we can read/write properties for it

                // Because we can't really create a Office file or Picture file, just copying
                // an existing file to show the properties
                if (saveCFD.SelectedFileTypeIndex == 1)
                    File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "sample files\\test.docx"), saveCFD.FileName, true);
                else
                    File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "sample files\\test.jpg"), saveCFD.FileName, true);

                // Get the ShellObject for this file
                ShellObject selectedSO = ShellFile.FromFilePath(saveCFD.FileName);

                // Get the properties from the dialog (user might have updated the properties)
                ShellPropertyCollection propColl = saveCFD.CollectedProperties;

                // Write the properties on our shell object
                using (ShellPropertyWriter propertyWriter = selectedSO.Properties.GetPropertyWriter())
                {
                    if (propColl.Contains(SystemProperties.System.Title))
                        propertyWriter.WriteProperty(SystemProperties.System.Title, propColl[SystemProperties.System.Title].ValueAsObject);

                    if (propColl.Contains(SystemProperties.System.Author))
                        propertyWriter.WriteProperty(SystemProperties.System.Author, propColl[SystemProperties.System.Author].ValueAsObject);

                    if (propColl.Contains(SystemProperties.System.Keywords))
                        propertyWriter.WriteProperty(SystemProperties.System.Keywords, propColl[SystemProperties.System.Keywords].ValueAsObject);

                    if (propColl.Contains(SystemProperties.System.Comment))
                        propertyWriter.WriteProperty(SystemProperties.System.Comment, propColl[SystemProperties.System.Comment].ValueAsObject);

                    if (propColl.Contains(SystemProperties.System.Category))
                        propertyWriter.WriteProperty(SystemProperties.System.Category, propColl[SystemProperties.System.Category].ValueAsObject);

                    if (propColl.Contains(SystemProperties.System.ContentStatus))
                        propertyWriter.WriteProperty(SystemProperties.System.Title, propColl[SystemProperties.System.Title].ValueAsObject);

                    if (propColl.Contains(SystemProperties.System.Photo.DateTaken))
                        propertyWriter.WriteProperty(SystemProperties.System.Photo.DateTaken, propColl[SystemProperties.System.Photo.DateTaken].ValueAsObject);

                    if (propColl.Contains(SystemProperties.System.Photo.CameraModel))
                        propertyWriter.WriteProperty(SystemProperties.System.Photo.CameraModel, propColl[SystemProperties.System.Photo.CameraModel].ValueAsObject);

                    if (propColl.Contains(SystemProperties.System.Rating))
                        propertyWriter.WriteProperty(SystemProperties.System.Rating, propColl[SystemProperties.System.Rating].ValueAsObject);
                }

                currentlySelected = selectedSO;
                DisplayProperties(selectedSO);

                showChildItemsButton.Enabled = selectedSO is ShellContainer ? true : false;
            }
        }

        void saveCFD_FileTypeChanged(object sender, EventArgs e)
        {
            CommonSaveFileDialog cfd = sender as CommonSaveFileDialog;

            if (cfd.SelectedFileTypeIndex == 1)
            {
                cfd.SetCollectedPropertyKeys(true, SystemProperties.System.Title, SystemProperties.System.Author);
                cfd.DefaultExtension = ".docx";
            }
            else if (cfd.SelectedFileTypeIndex == 2)
            {
                cfd.SetCollectedPropertyKeys(true, SystemProperties.System.Photo.DateTaken, SystemProperties.System.Photo.CameraModel);
                cfd.DefaultExtension = ".jpg";
            }
        }
    }
}