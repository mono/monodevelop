//
// MonoDevelopActionGroupToolbar.cs
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
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class MonoDevelopActionGroupToolbar: Stetic.ActionGroupToolbar
	{
		public event EventHandler BindField;
		
		public MonoDevelopActionGroupToolbar (bool singleMode): base (singleMode)
		{
		}
		
		public MonoDevelopActionGroupToolbar (Stetic.Wrapper.ActionGroupCollection actionGroups): base (actionGroups)
		{
		}
		
		protected override void AddActionCommands (Stetic.Wrapper.Action action)
		{
			ToolButton bindButton = new ToolButton (null, GettextCatalog.GetString ("Bind to Field"));
			bindButton.IsImportant = true;
			bindButton.Clicked += new EventHandler (OnBindWidget);
			bindButton.Show ();
			Insert (bindButton, -1);
			if (action == null)
				bindButton.Sensitive = false;
			
			base.AddActionCommands (action);
		}

		void OnBindWidget (object o, EventArgs a)
		{
			if (BindField != null)
				BindField (this, a);
		}
	}
}
