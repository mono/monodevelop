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
		public TitleMenuItem (MonoDevelop.Components.Commands.CommandManager manager, CommandEntry entry, CommandInfo commandArrayInfo = null, CommandSource commandSource = CommandSource.MainMenu, object initialCommandTarget = null)
		{
			this.manager = manager;
			this.initialCommandTarget = initialCommandTarget;
			this.commandSource = commandSource;
			this.commandArrayInfo = commandArrayInfo;

			menuEntry = entry;
			menuEntrySet = entry as CommandEntrySet;
			menuLinkEntry = entry as LinkCommandEntry;

			if (commandArrayInfo != null) {
				Header = commandArrayInfo.Text;

				var commandArrayInfoSet = commandArrayInfo as CommandInfoSet;
				if (commandArrayInfoSet != null) {
					foreach (var item in commandArrayInfoSet.CommandInfos) {
						if (item.IsArraySeparator)
							Items.Add (new Separator ());
						else
							Items.Add (new TitleMenuItem (manager, entry, item, commandSource, initialCommandTarget));
					}
				}
			}

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
			} else if (entry != null) {
				actionCommand = manager.GetCommand (menuEntry.CommandId) as ActionCommand;
				if (actionCommand == null)
					return;

				IsCheckable = actionCommand.ActionType == ActionType.Check;

				// FIXME: Use proper keybinding text.
				if (actionCommand.KeyBinding != null)
					InputGestureText = actionCommand.KeyBinding.ToString ();

				try {
					if (!actionCommand.Icon.IsNull)
						Icon = new Image { Source = ImageService.GetIcon (actionCommand.Icon).WithSize (Xwt.IconSize.Small).GetImageSource () };
				} catch (Exception ex) {
					MonoDevelop.Core.LoggingService.LogError ("Failed loading menu icon: " + actionCommand.Icon, ex);
				}
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

			if (menuEntrySet != null || commandArrayInfo is CommandInfoSet) {
				if (includeChildren) {
					for (int i = 0; i < Items.Count; ++i) {
						var titleMenuItem = Items[i] as TitleMenuItem;

						if (titleMenuItem != null) {
							titleMenuItem.Update (false);
							continue;
						}

						// If we have a separator, don't draw another one if the previous visible item is a separator.
						var separatorMenuItem = Items [i] as Separator;
						separatorMenuItem.Visibility = System.Windows.Visibility.Collapsed;
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
					if (menuEntrySet != null && menuEntrySet.AutoHide)
						Visibility = Items.Cast<Control> ().Any (item => item.Visibility == System.Windows.Visibility.Visible) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
				}
				return;
			}

			var info = manager.GetCommandInfo (menuEntry.CommandId, new CommandTargetRoute (initialCommandTarget));
			if (actionCommand != null) {
				if (!string.IsNullOrEmpty (info.Description) && (string)ToolTip != info.Description)
					ToolTip = info.Description;

				if (actionCommand.CommandArray && commandArrayInfo == null) {
					Visibility = System.Windows.Visibility.Collapsed;

					var parent = (TitleMenuItem)Parent;

					int count = 1;
					int indexOfThis = parent.Items.IndexOf (this);
					foreach (var child in info.ArrayInfo) {
						Control toAdd;
						if (child.IsArraySeparator) {
							toAdd = new Separator ();
						} else {
							toAdd = new TitleMenuItem (manager, menuEntry, child);
						}

						toRemoveFromParent.Add (toAdd);
						parent.Items.Insert (indexOfThis + (count++), toAdd);
					}
					return;
				}
			}

			SetInfo (commandArrayInfo != null ? commandArrayInfo : info);
			Click += OnMenuClicked;
		}

		void SetInfo (CommandInfo info)
		{
			Header = info.Text;
			IsEnabled = info.Enabled;
			Visibility = info.Visible && (menuEntry.DisabledVisible || IsEnabled) ?
				System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
			IsChecked = info.Checked || info.CheckedInconsistent;
			ToolTip = info.Description;
		}

		/// <summary>
		/// Clears a command entry's saved data. IncludeChildren will be used to detect whether the command was issued from a top level node.
		/// This will update all the menu's children on a first call, then only the nodes themselves on the recursive call.
		/// </summary>
		/// <param name="includeChildren">If set to <c>true</c> include children.</param>
		IEnumerable<Control> Clear (bool includeChildren)
		{
			if (menuLinkEntry != null) {
				Click -= OnMenuLinkClicked;
				return Enumerable.Empty<TitleMenuItem> ();
			}

			if (menuEntrySet != null) {
				if (includeChildren) {
					var toRemove = Enumerable.Empty<Control> ();
                    foreach (var item in Items) {
						var titleMenuItem = item as TitleMenuItem;
						if (titleMenuItem == null)
							continue;

						toRemove = toRemove.Concat (titleMenuItem.Clear (false));
					}

					foreach (var item in toRemove)
						Items.Remove (item);
				}
				return Enumerable.Empty<TitleMenuItem> (); 
			}

			Click -= OnMenuClicked;

			var ret = toRemoveFromParent;
			toRemoveFromParent = new List<Control> ();
			return ret;
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
			if (commandArrayInfo != null) {
				manager.DispatchCommand (menuEntry.CommandId, commandArrayInfo.DataItem, initialCommandTarget, commandSource);
			} else {
				manager.DispatchCommand (menuEntry.CommandId, null, initialCommandTarget, commandSource);
			}
		}

		void OnMenuLinkClicked (object sender, System.Windows.RoutedEventArgs e)
		{
			DesktopService.ShowUrl (menuLinkEntry.Url);
		}

		readonly MonoDevelop.Components.Commands.CommandManager manager;
		readonly object initialCommandTarget;
		readonly CommandSource commandSource;
		readonly CommandInfo commandArrayInfo;
		readonly ActionCommand actionCommand;
		readonly CommandEntry menuEntry;
		readonly CommandEntrySet menuEntrySet;
		readonly LinkCommandEntry menuLinkEntry;
		List<Control> toRemoveFromParent = new List<Control> ();
	}
}
