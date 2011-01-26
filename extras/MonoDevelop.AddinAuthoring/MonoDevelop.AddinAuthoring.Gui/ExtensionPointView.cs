// 
// ExtensionPointView.cs
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
using System.Collections.Generic;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ExtensionPointView : Gtk.Bin
	{
		AddinRegistry reg;
		
		public ExtensionPointView ()
		{
			this.Build ();
		}
		
		public void Fill (ExtensionPoint ep, AddinRegistry reg)
		{
			string name;
			if (!string.IsNullOrEmpty (ep.Name))
				name = ep.Name;
			else
				name = ep.Path;
			
			labelName.Markup = "<small>Extension Point</small>\n<big><b>" + GLib.Markup.EscapeText (name) + "</b></big>";
			if (!string.IsNullOrEmpty (ep.Description))
				labelDesc.Text = ep.Description;
			else
				labelDesc.Text = AddinManager.CurrentLocalizer.GetString ("No additional documentation");
			
			List<ExtensionNodeType> types = new List<ExtensionNodeType> ();
			GetNodeTypes (reg, ep.NodeSet, types);
			
			uint row = 0;
			foreach (ExtensionNodeType nt in types) {
				Gtk.Label lab = new Gtk.Label ();
				lab.Markup = "<b>" + GLib.Markup.EscapeText (nt.NodeName) + "</b>";
				lab.UseUnderline = false;
				lab.Xalign = lab.Yalign = 0;
				Gtk.Button but = new Gtk.Button (lab);
				but.Relief = Gtk.ReliefStyle.None;
				tableNodes.Attach (but, 0, 1, row, row + 1);
				Gtk.Table.TableChild ct = (Gtk.Table.TableChild) tableNodes [but];
				ct.XOptions = Gtk.AttachOptions.Fill;
				
				lab = new Gtk.Label (nt.Description);
				lab.UseUnderline = false;
				lab.Xalign = lab.Yalign = 0;
				lab.Wrap = true;
				tableNodes.Attach (lab, 1, 2, row, row + 1);
				ct = (Gtk.Table.TableChild) tableNodes [lab];
				ct.XOptions = Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill;
				row++;
			}
			tableNodes.ShowAll ();
		}
		
		void GetNodeTypes (AddinRegistry reg, ExtensionNodeSet nset, List<ExtensionNodeType> list)
		{
			foreach (ExtensionNodeType nt in nset.NodeTypes)
				list.Add (nt);
			
			foreach (string ns in nset.NodeSets) {
				ExtensionNodeSet cset = FindNodeSet (reg, nset.ParentAddinDescription, ns);
				if (cset != null)
					GetNodeTypes (reg, nset, list);
			}
		}
		
		ExtensionNodeSet FindNodeSet (AddinRegistry reg, AddinDescription adesc, string name)
		{
			ExtensionNodeSet nset = adesc.ExtensionNodeSets [name];
			if (nset != null)
				return nset;
			foreach (AddinDependency adep in adesc.MainModule.Dependencies) {
				Addin addin = reg.GetAddin (adep.FullAddinId);
				if (addin != null) {
					nset = adesc.ExtensionNodeSets [name];
					if (nset != null)
						return nset;
				}
			}
			return null;
		}
	}
}

