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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Components.Chart;
using Gtk;
using System.Globalization;
using MonoDevelop.Components;

namespace MonoDevelop.Core.Gui.Instrumentation
{
	public partial class InstrumentationViewerDialog : Gtk.Dialog
	{
		TreeStore store;
		List<ChartView> views = new List<ChartView> ();
		
		TreeIter iterViews;
		TreeIter iterStart;
		TreeIter iterTimers;
		Dictionary<ChartView,InstrumenationChartView> chartWidgets = new Dictionary<ChartView, InstrumenationChartView> ();
		InstrumenationChartView timersWidget;
		
		public InstrumentationViewerDialog ()
		{
			Build ();
			
			store = new TreeStore (typeof(string), typeof(ChartView));
			treeCounters.Model = store;
			treeCounters.AppendColumn ("", new CellRendererText (), "text", 0);
			
			//iterStart = store.AppendValues ("Start");
			iterTimers = store.AppendValues ("Timers Events");
			iterViews = store.AppendValues ("Views");
			LoadViews ();
			
			treeCounters.ExpandAll ();
			treeCounters.Selection.Changed += HandleTreeCountersSelectionChanged;
			ShowAll ();
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
		
		public void SaveViews ()
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
			TreeIter it;
			while (store.IterChildren (out it, iterViews))
				store.Remove (ref it);
			
			foreach (ChartView view in views) {
				store.AppendValues (iterViews, view.Name, view);
			}
		}
		
		internal void NewView (ChartView source)
		{
			string name;
			do {
				name = MessageService.GetTextResponse ("Profile Name", "New Profile", "");
			} while (name == string.Empty);
			
			if (name == null)
				return;
			
			ChartView v = new ChartView ();
			v.CopyFrom (source);
			v.Name = name;
			views.Add (v);
			SaveViews ();
			
			TreeIter it = store.AppendValues (iterViews, name, v);
			treeCounters.Selection.SelectIter (it);
		}
		
		internal void DeleteView (ChartView view)
		{
			if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the profile '{0}'?", view.Name), AlertButton.Delete))
				return;

			views.Remove (view);
			SaveViews ();
			UpdateViews ();
		}

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			Destroy ();
		}

		protected virtual void OnDeleteEvent (object o, Gtk.DeleteEventArgs args)
		{
			Destroy ();
		}
		
		protected override void OnDestroyed ()
		{
			if (timersWidget != null && timersWidget.Parent == null)
				timersWidget.Destroy ();
			foreach (Widget w in chartWidgets.Values) {
				if (w.Parent == null)
					w.Destroy ();
			}
			base.OnDestroyed ();
		}


		void HandleTreeCountersSelectionChanged (object sender, EventArgs e)
		{
			TreeIter it;
			if (!treeCounters.Selection.GetSelected (out it))
				return;
			
			if (store.GetPath (it).Equals (store.GetPath (iterTimers))) {
				if (timersWidget == null) {
					timersWidget = new InstrumenationChartView (this);
					timersWidget.ShowAllTimers ();
				}
				SetView (timersWidget);
			}
			else {
				ChartView v = (ChartView) store.GetValue (it, 1);
				if (v != null) {
					InstrumenationChartView cv;
					if (!chartWidgets.TryGetValue (v, out cv)) {
						cv = new InstrumenationChartView (this);
						chartWidgets [v] = cv;
						cv.SetView (v);
					}
					SetView (cv);
				}
			}
		}
		
		void SetView (Widget w)
		{
			if (hpaned.Child2 != null) {
				Widget ow = hpaned.Child2;
				hpaned.Remove (ow);
			}
			if (w != null) {
				hpaned.Add2 (w);
				w.Show ();
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
		
		public IEnumerable<Counter> GetCounters ()
		{
			foreach (ChartSerieInfo ci in Series)
				yield return ci.Counter;
		}
		
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
/*			foreach (string color in knownColors) {
				if (!IsColorUsed (color)) {
					info.Color = color;
					break;
				}
			}
			if (info.Color == null)
				return;*/
			
			Gdk.Color col = c.GetColor ();
			info.Color = (col.Red >> 8).ToString ("X2") + (col.Green >> 8).ToString ("X2") + (col.Blue >> 8).ToString ("X2");
			
			Series.Add (info);
			Modified = true;
		}
		
/*		bool IsColorUsed (string color)
		{
			foreach (ChartSerieInfo info in Series)
				if (info.Color == color)
					return true;
			return false;
		}*/
		
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
		bool gotGdkColor;
		Gdk.Color gdkColor;
		Gdk.Pixbuf colorIcon;
		
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
		
		public Gdk.Color GdkColor {
			get {
				if (!gotGdkColor) {
					byte r = byte.Parse (Color.Substring (0, 2), NumberStyles.HexNumber);
					byte g = byte.Parse (Color.Substring (2, 2), NumberStyles.HexNumber);
					byte b = byte.Parse (Color.Substring (4, 2), NumberStyles.HexNumber);
					gdkColor = new Gdk.Color (r, g, b);
				}
				return gdkColor;
			}
		}
		
		public Gdk.Pixbuf ColorIcon {
			get {
				if (colorIcon == null)
					colorIcon = ImageService.GetPixbuf ("#" + Color, IconSize.Menu);
				return colorIcon;
			}
		}
	}
	
	internal static class CounterColor
	{
		public static Gdk.Color GetColor (this Counter c)
		{
			if (c.Color != null)
				return (Gdk.Color) c.Color;
			Random r = new Random (c.Name.GetHashCode ());
			HslColor col = new HslColor ();
			int nc = c.Name.GetHashCode ();
			if (nc < 0) nc = -nc;
			col.H = r.NextDouble ();
			col.S = 1;
			col.L = 0.3 + (r.NextDouble () * 0.3);
			Gdk.Color gc = col;
			c.Color = gc;
			return gc;
		}
	}
}
