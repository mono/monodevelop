// 
// AddControllerDialog.cs
//  
// Author:
//       Piotr Dowgiallo <sparekd@gmail.com>
// 
// Copyright (c) 2012 Piotr Dowgiallo
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.AspNet.Projects;

namespace MonoDevelop.AspNet.Commands
{
	class AddControllerDialog : Dialog
	{
		IList<string> loadedTemplateList;
		System.CodeDom.Compiler.CodeDomProvider provider;

		Button buttonCancel;
		Button buttonOk;
		Entry nameEntry;
		ComboBox templateCombo;

		public string ControllerName {
			get	{
				return nameEntry.Text;
			}
			set {
				nameEntry.Text = value ?? "";
			}
		}

		public string TemplateFile {
			get	{
				return loadedTemplateList[templateCombo.Active];
			}
		}

		public AddControllerDialog (AspNetAppProject project)
		{
			Build ();

			provider = project.LanguageBinding.GetCodeDomProvider ();

			loadedTemplateList = project.GetCodeTemplates ("AddController");
			bool foundEmptyTemplate = false;
			int templateIndex = 0;
			foreach (string file in loadedTemplateList) {
				string name = System.IO.Path.GetFileNameWithoutExtension (file);
				templateCombo.AppendText (name);
				if (!foundEmptyTemplate) {
					if (name == "Empty") {
						templateCombo.Active = templateIndex;
						foundEmptyTemplate = true;
					} else
						templateIndex++;
				}
			}
			if (!foundEmptyTemplate)
				throw new Exception ("The Empty.tt template is missing.");

			nameEntry.Text = "Controller";
			nameEntry.Position = 0;

			Validate ();
		}

		void Build ()
		{
			DefaultWidth = 400;
			DefaultHeight = 150;
			BorderWidth = 6;
			Resizable = false;

			buttonCancel = new Button (Stock.Cancel);
			AddActionWidget (buttonCancel, ResponseType.Cancel);
			buttonOk = new Button (Stock.Ok);
			AddActionWidget (buttonOk, ResponseType.Ok);

			nameEntry = new Entry ();
			templateCombo = ComboBox.NewText ();

			var nameLabel = new Label {
				TextWithMnemonic = GettextCatalog.GetString ("_Name:"),
				Xalign = 0,
				MnemonicWidget = nameEntry
			};
			var templateLabel = new Label {
				TextWithMnemonic = GettextCatalog.GetString ("_Template:"),
				Xalign = 0,
				MnemonicWidget = templateCombo
			};

			var table = new Table (2, 2, false) {
				RowSpacing = 6,
				ColumnSpacing = 6
			};

			table.Attach (nameLabel, 0, 1, 0, 1, AttachOptions.Fill, 0, 0, 0);
			table.Attach (nameEntry, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, 0, 0, 0);
			table.Attach (templateLabel, 0, 1, 1, 2, AttachOptions.Fill, 0, 0, 0);
			table.Attach (templateCombo, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, 0, 0, 0);
			VBox.PackStart (table);

			Child.ShowAll ();
		}

		protected virtual void Validate (object sender, EventArgs e)
		{
			Validate ();
		}

		void Validate ()
		{
			buttonOk.Sensitive = IsValidName (ControllerName);
		}

		bool IsValidName (string name)
		{
			return !String.IsNullOrEmpty (name) && provider.IsValidIdentifier (name);
		}

		public bool IsValid()
		{
			return IsValidName (ControllerName);
		}
	}
}

