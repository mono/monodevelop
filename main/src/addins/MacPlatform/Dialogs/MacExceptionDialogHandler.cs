// 
// MacErrorDialogHandler.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright 2011 Xamarin, Inc.
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
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using MonoMac.Foundation;
using MonoMac.AppKit;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Components.Extensions;
using MonoDevelop.MacInterop;
	
namespace MonoDevelop.MacIntegration
{
	class MacExceptionDialogHandler : IExceptionDialogHandler
	{
		class MyTextView : NSTextView
		{
			public MyTextView (RectangleF frame)
				: base (frame)
			{

			}

			public override void KeyDown (NSEvent theEvent)
			{
				if (theEvent.ModifierFlags.HasFlag (NSEventModifierMask.CommandKeyMask)) {
					switch (theEvent.Characters) {
					case "x":
						Cut (this);
						break;
					case "c":
						Copy (this);
						break;
					case "a":
						SelectAll (this);
						break;
					}
				}
				
				base.KeyDown (theEvent);
			}
		}
		public bool Run (ExceptionDialogData data)
		{
			using (var alert = new NSAlert { AlertStyle = NSAlertStyle.Critical }) {
				var pix = ImageService.GetPixbuf (Gtk.Stock.DialogError, Gtk.IconSize.Dialog);
				byte[] buf = pix.SaveToBuffer ("tiff");
				alert.Icon = new NSImage (NSData.FromArray (buf));
				
				alert.MessageText = data.Title ?? "Some Message";
				alert.InformativeText = data.Message ?? "Some Info";

				List<AlertButton> buttons = null;
				if (data.Buttons != null && data.Buttons.Length > 0)
					buttons = data.Buttons.Reverse ().ToList ();

				if (buttons != null) {
					foreach (var button in buttons) {
						var label = button.Label;
						if (button.IsStockButton)
							label = Gtk.Stock.Lookup (label).Label;
						label = label.Replace ("_", "");

						//this message seems to be a standard Mac message since alert handles it specially
						if (button == AlertButton.CloseWithoutSave)
							label = GettextCatalog.GetString ("Don't Save");

						alert.AddButton (label);
					}
				}

				if (data.Exception != null) {
					var scrollSize = new SizeF (500, 130);

					var text = new MyTextView (new RectangleF (0, 0, float.MaxValue, float.MaxValue));
					text.HorizontallyResizable = true;
					text.TextContainer.ContainerSize = new SizeF (float.MaxValue, float.MaxValue);
					text.TextContainer.WidthTracksTextView = false;
					text.InsertText (new NSString (data.Exception.ToString ()));

					text.Editable = false;

					var scrollView = new NSScrollView (new RectangleF (PointF.Empty, scrollSize)) {
						HasHorizontalScroller = true,
						HasVerticalScroller = true,
						DocumentView = text,
					};

					alert.AccessoryView = scrollView;
				}

				int result = alert.RunModal () - (int)NSAlertButtonReturn.First;
				data.ResultButton = buttons != null ? buttons [result] : null;
				GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
			}
			
			return true;
		}
	}
}
