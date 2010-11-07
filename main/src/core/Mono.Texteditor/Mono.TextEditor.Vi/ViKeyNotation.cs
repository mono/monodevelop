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
	public struct ViKey : IEquatable<ViKey>
	{
		Gdk.ModifierType modifiers;
		char ch;
		Gdk.Key key;
		
		public ViKey (char ch) : this (ModifierType.None, ch, (Gdk.Key) 0)
		{
		}
		
		public ViKey (Gdk.Key key) : this (ModifierType.None, '\0', key)
		{
		}
		
		public ViKey (ModifierType modifiers, Gdk.Key key) : this (modifiers, '\0', key)
		{
		}
		
		public ViKey (ModifierType modifiers, char ch) : this (modifiers, ch, (Gdk.Key)0)
		{
		}
		
		ViKey (ModifierType modifiers, char ch, Gdk.Key key): this ()
		{
			this.modifiers = modifiers & KnownModifiers;
			this.ch = ch;
			this.key = key;
		}
		
		public ModifierType Modifiers { get { return this.modifiers; } }
		public char Char { get { return this.ch; } }
		public Key Key { get { return this.key; } }
		
		static ModifierType KnownModifiers = ModifierType.ShiftMask | ModifierType.MetaMask
			| ModifierType.ControlMask | ModifierType.Mod1Mask;
		
		public static implicit operator ViKey (char ch)
		{
			return new ViKey (ch);
		}
		
		public static implicit operator ViKey (Gdk.Key key)
		{
			return new ViKey (key);
		}
		
		public bool Equals (ViKey other)
		{
			return modifiers == other.modifiers && ch == other.ch && key == other.key;
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (!(obj is ViKey))
				return false;
			return Equals ((ViKey)obj);
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				return modifiers.GetHashCode () ^ ch.GetHashCode () ^ key.GetHashCode ();
			}
		}
		
		public override string ToString()
		{
			return ViKeyNotation.IsValid(this)? ViKeyNotation.ToString(this) : "<Invalid>";
		}
	}
	
	public static class ViKeyNotation
	{
		public static string ToString(ViKey key)
		{
			var sb = new StringBuilder();
			ViKeyNotation.AppendToString(key, sb);
			return sb.ToString();
		}

		public static string ToString (IList<ViKey> keys)
		{
			var sb = new StringBuilder ();
			foreach (var k in keys)
				AppendToString (k, sb);
			return sb.ToString ();
		}
		
		static void AppendToString (ViKey key, StringBuilder sb)
		{
			var c = GetString (key.Char);
			if (c != null && key.Char != '\0') {
				if (c.Length == 1 && key.Modifiers == ModifierType.None) {
					sb.Append (c);
					return;
				}
			} else {
				c = GetString (key.Key);
			}
			
			if (c == null) {
				var msg = string.Format ("Invalid key char=0x{0:x} key={1}", (int)key.Char, key.Key);
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
			return GetString (key.Char) != null || keyStringMaps.ContainsKey (key.Key);
		}
		
		static string GetString (char ch)
		{
			string s;
			if (charStringMaps.TryGetValue (ch, out s))
				return s;
			if (char.IsControl (ch))
				return null;
			return ch.ToString ();
		}
		
		static Dictionary<char, string> charStringMaps = new Dictionary<char, string> () {
			{ '\0', "Nul" },
			{ ' ',  "Space" },
			{ '\r', "CR" },
			{ '\n', "NL" },
			{ '\f', "FF" },
			{ '\t', "Tab" },
			{ '<',  "lt" },
			{ '\\', "Bslash" },
			{ '|',  "Bar" },
		};
		
		static string GetString (Key key)
		{
			string str;
			if (keyStringMaps.TryGetValue (key, out str))
				return str;
			return null;
		}
		
		static Dictionary<Gdk.Key, string> keyStringMaps = new Dictionary<Gdk.Key, string> () {
			{ Gdk.Key.BackSpace,    "BS"        },
			{ Gdk.Key.Tab,          "Tab"       },
			{ Gdk.Key.Return,       "Enter"     }, //"CR" "Return"
			{ Gdk.Key.Escape,       "Esc"       },
			{ Gdk.Key.space,        "Space"     },
			{ Gdk.Key.KP_Up,        "Up"        },
			{ Gdk.Key.Up,           "Up"        },
			{ Gdk.Key.KP_Down,      "Down"      },
			{ Gdk.Key.Down,         "Down"      },
			{ Gdk.Key.KP_Left,      "Left"      },
			{ Gdk.Key.Left,         "Left"      },
			{ Gdk.Key.KP_Right,     "Right"     },
			{ Gdk.Key.Right,        "Right"     },
			{ Gdk.Key.F1,           "F1"        },
			{ Gdk.Key.F2,           "F2"        },
			{ Gdk.Key.F3,           "F3"        },
			{ Gdk.Key.F4,           "F4"        },
			{ Gdk.Key.F5,           "F5"        },
			{ Gdk.Key.F6,           "F6"        },
			{ Gdk.Key.F7,           "F7"        },
			{ Gdk.Key.F8,           "F8"        },
			{ Gdk.Key.F9,           "F9"        },
			{ Gdk.Key.F10,          "F10"       },
			{ Gdk.Key.F11,          "F11"       },
			{ Gdk.Key.F12,          "F12"       },
			{ Gdk.Key.Insert,       "Insert"    },
			{ Gdk.Key.Delete,       "Del"       },
			{ Gdk.Key.KP_Delete,    "kDel"		},
			{ Gdk.Key.Home,         "Home"      },
			{ Gdk.Key.End,          "End"       },
			{ Gdk.Key.Page_Up,      "PageUp"    },
			{ Gdk.Key.Page_Down,    "PageDown"  },
			{ Gdk.Key.KP_Home,      "kHome"     },
			{ Gdk.Key.KP_End,       "kEnd"      },
			{ Gdk.Key.KP_Page_Up,   "kPageUp"   },
			{ Gdk.Key.KP_Page_Down, "kPageDown" },
			{ Gdk.Key.KP_Add,       "kPlus"     },
			{ Gdk.Key.KP_Subtract,  "kMinus"    },
			{ Gdk.Key.KP_Multiply,  "kMultiply" },
			{ Gdk.Key.KP_Divide,    "kDivide"   },
			{ Gdk.Key.KP_Enter,     "kEnter"    },
			{ Gdk.Key.KP_Decimal,   "kPoint"    },
			{ Gdk.Key.KP_0,         "k0"        },
			{ Gdk.Key.KP_1,         "k1"        },
			{ Gdk.Key.KP_2,         "k2"        },
			{ Gdk.Key.KP_3,         "k3"        },
			{ Gdk.Key.KP_4,         "k4"        },
			{ Gdk.Key.KP_5,         "k5"        },
			{ Gdk.Key.KP_6,         "k6"        },
			{ Gdk.Key.KP_7,         "k7"        },
			{ Gdk.Key.KP_8,         "k8"        },
			{ Gdk.Key.KP_9,         "k9"        },
			{ Gdk.Key.Help,         "Help"      },
			{ Gdk.Key.Undo,         "Undo"      },
		};
		
		static char GetChar (string charName)
		{
			if (charName.Length == 1)
				return charName[0];
			
			char c;
			if (stringCharMaps.TryGetValue (charName, out c))
				return c;
			
			//FIXME this should be environment/editor-dependent
			if (charName == "EOL")
				return '\n';
			
			throw new FormatException ("Unknown char '" + charName +"'");
		}
		
		static Dictionary<string, char> stringCharMaps = new Dictionary<string, char> () {
			{ "Nul",     '\0' },
			{ "Space",   ' '  },
			{ "CR",      '\r' },
			{ "NL",      '\n' },
			{ "FF",      '\f' },
			{ "Tab",     '\t' },
			{ "lt",      '<'  },
			{ "Bslash",  '\\' },
			{ "Bar",     '|'  },
		};
		
		static Gdk.Key GetKey (string code)
		{
			Gdk.Key k;
			if (stringKeyMaps.TryGetValue (code, out k))
				return k;
			return (Gdk.Key)0;
		}
		
		static Dictionary<string, Gdk.Key> stringKeyMaps = new Dictionary<string, Gdk.Key> () {
			{ "BS",        Gdk.Key.BackSpace    },
			{ "Tab",       Gdk.Key.Tab          },
			{ "CR",        Gdk.Key.Return       },
			{ "Return",    Gdk.Key.Return       },
			{ "Enter",     Gdk.Key.Return       },
			{ "Esc",       Gdk.Key.Escape       },
			{ "Space",     Gdk.Key.space        },
			{ "Up",        Gdk.Key.Up           },
			{ "Down",      Gdk.Key.Down         },
			{ "Left",      Gdk.Key.Left         },
			{ "Right",     Gdk.Key.Right        },
			{ "#1",        Gdk.Key.F1           },
			{ "F1",        Gdk.Key.F1           },
			{ "#2",        Gdk.Key.F2           },
			{ "F2",        Gdk.Key.F2           },
			{ "#3",        Gdk.Key.F3           },
			{ "F3",        Gdk.Key.F3           },
			{ "#4",        Gdk.Key.F4           },
			{ "F4",        Gdk.Key.F4           },
			{ "#5",        Gdk.Key.F5           },
			{ "F5",        Gdk.Key.F5           },
			{ "#6",        Gdk.Key.F6           },
			{ "F6",        Gdk.Key.F6           },
			{ "#7",        Gdk.Key.F7           },
			{ "F7",        Gdk.Key.F7           },
			{ "#8",        Gdk.Key.F8           },
			{ "F8",        Gdk.Key.F8           },
			{ "#9",        Gdk.Key.F9           },
			{ "F9",        Gdk.Key.F9           },
			{ "#0",        Gdk.Key.F10          },
			{ "F10",       Gdk.Key.F10          },
			{ "F11",       Gdk.Key.F11          },
			{ "F12",       Gdk.Key.F12          },
			{ "Insert",    Gdk.Key.Insert       },
			{ "Del",       Gdk.Key.Delete       },
			{ "Home",      Gdk.Key.Home         },
			{ "End",       Gdk.Key.End          },
			{ "PageUp",    Gdk.Key.Page_Up      },
			{ "PageDown",  Gdk.Key.Page_Down    },
			{ "kHome",     Gdk.Key.KP_Home      },
			{ "kEnd",      Gdk.Key.KP_End       },
			{ "kPageUp",   Gdk.Key.KP_Page_Up   },
			{ "kPageDown", Gdk.Key.KP_Page_Down },
			{ "kPlus",     Gdk.Key.KP_Add       },
			{ "kMinus",    Gdk.Key.KP_Subtract  },
			{ "kMultiply", Gdk.Key.KP_Multiply  },
			{ "kDivide",   Gdk.Key.KP_Divide    },
			{ "kEnter",    Gdk.Key.KP_Enter     },
			{ "kPoint",    Gdk.Key.KP_Decimal   },
			{ "k0",        Gdk.Key.KP_0         },
			{ "k1",        Gdk.Key.KP_1         },
			{ "k2",        Gdk.Key.KP_2         },
			{ "k3",        Gdk.Key.KP_3         },
			{ "k4",        Gdk.Key.KP_4         },
			{ "k5",        Gdk.Key.KP_5         },
			{ "k6",        Gdk.Key.KP_6         },
			{ "k7",        Gdk.Key.KP_7         },
			{ "k8",        Gdk.Key.KP_8         },
			{ "k9",        Gdk.Key.KP_9         },
			{ "Help",      Gdk.Key.Help         },
			{ "Undo",      Gdk.Key.Undo         },
		};
		
		static ViKey FlattenControlMappings (ViKey k)
		{
			ViKey ret;
			if ((k.Modifiers & ModifierType.ControlMask) == k.Modifiers &&
			    controlMappings.TryGetValue (k.Char, out ret))
					return ret;
			return k;
		}
		
		static Dictionary<uint, ViKey> controlMappings = new Dictionary<uint, ViKey> () {
			{ '@', '\0'          },
			{ 'h', Key.BackSpace },
			{ 'i', '\t'          },
			{ 'j', '\n'          },
			{ 'l', '\f'          },
			{ 'm', '\r'          },
			{ '[', Key.Escape    },
			{ 'p', Key.Up        },
		};

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
					list.Add (command[i]);
				}
			}
			return list;
		}
		
		//TODO: <CSI> <xCSI>
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
			
			var key = c == '\0'? new ViKey (modifiers, k) : new ViKey (modifiers, c);
			if (!IsValid (key))
				throw new FormatException ("Invalid key sequence '" + seq + "'");
			return key;
		}
	}
}

