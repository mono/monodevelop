//
// KeyBindingManager.cs
//
// Author: Jeffrey Stedfast <fejj@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;

// Terminology:
//   mode: A 'mode' is a key binding prefix / modifier meant to allow a
//         wider set of key bindings to exist. If you are familiar with
//         GNU/Emacs, you might be familiar with the key binding for
//         'Save'. This key binding would have a 'mode' of 'Control+x'
//         and an accel of 'Control+s' - the full binding being:
//         'Control+x Control+s'.
//
//   accel: A (possibly partial) key binding for some command/action.
//          When combined with the mode prefix, results in the full key
//          binding.
//
//   binding: The full key binding for a command/action.
//

namespace MonoDevelop.Components.Commands {
	public class KeyBindingManager {
		Dictionary<string, List<Command>> bindings;
		Dictionary<string, int> modes;
		List<Command> commands;
		
#region Constructors
		public KeyBindingManager ()
		{
			bindings = new Dictionary<string, List<Command>> ();
			modes = new Dictionary<string, int> ();
			commands = new List<Command> ();
		}
		
		~KeyBindingManager ()
		{
			for (int i = 0; i < commands.Count; i++)
				commands[i].KeyBindingChanged -= OnKeyBindingChanged;
		}
#endregion
		
#region Static Public Helpers For Building Key-Binding Strings
		static string KeyToString (Gdk.Key key)
		{
			char c;
			
			if ((c = (char) Gdk.Keyval.ToUnicode ((uint) key)) != 0) {
				if (c == ' ')
					return "Space";
				
				return Char.ToUpper (c).ToString ();
			}
			
			//HACK: Page_Down and Next are synonyms for the same enum value, but alphabetically, Next gets returned 
			// first by enum ToString(). Similarly, Page_Up and Prior are synonyms, but Page_Up is returned. Hence the 
			// default pairing is Next and Page_Up, which is confusingly inconsistent, so we fix this here.
			if (key == Gdk.Key.Next)
				return "Page_Down";
			
			return key.ToString ();
		}
		
		public static string AccelFromKey (Gdk.Key key, Gdk.ModifierType modifier)
		{
			string accel = String.Empty;
			
			switch (key) {
			case Gdk.Key.Alt_L:
				// incomplete accel
				return null;
			case Gdk.Key.Alt_R:
				// incomplete accel
				return null;
			case Gdk.Key.Meta_L:
				// incomplete accel
				return null;
			case Gdk.Key.Meta_R:
				// incomplete accel
				return null;
			case Gdk.Key.Shift_L:
				// incomplete accel
				return null;
			case Gdk.Key.Shift_R:
				// incomplete accel
				return null;
			case Gdk.Key.Control_L:
				// incomplete accel
				return null;
			case Gdk.Key.Control_R:
				// incomplete accel
				return null;
			}
			
			if ((modifier & Gdk.ModifierType.ControlMask) != 0)
				accel += "Control+";
			if ((modifier & Gdk.ModifierType.Mod1Mask) != 0)
				accel += "Alt+";
			if ((modifier & Gdk.ModifierType.ShiftMask) != 0)
				accel += "Shift+";
			
			accel += KeyToString (key);
			
			return accel;
		}
		
		static bool AccelToKey (string accel, ref int i, out uint key, out Gdk.ModifierType modifier)
		{
			Gdk.ModifierType mod, mask = 0;
			string str;
			uint k = 0;
			int j = i;
			
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			if (accel == null || i >= accel.Length)
				return false;
			
			while (i < accel.Length) {
				for (j = i + 1; j < accel.Length; j++) {
					if (accel[j] == '+' || accel[j] == '|')
						break;
				}
				
				str = accel.Substring (i, j - i);
				if ((mod = ModifierMask (str)) == Gdk.ModifierType.None) {
					if (str == "Space")
						k = (uint) Gdk.Key.space;
					else if (str.Length > 1)
						k = Gdk.Keyval.FromName (str);
					else
						k = (uint) str[0];
					
					break;
				}
				
				mask |= mod;
				i = j + 1;
			}
			
			i = j;
			
			if (k == 0)
				return false;
			
			modifier = mask;
			key = k;
			
			return true;
		}
		
		public static bool AccelToKey (string accel, out uint key, out Gdk.ModifierType modifier)
		{
			int i = 0;
			
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			if (accel == null || accel == String.Empty)
				return false;
			
			if (AccelToKey (accel, ref i, out key, out modifier) && i == accel.Length)
				return true;
			
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			return false;
		}
		
