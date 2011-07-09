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
using MonoMac.Foundation;

namespace MonoDevelop.MacDev.Plist
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PListEditorWidget : Gtk.Bin
	{
		TreeStore treeStore = new TreeStore (typeof(string), typeof (NSObject));
		NSDictionary nsDictionary;
		
		static Dictionary<string, string> keyTable = new Dictionary<string, string> ();
		
		public NSDictionary NSDictionary {
			get {
				return nsDictionary;
			}
			set {
				nsDictionary = value;
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
		
		static string GetObjectTypeString (NSObject obj)
		{
			if (obj is NSArray)
				return GettextCatalog.GetString ("Array");
			if (obj is NSDictionary)
				return GettextCatalog.GetString ("Dictionary");
			
			if (obj is NSData)
				return GettextCatalog.GetString ("Data");
			
			if (obj is NSDate)
				return GettextCatalog.GetString ("Date");
			if (obj is NSNumber)
				return GettextCatalog.GetString ("Number");
			if (obj is NSString)
				return GettextCatalog.GetString ("String");
			throw new InvalidOperationException ("unknown object :" + obj.GetType ());
//			if (obj is NSBool)
//				return GettextCatalog.GetString ("Boolean");
		}
		
		static void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, NSDictionary dict)
		{
			foreach (var item in dict) {
				var key = item.Key.ToString ();
				var subIter = iter.Equals (TreeIter.Zero) ? treeStore.AppendValues (key, item.Value) : treeStore.AppendValues (iter, key, item.Value);
				if (item.Value is NSArray)
					AddToTree (treeStore, subIter, (NSArray)item.Value);
				if (item.Value is NSDictionary)
					AddToTree (treeStore, subIter, (NSDictionary)item.Value);
			}
		}
		
		static void AddToTree (Gtk.TreeStore treeStore, Gtk.TreeIter iter, NSArray arr)
		{
			for (uint i = 0; i < arr.Count; i++) {
				NSObject item = MonoMac.ObjCRuntime.Runtime.GetNSObject (arr.ValueAt (i));
				
				var txt = string.Format (GettextCatalog.GetString ("Item {0}"), i);
				var subIter = iter.Equals (TreeIter.Zero) ? treeStore.AppendValues (txt, item) : treeStore.AppendValues (iter, txt, item);
				
				if (item is NSArray)
					AddToTree (treeStore, subIter, (NSArray)item);
				if (item is NSDictionary)
					AddToTree (treeStore, subIter, (NSDictionary)item);
			}
		}
		
		
		void RenderValue (PListEditorWidget.CellRendererProperty renderer, NSObject obj)
		{
			if (obj is NSArray) {
				var arr = (NSArray)obj;
				renderer.Sensitive = false;
				renderer.RenderValue = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", (int)arr.Count), (int)arr.Count);
				return;
			}
			if (obj is NSDictionary) {
				var dict = (NSDictionary)obj;
				renderer.Sensitive = false;
				renderer.RenderValue = string.Format (GettextCatalog.GetPluralString ("({0} item)", "({0} items)", (int)dict.Count), (int)dict.Count);
				return;
			}
			renderer.Sensitive = true;
			
			if (obj is NSData) {
				renderer.RenderValue = "byte[]";
			} else if (obj is NSDate) {
				renderer.RenderValue = ((NSDate)obj).ToString ();
			} else if (obj is NSNumber) {
				renderer.RenderValue = ((NSNumber)obj).ToString ();
			} else if (obj is NSString) {
				renderer.RenderValue = ((NSString)obj).ToString ();
			}
		}
		
		public PListEditorWidget ()
		{
			this.Build ();
			treeview1.AppendColumn (GettextCatalog.GetString ("Key"), new CellRendererText (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererText)cell;
				string key = (string)tree_model.GetValue (iter, 0) ?? "";
				string txt;
				if (!keyTable.TryGetValue (key, out txt))
					txt = key;
				renderer.Text = txt;
			});
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Type"), new CellRendererText (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererText)cell;
				var obj      = (NSObject)tree_model.GetValue (iter, 1);
				renderer.ForegroundGdk = Style.Text (StateType.Insensitive);
				renderer.Text = GetObjectTypeString (obj);
			});
			
			treeview1.AppendColumn (GettextCatalog.GetString ("Value"), new CellRendererProperty (), delegate(TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter) {
				var renderer = (CellRendererProperty)cell;
				var obj      = (NSObject)tree_model.GetValue (iter, 1);
				RenderValue (renderer, obj);
			});
			treeview1.EnableGridLines = TreeViewGridLines.Horizontal;
			treeview1.Model = treeStore;
		}
		
		void RefreshTree ()
		{
			treeStore.Clear ();
			if (nsDictionary != null)
				AddToTree (treeStore, Gtk.TreeIter.Zero, nsDictionary);
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

