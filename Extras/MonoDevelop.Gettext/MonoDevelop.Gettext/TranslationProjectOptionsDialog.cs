//
// TranslationProjectOptionsDialog.cs
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

namespace MonoDevelop.Gettext
{
	public partial class TranslationProjectOptionsDialog : Gtk.Dialog
	{
		TranslationProject project;
		public TranslationProjectOptionsDialog (TranslationProject project)
		{
			this.project = project;
			this.Build();
			
			TranslationProjectConfiguration config = this.project.ActiveConfiguration as TranslationProjectConfiguration;
			
			entryPackageName.Text        = config.PackageName;
			entryRelPath.Text            = config.RelPath;
			folderentrySystemPath.Path   = config.AbsPath;
			radiobuttonRelPath.Active    = config.OutputType == TranslationOutputType.RelativeToOutput;
			radiobuttonSystemPath.Active = config.OutputType == TranslationOutputType.SystemPath;
			
			entryPackageName.Changed += new EventHandler (UpdateInitString);
			entryRelPath.Changed += new EventHandler (UpdateInitString);
			folderentrySystemPath.PathChanged  += new EventHandler (UpdateInitString);
			radiobuttonRelPath.Activated += new EventHandler (UpdateInitString);
			radiobuttonSystemPath.Activated += new EventHandler (UpdateInitString);
			
			UpdateInitString (this, EventArgs.Empty);
			this.buttonOk.Clicked += delegate {
				config.PackageName = entryPackageName.Text;
				config.RelPath = entryRelPath.Text;
				config.AbsPath = folderentrySystemPath.Path;
				if (radiobuttonRelPath.Active) {
					config.OutputType = TranslationOutputType.RelativeToOutput;
				} else {
					config.OutputType = TranslationOutputType.SystemPath;
				}
				this.Destroy ();
			};
			this.buttonCancel.Clicked += delegate {
				this.Destroy ();
			};
		}
		
		void UpdateInitString (object sender, EventArgs e)
		{
			string path;
			if (radiobuttonRelPath.Active) {
				path = "./" + entryRelPath.Text;
			} else {
				path = folderentrySystemPath.Path;
			}
			labelInitString.Text = "Mono.Unix.Catalog.Init (\"" + entryPackageName.Text + "\", \"" + path + "\");";
		}
		
	}
}
