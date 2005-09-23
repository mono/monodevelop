//
// CommandInfo.cs
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
	public class CommandInfo
	{
		string text;
		string icon;
		string accelKey;
		string description;
		bool enabled = true;
		bool visible = true;
		bool checkd;
		bool useMarkup;
		internal object DataItem; 
		internal CommandArrayInfo ArrayInfo;
		internal bool IsArraySeparator;
		
		internal CommandInfo (Command cmd)
		{
			text = cmd.Text;
			icon = cmd.Icon;
			accelKey = cmd.AccelKey;
			description = cmd.Description;
		}
		
		public CommandInfo (string text)
		{
			Text = text;
		}
		
		public CommandInfo (string text, bool enabled, bool checkd)
		{
			Text = text;
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
		
		public bool UseMarkup {
			get { return useMarkup; }
			set { useMarkup = value; }
		}
	}
}
