//
// SelectEncodingsDialog.cs
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
using System.Linq;
using Gtk;
using MonoDevelop.Core;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Ide
{
	internal partial class SelectEncodingsDialog: Gtk.Dialog
	{
		ListStore storeAvail;
		ListStore storeSelected;
		
		public SelectEncodingsDialog ()
		{
			Build ();
			try {
				storeAvail = new ListStore (typeof(string), typeof(string), typeof(int));
				listAvail.Model = storeAvail;
				listAvail.AppendColumn ("Name", new Gtk.CellRendererText (), "text", 0);
				listAvail.AppendColumn ("Encoding", new Gtk.CellRendererText (), "text", 1);
				
				storeSelected = new ListStore (typeof(string), typeof(string), typeof(int));
				listSelected.Model = storeSelected;
				listSelected.AppendColumn ("Name", new Gtk.CellRendererText (), "text", 0);
				listSelected.AppendColumn ("Encoding", new Gtk.CellRendererText (), "text", 1);
				
				foreach (var e in Encoding.GetEncodings ()) {
					var enc = e.GetEncoding ();
					storeAvail.AppendValues (enc.EncodingName, enc.WebName, e.CodePage);
				}
				
				foreach (var e in TextEncoding.ConversionEncodings) {
					var enc = Encoding.GetEncoding (e.CodePage);
					storeSelected.AppendValues (enc.EncodingName, enc.WebName, enc.CodePage);
				}
			} catch (Exception  ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}
		
		

		protected void OnRespond (object o, ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok)
				return;
				
			TreeIter iter;
			var list = new List<int> ();
			if (storeSelected.GetIterFirst (out iter)) {
				do {
					var enc = (int)storeSelected.GetValue (iter, 2);
					list.Add (enc);
				} while (storeSelected.IterNext (ref iter));
			}
			TextEncoding.ConversionEncodings = list.Select (TextEncoding.GetEncoding).ToArray ();
		}
		
		protected void OnAddClicked (object ob, EventArgs args)
		{
			MoveItem (listAvail, storeAvail, listSelected, storeSelected);
		}
		
		protected void OnRemoveClicked (object ob, EventArgs args)
		{
			MoveItem (listSelected, storeSelected, listAvail, storeAvail);
			EnsureItemIsSelected ();
		}

		void EnsureItemIsSelected ()
		{
			TreeModel model;
			TreeIter iter;
			// if the last item is removed no item is selected.
			if (!listSelected.Selection.GetSelected (out model, out iter)) {
				// Select last item
				if (storeSelected.GetIterFirst (out iter)) {
					TreeIter last = iter;
					while (storeSelected.IterNext (ref iter))
						last = iter;
					listSelected.Selection.SelectIter (last);
				}
			}
		}
		
		void MoveItem (TreeView sourceList, ListStore sourceStore, TreeView targetList, ListStore targetStore)
		{
			TreeModel model;
			TreeIter iter;
			
			if (sourceList.Selection.GetSelected (out model, out iter)) {
				TreeIter newiter = targetStore.AppendValues (sourceStore.GetValue (iter, 0), sourceStore.GetValue (iter, 1), sourceStore.GetValue (iter, 2));
				targetList.Selection.SelectIter (newiter);
				
				TreeIter oldIter = iter;
				if (sourceStore.IterNext (ref iter))
					sourceList.Selection.SelectIter (iter);
				sourceStore.Remove (ref oldIter);
			}
		}
		
		protected void OnUpClicked (object ob, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;
			
			if (listSelected.Selection.GetSelected (out model, out iter)) {
				TreePath iterPath = storeSelected.GetPath (iter);
				TreeIter oldIter;
				if (storeSelected.GetIterFirst (out oldIter)) {
					if (storeSelected.GetPath (oldIter).Equals (iterPath))
						return;
						
					TreeIter prevIter;
						
					do  {
						prevIter = oldIter;
						if (!storeSelected.IterNext (ref oldIter))
							return;
					}
					while (!storeSelected.GetPath (oldIter).Equals (iterPath));
					storeSelected.Swap (prevIter, iter);
				}
				
				storeSelected.Swap (oldIter, iter);
			}
		}
		
		protected void OnDownClicked (object ob, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;
			
			if (listSelected.Selection.GetSelected (out model, out iter)) {
				TreeIter oldIter = iter;
				if (storeSelected.IterNext (ref iter))
					storeSelected.Swap (oldIter, iter);
			}
		}
	}
}
