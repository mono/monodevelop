// 
// IPhoneDeploymentInfo.cs
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
	public partial class IPhoneDeploymentInfo : Gtk.Bin
	{
		const string NibFileKey = "NSMainNibFile";
		
		PDictionary dict;
		PlistIconFileManager iconFileManager;
		
		void HandleDictChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		public IPhoneDeploymentInfo (Project project, PDictionary dict, PlistIconFileManager iconFileManager)
		{
			this.iconFileManager = iconFileManager;
			this.dict = dict;
			
			this.Build ();
			
			imageIPhoneAppIcon.DisplaySize = new Size (57, 57);
			imageIPhoneAppIcon.AcceptedSize = new Size (57, 57);
			imageIPhoneAppIcon.SetProject (project);
			imageIPhoneAppIcon.Changed += delegate {
				iconFileManager.SetIcon (imageIPhoneAppIcon.SelectedProjectFile, 57, 57);
			};
			
			imageIPhoneAppIconRetina.DisplaySize = new Size (57, 57);
			imageIPhoneAppIconRetina.AcceptedSize = new Size (114, 114);
			imageIPhoneAppIconRetina.SetProject (project);
			imageIPhoneAppIconRetina.Changed += delegate {
//				widget.SetIcon (imageIPhoneAppIconRetina.SelectedProjectFile, 114, 114);
			};
			
			imageIPhoneLaunch.DisplaySize = new Size (80, 120);
			imageIPhoneLaunchRetina.DisplaySize = new Size (80, 120);
			imageIPhoneLaunch.AcceptedSize = new Size (320, 480);
			imageIPhoneLaunchRetina.AcceptedSize = new Size (640, 960);
			
			interfacePicker.Project = project;
			interfacePicker.DefaultFilter = "*.xib|*.storyboard";
			interfacePicker.EntryIsEditable = true;
			interfacePicker.DialogTitle = GettextCatalog.GetString ("Select main interface file...");
			interfacePicker.Changed += delegate {
				dict.SetString (NibFileKey, project.GetRelativeChildPath (interfacePicker.SelectedFile));
			};
			
			togglePortrait.Toggled += HandleToggled;
			toggleUpsideDown.Toggled += HandleToggled;
			toggleLandscapeLeft.Toggled += HandleToggled;
			toggleLandscapeRight.Toggled += HandleToggled;
			
			dict.Changed += HandleDictChanged;
			Update ();
		}

		void HandleToggled (object sender, EventArgs e)
		{
			if (inUpdate)
				return;
			var arr = dict.GetArray ("UISupportedInterfaceOrientations");
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
			interfacePicker.SelectedFile = dict.Get<PString> (NibFileKey) ?? "";
			
			imageIPhoneAppIcon.SetDisplayPixbuf (iconFileManager.GetIcon (57, 57));
			imageIPhoneAppIconRetina.SetDisplayPixbuf (iconFileManager.GetIcon (114, 114));
			
			var iphone = dict.Get<PArray> ("UISupportedInterfaceOrientations");
			
			togglePortrait.Active = false;
			toggleUpsideDown.Active = false;
			toggleLandscapeLeft.Active = false;
			toggleLandscapeRight.Active = false;
			
			if (iphone != null) {
				foreach (PString val in iphone) {
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

