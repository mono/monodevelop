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
		readonly Xwt.Drawing.Image searchIcon, clearIcon;

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
				if (Keyboard.FocusedElement == SearchBar) {
					Keyboard.ClearFocus ();
					IdeApp.Workbench.RootWindow.Present ();
				}
			};

			searchIcon = Stock.SearchboxSearch.GetStockIcon ().WithSize (Xwt.IconSize.Small);
			clearIcon = ((MonoDevelop.Core.IconId)"md-searchbox-clear").GetStockIcon ().WithSize (Xwt.IconSize.Small);
			SearchIcon.Image = searchIcon;
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
					isClearShown = false;
				}
			} else if (!isClearShown) {
				SearchIcon.Image = clearIcon;
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
				ClearIconClicked?.Invoke(this, EventArgs.Empty);
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

		public event EventHandler ClearIconClicked;
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
