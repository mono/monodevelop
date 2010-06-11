// 
// ViKeyNotation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Gdk;
using System.Collections.Generic;
using System.Text;

namespace Mono.TextEditor.Vi
{
	public struct ViKey
	{
		public ViKey (Gdk.ModifierType modifiers, uint unicodeKey, Gdk.Key key)
		{
			this.Modifiers = modifiers & KnownModifiers;
			this.UnicodeKey = unicodeKey;
			this.Key = key;
		}
		
		public Gdk.ModifierType Modifiers { get; private set; }
		public uint UnicodeKey { get; private set; }
		public Gdk.Key Key { get; private set; }
		
		static ModifierType KnownModifiers = ModifierType.ShiftMask | ModifierType.MetaMask
			| ModifierType.ControlMask | ModifierType.Mod1Mask;
	}
	
	public static class ViKeyNotation
	{
		public static string ToString (IList<ViKey> keys)
		{
			var sb = new StringBuilder ();
			foreach (var k in keys)
				AppendToString (k, sb);
			return sb.ToString ();
		}
		
		static void AppendToString (ViKey key, StringBuilder sb)
		{
			var c = GetString (key.UnicodeKey);
			if (c != null && key.UnicodeKey != 0) {
				if (c.Length == 1 && key.Modifiers == ModifierType.None) {
					sb.Append (c);
					return;
				}
			} else {
				c = GetString (key.Key);
			}
			
			if (c == null) {
				var msg = string.Format ("Invalid key char=0x{0:x} key={1}", (int)key.UnicodeKey, key.Key);
				throw new InvalidOperationException (msg);
			}
			
			sb.Append ("<");
			
			if ((key.Modifiers & ModifierType.ShiftMask) != 0)
				sb.Append ("S-");
			if ((key.Modifiers & ModifierType.ControlMask) != 0)
				sb.Append ("C-");
			if ((key.Modifiers & ModifierType.Mod1Mask) != 0)
				sb.Append ("M-");
			if ((key.Modifiers & ModifierType.MetaMask) != 0) //HACK: Mac command key
				sb.Append ("D-");
			
			sb.Append (c);
			sb.Append (">");
		}
		
		public static bool IsValid (ViKey key)
		{
			return GetString (key.UnicodeKey) != null || GetString (key.Key) != null;
		}
		
		static string GetString (uint unicodeKey)
		{
			string str = char.ConvertFromUtf32 ((int)unicodeKey);
			if (str.Length == 1) {
				switch (str[0]) {
				case '\0': return "Nul";
				case ' ':  return "Space";
				case '\r': return "CR";
				case '\n': return "NL";
				case '\f': return "FF";
				case '\t': return "Tab";
				case '<':  return "lt";
				case '\\': return "Bslash";
				case '|':  return "Bar";
				}
				if (char.IsControl (str[0]))
					return null;
			}
			return str;
		}
		
