// 
// URLTypeWidget.cs
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
using System.Text;
using System.Linq;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class URLTypeWidget : Gtk.Bin
	{
		readonly PDictionary dict;
		
		MacExpander expander;
		public MacExpander Expander {
			get {
				return expander;
			}
			set {
				expander = value;
				Update ();
			}
		}
		
		const string UrlNameKey = "CFBundleURLName";
		const string UrlShemesKey = "CFBundleURLSchemes";
		const string IconKey = "CFBundleURLIconFile";
		const string TypeKey = "CFBundleTypeRole";
		
			
		public URLTypeWidget (PDictionary dict)
		{
			if (dict == null)
				throw new ArgumentNullException ("dict");
			this.dict = dict;
			dict.Changed += HandleDictChanged;
			this.Build ();
			imagechooser.PictureSize  = new Gdk.Size (58, 58);
				
			comboboxType.AppendText ("Viewer");
			comboboxType.AppendText ("Editor");
			comboboxType.AppendText ("None");
			
			entryIdentifier.Changed += delegate {
				if (!inUpdate)
					GetString (UrlNameKey).SetValue (entryIdentifier.Text);
			};
			comboboxentryIcon.Entry.Changed += delegate {
				if (!inUpdate)
					GetString (IconKey).SetValue (comboboxentryIcon.Entry.Text);
			};
			comboboxType.Changed += delegate {
				if (!inUpdate)
					GetString (TypeKey).SetValue (comboboxType.ActiveText);
			};
			entryUrlShemes.Changed += HandleEntryUrlShemesChanged;;
			
			customProperiesWidget.NSDictionary = dict;
			Update ();
		}
		
		PString GetString (string key)
		{
			var result = dict.Get<PString> (key);
			if (result == null) {
				dict.Value[key] = result = new PString (null);
				result.Parent = dict;
				dict.QueueRebuild ();
			}
			return result;
		}
		
		void HandleEntryUrlShemesChanged (object sender, EventArgs e)
		{
			if (inUpdate)
				return;
			dict.Changed -= HandleDictChanged;
			var contentTypes = dict.Get<PArray> (UrlShemesKey);
			if (contentTypes == null) {
				dict.Value[UrlShemesKey] = contentTypes = new PArray ();
				contentTypes.Parent = dict;
				dict.QueueRebuild ();
			}
			contentTypes.Value.Clear ();
			string[] types = entryUrlShemes.Text.Split (',', ' ');
			foreach (var type in types) {
				if (string.IsNullOrEmpty (type))
					continue;
				contentTypes.Value.Add (new PString (type));
			}
			contentTypes.QueueRebuild ();
			dict.Changed += HandleDictChanged;
		}
		
		void HandleDictChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		bool inUpdate = false;
		void Update ()
		{
			inUpdate = true;
			
			entryIdentifier.Text = dict.Get<PString> (UrlNameKey) ?? "";
			if (Expander != null)
				Expander.ContentLabel = entryIdentifier.Text;
			
			var sb = new StringBuilder ();
			var urlShemes = dict.Get<PArray> (UrlShemesKey);
			if (urlShemes != null) {
				foreach (PString str in urlShemes.Value.Where (o => o is PString)) {
					if (sb.Length > 0)
						sb.Append (", ");
					sb.Append (str);
				}
			}
			entryUrlShemes.Text = sb.ToString ();
			
			comboboxentryIcon.Entry.Text = dict.Get<PString> (IconKey) ?? "";
			
			switch (dict.Get<PString> (TypeKey) ?? "") {
			case "Viewer":
				comboboxType.Active = 0;
				break;
			case "Editor":
				comboboxType.Active = 1;
				break;
			case "None":
				comboboxType.Active = 2;
				break;
			default:
				comboboxType.Active = 2;
				break;
			}
			
			
			inUpdate = false;
		}
		
		
	}
}

