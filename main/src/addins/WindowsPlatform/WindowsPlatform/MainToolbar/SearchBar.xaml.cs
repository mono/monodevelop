using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace WindowsPlatform.MainToolbar
{
	/// <summary>
	/// Interaction logic for SearchBar.xaml
	/// </summary>
	public partial class SearchBarControl : UserControl, INotifyPropertyChanged
	{
		readonly ImageSource searchIcon, searchIconHovered, searchIconPressed;
		readonly ImageSource clearIcon, clearIconHovered, clearIconPressed;

		public SearchBarControl ()
		{
			InitializeComponent ();
			DataContext = this;

			SearchBar.GotKeyboardFocus += (o, e) => {
				if (searchText == placeholderText)
					SearchText = string.Empty;
			};
			IdeApp.Workbench.RootWindow.SetFocus += (o, e) =>
			{
				Keyboard.ClearFocus();
				IdeApp.Workbench.RootWindow.Present();
			};

			searchIcon = Stock.SearchboxSearch.GetImageSource (Xwt.IconSize.Small);
			searchIconHovered = Xwt.Drawing.Image.FromResource (typeof(IdeApp), "searchbox-search-win-16~hover.png").WithSize (Xwt.IconSize.Small).GetImageSource ();
			searchIconPressed = Xwt.Drawing.Image.FromResource (typeof(IdeApp), "searchbox-search-win-16~pressed.png").WithSize (Xwt.IconSize.Small).GetImageSource ();
			clearIcon = ((MonoDevelop.Core.IconId)"md-searchbox-clear").GetImageSource (Xwt.IconSize.Small);
			clearIconHovered = Xwt.Drawing.Image.FromResource (typeof(IdeApp),"searchbox-clear-win-16~hover.png").WithSize (Xwt.IconSize.Small).GetImageSource ();
			clearIconPressed = Xwt.Drawing.Image.FromResource (typeof(IdeApp), "searchbox-clear-win-16~pressed.png").WithSize (Xwt.IconSize.Small).GetImageSource ();
			SearchIcon.Image = searchIcon;
			SearchIcon.ImageHovered = searchIconHovered;
			SearchIcon.ImagePressed = searchIconPressed;
			SearchIcon.Focusable = false;
		}

		string placeholderText;
		public string PlaceholderText {
			get	{
				return placeholderText;
			}
			set	{
				var oldPlaceholderText = placeholderText;
				placeholderText = value;
				if (string.IsNullOrEmpty (SearchText) || searchText == oldPlaceholderText)
					SearchText = placeholderText;
				UpdateIcon ();
            }
		}

		string searchText;
		public string SearchText
		{
			get { return searchText; }
			set	{
					searchText = value;
					UpdateIcon ();
					RaisePropertyChanged ();
			}
		}

		bool isClearShown = false;
		void UpdateIcon ()
		{
			if (string.IsNullOrEmpty (searchText) || searchText == PlaceholderText) {
				if (isClearShown) {
					SearchIcon.Image = searchIcon;
					SearchIcon.ImageHovered = searchIconHovered;
					SearchIcon.ImagePressed = searchIconPressed;
					isClearShown = false;
				}
			} else if (!isClearShown) {
				SearchIcon.Image = clearIcon;
				SearchIcon.ImageHovered = clearIconHovered;
				SearchIcon.ImagePressed = clearIconPressed;
				isClearShown = true;
			}
		}

		IEnumerable<ISearchMenuModel> searchMenuItems;
        public IEnumerable<ISearchMenuModel> SearchMenuItems
		{
			get { return searchMenuItems; }
			set {
				searchMenuItems = value;

				foreach (MenuItem menuItem in SearchIcon.ContextMenu.Items)
					menuItem.Click -= OnMenuItemClicked;

				SearchIcon.ContextMenu.Items.Clear ();
				foreach (var item in value) {
					var menuItem = new SimpleMenuItem {
						Header = item.DisplayString,
						Tag = item,
					};
					menuItem.Click += OnMenuItemClicked;
					SearchIcon.ContextMenu.Items.Add (menuItem);
				}
			}
		}

		void OnIconClicked (object sender, RoutedEventArgs args)
		{
			if (isClearShown)
				SearchText = string.Empty;
			else
				SearchIcon.ContextMenu.IsOpen = true;
		}

		void OnMenuItemClicked (object sender, RoutedEventArgs args)
		{
			var menuItem = (MenuItem)sender;
			var searchModel = (ISearchMenuModel)menuItem.Tag;
			searchModel.NotifyActivated ();
			SearchBar.Focus ();
		}

		void RaisePropertyChanged ([CallerMemberName] string propName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
