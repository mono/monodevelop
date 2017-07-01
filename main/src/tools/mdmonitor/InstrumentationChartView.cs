//
// InstrumenationChartView.cs
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Components.Chart;
using Gtk;
using System.Globalization;

namespace Mono.Instrumentation.Monitor
{
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class InstrumentationChartView : Gtk.Bin
	{
		InstrumentationViewerDialog parent;
		ListStore seriesStore;
		ChartView view = new ChartView ();
		ChartView originalView;

		ListStore listViewStore;
		TreeView listView;
		ScrolledWindow listViewScrolled;

		BasicChart countChart;
		BasicChart timeChart;
		DateTime startTime;
		DateTime endTime;

		DateTimeAxis countAxisX;
		IntegerAxis countAxisY;
		DateTimeAxis timeAxisX;
		IntegerAxis timeAxisY;

		TimeSpan visibleTime = TimeSpan.FromMinutes (5);

		List<Serie> currentSeries = new List<Serie> ();

		public InstrumentationChartView (InstrumentationViewerDialog parent)
		{
			Build ();

			this.parent = parent;

			// The list for the List Mode

			listViewStore = new ListStore (typeof(ListViewValueInfo), typeof(Gdk.Pixbuf), typeof (string), typeof(string), typeof(string), typeof (string), typeof (string));
			listView = new TreeView ();
			listView.Model = listViewStore;

			CellRendererText crx = new CellRendererText ();
			listView.AppendColumn ("Timestamp", crx, "text", 3);

			TreeViewColumn col = new TreeViewColumn ();
			col.Title = "Counter";
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "pixbuf", 1);
			col.PackStart (crx, true);
			col.AddAttribute (crx, "text", 2);
			listView.AppendColumn (col);

			listView.AppendColumn ("Count", crx, "text", 4);
			listView.AppendColumn ("Total Count", crx, "text", 5);
			listView.AppendColumn ("Time", crx, "text", 6);

			listView.RowActivated += HandleListViewRowActivated;

			listViewScrolled = new ScrolledWindow ();
			listViewScrolled.Add (listView);
			listViewScrolled.ShadowType = ShadowType.In;
			listViewScrolled.HscrollbarPolicy = PolicyType.Automatic;
			listViewScrolled.VscrollbarPolicy = PolicyType.Automatic;
			listViewScrolled.ShowAll ();
			boxCharts.PackStart (listViewScrolled, true, true, 0);

			// The series list

			seriesStore = new ListStore (typeof(bool), typeof(Gdk.Pixbuf), typeof (string), typeof(ChartSerieInfo), typeof(String), typeof(String), typeof (String));
			listSeries.Model = seriesStore;

			col = new TreeViewColumn ();
			col.Title = "Counter";
			CellRendererToggle crt = new CellRendererToggle ();
			col.PackStart (crt, false);
			col.AddAttribute (crt, "active", 0);

			crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "pixbuf", 1);

			crx = new CellRendererText ();
			col.PackStart (crx, true);
			col.AddAttribute (crx, "text", 2);
			listSeries.AppendColumn (col);

			listSeries.AppendColumn ("Last", crx, "text", 4);
			listSeries.AppendColumn ("Sel", crx, "text", 5);
			listSeries.AppendColumn ("Diff", crx, "text", 6);

			crt.Toggled += SerieToggled;

			countChart = new BasicChart ();
			countAxisY = new IntegerAxis (true);
			countAxisX = new DateTimeAxis (true);
			countChart.AddAxis (countAxisX, AxisPosition.Bottom);
			countChart.AddAxis (countAxisY, AxisPosition.Right);
			countChart.OriginY = 0;
			countChart.StartY = 0;
//			countChart.EndY = 100;
			countChart.AllowSelection = true;
			countChart.SetAutoScale (AxisDimension.Y, false, true);
			countChart.SelectionStart.LabelAxis = countAxisX;
			countChart.SelectionEnd.LabelAxis = countAxisX;
			countChart.SelectionChanged += CountChartSelectionChanged;

