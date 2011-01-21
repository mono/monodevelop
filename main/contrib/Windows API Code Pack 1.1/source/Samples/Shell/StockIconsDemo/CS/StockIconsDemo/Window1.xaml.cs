//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Shell;

namespace StockIconsDemo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private StockIcons stockIcons = null;

        public Window1()
        {
            InitializeComponent();

            // Initialize our default collection of StockIcons.
            // We will reuse this collection and only change certain properties as needed
            stockIcons = new StockIcons();

            // Select large size
            comboBox1.SelectedIndex = 1;
        }

        private void UpdateStockIcon(StockIconSize size, bool? isLinkOverlay, bool? isSelected)
        {
            UpdateStockIcon(size, isLinkOverlay == true, isSelected == true);
        }

        private void UpdateStockIcon(StockIconSize newSize, bool isLinkOverlay, bool isSelected)
        {
            // Clear any existing items in the wrap panel
            // Using the updated UI settings, get all the stock icons and show them in an Image control
            wrapPanel1.Children.Clear();
            
            // Update all the stock icons with these latest settings
            UpdateStockIconSettings(newSize, isLinkOverlay, isSelected);

            // Get the new bitmap source
            foreach (StockIcon icon in stockIcons.AllStockIcons)
            {
                Image img = new Image();
                img.Tag = icon;
                img.Stretch = Stretch.None;
                img.Source = icon.BitmapSource;
                img.Margin = new Thickness(10);
                img.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(img_MouseLeftButtonDown);
                wrapPanel1.Children.Add(img);
            }

            stockIconsCount.Text = stockIcons.AllStockIcons.Count.ToString();
        }

        void img_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image img = sender as Image;

            // Get the stock icon object that we stored in the tag property
            if(img.Tag != null)
            {
                // Toggle the selection (i.e. get a new bitmapsource)
                bool selected = ((StockIcon)img.Tag).Selected;
                ((StockIcon)img.Tag).Selected = !selected;
                img.Source = ((StockIcon)img.Tag).BitmapSource;
            }
        }

        private void UpdateStockIconSettings(StockIconSize newSize, bool isLinkOverlay, bool isSelected)
        {
            // Update all the stock icons in the collection with the latest settings
            foreach (StockIcon icon in stockIcons.AllStockIcons)
            {
                icon.CurrentSize = newSize;
                icon.LinkOverlay = isLinkOverlay;
                icon.Selected = isSelected;
            }
        }

        private void linkOverlayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateStockIcon((StockIconSize)comboBox1.SelectedIndex, linkOverlayCheckBox.IsChecked, selectedCheckBox.IsChecked);
        }

        private void selectedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateStockIcon((StockIconSize)comboBox1.SelectedIndex, linkOverlayCheckBox.IsChecked, selectedCheckBox.IsChecked);
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStockIcon((StockIconSize)comboBox1.SelectedIndex, linkOverlayCheckBox.IsChecked, selectedCheckBox.IsChecked);
        }
    }
}
