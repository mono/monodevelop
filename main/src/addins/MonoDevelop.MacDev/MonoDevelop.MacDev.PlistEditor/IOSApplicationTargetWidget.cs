// 
// IOSApplicationTargetWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using MonoDevelop.Core;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class IOSApplicationTargetWidget : Gtk.Bin
	{
		const string IdentifierKey = "CFBundleIdentifier";
		const string VersionKey = "CFBundleVersion";
		
		PDictionary dict;
		public PDictionary Dict {
			get {
				return dict;
			}
			set {
				dict = value;
				dict.Changed += HandleDictChanged;
				Update ();
			}
		}

		void HandleDictChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		static string[] knownOSVersions = new [] {
			"3.0",
			"3.1",
			"3.1.2",
			"3.2",
			"4.0",
			"4.1",
			"4.2",
			"4.3"
		};
		
		public IOSApplicationTargetWidget ()
		{
			this.Build ();
			
			Gtk.ListStore devices = new Gtk.ListStore (typeof (string));
			devices.AppendValues (GettextCatalog.GetString ("iPhone/iPod"));
			devices.AppendValues (GettextCatalog.GetString ("iPad"));
			devices.AppendValues (GettextCatalog.GetString ("Universal"));
			
			comboboxDevices.Model = devices;
			comboboxDevices.Changed += delegate {
				switch (comboboxDevices.Active) {
				case 0: // iPhone
					dict.Remove ("UISupportedInterfaceOrientations~ipad");
					dict.GetArray ("UISupportedInterfaceOrientations");
					dict.QueueRebuild ();
					break;
				case 1: // iPad
					dict.Remove ("UISupportedInterfaceOrientations");
					dict.GetArray ("UISupportedInterfaceOrientations~ipad");
					dict.QueueRebuild ();
					break;
				default: // Universal
					dict.GetArray ("UISupportedInterfaceOrientations");
					dict.GetArray ("UISupportedInterfaceOrientations~ipad");
					dict.QueueRebuild ();
					break;
				}
			};
			
			entryIdentifier.Changed += delegate {
				dict.SetString (IdentifierKey, entryIdentifier.Text);
			};
			
			entryVersion.Changed += delegate {
				dict.SetString (VersionKey, entryVersion.Text);
			};
			
			foreach (var ver in knownOSVersions.Reverse ()) {
				comboboxentryDeploymentTarget.AppendText (ver);
			}
		}
		
		public void Update ()
		{
			var identifier = dict.Get<PString> (IdentifierKey);
			entryIdentifier.Text = identifier != null ? identifier : "";
			
			var version = dict.Get<PString> (VersionKey);
			entryVersion.Text = version != null ? version : "";
			
			var iphone = dict.Get<PArray> ("UISupportedInterfaceOrientations");
			var ipad   = dict.Get<PArray> ("UISupportedInterfaceOrientations~ipad");
			
			if (iphone != null && ipad != null) {
				comboboxDevices.Active = 2;
			} else if (ipad != null) {
				comboboxDevices.Active = 1;
			} else {
				comboboxDevices.Active = 0;
			}
		}
	}
}