			timeChart = new BasicChart ();
			timeAxisY = new IntegerAxis (true);
			timeAxisX = new DateTimeAxis (true);
			timeChart.AddAxis (timeAxisX, AxisPosition.Bottom);
			timeChart.AddAxis (timeAxisY, AxisPosition.Right);
			timeChart.OriginY = 0;
			timeChart.StartY = 0;
			timeChart.EndY = 100;
			timeChart.AllowSelection = true;
//			timeChart.SetAutoScale (AxisDimension.Y, true, true);
			timeChart.SelectionStart.LabelAxis = timeAxisX;
			timeChart.SelectionEnd.LabelAxis = timeAxisX;

			frameCharts.PackStart (countChart, true, true, 0);
			frameCharts.PackStart (timeChart, true, true, 0);
			frameCharts.ShowAll ();

			if (App.FromFile) {
				if (visibleTime > App.Service.EndTime - App.Service.StartTime)
					visibleTime = App.Service.EndTime - App.Service.StartTime;
				startTime = App.Service.StartTime;
				endTime = startTime + visibleTime;
			}
			else {
				endTime = DateTime.Now;
				startTime = endTime - visibleTime;
			}

			DateTime st = App.Service.StartTime;
			if (st > startTime) st = startTime;

			chartScroller.Adjustment.Lower = st.Ticks;

			UpdateCharts ();
			chartScroller.Value = chartScroller.Adjustment.Upper;

			if (!App.FromFile)
				StartAutoscroll ();

