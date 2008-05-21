//
// GettextFeatureWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Gettext.Translator;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Gettext
{
	public partial class GettextFeatureWidget : Gtk.Bin
	{
		ListStore store;
		
		public GettextFeatureWidget()
		{
			this.Build();
			
			store = new ListStore (typeof (string), typeof (string));
			this.treeviewTranslations.Model = store;
			this.treeviewTranslations.AppendColumn ("", new CellRendererText (), "text", 0);
			this.treeviewTranslations.AppendColumn ("", new CellRendererText (), "text", 1);
			this.treeviewTranslations.HeadersVisible = false;
			
			this.buttonAdd.Clicked += delegate {
				MonoDevelop.Gettext.Translator.LanguageChooserDialog chooser = new MonoDevelop.Gettext.Translator.LanguageChooserDialog ();

				int response = 0;
				chooser.Response += delegate(object o, Gtk.ResponseArgs args) {
					response = (int)args.ResponseId;
				};
				chooser.Run ();
				
				if (response == (int)Gtk.ResponseType.Ok) {
					string language = chooser.Language + (chooser.HasCountry ? "_" + chooser.Country : "");
					store.AppendValues (chooser.LanguageLong, language);
				}
				
				chooser.Destroy ();
			};
			this.buttonRemove.Sensitive = false;
			treeviewTranslations.Selection.Changed += delegate {
				Gtk.TreeIter iter;
				this.buttonRemove.Sensitive = treeviewTranslations.Selection.GetSelected (out iter);
			};
			this.buttonRemove.Clicked += delegate {
				Gtk.TreeIter iter;
				if (treeviewTranslations.Selection.GetSelected (out iter)) {
					this.store.Remove (ref iter);
				}
			};
		}
		
		public void ApplyFeature (SolutionFolder parentCombine, SolutionItem entry)
		{
			TranslationProject newProject;
			if (entry is TranslationProject)
				newProject = (TranslationProject) entry;
			else {
				newProject = new TranslationProject ();
				newProject.Name = entry.Name + "Translation";
				string path = System.IO.Path.Combine (parentCombine.BaseDirectory, newProject.Name);
				if (!System.IO.Directory.Exists (path))
					System.IO.Directory.CreateDirectory (path);
				newProject.FileName = System.IO.Path.Combine (path, newProject.Name + ".mdse");
				parentCombine.Items.Add (newProject);
			}
			
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					string code = (string)store.GetValue (iter, 1);
					newProject.AddNewTranslation (code, new NullProgressMonitor ());
				} while (store.IterNext (ref iter));
			}
		}
	}
}
