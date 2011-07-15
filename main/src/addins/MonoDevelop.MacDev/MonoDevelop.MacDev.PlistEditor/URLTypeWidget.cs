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
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class URLTypeWidget : Gtk.Bin
	{
		readonly PDictionary dict;
		
		ClosableExpander expander;
		public ClosableExpander Expander {
			get {
				return expander;
			}
			set {
				expander = value;
				UpdateExpanderLabel ();
			}
		}
		
		const string UrlNameKey = "CFBundleURLName";
		const string UrlShemesKey = "CFBundleURLSchemes";
		const string IconKey = "CFBundleURLIconFile";
		const string TypeKey = "CFBundleTypeRole";
		
			
		public URLTypeWidget (Project proj, PDictionary dict)
		{
			if (dict == null)
				throw new ArgumentNullException ("dict");
			this.dict = dict;
			dict.Changed += HandleDictChanged;
			this.Build ();
			
			iconPicker.Project = proj;
			iconPicker.DefaultFilter = "*.png";
			iconPicker.EntryIsEditable = true;
			iconPicker.DialogTitle = GettextCatalog.GetString ("Select icon...");
			
			imagechooser.PictureSize  = new Gdk.Size (58, 58);
			
			comboboxType.AppendText ("Viewer");
			comboboxType.AppendText ("Editor");
			comboboxType.AppendText ("None");
			
			entryIdentifier.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.GetString (UrlNameKey).SetValue (entryIdentifier.Text);
				UpdateExpanderLabel ();
				dict.Changed += HandleDictChanged;
			};
			
			iconPicker.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.GetString (IconKey).SetValue (iconPicker.SelectedFile);
				dict.Changed += HandleDictChanged;
			};
			
			comboboxType.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.GetString (TypeKey).SetValue (comboboxType.ActiveText);
				dict.Changed += HandleDictChanged;
			};
			
			entryUrlShemes.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.GetArray (UrlShemesKey).AssignStringList (entryUrlShemes.Text);
				dict.Changed += HandleDictChanged;
			};
			
			customProperiesWidget.NSDictionary = dict;
			Update ();
			
		}
		
		void HandleDictChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		void UpdateExpanderLabel ()
		{
			if (Expander != null)
				Expander.ContentLabel = dict.Get<PString> (UrlNameKey) ?? "";
		}
		
		bool inUpdate = false;
		void Update ()
		{
			inUpdate = true;
			
			entryIdentifier.Text = dict.Get<PString> (UrlNameKey) ?? "";
			UpdateExpanderLabel ();
			
			var urlShemes = dict.Get<PArray> (UrlShemesKey);
			entryUrlShemes.Text = urlShemes != null ? urlShemes.ToStringList () : "";
			
			iconPicker.SelectedFile = dict.Get<PString> (IconKey) ?? "";
			
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
