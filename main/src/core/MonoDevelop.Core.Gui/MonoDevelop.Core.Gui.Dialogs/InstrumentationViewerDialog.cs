// 
// InstrumentationViewerDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Components.Chart;
using Gtk;

namespace MonoDevelop.Core.Gui.Dialogs
{
	public partial class InstrumentationViewerDialog : Gtk.Dialog
	{
		TreeStore store;
		ListStore seriesStore;
		ChartView view = new ChartView ();
		List<ChartView> views = new List<ChartView> ();
		
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
		
		public InstrumentationViewerDialog ()
		{
			Build ();
			
			// The serie selector list
			
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
			
			crt.Toggled += CrtToggled;
			
			// The series list
			
			seriesStore = new ListStore (typeof(bool), typeof(Gdk.Pixbuf), typeof (string), typeof(ChartSerieInfo), typeof(String), typeof(String), typeof (String));
			listSeries.Model = seriesStore;
			
			col = new TreeViewColumn ();
			col.Title = "Counter";
			crt = new CellRendererToggle ();
			col.PackStart (crt, false);
			col.AddAttribute (crt, "active", 0);
			
			CellRendererPixbuf crp = new CellRendererPixbuf ();
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
			
			// The counter selector
			
			foreach (CounterCategory cat in InstrumentationService.GetCategories ())
				AppendCategory (TreeIter.Zero, cat);
			
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
			
			endTime = DateTime.Now;
			startTime = endTime - visibleTime;
			
			DateTime st = InstrumentationService.StartTime;
			if (st > startTime) st = startTime;
			
			chartScroller.Adjustment.Lower = st.Ticks;
			
			UpdateCharts ();
			chartScroller.Value = chartScroller.Adjustment.Upper;
			StartAutoscroll ();
			
			LoadViews ();
		}
		
