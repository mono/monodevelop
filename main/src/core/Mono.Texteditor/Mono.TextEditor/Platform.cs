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
		const Gdk.ModifierType META_MASK = (Gdk.ModifierType) 0x10000000; //FIXME GTK+ 2.12: Gdk.ModifierType.MetaMask;

		static Platform ()
		{
			IsMac = IsRunningOnMac ();
			IsX11 = !IsMac && System.Environment.OSVersion.Platform == PlatformID.Unix;
			IsWindows = System.IO.Path.PathSeparator == '\\';
		}
		
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
			key = evt.Key;
			mod = evt.State;
			
			if (IsX11) {
				//this is a workaround for a common X mapping issue
				//where shift-alt is translated to the meta key
				if (key.Equals (Gdk.Key.Meta_L) || key.Equals (Gdk.Key.Meta_R))
					key = Gdk.Key.Alt_L;
			}
			
			//HACK: the MAC GTK+ port currently does some horrible, un-GTK-ish key mappings
			if (IsMac && !IsX11) {
				if ((mod & Gdk.ModifierType.Mod1Mask) != 0) {
					mod ^= Gdk.ModifierType.Mod1Mask;
					mod |= META_MASK;
				}
				if ((mod & Gdk.ModifierType.Mod5Mask) != 0) {
					mod ^= Gdk.ModifierType.Mod5Mask;
					mod |= Gdk.ModifierType.Mod1Mask;
				}
				if (evt.Group == (byte) 1) {
					mod |= Gdk.ModifierType.Mod1Mask;
					key = GetRootKey (evt);
				}
			}
		}
		
		static Dictionary<Gdk.Key,Gdk.Key> hardwareMappings;
		static Gdk.Keymap keymap;
		
		static Gdk.Key GetRootKey (Gdk.EventKey evt)
		{
			if (keymap == null) {
				keymap = Gdk.Keymap.Default;
				keymap.KeysChanged += delegate { hardwareMappings.Clear (); };
				hardwareMappings = new Dictionary<Gdk.Key,Gdk.Key> ();
			}
			
			Gdk.Key ret;
			if (hardwareMappings.TryGetValue (evt.Key, out ret))
				return ret;
			
			//FIXME: LookupKey isn't implemented on Mac, so we have to use this workaround
			uint[] keyvals;
			Gdk.KeymapKey [] keys;
			keymap.GetEntriesForKeycode (evt.HardwareKeycode, out keys, out keyvals);
			
			for (uint i = 0; i < keys.Length; i++) {
				if (keys[i].Group == 0 && keys[i].Level == 0) {
					ret = (Gdk.Key)keyvals[i];
					foreach (Gdk.Key key in keyvals)
						hardwareMappings[key] = ret;
					return ret;
				}
			}
			
			//failed, but avoid looking it up again
			return hardwareMappings[evt.Key] = evt.Key;
		}
	}
}
