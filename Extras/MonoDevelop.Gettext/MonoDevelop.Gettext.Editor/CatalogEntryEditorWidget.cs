//
// CatalogEntryEditorWidget.cs
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
using System.Text;
using MonoDevelop.Core; 
using Gtk;

namespace MonoDevelop.Gettext.Editor
{
	class CatalogEntryEditorWidget : VPaned
	{
		CatalogEntry entry;
		string[] pluralDescriptions;
		bool changingEntry = false;
		
		ScrolledWindow sw;
		TextView referencesView;
		
		Table table;
		VBox vb1, vb2, vb3, vb4;
		Label l1, l2, l3, l4;
		
		ScrolledWindow swAutoComments, swComments;
		TextView tvAutoComments, tvComments;
		
		Notebook nbOriginal;
		ScrolledWindow swOriginalSingular, swOriginalPlural;
		TextView tvOriginalSingular, tvOriginalPlural;
		Label lbOriginalSingular, lbOriginalPlural;
		Notebook nbTranslations;
		ScrolledWindow[] swTranslations;
		TextView[] tvTranslations;
		Label[] lbTranslations;
		IntPtr[] spellCheckers;
		
		public CatalogEntryEditorWidget (string[] pluralDescriptions, string locale)
			: base ()
		{
			sw = new ScrolledWindow ();
			sw.VscrollbarPolicy = PolicyType.Automatic;
			sw.HscrollbarPolicy = PolicyType.Automatic;
			sw.ShadowType = ShadowType.In;
			
			referencesView = new TextView ();
			referencesView.Editable = false;
			
			sw.Add (referencesView);
			this.Add1 (sw);
			
			table = new Table (2, 2, true);
			table.ColumnSpacing = 4;
			table.RowSpacing = 4;
			
			vb1 = new VBox ();
			l1 = new Label (GettextCatalog.GetString ("Original:"));
			l1.Xalign = 0F;
			
			nbOriginal = new Notebook ();
			swOriginalSingular = new ScrolledWindow ();
			swOriginalPlural = new ScrolledWindow ();
			swOriginalSingular.VscrollbarPolicy = PolicyType.Automatic;
			swOriginalSingular.HscrollbarPolicy = PolicyType.Automatic;
			swOriginalSingular.ShadowType = ShadowType.In;
			swOriginalPlural.VscrollbarPolicy = PolicyType.Automatic;
			swOriginalPlural.HscrollbarPolicy = PolicyType.Automatic;
			swOriginalPlural.ShadowType = ShadowType.In;
			tvOriginalSingular = new TextView ();
			tvOriginalSingular.Editable = false;
			tvOriginalPlural = new TextView ();
			tvOriginalPlural.Editable = false;
			swOriginalSingular.Add (tvOriginalSingular);
			swOriginalPlural.Add (tvOriginalPlural);
			lbOriginalSingular = new Label (GettextCatalog.GetString ("Singular"));
			nbOriginal.AppendPage (swOriginalSingular, lbOriginalSingular);
			lbOriginalPlural = new Label (GettextCatalog.GetString ("Plural"));
			nbOriginal.AppendPage (swOriginalPlural, lbOriginalPlural);
			vb1.PackStart (l1, false, false, 0);
			vb1.PackStart (nbOriginal, true, true, 0);			
			vb1.ShowAll ();
			table.Attach (vb1, 0, 1, 0, 1);
			
			vb2 = new VBox ();
			l2 = new Label (GettextCatalog.GetString ("Autocomments:"));
			l2.Xalign = 0F;
			swAutoComments = new ScrolledWindow ();
			swAutoComments.VscrollbarPolicy = PolicyType.Automatic;
			swAutoComments.HscrollbarPolicy = PolicyType.Automatic;
			swAutoComments.ShadowType = ShadowType.In;
			tvAutoComments = new TextView ();
			tvAutoComments.Editable = false;
			swAutoComments.Add (tvAutoComments);
			vb2.PackStart (l2, false, false, 0);
			vb2.PackStart (swAutoComments, true, true, 0);
			vb2.ShowAll ();
			table.Attach (vb2, 1, 2, 0, 1);
			
			vb3 = new VBox ();
			l3 = new Label (GettextCatalog.GetString ("Translation:"));
			l3.Xalign = 0F;
			nbTranslations = new Notebook ();
			this.SetPluralDescriptions (pluralDescriptions, locale);
			vb3.PackStart (l3, false, false, 0);
			vb3.PackStart (nbTranslations, true, true, 0);
			vb3.ShowAll ();
			table.Attach (vb3, 0, 1, 1, 2);
						
			vb4 = new VBox ();
			l4 = new Label (GettextCatalog.GetString ("Translator comments:"));
			l4.Xalign = 0F;
			swComments = new ScrolledWindow ();
			swComments.VscrollbarPolicy = PolicyType.Automatic;
			swComments.HscrollbarPolicy = PolicyType.Automatic;
			swComments.ShadowType = ShadowType.In;
			tvComments = new TextView ();
			swComments.Add (tvComments);
			vb4.PackStart (l4, false, false, 0);
			vb4.PackStart (swComments, true, true, 0);
			vb4.ShowAll ();
			table.Attach (vb4, 1, 2, 1, 2);
			
			table.ShowAll ();
			this.Add2 (table);
			this.Sensitive = false;
		}
		
