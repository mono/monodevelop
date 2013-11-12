//
// SelectRenamedClassDialog.cs
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
using System.Collections.Generic;
using Gtk;
using Gdk;
using Glade;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;


namespace MonoDevelop.GtkCore.Dialogs
{
	public class SelectRenamedClassDialog: IDisposable
	{
		[Glade.Widget ("SelectRenamedClassDialog")] protected Dialog dialog;
		[Glade.Widget] protected TreeView treeClasses;
		readonly ListStore store;
		
		public SelectRenamedClassDialog (IEnumerable<IType> classes)
		{
			var glade = new XML (null, "gui.glade", "SelectRenamedClassDialog", null);
			glade.Autoconnect (this);
			
			store = new ListStore (typeof(Pixbuf), typeof(string));
			treeClasses.Model = store;
			
			var column = new TreeViewColumn ();
		
			var pr = new CellRendererPixbuf ();
			column.PackStart (pr, false);
			column.AddAttribute (pr, "pixbuf", 0);
			
			var crt = new CellRendererText ();
			column.PackStart (crt, true);
			column.AddAttribute (crt, "text", 1);
			
			treeClasses.AppendColumn (column);
			
			foreach (var cls in classes) {
				Pixbuf pic = ImageService.GetPixbuf (cls.GetStockIcon ());
				store.AppendValues (pic, cls.FullName);
			}
		}
		
		public bool Run ()
		{
			return MessageService.ShowCustomDialog (dialog) == (int) ResponseType.Ok;
		}
		
		public string SelectedClass {
			get {
				TreeModel foo;
				TreeIter iter;
				if (!treeClasses.Selection.GetSelected (out foo, out iter))
					return null;
				return (string) store.GetValue (iter, 1);
			}
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
	}
	
}
