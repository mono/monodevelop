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

namespace MonoDevelop.Components.Commands
{
	public delegate void KeyBindingChangedEventHandler (object s, KeyBindingChangedEventArgs args);
	
	public abstract class Command
	{
		public static readonly object Separator = new Object ();
		
		object id;
		string text;
		string description;
		string icon;
		string accelKey;
		string category;
		bool disabledVisible;
		internal string AccelPath;
		internal object HandlerData; // Used internally when dispatching the command
		
		public Command ()
		{
		}
		
		public Command (object id, string text)
		{
			this.id = id;
			this.text = text;
		}
		
		public object Id {
			get { return id; }
			set { id = value; }
		}
		
		public string Text {
			get { return text; }
			set { text = value; }
		}
		
		public string Icon {
			get { return icon; }
			set { icon = value; }
		}
		
		public string AccelKey {
			get { return accelKey; }
			set {
				string binding = accelKey;
				accelKey = value == String.Empty ? null : value;
				
				if (KeyBindingChanged != null && accelKey != binding)
					KeyBindingChanged (this, new KeyBindingChangedEventArgs (this, binding));
			}
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
	}
	
	public class KeyBindingChangedEventArgs {
		Command command;
		string binding;
		
		public KeyBindingChangedEventArgs (Command command, string oldBinding)
		{
			this.command = command;
			this.binding = oldBinding;
		}
		
		public Command Command {
			get { return command; }
		}
		
		public string OldKeyBinding {
			get { return binding; }
		}
		
		public string NewKeyBinding {
			get { return command.AccelKey; }
		}
	}
}

