// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Input;

namespace Microsoft.WindowsAPICodePack.Samples.SearchApp
{
    /// <summary>
    /// Interaction logic for AdvancedSearch.xaml
    /// </summary>
    public partial class AdvancedSearch : Window
    {
        private StockIcons stockIcons;
        private StockIcon documentsStockIcon;
        private StockIcon picturesStockIcon;
        private StockIcon musicStockIcon;
        private StockIcon videosStockIcon;

        internal Window1 MainWindow;

        // Background thread for our search
        private Thread backgroundSearchThread = null;

        public AdvancedSearch()
        {
            stockIcons = new StockIcons();

            documentsStockIcon = stockIcons.DocumentAssociated;
            videosStockIcon = stockIcons.VideoFiles;
            musicStockIcon = stockIcons.AudioFiles;
            picturesStockIcon = stockIcons.ImageFiles;

            InitializeComponent();

            // Set our default
            DocumentsRadioButton.IsChecked = true;

            // 
            prop1prop2OperationComboBox.SelectedIndex = 0;

            // Because the search can take some time, using a background thread.
            // This timer will check if that thread is still alive and accordingly update
            // the cursor
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.IsEnabled = true;
            timer.Tick += new EventHandler(timer_Tick);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            // Using a timer, check if our background search thread is still alive.
            // If not alive, update the cursor to arrow
            if (backgroundSearchThread != null && !backgroundSearchThread.IsAlive)
            {
                this.Cursor = Cursors.Arrow;
                MainWindow.Cursor = Cursors.Arrow;

                // Also enable the search textbox again
                MainWindow.SearchBox.IsEnabled = true;
                MainWindow.buttonSearchAdv.IsEnabled = true;
                buttonSearch.IsEnabled = true;
                buttonClear.IsEnabled = true;
            }
        }

        private void DocumentsRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateDocumentsSearchSettings();

            documentsStockIcon.Selected = true;
            DocumentsRadioButton.Content = new Image { Source = documentsStockIcon.BitmapSource };

            picturesStockIcon.Selected = false;
            PicturesRadioButton.Content = new Image { Source = picturesStockIcon.BitmapSource };

            musicStockIcon.Selected = false;
            MusicRadioButton.Content = new Image { Source = musicStockIcon.BitmapSource };

            videosStockIcon.Selected = false;
            VideosRadioButton.Content = new Image { Source = videosStockIcon.BitmapSource };
        }

        private void PicturesRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdatePicturesSearchSettings();

            documentsStockIcon.Selected = false;
            DocumentsRadioButton.Content = new Image { Source = documentsStockIcon.BitmapSource };

            picturesStockIcon.Selected = true;
            PicturesRadioButton.Content = new Image { Source = picturesStockIcon.BitmapSource };

            musicStockIcon.Selected = false;
            MusicRadioButton.Content = new Image { Source = musicStockIcon.BitmapSource };

