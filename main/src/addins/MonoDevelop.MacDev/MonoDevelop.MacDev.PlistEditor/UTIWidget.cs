// 
// UTIWidget.cs
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
using System.Linq;
using System.Text;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class UTIWidget : Gtk.Bin
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
		
		const string DescriptionKey = "UTTypeDescription";
		const string TypeIdentifierKey = "UTTypeIdentifier";
		const string LargeIconKey = "UTTypeSize320IconFile";
		const string SmallIconKey = "UTTypeSize64IconFile";
		const string ConformsToKey = "UTTypeConformsTo";
		
		public UTIWidget (Project proj, PDictionary dict)
		{
			if (dict == null)
				throw new ArgumentNullException ("dict");
			this.dict = dict;
			this.Build ();
			dict.Changed += HandleDictChanged;
			
			
			iconPickerLarge.Project = iconPickerSmall.Project = proj;
			iconPickerLarge.DefaultFilter = iconPickerSmall.DefaultFilter = "*.png";
			iconPickerLarge.EntryIsEditable = iconPickerSmall.EntryIsEditable = true;
			iconPickerLarge.DialogTitle = iconPickerSmall.DialogTitle = GettextCatalog.GetString ("Select icon...");
			
			entryDescription.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.SetString (DescriptionKey, entryDescription.Text);
				UpdateExpanderLabel ();
				dict.Changed += HandleDictChanged;
			};
			
			entryIdentifier.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.SetString (TypeIdentifierKey, entryIdentifier.Text);
				dict.Changed += HandleDictChanged;
			};
			
			iconPickerSmall.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.SetString (SmallIconKey, iconPickerSmall.SelectedFile);
				dict.Changed += HandleDictChanged;
			};
			
			iconPickerLarge.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.SetString (LargeIconKey, iconPickerLarge.SelectedFile);
				dict.Changed += HandleDictChanged;
			};
			
			entryConformsTo.Changed += delegate {
				if (inUpdate)
					return;
				dict.Changed -= HandleDictChanged;
				dict.GetArray (ConformsToKey).AssignStringList (entryConformsTo.Text);
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
				Expander.ContentLabel = dict.Get<PString> (DescriptionKey) ?? "";
		}
		
		bool inUpdate = false;
		void Update ()
		{
			inUpdate = true;
			
			entryDescription.Text = dict.Get<PString> (DescriptionKey) ?? "";
			UpdateExpanderLabel ();
			
			entryIdentifier.Text = dict.Get<PString> (TypeIdentifierKey) ?? "";
			iconPickerSmall.SelectedFile = dict.Get<PString> (SmallIconKey) ?? "";
			iconPickerLarge.SelectedFile = dict.Get<PString> (LargeIconKey) ?? "";
			
			var conformsTo = dict.Get<PArray> (ConformsToKey);
			entryConformsTo.Text = conformsTo != null ? conformsTo.ToStringList () : "";
			
			inUpdate = false;
		}
			
	}
}

