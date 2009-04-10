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
		
		static IntPtr openingHandlerRef, updateHandlerRef, commandHandlerRef, closedHandlerRef;
		
		static CommandManager manager;
		
		static uint cmdSeq = 1; //reserve 0, since it gets used by submenus' parent items
		static ushort idSeq;
		
		static Dictionary<uint,object> commands = new Dictionary<uint,object> ();
		static Dictionary<uint,object> appMenuCommands = new Dictionary<uint,object> ();
		static Dictionary<IntPtr,string> menus = new Dictionary<IntPtr,string> ();
		
		static Gdk.Keymap keymap = Gdk.Keymap.Default;
		
		static string GetName (CommandEntrySet ces)
		{
			string text = ces.Name ?? "";
			return text.Replace ("_", "");
		}
		
		public static void Update (CommandManager manager, CommandEntrySet entrySet,
		                            Dictionary<object,CarbonCommandID> cmdIdMap, HashSet<object> ignoreCommands) 
		{
			if (manager == null)
				throw new ArgumentException ("manager");
			OSXMenu.manager = manager;
			
			if (rootMenu == IntPtr.Zero) {
				rootMenu = HIToolbox.CreateMenu (idSeq++, GetName (entrySet), 0);
				CreateChildren (rootMenu, entrySet, cmdIdMap, ignoreCommands);
				InstallRootMenu ();
			} else {
				Destroy (false);
				CreateChildren (rootMenu, entrySet, cmdIdMap, ignoreCommands);
			}
		}
		
		static void InstallRootMenu ()
		{
			HIToolbox.CheckResult (HIToolbox.SetRootMenu (rootMenu));
			
			openingHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuOpening, CarbonEventMenu.Opening);
			closedHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuClosed, CarbonEventMenu.Closed);
			commandHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuCommand, CarbonEventCommand.Process);
			updateHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuCommandUpdate, CarbonEventCommand.UpdateStatus);
		}

		static void CreateChildren (IntPtr parentMenu, CommandEntrySet entrySet, 
		                            Dictionary<object,CarbonCommandID> cmdIdMap, HashSet<object> ignoreCommands) 
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

					Command cmd = manager.GetCommand (entry.CommandId);
					if (cmd == null){
						Console.Error.WriteLine ("ERROR: Null command");
						continue;
					} else if (cmd is CustomCommand){
						Console.Error.WriteLine ("Warning: CustomCommands not supported on OSX");
					} else {
						ActionCommand acmd = cmd as ActionCommand;

						if (acmd == null){
							Console.Error.WriteLine ("Not sure how to handle {0}", cmd);
							continue;
						}
						
						uint macCmdId = GetNewMenuItemId (cmd, cmdIdMap);

						bool isArray = acmd.CommandArray;
						
						pos = HIToolbox.AppendMenuItem (parentMenu, (cmd.Text ?? "").Replace ("_", ""), 0, macCmdId);
						SetMenuAccelerator (new HIMenuItem (parentMenu, pos), cmd.AccelKey);
						
						// Deal with checked items here.
					}
				} else {
					IntPtr menuRef = HIToolbox.CreateMenu (idSeq++, GetName (ces), MenuAttributes.CondenseSeparators);
					menus [menuRef] = GetName (ces);
					CreateChildren (menuRef, ces, cmdIdMap, ignoreCommands);
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
					
					if ((mod & Gdk.ModifierType.Mod1Mask) != 0)
						outMod |= MenuAccelModifier.OptionModifier;
					if ((mod & Gdk.ModifierType.ShiftMask) != 0)
						outMod |= MenuAccelModifier.ShiftModifier;
					if ((mod & Gdk.ModifierType.ControlMask) != 0)
						outMod |= MenuAccelModifier.ControlModifier;
					
					// This is inverted, because by default on OSX no setting means use the Command-key
					if ((mod & Gdk.ModifierType.MetaMask) == 0)
						outMod |= MenuAccelModifier.None;
					
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
		
		static uint GetNewMenuItemId (Command cmd, Dictionary<object,CarbonCommandID> cmdIdMap)
		{
			uint macCmdId;
			CarbonCommandID standardId;
			if (cmdIdMap.TryGetValue (cmd.Id, out standardId))
				macCmdId = (uint) standardId;
			else
				macCmdId = cmdSeq++;
			
			commands[macCmdId] = cmd.Id;
			return macCmdId;
		}
		
		static void SetMenuItemAttributes (HIMenuItem item, CommandInfo ci)
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
					bool disabled = !ci.Enabled || !MonoDevelop.Ide.Gui.IdeApp.Workbench.RootWindow.HasToplevelFocus;
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
				}
				HIToolbox.SetMenuItemData (item.MenuRef, item.Index, false, data);
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
			appMenuCommands[(uint)CarbonCommandID.Quit] = cmdID;
		}
		
		public static void AddAppMenuItems (CommandManager manager, Dictionary<object,CarbonCommandID> cmdIdMap, params object [] cmdIds)
		{
			HIMenuItem mnu = HIToolbox.GetMenuItem ((uint)CarbonCommandID.Hide);
			for (int i = cmdIds.Length - 1; i >= 0; i--) {
				object cmdId = cmdIds[i];
				if (cmdId == Command.Separator) {
					HIToolbox.InsertMenuSeparator (mnu.MenuRef, 0);
					continue;
				}
				
				Command cmd = manager.GetCommand (cmdId);
				if (cmd == null){
					Console.Error.WriteLine ("ERROR: Null command");
					continue;
				}
				
				uint macCmdId = GetNewMenuItemId (cmd, cmdIdMap);
				System.Console.WriteLine(cmd.Text);
				ushort pos = HIToolbox.InsertMenuItem (mnu.MenuRef, (cmd.Text ?? "").Replace ("_", ""), 0, 0, macCmdId);
				SetMenuAccelerator (new HIMenuItem (mnu.MenuRef, pos), cmd.AccelKey);
			}
		}
		
		#endregion
		
		#region Event handlers
		
		static CarbonEventHandlerStatus HandleMenuOpening (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			try {
				IntPtr menuRef = Carbon.GetEventParameter (eventRef, CarbonEventParameterName.DirectObject, CarbonEventParameterType.MenuRef);
			//	uint cmd = HIToolbox.GetMenuItemCommandID (new HIMenuItem (menuRef, 0));
				string name;
				if (menus.TryGetValue (menuRef, out name))
					Console.WriteLine ("Menu opened: {0}", name);
				else
					System.Console.WriteLine ("Menu not found");
			} catch (Exception ex) {
				System.Console.WriteLine (ex);
			}
			
			return CarbonEventHandlerStatus.NotHandled;
		}
		
		static CarbonEventHandlerStatus HandleMenuClosed (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
		{
			try {
				IntPtr menuRef = Carbon.GetEventParameter (eventRef, CarbonEventParameterName.DirectObject, CarbonEventParameterType.MenuRef);
			//	uint cmd = HIToolbox.GetMenuItemCommandID (new HIMenuItem (menuRef, 0));
				string name;
				if (menus.TryGetValue (menuRef, out name))
					Console.WriteLine ("Menu closed: {0}", name);
				else
					System.Console.WriteLine ("Menu not found");
			} catch (Exception ex) {
				System.Console.WriteLine (ex);
			}
			
			return CarbonEventHandlerStatus.NotHandled;
		}
		
		static object GetCommandID (CarbonHICommand cmdEvent)
		{
			object cmdID;
			if (!commands.TryGetValue (cmdEvent.CommandID, out cmdID))
				appMenuCommands.TryGetValue (cmdEvent.CommandID, out cmdID);
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
				object cmdID = GetCommandID (GetCarbonHICommand (eventRef));
				if (cmdID != null) {
					//need to return before we execute the command, so that the menu unhighlights
					Gtk.Application.Invoke (delegate { manager.DispatchCommand (cmdID); });
					return CarbonEventHandlerStatus.Handled;
				}
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled error handling menu command", ex);
			}
			return CarbonEventHandlerStatus.NotHandled;
		}
		
		static CarbonEventHandlerStatus HandleMenuCommandUpdate (IntPtr callRef, IntPtr eventRef, IntPtr userData)
		{
			try {
				CarbonHICommand cmdEvent = GetCarbonHICommand (eventRef);
				object cmdID = GetCommandID (cmdEvent);
				if (cmdID != null) {
					CommandInfo cinfo = manager.GetCommandInfo (cmdID, null);
					SetMenuItemAttributes (cmdEvent.MenuItem, cinfo);
					return CarbonEventHandlerStatus.Handled;
				}
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled error handling menu command update", ex);
			}
			return CarbonEventHandlerStatus.NotHandled;
		}
		
		#endregion
		
		public static void Destroy (bool removeRoot)
		{
			if (commands.Count > 0 || menus.Count > 0) {
				foreach (IntPtr menu in menus.Keys)
					CoreFoundation.Release (menu);
				menus.Clear ();
				commands.Clear ();
				cmdSeq = 1;
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
				Carbon.RemoveEventHandler (updateHandlerRef);
				commandHandlerRef = openingHandlerRef = updateHandlerRef = rootMenu = IntPtr.Zero;
				cmdSeq = 1;
				idSeq = 0;
			}
		}
	}
	
}