		static string GetString (Key key)
		{
			switch (key) {
			case Gdk.Key.BackSpace:    return "BS";
			case Gdk.Key.Tab:          return "Tab";
			case Gdk.Key.Return:       return "Enter"; //"CR" "Return"
			case Gdk.Key.Escape:       return "Esc";
			case Gdk.Key.space:        return "Space";
			case Gdk.Key.KP_Up:
			case Gdk.Key.Up:           return "Up";
			case Gdk.Key.KP_Down:
			case Gdk.Key.Down:         return "Down";
			case Gdk.Key.KP_Left:
			case Gdk.Key.Left:         return "Left";
			case Gdk.Key.KP_Right:
			case Gdk.Key.Right:        return "Right";
			case Gdk.Key.F1:           return "F1";
			case Gdk.Key.F2:           return "F2";
			case Gdk.Key.F3:           return "F3";
			case Gdk.Key.F4:           return "F4";
			case Gdk.Key.F5:           return "F5";
			case Gdk.Key.F6:           return "F6";
			case Gdk.Key.F7:           return "F7";
			case Gdk.Key.F8:           return "F8";
			case Gdk.Key.F9:           return "F9";
			case Gdk.Key.F10:          return "F10";
			case Gdk.Key.F11:          return "F11";
			case Gdk.Key.F12:          return "F12";
			case Gdk.Key.Insert:       return "Insert";
			case Gdk.Key.Delete:       return "Del";
			case Gdk.Key.Home:         return "Home";
			case Gdk.Key.End:          return "End";
			case Gdk.Key.Page_Up:      return "PageUp";
			case Gdk.Key.Page_Down:    return "PageDown";
			case Gdk.Key.KP_Home:      return "kHome";
			case Gdk.Key.KP_End:       return "kEnd";
			case Gdk.Key.KP_Page_Up:   return "kPageUp";
			case Gdk.Key.KP_Page_Down: return "kPageDown";
			case Gdk.Key.KP_Add:       return "kPlus";
			case Gdk.Key.KP_Subtract:  return "kMinus";
			case Gdk.Key.KP_Multiply:  return "kMultiply";
			case Gdk.Key.KP_Divide:    return "kDivide";
			case Gdk.Key.KP_Enter:     return "kEnter";
			case Gdk.Key.KP_Decimal:   return "kPoint";
			case Gdk.Key.KP_0:         return "k0";
			case Gdk.Key.KP_1:         return "k1";
			case Gdk.Key.KP_2:         return "k2";
			case Gdk.Key.KP_3:         return "k3";
			case Gdk.Key.KP_4:         return "k4";
			case Gdk.Key.KP_5:         return "k5";
			case Gdk.Key.KP_6:         return "k6";
			case Gdk.Key.KP_7:         return "k7";
			case Gdk.Key.KP_8:         return "k8";
			case Gdk.Key.KP_9:         return "k9";
			case Gdk.Key.Help:         return "Help";
			case Gdk.Key.Undo:         return "Undo";
			default:                   return null;
			}
		}
		
		static char GetChar (string charName)
		{
			switch (charName) {
			case "Nul":    return '\0';
			case "Space":  return ' ';
			case "CR":     return '\r';
			//FIXME this should be environment/editor-dependent
			case "EOL":
			case "NL":     return '\n';
			case "FF":     return '\f';
			case "Tab":    return '\t';
			case "lt":     return '<';
			case "Bslash": return '\\';
			case "Bar":    return '|';
			}
			if (charName.Length == 1)
				return charName[0];
			throw new FormatException ("Unknown char '" + charName +"'");
		}
		
		
		static Gdk.Key GetKey (string code)
		{
			switch (code) {
			case "BS":        return Gdk.Key.BackSpace;
			case "Tab":       return Gdk.Key.Tab;
			case "CR":
			case "Return":
			case "Enter":     return Gdk.Key.Return;
			case "Esc":       return Gdk.Key.Escape;
			case "Space":     return Gdk.Key.space;
			case "Up":        return Gdk.Key.Up;
			case "Down":      return Gdk.Key.Down;
			case "Left":      return Gdk.Key.Left;
			case "Right":     return Gdk.Key.Right;
			case "#1":
			case "F1":        return Gdk.Key.F1;
			case "#2":
			case "F2":        return Gdk.Key.F2;
			case "#3":
			case "F3":        return Gdk.Key.F3;
			case "#4":
			case "F4":        return Gdk.Key.F4;
			case "#5":
			case "F5":        return Gdk.Key.F5;
			case "#6":
			case "F6":        return Gdk.Key.F6;
			case "#7":
			case "F7":        return Gdk.Key.F7;
			case "#8":
			case "F8":        return Gdk.Key.F8;
			case "#9":
			case "F9":        return Gdk.Key.F9;
			case "#0":
			case "F10":       return Gdk.Key.F10;
			case "F11":       return Gdk.Key.F11;
			case "F12":       return Gdk.Key.F12;
			case "Insert":    return Gdk.Key.Insert;
			case "Del":       return Gdk.Key.Delete;
			case "Home":      return Gdk.Key.Home;
			case "End":       return Gdk.Key.End;
			case "PageUp":    return Gdk.Key.Page_Up;
			case "PageDown":  return Gdk.Key.Page_Down;
			case "kHome":     return Gdk.Key.KP_Home;
			case "kEnd":      return Gdk.Key.KP_End;
			case "kPageUp":   return Gdk.Key.KP_Page_Up;
			case "kPageDown": return Gdk.Key.KP_Page_Down;
			case "kPlus":     return Gdk.Key.KP_Add;
			case "kMinus":    return Gdk.Key.KP_Subtract;
			case "kMultiply": return Gdk.Key.KP_Multiply;
			case "kDivide":   return Gdk.Key.KP_Divide;
			case "kEnter":    return Gdk.Key.KP_Enter;
			case "kPoint":    return Gdk.Key.KP_Decimal;
			case "k0":        return Gdk.Key.KP_0;
			case "k1":        return Gdk.Key.KP_1;
			case "k2":        return Gdk.Key.KP_2;
			case "k3":        return Gdk.Key.KP_3;
			case "k4":        return Gdk.Key.KP_4;
			case "k5":        return Gdk.Key.KP_5;
			case "k6":        return Gdk.Key.KP_6;
			case "k7":        return Gdk.Key.KP_7;
			case "k8":        return Gdk.Key.KP_8;
			case "k9":        return Gdk.Key.KP_9;
			case "Help":      return Gdk.Key.Help;
			case "Undo":      return Gdk.Key.Undo;
			default: return (Gdk.Key)0;
			}
		}
		
