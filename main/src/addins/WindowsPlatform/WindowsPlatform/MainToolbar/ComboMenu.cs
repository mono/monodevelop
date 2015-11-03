using MonoDevelop.Components.MainToolbar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
			/*
			
            <WrapPanel>
                <TextBlock x:Name="HeaderText" Text="Default" HorizontalAlignment="Left"/>
                <Path Fill="Black" Data="M 0 6 L 6 12 L 12 6 Z" Margin="5,0,0,0" HorizontalAlignment="Right"/>
            </WrapPanel>
	*/
			var content = new StackPanel {
				Orientation = Orientation.Horizontal,
				Height = 18,
			};

			Foreground = Styles.MainToolbarForegroundBrush;
			content.Children.Add (new TextBlock {
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
			});

			var arrow = new Polygon {
				Fill = Styles.MainToolbarForegroundBrush,
				Margin = new Thickness (5, 0, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
			};

			arrow.Points.Add (new Point (0, 3));
			arrow.Points.Add (new Point (3, 6));
			arrow.Points.Add (new Point (6, 3));
			content.Children.Add (arrow);

			Items.Add (new MenuItem {
				Header = content,
			});
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
				DropMenuText = active == null ? "Default" : active.DisplayString;
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
			}
		}

		void OnMenuItemClicked (object sender, RoutedEventArgs args)
		{
			var item = (ConfigurationMenuItem)sender;
			var old = active;

			if (SelectionChanged != null)
				SelectionChanged (this, new SelectionChangedEventArgs<IConfigurationModel> (item.Model, old));
        }

		class ConfigurationMenuItem : MenuItem
		{
			public ConfigurationMenuItem (IConfigurationModel model)
			{
				Model = model;
				Header = model.DisplayString;
			}
			public IConfigurationModel Model { get; private set; }
		}

		public override event EventHandler<SelectionChangedEventArgs<IConfigurationModel>> SelectionChanged;
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
					return;
				}

				active = menuItem.Model;
				menuItem.Margin = new Thickness (0, 0, 0, 0);
				menuItem.FontWeight = FontWeights.Normal;

				using (var mutableModel = active.GetMutableModel ()) {
					DropMenuText = mutableModel.FullDisplayString;
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
				
				FillSource (DropMenu.Items, value);

				int count = DropMenu.Items.Count;
				IsEnabled = Focusable = IsHitTestVisible = count > 1;
				if (count == 0)
					DropMenuText = "Default";
			}
		}

		void OnMenuItemClicked (object sender, RoutedEventArgs args)
		{
			var item = (RuntimeMenuItem)sender;

			if (SelectionChanged != null)
				SelectionChanged (this, new SelectionChangedEventArgs<IRuntimeModel> (item.Model, Active));
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
					source.Add (new Separator ());
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

		class RuntimeMenuItem : MenuItem
		{
			public IRuntimeModel Model { get; private set; }
			public RuntimeMenuItem (IRuntimeModel model)
			{
				Model = model;

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
