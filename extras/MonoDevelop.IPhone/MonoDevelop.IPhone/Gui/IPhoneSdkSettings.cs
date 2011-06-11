// 
// IPhoneSdkSettings.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin, Inc.
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;

namespace MonoDevelop.IPhone.Gui
{
	class IPhoneSdkSettings : OptionsPanel
	{
		IPhoneSdkSettingsWidget w;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return w = new IPhoneSdkSettingsWidget ();
		}
		
		public override void ApplyChanges ()
		{
			w.ApplyChanges ();
		}
	}
	
	partial class IPhoneSdkSettingsWidget : Gtk.Bin
	{
		public IPhoneSdkSettingsWidget ()
		{
			this.Build ();
			
			string configuredNativeSdk = IPhoneSdks.GetConfiguredNativeSdkRoot ();
			string configuredMonoTouchSdk = IPhoneSdks.GetConfiguredMonoTouchSdkRoot ();
			
			iphoneSdkFolderEntry.Path = configuredNativeSdk ?? "";
			monoTouchSdkFolderEntry.Path = configuredMonoTouchSdk ?? "";
			
			iphoneSdkFolderEntry.PathChanged += delegate {
				ValidateNative ();
			};
			monoTouchSdkFolderEntry.PathChanged += delegate {
				ValidateMonoTouch ();
			};
			ValidateNative ();
			ValidateMonoTouch ();
		}
		
		void ValidateNative ()
		{
			FilePath location = CleanPath (iphoneSdkFolderEntry.Path);
			if (!location.IsNullOrEmpty) {
				if (AppleIPhoneSdk.ValidateSdkLocation (location)) {
					iphoneLocationMessage.Text = GettextCatalog.GetString ("No SDK found at specified location.");
					iphoneLocationIcon.Stock = Gtk.Stock.Cancel;
				} else {
					iphoneLocationMessage.Text = GettextCatalog.GetString ("SDK found at specified location.");
					iphoneLocationIcon.Stock = Gtk.Stock.Apply;
				}
			} else if (AppleIPhoneSdk.ValidateSdkLocation ("/Developer")) {
				iphoneLocationMessage.Text = GettextCatalog.GetString ("SDK found at default location.");
				iphoneLocationIcon.Stock = Gtk.Stock.Apply;
			}
		}
		
		void ValidateMonoTouch ()
		{
			FilePath location = CleanPath (monoTouchSdkFolderEntry.Path);
			if (!location.IsNullOrEmpty) {
				if (MonoTouchSdk.ValidateSdkLocation (location)) {
					monotouchLocationMessage.Text = GettextCatalog.GetString ("No SDK found at specified location.");
					monotouchLocationIcon.Stock = Gtk.Stock.Cancel;
				} else {
					monotouchLocationMessage.Text = GettextCatalog.GetString ("SDK found at specified location.");
					monotouchLocationIcon.Stock = Gtk.Stock.Apply;
				}
			} else if (MonoTouchSdk.ValidateSdkLocation ("/Developer")) {
				monotouchLocationMessage.Text = GettextCatalog.GetString ("SDK found at default location.");
				monotouchLocationIcon.Stock = Gtk.Stock.Apply;
			}
		}
		
		static string CleanPath (string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			path = System.IO.Path.GetFullPath (path);
			if (path == "/Developer")
				return null;
			return path;
		}
		
		public void ApplyChanges ()
		{
			IPhoneSdks.SetConfiguredNativeSdkRoot (CleanPath (iphoneSdkFolderEntry.Path));
			IPhoneSdks.SetConfiguredMonoTouchSdkRoot (CleanPath (monoTouchSdkFolderEntry.Path));
		}
	}
}

