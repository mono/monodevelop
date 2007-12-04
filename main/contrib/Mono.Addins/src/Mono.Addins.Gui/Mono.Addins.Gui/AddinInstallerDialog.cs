//
// AddinInstallerDialog.cs
//
// Author:
//   Lluis Sanchez Gual
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
using System.Threading;
using System.Collections;
using Mono.Addins.Setup;
using Mono.Addins.Description;
using Mono.Unix;

namespace Mono.Addins.Gui
{
	internal partial class AddinInstallerDialog : Gtk.Dialog, IProgressStatus
	{
		PackageCollection entries = new PackageCollection ();
		string[] addinIds;
		bool addinsNotFound;
		string errMessage;
		SetupService setup;
		
		public AddinInstallerDialog (AddinRegistry reg, string message, string[] addinIds)
		{
			this.Build();
			
			this.addinIds = addinIds;
			setup = new SetupService (reg);

			if (!CheckAddins (true))
				UpdateRepos ();
		}
		
		bool CheckAddins (bool updating)
		{
			string txt = "";
			entries.Clear ();
			bool addinsNotFound = false;
			foreach (string id in addinIds) {
				string name = Addin.GetIdName (id);
				string version = Addin.GetIdVersion (id);
				AddinRepositoryEntry[] ares = setup.Repositories.GetAvailableAddin (name, version);
				if (ares.Length == 0) {
					addinsNotFound = true;
					if (updating)
						txt += "<span foreground='grey'><b>" + name + " " + version + "</b> (searching add-in)</span>\n";
					else
						txt += "<span foreground='red'><b>" + name + " " + version + "</b> (not found)</span>\n";
				} else {
					entries.Add (Package.FromRepository (ares[0]));
					txt += "<b>" + ares[0].Addin.Name + " " + ares[0].Addin.Version + "</b>\n";
				}
			}
			PackageCollection toUninstall;
			DependencyCollection unresolved;
			if (!setup.ResolveDependencies (this, entries, out toUninstall, out unresolved)) {
				foreach (Dependency dep in unresolved) {
					txt += "<span foreground='red'><b>" + dep.Name + "</b> (not found)</span>\n";
				}
				addinsNotFound = true;
			}
			addinList.Markup = txt;
			return !addinsNotFound;
		}
		
		void UpdateRepos ()
		{
			progressBar.Show ();
			setup.Repositories.UpdateAllRepositories (this);
			progressBar.Hide ();
			addinsNotFound = CheckAddins (false);
			if (errMessage != null) {
				Services.ShowError (null, errMessage, this, true);
				errMessage = null;
			}
		}
		
		public int LogLevel {
			get {
				return 1;
			}
		}

		public bool IsCanceled {
			get {
				return false;
			}
		}

		public bool AddinsNotFound {
			get {
				return addinsNotFound;
			}
		}

		public string ErrMessage {
			get {
				return errMessage;
			}
		}

		public void SetMessage (string msg)
		{
			progressBar.Text = msg;
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();
		}
			       
		public void SetProgress (double progress)
		{
			progressBar.Fraction = progress;
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();
		}

		public void Log (string msg)
		{
		}

		public void ReportWarning (string message)
		{
		}

		public void ReportError (string message, System.Exception exception)
		{
			errMessage = message;
		}

		public void Cancel ()
		{
		}

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			if (addinsNotFound) {
				errMessage = Catalog.GetString ("Some of the required add-ins were not found");
				Respond (Gtk.ResponseType.Ok);
			}
			else {
				errMessage = null;
				progressBar.Show ();
				progressBar.Fraction = 0;
				progressBar.Text = "";
				bool res = setup.Install (this, entries);
				if (!res) {
					buttonCancel.Sensitive = buttonOk.Sensitive = false;
					if (errMessage == null)
						errMessage = Catalog.GetString ("Installation failed");
					Services.ShowError (null, errMessage, this, true);
				}
			}
			Respond (Gtk.ResponseType.Ok);
		}
	}
}