		public static bool BindingToKeys (string binding, out uint modeKey, out Gdk.ModifierType modeModifier, out uint key, out Gdk.ModifierType modifier)
		{
			int i = 0;
			
			modeModifier = Gdk.ModifierType.None;
			modeKey = 0;
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			if (binding == null || binding == String.Empty)
				return false;
			
			if (!AccelToKey (binding, ref i, out key, out modifier))
				return false;
			
			if (i == binding.Length) {
				// no mode in this binding, we're done
				return true;
			}
			
			if (binding[i] != '|') {
				// bad format
				return false;
			}
			
			modeModifier = modifier;
			modeKey = key;
			i++;
			
			return AccelToKey (binding, ref i, out key, out modifier) && i == binding.Length;
		}
		
		public static string BindingFromKey (string mode, Gdk.Key key, Gdk.ModifierType modifier)
		{
			return Binding (mode, AccelFromKey (key, modifier));
		}
		
		public static string Binding (string mode, string accel)
		{
			if (mode == null) {
				if (accel == String.Empty)
					return null;
				
				return accel;
			}
			
			if (accel == String.Empty)
				return mode;
			
			return mode + "|" + accel;
		}
		
		static string ModifierToLabel (Gdk.ModifierType mod)
		{
			string label = String.Empty;
			
			if ((mod & Gdk.ModifierType.ControlMask) != 0)
				label += "Ctrl+";
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0)
				label += "Alt+";
			if ((mod & Gdk.ModifierType.ShiftMask) != 0)
				label += "Shift+";
			
			return label;
		}
		
		public static string BindingToLabel (string binding)
		{
			Gdk.ModifierType modeMod, mod;
			string label = String.Empty;
			uint modeKey, key;
			
			if (!BindingToKeys (binding, out modeKey, out modeMod, out key, out mod))
				return null;
			
			if (modeKey != 0)
				label += ModifierToLabel (modeMod) + KeyToString ((Gdk.Key) modeKey) + " ";
			
			label += ModifierToLabel (mod) + KeyToString ((Gdk.Key) key);
			
			return label;
		}
		
		static Gdk.ModifierType ModifierMask (string name)
		{
			switch (name) {
			case "Alt":
				return Gdk.ModifierType.Mod1Mask;
			case "Shift":
				return Gdk.ModifierType.ShiftMask;
			case "Control":
				return Gdk.ModifierType.ControlMask;
			default:
				return Gdk.ModifierType.None;
			}
		}
		
		///
		/// Converts the old shortcut format into the new "key binding" format
		///
		static string ShortcutToBinding (string shortcut)
		{
			Gdk.ModifierType mod, mask = 0;
			int i = 0, j;
			Gdk.Key key;
			string str;
			
			if (shortcut == null || shortcut == String.Empty)
				return null;
			
			while (i < shortcut.Length) {
				if ((j = shortcut.IndexOf ('|', i + 1)) == -1) {
					// we are at the last part of the shortcut, the Gdk.Key
					j = shortcut.Length;
				}
				
				str = shortcut.Substring (i, j - i);
				if ((mod = ModifierMask (str)) == Gdk.ModifierType.None) {
					if (str.Length > 1)
						key = (Gdk.Key) Gdk.Key.Parse (typeof (Gdk.Key), str);
					else
						key = (Gdk.Key) str[0];
					
					if (j < shortcut.Length)
						Console.WriteLine ("WARNING: trailing data after Gdk.Key portion of shortcut {0}", shortcut);
					
					return AccelFromKey (key, mask);
				}
				
				mask |= mod;
				i = j + 1;
			}
			
			Console.WriteLine ("WARNING: Incomplete shortcut '{0}'?", shortcut);
			
			return null;
		}
		
		///
		/// Returns true if @shortcut is in the old string format or false if it is the newer string format.
		///
		static bool IsShortcutFormat (string shortcut)
		{
			int i = 0;
			
			if (shortcut == null || shortcut == String.Empty)
				return false;
			
			if (shortcut.Length == 1 && (shortcut[0] == '|' || shortcut[0] == '+')) {
				// single-key binding
				return false;
			}
			
			while (i < shortcut.Length && (shortcut[i] != '|' && shortcut[i] != '+'))
				i++;
			
			return i < shortcut.Length && shortcut[i] == '|' && ModifierMask (shortcut.Substring (0, i)) != Gdk.ModifierType.None;
		}
		
		public static string CanonicalizeBinding (string binding)
		{
			Gdk.ModifierType modeMod, mod;
			string accel, mode = null;
			uint modeKey, key;
			
			if (binding == null || binding == String.Empty)
				return null;
			
			if (IsShortcutFormat (binding))
				return ShortcutToBinding (binding);
			
			if (!BindingToKeys (binding, out modeKey, out modeMod, out key, out mod)) {
				Console.WriteLine ("WARNING: failed to canonicalize binding {0}", binding);
				return null;
			}
			
			if (modeKey != 0)
				mode = AccelFromKey ((Gdk.Key) modeKey, modeMod);
			
			accel = AccelFromKey ((Gdk.Key) key, mod);
			
			return Binding (mode, accel);
		}
#endregion
		
#region Public Methods
		public bool BindingExists (string binding)
		{
			return bindings.ContainsKey (binding);
		}
		
