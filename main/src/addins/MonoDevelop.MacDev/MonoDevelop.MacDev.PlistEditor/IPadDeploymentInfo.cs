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
		
		const string NibFileKey = "NSMainNibFile~ipad";
		
		void HandleDictChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		PListEditorWidget widget;
		public IPadDeploymentInfo (PListEditorWidget widget)
		{
			this.widget = widget;
			this.Build ();
			
			imageIPadAppIcon.PictureSize = new Size (72, 72);
			imageIPadAppIcon.AccepptedSize = new Size (72, 72);
			imageIPadAppIcon.SetProject (widget.Project);
			imageIPadAppIcon.Changed += delegate {
				widget.SetIcon (imageIPadAppIcon.SelectedPixbuf, 114, 114);
			};
			
			imageIPadLaunch1.PictureSize = new Size (58, 58);
			imageIPadLaunch2.PictureSize = new Size (58, 58);
			
			interfacePicker.Project = widget.Project;
			interfacePicker.DefaultFilter = "*.xib|*.storyboard";
			interfacePicker.EntryIsEditable = true;
			interfacePicker.DialogTitle = GettextCatalog.GetString ("Select iPad interface file...");
			interfacePicker.Changed += delegate {
				dict.SetString (NibFileKey, widget.Project.GetRelativeChildPath (interfacePicker.SelectedFile));
			};
			
			togglebutton9.Toggled += HandleToggled;
			togglebutton10.Toggled += HandleToggled;
			togglebutton11.Toggled += HandleToggled;
			togglebutton12.Toggled += HandleToggled;
		}
		
		void HandleToggled (object sender, EventArgs e)
		{
			if (inUpdate)
				return;
			var arr = dict.GetArray ("UISupportedInterfaceOrientations~ipad");
			arr.Value.Clear ();
			if (togglebutton9.Active)
				arr.Value.Add (new PString ("UIInterfaceOrientationPortrait") { Parent = arr });
			if (togglebutton10.Active)
				arr.Value.Add (new PString ("UIInterfaceOrientationPortraitUpsideDown") { Parent = arr });
			if (togglebutton11.Active)
				arr.Value.Add (new PString ("UIInterfaceOrientationLandscapeLeft") { Parent = arr });
			if (togglebutton12.Active)
				arr.Value.Add (new PString ("UIInterfaceOrientationLandscapeRight") { Parent = arr });
			arr.QueueRebuild ();
		}
		
		bool inUpdate;
		public void Update ()
		{
			inUpdate = true;
			
			foreach (var pair in widget.IconFiles) {
				if (pair.Value.Width == 72)
					imageIPadAppIcon.Pixbuf = pair.Value;
			}
			
			interfacePicker.SelectedFile = dict.Get<PString> (NibFileKey) ?? "";
			
			var ipad   = dict.Get<PArray> ("UISupportedInterfaceOrientations~ipad");
			togglebutton9.Active = togglebutton10.Active = togglebutton11.Active = togglebutton12.Active = false;
			if (ipad != null) {
				foreach (PString val in ipad.Value) {
					if (val == "UIInterfaceOrientationPortrait")
						togglebutton9.Active = true;
					if (val == "UIInterfaceOrientationPortraitUpsideDown")
						togglebutton10.Active = true;
					if (val == "UIInterfaceOrientationLandscapeLeft")
						togglebutton11.Active = true;
					if (val == "UIInterfaceOrientationLandscapeRight")
						togglebutton12.Active = true;
				}
			}
			inUpdate = false;
		}
	}
}

