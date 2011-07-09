// 
// PListEditorWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.MacDev.Plist
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PListEditorWidget : Gtk.Bin
	{
		TreeStore treeStore = new TreeStore (typeof(string), typeof (PlistObjectBase));
		PlistDocument plistDocument;
		
		static Dictionary<string, string> keyTable = new Dictionary<string, string> ();
		
		public PlistDocument PlistDocument {
			get {
				return plistDocument;
			}
			set {
				plistDocument = value;
				RefreshTree ();
			}
		}
		
		static PListEditorWidget ()
		{
			// Recommended keys for iOS applications
			keyTable["CFBundleDevelopmentRegion"] = GettextCatalog.GetString ("Localization development region");
			keyTable["CFBundleDisplayName"] = GettextCatalog.GetString ("Bundle display name");
			keyTable["CFBundleExecutable"] = GettextCatalog.GetString ("Executable file");
			keyTable["CFBundleIconFiles"] = GettextCatalog.GetString ("Icon files");
			keyTable["CFBundleIdentifier"] = GettextCatalog.GetString ("Bundle identifier");
			keyTable["CFBundleInfoDictionaryVersion"] = GettextCatalog.GetString ("InfoDictionary version");
			keyTable["CFBundlePackageType"] = GettextCatalog.GetString ("Bundle OS type code");
			keyTable["CFBundleVersion"] = GettextCatalog.GetString ("Bundle version");
			keyTable["LSRequiresIPhoneOS"] = GettextCatalog.GetString ("iPhone OS required");
			keyTable["NSMainNibFile"] = GettextCatalog.GetString ("Main nib file name");
			
			
		}
		
		public PListEditorWidget ()
		{
			this.Build ();
			treeview1.AppendColumn (GettextCatalog.GetString ("Key"), new CellRendererText (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererText)cell;
				string key = (string)tree_model.GetValue (iter, 0);
				string txt;
				if (!keyTable.TryGetValue (key, out txt))
					txt = key;
				renderer.Text = txt;
			});
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Type"), new CellRendererText (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererText)cell;
				var obj      = (PlistObjectBase)tree_model.GetValue (iter, 1);
				renderer.ForegroundGdk = Style.Text (StateType.Insensitive);
				renderer.Text = obj != null ? obj.ObjectTypeString : "";
			});
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Value"), new CellRendererProperty (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererProperty)cell;
				var obj      = (PlistObjectBase)tree_model.GetValue (iter, 1);
				obj.RenderValue (this, renderer);
			});	
			treeview1.EnableGridLines = TreeViewGridLines.Horizontal;
			treeview1.Model = treeStore;
		}
		
		void RefreshTree ()
		{
			treeStore.Clear ();
			if (plistDocument != null)
				plistDocument.AddToTree (treeStore, Gtk.TreeIter.Zero);
		}
		
		public class CellRendererProperty : CellRenderer
		{
			public object RenderValue {
				get;
				set;
			}
			
			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				using (var layout = new Pango.Layout (widget.PangoContext)) {
					layout.SetMarkup (RenderValue.ToString ());
					layout.Width = -1;
					layout.GetPixelSize (out width, out height);
					width += (int)Xpad * 2;
					height += (int)Ypad * 2;
					
					x_offset = y_offset = 0;
				}
			}
			
			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				using (var layout = new Pango.Layout (widget.PangoContext)) {
					layout.SetMarkup (RenderValue.ToString ());
					layout.Width = -1;
					int width, height;
					layout.GetPixelSize (out width, out height);
					
					int x = (int) (cell_area.X + Xpad);
					int y = cell_area.Y + (cell_area.Height - height) / 2;
					
					StateType state;
					if (flags.HasFlag (CellRendererState.Selected)) {
						state = StateType.Selected;
					} else {
						state = Sensitive ? StateType.Normal : StateType.Insensitive;
					}
					
					window.DrawLayout (widget.Style.TextGC (state), x, y, layout);
				}
			}
		}

	}
}

