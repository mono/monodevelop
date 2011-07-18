// 
// IPadDeploymentInfo.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using Gdk;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class IPadDeploymentInfo : Gtk.Bin
	{
		PDictionary dict;
		PlistIconFileManager iconFileManager;
		
		const string NibFileKey = "NSMainNibFile~ipad";
		
		public IPadDeploymentInfo (Project project, PDictionary dict, PlistIconFileManager iconFileManager)
		{
			this.iconFileManager = iconFileManager;
			this.dict = dict;
			
			this.Build ();
			
			imageIPadAppIcon.DisplaySize = new Size (72, 72);
			imageIPadAppIcon.AcceptedSize = new Size (72, 72);
			imageIPadAppIcon.SetProject (project);
			imageIPadAppIcon.Changed += delegate {
				iconFileManager.SetIcon (imageIPadAppIcon.SelectedProjectFile, 72, 72);
			};
			
			imageIPadLaunchPortrait.AcceptedSize = new Size (768, 1004);
			imageIPadLaunchPortrait.DisplaySize = new Size (96, 128); // acceptedsize / 8
			imageIPadLaunchLandscape.AcceptedSize = new Size (1024, 748);
			imageIPadLaunchLandscape.DisplaySize = new Size (128, 96); // acceptedsize / 8
			
			interfacePicker.Project = project;
			interfacePicker.DefaultFilter = "*.xib|*.storyboard";
			interfacePicker.EntryIsEditable = true;
			interfacePicker.DialogTitle = GettextCatalog.GetString ("Select iPad interface file...");
			interfacePicker.Changed += delegate {
				dict.SetString (NibFileKey, project.GetRelativeChildPath (interfacePicker.SelectedFile));
			};
			
			togglePortrait.Toggled += HandleToggled;
			toggleUpsideDown.Toggled += HandleToggled;
			toggleLandscapeLeft.Toggled += HandleToggled;
			toggleLandscapeRight.Toggled += HandleToggled;
			
			dict.Changed += delegate {
				Update ();
			};
			Update ();
		}
		
		void HandleToggled (object sender, EventArgs e)
		{
			if (inUpdate)
				return;
			var arr = dict.GetArray ("UISupportedInterfaceOrientations~ipad");
			arr.Clear ();
			if (togglePortrait.Active)
				arr.Add (new PString ("UIInterfaceOrientationPortrait"));
			if (toggleUpsideDown.Active)
				arr.Add (new PString ("UIInterfaceOrientationPortraitUpsideDown"));
			if (toggleLandscapeLeft.Active)
				arr.Add (new PString ("UIInterfaceOrientationLandscapeLeft"));
			if (toggleLandscapeRight.Active)
				arr.Add (new PString ("UIInterfaceOrientationLandscapeRight"));
			arr.QueueRebuild ();
		}
		
		bool inUpdate;
		public void Update ()
		{
			inUpdate = true;
			imageIPadAppIcon.SetDisplayPixbuf (iconFileManager.GetIcon (72, 72));
			
			interfacePicker.SelectedFile = dict.Get<PString> (NibFileKey) ?? "";
			
			var ipad   = dict.Get<PArray> ("UISupportedInterfaceOrientations~ipad");
			
			togglePortrait.Active = false;
			toggleUpsideDown.Active = false;
			toggleLandscapeLeft.Active = false;
			toggleLandscapeRight.Active = false;
			
			if (ipad != null) {
				foreach (PString val in ipad) {
					if (val == "UIInterfaceOrientationPortrait")
						togglePortrait.Active = true;
					if (val == "UIInterfaceOrientationPortraitUpsideDown")
						toggleUpsideDown.Active = true;
					if (val == "UIInterfaceOrientationLandscapeLeft")
						toggleLandscapeLeft.Active = true;
					if (val == "UIInterfaceOrientationLandscapeRight")
						toggleLandscapeRight.Active = true;
				}
			}
			inUpdate = false;
		}
	}
}

