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

namespace WindowsPlatform.MainToolbar
{
	public class WPFToolbar : GtkWPFWidget, IMainToolbarView
	{
		ToolBar toolbar;
		WPFToolbar (ToolBar toolbar) : base (toolbar)
		{
			this.toolbar = toolbar;

			toolbar.ConfigurationCombo.DataContext = this;
			toolbar.RuntimeCombo.DataContext = this;

			toolbar.RunButton.Click += (o, e) => {
				if (RunButtonClicked != null)
					RunButtonClicked (o, e);
			};
		}

		public WPFToolbar () : this (new ToolBar ())
		{
		}

		IConfigurationModel activeConfiguration;
		public IConfigurationModel ActiveConfiguration {
			get {
				return (IConfigurationModel)toolbar.ConfigurationCombo.SelectedItem;
            }
			set {
				toolbar.ConfigurationCombo.SelectedItem = value;
			}
		}

		IRuntimeModel activeRuntime;
		public IRuntimeModel ActiveRuntime {
			get	{ return (IRuntimeModel)toolbar.RuntimeCombo.SelectedItem; }
			set	{ toolbar.RuntimeCombo.SelectedItem = value; }
		}

		public bool ButtonBarSensitivity {
			set	{  }
		}

		public IEnumerable<IConfigurationModel> ConfigurationModel {
			get	{ return (IEnumerable<IConfigurationModel>)toolbar.ConfigurationCombo.ItemsSource; }
			set	{ toolbar.ConfigurationCombo.ItemsSource = value; }
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
			set { toolbar.RuntimeCombo.ItemsSource = value; }
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
		public event EventHandler<HandledEventArgs> RuntimeChanged;
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
	}
}
