// 
// ExtensionNodeView.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Mono.Addins.Description;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ExtensionNodeView : Gtk.Bin
	{
		public ExtensionNodeView ()
		{
			this.Build ();
		}
		
		public void Fill (ExtensionNodeDescription node)
		{
			ExtensionNodeType ntype = node.GetNodeType ();
			labelName.Markup = "<small>Extension Node</small>\n<big><b>" + GLib.Markup.EscapeText (ntype.NodeName) + "</b></big>";
			
			if (!string.IsNullOrEmpty (ntype.Description))
				labelDesc.Text = ntype.Description;
			else
				labelDesc.Text = AddinManager.CurrentLocalizer.GetString ("No additional documentation");
			
			uint row = 0;
			foreach (var att in node.Attributes) {
				Gtk.Label lab = new Gtk.Label ();
				lab.Markup = "<b>" + GLib.Markup.EscapeText (att.Name) + ":</b>";
				lab.UseUnderline = false;
				lab.Xalign = 0;
				tableAtts.Attach (lab, 0, 1, row, row + 1);
				Gtk.Table.TableChild ct = (Gtk.Table.TableChild) tableAtts [lab];
				ct.XOptions = Gtk.AttachOptions.Fill;
				
				lab = new Gtk.Label (att.Value);
				lab.UseUnderline = false;
				lab.Xalign = 0;
				lab.Wrap = true;
				tableAtts.Attach (lab, 1, 2, row, row + 1);
				ct = (Gtk.Table.TableChild) tableAtts [lab];
				ct.XOptions = Gtk.AttachOptions.Fill;
				row++;
			}
			tableAtts.ShowAll ();
		}
	}
}

