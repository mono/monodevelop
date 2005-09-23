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

namespace MonoDevelop.Commands
{
	public class ActionCommand: Command
	{
		ActionType type;
		bool commandArray;
		Type defaultHandlerType;
		CommandHandler defaultHandler;
		
		public ActionCommand ()
		{
		}
		
		public ActionCommand (object id, string text, string icon): base (id, text)
		{
			Icon = icon;
		}
		
		public ActionCommand (object id, string text, string icon, string accelKey, ActionType type): base (id, text)
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
			get { return defaultHandlerType; }
			set {
				if (!typeof (CommandHandler).IsAssignableFrom (value))
					throw new ArgumentException ("Value must be a subclass of CommandHandler");

				defaultHandlerType = value;
			}
		}

		public virtual bool DispatchCommand (object dataItem)
		{
			if (defaultHandlerType == null)
				return false;
			
			if (defaultHandler == null)
				defaultHandler = (CommandHandler) Activator.CreateInstance (defaultHandlerType);
			
			defaultHandler.Run (dataItem);
			return true;
		}

		public virtual void UpdateCommandInfo (CommandInfo info)
		{
			if (defaultHandlerType == null) {
				info.Enabled = false;
				if (!DisabledVisible)
					info.Visible = false;
			} else {
				if (defaultHandler == null)
					defaultHandler = (CommandHandler) Activator.CreateInstance (defaultHandlerType);
					
				if (commandArray) {
					info.ArrayInfo = new CommandArrayInfo (info);
					defaultHandler.Update (info.ArrayInfo);
				}
				else
					defaultHandler.Update (info);
			}
		}
	}
}

