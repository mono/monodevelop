// 
// SearchPopupWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Collections.Generic;
using Gtk;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.MainToolbar
{
	public class SearchPopupWidget : Gtk.DrawingArea
	{
		const int yMargin = 6;
		const int xMargin = 6;
		const int categorySeparatorHeight = 16;

		List<SearchCategory> categories = new List<SearchCategory> ();
		List<Tuple<SearchCategory, ISearchDataSource>> results = new List<Tuple<SearchCategory, ISearchDataSource>> ();
		Pango.Layout layout, headerLayout;
		CancellationTokenSource src;
		Cairo.Color headerColor;

		SearchPopupWindow searchPopupWindow;

		public SearchPopupWidget (SearchPopupWindow searchPopupWindow)
		{
			this.searchPopupWindow = searchPopupWindow;
			Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonMotionMask | Gdk.EventMask.ButtonReleaseMask;
			headerColor = CairoExtensions.ParseColor ("b3b3b3");
			categories.Add (new ProjectSearchCategory (this));
			categories.Add (new FileSearchCategory (this));
			layout = new Pango.Layout (PangoContext);
			headerLayout = new Pango.Layout (PangoContext);
		}

		public void Update (string searchPattern)
		{
			if (src != null)
				src.Cancel ();
			selectedCategory = null;
			selectedDataSource = null;
			selectedItem = 0;	

			src = new CancellationTokenSource ();
			results.Clear ();
			foreach (var _cat in categories) {
				var cat = _cat;
				var token = src.Token;
				var task = cat.GetResults (searchPattern, token);
				task.ContinueWith (delegate {
					if (token.IsCancellationRequested || task.Result == null) {
						return;
					}
					Application.Invoke (delegate {
						if (token.IsCancellationRequested)
							return;
						ShowResult (cat, task.Result);
					}
					);
				}
				);
			}
		}

		void ShowResult (SearchCategory cat, ISearchDataSource result)
		{
			results.Add (Tuple.Create (cat, result));

			results.Sort ((x, y) => {
				return categories.IndexOf (x.Item1).CompareTo (categories.IndexOf (y.Item1));
			}
			);

			selectedCategory = results [0].Item1;
			selectedDataSource = results [0].Item2;
			selectedItem = 0;

			if (results.Count == categories.Count) {
				QueueResize ();
				QueueDraw ();
			}
		}

		const int maxItems = 8;

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			int ox, oy;
			searchPopupWindow.GetPosition (out ox, out oy);
			Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (ox, oy));

			double maxX = 0, y = yMargin;
				
			foreach (var result in results) {
//				var category = result.Item1;
				var dataSrc = result.Item2;
				if (dataSrc.ItemCount == 0)
					continue;
				
				for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
					layout.SetMarkup (dataSrc.GetMarkup (i, false) +"\n<small>\t"+dataSrc.GetDescriptionMarkup (i, false) +"</small>");

					int w,h;
					layout.GetPixelSize (out w, out h);
					y += h;
					maxX = Math.Max (maxX, w);
				}
			}
			requisition.Width = Math.Min (geometry.Width - 16, Math.Max (Allocation.Width, Math.Max (480, (int)maxX + 100 + xMargin * 2)));
			requisition.Height = Math.Min (geometry.Height - 16, (int)y + 4 + yMargin * 2 + (results.Count - 1) * categorySeparatorHeight);
		}

		Tuple<SearchCategory, ISearchDataSource, int> GetItemAt (double px, double py)
		{
			double maxX = 0, y = yMargin;
				
			foreach (var result in results) {
				var category = result.Item1;
				var dataSrc = result.Item2;
				if (dataSrc.ItemCount == 0)
					continue;
				
				for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
					layout.SetMarkup (dataSrc.GetMarkup (i, false));

					int w, h;
					layout.GetPixelSize (out w, out h);
					y += h;
					if (y > py){
						return Tuple.Create (category, dataSrc, i);
					}

					var region = dataSrc.GetRegion (i);
					if (!region.Begin.IsEmpty) {
						layout.SetMarkup (region.BeginLine.ToString ());
						int w2, h2;
						layout.GetPixelSize (out w2, out h2);
						w += w2;
					}
					maxX = Math.Max (maxX, w);
				}
			}
			return new Tuple<SearchCategory, ISearchDataSource, int> (null, null, -1);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				var item = GetItemAt (evnt.X, evnt.Y);
				if (item.Item1 != null) {
					selectedCategory = item.Item1;
					selectedDataSource = item.Item2;
					selectedItem = item.Item3;
					QueueDraw ();
				}
				if (evnt.Type == Gdk.EventType.TwoButtonPress)
					OnItemActivated (EventArgs.Empty);
			}

			return base.OnButtonPressEvent (evnt);
		}


		internal bool ProcessKey (Gdk.Key key)
		{
			switch (key) {
			case Gdk.Key.Up:
				if (selectedDataSource == null)
					return true;
				if (selectedItem > 0) {
					selectedItem--;
				} else {
					for (int i = 1; i < results.Count; i++) {
						if (results [i].Item1 == selectedCategory) {
							selectedCategory = results [i - 1].Item1;
							selectedDataSource = results [i - 1].Item2;
							selectedItem = Math.Min (maxItems, selectedDataSource.ItemCount) - 1;
						}
					}
				}
				QueueDraw ();
				return true;
			case Gdk.Key.Down:
				if (selectedDataSource == null)
					return true;
				if (selectedItem + 1 < Math.Min (maxItems, selectedDataSource.ItemCount)) {
					selectedItem++;
				} else {
					for (int i = 0; i < results.Count - 1; i++) {
						if (results [i].Item1 == selectedCategory && results [i + 1].Item2.ItemCount > 0) {
							selectedCategory = results [i + 1].Item1;
							selectedDataSource = results [i + 1].Item2;
							selectedItem = 0;
						}
					}
				}
				QueueDraw ();
				return true;
			case Gdk.Key.Home:
				if (results.Any ()) {
					selectedCategory = results.First ().Item1;
					selectedDataSource = results.First ().Item2;
					selectedItem = 0;
					QueueDraw ();
				}
				return true;
			case Gdk.Key.End:
				if (results.Any ()) {
					selectedCategory = results.Last ().Item1;
					selectedDataSource = results.Last ().Item2;
					selectedItem = selectedDataSource.ItemCount - 1;
					QueueDraw ();
				}
				return true;
			
			case Gdk.Key.Return:
				OnItemActivated (EventArgs.Empty);
				return true;
			}
			return false;
		}

		public event EventHandler ItemActivated;

		protected virtual void OnItemActivated (EventArgs e)
		{
			EventHandler handler = this.ItemActivated;
			if (handler != null)
				handler (this, e);
		}

		public DomRegion SelectedItemRegion {
			get {
				if (selectedDataSource == null || selectedItem < 0 || selectedItem >= selectedDataSource.ItemCount)
					return DomRegion.Empty;
				return selectedDataSource.GetRegion (selectedItem);
			}
		}

		SearchCategory selectedCategory;
		ISearchDataSource selectedDataSource;
		int selectedItem;

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.LineWidth = 1;
				context.Color = new Cairo.Color (1, 1, 1);

				context.Rectangle (evnt.Area.X, evnt.Area.Y, evnt.Area.Width, evnt.Area.Height);
				context.Fill ();

				double x = xMargin, y = yMargin;
				int w, h;
				foreach (var result in results) {
					var category = result.Item1;
					var dataSrc = result.Item2;
					if (dataSrc.ItemCount == 0)
						continue;
					headerLayout.SetText (category.Name);
					headerLayout.GetPixelSize (out w, out h);
					context.MoveTo (100 - w, y);
					context.Color = headerColor;
					PangoCairoHelper.ShowLayout (context, headerLayout);

					for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
						context.Color = new Cairo.Color (0, 0, 0);
						layout.SetMarkup ("<span foreground=\"#808080\">" + dataSrc.GetMarkup (i, false) +"\n<small>\t"+dataSrc.GetDescriptionMarkup (i, false) +"</small>" + "</span>");
						layout.GetPixelSize (out w, out h);
						if (selectedCategory == category && selectedItem == i) {
							context.Color = new Cairo.Color (0.8, 0.8, 0.8);
							context.Rectangle (x + 100, y, evnt.Area.Width - 100, h);
							context.Fill ();
							context.Color = new Cairo.Color (1, 1, 1);
						}

						context.MoveTo (x + 100, y);
						PangoCairoHelper.ShowLayout (context, layout);

						y += h;
					}
					if (result != results.Last ()) {
						context.MoveTo (0, y + categorySeparatorHeight / 2 + 0.5);
						context.LineTo (Allocation.Width, y + categorySeparatorHeight / 2 + 0.5);
						context.Color = (HslColor)Style.Mid (StateType.Normal);
						context.Stroke ();
						y += categorySeparatorHeight;
					}
				}
			}
			return base.OnExposeEvent (evnt);
		}
	}
}

