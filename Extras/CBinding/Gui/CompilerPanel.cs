//
// CompilerPanel.cs: Allows the user to select what compiler to use for their project
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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
using System.IO;
using System.Collections;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

namespace CBinding
{
	public partial class CompilerPanel : Gtk.Bin
	{
		private CProject project;
		private object[] compilers;
		private ICompiler active_compiler;
		
		public CompilerPanel (Properties customizationObject)
		{
			this.Build ();
			
			project = customizationObject.Get<CProject> ("Project");
			
			compilers = AddinManager.GetExtensionObjects ("/CBinding/Compilers");
			
			foreach (ICompiler compiler in compilers) {
				compilerComboBox.AppendText (compiler.Name);
			}
			
			int active = 0;
			Gtk.TreeIter iter;
			Gtk.ListStore store = (Gtk.ListStore)compilerComboBox.Model;
			store.GetIterFirst (out iter);
			while (store.IterIsValid (iter)) {
				if ((string)store.GetValue (iter, 0) == project.Compiler.Name) {
					break;
				}
				store.IterNext (ref iter);
				active++;
			}

			compilerComboBox.Active = active;
			
			useCcacheCheckBox.Active = ((CProjectConfiguration)project.ActiveConfiguration).UseCcache;
			
			Update ();
		}
		
		public bool Store ()
		{
			if (project == null)
				return false;
			
			if (!active_compiler.Equals (project.Compiler)) {
				project.Compiler = active_compiler;
				project.Language = active_compiler.Language;
			}
			
			// Update use_ccache for all configurations
			foreach (CProjectConfiguration conf in project.Configurations)
				conf.UseCcache = useCcacheCheckBox.Active;

			return true;
		}

		protected virtual void OnCompilerComboBoxChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		private void Update ()
		{
			foreach (ICompiler compiler in compilers) {
				if (compilerComboBox.ActiveText == compiler.Name) {
					active_compiler = compiler;
					break;
				}
			}
			
			if (active_compiler.SupportsCcache)
				useCcacheCheckBox.Sensitive = true;
			else
				useCcacheCheckBox.Sensitive = false;
		}
	}
	
	public class CompilerPanelBinding : AbstractOptionPanel
	{
		CompilerPanel panel;
		
		public override void LoadPanelContents ()
		{
			panel = new CompilerPanel ((Properties)CustomizationObject);
			Add (panel);
		}

		
		public override bool StorePanelContents ()
		{
			return panel.Store ();
		}
	}
}
