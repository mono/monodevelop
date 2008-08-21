//
// CellRendererComboBox.cs
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
using Gdk;
using MonoDevelop.Components;
using Mono.Addins;
using Mono.Addins.Description;

namespace MonoDevelop.AddinAuthoring
{
	public class CellRendererExtension: CellRendererText
	{
		ExtensionEditor editor;
		
		public CellRendererExtension ()
		{
			//Mode |= Gtk.CellRendererMode.Editable;
		}

		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
			if (editor != null) {
				Gtk.Requisition req = editor.SizeRequest ();
				if (req.Height > height)
					height = req.Height;
			}
		}
		
		public override CellEditable StartEditing (Gdk.Event ev, Widget widget, string path, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
		{
			TreeView tree = (TreeView) widget;
			TreeIter iter;
			tree.Model.GetIterFromString (out iter, path);
			ExtensionNodeDescription node = tree.Model.GetValue (iter, 2) as ExtensionNodeDescription;
			if (node != null) {
				editor = new ExtensionEditor (node);
				tree.Model.EmitRowChanged (new TreePath (path), iter);
				TreeViewCellContainer tc = new TreeViewCellContainer (editor);
				tc.EditingDone += delegate {
					editor = null;
					tree.Model.EmitRowChanged (new TreePath (path), iter);
				};
				return tc;
			}
			else {
				this.StopEditing (false);
				return null;
			}
		}
	}
	
	public class ExtensionEditor: Gtk.VBox
	{
		public ExtensionEditor (ExtensionNodeDescription node)
		{
			HBox fieldsBox = new HBox ();
			fieldsBox.Spacing = 3;
			Gtk.Label lab = new Gtk.Label ();
			lab.Markup = "<b>" + node.NodeName + "</b>";
			fieldsBox.PackStart (lab, false, false, 0);
			ExtensionNodeType nt = node.GetNodeType ();
			if (nt == null) {
				fieldsBox.PackStart (new Gtk.Label ("Unknown node type"), false, false, 0);
			}
			else {
				AddAttribute (fieldsBox, node, "id", "System.String", false);
				Console.WriteLine ("ppAA: " + nt.Attributes.Count);
				foreach (NodeTypeAttribute at in nt.Attributes) {
					AddAttribute (fieldsBox, node, at.Name, at.Type, at.Required);
				}
			}
			PackStart (fieldsBox, false, false, 0);
			ShowAll ();
		}
		
		void AddAttribute (HBox fieldsBox, ExtensionNodeDescription node, string name, string type, bool req)
		{
			HBox box = new HBox ();
			Gtk.Label lab = new Gtk.Label ();
			lab.Markup = "<b>" + name + "</b>=\"";
			box.PackStart (lab, false, false, 0);
			Gtk.Entry entry = new AutoSizeEntry ();
			entry.Text = node.GetAttribute (name);
			box.PackStart (entry, false, false, 0);
			box.PackStart (new Gtk.Label ("\" "), false, false, 0);
			fieldsBox.PackStart (box, false, false, 0);
		}
	}
	
	[System.ComponentModel.Category("MonoDevelop.AddinAuthoring")]
	[System.ComponentModel.ToolboxItem(true)]
	class AutoSizeEntry: Gtk.Entry
	{
		public AutoSizeEntry ()
		{
//			HasFrame = false;
			Resize ();
		}
		
		void Resize ()
		{
			int len;
			if (Text.Length < 2)
				len = 2;
			else if (Text.Length > 100)
				len = 100;
			else
				len = Text.Length;
			this.WidthChars = len;
		}
		
		protected override void OnChanged ()
		{
			Resize ();
		}
	}
}
