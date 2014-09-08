//
// Command.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands
{
	public delegate void KeyBindingChangedEventHandler (object s, KeyBindingChangedEventArgs args);
	
	public abstract class Command
	{
		public static readonly object Separator = new Object ();
		
		object id;
		string text;
		string description;
		IconId icon;
		string accelKey;
		KeyBinding binding;
		string category;
		bool disabledVisible;
		internal string AccelPath;
		internal object HandlerData; // Used internally when dispatching the command
		
		public Command ()
		{
		}
		
		public Command (object id, string text)
		{
			this.id = CommandManager.ToCommandId (id);
			this.text = text;
		}
		
		public object Id {
			get { return id; }
			set { id = CommandManager.ToCommandId (value); }
		}
		
		public string Text {
			get { return text; }
			set { text = value; }
		}
		
		public IconId Icon {
			get { return icon; }
			set { icon = value; }
		}
		
		public string AccelKey {
			get { return accelKey; }
			set {
				if (string.IsNullOrEmpty (value))
					value = null;
				
				if (value == accelKey)
					return;
				
				var oldKeyBinding = binding;
				
				if (value != null)
					KeyBinding.TryParse (value, out binding);
				else
					binding = null;
				
				accelKey = value;
				
				if (KeyBindingChanged != null)
					KeyBindingChanged (this, new KeyBindingChangedEventArgs (this, oldKeyBinding));
			}
		}

		string[] alternateAccelKeys;
		KeyBinding[] alternateKeyBindings;
		static readonly KeyBinding[] emptyBindings = new KeyBinding[0];

		public string[] AlternateAccelKeys {
			get { return alternateAccelKeys; }
			set { 
				var oldKeybindings = alternateKeyBindings;
				if (value == null || value.Length == 0) {
					alternateKeyBindings = null;
				} else {
					alternateKeyBindings = new KeyBinding[value.Length];
					for (int i = 0; i < value.Length; i++) {
						KeyBinding b;
						KeyBinding.TryParse (value[i], out b);
						alternateKeyBindings [i] = b;
					}
				}
				alternateAccelKeys = value; 
				if (AlternateKeyBindingChanged != null)
					AlternateKeyBindingChanged (this, new AlternateKeyBindingChangedEventArgs (this, oldKeybindings));
			}
		} 

		public KeyBinding KeyBinding {
			get { return binding; }
		}

		public KeyBinding[] AlternateKeyBindings {
			get { return alternateKeyBindings ?? emptyBindings; }
		} 

		
		public bool DisabledVisible {
			get { return disabledVisible; }
			set { disabledVisible = value; }
		}
		
		public string Description {
			get { return description; }
			set { description = value; }
		}
		
		public string Category {
			get { return category == null ? string.Empty : category; }
			set { category = value; }
		}
		
		public event KeyBindingChangedEventHandler KeyBindingChanged;
		public event EventHandler<AlternateKeyBindingChangedEventArgs> AlternateKeyBindingChanged;
	}
	
	public class KeyBindingChangedEventArgs  : EventArgs 
	{
		public KeyBindingChangedEventArgs (Command command, KeyBinding oldKeyBinding)
		{
			OldKeyBinding = oldKeyBinding;
			Command = command;
		}

		public Command Command {
			get; private set;
		}

		public KeyBinding OldKeyBinding {
			get; private set;
		}

		public KeyBinding NewKeyBinding {
			get { return Command.KeyBinding; }
		}
	}

	public class AlternateKeyBindingChangedEventArgs : EventArgs 
	{
		public AlternateKeyBindingChangedEventArgs (Command command, KeyBinding[] oldKeyBinding)
		{
			OldKeyBinding = oldKeyBinding;
			Command = command;
		}

		public Command Command {
			get; private set;
		}

		public KeyBinding[] OldKeyBinding {
			get; private set;
		}

		public KeyBinding[] NewKeyBinding {
			get { return Command.AlternateKeyBindings; }
		}
	}
}

