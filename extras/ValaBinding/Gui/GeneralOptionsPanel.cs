//
// GeneralOptionsPanel.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;
using System.Collections.Generic;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

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
		}
		
		public bool Store ()
		{
			PropertyService.Set ("ValaBinding.DefaultValaCompiler", default_vala_compiler.Name);
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
