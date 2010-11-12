//
// OSXMenu.cs
//
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//       Miguel de Icaza
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using OSXIntegration.Framework;
using System.Text;
using MonoDevelop.Ide;
using MonoDevelop.Platform;

namespace OSXIntegration
{
	
	public static class OSXMenu
	{
		static IntPtr rootMenu;
		static IntPtr appMenu;
		
		static IntPtr openingHandlerRef, commandHandlerRef, closedHandlerRef;
		
		static CommandManager manager;
		
		const uint submenuCommandId = 0;
		const uint linkCommandId = 1;
		const uint autohideSubmenuCommandId = 2;
		static uint cmdSeq = 3;
		
		static ushort idSeq;
		
		static HashSet<IntPtr> mainMenus = new HashSet<IntPtr> ();
		static Dictionary<uint,object> commands = new Dictionary<uint,object> ();
		static Dictionary<object,CarbonCommandID> cmdIdMap = new Dictionary<object, CarbonCommandID> ();
		static Dictionary<object,uint> menuIdMap = new Dictionary<object, uint> ();
		
		static List<object> objectsToDestroyOnMenuClose = new List<object> ();
		static List<string> linkCommands = new List<string> ();
		static int menuOpenDepth = 0;
		
		static Gdk.Keymap keymap = Gdk.Keymap.Default;
		
		private class DestructableMenu
		{
			public DestructableMenu (IntPtr ptr)
			{
				Ref = ptr;
			}
			
			public void Destroy ()
			{
				if (Ref != IntPtr.Zero) {
					HIToolbox.DeleteMenu (Ref);
					CoreFoundation.Release (Ref);
					Ref = IntPtr.Zero;
				}
			}
			IntPtr Ref;
		}
		
		static string GetName (CommandEntrySet ces)
		{
			string text = ces.Name ?? "";
			return text.Replace ("_", "");
		}
		
		static public void AddCommandIDMappings (Dictionary<object,CarbonCommandID> map)
		{
			if (cmdIdMap.Count > 0)
				throw new InvalidOperationException ("This can only be done once, before creating menus");
			cmdIdMap = new Dictionary<object, CarbonCommandID> (map);
		}
		
		public static void Recreate (CommandManager manager, CommandEntrySet entrySet, HashSet<object> ignoreCommands) 
		{
			if (manager == null)
				throw new ArgumentException ("manager");
			if (OSXMenu.manager != null) {
				OSXMenu.manager.CommandActivating -= OnCommandActivating;
			}
			OSXMenu.manager = manager;
			OSXMenu.manager.CommandActivating += OnCommandActivating;
			
			if (rootMenu == IntPtr.Zero) {
				rootMenu = HIToolbox.CreateMenu (idSeq++, GetName (entrySet), 0);
				CreateChildren (rootMenu, entrySet, ignoreCommands);
				InstallRootMenu ();
			} else {
				Destroy (false);
				CreateChildren (rootMenu, entrySet, ignoreCommands);
			}
		}

		static void OnCommandActivating (object sender, CommandActivationEventArgs args)
		{
			uint menuId;
			if (args.Source == CommandSource.Keybinding && menuIdMap.TryGetValue (args.CommandId, out menuId)) {
				//FIXME: for some reason we have to flash again after a delay to toggle the previous flash off?
				//some flashes can be unreliable, e.g. minimize, and modal dialogs don't seem to run timeouts, so the flash comes late
				GLib.Timeout.Add (50, delegate {
					HIToolbox.FlashMenuBar (menuId);
					return false;
				});
				GLib.Timeout.Add (250, delegate {
					HIToolbox.FlashMenuBar (menuId);
					return false;
				});
			}
		}
		
