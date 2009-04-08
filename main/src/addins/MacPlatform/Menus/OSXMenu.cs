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
	

	public class OSXMenu : IDisposable
	{
		protected IntPtr menuref;		// The handle returned by the CreateNewMenu
		ushort id; 				// The ID for this menu
		ushort id_sequence;
		static CommandManager manager;
		object commandId;

		static uint cmdseq;
		
		static Dictionary<uint,object> commands = new Dictionary<uint,object> ();
		static Dictionary<IntPtr,string> menus = new Dictionary<IntPtr,string> ();
		
		string GetName (CommandEntrySet ces)
		{
			string text = ces.Name ?? "";

			return text.Replace ("_", "");
		}

		public OSXMenu (CommandManager manager, CommandEntrySet entrySet) 
		{
			OSXMenu.manager = manager;
			id = id_sequence++;
			
			HIToolbox.CheckResult (HIToolbox.CreateNewMenu (id, 0, out menuref));
			
			menus[menuref] = entrySet.Name;
			
			HIToolbox.SetMenuTitle (menuref, GetName (entrySet));

			Gdk.Keymap keymap = Gdk.Keymap.Default;
			foreach (CommandEntry entry in entrySet){
				CommandEntrySet ces = entry as CommandEntrySet;

				if (ces == null){
					ushort pos;

					object cmdId = entry.CommandId;

					if (cmdId == Command.Separator){
						HIToolbox.AppendMenuSeparator (menuref);
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
						
						commands[cmdseq] = cmd.Id;

						bool isArray = acmd.CommandArray;
						
						pos = HIToolbox.AppendMenuItem (menuref, (cmd.Text ?? "").Replace ("_", ""), 0, cmdseq++);
						
						uint modeKey, key;
						Gdk.ModifierType modeMod, mod;
						
						if (KeyBindingManager.BindingToKeys (cmd.AccelKey, out modeKey, out modeMod, out key, out mod)){
							Gdk.KeymapKey [] map = keymap.GetEntriesForKeyval (key);
							if (map != null && map.Length > 0){
								HIToolbox.SetMenuItemCommandKey (menuref, pos, true, (short) map [0].Keycode);
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
								
								HIToolbox.SetMenuItemModifiers (menuref, pos, menu_mod);
							}
						}
						
						// Deal with checked items here.
					}
				} else {
					OSXMenu child = new OSXMenu (manager, ces);
					ushort pos;
					
					pos = HIToolbox.AppendMenuItem (menuref, GetName (ces), 0, 0);
					var r = HIToolbox.SetMenuItemHierarchicalMenu (menuref, pos, child.menuref);
					if (r != MenuResult.Ok)
						Console.WriteLine ("AttachResult={0}", r);
				}
			}
		}
		
		static CarbonEventTypeSpec[] MenuOpeningEventsSpec = new CarbonEventTypeSpec [] {
			new CarbonEventTypeSpec (CarbonEventMenu.Opening)
		};
		
		static CarbonEventTypeSpec[] CommandsEventsSpec = new CarbonEventTypeSpec [] {
			new CarbonEventTypeSpec (CarbonEventCommand.Process)
		};

		public void InstallMenu ()
		{
			if (HIToolbox.SetRootMenu (menuref) != 0)
				throw new SystemException ("Unable to set the root menu");
			
			IntPtr handlerRef;
			Carbon.InstallApplicationEventHandler (HandleMenuOpening, CarbonEventMenu.Opening, out handlerRef);
			
			IntPtr handlerRef2;
			Carbon.InstallApplicationEventHandler (HandleMenuCommand, CarbonEventCommand.Process, out handlerRef2);
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

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		public void Dispose (bool disposing)
		{
			if (menuref != IntPtr.Zero) {
				menus.Remove (menuref);
				CoreFoundation.Release (menuref);
				if (!disposing)
					MonoDevelop.Core.LoggingService.LogWarning ("Warning, Mac menu was finalized, not disposed");
				menuref = IntPtr.Zero;
			}
		}
	}
	
}