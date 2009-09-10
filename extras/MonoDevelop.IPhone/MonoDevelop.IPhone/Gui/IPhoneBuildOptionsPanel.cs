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
using Gtk;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core;

namespace MonoDevelop.IPhone.Gui
{

	class IPhoneBuildOptionsPanel : MultiConfigItemOptionsPanel
	{
		IPhoneBuildOptionsPanelWidget widget;
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is IPhoneProject;
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
		
		MenuButtonEntry mbe;
		
		public IPhoneBuildOptionsPanelWidget ()
		{
			this.Build ();
			mbe = new MenuButtonEntry ();
			contentsAlignment.Add (mbe);
			mbe.AddOptions (menuOptions);
			
			this.ShowAll ();
		}
		
		public void LoadPanelContents (IPhoneProjectConfiguration cfg)
		{
			mbe.Entry.Text = cfg.ExtraMtouchArgs ?? "";
		}
		
		public void StorePanelContents (IPhoneProjectConfiguration cfg)
		{
			cfg.ExtraMtouchArgs = NullIfEmpty (mbe.Entry.Text);
		}
		
		string NullIfEmpty (string s)
		{
			if (s == null || s.Length != 0)
				return s;
			return null;
		}
	}
}
