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
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.Debugger
{
	public class PreviewVisualizerWindow : PopoverWindow
	{
		GenericPreviewVisualizer genericPreview;
		public PreviewVisualizerWindow (ObjectValue val, Gtk.Widget invokingWidget) : base (Gtk.WindowType.Toplevel)
		{
			this.TypeHint = WindowTypeHint.PopupMenu;
			this.Decorated = false;
			if (((Gtk.Window)invokingWidget.Toplevel).Modal)
				this.Modal = true;
			TransientFor = (Gtk.Window) invokingWidget.Toplevel;

			Theme.SetBackgroundColor (Styles.PreviewVisualizerBackgroundColor.ToCairoColor ());
			Theme.Padding = 3;
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
			headerTitle.ModifyFg (StateType.Normal, Styles.PreviewVisualizerHeaderTextColor.ToGdkColor ());
			var font = FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale12);
			font.Weight = Pango.Weight.Bold;
			headerTitle.ModifyFont (font);
			headerTitle.Text = val.TypeName;
			var vbTitle = new VBox ();
			vbTitle.PackStart (headerTitle, false, false, 3);
			headerTable.Attach (vbTitle, 1, 2, 0, 1);

			if (DebuggingService.HasValueVisualizers (val)) {
				var openButton = new Button ();
				openButton.Label = "Open";
				openButton.Relief = ReliefStyle.Half;
				openButton.Clicked += delegate {
					PreviewWindowManager.DestroyWindow ();
					DebuggingService.ShowValueVisualizer (val);
				};
				var hbox = new HBox ();
				hbox.PackEnd (openButton, false, false, 2);
				headerTable.Attach (hbox, 2, 3, 0, 1);
			} else {
				headerTable.Attach (new Label (), 2, 3, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill | AttachOptions.Expand, 10, 0);
			}
			mainBox.PackStart (headerTable);
			mainBox.ShowAll ();

			var previewVisualizer = DebuggingService.GetPreviewVisualizer (val);
			Control widget = null;
			try {
				widget = previewVisualizer?.GetVisualizerWidget (val);
			} catch (Exception e) {
				DebuggingService.DebuggerSession.LogWriter (true, "Exception during preview widget creation: " + e.Message);
			}
			if (widget == null) {
				genericPreview = new GenericPreviewVisualizer ();
				widget = genericPreview.GetVisualizerWidget (val);
			}
			var alignment = new Alignment (0, 0, 1, 1);
			alignment.SetPadding (3, 5, 5, 5);
			alignment.Show ();
			alignment.Add (widget);
			mainBox.PackStart (alignment);
			ContentBox.Add (mainBox);
		}

		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnCopyUpdate (CommandInfo cmd)
		{
			cmd.Enabled = genericPreview!=null;
		}

		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			genericPreview?.Copy ();
		}
	}
}

