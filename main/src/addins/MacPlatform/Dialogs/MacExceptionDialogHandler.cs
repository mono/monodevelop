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
		public bool Run (ExceptionDialogData data)
		{
			using (var alert = new NSAlert ()) {
				alert.AlertStyle = NSAlertStyle.Critical;
				
				var pix = ImageService.GetPixbuf (Gtk.Stock.DialogError, Gtk.IconSize.Dialog);
				byte[] buf = pix.SaveToBuffer ("tiff");
				alert.Icon = new NSImage (NSData.FromArray (buf));
				
				alert.MessageText = data.Title ?? "Some Message";
				alert.InformativeText = data.Message ?? "Some Info";

				if (data.Exception != null) {

					var text = new NSTextView (new RectangleF (0, 0, float.MaxValue, float.MaxValue));
					text.HorizontallyResizable = true;
					text.TextContainer.ContainerSize = new SizeF (float.MaxValue, float.MaxValue);
					text.TextContainer.WidthTracksTextView = false;
					text.InsertText (new NSString (data.Exception.ToString ()));
					text.Editable = false;
					
					var scrollView = new NSScrollView (new RectangleF (0, 0, 450, 150)) {
						HasHorizontalScroller = true,
						DocumentView = text,
					};
;
					alert.AccessoryView = scrollView;
				}
				
				// Hack up a slightly wider than normal alert dialog. I don't know how to do this in a nicer way
				// as the min size constraints are apparently ignored.
				var frame = ((NSPanel) alert.Window).Frame;
				((NSPanel) alert.Window).SetFrame (new RectangleF (frame.X, frame.Y, Math.Max (frame.Width, 600), frame.Height), true);
				
				int result = alert.RunModal () - (int)NSAlertButtonReturn.First;
				
				GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
			}
			
			return true;
		}
	}
}
