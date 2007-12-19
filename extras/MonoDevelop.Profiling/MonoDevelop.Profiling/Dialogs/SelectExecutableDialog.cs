//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
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

using Gtk;
using System;
using MonoDevelop.Core;

namespace MonoDevelop.Profiling
{
	public partial class SelectExecutableDialog : Gtk.Dialog
	{
		private ListStore storeProfilers;
		
		public SelectExecutableDialog()
		{
			this.Build();
			
			storeProfilers = new ListStore (typeof (string), typeof (string));
			comboProfilers.Model = storeProfilers;
			
			foreach (IProfiler profiler in ProfilingService.Profilers)
				if (profiler.IsSupported)
					storeProfilers.AppendValues (profiler.Name, profiler.Identifier);
			comboProfilers.Active = 0;
		}
		
		public string Executable {
			get { return entryExecutable.Text; }
		}
		
		public string Arguments {
			get { return entryArguments.Text; }
		}
		
		public IProfiler Profiler {
			get {
				TreeIter iter;
				if (comboProfilers.GetActiveIter (out iter)) {
					string identifier = (string)storeProfilers.GetValue (iter, 1);
					return MonoDevelop.Profiling.ProfilingService.GetProfiler (identifier);
				}
				return null;
			}
		}

		protected virtual void OpenClicked (object sender, System.EventArgs e)
		{
			FileChooserDialog dlg = new FileChooserDialog (
				GettextCatalog.GetString ("Select Executable"), null, FileChooserAction.Open,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-open", ResponseType.Accept
			);
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;
			dlg.SetCurrentFolder (Environment.GetFolderPath (Environment.SpecialFolder.Personal));
			
			FileFilter filterExe = new FileFilter ();
			filterExe.AddPattern ("*.exe");
			filterExe.Name = GettextCatalog.GetString ("Executables");
			FileFilter filterAll = new FileFilter ();
			filterAll.AddPattern ("*");
			filterAll.Name = GettextCatalog.GetString ("All files");
			dlg.AddFilter (filterExe);
			dlg.AddFilter (filterAll);

			if (dlg.Run () == (int)ResponseType.Accept)
				entryExecutable.Text = dlg.Filename;
			dlg.Destroy ();
		}

		protected virtual void ExecutableChanged (object sender, System.EventArgs e)
		{
			buttonOk.Sensitive = entryExecutable.Text.Length > 0 && System.IO.File.Exists (entryExecutable.Text);
		}
	}
}
