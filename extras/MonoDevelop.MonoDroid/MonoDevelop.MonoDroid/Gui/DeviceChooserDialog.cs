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
		StatusLabel status = new StatusLabel ();
		
		//we only allow use of emulators that were started from this MD instance
		static Dictionary<string,string> runningEmulators = new Dictionary<string,string> ();
		
		public DeviceChooserDialog ()
		{
			this.Build ();
			
			var banner = new HeaderBanner () {
				Text = GettextCatalog.GetString ("Select Device"),
				Image = Gdk.Pixbuf.LoadFromResource ("banner.png"),
			};
			bannerPlaceholder.Add (banner);
			
			deviceListTreeView.Model = store;
			var txtRenderer = new CellRendererText ();
			deviceListTreeView.AppendColumn ("Devices", txtRenderer, DeviceDataFunc);
			
			createEmulatorButton.Clicked += delegate {
				MonoDroidFramework.Toolbox.StartAvdManager ();
			};
			
			startEmulatorButton.Clicked += delegate {
				TreeIter iter;
				if (deviceListTreeView.Selection.GetSelected (out iter)) {
					var avd = store.GetValue (iter, 0) as AndroidVirtualDevice;
					//status.StartOperation (GettextCatalog.GetString ("Starting virtual device '{0}'...", avd.Name));
					if (avd != null) {
						MonoDroidFramework.DeviceManager.StartEmulator (avd);
					}
				}
			};
			
			deviceListTreeView.Selection.Changed += UpdatedSelection;
			
			buttonOk.Sensitive = false;
			startEmulatorButton.Sensitive = false;
			
			MonoDroidFramework.DeviceManager.DevicesUpdated += OnDevicesUpdated;
			OnDevicesUpdated (null, EventArgs.Empty);
		}
		
		protected override void OnDestroyed ()
		{
			MonoDroidFramework.DeviceManager.DevicesUpdated -= OnDevicesUpdated;
			base.OnDestroyed ();
		}

		void OnDevicesUpdated (object sender, EventArgs e)
		{
			var values = new Dictionary<string,object> ();
			if (MonoDroidFramework.DeviceManager.VirtualDevices != null)
				foreach (var dev in MonoDroidFramework.DeviceManager.VirtualDevices)
					values.Add (dev.Name, dev);
			if (MonoDroidFramework.DeviceManager.Devices != null)
				foreach (var dev in MonoDroidFramework.DeviceManager.Devices)
					values.Add (dev.ID, dev);
			LoadData (values);
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
		
		void LoadData (Dictionary<string,object> devices)
		{
			store.Clear ();
			
			foreach (var o in devices.OrderBy (kvp => kvp.Key))
				store.AppendValues (o.Value);
		}
		
		public AndroidDevice Device { get; private set; }
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
			spinner = Gdk.PixbufAnimation.LoadFromResource ("spinner-big.gif");
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

