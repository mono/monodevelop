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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

				ActiveConfiguration = newModel;
				if (newModel == null) {
					toolbar.ConfigurationCombo.Text = "Default";
				}
			};

			toolbar.RuntimeCombo.DataContext = this;
			toolbar.RuntimeCombo.SelectionChanged += (o, e) => {
				//var oldModel = (IRuntimeModel)e.RemovedItems[0];
				//var newModel = (IRuntimeModel)e.AddedItems[0];
				//if (newModel == null)
				//	return;

				//using (var mutableModel = newModel.GetMutableModel ()) {
				//	if (newModel == null)
				//		return;

				//	ActiveRuntime = newModel;
				//	var ea = new HandledEventArgs ();
				//	if (RuntimeChanged != null)
				//		RuntimeChanged (o, ea);

				//	if (ea.Handled)
				//		using (var oldRuntimeMutableModel = oldModel.GetMutableModel ()) {
				//			ActiveRuntime = RuntimeModel.First (r => {
				//				using (var newRuntimeMutableModel = r.GetMutableModel ())
				//					return newRuntimeMutableModel.FullDisplayString == oldRuntimeMutableModel.FullDisplayString;
				//			});
				//		};
				//}
			};

			toolbar.RunButton.Click += (o, e) => {
				if (RunButtonClicked != null)
					RunButtonClicked (o, e);
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
			get	{ return (IRuntimeModel)toolbar.RuntimeCombo.SelectedItem; }
			set	{
				toolbar.RuntimeCombo.SelectedItem = value;
				RaisePropertyChanged ();
			}
		}

		public bool ButtonBarSensitivity {
			set	{  }
		}

		public IEnumerable<IConfigurationModel> ConfigurationModel {
			get	{ return (IEnumerable<IConfigurationModel>)toolbar.ConfigurationCombo.ItemsSource; }
			set	{
				toolbar.ConfigurationCombo.ItemsSource = value;
				if (!value.Any ())
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
				throw new NotImplementedException ();
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
			get { return (IEnumerable<IRuntimeModel>)toolbar.RuntimeCombo.ItemsSource; }
			set {
				toolbar.RuntimeCombo.ItemsSource = value;
				if (!value.Any ())
					toolbar.RuntimeCombo.Text = "Default";
			}
		}

		public string SearchCategory {
			set	{
			}
		}

		public IEnumerable<ISearchMenuModel> SearchMenuItems {
			set	{
			}
		}

		public string SearchPlaceholderMessage {
			set	{
			}
		}

		public bool SearchSensivitity {
			set	{
			}
		}

		public string SearchText {
			get; set;
			//get	{
			//	throw new NotImplementedException ();
			//}
			//set	{
			//	throw new NotImplementedException ();
			//}
		}

		public StatusBar StatusBar {
			get	{
				return toolbar.StatusBar;
			}
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
			toolbar.SearchBar.Focus ();
		}

		public void RebuildToolbar (IEnumerable<IButtonBarButton> buttons)
		{
			//throw new NotImplementedException ();
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