		public CatalogEntry Entry
		{
			get { return entry; }
			set
			{
				changingEntry = true;
				referencesView.Buffer.Clear ();
				
				entry = value;
				if (entry != null)
				{
					// references
					StringBuilder sb = new StringBuilder ();
					bool first = true;
					foreach (string reference in entry.References)
					{
						if (! first)
							sb.Append ('\n');
						sb.Append (reference);
						if (first)
							first = false;
					}
					referencesView.Buffer.InsertAtCursor (sb.ToString ());
					
					// autocomments
					tvAutoComments.Buffer.Clear ();
					first = true;
					sb = new StringBuilder ();
					foreach (string autocomment in entry.AutoComments)
					{
						if (! first)
							sb.Append ('\n');
						sb.Append (autocomment);
						if (first)
							first = false;
					}
					tvAutoComments.Buffer.InsertAtCursor (sb.ToString ());
					
					// translator comments
					tvComments.Buffer.Clear ();
					if (! String.IsNullOrEmpty (entry.Comment))
						tvComments.Buffer.InsertAtCursor (entry.Comment);
					
					// original strings
					tvOriginalSingular.Buffer.Clear ();
					tvOriginalSingular.Buffer.InsertAtCursor (entry.String);
					nbOriginal.CurrentPage = 0;
					if (entry.HasPlural)
					{
						nbOriginal.ShowTabs = true;
						tvOriginalPlural.Buffer.Clear ();
						tvOriginalPlural.Buffer.InsertAtCursor (entry.PluralString);
					} else
					{
						nbOriginal.ShowTabs = false;
					}
					
					// translations
					if (pluralDescriptions != null && pluralDescriptions.Length > 0)
					{	
						nbTranslations.CurrentPage = 0;
						tvTranslations[0].Buffer.Clear ();
						tvTranslations[0].Buffer.InsertAtCursor (entry.GetTranslation (0));
						if (entry.HasPlural)
						{
							nbTranslations.ShowTabs = true;
							for (int i = 1; i < pluralDescriptions.Length; i++)
							{
								tvTranslations[i].Buffer.Clear ();
								tvTranslations[i].Buffer.InsertAtCursor (entry.GetTranslation (i));
							}
						} else
						{
							nbTranslations.ShowTabs = false;
						}
					}
					
					
					if (! this.Sensitive)
						this.Sensitive = true;
					
				} else
				{
					this.Sensitive = false;
				}
				changingEntry = false;
			}
		}
		
