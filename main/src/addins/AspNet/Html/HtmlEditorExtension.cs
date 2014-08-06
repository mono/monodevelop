// 
// HtmlEditorExtension.cs
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

using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.Html
{
	sealed class HtmlEditorExtension : BaseHtmlEditorExtension
	{
		//copied from MoonlightEditorExtension
		#region Document outline
		
		protected override void RefillOutlineStore (ParsedDocument doc, Gtk.TreeStore store)
		{
			XDocument xdoc = ((MonoDevelop.Xml.Editor.XmlParsedDocument)doc).XDocument;
			if (xdoc == null)
				return;
//			Gtk.TreeIter iter = outlineTreeStore.AppendValues (System.IO.Path.GetFileName (CU.Document.FilePath), p);
			BuildTreeChildren (store, Gtk.TreeIter.Zero, xdoc);
		}
		
		protected override void InitializeOutlineColumns (MonoDevelop.Ide.Gui.Components.PadTreeView outlineTree)
		{
			outlineTree.TextRenderer.Xpad = 0;
			outlineTree.TextRenderer.Ypad = 0;
			outlineTree.AppendColumn ("Node", outlineTree.TextRenderer, OutlineTreeDataFunc);
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
		
		void OutlineTreeDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText txtRenderer = (Gtk.CellRendererText) cell;
			XNode n = (XNode) model.GetValue (iter, 0);
			txtRenderer.Text = n.FriendlyPathRepresentation;
		}
		
		new void SelectNode (XNode n)
		{
			var region = n.Region;
			
			XElement el = n as XElement;
			if (el != null && el.IsClosed && el.ClosingTag.Region.End > region.End) {
				region = new DomRegion (region.Begin, el.ClosingTag.Region.End);
			}
			
			int s = Editor.Document.LocationToOffset (region.BeginLine, region.BeginColumn );
			int e = Editor.Document.LocationToOffset (region.EndLine, region.EndColumn);
			if (e > s && s > -1)
				Editor.SetSelection (s, e);
		}
		
		#endregion
	}
}
