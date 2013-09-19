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

using Mono.TextEditor;

// Terminology:
//   chord: A 'chord' is a key binding prefix / modifier meant to allow a
//          wider set of key bindings to exist. If you are familiar with
//          GNU/Emacs, you might be familiar with the key binding for
//          'Save'. This key binding would have a 'chord' of 'Control+x'
//          and an accel of 'Control+s' - the full binding being:
//          'Control+x Control+s'.
//
//   accel: A (possibly partial) key binding for some command/action.
//          When combined with the mode prefix, results in the full key
//          binding.
//
//   binding: The full key binding for a command/action.
//

namespace MonoDevelop.Components.Commands
{
	public class KeyBindingManager : IDisposable
	{
		Dictionary<KeyBinding, List<Command>> bindings = new Dictionary<KeyBinding, List<Command>> ();
		Dictionary<KeyboardShortcut, int> chords = new Dictionary<KeyboardShortcut, int> ();
		List<Command> commands = new List<Command> ();
		
		static KeyBindingManager ()
		{
			if (isMac) {
				SelectionModifierAlt = Gdk.ModifierType.Mod5Mask;
				SelectionModifierControl = Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.MetaMask;
				SelectionModifierSuper = Gdk.ModifierType.ControlMask;
			} else {
				SelectionModifierAlt = Gdk.ModifierType.Mod1Mask;
				SelectionModifierControl = Gdk.ModifierType.ControlMask;
				SelectionModifierSuper = Gdk.ModifierType.SuperMask;
			}
		}
		
		static bool isMac {
			get { return MonoDevelop.Core.Platform.IsMac; }
		}
		
		public void Dispose ()
		{
			if (commands == null)
				return;
			
			for (int i = 0; i < commands.Count; i++)
				commands[i].KeyBindingChanged -= OnKeyBindingChanged;
			commands = null;
		}
		
		#region Platform-dependent selection modifiers
		//FIXME: these should be named for what they do, not what they are
		
		public static Gdk.ModifierType SelectionModifierAlt {
			get; private set;
		}
		
		public static Gdk.ModifierType SelectionModifierControl {
			get; private set;
		}
		
		public static Gdk.ModifierType SelectionModifierSuper {
			get; private set;
		}
		
		#endregion
		
		#region Static Public Helpers For Building Key-Binding Strings
		
		static bool KeyIsModifier (Gdk.Key key)
		{
			if (key.Equals (Gdk.Key.Control_L) || key.Equals (Gdk.Key.Control_R))
				return true;
			else if (key.Equals (Gdk.Key.Alt_L) || key.Equals (Gdk.Key.Alt_R))
				return true;
			else if (key.Equals (Gdk.Key.Shift_L) || key.Equals (Gdk.Key.Shift_R))
				return true;
			else if (key.Equals (Gdk.Key.Meta_L) || key.Equals (Gdk.Key.Meta_R))
				return true;
			else if (key.Equals (Gdk.Key.Super_L) || key.Equals (Gdk.Key.Super_R))
				return true;
			
			return false;
		}
		
		static string ModifierToString (Gdk.ModifierType mod)
		{
			string label = string.Empty;
			
			if ((mod & Gdk.ModifierType.ControlMask) != 0)
				label += "Control+";
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0)
				label += "Alt+";
			if ((mod & Gdk.ModifierType.ShiftMask) != 0)
				label += "Shift+";
			if ((mod & Gdk.ModifierType.MetaMask) != 0)
				label += "Meta+";
			if ((mod & Gdk.ModifierType.SuperMask) != 0)
				label += "Super+";
			
			return label;
		}
		
