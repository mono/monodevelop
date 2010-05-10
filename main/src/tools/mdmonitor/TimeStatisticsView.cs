// 
// TimeStatisticsView.cs
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
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core.Instrumentation;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Instrumentation.Monitor
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TimeStatisticsView : Gtk.Bin
	{
		TreeStore store;
		Gdk.Color normalColor;
		
		const int ColFilled = 7;
		const int ColCounter = 8;
		const int ColValue = 9;
		const int ColShowStats = 10;
		const int ColShowIcon = 11;
		const int ColColor = 12;
		
		public TimeStatisticsView (Gtk.Widget parent)
		{
			this.Build ();
			store = new TreeStore (
               typeof (Gdk.Pixbuf), // Icon
               typeof(string), // Text
               typeof(int),    // Count
               typeof(float),  // Total time
               typeof(float),  // Average
               typeof(float),  // Min
               typeof(float),  // Max
               typeof(bool),   // Expanded
               typeof(Counter),
               typeof(CounterValue), 
               typeof(bool),  // Show stats
               typeof(bool), // Show icon
               typeof(Gdk.Color)); // Color
			
			treeView.Model = store;
			normalColor = parent.Style.Foreground (StateType.Normal);
			
			CellRendererText crt = new CellRendererText ();
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = "Counter";
			col.PackStart (crp, false);
			col.AddAttribute (crp, "pixbuf", 0);
			col.AddAttribute (crp, "visible", ColShowIcon);
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", 1);
			treeView.AppendColumn (col);
			col.SortColumnId = 1;
			
			treeView.AppendColumn ("Count", crt, "text", 2).SortColumnId = 2;
			treeView.AppendColumn ("Total Time", new CellRendererText (), "text", 3, "foreground-gdk", ColColor).SortColumnId = 3;
			treeView.AppendColumn ("Average Time", crt, "text", 4, "visible", ColShowStats).SortColumnId = 4;
			treeView.AppendColumn ("Min Time", crt, "text", 5, "visible", ColShowStats).SortColumnId = 5;
			treeView.AppendColumn ("Max Time", crt, "text", 6, "visible", ColShowStats).SortColumnId = 6;
			
			Show ();
			
			foreach (TreeViewColumn c in treeView.Columns)
				c.Resizable = true;
			
			treeView.TestExpandRow += HandleTreeViewTestExpandRow;
			treeView.RowActivated += HandleTreeViewRowActivated;
		}

		void HandleTreeViewRowActivated (object o, RowActivatedArgs args)
		{
			TreeIter it;
			store.GetIter (out it, args.Path);
			object oval = store.GetValue (it, ColValue);
			if (oval == null)
				return;
			CounterValue val = (CounterValue) oval;
			store.IterParent (out it, it);
			Counter c = (Counter) store.GetValue (it, ColCounter);
			TimeLineViewWindow win = new TimeLineViewWindow (c, val);
			win.Show ();
		}
		
		public void Fill ()
		{
			TreeViewState s = new TreeViewState (treeView, 1);
			s.Save ();
			store.Clear ();
			
			if (checkShowCats.Active) {
				foreach (CounterCategory cat in App.Service.GetCategories ())
					AppendCategory (cat);
			} else {
				foreach (Counter c in App.Service.GetCounters ()) {
					if (c is TimerCounter)
						AppendCounter (TreeIter.Zero, (TimerCounter) c);
				}
			}
		}

		void HandleTreeViewTestExpandRow (object o, TestExpandRowArgs args)
		{
			bool filled = (bool) store.GetValue (args.Iter, ColFilled);
			Counter counter = (Counter) store.GetValue (args.Iter, ColCounter);
			if (!filled && counter != null) {
				store.SetValue (args.Iter, ColFilled, true);
				TreeIter it;
				store.IterChildren (out it, args.Iter);
				store.Remove (ref it);
				AppendValues (args.Iter, (TimerCounter) counter);
			} else
				args.RetVal = false;
		}
		
		void AppendCategory (CounterCategory cat)
		{
			IEnumerable<Counter> counters = cat.Counters.Where (c => c is TimerCounter);
			if (counters.Any ()) {
				TimeSpan time = TimeSpan.Zero;
				TimeSpan min = TimeSpan.MaxValue;
				TimeSpan max = TimeSpan.Zero;
				int count = 0;
				foreach (TimerCounter c in counters) {
					if (c.CountWithDuration > 0) {
						time += c.TotalTime;
						count += c.CountWithDuration;
						if (c.MinTime < min)
							min = c.MinTime;
						if (c.MaxTime > max)
							max = c.MaxTime;
					}
				}
				double avg = count > 0 ? (time.TotalMilliseconds / count) : 0d;
				if (count == 0)
					min = TimeSpan.Zero;
				
				TreeIter it = store.AppendValues (null, cat.Name, count, (float)time.TotalMilliseconds, (float)avg, (double)min.TotalMilliseconds, (double)max.TotalMilliseconds, false, null, null, true, false, normalColor);
				foreach (Counter c in counters)
					AppendCounter (it, (TimerCounter) c);
			}
		}
		
		void AppendCounter (TreeIter parent, TimerCounter c)
		{
			TreeIter it;
			if (parent.Equals (TreeIter.Zero))
				it = store.AppendValues (c.GetIcon (), c.Name, c.Count, (float)c.TotalTime.TotalMilliseconds, (float)c.AverageTime.TotalMilliseconds, (float)c.MinTime.TotalMilliseconds, (float)c.MaxTime.TotalMilliseconds, false, c, null, true, true, normalColor);
			else
				it = store.AppendValues (parent, c.GetIcon (), c.Name, c.Count, (float)c.TotalTime.TotalMilliseconds, (float)c.AverageTime.TotalMilliseconds, (float)c.MinTime.TotalMilliseconds, (float)c.MaxTime.TotalMilliseconds, false, c, null, true, true, normalColor);

			// Dummy node
			store.AppendValues (it, null, "*");
		}
		
		void AppendValues (TreeIter parent, TimerCounter c)
		{
			Gdk.Pixbuf icon = c.GetIcon ();
			foreach (CounterValue val in c.GetValues ().Where (val => val.HasTimerTraces)) {
				string msg = !string.IsNullOrEmpty (val.Message) ? val.Message : c.Name;
				store.AppendValues (parent, icon, msg, val.Value, (float) val.Duration.TotalMilliseconds, 0f, 0f, 0f, false, null, val, true, true, val.GetTimeColor (c));
			}
		}
		
		protected virtual void OnButtonUpdateClicked (object sender, System.EventArgs e)
		{
			Fill ();
		}
		
		protected virtual void OnCheckShowCatsToggled (object sender, System.EventArgs e)
		{
			Fill ();
		}
	}
}

