// 
// ImageChooser.cs
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
using System.ComponentModel;

using Gdk;

using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.MacDev.PlistEditor
{
	[ToolboxItem(true)]
	public class ImageChooser : VBox
	{
		Button buttonImage;
		Gtk.Image image;
		Label labelDescription;
		
		Pixbuf pixbuf;
		public string Description {
			get {
				return labelDescription.Text;
			}
			set {
				labelDescription.Text = value;
			}
		}
		
		Size pictureSize;
		public Size PictureSize {
			get {
				return pictureSize;
			}
			set {
				this.pictureSize = value;
			}
		}
		
		
		public Size AccepptedSize {
			get;
			set;
		}
		
		public FilePath SelectedPixbuf {
			get;
			set;
		}

		public Pixbuf Pixbuf {
			get {
				return this.pixbuf;
			}
			set {
				DestroyPixbuf ();
				image.Pixbuf = pixbuf = value;
			}
		}
		
		public ImageChooser ()
		{
			this.buttonImage = new Button ();
			this.image = new Gtk.Image ();
			this.buttonImage.Add (this.image);
			this.PackStart (this.buttonImage, false, false, 0);
			
			this.labelDescription = new Label ();
			this.PackEnd (this.labelDescription, false, false, 0);
			
			ShowAll ();
		}
		
		public void SetProject (Project proj)
		{
			this.buttonImage.Clicked += delegate {
				var dialog = new ProjectFileSelectorDialog (proj, null, "*.png");
				try {
					dialog.Title = GettextCatalog.GetString ("Select icon...");
					int response = MessageService.RunCustomDialog (dialog);
					if (response == (int)Gtk.ResponseType.Ok && dialog.SelectedFile != null) {
						
						var path = dialog.SelectedFile.FilePath;
						if (AccepptedSize.Width > 0) {
							using (var pb = new Pixbuf (path)) {
								if (pb.Width != AccepptedSize.Width || pb.Height != AccepptedSize.Height) {
									MessageService.ShowError (GettextCatalog.GetString ("Wrong picture size"), string.Format (GettextCatalog.GetString ("Only pictures with {0}x{1} are acceppted. Picture was {2}x{3}."), AccepptedSize.Width, AccepptedSize.Height, pb.Width, pb.Height));
									return;
								}
							}
						}
						SelectedPixbuf = dialog.SelectedFile.ProjectVirtualPath;
						OnChanged (EventArgs.Empty);
					}
				} finally {
					dialog.Destroy ();
				}
			};
		}
			
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			DestroyPixbuf ();
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			if (pictureSize.Width <= 0 || pictureSize.Height <= 0)
				throw new InvalidOperationException ("Picture size not set.");
			
			if (pixbuf == null)
				pixbuf = CreateNoImageIcon (pictureSize.Width, pictureSize.Height);
			image.Pixbuf = pixbuf;
		}
		
		bool shouldDestroyPixbuf;
		void DestroyPixbuf ()
		{
			if (shouldDestroyPixbuf && pixbuf != null) {
				pixbuf.Dispose ();
				pixbuf = null;
				shouldDestroyPixbuf = false;
			}
		}
		
		Pixbuf CreateNoImageIcon (int w, int h)
		{
			using (var pixmap = new Pixmap (GdkWindow, w, h)) {
				using (var cr = CairoHelper.Create (pixmap)) {
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
				shouldDestroyPixbuf = true;
				return Pixbuf.FromDrawable (pixmap, this.Colormap, 0, 0, 0, 0, w, h);
			}
		}
		
		protected virtual void OnChanged (System.EventArgs e)
		{
			EventHandler handler = this.Changed;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler Changed;
	}
}