			toggleTimeView.Active = true;
		}

		public void ShowAllTimers ()
		{
			hboxChartBar.Visible = hboxSeriesBar.Visible = false;

			view = new ChartView ();
			foreach (Counter c in App.Service.GetCounters ()) {
				if (c is TimerCounter)
					view.Add (c);
			}

			FillSelectedSeries ();
			toggleListView.Active = true;
		}

		void HandleListViewRowActivated (object o, RowActivatedArgs args)
		{
			TreeIter sel;
			if (!listView.Selection.GetSelected (out sel))
				return;
			ListViewValueInfo vinfo = (ListViewValueInfo) listViewStore.GetValue (sel, 0);
			if (vinfo.Value.HasTimerTraces) {
				TimeLineViewWindow win = new TimeLineViewWindow (vinfo.Serie.Counter, vinfo.Value);
				win.Show ();
			}
		}

		void CountChartSelectionChanged (object sender, EventArgs e)
		{
			UpdateListValues ();
		}

		void FillSelectedSeries ()
		{
			seriesStore.Clear ();
			foreach (ChartSerieInfo si in view.Series)
				seriesStore.AppendValues (si.Visible, si.ColorIcon, si.Name, si, "", "", "");
		}

		void SerieToggled (object o, ToggledArgs args)
		{
			TreeIter it;
			if (seriesStore.GetIterFromString (out it, args.Path)) {
				bool val = !(bool) seriesStore.GetValue (it, 0);
				seriesStore.SetValue (it, 0, val);
				ChartSerieInfo c = (ChartSerieInfo) seriesStore.GetValue (it, 3);
				if (c != null) {
					view.SetVisible (c, val);
					UpdateButtonStatus ();
					if (listViewScrolled.Visible)
						FillValuesList ();
				}
			}
		}

		void UpdateButtonStatus ()
		{
			parent.EnableSave (view.Modified);
		}

		void UpdateCharts ()
		{
			listViewScrolled.Visible = !toggleTimeView.Active;
			frameCharts.Visible = toggleTimeView.Active;

			if (frameCharts.Visible) {
				var timeWidget = timeChart.GetNativeWidget<Widget> ();
				timeWidget.Visible = false;
				var countWidget = countChart.GetNativeWidget<Widget> ();
				countWidget.Visible = true;
				foreach (Serie s in currentSeries) {
					countChart.RemoveSerie (s);
					timeChart.RemoveSerie (s);
				}

				UpdateCharView ();

				foreach (ChartSerieInfo si in view.Series) {
					si.UpdateSerie ();
					countChart.AddSerie (si.Serie);
					currentSeries.Add (si.Serie);
				}

				DateTime t = DateTime.Now;
				chartScroller.Adjustment.Upper = t.Ticks;
				UpdatePageSize ();
			}
			else if (listViewScrolled.Visible) {
				FillValuesList ();
			}
		}

		void FillValuesList ()
		{
			listViewStore.Clear ();
			List<ListViewValueInfo> values = new List<ListViewValueInfo> ();

			foreach (var serie in view.Series.Where (s => s.Visible)) {
				foreach (CounterValue val in serie.Counter.GetValues ())
					values.Add (new ListViewValueInfo () { Serie=serie, Value=val });
			}

			values.Sort (delegate (ListViewValueInfo v1, ListViewValueInfo v2) {
				return v1.Value.TimeStamp.CompareTo (v2.Value.TimeStamp);
			});

			foreach (ListViewValueInfo vinfo in values) {
				CounterValue val = vinfo.Value;
				string time = val.TimeStamp.ToLongTimeString ();
				listViewStore.AppendValues (vinfo, vinfo.Serie.ColorIcon, vinfo.Serie.Counter.Name, time, val.Value.ToString (), val.TotalCount.ToString (), val.HasTimerTraces ? val.Duration.TotalMilliseconds.ToString () : "");
			}
		}

		void UpdateCharView ()
		{
			countChart.StartX = startTime.Ticks;
			countChart.EndX = endTime.Ticks;
			timeChart.StartX = startTime.Ticks;
			timeChart.EndX = endTime.Ticks;
			countChart.OriginX = countChart.StartX;
			timeChart.OriginX = timeChart.StartX;
		}

		uint scrollFunc;

		void StartAutoscroll ()
		{
			scrollFunc = GLib.Timeout.Add (1000, ScrollCharts);
		}

		void StopAutoscroll ()
		{
			if (scrollFunc != 0) {
				GLib.Source.Remove (scrollFunc);
				scrollFunc = 0;
			}
		}

		bool IsShowingLatest {
			get {
				return (chartScroller.Value == chartScroller.Adjustment.Upper - chartScroller.Adjustment.PageSize);
			}
		}

		bool ScrollCharts ()
		{
			double ticks = DateTime.Now.Ticks;
			if (IsShowingLatest) {
				chartScroller.Adjustment.Upper = ticks;
				chartScroller.Value = chartScroller.Adjustment.Upper - chartScroller.Adjustment.PageSize;
			} else
				chartScroller.Adjustment.Upper = ticks;

			// If any of the counters has been disposed, update it
			foreach (ChartSerieInfo info in view.Series) {
				if (info.UpdateCounter ()) {
					UpdateCharts ();
					break;
				}
			}
			UpdateListValues ();
			view.UpdateSeries ();
			return true;
		}

		void UpdateListValues ()
		{
			TreeIter it;
			if (seriesStore.GetIterFirst (out it)) {
				do {
					ChartSerieInfo ci = (ChartSerieInfo) seriesStore.GetValue (it, 3);
					if (ci.Counter == null)
						continue;
					CounterValue val = ci.Counter.LastValue;
					seriesStore.SetValue (it, 4, val.Value.ToString ());

					if (countChart.ActiveCursor != null) {
						val = ci.Counter.GetValueAt (new DateTime ((long)countChart.ActiveCursor.Value));
						seriesStore.SetValue (it, 5, val.Value.ToString ());
					}

					val = ci.Counter.GetValueAt (new DateTime ((long)countChart.SelectionStart.Value));
					CounterValue val2 = ci.Counter.GetValueAt (new DateTime ((long)countChart.SelectionEnd.Value));

					seriesStore.SetValue (it, 6, (val2.Value - val.Value).ToString ());
				}
				while (seriesStore.IterNext (ref it));
			}
		}

		protected override void OnDestroyed ()
		{
			StopAutoscroll ();
			base.OnDestroyed ();
		}


		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			Destroy ();
		}

		protected virtual void OnDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			Destroy ();
		}

		protected virtual void OnChartScrollerValueChanged (object sender, System.EventArgs e)
		{
			startTime = new DateTime ((long)chartScroller.Value);
			endTime = startTime + visibleTime;
			UpdateCharView ();
		}

		void UpdatePageSize ()
		{
			chartScroller.Adjustment.PageSize = visibleTime.Ticks;
			chartScroller.Adjustment.PageIncrement = visibleTime.Ticks * 0.9;
			chartScroller.Adjustment.StepIncrement = visibleTime.Ticks * 0.1;
		}

		protected virtual void OnButtonZoomInClicked (object sender, System.EventArgs e)
		{
			if (countChart.SelectionStart.Value != countChart.SelectionEnd.Value) {
				startTime = new DateTime ((long)countChart.SelectionStart.Value);
				endTime = new DateTime ((long)countChart.SelectionEnd.Value);
				visibleTime = endTime - startTime;
			}
			else {
				long oldTime = visibleTime.Ticks;
				visibleTime = new TimeSpan ((long)(visibleTime.Ticks * 0.7));
				if (visibleTime < TimeSpan.FromSeconds (1))
					visibleTime = TimeSpan.FromSeconds (1);
				if (IsShowingLatest) {
					startTime = endTime - visibleTime;
				} else {
					DateTime t = new DateTime ((long)countChart.SelectionStart.Value);
					if (t > startTime && t < endTime)
						// Center to the cursor
						endTime = t + new TimeSpan (visibleTime.Ticks/2);
					else {
						long diff = oldTime - visibleTime.Ticks;
						endTime -= new TimeSpan (diff/2);
					}
					startTime = endTime - visibleTime;
				}
			}
			UpdatePageSize ();
			chartScroller.Value = startTime.Ticks;
			UpdateCharView ();
		}

		protected virtual void OnButtonZoomOutClicked (object sender, System.EventArgs e)
		{
			long oldTime = visibleTime.Ticks;
			visibleTime = new TimeSpan ((long)(visibleTime.Ticks * 1.4));
			if (visibleTime > TimeSpan.FromDays (2))
				visibleTime = TimeSpan.FromDays (2);
			long diff = visibleTime.Ticks - oldTime;
			endTime += new TimeSpan (diff/2);
			startTime = endTime - visibleTime;
			UpdatePageSize ();
			chartScroller.Value = startTime.Ticks;
			UpdateCharView ();
		}

		public void SetView (ChartView v)
		{
			originalView = v;
			view = new ChartView ();

			if (v.EditedView != null)
				view = v.EditedView;
			else {
				view.CopyFrom (v);
				v.EditedView = view;
			}
			FillSelectedSeries ();
			UpdateCharts ();
			UpdateButtonStatus ();
		}

		bool uppdatingToggles;

		protected virtual void OnToggleListViewToggled (object sender, System.EventArgs e)
		{
			if (uppdatingToggles)
				return;
			uppdatingToggles = true;
			if (!toggleListView.Active)
				toggleListView.Active = true;
			else
				toggleTimeView.Active = false;
			uppdatingToggles = false;
			UpdateCharts ();
		}


		protected virtual void OnToggleTimeViewToggled (object sender, System.EventArgs e)
		{
			if (uppdatingToggles)
				return;
			uppdatingToggles = true;
			if (!toggleTimeView.Active)
				toggleTimeView.Active = true;
			else
				toggleListView.Active = false;
			uppdatingToggles = false;
			UpdateCharts ();
		}

		public void Save ()
		{
			originalView.CopyFrom (view);
			view.Modified = false;
			parent.SaveViews ();
			UpdateButtonStatus ();
		}

		public void SaveAs ()
		{
			Application.Invoke ((o, args) => {
				parent.NewView (view);
			});
		}

		public void Delete ()
		{
			Application.Invoke ((o, args) => {
				parent.DeleteView (originalView);
			});
		}

		protected virtual void OnButtonRemoveCounterClicked (object sender, System.EventArgs e)
		{
			foreach (TreePath p in listSeries.Selection.GetSelectedRows ()) {
				TreeIter it;
				if (seriesStore.GetIter (out it, p)) {
					ChartSerieInfo s = (ChartSerieInfo) seriesStore.GetValue (it, 3);
					view.Remove (s.Counter);
				}
			}
			FillSelectedSeries ();
		}

		protected virtual void OnButtonAddCounterClicked (object sender, System.EventArgs e)
		{
			CounterSelectorDialog dlg = new CounterSelectorDialog ();
			dlg.TransientFor = (Gtk.Window) this.Toplevel;
			if (dlg.Run () == (int) ResponseType.Ok) {
				foreach (Counter c in dlg.Selection)
					view.Add (c);
				FillSelectedSeries ();
				UpdateCharts ();
			}
			dlg.Destroy ();
		}
	}

	class ListViewValueInfo
	{
		public CounterValue Value;
		public ChartSerieInfo Serie;
	}
}
