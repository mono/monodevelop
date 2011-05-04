// 
// MonoDroidBuildOptions.cs
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
using System.Linq;
using System.Text;
using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.MonoDroid.Gui
{
	class MonoDroidBuildOptions : MultiConfigItemOptionsPanel
	{
		MonoDroidBuildOptionsWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			AllowMixedConfigurations = false;
			return widget = new MonoDroidBuildOptionsWidget ();
		}
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is MonoDroidProject
				&& (((MonoDroidProject)ConfiguredProject).IsAndroidApplication);
		}
		
		public override void LoadConfigData ()
		{
			widget.LoadPanelContents ((MonoDroidProjectConfiguration)CurrentConfiguration);
		}

		public override void ApplyChanges ()
		{
			widget.StorePanelContents ((MonoDroidProjectConfiguration)CurrentConfiguration);
		}
	}
	
	public partial class MonoDroidBuildOptionsWidget : Gtk.Bin
	{
		string[] i18n = { "cjk", "mideast", "other", "rare", "west" };
		string[] abis = { "armeabi", "armeabi-v7a" };
		
		ListStore i18nStore = new ListStore (typeof (string), typeof (bool));
		ListStore abisStore = new ListStore (typeof (string), typeof (bool));
		
		public MonoDroidBuildOptionsWidget ()
		{
			this.Build ();
			
			linkerCombo.AppendText ("Don't link"); //MtouchLinkMode.None
			linkerCombo.AppendText ("Link SDK assemblies only"); //MtouchLinkMode.SdkOnly
			linkerCombo.AppendText ("Link all assemblies"); //MtouchLinkMode.All
			
			i18NTreeView.Model = i18nStore;
			
			var toggle = new CellRendererToggle ();
			i18NTreeView.AppendColumn ("", toggle, "active", 1);
			i18NTreeView.AppendColumn ("", new CellRendererText (), "text", 0);
			i18NTreeView.HeadersVisible = false;
			toggle.Toggled += delegate(object o, ToggledArgs args) {
				TreeIter iter;
				if (i18nStore.GetIter (out iter, new TreePath (args.Path)))
					i18nStore.SetValue (iter, 1, !(bool)i18nStore.GetValue (iter, 1));
			};
			
			abisTreeView.Model = abisStore;

			var abiToggle = new CellRendererToggle ();
			abisTreeView.AppendColumn ("", abiToggle, "active", 1);
			abisTreeView.AppendColumn ("", new CellRendererText (), "text", 0);
			abisTreeView.HeadersVisible = false;
			abiToggle.Toggled += delegate (object o, ToggledArgs args) {
				TreeIter iter;
				if (abisStore.GetIter (out iter, new TreePath (args.Path)))
					abisStore.SetValue (iter, 1, !(bool)abisStore.GetValue (iter, 1));
			};
			
			ShowAll ();
		}
		
		public void LoadPanelContents (MonoDroidProjectConfiguration cfg)
		{
			extraMonoDroidArgsEntry.Text = cfg.MonoDroidExtraArgs ?? "";
			linkerCombo.Active = (int) cfg.MonoDroidLinkMode;
			sharedRuntimeCheck.Active = cfg.AndroidUseSharedRuntime;
			LoadI18nValues (cfg.MandroidI18n);
			LoadABIValues (cfg.SupportedAbis);
		}
		
		public void StorePanelContents (MonoDroidProjectConfiguration cfg)
		{
			cfg.MonoDroidExtraArgs = extraMonoDroidArgsEntry.Text;
			cfg.MonoDroidLinkMode = (MonoDroidLinkMode) linkerCombo.Active;
			cfg.AndroidUseSharedRuntime = sharedRuntimeCheck.Active;
			cfg.MandroidI18n = GetI18nValues ();
			cfg.SupportedAbis = GetABIValues ();
		}
		
		void LoadI18nValues (string values)
		{
			i18nStore.Clear ();
			if (values == null) {
				foreach (string s in i18n)				
					i18nStore.AppendValues (s, false);
			} else {		
				var arr = values.Split (',');
				foreach (string s in i18n)
					i18nStore.AppendValues (s, arr.Contains (s));
			}
		}
		
		string GetI18nValues ()
		{	
			var sb = new StringBuilder ();
			TreeIter iter;
			if (i18nStore.GetIterFirst (out iter)) {
				do {
					if ((bool)i18nStore.GetValue (iter, 1)) {
						if (sb.Length != 0)
							sb.Append (",");
						sb.Append ((string)i18nStore.GetValue (iter, 0));
					}
				} while (i18nStore.IterNext (ref iter));
			}
			return sb.ToString ();
		}
		
		void LoadABIValues (string values)
		{
			abisStore.Clear ();
			if (values == null)
				foreach (string s in abis)
					abisStore.AppendValues (s, false);
			else {
				var arr = values.Split (',');
				foreach (string s in abis)
					abisStore.AppendValues (s, arr.Contains (s));
			}
		}
		
		string GetABIValues ()
		{
			var sb = new StringBuilder ();
			TreeIter iter;
			if (abisStore.GetIterFirst (out iter)) {
				do {
					if ((bool)abisStore.GetValue (iter, 1)) {
						if (sb.Length != 0)
							sb.Append (",");
						sb.Append ((string)abisStore.GetValue (iter, 0));
					}
				} while (abisStore.IterNext (ref iter));
			}
			return sb.ToString ();
		}
	}
}