		public void SetPluralDescriptions (string[] value, string locale)
		{
			// clean
			if (pluralDescriptions != null && pluralDescriptions.Length > 0)
			{
				for (int i = 0; i < swTranslations.Length; i++)
				{
					tvTranslations[i].Buffer.Changed -= new EventHandler (OnTranslationChanged);
					swTranslations[i].Remove (tvTranslations[i]);
					if (spellCheckers[i] != IntPtr.Zero)
						GtkSpell.Detach (spellCheckers[i]);
					tvTranslations[i].Destroy ();
					tvTranslations[i] = null;
					nbTranslations.RemovePage (i);
					swTranslations[i].Destroy ();
					swTranslations[i] = null;
					lbTranslations[i].Destroy ();
					lbTranslations[i] = null;
				}
				lbTranslations = null;
				tvTranslations = null;
				swTranslations = null;
			}
			
			pluralDescriptions = value;
			
			// create new
			if (pluralDescriptions != null && pluralDescriptions.Length > 0)
			{
				bool useSpellCheck = ! String.IsNullOrEmpty (locale) && GtkSpell.IsSupported;
				swTranslations = new ScrolledWindow[pluralDescriptions.Length];
				tvTranslations = new TextView[pluralDescriptions.Length];
				lbTranslations = new Label[pluralDescriptions.Length];
				spellCheckers = new IntPtr[pluralDescriptions.Length];
				
				for (int i = 0; i < pluralDescriptions.Length; i++)
				{
					swTranslations[i] = new ScrolledWindow ();
					swTranslations[i].VscrollbarPolicy = PolicyType.Automatic;
					swTranslations[i].HscrollbarPolicy = PolicyType.Automatic;
					swTranslations[i].ShadowType = ShadowType.In;
					tvTranslations[i] = new TextView ();
					if (useSpellCheck)
						spellCheckers[i] = GtkSpell.Attach (tvTranslations[i], locale);
					else
						spellCheckers[i] = IntPtr.Zero;
					swTranslations[i].Add (tvTranslations[i]);
					lbTranslations[i] = new Label ();
					lbTranslations[i].Text = pluralDescriptions[i];
					nbTranslations.AppendPage (swTranslations[i], lbTranslations[i]);
					tvTranslations[i].Buffer.Changed += new EventHandler (OnTranslationChanged);
				}
				nbTranslations.ShowAll ();
			}
		}
		
		void OnTranslationChanged (object sender, EventArgs args)
		{
			if (! changingEntry)
			{
				int tranIndex = -1;
				for (int i = 0; i < tvTranslations.Length; i++)
				{
					if (tvTranslations[i].Buffer == sender)
						tranIndex = i;
				}
				if (tranIndex != -1)
				{ 
					entry.SetTranslation (tvTranslations[tranIndex].Buffer.Text, tranIndex);
				}
			}
		}
		
		public override void Destroy ()
		{
			Entry = null;
			SetPluralDescriptions (null, null);
			
			vb4.Remove (l4);
			l4.Destroy ();
			swComments.Remove (tvComments);
			tvComments.Destroy ();
			vb4.Remove (swComments);
			swComments.Destroy ();
			table.Remove (vb4);
			vb4.Destroy ();
			
			vb3.Remove (nbTranslations);
			nbTranslations.Destroy ();
			vb3.Remove (l3);
			l3.Destroy ();
			table.Remove (vb3);
			vb3.Destroy ();
			
			vb2.Remove (l2);
			l2.Destroy ();
			swAutoComments.Remove (tvAutoComments);
			tvAutoComments.Destroy ();
			vb2.Remove (swAutoComments);
			swAutoComments.Destroy ();
			table.Remove (vb2);
			vb2.Destroy ();
			
			swOriginalPlural.Remove (tvOriginalPlural);
			tvOriginalPlural.Destroy ();
			swOriginalSingular.Remove (tvOriginalSingular);
			tvOriginalSingular.Destroy ();
			nbOriginal.Remove (swOriginalPlural);
			swOriginalPlural.Destroy ();
			nbOriginal.Remove (swOriginalSingular);
			swOriginalSingular.Destroy ();
			vb1.Remove (l1);
			l1.Destroy ();
			vb1.Remove (nbOriginal);
			nbOriginal.Destroy ();
			table.Remove (vb1);
			vb1.Destroy ();
			
			this.Remove (table);
			table.Destroy ();
			
			sw.Remove (referencesView);
			referencesView.Destroy ();
			this.Remove (sw);
			sw.Destroy ();
			
			base.Destroy ();
		}
	}
}