		public bool BindingExists (string mode, string accel)
		{
			return BindingExists (Binding (mode, accel));
		}
		
		public bool ModeExists (string mode)
		{
			return modes.ContainsKey (mode);
		}
		
		string BindingMode (string binding)
		{
			bool plus = false;
			int i;
			
			if (binding == null)
				return null;
			
			for (i = 0; i < binding.Length; i++) {
				if (!plus && binding[i] == '|')
					return binding.Substring (0, i);
				
				if (binding[i] == '+')
					plus = !plus;
				else
					plus = false;
			}
			
			return null;
		}
		
		void SetBinding (Command command, string oldMode, string oldBinding, string newMode, string newBinding)
		{
			List<Command> list;
			int refs;
			
			if (newMode != oldMode) {
				// keep track of valid modes
				if (oldMode != null) {
					if ((refs = modes[oldMode] - 1) == 0)
						modes.Remove (oldMode);
					else
						modes[oldMode] = refs;
				}
				
				if (newMode != null) {
					if (modes.ContainsKey (newMode)) {
						modes[newMode]++;
					} else
						modes.Add (newMode, 1);
				}
			}
			
			if (oldBinding != null) {
				list = bindings[oldBinding];
				list.Remove (command);
				
				if (list.Count == 0)
					bindings.Remove (oldBinding);
			}
			
			if (newBinding != null) {
				if (!bindings.ContainsKey (newBinding)) {
					list = new List<Command> ();
					list.Add (command);
					
					bindings.Add (newBinding, list);
				} else {
					list = bindings[newBinding];
					list.Add (command);
				}
			}
		}
		
		void SetBinding (Command command, string oldBinding, string newBinding)
		{
			SetBinding (command, BindingMode (oldBinding), oldBinding, BindingMode (newBinding), newBinding);
		}
		
		void OnKeyBindingChanged (object o, KeyBindingChangedEventArgs args)
		{
			SetBinding (args.Command, args.OldKeyBinding, args.NewKeyBinding);
		}
		
		public bool CommandIsRegistered (Command command)
		{
			return commands.Contains (command);
		}
		
		public void RegisterCommand (Command command)
		{
			if (commands.Contains (command)) {
				Console.WriteLine ("WARNING: trying to re-register command {0}", command);
				return;
			}
			
			SetBinding (command, null, null, BindingMode (command.AccelKey), command.AccelKey);
			command.KeyBindingChanged += new KeyBindingChangedEventHandler (OnKeyBindingChanged);
			commands.Add (command);
		}
		
		public void UnregisterCommand (Command command)
		{
			List<Command> list;
			string mode;
			int refs;
			
			if (!commands.Contains (command)) {
				Console.WriteLine ("WARNING: trying to unregister unknown command {0}", command);
				return;
			}
			
			command.KeyBindingChanged -= OnKeyBindingChanged;
			commands.Remove (command);
			
			if (string.IsNullOrEmpty (command.AccelKey))
				return;

			list = bindings[command.AccelKey];
			list.Remove (command);
			if (list.Count == 0)
				bindings.Remove (command.AccelKey);
			
			mode = BindingMode (command.AccelKey);
			if (mode != null && modes.ContainsKey (mode)) {
				if ((refs = modes[mode] - 1) == 0)
					modes.Remove (mode);
				else
					modes[mode] = refs;
			}
		}
		
		///
		/// Returns the oldest registered command with the specified key binding.
		///
		public Command Command (string binding)
		{
			List<Command> list;
			
			if (!BindingExists (binding))
				return null;
			
			list = bindings[binding];
			
			return list[0];
		}
		
		///
		/// Returns the list of commands registered for the specified key binding.
		///
		public List<Command> Commands (string binding)
		{
			if (!BindingExists (binding))
				return null;
			
			return bindings[binding];
		}
#endregion
	}
	
//#region KeyBindingException
//	public class KeyBindingConflictException : Exception {
//		Command command;
//		string binding;
//		
//		public KeyBindingConflictException (Command command, string binding)
//			: base (String.Format ("An existing command, {0}, is already bound to {1}",
//			                       command, binding))
//		{
//			this.command = command;
//			this.binding = binding;
//		}
//		
//		public Command Command {
//			get { return command; }
//		}
//		
//		public string Binding {
//			get { return binding; }
//		}
//	}
//#endregion
}
