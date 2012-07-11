//
// CommandInfo.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
	public class CommandInfo
	{
		Command command;
		string text;
		IconId icon;
		string accelKey;
		string description;
		bool enabled = true;
		bool visible = true;
		bool checkd;
		bool useMarkup;
		bool bypass;
		bool checkedInconsistent;
		internal object UpdateHandlerData;
		
		internal CommandInfo (Command cmd)
		{
			text = cmd.Text;
			icon = cmd.Icon;
			accelKey = cmd.AccelKey;
			description = cmd.Description;
			command = cmd;
		}
		
		public CommandInfo ()
		{
		}
		
		public CommandInfo (string text)
		{
			Text = text;
		}
		
		public CommandInfo (string text, bool enabled, bool checkd)
		{
			Text = text;
			this.enabled = enabled;
			this.checkd = checkd;
		}
		
		public Command Command {
			get { return command; }
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
			set { accelKey = value; }
		}
		
		public string Description {
			get { return description; }
			set { description = value; }
		}
		
		public bool Enabled {
			get { return enabled; }
			set { enabled = value; }
		}
		
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}
		
		public bool Checked {
			get { return checkd; }
			set { checkd = value; }
		}
		
		public bool CheckedInconsistent {
			get { return checkedInconsistent; }
			set { checkedInconsistent = value; }
		}
		
		public bool UseMarkup {
			get { return useMarkup; }
			set { useMarkup = value; }
		}
		
		// When set in an update handler, the command manager will ignore this handler method
		// and will keep looking in the command route.
		public bool Bypass {
			get { return bypass; }
			set { bypass = value; }
		}
		
		public CommandArrayInfo ArrayInfo {
			get; internal set;
		}
		
		public object DataItem {
			get; internal set;
		}
		
		public bool IsArraySeparator {
			get; internal set;
		}
		
		public bool HandlesItem (object item)
		{
			return item == DataItem || Object.Equals (item, DataItem);
		}
	}
}
