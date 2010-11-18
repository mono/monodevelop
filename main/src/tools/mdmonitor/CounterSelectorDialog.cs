// 
// CounterSelectorDialog.cs
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
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using Gtk;
using MonoDevelop.Components;

namespace Mono.Instrumentation.Monitor
{
	public partial class CounterSelectorDialog : Gtk.Dialog
	{
		TreeStore store;
		List<Counter> selection = new List<Counter> ();
		
		public CounterSelectorDialog ()
		{
			this.Build ();
			
			HasSeparator = false;
			store = new TreeStore (typeof(bool), typeof(string), typeof(CounterCategory), typeof(Counter), typeof(bool));
			treeCounters.Model = store;
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererToggle crt = new CellRendererToggle ();
			col.PackStart (crt, false);
			col.AddAttribute (crt, "active", 0);
			col.AddAttribute (crt, "visible", 4);
			
			CellRendererText crx = new CellRendererText ();
			col.PackStart (crx, true);
			col.AddAttribute (crx, "text", 1);
			treeCounters.AppendColumn (col);
			
			foreach (CounterCategory cat in App.Service.GetCategories ())
				AppendCategory (TreeIter.Zero, cat);
			
			UpdateSelectedCounters ();
			treeCounters.ExpandAll ();
			
			crt.Toggled += CrtToggled;
		}
		
		public IEnumerable<Counter> Selection {
			get {
				return selection;
			}
			set {
				selection = new List<Counter> (value);
			}
		}
		
		void AppendCategory (TreeIter it, CounterCategory cat)
		{
			TreeIter catIt;
			if (it.Equals (TreeIter.Zero))
				catIt = store.AppendValues (false, cat.Name, cat, null, false);
			else
				catIt = store.AppendValues (it, false, cat.Name, cat, null, false);
			
			foreach (Counter c in cat.Counters)
				store.AppendValues (catIt, false, c.Name, null, c, true);
		}
				
		void UpdateSelectedCounters ()
		{
			TreeIter it;
			if (store.GetIterFirst (out it))
				UpdateSelectedCounters (it);
		}
		
		void UpdateSelectedCounters (TreeIter it)
		{
			do {
				Counter c = (Counter) store.GetValue (it, 3);
				if (c != null) {
					store.SetValue (it, 0, selection.Contains (c));
				}
				else {
					CounterCategory cat = (CounterCategory) store.GetValue (it, 2);
					if (cat != null) {
						TreeIter ci;
						if (store.IterChildren (out ci, it))
							UpdateSelectedCounters (ci);
					}
				}
			}
			while (store.IterNext (ref it));
		}

		void CrtToggled (object o, ToggledArgs args)
		{
			TreeIter it;
			if (store.GetIterFromString (out it, args.Path)) {
				bool val = !(bool) store.GetValue (it, 0);
				store.SetValue (it, 0, val);
				Counter c = (Counter) store.GetValue (it, 3);
				if (c != null) {
					if (val)
						selection.Add (c);
					else
						selection.Remove (c);
				}
			}
		}
	}
}

