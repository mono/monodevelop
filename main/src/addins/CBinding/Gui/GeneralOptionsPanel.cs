//
// GeneralOptionsPanel.cs
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
using System.Collections.Generic;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

namespace CBinding
{
	public partial class GeneralOptionsPanel : Gtk.Bin
	{
		ICompiler default_c_compiler;
		List<ICompiler> c_compilers = new List<ICompiler> ();
		
		ICompiler default_cpp_compiler;
		List<ICompiler> cpp_compilers = new List<ICompiler> ();
		
		public GeneralOptionsPanel ()
		{
			this.Build ();
			
			object[] compilers = AddinManager.GetExtensionObjects ("/CBinding/Compilers");
		
			foreach (ICompiler compiler in compilers) {
				if (compiler.Language == Language.C) {
					c_compilers.Add (compiler);
				} else if (compiler.Language == Language.CPP) {
					cpp_compilers.Add (compiler);
				}
			}
			
			foreach (ICompiler compiler in c_compilers)
				cCombo.AppendText (compiler.Name);
			
			foreach (ICompiler compiler in cpp_compilers)
				cppCombo.AppendText (compiler.Name);
			
			string c_compiler = PropertyService.Get<string> ("CBinding.DefaultCCompiler", new GccCompiler ().Name);
			string cpp_compiler = PropertyService.Get<string> ("CBinding.DefaultCppCompiler", new GppCompiler ().Name);
			parseSystemTagsCheck.Active = PropertyService.Get<bool> ("CBinding.ParseSystemTags", true);
			parseLocalVariablesCheck.Active = PropertyService.Get<bool> ("CBinding.ParseLocalVariables", false);
			
			foreach (ICompiler compiler in c_compilers) {
				if (compiler.Name == c_compiler) {
					default_c_compiler = compiler;
				}
			}
			
			if (default_c_compiler == null)
				default_c_compiler = new GccCompiler ();
			
			foreach (ICompiler compiler in cpp_compilers) {
				if (compiler.Name == cpp_compiler) {
					default_cpp_compiler = compiler;
				}
			}
			
			if (default_cpp_compiler == null)
				default_cpp_compiler = new GppCompiler ();
			
			int active;
			Gtk.TreeIter iter;
			Gtk.ListStore store;
			
			active = 0;
			store = (Gtk.ListStore)cCombo.Model;
			store.GetIterFirst (out iter);
			
			while (store.IterIsValid (iter)) {
				if ((string)store.GetValue (iter, 0) == default_c_compiler.Name) {
					break;
				}
				store.IterNext (ref iter);
				active++;
			}

			cCombo.Active = active;
			
			active = 0;
			store = (Gtk.ListStore)cppCombo.Model;
			store.GetIterFirst (out iter);
			
			while (store.IterIsValid (iter)) {
				if ((string)store.GetValue (iter, 0) == default_cpp_compiler.Name) {
					break;
				}
				store.IterNext (ref iter);
				active++;
			}

			cppCombo.Active = active;
		}
		
		public bool Store ()
		{
			PropertyService.Set ("CBinding.DefaultCCompiler", default_c_compiler.Name);
			PropertyService.Set ("CBinding.DefaultCppCompiler", default_cpp_compiler.Name);
			PropertyService.Set ("CBinding.ParseSystemTags", parseSystemTagsCheck.Active);
			PropertyService.Set ("CBinding.ParseLocalVariables", parseLocalVariablesCheck.Active);
			PropertyService.SaveProperties ();
			return true;
		}

		protected virtual void OnCComboChanged (object sender, System.EventArgs e)
		{
			 string activeCompiler = cCombo.ActiveText;
			
			foreach (ICompiler compiler in c_compilers) {
				if (compiler.Name == activeCompiler) {
				 	default_c_compiler = compiler;
				}
			}
			
			if (default_c_compiler == null)
				default_c_compiler = new GccCompiler ();
		}

		protected virtual void OnCppComboChanged (object sender, System.EventArgs e)
		{
			string activeCompiler = cppCombo.ActiveText;
			
			foreach (ICompiler compiler in cpp_compilers) {
				if (compiler.Name == activeCompiler) {
				 	default_cpp_compiler = compiler;
				}
			}
			
			if (default_cpp_compiler == null)
				default_cpp_compiler = new GppCompiler ();
		}
	}
	
	public class GeneralOptionsPanelBinding : OptionsPanel
	{
		private GeneralOptionsPanel panel;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			panel = new GeneralOptionsPanel ();
			return panel;
		}
		
		public override void ApplyChanges ()
		{
			panel.Store ();
		}
	}
}