		static string ModifierKeyToString (Gdk.Key key)
		{
			string label = string.Empty;
			
			if (key.Equals (Gdk.Key.Control_L) || key.Equals (Gdk.Key.Control_R))
				label += "Control+";
			else if (key.Equals (Gdk.Key.Alt_L) || key.Equals (Gdk.Key.Alt_R))
				label += "Alt+";
			else if (key.Equals (Gdk.Key.Shift_L) || key.Equals (Gdk.Key.Shift_R))
				label += "Shift+";
			else if (key.Equals (Gdk.Key.Meta_L) || key.Equals (Gdk.Key.Meta_R))
				label += "Meta+";
			else if (key.Equals (Gdk.Key.Super_L) || key.Equals (Gdk.Key.Super_R))
				label += "Super+";
			
			return label;
		}
		
		static string KeyToString (Gdk.Key key)
		{
			char c = (char) Gdk.Keyval.ToUnicode ((uint) key);
			
			if (c != 0) {
				if (c == ' ')
					return "Space";
				
				return Char.ToUpper (c).ToString ();
			}
			
			//HACK: Page_Down and Next are synonyms for the same enum value, but alphabetically, Next gets returned 
			// first by enum ToString(). Similarly, Page_Up and Prior are synonyms, but Page_Up is returned. Hence the 
			// default pairing is Next and Page_Up, which is confusingly inconsistent, so we fix this here.
			//
			//The same problem applies to some other common keys, so we ensure certain values are mapped
			// to the most common name.
			switch (key) {
			case Gdk.Key.Next:
				return "Page_Down";
			case Gdk.Key.L1:
				return "F11";
			case Gdk.Key.L2:
				return "F12";
			}
			
			return key.ToString ();
		}
		
		// This version is used by the CommandManager to track the keystrokes the user has hit for binding lookups.
		public static KeyboardShortcut[] AccelsFromKey (Gdk.EventKey raw, out bool complete)
		{
			KeyboardShortcut[] shortcuts;
			Gdk.ModifierType modifier;
			Gdk.Key key;
			
			GtkWorkarounds.MapKeys (raw, out key, out modifier, out shortcuts);
			complete = !KeyIsModifier (key);
			
			return shortcuts;
		}
		
		// This version is used by the KeyBindingsPanel to display the keystrokes that the user has hit.
		public static string AccelLabelFromKey (Gdk.EventKey raw, out bool complete)
		{
			KeyboardShortcut[] shortcuts;
			Gdk.ModifierType modifier;
			Gdk.Key key;
			
			GtkWorkarounds.MapKeys (raw, out key, out modifier, out shortcuts);
			bool keyIsModifier = KeyIsModifier (key);
			complete = !keyIsModifier;
			
			// The first shortcut is the fully decomposed version
			string accel = ModifierToString (shortcuts[0].Modifier);
			if (keyIsModifier)
				return accel + ModifierKeyToString (shortcuts[0].Key);
			
			return accel + KeyToString (shortcuts[0].Key);
		}
		
		public static string AccelLabelFromKey (Gdk.EventKey raw)
		{
			bool complete;
			
			return AccelLabelFromKey (raw, out complete);
		}
		
		static string AccelLabelFromKey (Gdk.Key key, Gdk.ModifierType modifier, out bool complete)
		{
			string accel = ModifierToString (modifier);
			
			if ((complete = !KeyIsModifier (key)))
				return accel + KeyToString (key);
			
			return accel + ModifierKeyToString (key);
		}
		
		/// <summary>
		/// Used for canonicalizing bindings after parsing them
		/// </summary>
		internal static string AccelLabelFromKey (Gdk.Key key, Gdk.ModifierType modifier)
		{
			bool complete;
			
			return AccelLabelFromKey (key, modifier, out complete);
		}
		
		static bool AccelToKeyPartial (string accel, ref int i, out uint key, out Gdk.ModifierType modifier)
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
					else if (str[0] >= 'A' && str[0] <= 'Z')
						k = (uint) str[0] + 32;
					else
						k = (uint) str[0];
					
