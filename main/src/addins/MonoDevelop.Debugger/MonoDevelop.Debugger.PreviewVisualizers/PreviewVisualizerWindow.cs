//
// PreviewVisualizerWindow.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Components;
using Mono.Debugging.Client;
using Gdk;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.Debugger.PreviewVisualizers;

namespace MonoDevelop.Debugger
{
	public class PreviewVisualizerWindow : PopoverWindow
	{
		public PreviewVisualizerWindow ()
		{
			TransientFor = IdeApp.Workbench.RootWindow;
		}

		public void Show (ObjectValue val, Gtk.Widget invokingWidget, Rectangle previewButtonArea)
		{
			var previewVisualizer = DebuggingService.GetPreviewVisualizer (val);
			if (previewVisualizer == null)
				previewVisualizer = new GenericPreviewVisualizer ();
			Theme.SetFlatColor (new Cairo.Color (245 / 256.0, 245 / 256.0, 245 / 256.0));
			ShowArrow = true;
			var mainBox = new VBox ();
			var headerTable = new Table (1, 3, false);
			headerTable.ColumnSpacing = 5;
			var closeButton = new ImageButton () {
				InactiveImage = ImageService.GetIcon ("md-popup-close", IconSize.Menu),
				Image = ImageService.GetIcon ("md-popup-close-hover", IconSize.Menu)
			};
			closeButton.Clicked += delegate {
				this.Destroy ();
			};
			var hb = new HBox ();
			var vb = new VBox ();
			hb.PackStart (vb, false, false, 0);
			vb.PackStart (closeButton, false, false, 0);
			headerTable.Attach (hb, 0, 1, 0, 1);

			var headerTitle = new Label ();
			headerTitle.UseMarkup = true;
			headerTitle.Markup = "<b>" + val.TypeName.Split ('.').LastOrDefault ().Replace ("<", "&lt;").Replace (">", "&gt;") + "</b>";

			headerTable.Attach (headerTitle, 1, 2, 0, 1);

			if (DebuggingService.HasValueVisualizers (val)) {
				var openButton = new Button ();
				openButton.Label = "Open";
				openButton.Relief = ReliefStyle.Half;
				openButton.Clicked += delegate {
					DebuggingService.ShowValueVisualizer (val);
				};
				var hbox = new HBox ();
				hbox.PackEnd (openButton, false, false, 0);
				headerTable.Attach (hbox, 2, 3, 0, 1);
			} else {
				headerTable.Attach (new Label (), 2, 3, 0, 1);
			}
			mainBox.PackStart (headerTable);
			mainBox.ShowAll ();

			var widget = previewVisualizer.GetVisualizerWidget (val);
			mainBox.PackStart (widget);
			ContentBox.Add (mainBox);
			ShowPopup (invokingWidget, previewButtonArea, PopupPosition.Left);
		}
	}
}

