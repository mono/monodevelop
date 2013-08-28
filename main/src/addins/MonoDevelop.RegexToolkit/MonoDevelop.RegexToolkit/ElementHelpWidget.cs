// 
// ElementHelpWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.RegexToolkit
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class ElementHelpWidget : Gtk.Bin
	{
		TreeStore elementsStore;
//		IWorkbenchWindow workbenchWindow;
//		RegexToolkitWidget regexWidget;

		public ElementHelpWidget (IWorkbenchWindow workbenchWindow, RegexToolkitWidget regexWidget)
		{
//			this.workbenchWindow = workbenchWindow;
//			this.regexWidget = regexWidget;
			this.Build ();
			
			elementsStore = new Gtk.TreeStore (typeof(string), typeof(string), typeof(string), typeof(string));
			this.elementsTreeview.Model = this.elementsStore;
			this.elementsTreeview.HeadersVisible = false;
			this.elementsTreeview.Selection.Mode = SelectionMode.Browse;
			
			var col = new TreeViewColumn ();
			this.elementsTreeview.AppendColumn (col);
			var pix = new CellRendererPixbuf ();
			var cellRendText = new CellRendererText ();
			
			col.PackStart (pix, false);
			col.AddAttribute (pix, "stock_id", 0);
			col.PackStart (cellRendText, false);
			
			col.AddAttribute (cellRendText, "text", 1);
			
			var cellRendText2 = new CellRendererText ();
			col.PackStart (cellRendText2, true);
			col.SetCellDataFunc (cellRendText2, ElementDescriptionFunc);
			
			
//			this.elementsTreeview.Selection.Changed += delegate {
//				ShowTooltipForSelectedEntry ();
//			};
//			this.elementsTreeview.MotionNotifyEvent += HandleMotionNotifyEvent;
			
			this.elementsTreeview.RowActivated += delegate (object sender, RowActivatedArgs e) {
				Gtk.TreeIter iter;
				if (elementsStore.GetIter (out iter, e.Path)) {
					string text = elementsStore.GetValue (iter, 3) as string;
					if (!System.String.IsNullOrEmpty (text)) {
						regexWidget.InsertText (text);
						workbenchWindow.SwitchView (0);
					}
				}
			};
			this.LeaveNotifyEvent += delegate {
				this.HideTooltipWindow ();
			};
			FillElementsBox ();
			Show ();
		}
		
		void ElementDescriptionFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			string str = (string)model.GetValue (iter, 2);
			if (string.IsNullOrEmpty (str)) {
				cell.Visible = false;
				return;
			}
			CellRendererText txtRenderer = (CellRendererText)cell;
			txtRenderer.Visible = true;
			txtRenderer.Text = str;
		}
		
//		CustomTooltipWindow tooltipWindow = null;

		void FillElementsBox ()
		{
			Stream stream = typeof(RegexToolkitWidget).Assembly.GetManifestResourceStream ("RegexElements.xml");
			if (stream == null)
				return;
			XmlReader reader = new XmlTextReader (stream);
			while (reader.Read ()) {
				if (reader.NodeType != XmlNodeType.Element)
					continue;
				switch (reader.LocalName) {
				case "Group":
					TreeIter groupIter = this.elementsStore.AppendValues (Gtk.Stock.Info,
						GettextCatalog.GetString (reader.GetAttribute ("_name")), "", "");
					while (reader.Read ()) {
						if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "Group") 
							break;
						switch (reader.LocalName) {
						case "Element":
							this.elementsStore.AppendValues (groupIter, null, 
							        	GettextCatalog.GetString (reader.GetAttribute ("_name")),
									GettextCatalog.GetString (reader.GetAttribute ("_description")),
									reader.ReadElementString ());
							break;
						}
					}
					break;
				}
			}
		}

		public void HideTooltipWindow ()
		{
//			if (tooltipWindow != null) {
//				tooltipWindow.Destroy ();
//				tooltipWindow = null;
//			}
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (elementsStore != null) {
				elementsStore.Dispose ();
				elementsStore = null;
			}
			
			HideTooltipWindow ();
		}
		
//		int ox = -1, oy = -1;
//
//		[GLib.ConnectBefore]
//		void HandleMotionNotifyEvent (object o, MotionNotifyEventArgs args)
//		{
//			TreeIter iter;
//				
//			if (!elementsTreeview.Selection.GetSelected (out iter))
//				return;
//			Gdk.Rectangle rect = elementsTreeview.GetCellArea (elementsStore.GetPath (iter), elementsTreeview.GetColumn (0));
//			int x, y;
//			this.GdkWindow.GetOrigin (out x, out y);
//			x += rect.X;
//			y += rect.Y;
//			if (this.tooltipWindow == null || ox != x || oy != y) {
//				ShowTooltipForSelectedEntry ();
//				ox = x;
//				oy = y;
//			}
//		}
//		
//		void ShowTooltipForSelectedEntry ()
//		{
//			TreeIter iter;
//			if (elementsTreeview.Selection.GetSelected (out iter)) {
//				string description = elementsStore.GetValue (iter, 2) as string;
//				if (!String.IsNullOrEmpty (description)) {
//					Gdk.Rectangle rect = elementsTreeview.GetCellArea (elementsStore.GetPath (iter), elementsTreeview.GetColumn (1));
//					int wx, wy, wy2; 
//					elementsTreeview.TranslateCoordinates (this, rect.X, rect.Bottom, out wx, out wy);
//					elementsTreeview.TranslateCoordinates (this, rect.X, rect.Y, out wx, out wy2);
//					ShowTooltip (description, wx, wy, wy2);
//				} else {
//					HideTooltipWindow ();
//				}
//			} else {
//				HideTooltipWindow ();
//			}
//		}
//		
//		const int tooltipXOffset = 100;
//		
//		public void ShowTooltip (string text, int x, int y, int altY)
//		{
//			if (tooltipWindow != null) {
//				tooltipWindow.Hide ();
//			} else {
//				tooltipWindow = new CustomTooltipWindow ();
//				tooltipWindow.DestroyWithParent = true;
//			}
//			tooltipWindow.Tooltip = text;
//			int ox, oy;
//			elementsTreeview.GdkWindow.GetOrigin (out ox, out oy);
//			int w = tooltipWindow.Child.SizeRequest ().Width;
//			int h = tooltipWindow.Child.SizeRequest ().Height;
//			
//			Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtWindow (this.GdkWindow));
//			if (oy + altY > geometry.Bottom) {
//				tooltipWindow.Move (ox, altY - h);
//			} else {
//				tooltipWindow.Move (ox, oy + y - h / 2);
//			}
//			tooltipWindow.ShowAll ();
//		}
//			
//		public class CustomTooltipWindow : MonoDevelop.Components.TooltipWindow
//		{
//			string tooltip;
//
//			public string Tooltip {
//				get {
//					return tooltip;
//				}
//				set {
//					tooltip = value;
//					label.Text = tooltip;
//				}
//			}
//			
//			Label label = new Label ();
//
//			public CustomTooltipWindow ()
//			{
//				label.Xalign = 0;
//				label.Xpad = 3;
//				label.Ypad = 3;
//				Add (label);
//			}
//		}
		
		

	}
}