					break;
				}
				
				mask |= mod;
				i = j + 1;
			}
			
			i = j;
			
			modifier = mask;
			key = k;
			
			return k != 0;
		}
		
		static bool AccelToKeyPartial (string accel, out uint key, out Gdk.ModifierType modifier)
		{
			int i = 0;
			
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			if (string.IsNullOrEmpty (accel))
				return false;
			
			if (AccelToKeyPartial (accel, ref i, out key, out modifier) && i == accel.Length)
				return true;
			
			return false;
		}
		
		public static bool AccelToKey (string accel, out uint key, out Gdk.ModifierType modifier)
		{
			if (AccelToKeyPartial (accel, out key, out modifier))
				return true;
			
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			return false;
		}
		
		static bool BindingToKeysPartial (string binding, out uint chordKey, out Gdk.ModifierType chordModifier, out uint key, out Gdk.ModifierType modifier)
		{
			int i = 0;
			
			chordModifier = Gdk.ModifierType.None;
			chordKey = 0;
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			if (string.IsNullOrEmpty (binding))
				return false;
			
			if (!AccelToKeyPartial (binding, ref i, out key, out modifier))
				return false;
			
			if (i == binding.Length) {
				// no mode in this binding, we're done
				return true;
			}
			
			if (binding[i] != '|') {
				// bad format
				return false;
			}
			
			chordModifier = modifier;
			chordKey = key;
			i++;
			
			return AccelToKeyPartial (binding, ref i, out key, out modifier) && i == binding.Length;
		}
		
		public static bool BindingToKeys (string binding, out uint chordKey, out Gdk.ModifierType chordModifier, out uint key, out Gdk.ModifierType modifier)
		{
			if (BindingToKeysPartial (binding, out chordKey, out chordModifier, out key, out modifier))
				return true;
			
			chordModifier = modifier = Gdk.ModifierType.None;
			chordKey = key = 0;
			return false;
		}
				
		public static string Binding (string chord, string accel)
		{
			if (string.IsNullOrEmpty (chord)) {
				if (string.IsNullOrEmpty (accel))
					return null;
				
				return accel;
			}
			
			if (string.IsNullOrEmpty (accel))
				return chord;
			
			return chord + "|" + accel;
		}
		
		#endregion
		
		#region Display label formatting
		
		public static string BindingToDisplayLabel (string binding, bool concise)
		{
			return BindingToDisplayLabel (binding, concise, false);
		}
		
		public static string BindingToDisplayLabel (string binding, bool concise, bool includeIncomplete)
		{
			Gdk.ModifierType chordMod, mod;
			string label = string.Empty;
			uint chordKey, key;
			
			if (includeIncomplete) {
				BindingToKeysPartial (binding, out chordKey, out chordMod, out key, out mod);
			} else if (!BindingToKeys (binding, out chordKey, out chordMod, out key, out mod)) {
				return null;
			}
			
			if (chordKey != 0)
				label += ModifierToDisplayLabel (chordMod, concise) + KeyToDisplayLabel ((Gdk.Key) chordKey) + (isMac ? " " : "|");
			
			label += ModifierToDisplayLabel (mod, concise);
			if (key != 0)
				label += KeyToDisplayLabel ((Gdk.Key) key);
			
			return label;
		}
		
		public static string BindingToDisplayLabel (KeyBinding binding, bool concise)
		{
			string label;
			
			if (!binding.Chord.IsEmpty)
				label = ModifierToDisplayLabel (binding.Chord.Modifier, concise) + KeyToDisplayLabel (binding.Chord.Key) + (isMac ? " " : "|");
			else
				label = string.Empty;
			
			return label + ModifierToDisplayLabel (binding.Accel.Modifier, concise) + KeyToDisplayLabel (binding.Accel.Key);
		}
		
		static string KeyToDisplayLabel (Gdk.Key key)
		{
			if (isMac) {
				char appl = AppleMapKeyToSymbol (key);
				if (appl != '\0') {
					return new string (appl, 1);
				}
			}
			
			switch (key) {
			case Gdk.Key.Page_Down:
			//Gdk.Key.Next:
				return "Page Down";
			case Gdk.Key.Page_Up:
			//case Gdk.Key.Prior:
				return "Page Up";
			}
			
			return KeyToString (key);
		}
		
		static string ModifierToDisplayLabel (Gdk.ModifierType mod, bool concise)
		{
			if (isMac)
				return AppleMapModifierToSymbols (mod);
			
			string label = string.Empty;
			
			if ((mod & Gdk.ModifierType.ControlMask) != 0)
				label += concise? "Ctrl+" : "Control+";
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0)
				label += "Alt+";
			if ((mod & Gdk.ModifierType.ShiftMask) != 0)
				label += "Shift+";
			if ((mod & Gdk.ModifierType.MetaMask) != 0)
				label += "Meta+";
			if ((mod & Gdk.ModifierType.SuperMask) != 0)
				label += "Super+";
			
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
			case "Ctrl":
				return Gdk.ModifierType.ControlMask;
			case "Meta":
				return Gdk.ModifierType.MetaMask;
			case "Super":
				return Gdk.ModifierType.SuperMask;
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
			
			if (string.IsNullOrEmpty (shortcut))
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
					else if (str[0] >= 'a' && str[0] <= 'z')
						key = (Gdk.Key) str[0] - 32;
					else
						key = (Gdk.Key) str[0];
					
					if (j < shortcut.Length)
						Console.WriteLine ("WARNING: trailing data after Gdk.Key portion of shortcut {0}", shortcut);
					
					return AccelLabelFromKey (key, mask);
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
			
			if (string.IsNullOrEmpty (shortcut))
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
			Gdk.ModifierType chordMod, mod;
			string accel, chord = null;
			uint chordKey, key;
			
			if (string.IsNullOrEmpty (binding))
				return null;
			
			if (IsShortcutFormat (binding))
				return ShortcutToBinding (binding);
			
			if (!BindingToKeys (binding, out chordKey, out chordMod, out key, out mod)) {
				Console.WriteLine ("WARNING: failed to canonicalize binding {0}", binding);
				return null;
			}
			
			if (chordKey != 0)
				chord = AccelLabelFromKey ((Gdk.Key) chordKey, chordMod);
			
			accel = AccelLabelFromKey ((Gdk.Key) key, mod);
			
			return Binding (chord, accel);
		}
		
		#endregion
		
		#region Public Methods
		
		public bool BindingExists (KeyBinding binding)
		{
			return bindings.ContainsKey (binding);
		}
		
		public bool ChordExists (KeyboardShortcut chord)
		{
			return chords.ContainsKey (chord);
		}
		
		public bool AnyChordExists (KeyboardShortcut[] chords)
		{
			foreach (var chord in chords) {
				if (ChordExists (chord))
					return true;
			}
			
			return false;
		}
		
		static bool ChordChanged (KeyBinding old, KeyBinding @new)
		{
			if (old == null && @new == null)
				return false;
			
			if (old == null || @new == null)
				return true;
			
			return !old.Chord.Equals (@new.Chord);
		}
		
		void SetBinding (Command command, KeyBinding oldKeyBinding, KeyBinding newKeyBinding)
		{
			List<Command> list;
			int refs;
			
			if (ChordChanged (oldKeyBinding, newKeyBinding)) {
				// keep track of valid modes
				if (oldKeyBinding != null && !oldKeyBinding.Chord.IsEmpty) {
					if ((refs = chords[oldKeyBinding.Chord] - 1) == 0)
						chords.Remove (oldKeyBinding.Chord);
					else
						chords[oldKeyBinding.Chord] = refs;
				}
				
				if (newKeyBinding != null && !newKeyBinding.Chord.IsEmpty) {
					if (!chords.ContainsKey (newKeyBinding.Chord))
						chords.Add (newKeyBinding.Chord, 1);
					else
						chords[newKeyBinding.Chord]++;
				}
			}
			
			if (oldKeyBinding != null) {
				list = bindings[oldKeyBinding];
				list.Remove (command);
				
				if (list.Count == 0)
					bindings.Remove (oldKeyBinding);
			}
			
			if (newKeyBinding != null) {
				if (!bindings.ContainsKey (newKeyBinding)) {
					list = new List<Command> ();
					list.Add (command);
					
					bindings.Add (newKeyBinding, list);
				} else {
					list = bindings[newKeyBinding];
					list.Add (command);
				}
			}
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
			
			SetBinding (command, null, command.KeyBinding);
			command.KeyBindingChanged += OnKeyBindingChanged;
			commands.Add (command);
		}
		
		public void UnregisterCommand (Command command)
		{
			List<Command> list;
			int refs;
			
			if (!commands.Contains (command)) {
				Console.WriteLine ("WARNING: trying to unregister unknown command {0}", command);
				return;
			}
			
			command.KeyBindingChanged -= OnKeyBindingChanged;
			commands.Remove (command);
			
			if (command.KeyBinding == null)
				return;

			list = bindings[command.KeyBinding];
			list.Remove (command);
			if (list.Count == 0)
				bindings.Remove (command.KeyBinding);
			
			if (!command.KeyBinding.Chord.IsEmpty && chords.ContainsKey (command.KeyBinding.Chord)) {
				if ((refs = chords[command.KeyBinding.Chord] - 1) == 0)
					chords.Remove (command.KeyBinding.Chord);
				else
					chords[command.KeyBinding.Chord] = refs;
			}
		}
		
		///
		/// Returns the list of commands registered for the specified key binding.
		///
		public List<Command> Commands (KeyBinding binding)
		{
			if (binding == null || !BindingExists (binding))
				return null;
			
			return bindings[binding];
		}
		
		#endregion
		
		#region Apple symbol mappings
		
		static string AppleMapModifierToSymbols (Gdk.ModifierType mod)
		{
			string ret = "";
			
			if ((mod & Gdk.ModifierType.ControlMask) != 0) {
				ret += "⌃";
				mod ^= Gdk.ModifierType.ControlMask;
			}
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0) {
				ret += "⌥";
				mod ^= Gdk.ModifierType.Mod1Mask;
			}
			if ((mod & Gdk.ModifierType.ShiftMask) != 0) {
				ret += "⇧";
				mod ^= Gdk.ModifierType.ShiftMask;
			}
			if ((mod & Gdk.ModifierType.MetaMask) != 0) {
				ret += "⌘";
				mod ^= Gdk.ModifierType.MetaMask;
			}
			if (mod != 0)
				throw new InvalidOperationException ("Unexpected modifiers: " + mod.ToString ());
			return ret;
		}
		
		static char AppleMapKeyToSymbol (Gdk.Key key)
		{
			// unicode codes from http://macbiblioblog.blogspot.com/2005/05/special-key-symbols.html
			// unmapped:
			// ⇤ tab back
			// ⌤ enter
			
			switch (key) {
			case Gdk.Key.Escape:
				return '⎋';
			case Gdk.Key.Tab:
				return '⇥';
			case Gdk.Key.Caps_Lock:
				return '⇪';
			case Gdk.Key.Shift_L:
			case Gdk.Key.Shift_R:
				return '⇧';
			case Gdk.Key.Control_L:
			case Gdk.Key.Control_R:
				return '⌃';
			case Gdk.Key.BackSpace:
				return '⌫';
			case Gdk.Key.Delete:
				return '⌦';
			case Gdk.Key.Home:
				return '⇱';
			case Gdk.Key.End:
				return '⇲';
			case Gdk.Key.Page_Up:
				return '⇞';
			case Gdk.Key.Page_Down:
				return '⇟';
			case Gdk.Key.Up:
				return '↑';
			case Gdk.Key.Down:
				return '↓';
			case Gdk.Key.Left:
				return '←';
			case Gdk.Key.Right:
				return '→';
			case Gdk.Key.Clear:
				return '⌧';
			case Gdk.Key.Num_Lock:
				return '⇭';
			case Gdk.Key.Return:
				return '⏎';
			case Gdk.Key.space:
				return '␣';
			case Gdk.Key.Meta_L:
			case Gdk.Key.Meta_R:
				return '⌘';
			case Gdk.Key.Alt_L:
			case Gdk.Key.Alt_R:
				return '⌥';
			}
			return (char)0;
		}
		
