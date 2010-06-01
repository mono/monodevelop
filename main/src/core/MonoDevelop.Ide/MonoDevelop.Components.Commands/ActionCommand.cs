//
// ActionCommand.cs
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
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands
{
	public class ActionCommand: Command
	{
		ActionType type;
		bool commandArray;
		Type defaultHandlerType;
		CommandHandler defaultHandler;
		
		RuntimeAddin defaultHandlerAddin;
		string defaultHandlerTypeName;
		
		public ActionCommand ()
		{
		}
		
		public ActionCommand (object id, string text): base (id, text)
		{
		}
		
		public ActionCommand (object id, string text, IconId icon): base (id, text)
		{
			Icon = icon;
		}
		
		public ActionCommand (object id, string text, IconId icon, string accelKey, ActionType type): base (id, text)
		{
			Icon = icon;
			AccelKey = accelKey;
			this.type = type;
		}
		
		public ActionType ActionType {
			get { return type; }
			set { type = value; }
		}
		
		public bool CommandArray {
			get { return commandArray; }
			set { commandArray = value; }
		}

		public Type DefaultHandlerType {
			get {
				if (defaultHandlerType != null)
					return defaultHandlerType;
				if (defaultHandlerAddin != null)
					return defaultHandlerType = defaultHandlerAddin.GetType (defaultHandlerTypeName, true);
				return null;
			}
			set {
				if (!typeof (CommandHandler).IsAssignableFrom (value))
					throw new ArgumentException ("Value must be a subclass of CommandHandler (" + value + ")");

				defaultHandlerType = value;
			}
		}
		
		public void SetDefaultHandlerTypeInfo (RuntimeAddin addin, string typeName)
		{
			defaultHandlerAddin = addin;
			defaultHandlerTypeName = typeName;
		}

		public CommandHandler DefaultHandler {
			get { return defaultHandler; }
			set { defaultHandler = value; }
		}

		internal void UpdateCommandInfo (CommandInfo info)
		{
			if (defaultHandler == null) {
				if (DefaultHandlerType == null) {
					info.Enabled = false;
					if (!DisabledVisible)
						info.Visible = false;
					return;
				}
				defaultHandler = (CommandHandler) Activator.CreateInstance (DefaultHandlerType);
			}
			if (commandArray) {
				info.ArrayInfo = new CommandArrayInfo (info);
				defaultHandler.InternalUpdate (info.ArrayInfo);
			}
			else
				defaultHandler.InternalUpdate (info);
		}
	}
}

