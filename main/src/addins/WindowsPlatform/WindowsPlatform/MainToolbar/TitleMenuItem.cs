using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Windows;

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
							Items.Add (new Separator { UseLayoutRounding = true, });
						else
							Items.Add (new TitleMenuItem (manager, entry, item, commandSource, initialCommandTarget));
					}
				}
			}

			if (menuEntrySet != null) {
				Header = menuEntrySet.Name;

				foreach (CommandEntry item in menuEntrySet) {
					if (item.CommandId == MonoDevelop.Components.Commands.Command.Separator) {
						Items.Add (new Separator { UseLayoutRounding = true, });
					} else
						Items.Add (new TitleMenuItem (manager, item));
				}
			} else if (menuLinkEntry != null) {
				Header = menuLinkEntry.Text;
				Click += OnMenuLinkClicked;
			} else if (entry != null) {
				actionCommand = manager.GetCommand (menuEntry.CommandId) as ActionCommand;
				if (actionCommand == null)
					return;

				IsCheckable = actionCommand.ActionType == ActionType.Check;

				// FIXME: Use proper keybinding text.
				if (actionCommand.KeyBinding != null)
					InputGestureText = actionCommand.KeyBinding.ToString ();
				
				if (!actionCommand.Icon.IsNull)
					Icon = new Image { Source = actionCommand.Icon.GetImageSource (Xwt.IconSize.Small) };
				Click += OnMenuClicked;
			}

			Height = SystemParameters.CaptionHeight;
			UseLayoutRounding = true;
		}

		/// <summary>
		/// Updates a command entry. Should only be called from a toplevel node.
		/// This will update all the menu's children.
		/// </summary>
		void Update ()
		{
			hasCommand = false;
			if (menuLinkEntry != null)
				return;

			if (menuEntrySet != null || commandArrayInfo is CommandInfoSet) {
				for (int i = 0; i < Items.Count; ++i) {
					var titleMenuItem = Items[i] as TitleMenuItem;

					if (titleMenuItem != null) {
						titleMenuItem.Update ();
						continue;
					}

					// If we have a separator, don't draw another one if the previous visible item is a separator.
					var separatorMenuItem = Items [i] as Separator;
					separatorMenuItem.Visibility = Visibility.Collapsed;
					for (int j = i - 1; j >= 0; --j) {
						var iterMenuItem = Items [j] as Control;

						if (iterMenuItem is Separator)
							break;

						if (iterMenuItem.Visibility != Visibility.Visible)
							continue;

						separatorMenuItem.Visibility = Visibility.Visible;
						break;
					}
					if (menuEntrySet != null && menuEntrySet.AutoHide)
						Visibility = Items.Cast<Control> ().Any (item => item.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
				}
				return;
			}

			var info = manager.GetCommandInfo (menuEntry.CommandId, new CommandTargetRoute (initialCommandTarget));
			if (actionCommand != null) {
				if (!string.IsNullOrEmpty (info.Description) && (string)ToolTip != info.Description)
					ToolTip = info.Description;

				if (actionCommand.CommandArray && commandArrayInfo == null) {
					Visibility = Visibility.Collapsed;

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
		}

		bool hasCommand = false;
		void SetInfo (CommandInfo info)
		{
			hasCommand = true;
			Header = info.Text;
			Icon = new Image { Source = info.Icon.GetImageSource (Xwt.IconSize.Small) };
			IsEnabled = info.Enabled;
			Visibility = info.Visible && (menuEntry.DisabledVisible || IsEnabled) ?
				Visibility.Visible : Visibility.Collapsed;
			IsChecked = info.Checked || info.CheckedInconsistent;
			ToolTip = info.Description;
		}

		/// <summary>
		/// Clears a command entry's saved data. Should only be called from a toplevel node.
		/// This will update all the menu's children.
		/// </summary>
		IEnumerable<Control> Clear ()
		{
			if (menuLinkEntry != null) {
				return Enumerable.Empty<TitleMenuItem> ();
			}

			if (menuEntrySet != null) {
				var toRemove = Enumerable.Empty<Control> ();
                foreach (var item in Items) {
					var titleMenuItem = item as TitleMenuItem;
					if (titleMenuItem == null)
						continue;

					toRemove = toRemove.Concat (titleMenuItem.Clear ());
				}

				foreach (var item in toRemove)
					Items.Remove (item);

				return Enumerable.Empty<TitleMenuItem> (); 
			}
			
			var ret = toRemoveFromParent;
			toRemoveFromParent = new List<Control> ();
			return ret;
		}

		protected override void OnSubmenuOpened (RoutedEventArgs e)
		{
			if (Parent is Menu)
				Update ();

			base.OnSubmenuOpened (e);
		}

		protected override void OnSubmenuClosed (RoutedEventArgs e)
		{
			if (Parent is Menu)
				Clear ();

			base.OnSubmenuClosed (e);
		}

		void OnMenuClicked (object sender, RoutedEventArgs e)
		{
			if (!hasCommand)
				return;

			if (commandArrayInfo != null) {
				manager.DispatchCommand (menuEntry.CommandId, commandArrayInfo.DataItem, initialCommandTarget, commandSource);
			} else {
				manager.DispatchCommand (menuEntry.CommandId, null, initialCommandTarget, commandSource);
			}
		}

		void OnMenuLinkClicked (object sender, RoutedEventArgs e)
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