            videosStockIcon.Selected = false;
            VideosRadioButton.Content = new Image { Source = videosStockIcon.BitmapSource };
        }

        private void MusicRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMusicSearchSettings();

            documentsStockIcon.Selected = false;
            DocumentsRadioButton.Content = new Image { Source = documentsStockIcon.BitmapSource };

            picturesStockIcon.Selected = false;
            PicturesRadioButton.Content = new Image { Source = picturesStockIcon.BitmapSource };

            musicStockIcon.Selected = true;
            MusicRadioButton.Content = new Image { Source = musicStockIcon.BitmapSource };

            videosStockIcon.Selected = false;
            VideosRadioButton.Content = new Image { Source = videosStockIcon.BitmapSource };
        }

        private void VideosRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateVideosSearchSettings();

            documentsStockIcon.Selected = false;
            DocumentsRadioButton.Content = new Image { Source = documentsStockIcon.BitmapSource };

            picturesStockIcon.Selected = false;
            PicturesRadioButton.Content = new Image { Source = picturesStockIcon.BitmapSource };

            musicStockIcon.Selected = false;
            MusicRadioButton.Content = new Image { Source = musicStockIcon.BitmapSource };

            videosStockIcon.Selected = true;
            VideosRadioButton.Content = new Image { Source = videosStockIcon.BitmapSource };
        }

        private void UpdateDocumentsSearchSettings()
        {
            // We are in "documents" mode
            prop1ComboBox.Items.Clear();
            prop1ComboBox.Items.Add("Author");
            prop1ComboBox.Items.Add("Title");
            prop1ComboBox.Items.Add("Comment");
            prop1ComboBox.Items.Add("Copyright");
            prop1ComboBox.Items.Add("Pages");
            prop1ComboBox.Items.Add("Tags/Keywords");
            prop1ComboBox.SelectedIndex = 0;

            prop1OperationComboBox.SelectedIndex = 9;

            prop1TextBox.Text = "";

            prop2ComboBox.Items.Clear();
            prop2ComboBox.Items.Add("Author");
            prop2ComboBox.Items.Add("Title");
            prop2ComboBox.Items.Add("Comment");
            prop2ComboBox.Items.Add("Copyright");
            prop2ComboBox.Items.Add("Pages");
            prop2ComboBox.Items.Add("Tags/Keywords");
            prop2ComboBox.SelectedIndex = 5;

            prop2OperationComboBox.SelectedIndex = 0;

            prop2TextBox.Text = "";

            prop1prop2OperationComboBox.SelectedIndex = 0;

            comboBoxDateCreated.SelectedIndex = 0;

            // locations
            locationsListBox.Items.Clear();

            if (ShellLibrary.IsPlatformSupported)
                AddLocation((ShellContainer)KnownFolders.DocumentsLibrary);
            else
                AddLocation((ShellContainer)KnownFolders.Documents);
        }

        private void UpdatePicturesSearchSettings()
        {
            // We are in "Pictures" mode
            prop1ComboBox.Items.Clear();
            prop1ComboBox.Items.Add("Author");
            prop1ComboBox.Items.Add("Subject");
            prop1ComboBox.Items.Add("Camera maker");
            prop1ComboBox.Items.Add("Copyright");
            prop1ComboBox.Items.Add("Rating");
            prop1ComboBox.Items.Add("Tags/Keywords");
            prop1ComboBox.SelectedIndex = 0;

            prop1OperationComboBox.SelectedIndex = 9;

            prop1TextBox.Text = "";

            prop2ComboBox.Items.Clear();
            prop2ComboBox.Items.Add("Author");
            prop2ComboBox.Items.Add("Subject");
            prop2ComboBox.Items.Add("Camera maker");
            prop2ComboBox.Items.Add("Copyright");
            prop2ComboBox.Items.Add("Rating");
            prop2ComboBox.Items.Add("Tags/Keywords");
            prop2ComboBox.SelectedIndex = 0;

            prop2OperationComboBox.SelectedIndex = 0;

            prop2TextBox.Text = "";

            prop1prop2OperationComboBox.SelectedIndex = 0;

            comboBoxDateCreated.SelectedIndex = 0;

            // locations
            locationsListBox.Items.Clear();

            if (ShellLibrary.IsPlatformSupported)
                AddLocation((ShellContainer)KnownFolders.PicturesLibrary);
            else
                AddLocation((ShellContainer)KnownFolders.Pictures);

        }

        private void UpdateMusicSearchSettings()
        {
            // We are in "Music" mode
            prop1ComboBox.Items.Clear();
            prop1ComboBox.Items.Add("Album artist");
            prop1ComboBox.Items.Add("Album title");
            prop1ComboBox.Items.Add("Composer");
            prop1ComboBox.Items.Add("Rating");
            prop1ComboBox.Items.Add("Genre");
            prop1ComboBox.Items.Add("Year");
            prop1ComboBox.SelectedIndex = 1;

            prop1OperationComboBox.SelectedIndex = 9;

            prop1TextBox.Text = "";

            prop2ComboBox.Items.Clear();
            prop2ComboBox.Items.Add("Album artist");
            prop2ComboBox.Items.Add("Album title");
            prop2ComboBox.Items.Add("Composer");
            prop2ComboBox.Items.Add("Rating");
            prop2ComboBox.Items.Add("Genre");
            prop2ComboBox.Items.Add("Year");
            prop2ComboBox.SelectedIndex = 0;

            prop2OperationComboBox.SelectedIndex = 0;

            prop2TextBox.Text = "";

            prop1prop2OperationComboBox.SelectedIndex = 0;

            comboBoxDateCreated.SelectedIndex = 0;

            // locations
            locationsListBox.Items.Clear();

            if (ShellLibrary.IsPlatformSupported)
                AddLocation((ShellContainer)KnownFolders.MusicLibrary);
            else
                AddLocation((ShellContainer)KnownFolders.Music);
        }

        private void UpdateVideosSearchSettings()
        {
            // We are in "Videos" mode
            prop1ComboBox.Items.Clear();
            prop1ComboBox.Items.Add("Title");
            prop1ComboBox.Items.Add("Video length");
            prop1ComboBox.Items.Add("Comment");
            prop1ComboBox.SelectedIndex = 0;

            prop1OperationComboBox.SelectedIndex = 9;

            prop1TextBox.Text = "";

            prop2ComboBox.Items.Clear();
            prop1ComboBox.Items.Add("Title");
            prop1ComboBox.Items.Add("Video length");
            prop1ComboBox.Items.Add("Comment");
            prop2ComboBox.SelectedIndex = 0;

            prop2OperationComboBox.SelectedIndex = 0;

            prop2TextBox.Text = "";

            prop1prop2OperationComboBox.SelectedIndex = 0;

            comboBoxDateCreated.SelectedIndex = 0;

            // locations
            locationsListBox.Items.Clear();

            if (ShellLibrary.IsPlatformSupported)
                AddLocation((ShellContainer)KnownFolders.VideosLibrary);
            else
                AddLocation((ShellContainer)KnownFolders.Videos);
        }

        private void addLocationButton_Click(object sender, RoutedEventArgs e)
        {
            // Show CFD and let users pick a folder
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            cfd.AllowNonFileSystemItems = true;
            cfd.IsFolderPicker = true;
            cfd.Multiselect = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                // Loop through each "folder" and add it our list
                foreach (ShellContainer so in cfd.FilesAsShellObject)
                {
                    AddLocation(so);
                }
            }
        }

        private void AddLocation(ShellContainer so)
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            // Add the thumbnail/icon
            Image img = new Image();

            // Because we might be called from a different thread, freeze the bitmap source once
            // we get it
            BitmapSource smallBitmapSource = so.Thumbnail.SmallBitmapSource;
            smallBitmapSource.Freeze();
            img.Source = smallBitmapSource;

            img.Margin = new Thickness(5);
            sp.Children.Add(img);

            // Add the name/title
            TextBlock tb = new TextBlock();
            tb.Text = so.Name;
            tb.Margin = new Thickness(5);
            sp.Children.Add(tb);

            // Set our tag as the shell container user picked...
            sp.Tag = so;

            //
            locationsListBox.Items.Add(sp);
        }

        private void removeLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (locationsListBox.SelectedItem != null)
                locationsListBox.Items.Remove(locationsListBox.SelectedItem);
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            // Reset all the settings on the dialog for the selected search type
            if (DocumentsRadioButton.IsChecked.Value)
                UpdateDocumentsSearchSettings();
            else if (PicturesRadioButton.IsChecked.Value)
                UpdatePicturesSearchSettings();
            else if (VideosRadioButton.IsChecked.Value)
                UpdateVideosSearchSettings();
            else if (MusicRadioButton.IsChecked.Value)
                UpdateMusicSearchSettings();
        }

        private DateTime ParseDate(string toParse, DateTime relativeDate)
        {
            if (string.IsNullOrEmpty(toParse) || !toParse.StartsWith("date:"))
                throw new ArgumentException();

            string tmpToParse = toParse.ToLower().Replace("date:", "");

            switch (tmpToParse)
            {
                case "a long time ago":
                    DateTime longTimeAgo = new DateTime(relativeDate.Year - 2, 1, 1, 0, 0, 0);
                    return longTimeAgo;
                case "earlier this year":
                    DateTime thisYear = new DateTime(relativeDate.Year, 1, 1, 0, 0, 0);
                    return thisYear;
                case "earlier this month":
                    DateTime thisMonth = new DateTime(relativeDate.Year, relativeDate.Month, 1, 0, 0, 0);
                    return thisMonth;
                case "last week":
                    DayOfWeek dayOfWeek = relativeDate.DayOfWeek;
                    DateTime lastweekSunday = relativeDate.AddDays(-1 * (int)dayOfWeek);
                    return lastweekSunday;
                case "yesterday":
                    DateTime yesterday = relativeDate.AddDays(-1);
                    return yesterday;
                case "earlier this week":
                    DateTime lastWeek = relativeDate.AddDays(-7);
                    return lastWeek;
                default:
                    throw new ArgumentException();
            }
        }

        private void buttonSearch_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow == null)
                return;
            
            if (backgroundSearchThread != null)
                backgroundSearchThread.Abort();

            // Set our cursor to wait
            this.Cursor = Cursors.Wait;
            MainWindow.Cursor = Cursors.Wait;

            // Also disable the search textbox while our search is going on
            MainWindow.SearchBox.IsEnabled = false;
            MainWindow.buttonSearchAdv.IsEnabled = false;
            buttonSearch.IsEnabled = false;
            buttonClear.IsEnabled = false;
            MainWindow.SearchBox.Clear();

            // Bring the main window in the foreground
            MainWindow.Activate();

            // Create a background thread to do the search
            backgroundSearchThread = new Thread(new ThreadStart(DoAdvancedSearch));
            // ApartmentState.STA is required for COM
            backgroundSearchThread.SetApartmentState(ApartmentState.STA);
            backgroundSearchThread.Start();

        }

        private void DoAdvancedSearch()
        {

            // This is our final searchcondition that we'll create the search folder from
            SearchCondition finalSearchCondition = null;

            // This is Prop1 + prop2 search condition... if the user didn't specify one of the properties,
            // we can just use the one property/value they specify...if they do, then we can do the and/or operation
            SearchCondition combinedPropSearchCondition = null;

            // Because we are doing the search on a background thread,
            // we can't access the UI controls from that thread.
            // Invoke from the main UI thread and get the values
            string prop1TextBox_Text = string.Empty;
            string prop2TextBox_Text = string.Empty;
            string prop1ComboBox_Value = string.Empty;
            string prop2ComboBox_Value = string.Empty;
            SearchConditionOperation prop1ConditionOperation = SearchConditionOperation.ValueContains;
            SearchConditionOperation prop2ConditionOperation = SearchConditionOperation.ValueContains;
            string prop1prop2OperationComboBox_Value = string.Empty;
            string comboBoxDateCreated_Value = string.Empty;
            int prop1prop2OperationComboBox_SelectedIndex = 0;
            bool dateSelected = false;
            List<ShellContainer> scopes = new List<ShellContainer>();

            Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                delegate()
                {
                    prop1TextBox_Text = prop1TextBox.Text;
                    prop1TextBox_Text = prop1TextBox.Text;
                    prop1ComboBox_Value = prop1ComboBox.SelectedItem.ToString();
                    prop2ComboBox_Value = prop2ComboBox.SelectedItem.ToString();
                    prop1ConditionOperation = GetConditionOperation(prop1OperationComboBox);
                    prop2ConditionOperation = GetConditionOperation(prop2OperationComboBox);
                    prop1prop2OperationComboBox_Value = prop1prop2OperationComboBox.SelectedItem.ToString();
                    prop1prop2OperationComboBox_SelectedIndex = prop1prop2OperationComboBox.SelectedIndex;
                    comboBoxDateCreated_Value = comboBoxDateCreated.SelectedItem.ToString();
                    dateSelected = (comboBoxDateCreated.SelectedItem != dateCreatedNone);

                    foreach (StackPanel sp in locationsListBox.Items)
                    {
                        if (sp.Tag is ShellContainer)
                            scopes.Add((ShellContainer)sp.Tag);
                    }
                }));

            // If we have a valid first property/value, then create a search condition
            if (!string.IsNullOrEmpty(prop1TextBox_Text))
            {
                SearchCondition prop1Condition = SearchConditionFactory.CreateLeafCondition(
                    GetSearchProperty(prop1ComboBox_Value),
                    prop1TextBox_Text,
                    prop1ConditionOperation);

                // After creating the first condition, see if we need to create a second leaf condition
                if (prop1prop2OperationComboBox_SelectedIndex != 0 &&
                    !(string.IsNullOrEmpty(prop2TextBox_Text)))
                {
                    SearchCondition prop2Condition = SearchConditionFactory.CreateLeafCondition(
                        GetSearchProperty(prop2ComboBox_Value),
                        prop2TextBox_Text,
                        prop2ConditionOperation);

                    // Create our combined search condition AND or OR
                    if (prop1prop2OperationComboBox.SelectedIndex == 1)
                        combinedPropSearchCondition = SearchConditionFactory.CreateAndOrCondition(
                            SearchConditionType.And,
                            false, prop1Condition, prop2Condition);
                    else
                        combinedPropSearchCondition = SearchConditionFactory.CreateAndOrCondition(
                            SearchConditionType.Or,
                            false, prop1Condition, prop2Condition);
                }
                else
                    combinedPropSearchCondition = prop1Condition;
            }
            else
                return; // no search text entered

            // Get the date condition
            if (dateSelected)
            {
                SearchCondition dateCreatedCondition = SearchConditionFactory.CreateLeafCondition(
                    SystemProperties.System.DateCreated,
                    ParseDate(((ComboBoxItem)comboBoxDateCreated.SelectedItem).Tag.ToString(), DateTime.Now),
                    SearchConditionOperation.GreaterThan);

                // If we have a property based search condition, create an "AND" search condition from these 2
                if (combinedPropSearchCondition != null)
                    finalSearchCondition = SearchConditionFactory.CreateAndOrCondition(SearchConditionType.And,
                        false, combinedPropSearchCondition, dateCreatedCondition);
                else
                    finalSearchCondition = dateCreatedCondition;
            }
            else
                finalSearchCondition = combinedPropSearchCondition;


            ShellSearchFolder searchFolder = new ShellSearchFolder(finalSearchCondition, scopes.ToArray());
            
            //
            List<SearchItem> items = new List<SearchItem>();

            try
            {
                // Because we cannot pass ShellObject or IShellItem (native interface)
                // across multiple threads, creating a helper object and copying the data we need from the ShellObject
                foreach (ShellObject so in searchFolder)
                {
                    // For each of our ShellObject,
                    // create a SearchItem object
                    // We will bind these items to the ListView
                    SearchItem item = new SearchItem();
                    item.Name = so.Name;

                    // We must freeze the ImageSource before passing across threads
                    BitmapSource thumbnail = so.Thumbnail.MediumBitmapSource;
                    thumbnail.Freeze();
                    item.Thumbnail = thumbnail;

                    item.Authors = so.Properties.System.Author.Value;
                    item.Title = so.Properties.System.Title.Value;
                    item.Keywords = so.Properties.System.Keywords.Value;
                    item.Copyright = so.Properties.System.Copyright.Value;
                    item.TotalPages = so.Properties.System.Document.PageCount.Value.HasValue ? so.Properties.System.Document.PageCount.Value.Value : 0;
                    item.Rating = so.Properties.System.SimpleRating.Value.HasValue ? (int)so.Properties.System.SimpleRating.Value.Value : 0;
                    item.ParsingName = so.ParsingName;

                    items.Add(item);
                }

                // Invoke the search on the main thread

                Dispatcher.Invoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(
                    delegate()
                    {
                        MainWindow.UpdateSearchItems(items);
                    }
                ));
            }
            finally
            {
                // TODO - dispose other

                finalSearchCondition.Dispose();
                finalSearchCondition = null;

                searchFolder.Dispose();
                searchFolder = null;
            }
        }

        private PropertyKey GetSearchProperty(string prop)
        {
            switch (prop)
            {
                case "Author":
                    return SystemProperties.System.Author;
                case "Title":
                    return SystemProperties.System.Title;
                case "Comment":
                    return SystemProperties.System.Comment;
                case "Copyright":
                    return SystemProperties.System.Copyright;
                case "Pages":
                    return SystemProperties.System.Document.PageCount;
                case "Tags/Keywords":
                    return SystemProperties.System.Keywords;
                case "Subject":
                    return SystemProperties.System.Subject;
                case "Camera maker":
                    return SystemProperties.System.Photo.CameraManufacturer;
                case "Rating":
                    return SystemProperties.System.Rating;
                case "Album artist":
                    return SystemProperties.System.Music.AlbumArtist;
                case "Album title":
                    return SystemProperties.System.Music.AlbumTitle;
                case "Composer":
                    return SystemProperties.System.Music.Composer;
                case "Genre":
                    return SystemProperties.System.Music.Genre;
                case "Video length":
                    return SystemProperties.System.Media.Duration;
                case "Year":
                    return SystemProperties.System.Media.Year;
            }

            return SystemProperties.System.Null;
        }

        private SearchConditionOperation GetConditionOperation(ComboBox comboBox)
        {
            SearchConditionOperation operation = (SearchConditionOperation)Enum.Parse(
                typeof(SearchConditionOperation),
                ((ComboBoxItem)comboBox.Items[comboBox.SelectedIndex]).Tag.ToString(),
                true);

            return operation;
        }

        private void prop1prop2OperationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Based on the And/OR operation between the two properties, enable/disable
            // the second propertie's UI
            if (prop1prop2OperationComboBox.SelectedIndex == 0) // (None)
            {
                prop2ComboBox.IsEnabled = false;
                prop2OperationComboBox.IsEnabled = false;
                prop2TextBox.IsEnabled = false;
            }
            else
            {
                prop2ComboBox.IsEnabled = true;
                prop2OperationComboBox.IsEnabled = true;
                prop2TextBox.IsEnabled = true;
            }
        }
    }
}
