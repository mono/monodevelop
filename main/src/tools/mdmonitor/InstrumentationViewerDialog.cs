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
using MonoDevelop.Core;

namespace Mono.Instrumentation.Monitor
{
	public partial class InstrumentationViewerDialog : Gtk.Window
	{
		TreeStore store;
		List<ChartView> views = new List<ChartView> ();
		
		TreeIter iterViews;
//		TreeIter iterStart;
		TreeIter iterTimers;
		Dictionary<ChartView,InstrumenationChartView> chartWidgets = new Dictionary<ChartView, InstrumenationChartView> ();
		InstrumenationChartView timersWidget;
		
		public InstrumentationViewerDialog (): base ("Instrumentation Monitor")
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
				NewProfile dlg = new NewProfile ();
				if (dlg.Run () == (int) Gtk.ResponseType.Cancel)
					return;
				name = dlg.ViewName;
				dlg.Destroy ();
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
			string msg = string.Format ("Are you sure you want to delete the profile '{0}'?", view.Name);
			MessageDialog dlg = new MessageDialog (this, DialogFlags.Modal, MessageType.Question, ButtonsType.Ok | ButtonsType.Cancel, msg);
			if (dlg.Run () == (int) ResponseType.Cancel)
			
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
			Gtk.Application.Quit ();
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
		Gdk.Pixbuf colorIcon;
		
		[ItemProperty]
		public string Name;
		
		[ItemProperty]
//		public string Color;
		
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
			Visible = other.Visible;
			serie = other.serie;
			counter = other.counter;
		}
		
		public Counter Counter {
			get {
				if (counter == null && Name != null)
					counter = App.Service.GetCounter (Name);
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
					serie.Color = GdkColor.ToCairoColor ();
					foreach (CounterValue val in Counter.GetValues ()) {
						serie.AddData (val.TimeStamp.Ticks, val.Value);
						lastUpdateTime = val.TimeStamp;
					}
					serie.Visible = Visible;
				}
				return serie;
			}
		}
		
		public bool UpdateCounter ()
		{
			if (!Counter.Disposed)
				return false;
			serie = null;
			counter = null;
			lastUpdateTime = DateTime.MinValue;
			return true;
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
				return Counter.GetColor ();
			}
		}
		
		public Gdk.Pixbuf ColorIcon {
			get {
				if (colorIcon == null) {
					uint color = (((uint)GdkColor.Red >> 8) << 24) | (((uint)GdkColor.Green >> 8) << 16) | (((uint)GdkColor.Blue >> 8) << 8) | 0xff;
					if (!colorIcons.TryGetValue (color, out colorIcon)) {
						colorIcon = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 16, 16);
						colorIcon.Fill (color);
						colorIcons [GdkColor.Pixel] = colorIcon;
					}
				}
				return colorIcon;
			}
		}
		
		static Dictionary<uint,Gdk.Pixbuf> colorIcons = new Dictionary<uint, Gdk.Pixbuf> ();
	}
	
	internal static class CounterColor
	{
		static Dictionary<Counter,Gdk.Color> colors = new Dictionary<Counter, Gdk.Color> ();
		
		public static Gdk.Color GetColor (this Counter c)
		{
			Gdk.Color cachedColor;
			if (colors.TryGetValue (c, out cachedColor))
				return cachedColor;
			
			Random r = new Random (c.Name.GetHashCode ());
			HslColor col = new HslColor ();
			int nc = c.Name.GetHashCode ();
			if (nc < 0) nc = -nc;
			col.H = r.NextDouble ();
			col.S = r.NextDouble ();
			col.L = 0.3 + (r.NextDouble () * 0.3);
			Gdk.Color gc = col;
			colors [c] = gc;
			return gc;
		}
	}
}
