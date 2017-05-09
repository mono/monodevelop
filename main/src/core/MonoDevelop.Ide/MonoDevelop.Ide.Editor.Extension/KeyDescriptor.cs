//
// KeyDescriptor.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
#if MAC
using AppKit;
#endif

namespace MonoDevelop.Ide.Editor.Extension
{
	public struct KeyDescriptor
	{
		public static KeyDescriptor Empty = new KeyDescriptor (SpecialKey.None, '\0', ModifierKeys.None, null);
		public static KeyDescriptor Tab = new KeyDescriptor (SpecialKey.Tab, '\t', ModifierKeys.None, null);

		public readonly SpecialKey SpecialKey;
		public readonly char KeyChar;
		public readonly ModifierKeys ModifierKeys;
		public readonly object NativeKeyChar;

		KeyDescriptor (SpecialKey specialKey, char keyChar, ModifierKeys modifierKeys, object nativeKeyChar)
		{
			SpecialKey = specialKey;
			KeyChar = keyChar;
			ModifierKeys = modifierKeys;
			NativeKeyChar = nativeKeyChar;
		}

		public override string ToString ()
		{
			return string.Format ("[KeyDescriptor: SpecialKey={0}, KeyChar={1}, ModifierKeys={2}]", SpecialKey, KeyChar, ModifierKeys);
		}

		#region GTK
		public static KeyDescriptor FromGtk (Gdk.Key key, char ch, Gdk.ModifierType state)
		{
			return new KeyDescriptor (ConvertKey (key), ch, ConvertModifiers (state), Tuple.Create (key, state));
		}

		static SpecialKey ConvertKey (Gdk.Key key)
		{
			switch (key) {
			case Gdk.Key.BackSpace:
				return SpecialKey.BackSpace;
			case Gdk.Key.Tab:
			case Gdk.Key.KP_Tab:
			case Gdk.Key.ISO_Left_Tab:
				return SpecialKey.Tab;
			case Gdk.Key.Return:
			case Gdk.Key.KP_Enter:
			case Gdk.Key.ISO_Enter:
				return SpecialKey.Return;
			case Gdk.Key.Escape:
				return SpecialKey.Escape;
			case Gdk.Key.space:
			case Gdk.Key.KP_Space:
				return SpecialKey.Space;
			case Gdk.Key.Page_Up:
			case Gdk.Key.KP_Page_Up:
				return SpecialKey.PageUp;
			case Gdk.Key.Page_Down:
			case Gdk.Key.KP_Page_Down:
				return SpecialKey.PageDown;
			case Gdk.Key.End:
			case Gdk.Key.KP_End:
				return SpecialKey.End;
			case Gdk.Key.Home:
			case Gdk.Key.KP_Home:
				return SpecialKey.Home;
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
				return SpecialKey.Left;
			case Gdk.Key.Up:
			case Gdk.Key.KP_Up:
				return SpecialKey.Up;
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
				return SpecialKey.Right;
			case Gdk.Key.Down:
			case Gdk.Key.KP_Down:
				return SpecialKey.Down;
			case Gdk.Key.Delete:
			case Gdk.Key.KP_Delete:
				return SpecialKey.Delete;
			}
			return SpecialKey.None;
		}

		static ModifierKeys ConvertModifiers (Gdk.ModifierType s)
		{
			ModifierKeys m = ModifierKeys.None;
			if ((s & Gdk.ModifierType.ShiftMask) != 0)
				m |= ModifierKeys.Shift;
			if ((s & Gdk.ModifierType.ControlMask) != 0)
				m |= ModifierKeys.Control;
			if ((s & Gdk.ModifierType.Mod1Mask) != 0)
				m |= ModifierKeys.Alt;
			if ((s & Gdk.ModifierType.Mod2Mask) != 0)
				m |= ModifierKeys.Command;
			if ((s & Gdk.ModifierType.MetaMask) != 0)
				m |= ModifierKeys.Command;
			return m;
		}
		#endregion

		#if MAC

		public static KeyDescriptor FromMac (char ch, NSEventModifierMask state)
		{
			var specialKey = ConvertKey (ref ch);
			var keyDescriptor = new KeyDescriptor (specialKey, ch, ConvertModifiers (state), state);
			return keyDescriptor;
		}

		static SpecialKey ConvertKey (ref char ch)
		{
			if (ch == '\n' || ch == '\r') {
				ch = '\0';
				return SpecialKey.Return;
			}
			if (ch == '\t') {
				ch = '\0';
				return SpecialKey.Tab;
			}
			if (ch == '\b' || ch == (char)127) {
				ch = '\0';
				return SpecialKey.BackSpace;
			}
			if (ch == (char)27) {
				ch = '\0';
				return SpecialKey.Escape;
			}
			switch ((NSKey)ch) {
			case NSKey.Delete:
				ch = '\0';
				return SpecialKey.BackSpace;
			case NSKey.Tab:
				ch = '\0';
				return SpecialKey.Tab;
			case NSKey.Return:
			case NSKey.KeypadEnter:
				ch = '\0';
				return SpecialKey.Return;
			case NSKey.Escape:
				ch = '\0';
				return SpecialKey.Escape;
			case NSKey.Space:
				ch = '\0';
				return SpecialKey.Space;
			case NSKey.PageUp:
				ch = '\0';
				return SpecialKey.PageUp;
			case NSKey.PageDown:
				ch = '\0';
				return SpecialKey.PageDown;
			case NSKey.End:
				ch = '\0';
				return SpecialKey.End;
			case NSKey.Home:
				ch = '\0';
				return SpecialKey.Home;
			case NSKey.LeftArrow:
				ch = '\0';
				return SpecialKey.Left;
			case NSKey.UpArrow:
				ch = '\0';
				return SpecialKey.Up;
			case NSKey.RightArrow:
				ch = '\0';
				return SpecialKey.Right;
			case NSKey.DownArrow:
				ch = '\0';
				return SpecialKey.Down;
			case NSKey.ForwardDelete:
				ch = '\0';
				return SpecialKey.Delete;
			}
			return SpecialKey.None;
		}



		static ModifierKeys ConvertModifiers (NSEventModifierMask e)
		{
			var m = ModifierKeys.None;
			if ((e & NSEventModifierMask.ControlKeyMask) != 0)
				m |= ModifierKeys.Control;
			if ((e & NSEventModifierMask.AlternateKeyMask) != 0)
				m |= ModifierKeys.Alt;
			if ((e & NSEventModifierMask.CommandKeyMask) != 0)
				m |= ModifierKeys.Command;
			if ((e & NSEventModifierMask.ShiftKeyMask) != 0)
				m |= ModifierKeys.Shift;
			return m;
		}
		#endif
	}
}