		static ViKey FlattenControlMappings (ViKey k)
		{
			if ((k.Modifiers & ModifierType.ControlMask) == k.Modifiers) {
				switch (k.UnicodeKey) {
				case '@': return new ViKey (ModifierType.None, '\0', (Gdk.Key)0);
				case 'h': return new ViKey (ModifierType.None, '\0', Gdk.Key.BackSpace);
				case 'i': return new ViKey (ModifierType.None, '\t', Gdk.Key.Tab);
				case 'j': return new ViKey (ModifierType.None, '\n', Gdk.Key.Linefeed);
				case 'l': return new ViKey (ModifierType.None, '\f', (Gdk.Key)0);
				case 'm': return new ViKey (ModifierType.None, '\r', Gdk.Key.Return);
				case '[': return new ViKey (ModifierType.None, '\0', Gdk.Key.Escape);
				case 'p': return new ViKey (ModifierType.None, '\0', Gdk.Key.Up);
				}
			}
			return k;
		}

		public static IList<ViKey> Parse (string command)
		{
			var list = new List<ViKey> ();
			for (int i = 0; i < command.Length; i++) {
				if (command[i] == '<') {
					int j = command.IndexOf ('>', i);
					if (j < i + 2)
						throw new FormatException ("Could not find matching > at index " + i.ToString ());
					string seq = command.Substring (i + 1, j - i - 1);
					list.Add (ParseKeySequence (seq));
					i = j;
				} else {
					list.Add (new ViKey (ModifierType.None, command[i], (Gdk.Key)0));
				}
			}
			return list;
		}
		
		//TODO <CSI> <xCSI>
		static ViKey ParseKeySequence (string seq)
		{
			var modifiers = ModifierType.None;
			while (seq.Length > 2 && seq[1] == '-') {
				switch (seq[0]) {
				case 'S':
					modifiers |= ModifierType.ShiftMask;
					break;
				case 'C':
					modifiers |= ModifierType.ControlMask;
					break;
				case 'M':
				case 'A':
					modifiers |= ModifierType.Mod1Mask;
					break;
				case 'D':
					modifiers |= ModifierType.MetaMask;  //HACK: Mac command key
					break;
				default:
					throw new FormatException ("Unknown modifier " + seq[0].ToString ());
				}
				seq = seq.Substring (2);
			}
			var k = GetKey (seq);
			var c = '\0';
			if (k == (Gdk.Key)0)
				c = GetChar (seq);
			
			var key = new ViKey (modifiers, c, k);
			if (!IsValid (key))
				throw new FormatException ("Invalid key sequence '" + seq + "'");
			return key;
		}
	}
}

