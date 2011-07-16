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
		public PDictionary Dict {
			get {
				return dict;
			}
			set {
				dict = value;
				dict.Changed += HandleDictChanged;
				Update ();
			}
		}
		
		void HandleDictChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		PListEditorWidget widget;
		public IPhoneDeploymentInfo (PListEditorWidget widget)
		{
			this.widget = widget;
			this.Build ();
			
			imageIPhoneAppIcon1.PictureSize = new Size (57, 57);
			imageIPhoneAppIcon1.AccepptedSize = new Size (57, 57);
			imageIPhoneAppIcon1.SetProject (widget.Project);
			imageIPhoneAppIcon1.Changed += delegate {
				widget.SetIcon (imageIPhoneAppIcon1.SelectedPixbuf, 57, 57);
			};
			
			imageIPhoneAppIcon2.PictureSize = new Size (57, 57);
			imageIPhoneAppIcon2.AccepptedSize = new Size (114, 114);
			imageIPhoneAppIcon2.SetProject (widget.Project);
			imageIPhoneAppIcon2.Changed += delegate {
				widget.SetIcon (imageIPhoneAppIcon2.SelectedPixbuf, 114, 114);
			};
			
			imageIPhoneLaunch1.PictureSize = new Size (58, 58);
			imageIPhoneLaunch2.PictureSize = new Size (58, 58);
			
			interfacePicker.Project = widget.Project;
			interfacePicker.DefaultFilter = "*.xib|*.storyboard";
			interfacePicker.EntryIsEditable = true;
			interfacePicker.DialogTitle = GettextCatalog.GetString ("Select main interface file...");
			interfacePicker.Changed += delegate {
				dict.SetString (NibFileKey, widget.Project.GetRelativeChildPath (interfacePicker.SelectedFile));
			};
			
			togglebutton1.Toggled += HandleToggled;
			togglebutton2.Toggled += HandleToggled;
			togglebutton3.Toggled += HandleToggled;
			togglebutton4.Toggled += HandleToggled;
			
		}

		void HandleToggled (object sender, EventArgs e)
		{
			if (inUpdate)
				return;
			var arr = dict.GetArray ("UISupportedInterfaceOrientations");
			arr.Clear ();
			if (togglebutton1.Active)
				arr.Add (new PString ("UIInterfaceOrientationPortrait"));
			if (togglebutton2.Active)
				arr.Add (new PString ("UIInterfaceOrientationPortraitUpsideDown"));
			if (togglebutton3.Active)
				arr.Add (new PString ("UIInterfaceOrientationLandscapeLeft"));
			if (togglebutton4.Active)
				arr.Add (new PString ("UIInterfaceOrientationLandscapeRight"));
			arr.QueueRebuild ();
		}
		
		bool inUpdate;
		public void Update ()
		{
			inUpdate = true;
			interfacePicker.SelectedFile = dict.Get<PString> (NibFileKey) ?? "";
			
			foreach (var pair in widget.IconFiles) {
				if (pair.Value.Width == 57)
					imageIPhoneAppIcon1.Pixbuf = pair.Value;
				if (pair.Value.Width == 114)
					imageIPhoneAppIcon2.Pixbuf = pair.Value;
			}
			
			var iphone = dict.Get<PArray> ("UISupportedInterfaceOrientations");
			togglebutton1.Active = togglebutton2.Active = togglebutton3.Active = togglebutton4.Active = false;
			if (iphone != null) {
				foreach (PString val in iphone) {
					if (val == "UIInterfaceOrientationPortrait")
						togglebutton1.Active = true;
					if (val == "UIInterfaceOrientationPortraitUpsideDown")
						togglebutton2.Active = true;
					if (val == "UIInterfaceOrientationLandscapeLeft")
						togglebutton3.Active = true;
					if (val == "UIInterfaceOrientationLandscapeRight")
						togglebutton4.Active = true;
				}
			}
			inUpdate = false;
		}
	}
}

