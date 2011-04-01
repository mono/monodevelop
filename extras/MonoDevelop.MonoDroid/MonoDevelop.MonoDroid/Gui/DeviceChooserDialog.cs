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
		ListStore store = new ListStore (typeof (object));
		bool destroyed;
		bool isTrial;
		
		public DeviceChooserDialog ()
		{
			this.Build ();
			
			/*
			var banner = new HeaderBanner () {
				Text = GettextCatalog.GetString ("Select Device"),
				Image = Gdk.Pixbuf.LoadFromResource ("banner.png"),
			};
			bannerPlaceholder.Add (banner);
			bannerPlaceholder.ShowAll ();*/
			
			deviceListTreeView.Model = store;
			var txtRenderer = new CellRendererText ();
			deviceListTreeView.AppendColumn ("Devices", txtRenderer, DeviceDataFunc);
			
			createEmulatorButton.Clicked += delegate {
				MonoDroidFramework.Toolbox.StartAvdManager ();
			};
			
			startEmulatorButton.Clicked += delegate {
				TreeIter iter;
				if (deviceListTreeView.Selection.GetSelected (out iter)) {
					var dd = (DisplayDevice) store.GetValue (iter, 0) ;
					//status.StartOperation (GettextCatalog.GetString ("Starting virtual device '{0}'...", avd.Name));
					if (dd.VirtualDevice != null) {
						MonoDroidFramework.VirtualDeviceManager.StartEmulator (dd.VirtualDevice);
					}
				}
			};
			
			deviceListTreeView.Selection.Changed += UpdatedSelection;
			
			deviceListTreeView.RowActivated += delegate(object o, RowActivatedArgs args) {
				TreeIter iter;
				if (store.GetIter (out iter, args.Path)) {
					var dd = (DisplayDevice) store.GetValue (iter, 0);
					if (dd.Device != null) {
						Device = dd.Device;
						Respond (ResponseType.Ok);
					}
				}
			};
			
			buttonOk.Sensitive = false;
			startEmulatorButton.Sensitive = false;
			
			MonoDroidFramework.DeviceManager.DevicesUpdated += OnDevicesUpdated;
			MonoDroidFramework.VirtualDeviceManager.Changed += OnVirtualDevicesUpdated;
			OnDevicesUpdated (null, EventArgs.Empty);
			
			restartAdbButton.Clicked += delegate {
				store.Clear ();
				restartAdbButton.Sensitive = false;
				MonoDroidFramework.DeviceManager.RestartAdbServer (() => {
					Gtk.Application.Invoke (delegate {
						if (!destroyed)
							restartAdbButton.Sensitive = true;
					});
				});
			};
			
			isTrial = MonoDroidFramework.IsTrial;
			
			if (isTrial) {
				var ib = new MonoDevelop.Components.InfoBar ();
				var img = new Image (typeof (DeviceChooserDialog).Assembly, "information.png");
				img.SetAlignment (0.5f, 0.5f);
				ib.PackEnd (img, false, false, 0);
				var msg = GettextCatalog.GetString ("Trial version only supports the emulator");
				ib.MessageArea.Add (new Gtk.Label (msg) {
					Yalign = 0.5f,
					Xalign = 0f,
					Style = ib.Style,
				});
				string buyMessage;
				if (PropertyService.IsMac) { 
					buyMessage = GettextCatalog.GetString ("Buy Full Version");
				} else {
					buyMessage = GettextCatalog.GetString ("Activate");
				}
				var buyButton = new Button (buyMessage);
				buyButton.Clicked += delegate {
					if (MonoDroidFramework.Activate ())
						UnTrialify ();
					};
				ib.ActionArea.Add (buyButton);
				ib.ShowAll ();
				bannerPlaceholder.Add (ib);
			}
		}
		
		void UnTrialify ()
		{
			isTrial = false;
			var child = bannerPlaceholder.Child;
			bannerPlaceholder.Remove (child);
			child.Destroy ();
			OnDevicesUpdated (null, null);
		}
		
		protected override void OnDestroyed ()
		{
			destroyed = true;
			MonoDroidFramework.DeviceManager.DevicesUpdated -= OnDevicesUpdated;
			MonoDroidFramework.VirtualDeviceManager.Changed -= OnVirtualDevicesUpdated;
			base.OnDestroyed ();
		}

		void OnVirtualDevicesUpdated (IList<AndroidVirtualDevice> list)
		{
			OnDevicesUpdated (null, null);
		}

		void OnDevicesUpdated (object sender, EventArgs e)
		{
			var values = new List<DisplayDevice> ();
			var map = new Dictionary<string,DisplayDevice> ();
			
			var devices = MonoDroidFramework.DeviceManager.Devices;
			if (devices != null) {
				foreach (var dev in MonoDroidFramework.DeviceManager.Devices) {
					var dd = new DisplayDevice () { Device = dev };
					values.Add (dd);
					if (dev.IsEmulator && dev.Properties != null) {
						string avdname;
						if (dev.Properties.TryGetValue ("monodroid.avdname", out avdname))
							map[avdname] = dd;
					}
				}
			}
			
			var avds = MonoDroidFramework.VirtualDeviceManager.VirtualDevices;
			if (avds != null) {
				foreach (var dev in avds) {
					DisplayDevice dd;
					if (map.TryGetValue (dev.Name, out dd))
						dd.VirtualDevice = dev;
					else
						values.Add (new DisplayDevice () { VirtualDevice = dev });
				}
			}
			
			//FIXME sort this
			//values.Sort ();
			
			Gtk.Application.Invoke (delegate {
				LoadData (values);
			});
		}

		void UpdatedSelection (object sender, EventArgs e)
		{
			buttonOk.Sensitive = false;
			startEmulatorButton.Sensitive = false;
			Device = null;
			
			TreeIter iter;
			if (!deviceListTreeView.Selection.GetSelected (out iter))
				return;
			
			var device = (DisplayDevice) store.GetValue (iter, 0);
			if (device.Device != null && (!isTrial || device.VirtualDevice != null)) {
				buttonOk.Sensitive = true;
				Device = device.Device;
			} else if (device.VirtualDevice != null) {
				startEmulatorButton.Sensitive = true;
			}
		}
		
		void DeviceDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var store = (ListStore) model;
			var device = (DisplayDevice) store.GetValue (iter, 0);
			var txtCell = (CellRendererText) cell;
			
			txtCell.Markup = string.Format ("<span color=\"#{0}\">{1}\n    {2}</span>",
				device.Device != null && device.Device.IsOnline ? "000000" : "444444",
				GLib.Markup.EscapeText (device.GetName ()),
				device.GetStatus ());
		}
		
		void LoadData (List<DisplayDevice> devices)
		{
			store.Clear ();
			foreach (var o in devices.OrderBy (d => d.GetName ()))
				store.AppendValues (o);
		}
		
		public AndroidDevice Device { get; private set; }
		
		class DisplayDevice
		{
			public AndroidVirtualDevice VirtualDevice { get; set; }
			public AndroidDevice Device { get; set; }
			
			public string GetName ()
			{
				if (VirtualDevice != null) {
					if (Device != null)
						return string.Format ("{0} ({1})", VirtualDevice.Name, Device.ID);
					return VirtualDevice.Name;
				}
				return Device.ID;
			}
			
			public string GetStatus ()
			{
				if (Device != null)
					return Device.State;
				return "not started";
			}
		}
	}
	
	class HeaderBanner : DrawingArea 
	{
		Gdk.Pixbuf image;
		Pango.Layout layout;
		string text;
		
		public Gdk.Pixbuf Image {
			get { return image; }
			set {
				image = value;
				if (image != null) {
					HeightRequest = image.Height;
				} else {
					HeightRequest = 0;
				}
				QueueDraw ();
			}
		}
		
		public string Text {
			get { return text; }
			set {
				text = value;
				KillLayout ();
				QueueDraw ();
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			KillLayout ();
			if (image != null) {
				image.Dispose ();
				image = null;
			}
		}
		
		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			base.OnStyleSet (previous_style);
			KillLayout ();
		}
		
		Pango.Layout EnsureLayout ()
		{
			if (layout != null)
				layout.Dispose ();
			layout = null;
			
			if (text != null) {
				layout = CreatePangoLayout ("");
				
				//TODO: make this a property?
				var font = PropertyService.IsMac? "Lucida Grande 24" : "Sans 24";
				
				layout.SetMarkup ("<span font=\"" + font + "\">" + text + "</span>");
			}
			
			return layout;
		}
		
		void KillLayout ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			if (image == null)
				return base.OnExposeEvent (evt);
			
			var srcHeight = image.Height;
			var srcWidth = image.Width;
			
			var alloc = this.Allocation;
			var w = Math.Min (alloc.Width, srcWidth);
			var h = Math.Min (alloc.Height, srcHeight);
			
			var gc = this.Style.BlackGC; //dummy
			evt.Window.DrawPixbuf (gc, image, 0, 0, alloc.Left, alloc.Top, w, h, Gdk.RgbDither.Normal, 0, 0);
			
			var layout = EnsureLayout ();
			
			if (layout != null) {
				int txtH, txtW;
				layout.GetPixelSize (out txtW, out txtH);
				int pxFromBottom = 8;
				var textY = alloc.Top + srcHeight - txtH - pxFromBottom;
				
				evt.Window.DrawLayout (this.Style.WhiteGC, 140, textY, layout);
			}
			
			return false;
		}
	}
	
	class StatusLabel : Gtk.HBox
	{
		Image image = new Image ();
		Button cancelButton = new Button ();
		Button detailsButton = new Button ();
		Label label = new Label ();
		Gdk.PixbufAnimation spinner;
		
		public StatusLabel ()
		{
			this.PackStart (image, false, false, 0);
			this.PackStart (label, true, true, 0);
			this.PackStart (cancelButton, false, false, 0);
			this.PackStart (detailsButton, false, false, 0);
			spinner = Gdk.PixbufAnimation.LoadFromResource ("spinner.gif");
			this.HeightRequest = spinner.Height;
			label.Ellipsize = Pango.EllipsizeMode.End;
			label.Xalign = 0;
			label.Show ();
		}
		
		bool spinning;
		bool IsSpinning {
			get { return spinning; }
			set {
				if (spinning == value)
					return;
				spinning = value;
				if (spinning) {
					image.PixbufAnimation = spinner;
					image.Show ();
				} else {
					image.PixbufAnimation = null;
					image.Hide ();
				}
			}
		}
		
		public string Text {
			get { return label.Text; }
			set { label.Text = value; }
		}
		
		public void StartOperation (string message)
		{
			IsSpinning = true;
			Text = message;
		}
		
		public void EndOperation (string message)
		{
			IsSpinning = false;
			Text = message;
		}
	}
}

