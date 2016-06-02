using MonoDevelop.Components.MainToolbar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WindowsPlatform.MainToolbar
{
	public class SelectionChangedEventArgs<T> : EventArgs
	{
		public T Added { get; private set; }
		public T Removed { get; private set; }

		public SelectionChangedEventArgs (T added, T removed)
		{
			Added = added;
			Removed = removed;
		}
	}
	public abstract class ComboMenu<T> : Menu
	{
		protected ComboMenu () : base ()
		{
			UseLayoutRounding = true;

			var bindingFgColor = new Binding {
				Path = new PropertyPath (typeof(Styles).GetProperty("MainToolbarForegroundBrush")),
				Mode = BindingMode.OneWay,
			};
			var bindingDisabledFgColor = new Binding {
				Path = new PropertyPath (typeof(Styles).GetProperty("MainToolbarDisabledForegroundBrush")),
				Mode = BindingMode.OneWay,
			};

			var content = new StackPanel {
				Orientation = Orientation.Horizontal,
				Height = 20,
			};

			var textBlock = new TextBlock
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 0, 2),
			};
            content.Children.Add (textBlock);

			var arrow = new Polygon {
				Margin = new Thickness (5, 0, 0, 2),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
			};
			arrow.SetBinding (Shape.FillProperty, new Binding ("Foreground") { Source = this });

			IsEnabledChanged += (o, e) =>
			{
				textBlock.SetBinding(Control.ForegroundProperty, (bool)e.NewValue ? bindingFgColor : bindingDisabledFgColor);
				arrow.SetBinding(Polygon.FillProperty, (bool)e.NewValue ? bindingFgColor : bindingDisabledFgColor);
			};

			arrow.Points.Add (new Point (0, 3));
			arrow.Points.Add (new Point (3, 6));
			arrow.Points.Add (new Point (6, 3));
			content.Children.Add (arrow);

			Items.Add (new SimpleMenuItem {
				Header = content,
				UseLayoutRounding = true,
			});
			IsEnabled = false;
			DropMenuText = "Default";
        }

		protected MenuItem DropMenu
		{
			get { return (MenuItem)Items[0]; }
		}

		protected string DropMenuText
		{
			get { return ((TextBlock)((StackPanel)DropMenu.Header).Children[0]).Text; }
			set { ((TextBlock)((StackPanel)DropMenu.Header).Children[0]).Text = value; }
		} 

		public abstract T Active
		{
			get; set;
		}

		public abstract IEnumerable<T> Model
		{
			get; set;
		}

		public abstract event EventHandler<SelectionChangedEventArgs<T>> SelectionChanged;
	}

	public class ConfigurationComboMenu : ComboMenu<IConfigurationModel>
	{
		public ConfigurationComboMenu ()
		{
		}

		IConfigurationModel active;
		public override IConfigurationModel Active
		{
			get
			{
				return active;
			}

			set
			{
				active = model.FirstOrDefault (cm => cm.OriginalId == value.OriginalId);
				if (active == null)
				{
					DropMenuText = "Default";
					IsEnabled = false;
				}
				else
				{
					DropMenuText = active.DisplayString;
					IsEnabled = true;
				}
			}
		}

		IEnumerable<IConfigurationModel> model;
		public override IEnumerable<IConfigurationModel> Model
		{
			get
			{
				return model;
			}

			set
			{
				int count = value.Count ();
				model = value;

				var dropMenu = DropMenu;
				var open = dropMenu.IsSubmenuOpen;
				if (open)
					dropMenu.IsSubmenuOpen = false;

				foreach (MenuItem item in dropMenu.Items)
					item.Click -= OnMenuItemClicked;
				dropMenu.Items.Clear ();
				foreach (var item in value) {
					var menuItem = new ConfigurationMenuItem (item);
					menuItem.Click += OnMenuItemClicked;
                    dropMenu.Items.Add (menuItem);
				}
				IsEnabled = Focusable = IsHitTestVisible = dropMenu.Items.Count > 1;
				if (count == 0)
					DropMenuText = "Default";

				dropMenu.IsSubmenuOpen = open;
			}
		}

		void OnMenuItemClicked (object sender, RoutedEventArgs args)
		{
			var item = (ConfigurationMenuItem)sender;
			var old = active;

			SelectionChanged?.Invoke (this, new SelectionChangedEventArgs<IConfigurationModel> (item.Model, old));
        }

		class ConfigurationMenuItem : SimpleMenuItem
		{
			public ConfigurationMenuItem (IConfigurationModel model)
			{
				Model = model;
				Header = model.DisplayString;
				UseLayoutRounding = true;
			}
			public IConfigurationModel Model { get; private set; }
		}

		public override event EventHandler<SelectionChangedEventArgs<IConfigurationModel>> SelectionChanged;
	}

	public class RunConfigurationComboMenu : ComboMenu<IRunConfigurationModel>
	{
		public RunConfigurationComboMenu ()
		{
		}

		IRunConfigurationModel active;
		public override IRunConfigurationModel Active {
			get {
				return active;
			}

			set {
				active = model.FirstOrDefault (cm => cm.OriginalId == value.OriginalId);
				if (active == null) {
					DropMenuText = "Default";
					IsEnabled = false;
				} else {
					DropMenuText = active.DisplayString;
					IsEnabled = true;
				}
			}
		}

		IEnumerable<IRunConfigurationModel> model;
		public override IEnumerable<IRunConfigurationModel> Model {
			get {
				return model;
			}

			set {
				int count = value.Count ();
				model = value;

				var dropMenu = DropMenu;
				var open = dropMenu.IsSubmenuOpen;
				if (open)
					dropMenu.IsSubmenuOpen = false;

				foreach (MenuItem item in dropMenu.Items)
					item.Click -= OnMenuItemClicked;
				dropMenu.Items.Clear ();
				foreach (var item in value) {
					var menuItem = new ConfigurationMenuItem (item);
					menuItem.Click += OnMenuItemClicked;
					dropMenu.Items.Add (menuItem);
				}
				IsEnabled = Focusable = IsHitTestVisible = dropMenu.Items.Count > 1;
				if (count == 0)
					DropMenuText = "Default";

				dropMenu.IsSubmenuOpen = open;
			}
		}

		void OnMenuItemClicked (object sender, RoutedEventArgs args)
		{
			var item = (ConfigurationMenuItem)sender;
			var old = active;

			SelectionChanged?.Invoke (this, new SelectionChangedEventArgs<IRunConfigurationModel> (item.Model, old));
		}

		class ConfigurationMenuItem : SimpleMenuItem
		{
			public ConfigurationMenuItem (IRunConfigurationModel model)
			{
				Model = model;
				Header = model.DisplayString;
				UseLayoutRounding = true;
			}
			public IRunConfigurationModel Model { get; private set; }
		}

		public override event EventHandler<SelectionChangedEventArgs<IRunConfigurationModel>> SelectionChanged;
	}

	public class RuntimeComboMenu : ComboMenu<IRuntimeModel>
	{
		public RuntimeComboMenu ()
		{
			DropMenu.SubmenuOpened += (o, e) => {
				var menu = (MenuItem)o;

				foreach (var item in menu.Items.OfType<RuntimeMenuItem> ())
					item.Update ();
			};
		}

		IRuntimeModel active;
		public override IRuntimeModel Active
		{
			get
			{
				return active;
            }

			set
			{
				var menuItem = DropMenu.Items
					.OfType<RuntimeMenuItem> ()
					.FirstOrDefault (it => it.Model == value);

				if (menuItem == null) {
					active = null;
					DropMenuText = "Default";
					IsEnabled = false;
					return;
				}

				active = menuItem.Model;
				menuItem.Margin = new Thickness (0, 0, 0, 0);
				menuItem.FontWeight = FontWeights.Normal;

				using (var mutableModel = active.GetMutableModel ()) {
					DropMenuText = mutableModel.FullDisplayString;
					IsEnabled = true;
				}
            }
		}

		IEnumerable<IRuntimeModel> model;
		public override IEnumerable<IRuntimeModel> Model
		{
			get
			{
				return model;
			}

			set
			{
				model = value;

				var open = DropMenu.IsSubmenuOpen;
				if (open)
					DropMenu.IsSubmenuOpen = false;
				FillSource (DropMenu.Items, value);



				int count = DropMenu.Items.Count;
				IsEnabled = Focusable = IsHitTestVisible = count > 1;
				if (count == 0)
					DropMenuText = "Default";

				DropMenu.IsSubmenuOpen = open;
			}
		}

		void OnMenuItemClicked (object sender, RoutedEventArgs args)
		{
			var item = (RuntimeMenuItem)sender;

			SelectionChanged?.Invoke (this, new SelectionChangedEventArgs<IRuntimeModel> (item.Model, Active));
		}

		void FillSource (ItemCollection source, IEnumerable<IRuntimeModel> model)
		{
			foreach (var item in source.OfType<MenuItem> ()) {
				item.Click -= OnMenuItemClicked;
				foreach (var subItem in item.Items.OfType<MenuItem> ())
					subItem.Click -= OnMenuItemClicked;
			}

			source.Clear ();

			foreach (var item in model) {
				if (item.HasParent)
					continue;

				if (item.IsSeparator)
					source.Add (new Separator { UseLayoutRounding = true, });
				else {
					var menuItem = new RuntimeMenuItem (item);
					menuItem.Click += OnMenuItemClicked;
					foreach (var child in item.Children) {
						var childMenuItem = new RuntimeMenuItem (item);
						childMenuItem.Click += OnMenuItemClicked;
						menuItem.Items.Add (childMenuItem);
					}
					source.Add (menuItem);
				}
			}
		}

		public override event EventHandler<SelectionChangedEventArgs<IRuntimeModel>> SelectionChanged;

		class RuntimeMenuItem : SimpleMenuItem
		{
			public IRuntimeModel Model { get; private set; }
			public RuntimeMenuItem (IRuntimeModel model)
			{
				Model = model;
				UseLayoutRounding = true;

				Margin = new Thickness (model.IsIndented ? 15 : 0, 0, 0, 0);
				if (model.Notable)
					FontWeight = FontWeights.Bold;
			}

			public void Update ()
			{
				using (var mutableModel = Model.GetMutableModel ()) {
					Header = mutableModel.DisplayString;
					IsEnabled = mutableModel.Enabled;
					Visibility = mutableModel.Visible ? Visibility.Visible : Visibility.Collapsed;
				}

				foreach (var item in Items.OfType<RuntimeMenuItem> ())
					item.Update ();
			}
		}
	}
}
