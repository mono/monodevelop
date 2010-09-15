// 
// DeviceChooserDialog.cs
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
using MonoDevelop.MonoDroid;
using MonoDevelop.Core;
using System.Collections.Generic;
using Gtk;
using System.Linq;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.MonoDroid.Gui
{
	public partial class DeviceChooserDialog : Gtk.Dialog
	{
		Gdk.Pixbuf banner;
		Pango.Layout bannerText;
		
		ListStore store = new ListStore (typeof (object));
		
		public DeviceChooserDialog ()
		{
			this.Build ();
			
			banner = Gdk.Pixbuf.LoadFromResource ("banner.png");
			bannerPlaceholder.HeightRequest = banner.Height;
			
			bannerPlaceholder.ExposeEvent += BannerExpose;
			
			deviceListTreeView.Model = store;
			var txtRenderer = new CellRendererText ();
			deviceListTreeView.AppendColumn ("Devices", txtRenderer, DeviceDataFunc);
			
			refreshButton.Clicked += delegate {
				RefreshList ();
			};
			
			createEmulatorButton.Clicked += delegate {
				MonoDroidFramework.Toolbox.StartAvdManager ();
			};
			
			startEmulatorButton.Clicked += delegate {
				TreeIter iter;
				if (deviceListTreeView.Selection.GetSelected (out iter)) {
					var avd = store.GetValue (iter, 0) as AndroidVirtualDevice;
					if (avd != null) {
						//FIXME: actually log the output and status
						var op = MonoDroidFramework.Toolbox.StartAvd (avd);
						op.Completed += delegate {
							if (!op.Success) {
								MonoDevelop.Ide.MessageService.ShowError (
									"Failed to start AVD",
									op.ErrorText);
							}
							RefreshList ();
						};
					}
				}
			};
			
			deviceListTreeView.Selection.Changed += UpdatedSelection;
			
			buttonOk.Sensitive = false;
			startEmulatorButton.Sensitive = false;
			
			RefreshList ();
		}

		void UpdatedSelection (object sender, EventArgs e)
		{
			buttonOk.Sensitive = false;
			startEmulatorButton.Sensitive = false;
			Device = null;
			
			TreeIter iter;
			if (!deviceListTreeView.Selection.GetSelected (out iter))
				return;
			
			var device = store.GetValue (iter, 0);
			var avd = device as AndroidVirtualDevice;
			if (avd != null) {
				startEmulatorButton.Sensitive = true;
			} else {
				Device = (AndroidDevice) device;
				buttonOk.Sensitive = true;
			}
		}
		
		void DeviceDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var store = (ListStore) model;
			var device = store.GetValue (iter, 0);
			var txtCell = (CellRendererText) cell;
			
			var avd = device as AndroidVirtualDevice;
			if (avd != null) {
				 txtCell.Markup = string.Format ("<span color=\"#444444\">{0} ({1})</span>",
					GLib.Markup.EscapeText (avd.Name), GettextCatalog.GetString ("Not running"));
			} else {
				var dev = (AndroidDevice) device;
				txtCell.Markup = GLib.Markup.EscapeText (dev.ID);
			}
		}
		
		void EnsureBannerLayout ()
		{
			if (bannerText != null)
				bannerText.Dispose ();
			bannerText = CreatePangoLayout ("");
			var msg = GettextCatalog.GetString ("Select Device");
			var font = PropertyService.IsMac? "Lucida Grande 24" : "Sans 24";
			
			bannerText.SetMarkup ("<span font=\"" + font + "\">" + msg + "</span>");
		}
		
		void KillBannerLayout ()
		{
			if (bannerText != null) {
				bannerText.Dispose ();
				bannerText = null;
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			KillBannerLayout ();
		}
		
		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			base.OnStyleSet (previous_style);
			KillBannerLayout ();
		}
		
		[GLib.ConnectBefore]
		void BannerExpose (object o, Gtk.ExposeEventArgs args)
		{
			var evt = args.Event;
			var srcHeight = banner.Height;
			var srcWidth = banner.Width;
			
			var alloc = bannerPlaceholder.Allocation;
			var w = Math.Min (alloc.Width, srcWidth);
			var h = Math.Min (alloc.Height, srcHeight);
			
			var gc = bannerPlaceholder.Style.BlackGC; //dummy
			evt.Window.DrawPixbuf (gc, banner, 0, 0, alloc.Left, alloc.Top, w, h, Gdk.RgbDither.Normal, 0, 0);
			
			EnsureBannerLayout ();
			
			int txtH, txtW;
			bannerText.GetPixelSize (out txtW, out txtH);
			int pxFromBottom = 8;
			var textY = alloc.Top + srcHeight - txtH - pxFromBottom;
			
			evt.Window.DrawLayout (bannerPlaceholder.Style.WhiteGC, 140, textY, bannerText);
			
			args.RetVal = false;
		}
		
		//FIXME: hook up to a progress indicator, add a cancel button and display errors
		void RefreshList ()
		{
			refreshButton.Sensitive = false;
			store.Clear ();
			
			var op = new AggregatedAsyncOperation ();
			var devicesOp = MonoDroidFramework.Toolbox.GetDevices (Console.Out);
			var virtualDevicesOp = MonoDroidFramework.Toolbox.GetAllVirtualDevices (Console.Out);
			op.Add (devicesOp);
			op.Add (virtualDevicesOp);
			op.StartMonitoring ();
			op.Completed += delegate {
				try {
					if (devicesOp.Success || virtualDevicesOp.Success) {
						LoadDeviceData (devicesOp.Result, virtualDevicesOp.Result);
					} else {
						Gtk.Application.Invoke (delegate {
							refreshButton.Sensitive = true;
						});
					}
					devicesOp.Dispose ();
					virtualDevicesOp.Dispose ();
				} catch (Exception ex) {
					LoggingService.LogError ("Error loading device data", ex);
				}
			};
		}
		
		void LoadDeviceData (List<AndroidDevice> devices, List<AndroidVirtualDevice> virtualDevices)
		{
			var uniqueDevices = new Dictionary<string,object> ();
			if (devices != null) {
				foreach (var d in devices) {
					uniqueDevices.Add (d.ID, d);
				}
			}
			if (virtualDevices != null) {
				foreach (var avd in virtualDevices) {
					//FIXME: figure out how to match these properly
					if (!uniqueDevices.ContainsKey (avd.Name))
						uniqueDevices.Add (avd.Name, avd);
				}
			}
			Gtk.Application.Invoke (delegate {
				foreach (var o in uniqueDevices.OrderBy (kvp => kvp.Key))
					store.AppendValues (o.Value);
				refreshButton.Sensitive = true;
			});
		}
		
		public AndroidDevice Device { get; private set; }
	}
}

