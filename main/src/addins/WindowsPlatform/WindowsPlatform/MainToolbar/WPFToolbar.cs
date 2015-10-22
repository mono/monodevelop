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
			
			toolbar.ConfigurationCombo.DataContext = this;

			toolbar.ConfigurationCombo.SelectionChanged += (o, e) => {
				if (e.AddedItems.Count == 0)
					return;

				var newModel = (IConfigurationModel)e.AddedItems[0];
				if (newModel == null)
					return;
				
				ActiveConfiguration = newModel;
				if (ConfigurationChanged != null)
					ConfigurationChanged (o, e);
			};

			toolbar.RuntimeCombo.DataContext = this;
			toolbar.RuntimeCombo.SelectionChanged += (o, e) => {
				if (e.AddedItems.Count == 0)
					return;

				var selected = (RuntimeMenuItem)e.AddedItems[0];
				if (selected == null || selected.Model == null)
					return;

				IRuntimeModel newModel = selected.Model;
				using (var mutableModel = newModel.GetMutableModel ()) {
					ActiveRuntime = newModel;
					var ea = new MonoDevelop.Components.MainToolbar.HandledEventArgs ();
					if (RuntimeChanged != null)
						RuntimeChanged (o, ea);

					if (ea.Handled)
						ActiveRuntime = ((RuntimeMenuItem)e.RemovedItems[0]).Model;
				}
			};
			
			toolbar.RuntimeCombo.DropDownOpened += (o, e) => {
				foreach (var item in toolbar.RuntimeCombo.Items.OfType<RuntimeMenuItem> ())
					item.Update ();
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
			};

			toolbar.SearchBar.SearchBar.SizeChanged += (o, e) => {
				if (SearchEntryResized != null)
					SearchEntryResized (o, e);
			};

			toolbar.SearchBar.SearchBar.PreviewKeyDown += (o, e) => {
				var ka = new KeyEventArgs (KeyboardUtil.TranslateToXwtKey (e.Key), KeyboardUtil.GetModifiers (), e.IsRepeat, e.Timestamp);
                if (SearchEntryKeyPressed != null)
					SearchEntryKeyPressed (o, ka);
				e.Handled = ka.Handled;
			};
        }

		public WPFToolbar () : this (new ToolBar ())
		{
		}
		
		public IConfigurationModel ActiveConfiguration {
			get { return (IConfigurationModel)toolbar.ConfigurationCombo.SelectedItem; }
			set {
				toolbar.ConfigurationCombo.SelectedItem = toolbar.ConfigurationCombo.Items
					.Cast<IConfigurationModel>()
					.FirstOrDefault (it => it.OriginalId == value.OriginalId);
				RaisePropertyChanged ();
			}
		}
		
		public IRuntimeModel ActiveRuntime {
			get	{ return ((RuntimeMenuItem)toolbar.RuntimeCombo.SelectedItem).Model; }
			set	{
				toolbar.RuntimeCombo.SelectedItem = toolbar.RuntimeCombo.Items
					.OfType<RuntimeMenuItem> ()
					.FirstOrDefault (it => it.Model == value);

				var item = (RuntimeMenuItem)toolbar.RuntimeCombo.SelectedItem;
				item.Margin = new System.Windows.Thickness (0, 0, 0, 0);
				item.FontWeight = System.Windows.FontWeights.Normal;

				if (item != null)
					using (var mutableModel = item.Model.GetMutableModel ()) {
						item.Header = mutableModel.FullDisplayString;
					}

				RaisePropertyChanged ();
			}
		}

		public bool ButtonBarSensitivity {
			set	{ toolbar.ButtonBarPanel.IsEnabled = value; }
		}

		public IEnumerable<IConfigurationModel> ConfigurationModel {
			get	{ return (IEnumerable<IConfigurationModel>)toolbar.ConfigurationCombo.ItemsSource; }
			set	{
				int count = value.Count ();
				toolbar.ConfigurationCombo.IsEditable = count == 0;
				toolbar.ConfigurationCombo.IsEnabled = toolbar.ConfigurationCombo.Focusable = toolbar.ConfigurationCombo.IsHitTestVisible = count > 1;
				toolbar.ConfigurationCombo.ItemsSource = value;
				if (count == 0)
					toolbar.ConfigurationCombo.Text = "Default";
			}
		}
		
		public bool ConfigurationPlatformSensitivity {
			get { return toolbar.ConfigurationCombo.IsEnabled; }
			set { toolbar.ConfigurationCombo.IsEnabled = toolbar.RuntimeCombo.IsEnabled = value; }
		}

		public bool PlatformSensitivity {
			set	{ toolbar.RuntimeCombo.IsEnabled = value; }
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

		IEnumerable<IRuntimeModel> runtimeModel;
		public IEnumerable<IRuntimeModel> RuntimeModel {
			get { return runtimeModel; }
			set {
				runtimeModel = value;
				var source = new List<Control> ();
				FillSource (source, value);

				int count = source.Count;
				toolbar.RuntimeCombo.IsEditable = count == 0;
				toolbar.RuntimeCombo.IsEnabled = toolbar.RuntimeCombo.Focusable = toolbar.RuntimeCombo.IsHitTestVisible = count > 1;
				toolbar.RuntimeCombo.ItemsSource = source;
				if (count == 0)
					toolbar.RuntimeCombo.Text = "Default";
			}
		}

		void FillSource (List<Control> source, IEnumerable<IRuntimeModel> model)
		{
			foreach (var item in model) {
				if (item.HasParent)
					continue;

				if (item.IsSeparator)
					source.Add (new System.Windows.Controls.Separator ());
				else {
					var menuItem = new RuntimeMenuItem (item);
					foreach (var child in item.Children)
						menuItem.Items.Add (new RuntimeMenuItem (item));

					source.Add (menuItem);
				}
			}
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
					toolbar.ButtonBarPanel.Children.Add (new System.Windows.Controls.Separator {
						Style = sepStyle,
						MinWidth = 2,
						Margin = new System.Windows.Thickness{
							Left = 3,
							Right = 3,
						},
					});

				toolbar.ButtonBarPanel.Children.Add (new ButtonBarButton ((ImageSource)MonoDevelop.Platform.WindowsPlatform.WPFToolkit.GetNativeImage (ImageService.GetIcon (Stock.Add)), button));
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

	class RuntimeMenuItem : System.Windows.Controls.MenuItem
	{
		public IRuntimeModel Model { get; private set; }
		public RuntimeMenuItem (IRuntimeModel model)
		{
			Model = model;

			Margin = new System.Windows.Thickness (model.IsIndented ? 15 : 0, 0, 0, 0);
			if (model.Notable)
				FontWeight = System.Windows.FontWeights.Bold;
		}

		public void Update ()
		{
			using (var mutableModel = Model.GetMutableModel ()) {
				Header = mutableModel.DisplayString;
				IsEnabled = mutableModel.Enabled;
				Visibility = mutableModel.Visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
			}

			foreach (var item in Items.OfType<RuntimeMenuItem> ())
				item.Update ();
		}
	}
}