		void LoadViews ()
		{
			try {
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				FilePath file = PropertyService.ConfigPath.Combine ("monitor-views.xml");
				if (System.IO.File.Exists (file)) {
					views = (List<ChartView>) ser.Deserialize (PropertyService.ConfigPath.Combine ("monitor-views.xml"), typeof (List<ChartView>));
					UpdateViews ();
					return;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error while loading monitor-views.xml", ex);
			}
			views = new List<ChartView> ();
			ChartView v = new ChartView ();
			v.Name = "Default";
			views.Add (v);
			UpdateViews ();
		}
		
		void SaveViews ()
		{
			try {
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				ser.Serialize (PropertyService.ConfigPath.Combine ("monitor-views.xml"), views);
			} catch (Exception ex) {
				LoggingService.LogError ("Error while saving monitor-views.xml", ex);
			}
		}
		
		void UpdateViews ()
		{
			int i = comboProfiles.Active;
			
			((ListStore)comboProfiles.Model).Clear ();
			foreach (ChartView view in views)
				comboProfiles.AppendText (view.Name);
			
			if (i != -1 && i < views.Count)
				comboProfiles.Active = i;
			else
				comboProfiles.Active = 0;
		}

		void CountChartSelectionChanged (object sender, EventArgs e)
		{
			UpdateListValues ();
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
		
		void FillSelectedSeries ()
		{
			seriesStore.Clear ();
			foreach (ChartSerieInfo si in view.Series)
				seriesStore.AppendValues (si.Visible, ImageService.GetPixbuf ("#" + si.Color, IconSize.Menu), si.Name, si, "", "", "");
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
						view.Add (c);
					else
						view.Remove (c);
					FillSelectedSeries ();					
					UpdateButtonStatus ();
				}
				UpdateCharts ();
			}
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
				}
			}
		}
		
		void UpdateCharts ()
		{
			timeChart.Visible = false;
			countChart.Visible = true;
			
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
		
		void UpdateCharView ()
		{
			countChart.StartX = startTime.Ticks;
			countChart.EndX = endTime.Ticks;
			timeChart.StartX = startTime.Ticks;
			timeChart.EndX = endTime.Ticks;
			countChart.OriginX = countChart.StartX;
			timeChart.OriginX = timeChart.StartX;
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
					store.SetValue (it, 0, view.Contains (c));
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

		protected virtual void OnButtonSelectClicked (object sender, System.EventArgs e)
		{
			boxSelector.Show ();
			buttonSelect.Hide ();
			UpdateSelectedCounters ();
		}

		protected virtual void OnButtonHideSelectorClicked (object sender, System.EventArgs e)
		{
			boxSelector.Hide ();
			buttonSelect.Show ();
		}

		protected virtual void OnButtonSaveClicked (object sender, System.EventArgs e)
		{
			ChartView v = views [comboProfiles.Active];
			v.CopyFrom (view);
			view.Modified = false;
			SaveViews ();
			UpdateButtonStatus ();
		}

		protected virtual void OnButtonSaveAsClicked (object sender, System.EventArgs e)
		{
			string name;
			do {
				name = MessageService.GetTextResponse ("Profile Name", "New Profile", "");
			} while (name == string.Empty);
			
			if (name == null)
				return;
			
			ChartView v = new ChartView ();
			v.CopyFrom (view);
			v.Name = name;
			views.Add (v);
			
			comboProfiles.AppendText (name);
			comboProfiles.Active = views.Count - 1;
			SaveViews ();
			UpdateButtonStatus ();
		}

		protected virtual void OnButtonDeleteClicked (object sender, System.EventArgs e)
		{
			if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the profile '{0}'?", view.Name), AlertButton.Delete))
				return;

			views.RemoveAt (comboProfiles.Active);
			SaveViews ();
			UpdateViews ();
		}

		protected virtual void OnComboProfilesChanged (object sender, System.EventArgs e)
		{
			view = new ChartView ();
			if (comboProfiles.Active != -1) {
				ChartView v = views [comboProfiles.Active];
				if (v.EditedView != null)
					view = v.EditedView;
				else {
					view.CopyFrom (v);
					v.EditedView = view;
				}
			}
			FillSelectedSeries ();
			UpdateCharts ();
			UpdateButtonStatus ();
			UpdateSelectedCounters ();
		}
		
		void UpdateButtonStatus ()
		{
			if (comboProfiles.Active == -1) {
				buttonDelete.Sensitive = false;
				buttonSave.Sensitive = false;
				buttonSaveAs.Sensitive = false;
			}
			else {
				buttonDelete.Sensitive = comboProfiles.Active != 0;
				buttonSave.Sensitive = view.Modified;
				buttonSaveAs.Sensitive = true;
			}
		}

		protected virtual void OnButtonFlushClicked (object sender, System.EventArgs e)
		{
			GC.Collect ();
			for (int n=0; n<500; n++) {
				byte[] mem = new byte [1024 * 1000];
				mem [0] = 0;
			}
		}
	}
	
	class ChartView
	{
		static string[] knownColors = new string[] {
			"0000FF", "006400", "8B0000", "8A2BE2", "FFD700", "5F9EA0", "D2691E", 
			"6495ED", "DC143C", "00008B", "A9A9A9", "00FF00", "8B008B", "FF8C00", 
			"00CED3", "2F4F4F", "FF1493", "A52A2A", "ADFF2F", "90EE90", "808000", 
			"98FB98", "DB7093", "4169FF", "FA8072", "2E8B57", "A0522D", "00FF7F", 
			"FF6347", "9ACD32", "FFFF00"
		};
		
		[ItemProperty]
		public string Name { get; set; }
		
		[ItemProperty]
		public List<ChartSerieInfo> Series = new List<ChartSerieInfo> ();
		
		public ChartView EditedView;
		public bool Modified;
		
		public void CopyFrom (ChartView other)
		{
			Name = other.Name;
			Series.Clear ();
			foreach (ChartSerieInfo si in other.Series) {
				ChartSerieInfo c = new ChartSerieInfo ();
				c.CopyFrom (si);
				Series.Add (c);
			}
		}
		
		public bool Contains (Counter c)
		{
			foreach (ChartSerieInfo si in Series) {
				if (si.Name == c.Name)
					return true;
			}
			return false;
		}
		
		public void Add (Counter c)
		{
			ChartSerieInfo info = new ChartSerieInfo ();
			info.Init (c);
			foreach (string color in knownColors) {
				if (!IsColorUsed (color)) {
					info.Color = color;
					break;
				}
			}
			if (info.Color == null)
				return;
			
			Series.Add (info);
			Modified = true;
		}
		
		bool IsColorUsed (string color)
		{
			foreach (ChartSerieInfo info in Series)
				if (info.Color == color)
					return true;
			return false;
		}
		
		public void Remove (Counter c)
		{
			for (int n=0; n<Series.Count; n++) {
				if (Series [n].Name == c.Name) {
					Series.RemoveAt (n);
					return;
				}
			}
			Modified = true;
		}
		
		public void SetVisible (ChartSerieInfo info, bool visible)
		{
			info.Visible = visible;
			info.Serie.Visible = visible;
			Modified = true;
		}
		
		public void UpdateSeries ()
		{
			foreach (ChartSerieInfo info in Series)
				info.UpdateSerie ();
		}
		
		internal static Cairo.Color ParseColor (string s)
		{
			double r = ((double) int.Parse (s.Substring (0,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			double g = ((double) int.Parse (s.Substring (2,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			double b = ((double) int.Parse (s.Substring (4,2), System.Globalization.NumberStyles.HexNumber)) / 255;
			return new Cairo.Color (r, g, b);
		}
	}
	
	class ChartSerieInfo
	{
		Serie serie;
		Counter counter;
		
		[ItemProperty]
		public string Name;
		
		[ItemProperty]
		public string Color;
		
		[ItemProperty (DefaultValue=true)]
		public bool Visible = true;
		
		DateTime lastUpdateTime = DateTime.MinValue;
		
		public void Init (Counter counter)
		{
			this.counter = counter;
			Name = counter.Name;
		}
		
		public void CopyFrom (ChartSerieInfo other)
		{
			Name = other.Name;
			Color = other.Color;
			Visible = other.Visible;
			serie = other.serie;
			counter = other.counter;
		}
		
		public Counter Counter {
			get {
				if (counter == null && Name != null) {
					counter = InstrumentationService.GetCounter (Name);
				}
				return counter;
			}
		}
		
		public Serie Serie {
			get {
				if (serie == null) {
					serie = new Serie (Name);
					if (Counter == null)
						return serie;
					if (Counter.DisplayMode == CounterDisplayMode.Block) {
						serie.ExtendBoundingValues = true;
						serie.InitialValue = 0;
						serie.DisplayMode = DisplayMode.BlockLine;
					} else
						serie.DisplayMode = DisplayMode.Line;
					serie.Color = ChartView.ParseColor (Color);
					foreach (CounterValue val in Counter.GetValues ()) {
						serie.AddData (val.TimeStamp.Ticks, val.Value);
						lastUpdateTime = val.TimeStamp;
					}
					serie.Visible = Visible;
				}
				return serie;
			}
		}
		
		public void UpdateSerie ()
		{
			if (serie == null || Counter == null)
				return;
			foreach (CounterValue val in Counter.GetValuesAfter (lastUpdateTime)) {
				serie.AddData (val.TimeStamp.Ticks, val.Value);
				lastUpdateTime = val.TimeStamp;
			}
		}
	}
}