/*		static Gdk.Key AppleMapSymbolToKey (char ch)
		{
			// unicode codes from http://macbiblioblog.blogspot.com/2005/05/special-key-symbols.html
			// unmapped:
			// ⇤ tab back
			// ⌥ option (alt, alternative)
			// ⌘ command
			// ⌤ enter
			
			switch (ch) {
			case '⎋':
				return Gdk.Key.Escape;
			case '⇥':
				return Gdk.Key.Tab;
			case '⇪':
				return Gdk.Key.Caps_Lock;
			case '⇧':
				return Gdk.Key.Shift_L;
			case '⌃':
				return Gdk.Key.Control_L;
			case '⌫':
				return Gdk.Key.BackSpace;
			case '⌦':
				return Gdk.Key.Delete;
			case '⇱':
				return Gdk.Key.Home;
			case '⇲':
				return Gdk.Key.End;
			case '⇞':
				return Gdk.Key.Page_Up;
			case '⇟':
				return Gdk.Key.Page_Down;
			case '↑':
				return Gdk.Key.Up;
			case '↓':
				return Gdk.Key.Down;
			case '←':
				return Gdk.Key.Left;
			case '→':
				return Gdk.Key.Right;
			case '⌧':
				return Gdk.Key.Clear;
			case '⇭':
				return Gdk.Key.Num_Lock;
			case '⏎':
				return Gdk.Key.Return;
			case '␣':
				return Gdk.Key.space;
			case '⌘':
				return Gdk.Key.Meta_L;
			case '⌥':
				return Gdk.Key.Alt_L;
			}
			return (Gdk.Key)0;
		}
		*/
		#endregion
	}
	
	public class KeyBinding : IEquatable<KeyBinding>
	{
		public KeyBinding (KeyboardShortcut chord, KeyboardShortcut accel)
		{
			Chord = chord;
			Accel = accel;
		}
		
		public KeyBinding (KeyboardShortcut accel)
		{
			Chord = KeyboardShortcut.Empty;
			Accel = accel;
		}
		
		public static bool TryParse (string str, out KeyBinding binding)
		{
			Gdk.ModifierType chordModifier;
			Gdk.ModifierType modifier;
			uint chordKey;
			uint key;
			
			if (!KeyBindingManager.BindingToKeys (str, out chordKey, out chordModifier, out key, out modifier)) {
				binding = null;
				return false;
			}
			
			KeyboardShortcut chord = new KeyboardShortcut ((Gdk.Key) chordKey, chordModifier);
			KeyboardShortcut accel = new KeyboardShortcut ((Gdk.Key) key, modifier);
			
			binding = new KeyBinding (chord, accel);
			
			return true;
		}
		
		public KeyboardShortcut Chord {
			get; private set;
		}
		
		public KeyboardShortcut Accel {
			get; private set;
		}
		
		public override int GetHashCode ()
		{
			return Chord.GetHashCode () ^ Accel.GetHashCode ();
		}
		
		public override bool Equals (object obj)
		{
			return obj is KeyBinding && Equals ((KeyBinding) obj);
		}
		
		#region IEquatable[KeyBinding] implementation
		public bool Equals (KeyBinding other)
		{
			return Chord.Equals (other.Chord) && Accel.Equals (other.Accel);
		}
		#endregion
		
		public override string ToString ()
		{
			string chord;
			
			if (!Chord.IsEmpty)
				chord = KeyBindingManager.AccelLabelFromKey (Chord.Key, Chord.Modifier) + "|";
			else
				chord = string.Empty;
			
			return chord + KeyBindingManager.AccelLabelFromKey (Accel.Key, Accel.Modifier);
		}
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
