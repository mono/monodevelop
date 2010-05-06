// 
// IPhoneBuildOptionsPanelWidget.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Linq;
using Gtk;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.IPhone.Gui
{

	class IPhoneBuildOptionsPanel : MultiConfigItemOptionsPanel
	{
		IPhoneBuildOptionsPanelWidget widget;
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is IPhoneProject
				&& (((IPhoneProject)ConfiguredProject).CompileTarget == CompileTarget.Exe);
		}
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			AllowMixedConfigurations = false;
			return (widget = new IPhoneBuildOptionsPanelWidget ());
		}
		
		public override void LoadConfigData ()
		{
			widget.LoadPanelContents ((IPhoneProjectConfiguration)CurrentConfiguration);
		}

		public override void ApplyChanges ()
		{
			widget.StorePanelContents ((IPhoneProjectConfiguration)CurrentConfiguration);
		}
	}
	
	partial class IPhoneBuildOptionsPanelWidget : Gtk.Bin
	{
		internal static string[,] menuOptions = new string[,] {
			{GettextCatalog.GetString ("Target _Path"), "${TargetPath}"},
			{GettextCatalog.GetString ("_Target Directory"), "${TargetDir}"},
			{GettextCatalog.GetString ("_App Bundle Directory"), "${AppBundleDir}"},
			{"-", ""},
			{GettextCatalog.GetString ("_Project Directory"), "${ProjectDir}"},
			{GettextCatalog.GetString ("_Solution Directory"), "${SolutionDir}"},
		};
		
		string[] i18n = { "cjk", "mideast", "other", "rare", "west" };
		
		ListStore store;
		
		public IPhoneBuildOptionsPanelWidget ()
		{
			this.Build ();
			extraArgsEntry.AddOptions (menuOptions);
			
			linkCombo.AppendText ("Don't link"); //MtouchLinkMode.None
			linkCombo.AppendText ("Link SDK assemblies only"); //MtouchLinkMode.SdkOnly
			linkCombo.AppendText ("Link all assemblies"); //MtouchLinkMode.All
			
			foreach (var v in IPhoneFramework.InstalledSdkVersions)
				sdkComboEntry.AppendText (v.ToString ());
			
			foreach (var v in IPhoneFramework.KnownOSVersions)
				minOSComboEntry.AppendText (v.ToString ());
			
			store = new ListStore (typeof (string), typeof (bool));
			i18nTreeView.Model = store;
			
			var toggle = new CellRendererToggle ();
			i18nTreeView.AppendColumn ("", toggle, "active", 1);
			i18nTreeView.AppendColumn ("", new CellRendererText (), "text", 0);
			i18nTreeView.HeadersVisible = false;
			toggle.Toggled += delegate (object o, ToggledArgs args) {
				TreeIter iter;
				if (store.GetIter (out iter, new TreePath (args.Path)))
					store.SetValue (iter, 1, !(bool)store.GetValue (iter, 1));
			};
			
			this.ShowAll ();
		}
		
		public void LoadPanelContents (IPhoneProjectConfiguration cfg)
		{
			extraArgsEntry.Entry.Text = cfg.MtouchExtraArgs ?? "";
			debugCheck.Active = cfg.MtouchDebug;
			linkCombo.Active = (int) cfg.MtouchLink;
			sdkComboEntry.Entry.Text = cfg.MtouchSdkVersion;
			minOSComboEntry.Entry.Text = cfg.MtouchMinimumOSVersion;
			LoadI18nValues (cfg.MtouchI18n);
		}
		
		public void StorePanelContents (IPhoneProjectConfiguration cfg)
		{
			cfg.MtouchExtraArgs = NullIfEmpty (extraArgsEntry.Entry.Text);
			cfg.MtouchSdkVersion = sdkComboEntry.Entry.Text; //FIXME: validate this?
			cfg.MtouchMinimumOSVersion = minOSComboEntry.Entry.Text; //FIXME: validate this?
			cfg.MtouchDebug = debugCheck.Active;
			cfg.MtouchLink = (MtouchLinkMode) linkCombo.Active;
			cfg.MtouchI18n = GetI18nValues ();
		}
		
		void LoadI18nValues (string values)
		{
			store.Clear ();
			if (values == null) {
				foreach (string s in i18n)
					store.AppendValues (s, false);
			} else {
				var arr = values.Split (',');
				foreach (string s in i18n)
					store.AppendValues (s, arr.Contains (s));
			}
		}
		
		string GetI18nValues ()
		{
			var sb = new StringBuilder ();
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					if ((bool)store.GetValue (iter, 1)) {
						if (sb.Length != 0)
							sb.Append (",");
						sb.Append ((string)store.GetValue (iter, 0));
					}
				} while (store.IterNext (ref iter));
			}
			return sb.ToString ();
		}
		
		string NullIfEmpty (string s)
		{
			if (s == null || s.Length != 0)
				return s;
			return null;
		}
	}
}
