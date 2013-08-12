//
// LanguageChooserDialog.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2007 David Makovský
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using Gtk;

namespace MonoDevelop.Gettext.Translator
{
	partial class LanguageChooserDialog : Gtk.Dialog
	{
		ListStore languagesStore, countriesStore;
		
		public LanguageChooserDialog ()
		{
			this.Build ();
			// LOOK: Why Stetic doesn't save radio buttons toggle event? Even not generating a code...
			this.radiobuttonKnown.Toggled += new EventHandler (ChangeSensitivity);
			this.radiobuttonCustom.Toggled += new EventHandler (ChangeSensitivity);
			ChangeSensitivity (null, EventArgs.Empty);
			FillLanguages ();
			FillCountries ();
			this.languageTreeView.GrabFocus ();
		}
		
		void FillLanguages ()
		{
			languagesStore = new ListStore (typeof (string), typeof (string));
			foreach (MonoDevelop.Gettext.IsoCodes.IsoCode code in MonoDevelop.Gettext.IsoCodes.KnownLanguages) {
				languagesStore.AppendValues (code.Name, code.Iso);
			}
			
			this.languagesStore.SetSortColumnId (0, SortType.Ascending);
			this.languageTreeView.Model = languagesStore;
			this.languageTreeView.AppendColumn ("", new CellRendererText (), "text", 0);
		}
		
		void FillCountries ()
		{
			countriesStore = new ListStore (typeof (string), typeof (string));
			foreach (MonoDevelop.Gettext.IsoCodes.IsoCode code in MonoDevelop.Gettext.IsoCodes.KnownCountries) {
				countriesStore.AppendValues (code.Name, code.Iso);
			}
			this.countriesStore.SetSortColumnId (0, SortType.Ascending);
			this.countryTreeView.Model = countriesStore;
			this.countryTreeView.AppendColumn ("", new CellRendererText (), "text", 0);
		}

		void ChangeSensitivity (object sender, EventArgs e)
		{
			this.countryTreeView.Sensitive = this.checkbuttonUseCoutry.Active;
			/*this.labelCountry.Sensitive = this.comboboxCountry.Sensitive = this.checkbuttonUseCoutry.Active;*/
			this.tableKnown.Sensitive = this.radiobuttonKnown.Active;
			this.hboxUser.Sensitive = this.radiobuttonCustom.Active;
			OnEntryLocaleChanged (null, EventArgs.Empty);
			if (this.radiobuttonCustom.Active)
				this.entryLocale.GrabFocus ();
		}

		protected virtual void OnEntryLocaleChanged (object sender, System.EventArgs e)
		{
			this.buttonOK.Sensitive =
				this.radiobuttonKnown.Active ||
				(this.radiobuttonCustom.Active && this.entryLocale.Text.Length > 0);
		}
				
		public string Language {
			get {
				if (this.radiobuttonKnown.Active) {
					TreeIter iter;
					if (this.languageTreeView.Selection.GetSelected (out iter)) {
						return (string)languagesStore.GetValue (iter, 1);
					}
				} else {
					string str = this.entryLocale.Text;
					if (!String.IsNullOrEmpty (str)) {
						int index = str.IndexOf ('_');
						if (index != -1) 
							return str.Substring (0, index);
						return str;
					}
				}
				return String.Empty;
			}
		}

		public string LanguageLong {
			get {
				if (this.radiobuttonKnown.Active) {
					TreeIter iter;
					if (this.languageTreeView.Selection.GetSelected (out iter)) {
						return (string)languagesStore.GetValue (iter, 0);
					}
				} else {
					string str = this.entryLocale.Text;
					if (!String.IsNullOrEmpty (str)) {
						int index = str.IndexOf ('_');
						if (index != -1) 
							return str.Substring (0, index);
						return str;
					}
				}
				return String.Empty;
			}
		}

		public bool HasCountry {
			get {
				return 
					(this.radiobuttonKnown.Active && this.checkbuttonUseCoutry.Active) ||
					(this.radiobuttonCustom.Active && this.entryLocale.Text.IndexOf ('_') != -1);
			}
		}
		
		public string Country {
			get {
				if (this.radiobuttonKnown.Active) {
					TreeIter iter;
					if (this.countryTreeView.Selection.GetSelected (out iter))
						return (string)countriesStore.GetValue (iter, 1);
				} else {
					string str = this.entryLocale.Text;
					if (! String.IsNullOrEmpty (str)) {
						int index = str.IndexOf ('_');
						if (index != -1)
							return str.Substring (index + 1);
					}
				}
				return String.Empty;
			}
		}
	}
}
