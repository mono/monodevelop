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
		
		public MonoDroidApplicationSettingsWidget (MonoDroidProject project)
		{
			this.Build ();
			
			foreach (var v in MonoDroidFramework.AndroidVersions)
				minAndroidVersionCombo.AppendText (v.Label);
			
			foreach (var p in MonoDroidFramework.Permissions)
				permissionsStore.AppendValues (false, p);
			
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
			
			//try {
				filename = project.GetManifestFile (null).Name;
				manifest = AndroidAppManifest.Load (filename);
			//} catch (Exception ex) {
				// handle the case with invalid or nonexistent manifest
			//}
			
			Load ();
		}
		
		void Load ()
		{
			packageNameEntry.Text = manifest.PackageName ?? "";
			appNameEntry.Text = manifest.ApplicationLabel ?? "";
			versionNameEntry.Text = manifest.VersionName ?? "";
			versionNumberEntry.Text = manifest.VersionCode ?? "";
			appIconCombo.Entry.Text = manifest.ApplicationIcon ?? "";
			SetMinSdkVersion (manifest.MinSdkVersion);
			SetPermissions (manifest.AndroidPermissions);
		}
		
		void SetMinSdkVersion (int v)
		{
			for (int i = 0; i < MonoDroidFramework.AndroidVersions.Length; i++) {
				if (MonoDroidFramework.AndroidVersions[i].ApiLevel == v) {
					minAndroidVersionCombo.Active = i;
					return;
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
		
		public virtual void ApplyChanges ()
		{
			manifest.PackageName = packageNameEntry.Text;
			manifest.ApplicationLabel = appNameEntry.Text;
			manifest.VersionName = versionNameEntry.Text;
			manifest.VersionCode = versionNumberEntry.Text;
			manifest.ApplicationIcon = appIconCombo.Entry.Text;
			manifest.MinSdkVersion = MonoDroidFramework.AndroidVersions[minAndroidVersionCombo.Active].ApiLevel;
			manifest.SetAndroidPermissions (GetPermissions ());
			
			//FIXME:icon
			
			manifest.WriteToFile (filename);
		}
	}
}

