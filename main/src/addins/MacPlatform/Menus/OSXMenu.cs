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
using System.Runtime.InteropServices;
using MonoDevelop.Components.Commands;
using OSXIntegration.Framework;

namespace OSXIntegration
{
	
	public static class OSXMenu
	{
		static IntPtr rootMenu;
		static IntPtr appMenu;
		
		static IntPtr openingHandlerRef, commandHandlerRef, closedHandlerRef;
		
		static CommandManager manager;
		
		//reserve 0, since it gets used by submenus' parent items
		const uint linkCommandId = 1;
		static uint cmdSeq = 2;
		
		static ushort idSeq;
		
		static HashSet<IntPtr> mainMenus = new HashSet<IntPtr> ();
		static Dictionary<uint,object> commands = new Dictionary<uint,object> ();
		static Dictionary<object,CarbonCommandID> cmdIdMap = new Dictionary<object, CarbonCommandID> ();
		
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
			OSXMenu.manager = manager;
			
			if (rootMenu == IntPtr.Zero) {
				rootMenu = HIToolbox.CreateMenu (idSeq++, GetName (entrySet), 0);
				CreateChildren (rootMenu, entrySet, ignoreCommands);
				InstallRootMenu ();
			} else {
				Destroy (false);
				CreateChildren (rootMenu, entrySet, ignoreCommands);
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
			foreach (CommandEntry entry in entrySet){
				CommandEntrySet ces = entry as CommandEntrySet;

				if (ces == null){
					ushort pos;
					
					if (ignoreCommands.Contains (entry.CommandId))
						continue;

					if (entry.CommandId == Command.Separator){
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
					
					ActionCommand acmd = cmd as ActionCommand;
					if (acmd == null) {
						MonoDevelop.Core.LoggingService.LogWarning (
							"Mac main menu does not support command type '{0}' for command '{1}'", cmd.GetType (), entry.CommandId);
						continue;
					}
					
					uint macCmdId = GetNewMenuItemId (cmd);
					bool isArray = acmd.CommandArray;
					
					pos = HIToolbox.AppendMenuItem (parentMenu, (cmd.Text ?? "").Replace ("_", ""), 0, macCmdId);
				} else {
					IntPtr menuRef = HIToolbox.CreateMenu (idSeq++, GetName (ces), MenuAttributes.CondenseSeparators);
					mainMenus.Add (menuRef);
					CreateChildren (menuRef, ces, ignoreCommands);
					ushort pos = HIToolbox.AppendMenuItem (parentMenu, GetName (ces), 0, 0);
					HIToolbox.CheckResult (HIToolbox.SetMenuItemHierarchicalMenu (parentMenu, pos, menuRef));
				}
			}
		}
		
		static void SetMenuAccelerator (HIMenuItem item, string accelKey)
		{
			ushort key;
			MenuAccelModifier mod;
			if (GetAcceleratorKeys (accelKey, out key, out mod)) {
				HIToolbox.SetMenuItemCommandKey (item.MenuRef, item.Index, true, key);
				HIToolbox.SetMenuItemModifiers (item.MenuRef, item.Index, mod);
			}
		}
		
		//FIXME: handle the mode key
		static bool GetAcceleratorKeys (string accelKey, out ushort outKey, out MenuAccelModifier outMod)
		{
			uint modeKey, key;
			Gdk.ModifierType modeMod, mod;
			
			if (KeyBindingManager.BindingToKeys (accelKey, out modeKey, out modeMod, out key, out mod)){
				Gdk.KeymapKey [] map = keymap.GetEntriesForKeyval (key);
				if (map != null && map.Length > 0){
					outKey = (ushort) map [0].Keycode;
					outMod = 0;
					
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
					
					if (modeKey != 0) {
						System.Console.WriteLine("WARNING: Cannot display accelerators with mode keys ({0})", accelKey);
						return false;
					}
					
					return true;
				}
			}
			outKey = 0;
			outMod = (MenuAccelModifier) 0;
			return false;
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
				return !MonoDevelop.Ide.Gui.IdeApp.Workbench.RootWindow.HasToplevelFocus;
			}
		}
		
		static void SetMenuItemAttributes (HIMenuItem item, CommandInfo ci, uint refcon)
		{
			MenuItemData data = new MenuItemData ();
			IntPtr text = IntPtr.Zero;
			try {
				if (!ci.Visible) {
					data.Attributes |= MenuItemAttributes.Hidden;
				} else {
					data.CFText = CoreFoundation.CreateString (GetCleanCommandText (ci));
					
					//disable also when MD main window doesn't have toplevel focus, or commands will be 
					//accessible when modal dialogs are active
					bool disabled = !ci.Enabled || IsGloballyDisabled;
					data.Enabled = !disabled;
					if (disabled)
						data.Attributes |= MenuItemAttributes.Disabled;
					
					ushort key;
					MenuAccelModifier mod;
					if (GetAcceleratorKeys (ci.AccelKey, out key, out mod)) {
						data.CommandKeyModifiers = mod;
						data.Attributes |= MenuItemAttributes.UseVirtualKey;
						data.CommandVirtualKey = key;
					}
					
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
		
		//FIXME: remove markup
		static string GetCleanCommandText (CommandInfo ci)
		{
			if (ci.Text == null)
				return "";
			
			return ci.Text.Replace ("_", "");
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
			for (int i = cmdIds.Length - 1; i >= 0; i--) {
				object cmdId = cmdIds[i];
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
			}
		}
		
		#endregion
		
		#region Event handlers
		
		//updates commands and populates arrays and dynamic menus
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
					
					//items that map to command objects
					if (!commands.TryGetValue (macCmdID, out cmdID) || cmdID == null)
						continue;
					
					CommandInfo cinfo = manager.GetCommandInfo (cmdID, null);
					UpdateMenuItem (menuRef, menuRef, ref i, ref count, macCmdID, cinfo);
				}
			} catch (Exception ex) {
				System.Console.WriteLine (ex);
			}
			
			return CarbonEventHandlerStatus.NotHandled;
		}
		
		static void BuildDynamicSubMenu (IntPtr rootMenu, IntPtr parentMenu, ushort index, uint macCmdID, CommandInfoSet cinfoSet)
		{
			IntPtr menuRef = HIToolbox.CreateMenu (idSeq++, GetCleanCommandText (cinfoSet), MenuAttributes.CondenseSeparators);
			objectsToDestroyOnMenuClose.Add (new DestructableMenu (menuRef));
			HIToolbox.CheckResult (HIToolbox.SetMenuItemHierarchicalMenu (parentMenu, index, menuRef));
			
			ushort count = (ushort) cinfoSet.CommandInfos.Count;
			for (ushort i = 1, j = 0; i <= count; i++) {
				CommandInfo ci = cinfoSet.CommandInfos[j++];
				HIToolbox.AppendMenuItem (menuRef, ci.Text, 0, macCmdID);
				UpdateMenuItem (rootMenu, menuRef, ref i, ref count, macCmdID, ci);
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
						System.Diagnostics.Process.Start (url);
					} catch (Exception ex) {
						Gtk.Application.Invoke (delegate {
							MonoDevelop.Core.Gui.MessageService.ShowException (ex, MonoDevelop.Core.GettextCatalog.GetString ("Could not open the url {0}", url));
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
						Gtk.Application.Invoke (delegate { manager.DispatchCommand (cmdID, data); });
					} else {
						Gtk.Application.Invoke (delegate { manager.DispatchCommand (cmdID); });
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