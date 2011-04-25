// 
// MonoDroidApplicationSettings.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using Gtk;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MonoDevelop.MonoDroid.Gui
{
	class MonoDroidApplicationSettings : OptionsPanel
	{
		MonoDroidApplicationSettingsWidget w;
		
		public override bool IsVisible ()
		{
			var proj = DataObject as MonoDroidProject;
			return proj != null && proj.IsAndroidApplication;
		}
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return w = new MonoDroidApplicationSettingsWidget ((MonoDroidProject)DataObject);
		}
		
		public override void ApplyChanges ()
		{
			w.ApplyChanges ();
		}
	}
	
	partial class MonoDroidApplicationSettingsWidget : Gtk.Bin
	{
		FilePath filename;
		AndroidAppManifest manifest;
		ListStore permissionsStore = new ListStore (typeof (bool), typeof (string));
		MonoDroidProject project;
		
		bool loaded;
		
		public MonoDroidApplicationSettingsWidget (MonoDroidProject project)
		{
			this.project = project;
			this.Build ();
			Load ();
		}
		
		//this may be called again if it shows a load error widget
		//but once it's loaded the real widgets, it cannot be called again
		void Load ()
		{
			//remove and destroy error widgets, if any
			var c = this.Child;
			if (c != null && c != table1) {
				this.Remove (c);
				c.Destroy ();
			}
			
			filename = project.AndroidManifest;
			
			if (filename.IsNullOrEmpty) {
				var msg = GettextCatalog.GetString ("The project has no Android manifest");
				AddErrorWidget (CreateAddManifestButton (msg, Stock.Info));
				return;
			}
			
			if (!File.Exists (filename)) {
				var msg = GettextCatalog.GetString ("The project's Android manifest is missing");
				AddErrorWidget (CreateAddManifestButton (msg, Stock.DialogWarning));
				return;
			}
			
			try {
				manifest = AndroidAppManifest.Load (filename);
			} catch (Exception ex) {
				var vb = new VBox () { Spacing = 6 };
				var hb = new HBox () { Spacing = 6 };
				hb.PackStart (new Image (Stock.DialogError, IconSize.Button), false, false, 0);
				var msg = GettextCatalog.GetString ("Error reading Android manifest");
				hb.PackStart (new Label () { Markup = "<big>" + msg + "</big>"}, false, false, 0);
				vb.PackStart (hb, false, false, 0);
				var tv = new TextView ();
				tv.Buffer.InsertAtCursor (ex.ToString ());
				var sw = new ScrolledWindow ();
				sw.ShadowType = ShadowType.EtchedIn;
				sw.Add (tv);
				vb.PackStart (sw, true, true, 0);
				AddErrorWidget (vb);
				return;
			}
			
			if (c != table1)
				this.Add (table1);
			
			InitializeRealWidgets ();
			
			packageNameEntry.Text = manifest.PackageName ?? "";
			appNameEntry.Text = manifest.ApplicationLabel ?? "";
			versionNameEntry.Text = manifest.VersionName ?? "";
			versionNumberEntry.Text = manifest.VersionCode ?? "";
			appIconCombo.Entry.Text = manifest.ApplicationIcon ?? "";
			SetMinSdkVersion (manifest.MinSdkVersion);
			SetPermissions (manifest.AndroidPermissions);
			SetInstallLocation (manifest.InstallLocation);
			
			loaded = true;
		}
		
		void AddErrorWidget (Widget w)
		{
			//remove the able widget, if any
			var c = this.Child;
			if (c != null)
				this.Remove (c);
			this.Add (w);
			this.ShowAll ();
		}
		
		protected override void OnDestroyed ()
		{
			var c = this.Child;
			if (c != table1)
				table1.Destroy ();
		}
		
		Widget CreateAddManifestButton (string message, string imageId)
		{
			var img = new Image (imageId, IconSize.Button);
			var lbl = new Label () { Markup = "<big>" + message + "</big>"};
			var btn = new Button (GettextCatalog.GetString ("Add Android manifest"));
			
			btn.Clicked += delegate {
				project.AddManifest ();
				MonoDevelop.Ide.IdeApp.ProjectOperations.Save (project);
				Load ();
			};
			
			var tbl = new Table (4, 4, false);
			tbl.Attach (img, 2, 3, 2, 3, AttachOptions.Shrink, AttachOptions.Shrink, 6, 6);
			tbl.Attach (lbl, 3, 4, 2, 3, AttachOptions.Shrink, AttachOptions.Shrink, 6, 6);
			tbl.Attach (btn, 3, 4, 3, 4, AttachOptions.Shrink, AttachOptions.Shrink, 6, 6);
			
			var expandFill = AttachOptions.Expand | AttachOptions.Fill;
			tbl.Attach (new Label (""), 0, 1, 0, 1, expandFill, expandFill, 0, 0);
			tbl.Attach (new Label (""), 4, 5, 4, 5, expandFill, expandFill, 0, 0);
			
			return tbl;
		}
		
		void InitializeRealWidgets ()
		{
			minAndroidVersionCombo.AppendText (GettextCatalog.GetString ("Automatic"));
			foreach (var v in MonoDroidFramework.AndroidVersions)
				minAndroidVersionCombo.AppendText (v.Label);
			
			foreach (var p in MonoDroidFramework.Permissions)
				permissionsStore.AppendValues (false, p);
			
			installLocationCombo.AppendText (GettextCatalog.GetString ("Automatic"));
			foreach (var l in MonoDroidFramework.InstallLocations)
				installLocationCombo.AppendText (l);
			
			var toggleRenderer = new CellRendererToggle ();
			permissionsTreeView.AppendColumn ("", toggleRenderer, "active", 0);
			permissionsTreeView.AppendColumn ("", new CellRendererText (), "text", 1);
			permissionsTreeView.Model = permissionsStore;
			toggleRenderer.Toggled += delegate(object o, ToggledArgs args) {
				TreeIter iter;
				if (permissionsStore.GetIterFromString (out iter, args.Path))
					permissionsStore.SetValue (iter, 0, !toggleRenderer.Active);
			};
			permissionsTreeView.HeadersVisible = false;
			
			//FIXME: build a nice drawable resource picker
			foreach (var kv in project.GetAndroidResources ("drawable"))
				appIconCombo.AppendText ("@drawable/" + kv.Key);
			
			ShowAll ();
		}
		
		void SetMinSdkVersion (int? v)
		{
			if (v.HasValue) {
				for (int i = 0; i < MonoDroidFramework.AndroidVersions.Length; i++) {
					if (MonoDroidFramework.AndroidVersions[i].ApiLevel == v.Value) {
						minAndroidVersionCombo.Active = i + 1;
						return;
					}
				}
			}
			minAndroidVersionCombo.Active = 0;
		}
		
		IEnumerable<string> GetPermissions ()
		{
			TreeIter iter;
			if (!permissionsStore.GetIterFirst (out iter))
				yield break;
			while (permissionsStore.IterNext (ref iter))
				if ((bool)permissionsStore.GetValue (iter, 0))
					yield return (string)permissionsStore.GetValue (iter, 1);
		}
		
		void SetPermissions (IEnumerable<string> permissions)
		{
			var values = new HashSet<string> (permissions);
			TreeIter iter;
			if (!permissionsStore.GetIterFirst (out iter))
				return;
			while (permissionsStore.IterNext (ref iter)) {
				var isSet = values.Contains ((string)permissionsStore.GetValue (iter, 1));
				permissionsStore.SetValue (iter, 0, isSet);
			}
		}
		
		void SetInstallLocation (string installLocation)
		{
			if (!String.IsNullOrEmpty (installLocation))
				for (int i = 0; i < MonoDroidFramework.InstallLocations.Length; i++)
					if (MonoDroidFramework.InstallLocations [i] == installLocation) {
						installLocationCombo.Active = i + 1;
						return;
					}
			
			installLocationCombo.Active = 0;
		}
		
		public void ApplyChanges ()
		{
			if (!loaded)
				return;
			
			manifest.PackageName = packageNameEntry.Text;
			manifest.ApplicationLabel = appNameEntry.Text;
			manifest.VersionName = versionNameEntry.Text;
			manifest.VersionCode = versionNumberEntry.Text;
			manifest.ApplicationIcon = appIconCombo.Entry.Text;
			if (installLocationCombo.Active == 0)
				manifest.InstallLocation = null;
			else
				manifest.InstallLocation = MonoDroidFramework.InstallLocations [installLocationCombo.Active - 1];
			if (minAndroidVersionCombo.Active == 0)
				manifest.MinSdkVersion = null;
			else
				manifest.MinSdkVersion = MonoDroidFramework.AndroidVersions[minAndroidVersionCombo.Active - 1].ApiLevel;
			manifest.SetAndroidPermissions (GetPermissions ());
			
			//FIXME:icon
			
			manifest.WriteToFile (filename);
		}
	}
}

