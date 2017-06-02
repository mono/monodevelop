//
// ImageBox.cs
//
// Author:
//       Vsevolod Kukol <sevo@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Windows;
using Xwt.Drawing;
using System.Windows.Media;
using System.Windows.Controls;

namespace WindowsPlatform
{
	public class ImageBox : UserControl
	{
		public static readonly DependencyProperty ImageProperty =
			DependencyProperty.Register ("Image", typeof (Xwt.Drawing.Image), typeof (ImageBox), new FrameworkPropertyMetadata () { AffectsMeasure = true, AffectsRender = true });
		
		public static readonly DependencyProperty StretchProperty =
			Viewbox.StretchProperty.AddOwner(typeof(ImageBox));
		
		public static readonly DependencyProperty StretchDirectionProperty =
			Viewbox.StretchDirectionProperty.AddOwner(typeof(ImageBox));

		bool subscribed;
		public ImageBox ()
		{
			Loaded += HandleLoaded;
			Unloaded += HandleUnloaded;
			Image = null;
		}

		void HandleLoaded (object sender, EventArgs args)
		{
			if (subscribed)
				return;

			subscribed = true;
			MonoDevelop.Ide.Gui.Styles.Changed += HandleStylesChanged;
		}

		void HandleUnloaded(object sender, EventArgs args)
		{
			subscribed = false;
			MonoDevelop.Ide.Gui.Styles.Changed -= HandleStylesChanged;
		}

		public ImageBox (Xwt.Drawing.Image image) : this ()
		{
			Image = image;
		}

		public ImageBox (string iconId, Gtk.IconSize size) : this ()
		{
			Image = MonoDevelop.Ide.ImageService.GetIcon (iconId, size);
		}

		void HandleStylesChanged (object sender, EventArgs e)
		{
			InvalidateVisual ();
		}

		protected override void OnRender (DrawingContext dc)
		{
			var image = Image;
			if (image != null) {
				if (!IsEnabled)
					image = image.WithStyles ("disabled").WithAlpha (0.4);
				image = image.WithBoxSize (RenderSize.Width, RenderSize.Height);
				var x = (RenderSize.Width - image.Size.Width) / 2;
				var y = (RenderSize.Height - image.Size.Height) / 2;
				MonoDevelop.Platform.WindowsPlatform.WPFToolkit.RenderImage (this, dc, image, x, y);
			}
		}

		protected override void OnPropertyChanged (DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged (e);
			if (e.Property == IsEnabledProperty)
				InvalidateVisual ();
		}

		public Xwt.Drawing.Image Image
		{
			get { return (Xwt.Drawing.Image)GetValue (ImageProperty); }
			set { SetValue (ImageProperty, value); }
		}

		public Stretch Stretch
		{
			get { return (Stretch) GetValue(StretchProperty); }
			set { SetValue(StretchProperty, value); }
		}

		public StretchDirection StretchDirection
		{
			get { return (StretchDirection)GetValue(StretchDirectionProperty); }
			set { SetValue(StretchDirectionProperty, value); }
		}

		protected override Size MeasureOverride (Size constraint)
		{
			return CalcSizeForBounds (constraint);
		}

		protected override Size ArrangeOverride (Size arrangeBounds)
		{
			return CalcSizeForBounds (arrangeBounds);
		}

		Size CalcSizeForBounds (Size availableSize)
		{
			if (Image == null)
				return new Size (0, 0);
			
			double scaleX = 1.0;
			double scaleY = 1.0;

			bool isConstrainedWidth = !Double.IsPositiveInfinity(availableSize.Width);
			bool isConstrainedHeight = !Double.IsPositiveInfinity(availableSize.Height);

			if ((Stretch == Stretch.Uniform || Stretch == Stretch.UniformToFill || Stretch == Stretch.Fill)
				&& (isConstrainedWidth || isConstrainedHeight) )
			{
				scaleX = availableSize.Width / Image.Size.Width;
				scaleY = availableSize.Height / Image.Size.Height;

				if (!isConstrainedWidth)
					scaleX = scaleY;
				else if (!isConstrainedHeight)
					scaleY = scaleX;
				else switch (Stretch) 
				{
					case Stretch.Uniform:
						double minscale = scaleX < scaleY ? scaleX : scaleY;
						scaleX = scaleY = minscale;
						break;

					case Stretch.UniformToFill:
						double maxscale = scaleX > scaleY ? scaleX : scaleY;
						scaleX = scaleY = maxscale;
						break;
				}

				switch(StretchDirection)
				{
					case StretchDirection.UpOnly:
						if (scaleX < 1.0) scaleX = 1.0;
						if (scaleY < 1.0) scaleY = 1.0;
						break;

					case StretchDirection.DownOnly:
						if (scaleX > 1.0) scaleX = 1.0;
						if (scaleY > 1.0) scaleY = 1.0;
						break;
				}
			}

			return new Size(Image.Size.Width * scaleX, Image.Size.Height * scaleY);
		}
	}
}

