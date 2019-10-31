//
// ExtendedHeaderBox.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.Components
{
	public class ExtendedHeaderBox : Widget
	{
		Image image;
		FrameBox frame;
		Label headerTitle, headerSubtitle, headerSeparator;
		Button backButton;

		bool backButtonVisible;
		string backButtonTooltip;
		Image backImage = ImageService.GetIcon ("md-navigate-back").WithSize (IconSize.Medium);


		public string Title {
			get { return headerTitle.Text; }
			set { headerTitle.Text = value; }
		}

		public string Subtitle {
			get { return headerSubtitle.Text; }
			set {
				headerSubtitle.Text = value;
				headerSeparator.Visible = headerSubtitle.Visible = !string.IsNullOrEmpty (value);
			}
		}

		public Image Image {
			get { return image; }
			set {
				if (image != value) {
					image = value;
					if (image != null) {
						if (!BackButtonVisible) {
							if (Surface.ToolkitEngine.Type == ToolkitType.XamMac)
								backButton.Image = image.WithSize (IconSize.Medium).ToBitmap ();
							else
								backButton.Image = image.WithSize (IconSize.Medium);
							backButton.Visible = true;
							backButton.Sensitive = false;
						}
					} else {
						if (!BackButtonVisible) {
							backButton.Visible = false;
						}
						backButton.Visible = BackButtonVisible;
					}
				}
			}
		}

		public bool BackButtonVisible {
			get {
				return backButtonVisible;
			}
			set {
				backButtonVisible = value;
				if (value) {
					backButton.Visible = backButton.Sensitive = true;
					backButton.Image = backImage;
					backButton.TooltipText = BackButtonTooltip;
				} else {
					backButton.Visible = image != null;
					backButton.Sensitive = false;
					backButton.Image = image.WithSize (IconSize.Medium);
					backButton.TooltipText = string.Empty;
				}
			}
		}

		public string BackButtonTooltip {
			get { return backButtonTooltip; }
			set {
				backButtonTooltip = value;
				if (BackButtonVisible)
					backButton.TooltipText = value;
				backButton.Accessible.Title = value;
			}
		}

		public Color TitleColor {
			get { return headerTitle.TextColor; }
			set { headerTitle.TextColor = value; }
		}

		public Color SubtitleColor {
			get { return headerSubtitle.TextColor; }
			set { headerSubtitle.TextColor = value; }
		}

		public WidgetSpacing Padding {
			get { return frame.Padding; }
			set { frame.Padding = value; }
		}

		public double PaddingLeft {
			get { return frame.PaddingLeft; }
			set { frame.PaddingLeft = value; }
		}

		public double PaddingRight {
			get { return frame.PaddingRight; }
			set { frame.PaddingRight = value; }
		}

		public double PaddingTop {
			get { return frame.PaddingTop; }
			set { frame.PaddingTop = value; }
		}

		public double PaddingBottom {
			get { return frame.PaddingBottom; }
			set { frame.PaddingBottom = value; }
		}

		public WidgetSpacing BorderWidth {
			get { return frame.BorderWidth; }
			set { frame.BorderWidth = value; }
		}

		public double BorderWidthLeft {
			get { return frame.BorderWidthLeft; }
			set { frame.BorderWidthLeft = value; }
		}

		public double BorderWidthRight {
			get { return frame.BorderWidthRight; }
			set { frame.BorderWidthRight = value; }
		}

		public double BorderWidthTop {
			get { return frame.BorderWidthTop; }
			set { frame.BorderWidthTop = value; }
		}

		public double BorderWidthBottom {
			get { return frame.BorderWidthBottom; }
			set { frame.BorderWidthBottom = value; }
		}

		public Color BorderColor {
			get { return frame.BorderColor; }
			set { frame.BorderColor = value; }
		}

		public event EventHandler BackButtonClicked;

		public ExtendedHeaderBox (string title, string subtitle = null, Image image = null)
		{
			var headerBox = new HBox ();
			headerBox.Spacing = 0;

			headerTitle = new Label ();
			var font = headerTitle.Font;
			headerTitle.Font = font.WithSize (16);

			headerSeparator = new Label (" – ") {
				TextColor = Styles.SecondaryTextColor,
				Font = font.WithSize (14),
			};
			headerSeparator.Accessible.IsAccessible = false;

			headerSubtitle = new Label {
				TextColor = Styles.SecondaryTextColor,
				Font = font.WithSize (14),
			};

			backButton = new Button ();
			backButton.Style = ButtonStyle.Flat;
			backButton.MarginRight = 6;
			backButton.Visible = false;
			backButton.Clicked += (sender, e) => BackButtonClicked?.Invoke (this, EventArgs.Empty);


			headerBox.PackStart (backButton);
			headerBox.PackStart (headerTitle);
			headerBox.PackStart (headerSeparator);
			headerBox.PackStart (headerSubtitle);

			frame = new FrameBox {
				Content = headerBox,
			};

			Title = title;
			Image = image;
			Subtitle = subtitle;

			BackgroundColor = Styles.BaseBackgroundColor;
			BorderColor = Styles.ThinSplitterColor;
			Padding = 15;
			BorderWidthBottom = 1;

			Content = frame;
		}
	}
}
