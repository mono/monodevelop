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
namespace MonoDevelop.AspNet.Mvc.Gui
{
	public partial class AddControllerDialog : Gtk.Dialog
	{
		IList<string> loadedTemplateList;
		System.CodeDom.Compiler.CodeDomProvider provider;

		#region Public Properties

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

		#endregion

		public AddControllerDialog (AspMvcProject project)
		{
			this.Build ();

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

