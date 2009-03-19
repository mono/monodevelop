// 
// EditTemplateDialog.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeTemplates
{
	
	
	public partial class EditTemplateDialog : Gtk.Dialog
	{
		CodeTemplate template;
		Mono.TextEditor.TextEditor textEditor = new Mono.TextEditor.TextEditor ();
		
		public EditTemplateDialog (CodeTemplate template, bool isNew)
		{
			//this.Modal = true;
			this.Title = isNew ? GettextCatalog.GetString ("New template") : GettextCatalog.GetString ("Edit template");
			this.Build();
			this.template = template;
			this.entryShortcut1.Text = template.Shortcut;
			this.comboboxentryGroups.Entry.Text = template.Group;
			this.comboboxentryMime.Entry.Text = template.MimeType;
			this.entryDescription.Text = template.Description;
			this.textEditor.Document.MimeType = template.MimeType;
			this.textEditor.Document.Text = template.Code;
			
			checkbuttonExpansion.Active = (template.CodeTemplateType & CodeTemplateType.Expansion) == CodeTemplateType.Expansion;
			checkbuttonSurroundWith.Active = (template.CodeTemplateType & CodeTemplateType.SurroundsWith) == CodeTemplateType.SurroundsWith;
			
			scrolledwindow1.Child = textEditor;
			textEditor.ShowAll ();
			Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions ();
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = false;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			textEditor.Options = options;
			
			HashSet<string> mimeTypes = new HashSet<string> ();
			HashSet<string> groups    = new HashSet<string> ();
			foreach (CodeTemplate ct in CodeTemplateService.Templates) {
				mimeTypes.Add (ct.MimeType);
				groups.Add (ct.Group);
			}
			comboboxentryMime.AppendText ("");
			foreach (string mime in mimeTypes) {
				comboboxentryMime.AppendText (mime);
			}
			comboboxentryGroups.AppendText ("");
			foreach (string group in groups) {
				comboboxentryGroups.AppendText (group);
			}
			
			this.buttonEditVariables.Clicked += delegate {
				EditVariablesDialog editVariablesDialog = new EditVariablesDialog (template);
				editVariablesDialog.Run ();
				editVariablesDialog.Destroy ();
			};
			this.buttonOk.Clicked += delegate {
				template.Shortcut = this.entryShortcut1.Text;
				template.Group = this.comboboxentryGroups.Entry.Text;
				template.MimeType = this.comboboxentryMime.Entry.Text;
				template.Description = this.entryDescription.Text;
				template.Code = this.textEditor.Document.Text;
				
				template.CodeTemplateType = CodeTemplateType.Unknown;
				if (checkbuttonExpansion.Active)
					template.CodeTemplateType |= CodeTemplateType.Expansion;
				if (checkbuttonSurroundWith.Active)
					template.CodeTemplateType |= CodeTemplateType.SurroundsWith;
			};
			
			checkbuttonSurroundWith.Toggled += delegate {
				options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = checkbuttonSurroundWith.Active;
				textEditor.QueueDraw ();
			};
		}
	}
}
