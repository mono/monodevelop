// 
// DocumentTypeWidget.cs
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
using MonoDevelop.Core.Collections;
using System.Text;
using System.Linq;
using Gtk;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class DocumentTypeWidget : Gtk.Bin
	{
		readonly PDictionary dict;
		readonly ListStore iconStore = new ListStore (typeof (string));
		
		const string NameKey = "CFBundleTypeName";
		const string ContentTypesKey = "LSItemContentTypes";
		const string IconFilesKey = "CFBundleTypeIconFiles";
		
		public MacExpander Expander {
			get;
			set;
		}
		
		public DocumentTypeWidget (PDictionary dict)
		{
			if (dict == null)
				throw new ArgumentNullException ("dict");
			this.Build ();
			this.dict = dict;
			this.treeviewIcons.Model = iconStore;
			this.treeviewIcons.AppendColumn ("icon", new CellRendererText (), "text", 0);
			this.treeviewIcons.HeadersVisible = false;
			this.imagechooser1.PictureSize = new Gdk.Size (58, 58);
			dict.Changed += HandleDictChanged;			
			custompropertiesWidget.NSDictionary = dict;
			Update ();
			
			this.entryName.Changed += HandleEntryNamehandleChanged;
			this.entryContentTypes.Changed += HandleEntryContentTypeshandleChanged;
			
			this.buttonAdd.Clicked += AddIcon;
			this.buttonRemove.Clicked += RemoveIcon;
		}

		void RemoveIcon (object sender, EventArgs e)
		{
			dict.Changed -= HandleDictChanged;
			Gtk.TreeIter iter;
			if (!treeviewIcons.Selection.GetSelected (out iter))
				return;
			iconStore.Remove (ref iter);
			
			var iconFiles = dict.Get<PArray> (IconFilesKey);
			if (iconFiles == null) {
				dict.Value[ContentTypesKey] = iconFiles = new PArray ();
				iconFiles.Parent = dict;
				dict.QueueRebuild ();
			}
			
			iconFiles.Value.Clear ();
			
			if (iconStore.GetIterFirst (out iter)) {
				do {
					iconFiles.Value.Add ((PObject)(string)iconStore.GetValue (iter, 0));
				} while (iconStore.IterNext (ref iter));
			}
			iconFiles.QueueRebuild ();
			dict.Changed += HandleDictChanged;
		}

		void AddIcon (object sender, EventArgs e)
		{
			dict.Changed -= HandleDictChanged;
			var iconFiles = dict.Get<PArray> (IconFilesKey);
			if (iconFiles == null) {
				dict.Value[IconFilesKey] = iconFiles = new PArray ();
				iconFiles.Parent = dict;
				dict.QueueRebuild ();
			}
			
			// TODO: Select new Icon
			string newIcon = "new Icon";
			
			iconFiles.Value.Add (new PString (newIcon));
			iconFiles.QueueRebuild ();
			iconStore.AppendValues (newIcon);
			dict.Changed += HandleDictChanged;
		}

		void HandleDictChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		void HandleEntryNamehandleChanged (object sender, EventArgs e)
		{
			if (inUpdate)
				return;
			var typeName = dict.Get<PString> (NameKey);
			if (typeName == null) {
				dict.Value[NameKey] = typeName = new PString (null);
				typeName.Parent = dict;
				dict.QueueRebuild ();
			}
			typeName.SetValue (entryName.Text);
		}
		
		void HandleEntryContentTypeshandleChanged (object sender, EventArgs e)
		{
			if (inUpdate)
				return;
			dict.Changed -= HandleDictChanged;
			var contentTypes = dict.Get<PArray> (ContentTypesKey);
			if (contentTypes == null) {
				dict.Value[ContentTypesKey] = contentTypes = new PArray ();
				contentTypes.Parent = dict;
				dict.QueueRebuild ();
			}
			contentTypes.Value.Clear ();
			string[] types = entryContentTypes.Text.Split (',', ' ');
			foreach (var type in types) {
				if (string.IsNullOrEmpty (type))
					continue;
				contentTypes.Value.Add (new PString (type));
			}
			contentTypes.QueueRebuild ();
			dict.Changed += HandleDictChanged;
		}
		
		bool inUpdate = false;
		void Update ()
		{
			inUpdate = true;
			
			entryName.Text = dict.Get<PString> (NameKey) ?? "";
			if (Expander != null)
				Expander.ContentLabel = entryName.Text;
			
			var sb = new StringBuilder ();
			var contentTypes = dict.Get<PArray> (ContentTypesKey);
			if (contentTypes != null) {
				foreach (PString str in contentTypes.Value.Where (o => o is PString)) {
					if (sb.Length > 0)
						sb.Append (", ");
					sb.Append (str);
				}
			}
			entryContentTypes.Text = sb.ToString ();
			
			
			iconStore.Clear ();
			var iconFiles = dict.Get<PArray> (IconFilesKey);
			if (iconFiles != null) {
				foreach (PString str in iconFiles.Value.Where (o => o is PString)) {
					iconStore.AppendValues (str.Value);
				}
			}
			inUpdate = false;
		}
	}
}

