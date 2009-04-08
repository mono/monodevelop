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
		
		static IntPtr openingHandlerRef, updateHandlerRef, commandHandlerRef;
		
		static CommandManager manager;
		static object initialCommandTarget;
		
		static uint cmdSeq;
		static ushort idSeq;
		
		static Dictionary<uint,object> commands = new Dictionary<uint,object> ();
		static Dictionary<IntPtr,string> menus = new Dictionary<IntPtr,string> ();
		
		static string GetName (CommandEntrySet ces)
		{
			string text = ces.Name ?? "";
			return text.Replace ("_", "");
		}
		
		public static void Update (CommandManager manager, object initialCommandTarget, CommandEntrySet entrySet)
		{
			if (manager == null)
				throw new ArgumentException ("manager");
		//	if (initialCommandTarget == null)
		//		throw new ArgumentException ("initialCommandTarget");
				
			OSXMenu.manager = manager;
			OSXMenu.initialCommandTarget = initialCommandTarget;
			
			if (rootMenu == IntPtr.Zero) {
				rootMenu = HIToolbox.CreateMenu (idSeq++, GetName (entrySet), 0);
				CreateChildren (rootMenu, entrySet);
				InstallRootMenu ();
			} else {
				Destroy (false);
				CreateChildren (rootMenu, entrySet);
			}
		}

		static void CreateChildren (IntPtr parentMenu, CommandEntrySet entrySet) 
		{
			Gdk.Keymap keymap = Gdk.Keymap.Default;
			foreach (CommandEntry entry in entrySet){
				CommandEntrySet ces = entry as CommandEntrySet;

				if (ces == null){
					ushort pos;

					object cmdId = entry.CommandId;

					if (cmdId == Command.Separator){
						HIToolbox.AppendMenuSeparator (parentMenu);
						continue;
					}

					Command cmd = manager.GetCommand (cmdId);
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
						
						commands[cmdSeq] = cmd.Id;

						bool isArray = acmd.CommandArray;
						
						pos = HIToolbox.AppendMenuItem (parentMenu, (cmd.Text ?? "").Replace ("_", ""), 0, cmdSeq++);
						
						uint modeKey, key;
						Gdk.ModifierType modeMod, mod;
						
						if (KeyBindingManager.BindingToKeys (cmd.AccelKey, out modeKey, out modeMod, out key, out mod)){
							Gdk.KeymapKey [] map = keymap.GetEntriesForKeyval (key);
							if (map != null && map.Length > 0){
								HIToolbox.SetMenuItemCommandKey (parentMenu, pos, true, (short) map [0].Keycode);
								MenuModifier menu_mod = 0;
								
								if ((mod & Gdk.ModifierType.Mod1Mask) != 0)
									menu_mod |= MenuModifier.OptionModifier;
								if ((mod & Gdk.ModifierType.ShiftMask) != 0)
									menu_mod |= MenuModifier.ShiftModifier;
								if ((mod & Gdk.ModifierType.ControlMask) != 0)
									menu_mod |= MenuModifier.ControlModifier;
								
								// This is inverted, because by default on OSX no setting means use the Command-key
								if ((mod & Gdk.ModifierType.MetaMask) == 0)
									menu_mod |= MenuModifier.NoCommandModifier;
								
								HIToolbox.SetMenuItemModifiers (parentMenu, pos, menu_mod);
							}
						}
						
						// Deal with checked items here.
					}
				} else {
					IntPtr menuRef = HIToolbox.CreateMenu (idSeq++, GetName (ces), 0);
					menus [menuRef] = GetName (ces);
					CreateChildren (menuRef, ces);
					ushort pos = HIToolbox.AppendMenuItem (parentMenu, GetName (ces), 0, 0);
					HIToolbox.CheckResult (HIToolbox.SetMenuItemHierarchicalMenu (parentMenu, pos, menuRef));
				}
			}
		}

		static void InstallRootMenu ()
		{
			HIToolbox.CheckResult (HIToolbox.SetRootMenu (rootMenu));
			
			openingHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuOpening, CarbonEventMenu.Opening);
			commandHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuCommand, CarbonEventCommand.Process);
			updateHandlerRef = Carbon.InstallApplicationEventHandler (HandleMenuCommandUpdate, CarbonEventCommand.UpdateStatus);
		}
		
		static CarbonEventReturn HandleMenuOpening (IntPtr callRef, IntPtr eventRef, IntPtr user_data)
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
			
			return CarbonEventReturn.NotHandled;
		}
		
		static CarbonEventReturn HandleMenuCommand (IntPtr callRef, IntPtr eventRef, IntPtr userData)
		{
			try {
				CarbonHICommand cmd = Carbon.GetEventParameter<CarbonHICommand> (eventRef, CarbonEventParameterName.DirectObject, CarbonEventParameterType.HICommand);
				object cmdID;
				if (commands.TryGetValue (cmd.CommandID, out cmdID)) {
					manager.DispatchCommand (cmdID);
				} else {
					MonoDevelop.Core.LoggingService.LogError ("Command not found for Mac menu {0} {1}", cmd.MenuItem.MenuRef, cmd.MenuItem.Index);
				}
			} catch (Exception ex) {
				System.Console.WriteLine(ex);
			}
			return CarbonEventReturn.NotHandled;
		}
		
		static CarbonEventReturn HandleMenuCommandUpdate (IntPtr callRef, IntPtr eventRef, IntPtr userData)
		{
			try {
				CarbonHICommand cmd = Carbon.GetEventParameter<CarbonHICommand> (eventRef, CarbonEventParameterName.DirectObject, CarbonEventParameterType.HICommand);
				object cmdID;
				if (commands.TryGetValue (cmd.CommandID, out cmdID)) {
					Command mdCmd = manager.GetCommand (cmdID);
					
				} else {
					MonoDevelop.Core.LoggingService.LogError ("Command not found for Mac menu {0} {1}", cmd.MenuItem.MenuRef, cmd.MenuItem.Index);
				}
			} catch (Exception ex) {
				System.Console.WriteLine(ex);
			}
			return CarbonEventReturn.NotHandled;
		}
		
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
				Carbon.RemoveEventHandler (updateHandlerRef);
				commandHandlerRef = openingHandlerRef = updateHandlerRef = rootMenu = IntPtr.Zero;
				cmdSeq = 0;
				idSeq = 0;
			}
		}
	}
	
}