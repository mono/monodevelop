//
// GeneralOptionsPanel.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
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
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.ValaBinding
{
	public partial class GeneralOptionsPanel : Gtk.Bin
	{
		ICompiler default_vala_compiler;
		List<ICompiler> vala_compilers = new List<ICompiler> ();
		
		public GeneralOptionsPanel ()
		{
			this.Build ();
			
			object[] compilers = AddinManager.GetExtensionObjects ("/ValaBinding/Compilers");
		
			foreach (ICompiler compiler in compilers) {
				vala_compilers.Add (compiler);
			}
			
			foreach (ICompiler compiler in vala_compilers)
				valaCombo.AppendText (compiler.Name);
			
			string vala_compiler = PropertyService.Get<string> ("ValaBinding.DefaultValaCompiler", new ValaCompiler ().Name);
			
			foreach (ICompiler compiler in vala_compilers) {
				if (compiler.Name == vala_compiler) {
					default_vala_compiler = compiler;
				}
			}
			
			if (default_vala_compiler == null)
				default_vala_compiler = new ValaCompiler ();
			
			int active;
			Gtk.TreeIter iter;
			Gtk.ListStore store;
			
			active = 0;
			store = (Gtk.ListStore)valaCombo.Model;
			store.GetIterFirst (out iter);
			
			while (store.IterIsValid (iter)) {
				if ((string)store.GetValue (iter, 0) == default_vala_compiler.Name) {
					break;
				}
				store.IterNext (ref iter);
				active++;
			}

			valaCombo.Active = active;

			extraCompilerTextView.Buffer.Text = PropertyService.Get<string> ("ValaBinding.ExtraCompilerOptions");
		}
		
		public bool Store ()
		{
			PropertyService.Set ("ValaBinding.DefaultValaCompiler", default_vala_compiler.Name);
			PropertyService.Set ("ValaBinding.ExtraCompilerOptions", extraCompilerTextView.Buffer.Text);
			PropertyService.SaveProperties ();
			return true;
		}

		protected virtual void OnValaComboChanged (object sender, System.EventArgs e)
		{
			 string activeCompiler = valaCombo.ActiveText;
			
			foreach (ICompiler compiler in vala_compilers) {
				if (compiler.Name == activeCompiler) {
					 default_vala_compiler = compiler;
				}
			}
			
			if (default_vala_compiler == null)
				default_vala_compiler = new ValaCompiler ();
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
