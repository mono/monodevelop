using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace WindowsPlatform.MainToolbar
{
	public class TitleMenuItem : MenuItem
	{
		public TitleMenuItem (MonoDevelop.Components.Commands.CommandManager manager, CommandEntry entry, CommandSource commandSource = CommandSource.MainMenu, object initialCommandTarget = null)
		{
			this.manager = manager;
			this.initialCommandTarget = initialCommandTarget;
			this.commandSource = commandSource;

			menuEntry = entry;
			menuEntrySet = entry as CommandEntrySet;
			menuLinkEntry = entry as LinkCommandEntry;

			if (menuEntrySet != null) {
				Header = menuEntrySet.Name;

				foreach (CommandEntry item in menuEntrySet) {
					if (item.CommandId == MonoDevelop.Components.Commands.Command.Separator) {
						Items.Add (new Separator ());
					} else
						Items.Add (new TitleMenuItem (manager, item));
				}
			} else if (menuLinkEntry != null) {
				Header = menuLinkEntry.Text;
			} else {
				actionCommand = manager.GetCommand (menuEntry.CommandId) as ActionCommand;
				IsCheckable = actionCommand.CommandArray;

				// TODO: Load WPF Xwt engine and use the native image backend.
				//Icon = ImageService.GetIcon (actionCommand.Icon);
			}
		}

		/// <summary>
		/// Updates a command entry. IncludeChildren will be used to detect whether the command was issued from a top level node.
		/// This will update all the menu's children on a first call, then only the nodes themselves on the recursive call.
		/// </summary>
		/// <param name="includeChildren">If set to <c>true</c> include children.</param>
		void Update (bool includeChildren)
		{
			if (menuLinkEntry != null) {
				Click += OnMenuLinkClicked;
				return;
			}

			if (menuEntrySet != null) {
				if (includeChildren) {
					for (int i = 0; i < Items.Count; ++i) {
						var titleMenuItem = Items[i] as TitleMenuItem;

						if (titleMenuItem != null) {
							titleMenuItem.Update (false);
							continue;
						}

						// If we have a separator, don't draw another one if the previous visible item is a separator.
						var separatorMenuItem = Items [i] as Separator;
						separatorMenuItem.Visibility = System.Windows.Visibility.Hidden;
						for (int j = i - 1; j >= 0; --j) {
							var iterMenuItem = Items [j] as Control;

							if (iterMenuItem is Separator)
								break;

							if (iterMenuItem.Visibility != System.Windows.Visibility.Visible)
								continue;

							separatorMenuItem.Visibility = System.Windows.Visibility.Visible;
							break;
						}
					}
				}
				return;
			}

			var info = manager.GetCommandInfo (menuEntry.CommandId);
			if (actionCommand != null) {
				if (!string.IsNullOrEmpty (info.Description) && ToolTip != info.Description)
					ToolTip = info.Description;

				bool enabled = info.Enabled;
				bool visible = info.Visible && (menuEntry.DisabledVisible || info.Enabled);

				IsEnabled = enabled;
				Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

				IsChecked = info.Checked || info.CheckedInconsistent;
			}

			Header = info.Text;
			Visibility = info.Visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
			Click += OnMenuClicked;
		}

		/// <summary>
		/// Clears a command entry's saved data. IncludeChildren will be used to detect whether the command was issued from a top level node.
		/// This will update all the menu's children on a first call, then only the nodes themselves on the recursive call.
		/// </summary>
		/// <param name="includeChildren">If set to <c>true</c> include children.</param>
		void Clear (bool includeChildren)
		{
			if (menuLinkEntry != null) {
				Click -= OnMenuLinkClicked;
				return;
			}

			if (menuEntrySet != null) {
				if (includeChildren) {
					foreach (var item in Items) {
						var titleMenuItem = item as TitleMenuItem;
						if (titleMenuItem == null)
							continue;

						titleMenuItem.Clear (false);
					}
				}
				return;
			}

			Click -= OnMenuClicked;
		}

		protected override void OnSubmenuOpened (System.Windows.RoutedEventArgs e)
		{
			Update (includeChildren: true);
			base.OnSubmenuOpened (e);
		}

		protected override void OnSubmenuClosed (System.Windows.RoutedEventArgs e)
		{
			Clear (includeChildren: true);
			base.OnSubmenuClosed (e);
		}

		void OnMenuClicked (object sender, System.Windows.RoutedEventArgs e)
		{
			//if (array != null) {
			//	manager.DispatchCommand (menuEntry.CommandId, actionCommand.Info.DataItem, initialCommandTarget, commandSource);
			//} else {
				manager.DispatchCommand (menuEntry.CommandId, null, initialCommandTarget, commandSource);
			//}
		}

		void OnMenuLinkClicked (object sender, System.Windows.RoutedEventArgs e)
		{
			DesktopService.ShowUrl (menuLinkEntry.Url);
		}

		MonoDevelop.Components.Commands.CommandManager manager;
		object initialCommandTarget;
		CommandSource commandSource;
		readonly ActionCommand actionCommand;
		readonly CommandEntry menuEntry;
		readonly CommandEntrySet menuEntrySet;
		readonly LinkCommandEntry menuLinkEntry;
	}
}
