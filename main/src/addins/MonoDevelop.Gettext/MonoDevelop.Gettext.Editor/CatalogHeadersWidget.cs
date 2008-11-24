//
// CatalogHeadersWidget.cs
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

namespace MonoDevelop.Gettext.Editor
{
	
	partial class CatalogHeadersWidget : Bin
	{
		Catalog headers;
		bool inUpdate = false;
		public event EventHandler PluralDefinitionChanged;
		
		public CatalogHeadersWidget ()
		{
			this.Build ();
			this.textviewComments.Buffer.Changed += OnHeaderChanged;
			UpdateGui ();
		}

		internal Catalog CatalogHeaders {
			get { 
				return headers; 
			}
			set {
				headers = value;
				UpdateGui ();
			}
		}
		
		void UpdateGui ()
		{
			inUpdate = true;
			if (headers == null) {
				inUpdate = false;
				entryProjectName.Text = entryProjectVersion.Text = "";
				entryPluralsForms.Text = entryBugzilla.Text = labelPotCreation.Text = labelPoLastModification.Text =  "";
				return;
			}
			
			// project tab
			textviewComments.Buffer.Clear ();
			textviewComments.Buffer.InsertAtCursor (headers.CommentForGui);
			
			string project = String.Empty;
			string version = String.Empty;
			
			if (!String.IsNullOrEmpty (headers.Project)) {
				int pos = headers.Project.LastIndexOf (' ');
				if (pos != -1) {
					project = headers.Project.Substring (0, pos).TrimEnd (' ', '\t');
					version = headers.Project.Substring (pos + 1);
				} else {
					project = headers.Project;
				}
			}
			
			entryProjectName.Text = project;
			entryProjectVersion.Text = version;
			
			entryBugzilla.Text = headers.GetHeader ("Report-Msgid-Bugs-To");
			labelPotCreation.Text = "<b>" + headers.CreationDate + "</b>";
			labelPoLastModification.Text = "<b>" + headers.RevisionDate + "</b>";
			labelPotCreation.UseMarkup = labelPoLastModification.UseMarkup = true;
			
			// language tab
			
			entryTranslatorName.Text = headers.Translator == null ? String.Empty : headers.Translator;
			entryTranslatorEmail.Text = headers.TranslatorEmail == null ? String.Empty : headers.TranslatorEmail;
			
			entryLanguageGroupName.Text = headers.Team;
			entryLanguageGroupEmail.Text = headers.TeamEmail;
			
			if (headers.HasHeader ("Plural-Forms"))
				entryPluralsForms.Text = headers.GetHeader ("Plural-Forms");
			
			//comboboxentryCharset.TextColumn.ActiveText = headers.Charset;
			
			// other headers
			this.ShowAll ();
			inUpdate = false;
		}

		protected virtual void OnButtonPluralsHelpClicked (object sender, System.EventArgs e)
		{
			// TODO: show hints + examples
		}

		void OnHeaderChanged (object sender, System.EventArgs e)
		{
			if (inUpdate)
				return;
			headers.CommentForGui = textviewComments.Buffer.Text;
			headers.Project = (entryProjectName.Text + ' ' + entryProjectVersion.Text).Trim ();
			headers.SetHeader ("Report-Msgid-Bugs-To", entryBugzilla.Text);
			headers.Translator = entryTranslatorName.Text;
			headers.TranslatorEmail = entryTranslatorEmail.Text;
			headers.Team = entryLanguageGroupName.Text;
			headers.TeamEmail = entryLanguageGroupEmail.Text;
			
			if (!String.IsNullOrEmpty (entryPluralsForms.Text)) {
				PluralFormsCalculator calc = new PluralFormsCalculator ();
				PluralFormsScanner scanner = new PluralFormsScanner (entryPluralsForms.Text);
				PluralFormsParser parser = new PluralFormsParser (scanner);
				bool wellFormed = parser.Parse (calc);
				
				if (wellFormed) {
					for (int i = 0; i < headers.PluralFormsCount; i++) {
						int example = 0;
						for (example = 1; example < 1000; example++) {
							if (calc.Evaluate (example) == i)
								break;
						}
						
						if (example == 1000 && calc.Evaluate (0) == i)
							example = 0;
						
						if (i > 0 && (example == 0 || example == 1000)) {
							wellFormed = false;
							break;
						}
					}
				}
			
				Gdk.Color background = wellFormed ? new Gdk.Color (138, 226,52) : new Gdk.Color (204, 0, 0);
				entryPluralsForms.ModifyBase (StateType.Normal, background); //from tango palete - 8ae234 green, cc0000 red
				if (wellFormed) {
					headers.SetHeaderNotEmpty ("Plural-Forms", entryPluralsForms.Text);
					OnPluralDefinitionChanged ();
				}
			} else {
				entryPluralsForms.ModifyBase (StateType.Normal);
				headers.SetHeaderNotEmpty ("Plural-Forms", entryPluralsForms.Text);
				OnPluralDefinitionChanged ();
			}
			headers.UpdateHeaderDict ();
			headers.IsDirty = true;
		}
		
		void OnPluralDefinitionChanged ()
		{
			if (PluralDefinitionChanged != null)
				this.PluralDefinitionChanged (this, EventArgs.Empty);
		}
	}
}
