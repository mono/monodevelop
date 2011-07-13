// 
// PListEditorWidget.cs
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
using Gtk;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoMac.Foundation;
using MonoDevelop.Components;
using Mono.TextEditor;
using MonoDevelop.Ide;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(false)]
	public partial class PListEditorWidget : Gtk.Bin
	{
		public PDictionary NSDictionary {
			get {
				return customProperties.NSDictionary;
			}
			set {
				customProperties.NSDictionary = value;
				Update ();
			}
		}
		
		public PListEditorWidget ()
		{
			this.Build ();
			
			
			imageIPhoneAppIcon1.PictureSize = new Size (57, 57);
			imageIPhoneAppIcon2.PictureSize = new Size (114, 114);
			
			imageIPhoneLaunch1.PictureSize = new Size (58, 58);
			imageIPhoneLaunch2.PictureSize = new Size (58, 58);
			
			imageIPadAppIcon.PictureSize = new Size (72, 72);
			
			imageIPadLaunch1.PictureSize = new Size (58, 58);
			imageIPadLaunch2.PictureSize = new Size (58, 58);
			
			Gtk.ListStore devices = new Gtk.ListStore (typeof (string));
			devices.AppendValues (GettextCatalog.GetString ("iPhone/iPod"));
			devices.AppendValues (GettextCatalog.GetString ("iPad"));
			devices.AppendValues (GettextCatalog.GetString ("Universal"));
			
			comboboxDevices.Model = devices;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			createdImages.ForEach (px => px.Dispose ());
			createdImages.Clear ();
		}
		
		List<Gdk.Pixbuf> createdImages = new List<Gdk.Pixbuf> ();
		Gdk.Pixbuf CreateNoImageIcon (int w, int h)
		{
			using (var pixmap = new Pixmap (GdkWindow, w, h)) {
				using (var cr = Gdk.CairoHelper.Create (pixmap)) {
					cr.Rectangle (0, 0, w, h);
					cr.Color = new Cairo.Color (1, 1, 1);
					cr.FillPreserve ();
					cr.LineWidth = 1;
					cr.Color = new Cairo.Color (0.57, 0.57, 0.57);
					cr.Stroke ();
					
					CairoExtensions.RoundedRectangle (cr, 5, 5, w - 10, h - 10, 5);
					cr.Color = new Cairo.Color (0.97, 0.97, 0.97);
					cr.Fill ();
					
					using (var layout = new Pango.Layout (PangoContext)) {
						layout.SetText (GettextCatalog.GetString ("no image"));
					
						layout.Width = (int)((w - 20) * Pango.Scale.PangoScale);
						layout.Wrap = Pango.WrapMode.WordChar;
						layout.Alignment = Pango.Alignment.Center;
						int pw, ph;
						layout.GetPixelSize (out pw, out ph);
						cr.MoveTo ((w - layout.Width / Pango.Scale.PangoScale) / 2, (h - ph) / 2);
						
						cr.Color = new Cairo.Color (0.5, 0.5, 0.5);
						cr.ShowLayout (layout);
					}
					
					CairoExtensions.RoundedRectangle (cr, 5, 5, w - 10, h - 10, 5);
					cr.LineWidth = 3;
					cr.Color = new Cairo.Color (0.8, 0.8, 0.8);
					cr.SetDash (new double[] { 12, 2 }, 0);
					cr.Stroke ();
				}
				var result = Pixbuf.FromDrawable (pixmap, this.Colormap, 0, 0, 0, 0, w, h);
				createdImages.Add (result);
				return result;
			}
		}
		
		void Update ()
		{
			var identifier = NSDictionary.Get<PString> ("CFBundleIdentifier");
			entryIdentifier.Text = identifier != null ? identifier : "";
			
			
			var version = NSDictionary.Get<PString> ("CFBundleVersion");
			entryVersion.Text = version != null ? version : "";
			
			var iphone = NSDictionary.Get<PArray> ("UISupportedInterfaceOrientations");
			var ipad   = NSDictionary.Get<PArray> ("UISupportedInterfaceOrientations~ipad");
			
			if (iphone != null && ipad != null) {
				comboboxDevices.Active = 2;
			} else if (ipad != null) {
				comboboxDevices.Active = 1;
			} else {
				comboboxDevices.Active = 0;
			}
			
			expanderIPhone.Visible = iphone != null;
			togglebutton1.Active = togglebutton2.Active = togglebutton3.Active = togglebutton4.Active = false;
			if (iphone != null) {
				foreach (PString val in iphone.Value) {
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
			
			expanderIPad.Visible = ipad != null;
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
			
		}
		
	}
}

