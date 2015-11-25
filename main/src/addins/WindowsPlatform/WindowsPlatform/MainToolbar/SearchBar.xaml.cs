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

namespace WindowsPlatform.MainToolbar
{
	/// <summary>
	/// Interaction logic for SearchBar.xaml
	/// </summary>
	public partial class SearchBarControl : UserControl, INotifyPropertyChanged
	{
		public SearchBarControl ()
		{
			InitializeComponent ();
			DataContext = this;

			SearchBar.GotKeyboardFocus += (o, e) => {
				SearchText = string.Empty;
			};
			IdeApp.Workbench.RootWindow.SetFocus += (o, e) =>
			{
				Keyboard.ClearFocus();
				IdeApp.Workbench.RootWindow.Present();
			};

			SearchIcon.Source = Stock.SearchboxSearch.GetImageSource (Xwt.IconSize.Small);
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
            }
		}

		string searchText;
		public string SearchText
		{
			get { return searchText; }
			set	{ searchText = value; RaisePropertyChanged (); }
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
			SearchIcon.ContextMenu.IsOpen = true;
		}

		void OnMenuItemClicked (object sender, RoutedEventArgs args)
		{
			var menuItem = (MenuItem)sender;
			var searchModel = (ISearchMenuModel)menuItem.Tag;
			searchModel.NotifyActivated ();
		}

		void RaisePropertyChanged ([CallerMemberName] string propName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
