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
		const int itemSeparatorHeight = 2;
		const int marginIconSpacing = 4;
		const int iconTextSpacing = 6;
		const int categorySeparatorHeight = 8;
		const int headerMarginSize = 100;

		List<SearchCategory> categories = new List<SearchCategory> ();
		List<Tuple<SearchCategory, ISearchDataSource>> results = new List<Tuple<SearchCategory, ISearchDataSource>> ();
		Pango.Layout layout, headerLayout;
		CancellationTokenSource src;
		Cairo.Color headerColor;
		Cairo.Color separatorLine;
		Cairo.Color darkSearchBackground;
		Cairo.Color lightSearchBackground;

		SearchPopupWindow searchPopupWindow;

		bool isInSearch;
		public SearchPopupWidget (SearchPopupWindow searchPopupWindow)
		{
			this.searchPopupWindow = searchPopupWindow;
			Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonMotionMask | Gdk.EventMask.ButtonReleaseMask;
			headerColor = CairoExtensions.ParseColor ("8c8c8c");
			separatorLine = CairoExtensions.ParseColor ("dedede");
			lightSearchBackground = CairoExtensions.ParseColor ("ffffff");
			darkSearchBackground = CairoExtensions.ParseColor ("f7f7f7");

			categories.Add (new ProjectSearchCategory (this));
			categories.Add (new FileSearchCategory (this));
			layout = new Pango.Layout (PangoContext);
			headerLayout = new Pango.Layout (PangoContext);
		}

		public void Update (string searchPattern)
		{
			if (src != null)
				src.Cancel ();
			selectedItem = null;

			src = new CancellationTokenSource ();
			isInSearch = true;
			if (results.Count == 0)
				QueueDraw ();
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

			if (results.Count == categories.Count) {
				topItem = null;
				for (int i = 0; i < results.Count; i++) {
					if (results[i].Item2.ItemCount == 0)
						continue;
					if (topItem == null || topItem.DataSource.GetWeight (topItem.Item) <  results[i].Item2.GetWeight (0)) 
						topItem = new ItemIdentifier (results[i].Item1, results[i].Item2, 0);
				}
				selectedItem = topItem;

				QueueResize ();
				QueueDraw ();
				isInSearch = false;
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
					layout.SetMarkup (dataSrc.GetMarkup (i, false) + "\n<small>" + dataSrc.GetDescriptionMarkup (i, false) + "</small>");

					int w, h;
					layout.GetPixelSize (out w, out h);
					var px = dataSrc.GetIcon (i);
					if (px != null)
						w += px.Width + iconTextSpacing + marginIconSpacing;
					y += h + itemSeparatorHeight;
					maxX = Math.Max (maxX, w);
				}
			}
			requisition.Width = Math.Min (geometry.Width * 4 / 5, Math.Max (Allocation.Width, Math.Max (480, (int)maxX + headerMarginSize + xMargin * 2)));
			if (y == yMargin) {
				layout.SetMarkup (GettextCatalog.GetString ("No matches"));
				int w, h;
				layout.GetPixelSize (out w, out h);
				y += h + itemSeparatorHeight + 4;
			} else {
				y -= itemSeparatorHeight;
			}
			requisition.Height = Math.Min (geometry.Height * 4 / 5, (int)y + yMargin + (results.Count (res => res.Item2.ItemCount > 0) - 1) * categorySeparatorHeight);
		
		}

		ItemIdentifier GetItemAt (double px, double py)
		{
			double maxX = 0, y = yMargin;
				
			foreach (var result in results) {
				var category = result.Item1;
				var dataSrc = result.Item2;
				if (dataSrc.ItemCount == 0)
					continue;
				
				for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
					layout.SetMarkup (dataSrc.GetMarkup (i, false) + "\n<small>" + dataSrc.GetDescriptionMarkup (i, false) + "</small>");

					int w, h;
					layout.GetPixelSize (out w, out h);
					y += h + itemSeparatorHeight;
					if (y > py){
						return new ItemIdentifier (category, dataSrc, i);
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
			return null;
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				var item = GetItemAt (evnt.X, evnt.Y);
				if (item != null) {
					selectedItem = item;
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
				if (selectedItem == null)
					return true;
				if (selectedItem.Item > 0) {
					selectedItem = new ItemIdentifier (selectedItem.Category, selectedItem.DataSource, selectedItem.Item - 1);
				} else {
					for (int i = 1; i < results.Count; i++) {
						if (results [i].Item1 == selectedItem.Category) {
							selectedItem = new ItemIdentifier (
								results [i - 1].Item1,
								results [i - 1].Item2,
								Math.Min (maxItems, results [i - 1].Item2.ItemCount) - 1
							);
						}
					}
				}
				QueueDraw ();
				return true;
			case Gdk.Key.Down:
				if (selectedItem == null)
					return true;
				if (selectedItem.Item + 1 < Math.Min (maxItems, selectedItem.DataSource.ItemCount)) {
					selectedItem = new ItemIdentifier (selectedItem.Category, selectedItem.DataSource, selectedItem.Item + 1);
				} else {
					for (int i = 0; i < results.Count - 1; i++) {
						if (results [i].Item1 == selectedItem.Category && results [i + 1].Item2.ItemCount > 0) {
							selectedItem = new ItemIdentifier (
								results [i + 1].Item1,
								results [i + 1].Item2,
								0
							);
						}
					}
				}
				QueueDraw ();
				return true;
			case Gdk.Key.Home:
				if (results.Any ()) {
					var r = results.First (r2 => r2.Item2.ItemCount > 0);
					selectedItem = new ItemIdentifier (
						r.Item1,
						r.Item2,
						0
					);
					QueueDraw ();
				}
				return true;
			case Gdk.Key.End:
				if (results.Any ()) {
					var r = results.Last (r2 => r2.Item2.ItemCount > 0);
					selectedItem = new ItemIdentifier (
						r.Item1,
						r.Item2,
						r.Item2.ItemCount - 1
					);
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
				if (selectedItem == null || selectedItem.Item < 0 || selectedItem.Item >= selectedItem.DataSource.ItemCount)
					return DomRegion.Empty;
				return selectedItem.DataSource.GetRegion (selectedItem.Item);
			}
		}

		class ItemIdentifier {
			public SearchCategory Category { get; private set; }
			public ISearchDataSource DataSource { get; private set; }
			public int Item { get; private set; }

			public ItemIdentifier (SearchCategory category, ISearchDataSource dataSource, int item)
			{
				this.Category = category;
				this.DataSource = dataSource;
				this.Item = item;
			}
		}

		ItemIdentifier selectedItem = null, topItem = null;

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				var r = results.Where (res => res.Item2.ItemCount > 0).ToArray ();
				if (r.Any ()) {
					context.LineWidth = 1;
					context.Color = lightSearchBackground;
					context.Rectangle (evnt.Area.X, evnt.Area.Y, headerMarginSize, evnt.Area.Height);
					context.Fill ();

					context.Color = darkSearchBackground;
					context.Rectangle (evnt.Area.X + headerMarginSize, evnt.Area.Y, Allocation.Width - headerMarginSize, evnt.Area.Height);
					context.Fill ();
					context.MoveTo (0.5 + evnt.Area.X + headerMarginSize, 0);
					context.LineTo (0.5 + evnt.Area.X + headerMarginSize, Allocation.Height);
					context.Color = separatorLine;
					context.Stroke ();
				} else {
					context.Color = new Cairo.Color (1, 1, 1);
					context.Rectangle (evnt.Area.X, evnt.Area.Y, evnt.Area.Width, evnt.Area.Height);
					context.Fill ();
				}

				double y = yMargin;
				int w, h;
				if (topItem != null) {
					headerLayout.SetText (GettextCatalog.GetString ("Top result"));
					headerLayout.GetPixelSize (out w, out h);
					context.MoveTo (headerMarginSize - w - xMargin, y);
					context.Color = headerColor;
					PangoCairoHelper.ShowLayout (context, headerLayout);

					var category = topItem.Category;
					var dataSrc = topItem.DataSource;
					var i = topItem.Item;

					double x = xMargin + headerMarginSize;
					context.Color = new Cairo.Color (0, 0, 0);
					layout.SetMarkup ("<span foreground=\"#606060\">" + dataSrc.GetMarkup (i, false) +"</span><span foreground=\"#8F8F8F\" size=\"small\">\n"+dataSrc.GetDescriptionMarkup (i, false) +"</span>");
					layout.GetPixelSize (out w, out h);
					if (selectedItem != null && selectedItem.Category == category && selectedItem.Item == i) {
						context.Color = new Cairo.Color (0.8, 0.8, 0.8);
						context.Rectangle (headerMarginSize, y, evnt.Area.Width - headerMarginSize, h);
						context.Fill ();
						context.Color = new Cairo.Color (1, 1, 1);
					}

					var px = dataSrc.GetIcon (i);
					if (px != null) {
						evnt.Window.DrawPixbuf (Style.WhiteGC, px, 0, 0, (int)x + marginIconSpacing, (int)y + (h - px.Height) / 2, px.Width, px.Height, Gdk.RgbDither.None, 0, 0);
						x += px.Width + iconTextSpacing + marginIconSpacing;
					}

					context.MoveTo (x, y);
					PangoCairoHelper.ShowLayout (context, layout);

					y += h + itemSeparatorHeight;

				}

				foreach (var result in r) {
					var category = result.Item1;
					var dataSrc = result.Item2;
					if (dataSrc.ItemCount == 0)
						continue;
					if (dataSrc.ItemCount == 1 && topItem != null && topItem.DataSource == dataSrc)
						continue;
					headerLayout.SetText (category.Name);
					headerLayout.GetPixelSize (out w, out h);
					context.MoveTo (headerMarginSize - w - xMargin, y);
					context.Color = headerColor;
					PangoCairoHelper.ShowLayout (context, headerLayout);

					for (int i = 0; i < maxItems && i < dataSrc.ItemCount; i++) {
						if (topItem != null && topItem.Category == category && topItem.Item == i)
							continue;
						double x = xMargin + headerMarginSize;
						context.Color = new Cairo.Color (0, 0, 0);
						layout.SetMarkup ("<span foreground=\"#606060\">" + dataSrc.GetMarkup (i, false) +"</span><span foreground=\"#8F8F8F\" size=\"small\">\n"+dataSrc.GetDescriptionMarkup (i, false) +"</span>");
						layout.GetPixelSize (out w, out h);
						if (selectedItem != null && selectedItem.Category == category && selectedItem.Item == i) {
							context.Color = new Cairo.Color (0.8, 0.8, 0.8);
							context.Rectangle (headerMarginSize, y, evnt.Area.Width - headerMarginSize, h);
							context.Fill ();
							context.Color = new Cairo.Color (1, 1, 1);
						}

						var px = dataSrc.GetIcon (i);
						if (px != null) {
							evnt.Window.DrawPixbuf (Style.WhiteGC, px, 0, 0, (int)x + marginIconSpacing, (int)y + (h - px.Height) / 2, px.Width, px.Height, Gdk.RgbDither.None, 0, 0);
							x += px.Width + iconTextSpacing + marginIconSpacing;
						}

						context.MoveTo (x, y);
						PangoCairoHelper.ShowLayout (context, layout);

						y += h + itemSeparatorHeight;
					}
					if (result != r.Last ()) {
/*						context.MoveTo (0, y + categorySeparatorHeight / 2 + 0.5);
						context.LineTo (Allocation.Width, y + categorySeparatorHeight / 2 + 0.5);
						context.Color = (HslColor)Style.Mid (StateType.Normal);
						context.Stroke ();*/
						y += categorySeparatorHeight;
					}
				}
				if (y ==yMargin) {
					context.Color = new Cairo.Color (0, 0, 0);
					layout.SetMarkup (isInSearch ? GettextCatalog.GetString ("Searching...") : GettextCatalog.GetString ("No matches"));
					context.MoveTo (xMargin, y);
					PangoCairoHelper.ShowLayout (context, layout);
				}
			}
			return base.OnExposeEvent (evnt);
		}
	}
}