		static void InstallRootMenu ()
		{
			HIToolbox.CheckResult (HIToolbox.SetRootMenu (rootMenu));
			
			openingHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuOpening, CarbonEventMenu.Opening);
			closedHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuClosed, CarbonEventMenu.Closed);
			commandHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuCommand, CarbonEventCommand.Process);
		}

		static void CreateChildren (IntPtr parentMenu, CommandEntrySet entrySet, HashSet<object> ignoreCommands) 
		{
			var menuId = HIToolbox.GetMenuID (parentMenu);
			foreach (CommandEntry entry in entrySet){
				CommandEntrySet ces = entry as CommandEntrySet;

				if (ces == null){
					ushort pos;
					
					if (ignoreCommands.Contains (entry.CommandId))
						continue;

					if (entry.CommandId == Command.Separator) {
						HIToolbox.AppendMenuSeparator (parentMenu);
						continue;
					}
					
					if (entry is LinkCommandEntry) {
						LinkCommandEntry lce = (LinkCommandEntry)entry;
						pos = HIToolbox.AppendMenuItem (parentMenu, (lce.Text ?? "").Replace ("_", ""), 0, linkCommandId);
						HIToolbox.SetMenuItemReferenceConstant (new HIMenuItem (parentMenu, pos), (uint)linkCommands.Count);
						linkCommands.Add (lce.Url);
						continue;
					}

					Command cmd = manager.GetCommand (entry.CommandId);
					if (cmd == null) {
						MonoDevelop.Core.LoggingService.LogError (
							"Mac main menu '{0}' child '{1}' maps to null command", entrySet.Name, entry.CommandId);
						continue;
					}

					if (cmd is CustomCommand) {
						MonoDevelop.Core.LoggingService.LogWarning (
							"Mac main menu does not support custom command widgets for command '{0}'", entry.CommandId);
						continue;
					}
					
					menuIdMap[entry.CommandId] = menuId;
					
					ActionCommand acmd = cmd as ActionCommand;
					if (acmd == null) {
						MonoDevelop.Core.LoggingService.LogWarning (
							"Mac main menu does not support command type '{0}' for command '{1}'", cmd.GetType (), entry.CommandId);
						continue;
					}
					
					uint macCmdId = GetNewMenuItemId (cmd);
					
					pos = HIToolbox.AppendMenuItem (parentMenu, (cmd.Text ?? "").Replace ("_", ""), 0, macCmdId);
				} else {
					var macCmdId = (ces.AutoHide) ? autohideSubmenuCommandId : submenuCommandId;
					IntPtr menuRef = HIToolbox.CreateMenu (idSeq++, GetName (ces), MenuAttributes.CondenseSeparators);
					mainMenus.Add (menuRef);
					CreateChildren (menuRef, ces, ignoreCommands);
					ushort pos = HIToolbox.AppendMenuItem (parentMenu, GetName (ces), 0, macCmdId);
					HIToolbox.CheckResult (HIToolbox.SetMenuItemHierarchicalMenu (parentMenu, pos, menuRef));
				}
			}
		}
		
		static void SetMenuAccelerator (HIMenuItem item, string accelKey)
		{
			MenuAccelModifier mod;
			ushort glyphCode, charCode, hardwareCode; 
			if (GetAcceleratorKeys (accelKey, out glyphCode, out charCode, out hardwareCode, out mod)) {
				if (glyphCode != 0)
					HIToolbox.SetMenuItemKeyGlyph (item.MenuRef, item.Index, (short)glyphCode);
				else if (hardwareCode != 0)
					HIToolbox.SetMenuItemCommandKey (item.MenuRef, item.Index, true, hardwareCode);
				else
					HIToolbox.SetMenuItemCommandKey (item.MenuRef, item.Index, false, charCode);
				HIToolbox.SetMenuItemModifiers (item.MenuRef, item.Index, mod);
			}
		}
		
		//FIXME: handle the mode key
		static bool GetAcceleratorKeys (string accelKey, out ushort glyphCode, out ushort charCode, out ushort virtualHardwareCode,
		                                out MenuAccelModifier outMod)
		{
			uint modeKey, key;
			Gdk.ModifierType modeMod, mod;
			glyphCode = charCode = virtualHardwareCode = 0;
			outMod = (MenuAccelModifier) 0;
			
			if (!KeyBindingManager.BindingToKeys (accelKey, out modeKey, out modeMod, out key, out mod))
				return false;
			
			if (modeKey != 0) {
				System.Console.WriteLine("WARNING: Cannot display accelerators with mode keys ({0})", accelKey);
				return false;
			}
			
			glyphCode = (ushort)GlyphMappings ((Gdk.Key)key);
			if (glyphCode == 0) {
				charCode = (ushort)Gdk.Keyval.ToUnicode (key);
				if (charCode == 0) {
					var map = keymap.GetEntriesForKeyval (key);
					if (map != null && map.Length > 0)
						virtualHardwareCode = (ushort) map [0].Keycode;
					
					if (virtualHardwareCode == 0) {
						System.Console.WriteLine("WARNING: Could not map key ({0})", key);
						return false;
					}
				}
			}
			
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0) {
				outMod |= MenuAccelModifier.OptionModifier;
				mod ^= Gdk.ModifierType.Mod1Mask;
			}
			if ((mod & Gdk.ModifierType.ShiftMask) != 0) {
				outMod |= MenuAccelModifier.ShiftModifier;
				mod ^= Gdk.ModifierType.ShiftMask;
			}
			if ((mod & Gdk.ModifierType.ControlMask) != 0) {
				outMod |= MenuAccelModifier.ControlModifier;
				mod ^= Gdk.ModifierType.ControlMask;
			}
			
			// This is inverted, because by default on OSX no setting means use the Command-key
			if ((mod & Gdk.ModifierType.MetaMask) == 0) {
				outMod |= MenuAccelModifier.None;
			} else {
				mod ^= Gdk.ModifierType.MetaMask;
			}
			
			if (mod != 0) {
				System.Console.WriteLine("WARNING: Cannot display accelerators with modifiers: {0}", mod);
				return false;
			}
			
			return true;
		}
		
		static MenuGlyphs GlyphMappings (Gdk.Key key)
		{
			switch (key) {
			case Gdk.Key.Page_Up:
				return MenuGlyphs.PageUp;
			case Gdk.Key.Page_Down:
				return MenuGlyphs.PageDown;
			case Gdk.Key.Up:
				return MenuGlyphs.UpArrow;
			case Gdk.Key.Down:
				return MenuGlyphs.DownArrow;
			case Gdk.Key.Left:
				return MenuGlyphs.LeftArrow;
			case Gdk.Key.Right:
				return MenuGlyphs.RightArrow;
			case Gdk.Key.space:
				return MenuGlyphs.Space;
			case Gdk.Key.Escape:
				return MenuGlyphs.Escape;
			case Gdk.Key.Return:
				return MenuGlyphs.Return;
			default:
				return MenuGlyphs.None;
			}
		}
		
		static uint GetNewMenuItemId (Command cmd)
		{
			uint macCmdId;
			CarbonCommandID standardId;
			//use mapped values if possible
			if (cmdIdMap.TryGetValue (cmd.Id, out standardId))
				macCmdId = (uint) standardId;
			//or generate a new value
			else {
				//but avoid conflicts
				do cmdSeq++;
				while (commands.ContainsKey (cmdSeq));
				macCmdId = cmdSeq;
				cmdIdMap[cmd.Id] = (CarbonCommandID)macCmdId;
			}
			
			commands[macCmdId] = cmd.Id;
			return macCmdId;
		}
		
		//NOTE: This is used to disable the whole menu when there's a modal dialog.
		// We can justify this because safari 3.2.1 does it ("do you want to close all tabs?").
		static bool IsGloballyDisabled {
			get {
				return !IdeApp.Workbench.HasToplevelFocus;
			}
		}
		
		static void SetMenuItemAttributes (HIMenuItem item, CommandInfo ci, uint refcon)
		{
			MenuItemData data = new MenuItemData ();
			IntPtr text = IntPtr.Zero;
			try {
				if (ci.IsArraySeparator) {
					data.Attributes |= MenuItemAttributes.Separator;
				} else if (!ci.Visible) {
					data.Attributes |= MenuItemAttributes.Hidden;
				} else {
					data.Attributes &= ~MenuItemAttributes.Hidden;
					data.CFText = CoreFoundation.CreateString (GetCleanCommandText (ci));
					
					//disable also when MD main window doesn't have toplevel focus, or commands will be 
					//accessible when modal dialogs are active
					bool disabled = !ci.Enabled || IsGloballyDisabled;
					data.Enabled = !disabled;
					if (disabled)
						data.Attributes |= MenuItemAttributes.Disabled;
					
					ushort glyphCode, charCode, hardwareCode; 
					MenuAccelModifier mod;
					if (GetAcceleratorKeys (ci.AccelKey, out glyphCode, out charCode, out hardwareCode, out mod)) {
						data.CommandKeyModifiers = mod;
						if (glyphCode != 0) {
							data.CommandKeyGlyph = glyphCode;
							data.Attributes ^= MenuItemAttributes.UseVirtualKey;
						} else if (hardwareCode != 0) {
							data.CommandVirtualKey = (char)hardwareCode;
							data.Attributes |= MenuItemAttributes.UseVirtualKey;
						} else {
							data.CommandKey = (char)charCode;
							data.Attributes ^= MenuItemAttributes.UseVirtualKey;
						}
					}
					//else{
				 	//FIXME: remove existing commands if necessary
					
					data.Mark = ci.Checked
						? ci.CheckedInconsistent
							? '-' //FIXME: is this a good symbol for CheckedInconsistent?
							: (char)MenuGlyphs.Checkmark
						: '\0';
					
					data.ReferenceConstant = refcon;
				}
				HIToolbox.SetMenuItemData (item.MenuRef, item.Index, false, ref data);
			} finally {
				if (text != IntPtr.Zero)
					CoreFoundation.Release (text);
			}
		}
		
		static string GetCleanCommandText (CommandInfo ci)
		{
			string txt = ci.Text;
			if (txt == null)
				return "";
			
			if (!ci.UseMarkup)
				return txt.Replace ("_", "");
			
			//strip GMarkup
			//FIXME: markup stripping could be done better
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < txt.Length; i++) {
				char ch = txt[i];
				if (ch == '<') {
					while (++i < txt.Length && txt[i] != '>');
				} else if (ch == '&') {
					int j = i;
					while (++i < txt.Length && txt[i] != ';');
					int len = i - j - 1;
					if (len > 0) {
						string entityName = txt.Substring (j + 1, i - j - 1);
						switch (entityName) {
						case "quot":
							sb.Append ('"');
							break;
						case "amp":
							sb.Append ('&');
							break;
						case "apos":
							sb.Append ('\'');
							break;
						case "lt":
							sb.Append ('<');
							break;
						case "gt":
							sb.Append ('>');
							break;
						default:
							MonoDevelop.Core.LoggingService.LogWarning ("Could not de-markup entity '{0}'", entityName);
							break;
						}
					}
				} else if (ch != '_') {
					sb.Append (ch);
				}
			}
			
			return sb.ToString ();
		}
		
		#region App menu
		
		public static void SetAppQuitCommand (object cmdID)
		{
			commands[(uint)CarbonCommandID.Quit] = cmdID;
		}
		
		public static void AddAppMenuItems (CommandManager manager, params object [] cmdIds)
		{
			//FIXME: we assume we get first pick of cmdIDs
			
			HIMenuItem mnu = HIToolbox.GetMenuItem ((uint)CarbonCommandID.Hide);
			appMenu = mnu.MenuRef;
			var appMenuId = HIToolbox.GetMenuID (appMenu);
			for (int i = cmdIds.Length - 1; i >= 0; i--) {
				var cmdId = cmdIds[i];
				if (cmdId == Command.Separator) {
					HIToolbox.InsertMenuSeparator (mnu.MenuRef, 0);
					continue;
				}
				
				Command cmd = manager.GetCommand (cmdId);
				if (cmd == null){
					MonoDevelop.Core.LoggingService.LogError ("Null command in Mac app menu for ID {0}", cmdId);
					continue;
				}
				
				uint macCmdId = GetNewMenuItemId (cmd);
				ushort pos = HIToolbox.InsertMenuItem (mnu.MenuRef, (cmd.Text ?? "").Replace ("_", ""), 0, 0, macCmdId);
				SetMenuAccelerator (new HIMenuItem (mnu.MenuRef, pos), cmd.AccelKey);
				menuIdMap[cmdId] = appMenuId;
			}
		}
		
		#endregion
		
		#region Event handlers
		
		//updates commands and populates arrays and dynamic menus
		//NOTE: when Help menu is opened, Mac OS calls this for ALL menus because the Help menu can search menu items
		static CarbonEventHandlerStatus HandleMenuOpening (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			DestroyOldMenuObjects ();
			menuOpenDepth++;
			try {
				IntPtr menuRef = Carbon.GetEventParameter (eventRef, CarbonEventParameterName.DirectObject, CarbonEventParameterType.MenuRef);
				
				//don't update dynamic menus recursively
				if (!mainMenus.Contains (menuRef) && menuRef != appMenu)
					return CarbonEventHandlerStatus.NotHandled;
				
			//	uint cmd = HIToolbox.GetMenuItemCommandID (new HIMenuItem (menuRef, 0));
				
				CommandTargetRoute route = new CommandTargetRoute ();
				ushort count = HIToolbox.CountMenuItems (menuRef);
				for (ushort i = 1; i <= count; i++) {
					HIMenuItem mi = new HIMenuItem (menuRef, i);
					uint macCmdID = HIToolbox.GetMenuItemCommandID (mi);
					object cmdID;
					
					//link items
					if (macCmdID == linkCommandId) {
						if (IsGloballyDisabled)
							HIToolbox.DisableMenuItem (mi);
						else
							HIToolbox.EnableMenuItem (mi);
						continue;
					}
					
					if (macCmdID == submenuCommandId)
						continue;
					
					if (macCmdID == autohideSubmenuCommandId) {
						UpdateAutoHide (new HIMenuItem (menuRef, i));
						continue;
					}
					
					//items that map to command objects
					if (!commands.TryGetValue (macCmdID, out cmdID) || cmdID == null)
						continue;
					
					CommandInfo cinfo = manager.GetCommandInfo (cmdID, route);
					menuIdMap[cmdID] = HIToolbox.GetMenuID (menuRef);
					UpdateMenuItem (menuRef, menuRef, ref i, ref count, macCmdID, cinfo);
				}
			} catch (Exception ex) {
				System.Console.WriteLine (ex);
			}
			
			return CarbonEventHandlerStatus.NotHandled;
		}
		
		static void UpdateAutoHide (HIMenuItem item)
		{
			IntPtr submenu;
			if (HIToolbox.GetMenuItemHierarchicalMenu (item.MenuRef, item.Index, out submenu) != CarbonMenuStatus.Ok)
				return;
			
			if (HasVisibleItems (submenu)) {
				HIToolbox.ChangeMenuItemAttributes (item, 0, MenuItemAttributes.Hidden);
			} else {
				HIToolbox.ChangeMenuItemAttributes (item, MenuItemAttributes.Hidden, 0);
			}
		}
		
		static bool HasVisibleItems (IntPtr submenu)
		{
			var route = new CommandTargetRoute ();
			ushort count = HIToolbox.CountMenuItems (submenu);
			
			for (ushort i = 1; i <= count; i++) {
				HIMenuItem mi = new HIMenuItem (submenu, i);
				uint macCmdID = HIToolbox.GetMenuItemCommandID (mi);
				object cmdID;
				
				if (macCmdID == linkCommandId)
					return true;
				
				if (!commands.TryGetValue (macCmdID, out cmdID) || cmdID == null)
					continue;
				
				CommandInfo cinfo = manager.GetCommandInfo (cmdID, route);
				if (cinfo.ArrayInfo != null) {
					foreach (CommandInfo ci in cinfo.ArrayInfo)
						if (ci.Visible)
							return true;
				} else if (cinfo.Visible) {
					return true;
				}
			}
			
			return false;
		}
		
		static void BuildDynamicSubMenu (IntPtr rootMenu, IntPtr parentMenu, ushort index, uint macCmdID, CommandInfoSet cinfoSet)
		{
			IntPtr menuRef = HIToolbox.CreateMenu (idSeq++, GetCleanCommandText (cinfoSet), MenuAttributes.CondenseSeparators);
			objectsToDestroyOnMenuClose.Add (new DestructableMenu (menuRef));
			HIToolbox.CheckResult (HIToolbox.SetMenuItemHierarchicalMenu (parentMenu, index, menuRef));
			
			ushort count = (ushort) cinfoSet.CommandInfos.Count;
			for (ushort i = 1, j = 0; i <= count; i++) {
				CommandInfo ci = cinfoSet.CommandInfos[j++];
				if (ci.IsArraySeparator) {
					HIToolbox.AppendMenuSeparator (menuRef);
				} else {
					HIToolbox.AppendMenuItem (menuRef, ci.Text, 0, macCmdID);
					UpdateMenuItem (rootMenu, menuRef, ref i, ref count, macCmdID, ci);
					
					objectsToDestroyOnMenuClose.Add (ci.DataItem);
					uint refcon = (uint)objectsToDestroyOnMenuClose.Count;
					HIToolbox.SetMenuItemReferenceConstant (new HIMenuItem (menuRef, i), refcon);
				}
			}
		}
		
		static void UpdateMenuItem (IntPtr rootMenu, IntPtr menuRef, ref ushort index, ref ushort count,
		                            uint macCmdID, CommandInfo cinfo)
		{
			if (cinfo.ArrayInfo != null) {
				//remove the existing items, except one, which we hide, so it gets updated next time even if the list is empty
				HIToolbox.ChangeMenuItemAttributes (new HIMenuItem (menuRef, index), MenuItemAttributes.Hidden, 0);
				index++;
				while (index <= count && HIToolbox.GetMenuItemCommandID (new HIMenuItem (menuRef, index)) == macCmdID) {
					HIToolbox.DeleteMenuItem (menuRef, index);
					count--;
				}
				index--;
				
				//add the new items
				foreach (CommandInfo ci in cinfo.ArrayInfo) {
					count++;
					HIToolbox.InsertMenuItem (menuRef, ci.Text, index++, 0, macCmdID);
					
					//associate a reference constant with the menu, used to index the DataItem
					//it's one-based, so that 0 can be used as a flag that there's no associated object
					objectsToDestroyOnMenuClose.Add (ci.DataItem);
					uint refcon = (uint)objectsToDestroyOnMenuClose.Count;
					
					SetMenuItemAttributes (new HIMenuItem (menuRef, index), ci, refcon);
					if (ci is CommandInfoSet)
						BuildDynamicSubMenu (rootMenu, menuRef, index, macCmdID, (CommandInfoSet)ci);
				}
			} else {
				SetMenuItemAttributes (new HIMenuItem (menuRef, index), cinfo, 0);
				if (cinfo is CommandInfoSet)
					BuildDynamicSubMenu (rootMenu, menuRef, index, macCmdID, (CommandInfoSet)cinfo);
			}
		}
		
		//this releases resources and gc-prevention handles from dynamic menu creation
		static CarbonEventHandlerStatus HandleMenuClosed (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			menuOpenDepth--;
			//we can't destroy the menu objects instantly, since the command handler is invoked after the close event
			//FIXME: maybe we can get the close reason, and handle the command here?
			return CarbonEventHandlerStatus.NotHandled;
		}
		
		static void DestroyOldMenuObjects ()
		{
			if (menuOpenDepth > 0)
				return;
			foreach (object o in objectsToDestroyOnMenuClose) {
				if (o is DestructableMenu)
					try {
						((DestructableMenu)o).Destroy ();
					} catch (Exception ex) {
						MonoDevelop.Core.LoggingService.LogError ("Unhandled exception while destroying old menu objects", ex);
					}
			}
			objectsToDestroyOnMenuClose.Clear ();
		}
		
		static object GetCommandID (CarbonHICommand cmdEvent)
		{
			object cmdID;
			commands.TryGetValue (cmdEvent.CommandID, out cmdID);
			return cmdID;
		}
		
		static CarbonHICommand GetCarbonHICommand (IntPtr eventRef)
		{
			return Carbon.GetEventParameter<CarbonHICommand> (eventRef, CarbonEventParameterName.DirectObject,
			                                                  CarbonEventParameterType.HICommand);
		}
		
		static CarbonEventHandlerStatus HandleMenuCommand (IntPtr callRef, IntPtr eventRef, IntPtr userData)
		{
			try {
				CarbonHICommand hiCmd = GetCarbonHICommand (eventRef);
				uint refCon = HIToolbox.GetMenuItemReferenceConstant (hiCmd.MenuItem);
				
				//link commands
				if (hiCmd.CommandID == linkCommandId) {
					string url = "";
					try {
						url = linkCommands[(int)refCon];
						MacPlatform.OpenUrl (url);
					} catch (Exception ex) {
						Gtk.Application.Invoke (delegate {
							MonoDevelop.Ide.MessageService.ShowException (ex, MonoDevelop.Core.GettextCatalog.GetString ("Could not open the url {0}", url));
						});
					}
					DestroyOldMenuObjects ();
					return CarbonEventHandlerStatus.Handled;
				}
				
				//normal commands
				object cmdID = GetCommandID (hiCmd);
				if (cmdID != null) {
					if (refCon > 0) {
						object data = objectsToDestroyOnMenuClose[(int)refCon - 1];
						//need to return before we execute the command, so that the menu unhighlights
						Gtk.Application.Invoke (delegate { manager.DispatchCommand (cmdID, data, CommandSource.MainMenu); });
					} else {
						Gtk.Application.Invoke (delegate { manager.DispatchCommand (cmdID, CommandSource.MainMenu); });
					}
					DestroyOldMenuObjects ();
					return CarbonEventHandlerStatus.Handled;
				}
				
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled error handling menu command", ex);
			}
			return CarbonEventHandlerStatus.NotHandled;
		}
		
		#endregion
		
		public static void Destroy (bool removeRoot)
		{
			if (mainMenus.Count > 0) {
				foreach (IntPtr ptr in mainMenus) {
					HIToolbox.DeleteMenu (ptr);
					CoreFoundation.Release (ptr);
				}
				DestroyOldMenuObjects ();
				mainMenus.Clear ();
				linkCommands.Clear ();
				idSeq = 1;
			}
			
			HIToolbox.ClearMenuBar ();
			
			if (removeRoot && rootMenu != IntPtr.Zero) {
				HIToolbox.DeleteMenu (rootMenu);
				CoreFoundation.Release (rootMenu);
				HIToolbox.CheckResult (HIToolbox.SetRootMenu (IntPtr.Zero));
				Carbon.RemoveEventHandler (commandHandlerRef);
				Carbon.RemoveEventHandler (openingHandlerRef);
				Carbon.RemoveEventHandler (closedHandlerRef);
				commandHandlerRef = openingHandlerRef = rootMenu = IntPtr.Zero;
				idSeq = 0;
			}
		}
	}
	
}
