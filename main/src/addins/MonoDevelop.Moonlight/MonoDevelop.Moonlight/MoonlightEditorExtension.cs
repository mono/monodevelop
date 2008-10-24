// 
// MoonlightEditorExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

using MonoDevelop.Xml.StateEngine; 

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightEditorExtension : MonoDevelop.XmlEditor.Gui.BaseXmlEditorExtension
	{
		
		public MoonlightEditorExtension ()
		{
		}
		
		protected override IEnumerable<string> SupportedExtensions {
			get {
				yield return ".xaml";
			}
		}
		
		#region Document outline
		
		protected override void RefillOutlineStore (MonoDevelop.Projects.Dom.ParsedDocument doc, Gtk.TreeStore store)
		{
			XDocument xdoc = ((MoonlightParsedDocument)doc).XDocument;
			if (xdoc == null)
				return;
//			Gtk.TreeIter iter = outlineTreeStore.AppendValues (System.IO.Path.GetFileName (CU.Document.FilePath), p);
			BuildTreeChildren (store, Gtk.TreeIter.Zero, xdoc);
		}
		
		protected override void InitializeOutlineColumns (Gtk.TreeView outlineTree)
		{
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			crt.Xpad = 0;
			crt.Ypad = 0;
			outlineTree.AppendColumn ("Node", crt, new Gtk.TreeCellDataFunc (outlineTreeDataFunc));
		}
		
		protected override void OutlineSelectionChanged (object selection)
		{
			SelectNode ((XNode)selection);
		}
		
		static void BuildTreeChildren (Gtk.TreeStore store, Gtk.TreeIter parent, XContainer p)
		{
			foreach (XNode n in p.Nodes) {
				Gtk.TreeIter childIter;
				if (!parent.Equals (Gtk.TreeIter.Zero))
					childIter = store.AppendValues (parent, n);
				else
					childIter = store.AppendValues (n);
				
				XContainer c = n as XContainer;
				if (c != null && c.FirstChild != null)
					BuildTreeChildren (store, childIter, c);
			}
		}
		
		void outlineTreeDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText txtRenderer = (Gtk.CellRendererText) cell;
			XNode n = (XNode) model.GetValue (iter, 0);
			txtRenderer.Text = n.FriendlyPathRepresentation;
		}
		
		void SelectNode (XNode n)
		{
			MonoDevelop.Projects.Dom.DomRegion region = n.Region;
			
			XElement el = n as XElement;
			if (el != null && el.IsClosed && el.ClosingTag.Region.End > region.End) {
				region.End = el.ClosingTag.Region.End;
			}
			
			int s = Editor.GetPositionFromLineColumn (region.Start.Line, region.Start.Column);
			int e = Editor.GetPositionFromLineColumn (region.End.Line, region.End.Column);
			if (e > s && s > -1)
				Editor.Select (s, e);
		}
		
		#endregion
	}
}
