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
using System.Linq;
using System.Collections.Generic;

using Foundation;
using AppKit;
using CoreGraphics;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components.Extensions;
using MonoDevelop.MacInterop;
using MonoDevelop.Components;
	
namespace MonoDevelop.MacIntegration
{
	class MacAlertDialogHandler : IAlertDialogHandler
	{
		public bool Run (AlertDialogData data)
		{
			using (var alert = new NSAlert ()) {
				alert.Window.Title = data.Title ?? BrandingService.ApplicationName;
				IdeTheme.ApplyTheme (alert.Window);

				bool stockIcon;
				if (data.Message.Icon == MonoDevelop.Ide.Gui.Stock.Error || data.Message.Icon == Gtk.Stock.DialogError) {
					alert.AlertStyle = NSAlertStyle.Critical;
					stockIcon = true;
				} else if (data.Message.Icon == MonoDevelop.Ide.Gui.Stock.Warning || data.Message.Icon == Gtk.Stock.DialogWarning) {
					alert.AlertStyle = NSAlertStyle.Critical;
					stockIcon = true;
				} else {
					alert.AlertStyle = NSAlertStyle.Informational;
					stockIcon = data.Message.Icon == MonoDevelop.Ide.Gui.Stock.Information;
				}

				if (!stockIcon && !string.IsNullOrEmpty (data.Message.Icon)) {
					var img = ImageService.GetIcon (data.Message.Icon, Gtk.IconSize.Dialog);
					// HACK: VK The icon is not rendered in dark style correctly
					//       Use light variant and reder it here
					//       as long as NSAppearance.NameVibrantDark is broken
					if (IdeTheme.UserInterfaceTheme == Theme.Dark)
						alert.Icon = img.WithStyles ("-dark").ToBitmap (GtkWorkarounds.GetScaleFactor ()).ToNSImage ();
					else
						alert.Icon = img.ToNSImage ();
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
				
				var wrappers = new List<AlertButtonWrapper> (buttons.Count);
				foreach (var button in buttons) {
					var label = button.Label;
					if (button.IsStockButton)
						label = Gtk.Stock.Lookup (label).Label;
					label = label.Replace ("_", "");

					//this message seems to be a standard Mac message since alert handles it specially
					if (button == AlertButton.CloseWithoutSave)
						label = GettextCatalog.GetString ("Don't Save");

					var nsbutton = alert.AddButton (label);
					var wrapperButton = new AlertButtonWrapper (nsbutton, data.Message, button, alert);
					wrappers.Add (wrapperButton);
					nsbutton.Target = wrapperButton;
					nsbutton.Action = new ObjCRuntime.Selector ("buttonActivatedAction");
				}
				
				
				NSButton[] optionButtons = null;
				if (data.Options.Count > 0) {
					var box = new MDBox (LayoutDirection.Vertical, 2, 2);
					optionButtons = new NSButton[data.Options.Count];
					
					for (int i = data.Options.Count - 1; i >= 0; i--) {
						var option = data.Options[i];
						var button = new NSButton {
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
				var frame = alert.Window.Frame;
				alert.Window.SetFrame (new CGRect (frame.X, frame.Y, NMath.Max (frame.Width, 600), frame.Height), true);
				alert.Layout ();
				
				bool completed = false;
				if (data.Message.CancellationToken.CanBeCanceled) {
					data.Message.CancellationToken.Register (delegate {
						alert.InvokeOnMainThread (() => {
							if (!completed) {
								if (alert.Window.IsSheet && alert.Window.SheetParent != null)
									alert.Window.SheetParent.EndSheet (alert.Window);
								else
									NSApplication.SharedApplication.AbortModal ();
							}
						});
					});
				}

				int response = -1000;

				if (!data.Message.CancellationToken.IsCancellationRequested) {
					NSWindow parent = null;
					if (IdeTheme.UserInterfaceTheme != Theme.Dark || MacSystemInformation.OsVersion < MacSystemInformation.HighSierra) // sheeting is broken on High Sierra with dark NSAppearance
						parent = data.TransientFor ?? IdeApp.Workbench.RootWindow;

					if (parent == null) {
						response = (int)alert.RunModal ();
					} else {
						alert.BeginSheet (parent, (modalResponse) => {
							response = (int)modalResponse;
							NSApplication.SharedApplication.StopModal ();
						});

						NSApplication.SharedApplication.RunModalForWindow (alert.Window);
					}
				}

				var result = response - (long)(int)NSAlertButtonReturn.First;

				completed = true;

				if (result >= 0 && result < buttons.Count) {
					data.ResultButton = buttons [(int)result];
				} else {
					data.ResultButton = null;
				}

				if (data.ResultButton == null || data.Message.CancellationToken.IsCancellationRequested) {
					data.SetResultToCancelled ();
				}
				
				if (optionButtons != null) {
					foreach (var button in optionButtons) {
						var option = data.Options[(int)button.Tag];
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

	class AlertButtonWrapper : NSObject
	{
		readonly NSButton nsbutton;
		readonly MessageDescription message;
		readonly AlertButton alertButton;
		readonly ObjCRuntime.Selector oldAction;
		readonly NSAlert alert;
		public AlertButtonWrapper (NSButton nsbutton, MessageDescription message, AlertButton alertButton, NSAlert alert)
		{
			this.nsbutton = nsbutton;
			this.message = message;
			this.alertButton = alertButton;
			this.alert = alert;
			oldAction = nsbutton.Action;
		}

		[Export ("buttonActivatedAction")]
		void ButtonActivatedAction ()
		{
			bool close = message.NotifyClicked (alertButton);
			if (close)
				nsbutton.SendAction (oldAction, alert);
		}
	}
}
