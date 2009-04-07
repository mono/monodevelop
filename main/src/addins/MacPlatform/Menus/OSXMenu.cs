//
// OSXMenu.cs
//
// Author:
//   Miguel de Icaza
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
using System.Runtime.InteropServices;
using MonoDevelop.Components.Commands;

namespace OSXIntegration
{
	internal enum MenuResult {
		Ok = 0,
		PropertyInvalid = -5603,
		PropertyNotFound = -5604,
		NotFound  = -5620,
		UsesSystemDef = -5621,
		ItemNotFound = -5622,
		Invalid = -5623
	}

	[Flags]
	internal enum MenuAttributes {
		ExcludesMarkColumn = 1,
		AutoDisable = 1 << 2,
		UsePencilGlyph = 1 << 3,
		Hidden = 1 << 4,
		CondenseSeparators = 1 << 5,
		DoNotCacheImage = 1 << 6,
		DoNotUseUserCommandKeys = 1 << 7
	}

	internal enum MenuModifier : byte {
		NoModifier = 0,
		ShiftModifier = 1 << 0,
		OptionModifier = 1 << 1,
		ControlModifier = 1 << 2,
		NoCommandModifier = 1 << 3
	}
	
	[Flags]
	internal enum MenuItemAttributes {
		Disabled = 1 << 0,
		IconDisabled = 1 << 1,
		SubmenuParentChoosable = 1 << 2,
		Dynamic = 1 << 3,
		NotPreviousAlternate = 1 << 4,
		Hidden = 1 << 5,
		Separator = 1 << 6,
		SectionHeader = 1 << 7,
		IgnoreMeta = 1 << 8,
		AutoRepeat = 1 << 9,
		UseVirtualKey = 1 << 10,
		CustomDraw = 1 << 11,
		IncludeInCmdKeyMatching = 1 << 12,
		AutoDisable = 1 << 13,
		UpdateSingleItem = 1 << 14
	}

	public class OSXMenu : IDisposable
	{
		const string hitoolbox = "/System/Library/Frameworks/Carbon.framework/Versions/A/Frameworks/HIToolbox.framework/Versions/A/HIToolbox";
		const string cflib = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";
		
		[DllImport (hitoolbox)]
		internal static extern MenuResult CreateNewMenu (short menuid, MenuAttributes attributes, out IntPtr menuref);

		[DllImport (hitoolbox)]
		internal static extern MenuResult SetRootMenu (IntPtr menuref);

		[DllImport (hitoolbox)]
		internal static extern void DisposeMenu (IntPtr the_menu);

		[DllImport (hitoolbox)]
		internal static extern void InsertMenu (IntPtr menu_ref, short before_id);
		
		[DllImport (hitoolbox)]
		internal static extern MenuResult AppendMenuItemTextWithCFString (IntPtr menu_ref, IntPtr cfstring, MenuItemAttributes inAttributes, int command_id, out short index);

		[DllImport (hitoolbox)]
		internal static extern MenuResult SetMenuItemHierarchicalMenu (IntPtr parent_menu, short parent_index, IntPtr submenu);

		[DllImport (hitoolbox)]
		internal static extern MenuResult SetMenuTitleWithCFString (IntPtr menu_ref, IntPtr cfstring);

		[DllImport (hitoolbox)]
		internal static extern MenuResult SetMenuItemKeyGlyph (IntPtr menu_ref, short index, short glyph);

		[DllImport (hitoolbox)]
		internal static extern MenuResult SetMenuItemCommandKey (IntPtr menuref, short index, bool isVirtualKey, short key);

		[DllImport (hitoolbox)]
		internal static extern MenuResult SetMenuItemModifiers (IntPtr menuref, short index, MenuModifier modifiers);
		
		[DllImport (cflib)]
		internal static extern IntPtr CFStringCreateWithCString (IntPtr alloc, string str, int encoding);

		[DllImport (cflib)]
		internal static extern void CFRelease (IntPtr cfstr);

		internal static IntPtr GetCFString (string s)
		{
			// The magic value is "kCFStringENcodingUTF8"
			return CFStringCreateWithCString (IntPtr.Zero, s, 0x08000100);
		}

		protected IntPtr menuref;		// The handle returned by the CreateNewMenu
		short id; 				// The ID for this menu
		static short id_sequence;

		int cmdseq;

		string GetName (CommandEntrySet ces)
		{
			string text = ces.Name ?? "";

			return text.Replace ("_", "");
		}

		public OSXMenu (CommandManager manager, CommandEntrySet entrySet) 
		{
			id = id_sequence++;
			
			if (CreateNewMenu (id, 0, out menuref) != 0)
				throw new SystemException ("Unable to create a topmenu bar");

			string text = GetName (entrySet);
			IntPtr cfstring = GetCFString (text);
			SetMenuTitleWithCFString (menuref, cfstring);
			CFRelease (cfstring);

			Gdk.Keymap keymap = Gdk.Keymap.Default;
			foreach (CommandEntry entry in entrySet){
				CommandEntrySet ces = entry as CommandEntrySet;

				if (ces == null){
					short pos;

					object cmdId = entry.CommandId;

					if (cmdId == Command.Separator){
						AppendMenuItemTextWithCFString (menuref, IntPtr.Zero, MenuItemAttributes.Separator, 0, out pos);
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

						bool isArray = acmd.CommandArray;

						Console.WriteLine ("Accel for {0} is: {1}", cmd.Text ?? "empty", cmd.AccelKey);

						cfstring = GetCFString ((cmd.Text ?? "").Replace ("_", ""));
						AppendMenuItemTextWithCFString (menuref, cfstring, 0, cmdseq++, out pos);

						uint modeKey, key;
						Gdk.ModifierType modeMod, mod;
						
						if (KeyBindingManager.BindingToKeys (cmd.AccelKey, out modeKey, out modeMod, out key, out mod)){
							Gdk.KeymapKey [] map = keymap.GetEntriesForKeyval (key);
							if (map != null && map.Length > 0){
								SetMenuItemCommandKey (menuref, pos, true, (short) map [0].Keycode);
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
								
								SetMenuItemModifiers (menuref, pos, menu_mod);
							}
						}
						
						CFRelease (cfstring);
						// Deal with checked items here.
					}
				} else {
					OSXMenu child = new OSXMenu (manager, ces);
					short pos;

					cfstring = GetCFString (GetName (ces));
					AppendMenuItemTextWithCFString (menuref, cfstring, 0, 0, out pos);
					CFRelease (cfstring);
					var r = SetMenuItemHierarchicalMenu (menuref, pos, child.menuref);
					if (r != MenuResult.Ok)
						Console.WriteLine ("AttachResult={0}", r);
				}
			}
		}

		public void InstallMenu ()
		{
			if (SetRootMenu (menuref) != 0)
				throw new SystemException ("Unable to set the root menu");
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		public void Dispose (bool disposing)
		{
			if (menuref != IntPtr.Zero){
				if (disposing){
					Console.WriteLine ("SHOULD DISPOING");
					//DisposeMenu (menuref);
				} else {
					Console.WriteLine ("Warning, menu was finalized, not disposed");
				}
				menuref = IntPtr.Zero;
			}
		}
	}
	
}