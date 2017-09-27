using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Components.Windows;
using MonoDevelop.Core;
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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Globalization;
using System.Windows.Data;
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

				Runtime.RunInMainThread(() => {
					ActiveConfiguration = newModel;
					ConfigurationChanged?.Invoke (o, e);
				});
			};

			toolbar.RunConfigurationMenu.SelectionChanged += (o, e) => {
				var comboMenu = (ComboMenu<IRunConfigurationModel>)o;
				var newModel = e.Added;
				if (newModel == null)
					return;

				Runtime.RunInMainThread (() => {
					ActiveRunConfiguration = newModel;
					RunConfigurationChanged?.Invoke (o, e);
				});
			};

			toolbar.RuntimeMenu.SelectionChanged += (o, e) => {
				var newModel = e.Added;
				if (newModel == null)
					return;

				using (var mutableModel = newModel.GetMutableModel()) {
					Runtime.RunInMainThread(() => {
						ActiveRuntime = newModel;

						var ea = new MonoDevelop.Components.MainToolbar.HandledEventArgs();
						RuntimeChanged?.Invoke (o, ea);

						if (ea.Handled)
							ActiveRuntime = e.Removed;
					});
				}
			};

			toolbar.RunButton.Click += (o, e) => {
				if (RunButtonClicked != null)
					RunButtonClicked (o, e);
			};

			toolbar.SearchBar.SearchBar.TextChanged += (o, e) => {
				if (SearchText == SearchPlaceholderMessage)
					return;

				if (SearchEntryChanged != null)
					SearchEntryChanged (o, e);
			};

			toolbar.SearchBar.SearchBar.LostKeyboardFocus += (o, e) => {
				if (SearchEntryLostFocus != null)
					SearchEntryLostFocus (o, e);
				toolbar.SearchBar.SearchText = toolbar.SearchBar.PlaceholderText;
			};

			toolbar.SearchBar.SearchBar.GotKeyboardFocus += (o, e) => {
				SearchEntryActivated?.Invoke (o, e);
			};

			toolbar.SearchBar.SearchBar.SizeChanged += (o, e) => {
				if (SearchEntryResized != null)
					SearchEntryResized (o, e);
			};

			toolbar.SearchBar.SearchBar.PreviewKeyDown += (o, e) => {
				var ka = new KeyEventArgs(KeyboardUtil.TranslateToXwtKey(e.Key), KeyboardUtil.GetModifiers(), e.IsRepeat, e.Timestamp);
				SendKeyPress(ka);
				e.Handled = ka.Handled;
			};

			toolbar.SearchBar.ClearIconClicked += (o, e) =>
			{
				SendKeyPress(new KeyEventArgs(Xwt.Key.Escape, KeyboardUtil.GetModifiers(), false, 0));
			};
        }

        protected override void RepositionWpfWindow()
        {
            int scale = (int)MonoDevelop.Components.GtkWorkarounds.GetScaleFactor(this);
            RepositionWpfWindow (scale, scale);
        }

        void SendKeyPress(KeyEventArgs ka)
		{
			if (SearchEntryKeyPressed != null)
				SearchEntryKeyPressed(this, ka);
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

		public IRunConfigurationModel ActiveRunConfiguration {
			get { return toolbar.RunConfigurationMenu.Active; }
			set { toolbar.RunConfigurationMenu.Active = value; }
		}

		public bool ButtonBarSensitivity {
			set	{ toolbar.ButtonBarPanel.IsEnabled = value; }
		}

		public IEnumerable<IConfigurationModel> ConfigurationModel {
			get	{ return toolbar.ConfigurationMenu.Model; }
			set { toolbar.ConfigurationMenu.Model = value; }
		}

		public IEnumerable<IRuntimeModel> RuntimeModel {
			get { return toolbar.RuntimeMenu.Model; }
			set { toolbar.RuntimeMenu.Model = value; }
		}

		public IEnumerable<IRunConfigurationModel> RunConfigurationModel {
			get { return toolbar.RunConfigurationMenu.Model; }
			set { toolbar.RunConfigurationMenu.Model = value; }
		}

		bool configurationPlatformSensitivity;
        public bool ConfigurationPlatformSensitivity {
			get { return configurationPlatformSensitivity; }
			set {
				configurationPlatformSensitivity = value;
				toolbar.ConfigurationMenu.IsEnabled = value && ConfigurationModel.Count() > 1;
				toolbar.RuntimeMenu.IsEnabled = value && RuntimeModel.Count() > 1;
            }
		}
		
		public bool PlatformSensitivity {
			set {
				toolbar.RuntimeMenu.IsEnabled = value && RuntimeModel.Count() > 1;
			}
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

		public bool RunConfigurationVisible {
			get { return toolbar.RunConfigurationMenu.IsVisible; }
			set {
				System.Windows.Visibility visible = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
				toolbar.RunConfigurationMenu.Visibility = visible;
				toolbar.RunConfigurationSeparator.Visibility = visible;
			}
		}

		public string SearchCategory {
			set	{
				toolbar.SearchBar.SearchText = value;
				FocusSearchBar ();
				toolbar.SearchBar.SearchBar.Select (value.Length, 0);
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
			set {
				toolbar.SearchBar.SearchText = value;

				if (value != SearchPlaceholderMessage) {
					toolbar.SearchBar.SearchBar.SelectAll ();
				}
			}
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

		public void RebuildToolbar (IEnumerable<ButtonBarGroup> groups)
		{
			foreach (var item in toolbar.ButtonBarPanel.Children.OfType<IDisposable> ())
				item.Dispose ();

			toolbar.ButtonBarPanel.Children.Clear ();

			// Remove empty groups so we know when to put a separator
			var groupList = groups.ToList ();
			groupList.RemoveAll ((g) => g.Buttons.Count == 0);

			int idx = 0;
			int count = groupList.Count;

			var sepStyle = toolbar.FindResource (System.Windows.Controls.ToolBar.SeparatorStyleKey) as System.Windows.Style;

			foreach (var buttonGroup in groupList) {
				bool needsSeparator = (idx < count - 1);
				foreach (var button in buttonGroup.Buttons) {
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

				idx++;
			}
		}

		void RaisePropertyChanged ([CallerMemberName] string propName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new System.ComponentModel.PropertyChangedEventArgs (propName));
		}

		public void Focus ()
		{
			FocusSearchBar ();
		}

		public void Focus (System.Action exitAction)
		{
			FocusSearchBar ();
			exitAction ();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler RunConfigurationChanged;
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
