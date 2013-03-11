// 
// MacAlertFileDialogHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
	class MacAlertDialogHandler : IAlertDialogHandler
	{
		public bool Run (AlertDialogData data)
		{
			using (var alert = new NSAlert ()) {
				alert.Window.Title = data.Title ?? BrandingService.ApplicationName;

				if (data.Message.Icon == MonoDevelop.Ide.Gui.Stock.Information) {
					alert.AlertStyle = NSAlertStyle.Critical;
				} else if (data.Message.Icon == MonoDevelop.Ide.Gui.Stock.Warning) {
					alert.AlertStyle = NSAlertStyle.Warning;
				} else { //if (data.Message.Icon == MonoDevelop.Ide.Gui.Stock.Information) {
					alert.AlertStyle = NSAlertStyle.Informational;
				}
				
				//FIXME: use correct size so we don't get horrible scaling?
				if (!string.IsNullOrEmpty (data.Message.Icon)) {
					var pix = ImageService.GetPixbuf (data.Message.Icon, Gtk.IconSize.Dialog);
					byte[] buf = pix.SaveToBuffer ("tiff");
					unsafe {
						fixed (byte* b = buf) {
							alert.Icon = new NSImage (NSData.FromBytes ((IntPtr)b, (uint)buf.Length));
						}
					}
				} else {
					//for some reason the NSAlert doesn't pick up the app icon by default
					alert.Icon = NSApplication.SharedApplication.ApplicationIconImage;
				}
				
				alert.MessageText = data.Message.Text;
				alert.InformativeText = data.Message.SecondaryText ?? "";
				
				var buttons = data.Buttons.Reverse ().ToList ();
				
				for (int i = 0; i < buttons.Count - 1; i++) {
					if (i == data.Message.DefaultButton) {
						var next = buttons[i];
						for (int j = buttons.Count - 1; j >= i; j--) {
							var tmp = buttons[j];
							buttons[j] = next;
							next = tmp;
						}
						break;
					}
				}
				
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
				
				
				NSButton[] optionButtons = null;
				if (data.Options.Count > 0) {
					var box = new MDBox (LayoutDirection.Vertical, 2, 2);
					optionButtons = new NSButton[data.Options.Count];
					
					for (int i = data.Options.Count - 1; i >= 0; i--) {
						var option = data.Options[i];
						var button = new NSButton () {
							Title = option.Text,
							Tag = i,
							State = option.Value? NSCellStateValue.On : NSCellStateValue.Off,
						};
						button.SetButtonType (NSButtonType.Switch);
						optionButtons[i] = button;
						box.Add (new MDAlignment (button, true) { XAlign = LayoutAlign.Begin });
					}
					
					box.Layout ();
					alert.AccessoryView = box.View;
				}
				
				NSButton applyToAllCheck = null;
				if (data.Message.AllowApplyToAll) {
					alert.ShowsSuppressionButton = true;
					applyToAllCheck = alert.SuppressionButton;
					applyToAllCheck.Title = GettextCatalog.GetString ("Apply to all");
				}
				
				// Hack up a slightly wider than normal alert dialog. I don't know how to do this in a nicer way
				// as the min size constraints are apparently ignored.
				var frame = ((NSPanel) alert.Window).Frame;
				((NSPanel) alert.Window).SetFrame (new RectangleF (frame.X, frame.Y, Math.Max (frame.Width, 600), frame.Height), true);
				alert.Layout ();
				
				bool completed = false;
				if (data.Message.CancellationToken.CanBeCanceled) {
					data.Message.CancellationToken.Register (delegate {
						alert.InvokeOnMainThread (() => {
							if (!completed) {
								NSApplication.SharedApplication.AbortModal ();
							}
						});
					});
				}
				
				if (!data.Message.CancellationToken.IsCancellationRequested) {
					int result = alert.RunModal () - (int)NSAlertButtonReturn.First;
					completed = true;
					if (result >= 0 && result < buttons.Count) {
						data.ResultButton = buttons [result];
					} else {
						data.ResultButton = null;
					}
				}
				
				if (data.ResultButton == null || data.Message.CancellationToken.IsCancellationRequested) {
					data.SetResultToCancelled ();
				}
				
				if (optionButtons != null) {
					foreach (var button in optionButtons) {
						var option = data.Options[button.Tag];
						data.Message.SetOptionValue (option.Id, button.State != 0);
					}
				}
				
				if (applyToAllCheck != null && applyToAllCheck.State != 0)
					data.ApplyToAll = true;
				
				GtkQuartz.FocusWindow (data.TransientFor ?? MessageService.RootWindow);
			}
			
			return true;
		}
	}
}
