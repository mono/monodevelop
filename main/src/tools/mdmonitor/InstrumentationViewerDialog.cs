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
using MonoDevelop.Ide.Gui.Dialogs;

namespace Mono.Instrumentation.Monitor
{
	public partial class InstrumentationViewerDialog : Gtk.Window
	{
		TreeStore store;
		List<ChartView> views = new List<ChartView> ();
		
		TreeIter iterViews;
//		TreeIter iterStart;
		TreeIter iterTimers;
		TreeIter iterTimerStats;
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
			iterTimerStats = store.AppendValues ("Timer Statistics");
			iterViews = store.AppendValues ("Views");
			LoadViews ();
			
			treeCounters.ExpandAll ();
			treeCounters.Selection.Changed += HandleTreeCountersSelectionChanged;
			ShowAll ();
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			
			MonoDevelop.Components.HslColor c = Style.Background (Gtk.StateType.Normal);
			c.L -= 0.1;
			headerBox.ModifyBg (Gtk.StateType.Normal, c);
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
			hpaned.Sensitive = (App.Service != null);
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
			MessageDialog dlg = new MessageDialog (this, DialogFlags.Modal, MessageType.Question, ButtonsType.OkCancel, msg);
			if (dlg.Run () == (int) ResponseType.Ok) {
				views.Remove (view);
				SaveViews ();
				UpdateViews ();
			}
			dlg.Destroy ();
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

		TimeStatisticsView timerStatsView;
		void HandleTreeCountersSelectionChanged (object sender, EventArgs e)
		{
			TreeIter it;
			if (!treeCounters.Selection.GetSelected (out it)) {
				SetView (null, null, false);
				return;
			}
			
			if (store.GetPath (it).Equals (store.GetPath (iterTimers))) {
				if (timersWidget == null) {
					timersWidget = new InstrumenationChartView (this);
					timersWidget.ShowAllTimers ();
				}
				SetView (timersWidget, "Timers", false);
			}
			else if (store.GetPath (it).Equals (store.GetPath (iterTimerStats))) {
				if (timerStatsView == null)
					timerStatsView = new TimeStatisticsView (this);
				timerStatsView.Fill ();
				SetView (timerStatsView, "Timer Statistics", false);
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
					SetView (cv, v.Name, true);
				} else {
					SetView (null, null, false);
				}
			}
		}
		
		void SetView (Widget w, string title, bool showSaveButtons)
		{
			if (viewBox.Child != null) {
				Widget ow = viewBox.Child;
				viewBox.Remove (ow);
			}
			if (w != null) {
				viewBox.Add (w);
				w.Show ();
			}
			
			if (title != null) {
				labelHeader.Markup = "<big><b>" + GLib.Markup.EscapeText (title) + "</b></big>";
				headerBox.Show ();
				buttonsBox.Visible = showSaveButtons;
			} else {
				headerBox.Hide ();
				buttonsBox.Visible = false;
			}
		}
		
		public void EnableSave (bool enable)
		{
			buttonSave.Sensitive = enable;
		}
		
		protected virtual void OnButtonFlushClicked (object sender, System.EventArgs e)
		{
			GC.Collect ();
			for (int n=0; n<500; n++) {
				byte[] mem = new byte [1024 * 1000];
				mem [0] = 0;
			}
		}
		
		protected virtual void OnButtonSaveClicked (object sender, System.EventArgs e)
		{
			((InstrumenationChartView)viewBox.Child).Save ();
		}
		
		protected virtual void OnButtonSaveAsClicked (object sender, System.EventArgs e)
		{
			((InstrumenationChartView)viewBox.Child).SaveAs ();
		}
		
		protected virtual void OnButtonDeleteClicked (object sender, System.EventArgs e)
		{
			((InstrumenationChartView)viewBox.Child).Delete ();
		}
		
		protected virtual void OnFlushMemoryActionActivated (object sender, System.EventArgs e)
		{
			GC.Collect ();
			for (int n=0; n<500; n++) {
				byte[] mem = new byte [1024 * 1000];
				mem [0] = 0;
			}
		}
		
		protected virtual void OnExitActionActivated (object sender, System.EventArgs e)
		{
			Gtk.Application.Quit ();
		}
		
		protected virtual void OnOpenActionActivated (object sender, System.EventArgs e)
		{
			FileChooserDialog fdiag  = new FileChooserDialog ("Open Data File", this, FileChooserAction.Open);
			fdiag.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			fdiag.AddButton (Gtk.Stock.Open, ResponseType.Ok);
			fdiag.SelectMultiple = false;
			
			try {
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok && fdiag.Filenames.Length > 0) {
					treeCounters.Selection.UnselectAll ();
					App.LoadServiceData (fdiag.Filenames[0]);
					UpdateViews ();
				}
			} finally {
				fdiag.Destroy ();
			}
		}
		
		public void InstallMacGlobalMenu ()
		{
			MacIntegration.IgeMacMenu.GlobalKeyHandlerEnabled = true;
			MacIntegration.IgeMacMenu.MenuBar = menubar1;
			var quitItem = (MenuItem) UIManager.GetWidget ("/menubar1/FileAction/ExitAction");
			MacIntegration.IgeMacMenu.QuitMenuItem = quitItem;
			menubar1.Hide ();
		}
	}		
	
	internal static class CounterColor
	{
		static Dictionary<Counter,Gdk.Color> colors = new Dictionary<Counter, Gdk.Color> ();
		static Dictionary<Counter,Gdk.Pixbuf> icons = new Dictionary<Counter, Gdk.Pixbuf> ();
		
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
		
		public static Gdk.Pixbuf GetIcon (this Counter c)
		{
			Gdk.Pixbuf cachedIcon;
			if (icons.TryGetValue (c, out cachedIcon))
				return cachedIcon;
			
			Gdk.Color gcolor = c.GetColor ();
			uint color = (((uint)gcolor.Red >> 8) << 24) | (((uint)gcolor.Green >> 8) << 16) | (((uint)gcolor.Blue >> 8) << 8) | 0xff;
			cachedIcon = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 16, 16);
			cachedIcon.Fill (color);
			icons [c] = cachedIcon;
			return cachedIcon;
		}
		
		public static Gdk.Color GetTimeColor (this CounterValue val, TimerCounter c)
		{
			long m = c.AverageTime.Ticks;
			long v = val.Duration.Ticks;
			if (v >= m*3)
				return new Gdk.Color (255, 0, 0);
			if (v >= m*2)
				return new Gdk.Color (150, 0, 0);
			if (v >= (long)((double)m) * 1.5d)
				return new Gdk.Color (100, 0, 0);
			if (v >= (long)((double)m) * 1.1d)
				return new Gdk.Color (50, 0, 0);
			if (v <= m/3)
				return new Gdk.Color (0, 255, 0);
			if (v <= m/2)
				return new Gdk.Color (0, 150, 0);
			if (v <= (long)((double)m) * 0.75d)
				return new Gdk.Color (0, 100, 0);
			if (v <= (long)((double)m) * 0.90d)
				return new Gdk.Color (00, 50, 0);
			return new Gdk.Color (0, 0, 0);
		}
	}
}
