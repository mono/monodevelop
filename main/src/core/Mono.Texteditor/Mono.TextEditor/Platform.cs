// 
// Platform.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Gdk;

namespace Mono.TextEditor
{
	public static class Platform
	{
		static Platform ()
		{
 			IsWindows = System.IO.Path.DirectorySeparatorChar == '\\';
 			IsMac = !IsWindows && IsRunningOnMac();
			IsX11 = !IsMac && System.Environment.OSVersion.Platform == PlatformID.Unix;
			
			keymap.KeysChanged += delegate {
				groupZeroMappings.Clear ();
			};
		}
		
		static Gdk.Keymap keymap = Gdk.Keymap.Default;
		
		public static bool IsMac { get; private set; }
		public static bool IsX11 { get; private set; }
		public static bool IsWindows { get; private set; }
		
		//From Managed.Windows.Forms/XplatUI
		static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					Marshal.FreeHGlobal (buf);
			}
			return false;
		}
		
		[DllImport ("libc")]
		static extern int uname (IntPtr buf);
		
		//from MonoDevelop.Components.Commands.KeyBindingManager
		internal static void MapRawKeys (Gdk.EventKey evt, out Gdk.Key key, out Gdk.ModifierType mod)
		{
			mod = evt.State;
			key = evt.Key;
			
			uint keyval;
			int effectiveGroup, level;
			Gdk.ModifierType consumedModifiers;
			keymap.TranslateKeyboardState (evt.HardwareKeycode, evt.State, evt.Group, out keyval, out effectiveGroup,
			                               out level, out consumedModifiers);
			
			key = (Gdk.Key)keyval;
			mod = evt.State & ~consumedModifiers;
			
			//we restore the shift modifier if it was a letter, since those always get displayed uppercase
			if (char.IsUpper ((char)Gdk.Keyval.ToUnicode (keyval)) && ((consumedModifiers & Gdk.ModifierType.ShiftMask) != 0))
				mod |= Gdk.ModifierType.ShiftMask;
			
			if (IsX11) {
				//this is a workaround for a common X mapping issue
				//where the alt key is mapped to the meta key when the shift modifier is active
				if (key.Equals (Gdk.Key.Meta_L) || key.Equals (Gdk.Key.Meta_R))
					key = Gdk.Key.Alt_L;
			}
			
			//HACK: the MAC GTK+ port currently does some horrible, un-GTK-ish key mappings
			// so we work around them by playing some tricks to remap and decompose modifiers.
			// We also decompose keys to the root physical key so that the Mac command 
			// combinations appear as expected, e.g. shift-{ is treated as shift-[.
			if (IsMac && !IsX11) {
				// Mac GTK+ maps the command key to the Mod1 modifier, which usually means alt/
				// We map this instead to meta, because the Mac GTK+ has mapped the cmd key
				// to the meta key (yay inconsistency!). IMO super would have been saner.
				if ((mod & Gdk.ModifierType.Mod1Mask) != 0) {
					mod ^= Gdk.ModifierType.Mod1Mask;
					mod |= Gdk.ModifierType.MetaMask;
				}
				
				// If Mod5 is active it *might* mean that opt/alt is active,
				// so we can unset this and map it back to the normal modifier.
				if ((mod & Gdk.ModifierType.Mod5Mask) != 0) {
					mod ^= Gdk.ModifierType.Mod5Mask;
					mod |= Gdk.ModifierType.Mod1Mask;
				}
				
				// When opt modifier is active, we need to decompose this to make the command appear correct for Mac.
				// In addition, we can only inspect whether the opt/alt key is pressed by examining
				// the key's "group", because the Mac GTK+ treats opt as a group modifier and does
				// not expose it as an actual GDK modifier.
				if (evt.Group == (byte) 1) {
					mod |= Gdk.ModifierType.Mod1Mask;
					key = GetGroupZeroKey (key, evt);
				}
			}
			
			//fix shift-tab weirdness. There isn't a nice name for untab, so make it shift-tab
			if (key == Gdk.Key.ISO_Left_Tab) {
				key = Gdk.Key.Tab;
				mod |= Gdk.ModifierType.ShiftMask;
			}
		}
		
		static Dictionary<Gdk.Key,Gdk.Key> groupZeroMappings = new Dictionary<Gdk.Key,Gdk.Key> ();
		
		static Gdk.Key GetGroupZeroKey (Gdk.Key mappedKey, Gdk.EventKey evt)
		{
			Gdk.Key ret;
			if (groupZeroMappings.TryGetValue (mappedKey, out ret))
				return ret;
			
			//LookupKey isn't implemented on Mac, so we have to use this workaround
			uint[] keyvals;
			Gdk.KeymapKey [] keys;
			keymap.GetEntriesForKeycode (evt.HardwareKeycode, out keys, out keyvals);
			
			//find the key that has the same level (so we preserve shift) but with group 0
			for (uint i = 0; i < keyvals.Length; i++)
				if (keyvals[i] == (uint)mappedKey)
					for (uint j = 0; j < keys.Length; j++)
						if (keys[j].Group == 0 && keys[j].Level == keys[i].Level)
							return groupZeroMappings[mappedKey] = ret = (Gdk.Key)keyvals[j];
			
			//failed, but avoid looking it up again
			return groupZeroMappings[mappedKey] = mappedKey;
		}
	}
}
