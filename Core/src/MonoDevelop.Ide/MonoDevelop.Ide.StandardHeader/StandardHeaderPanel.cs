//
// StandardHeaderPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.IO;
using System.Collections;

using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.StandardHeaders
{
	public class StandardHeaderPanel : AbstractOptionPanel
	{
		StandardHeaderWidget widget;
		
		public override void LoadPanelContents ()
		{
			Add (widget = new  StandardHeaderWidget ());
		}
		
		public override bool StorePanelContents ()
		{			
			return widget.Store();
		}
		
		class StandardHeaderWidget : GladeWidgetExtract 
		{
			[Glade.Widget] public Gtk.TextView textView;
			
			public StandardHeaderWidget () : base ("Base.glade", "StandardHeaderPanel")
			{
				textView.Buffer.Text = StandardHeaderService.Header;
			}
			
			public bool Store ()
			{
				StandardHeaderService.Header = textView.Buffer.Text;
				return true;
			}
		}
	}
}
