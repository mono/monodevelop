using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Components.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.Ide;
using Xwt;
using Xwt.WPFBackend;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;

namespace WindowsPlatform.MainToolbar
{
	public class WPFToolbar : GtkWPFWidget, IMainToolbarView, INotifyPropertyChanged
	{
		ToolBar toolbar;
		WPFToolbar (ToolBar toolbar) : base (toolbar)
		{
			this.toolbar = toolbar;

			toolbar.ConfigurationMenu.SelectionChanged += (o, e) => {
				var comboMenu = (ComboMenu<IConfigurationModel>)o;
				var newModel = e.Added;
				if (newModel == null)
					return;

				if (ConfigurationChanged != null)
					ConfigurationChanged (o, e);

				ActiveConfiguration = newModel;
			};

			toolbar.RuntimeMenu.SelectionChanged += (o, e) => {
				var newModel = e.Added;
				if (newModel == null)
					return;
				
				using (var mutableModel = newModel.GetMutableModel ()) {
					ActiveRuntime = newModel;
					var ea = new MonoDevelop.Components.MainToolbar.HandledEventArgs ();
					if (RuntimeChanged != null)
						RuntimeChanged (o, ea);

					if (ea.Handled)
						ActiveRuntime = e.Removed;
				}
			};

			toolbar.RunButton.Click += (o, e) => {
				if (RunButtonClicked != null)
					RunButtonClicked (o, e);
			};

			toolbar.SearchBar.SearchBar.TextChanged += (o, e) => {
				if (string.IsNullOrEmpty (SearchText) || SearchText == SearchPlaceholderMessage)
					return;

				if (SearchEntryChanged != null)
					SearchEntryChanged (o, e);
			};

			toolbar.SearchBar.SearchBar.LostKeyboardFocus += (o, e) => {
				if (SearchEntryLostFocus != null)
					SearchEntryLostFocus (o, e);
				toolbar.SearchBar.SearchText = toolbar.SearchBar.PlaceholderText;
			};

			toolbar.SearchBar.SearchBar.SizeChanged += (o, e) => {
				if (SearchEntryResized != null)
					SearchEntryResized (o, e);
			};

			toolbar.SearchBar.SearchBar.PreviewKeyDown += (o, e) => {
				var ka = new KeyEventArgs (KeyboardUtil.TranslateToXwtKey (e.Key), KeyboardUtil.GetModifiers (), e.IsRepeat, e.Timestamp);
				if (ka.Key == Xwt.Key.Escape)
					SearchText = string.Empty;

                if (SearchEntryKeyPressed != null)
					SearchEntryKeyPressed (o, ka);
				e.Handled = ka.Handled;
			};
        }

		public WPFToolbar () : this (new ToolBar ())
		{
		}
		
		public IConfigurationModel ActiveConfiguration {
			get { return toolbar.ConfigurationMenu.Active; }
			set { toolbar.ConfigurationMenu.Active = value; }
		}
		
		public IRuntimeModel ActiveRuntime {
			get	{ return toolbar.RuntimeMenu.Active; }
			set	{ toolbar.RuntimeMenu.Active = value; }
		}

		public bool ButtonBarSensitivity {
			set	{ toolbar.ButtonBarPanel.IsEnabled = value; }
		}

		public IEnumerable<IConfigurationModel> ConfigurationModel {
			get	{ return toolbar.ConfigurationMenu.Model; }
			set { toolbar.ConfigurationMenu.Model = value; }
		}
		
		public bool ConfigurationPlatformSensitivity {
			get { return toolbar.ConfigurationMenu.IsEnabled; }
			set { toolbar.ConfigurationMenu.IsEnabled = toolbar.RuntimeMenu.IsEnabled = value; }
		}

		public bool PlatformSensitivity {
			set	{ toolbar.RuntimeMenu.IsEnabled = value; }
		}

		public Gtk.Widget PopupAnchor {
			get	{
				return this;
			}
		}

		public OperationIcon RunButtonIcon {
			set	{ toolbar.RunButton.Icon = value; }
		}

		public bool RunButtonSensitivity {
			get { return toolbar.RunButton.IsEnabled; }
			set { toolbar.RunButton.IsEnabled = value; }
		}
		
		public IEnumerable<IRuntimeModel> RuntimeModel {
			get { return toolbar.RuntimeMenu.Model; }
			set { toolbar.RuntimeMenu.Model = value; }
		}

		public string SearchCategory {
			set	{
				toolbar.SearchBar.SearchText = value;
				toolbar.SearchBar.SearchBar.SelectAll ();
			}
		}

		public IEnumerable<ISearchMenuModel> SearchMenuItems {
			set	{
				toolbar.SearchBar.SearchMenuItems = value;
			}
		}

		public string SearchPlaceholderMessage {
			get { return toolbar.SearchBar.PlaceholderText; }
			set	{ toolbar.SearchBar.PlaceholderText = value; }
		}

		public bool SearchSensivitity {
			set	{ toolbar.SearchBar.IsEnabled = value; }
		}

		public string SearchText {
			get { return toolbar.SearchBar.SearchText; }
			set { toolbar.SearchBar.SearchText = value; }
		}

		public StatusBar StatusBar {
			get	{ return toolbar.StatusBar; }
		}

		public event EventHandler ConfigurationChanged;
		public event EventHandler RunButtonClicked;
		public event EventHandler<MonoDevelop.Components.MainToolbar.HandledEventArgs> RuntimeChanged;
		public event EventHandler SearchEntryActivated;
		public event EventHandler SearchEntryChanged;
		public event EventHandler<KeyEventArgs> SearchEntryKeyPressed;
		public event EventHandler SearchEntryLostFocus;
		public event EventHandler SearchEntryResized;

		public void FocusSearchBar ()
		{
			toolbar.SearchBar.SearchBar.Focus ();
		}

		public void RebuildToolbar (IEnumerable<IButtonBarButton> buttons)
		{
			foreach (var item in toolbar.ButtonBarPanel.Children.OfType<IDisposable> ())
				item.Dispose ();

			toolbar.ButtonBarPanel.Children.Clear ();

			if (!buttons.Any ())
				return;

			var sepStyle = toolbar.FindResource (System.Windows.Controls.ToolBar.SeparatorStyleKey) as System.Windows.Style;

			bool needsSeparator = true;
			foreach (var button in buttons) {
				if (button.IsSeparator) {
					needsSeparator = true;
					continue;
				}

				if (needsSeparator)
					toolbar.ButtonBarPanel.Children.Add (new DottedSeparator {
						Margin = new System.Windows.Thickness {
							Left = 3,
							Right = 3,
						},
						UseLayoutRounding = true,
					});

				toolbar.ButtonBarPanel.Children.Add (new ButtonBarButton (button));
				needsSeparator = false;
			}
		}

		void RaisePropertyChanged ([CallerMemberName] string propName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public class NotNullConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value != null;
		}

		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException ();
		}
    }
}
