// 
// MonoDroidSdkSettings.cs
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDroid;

namespace MonoDevelop.MonoDroid.Gui
{
	class MonoDroidSdkSettings : OptionsPanel
	{
		MonoDroidSdkSettingsWidget w;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return w = new MonoDroidSdkSettingsWidget ();
		}
		
		public override void ApplyChanges ()
		{
			w.ApplyChanges ();
		}
	}
	
	partial class MonoDroidSdkSettingsWidget : Gtk.Bin
	{
		string[] pathDirs;
		bool isAndroidPathValid;
		bool isJavaPathValid;
		
		public MonoDroidSdkSettingsWidget ()
		{
			this.Build ();
			
			var path = Environment.GetEnvironmentVariable ("PATH");
			pathDirs = path.Split (new char[] { System.IO.Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
			
			
			string configuredAndroidSdk, configuredJavaSdk;
			MonoDroidSdk.GetConfiguredSdkLocations (out configuredAndroidSdk, out configuredJavaSdk);
			
			androidFolderEntry.Path = configuredAndroidSdk ?? "";
			javaFolderEntry.Path = configuredJavaSdk ?? "";
			
			androidFolderEntry.PathChanged += delegate {
				ValidateAndroid ();
				OnSettingsChanged (EventArgs.Empty);
			};
			javaFolderEntry.PathChanged += delegate {
				ValidateJava ();
				OnSettingsChanged (EventArgs.Empty);
			};
			
			ValidateAndroid ();
			ValidateJava ();
		}
		
		public bool IsAndroidPathValid {
			get {
				return isAndroidPathValid;
			}
		}
		
		public bool IsJavaPathValid {
			get {
				return isJavaPathValid;
			}
		}
		
		void ValidateAndroid ()
		{
			FilePath location = androidFolderEntry.Path;
			
			if (!location.IsNullOrEmpty) {
				if (!MonoDroidSdk.ValidateAndroidSdkLocation (location)) {
					androidLocationMessage.Text = GettextCatalog.GetString ("No SDK found at specified location.");
					androidLocationIcon.Stock = Gtk.Stock.Cancel;
					isAndroidPathValid = false;
				} else {
					androidLocationMessage.Text = GettextCatalog.GetString ("SDK found at specified location.");
					androidLocationIcon.Stock = Gtk.Stock.Apply;
					isAndroidPathValid = true;
				}
				return;
			}
			
			location = MonoDroidSdk.FindAndroidSdk (pathDirs);
			if (location.IsNullOrEmpty) {
				androidLocationMessage.Text = GettextCatalog.GetString ("SDK not found. Please specify location.");
				androidLocationIcon.Stock = Gtk.Stock.Cancel;
				isAndroidPathValid = false;
			} else {
				androidLocationMessage.Text = GettextCatalog.GetString ("SDK found automatically.");
				androidLocationIcon.Stock = Gtk.Stock.Apply;
				isAndroidPathValid = true;
			}
		}
		
		void ValidateJava ()
		{
			FilePath location = javaFolderEntry.Path;
			
			if (!location.IsNullOrEmpty) {
				if (!MonoDroidSdk.ValidateJavaSdkLocation (location)) {
					javaLocationMessage.Text = GettextCatalog.GetString ("No SDK found at specified location.");
					javaLocationIcon.Stock = Gtk.Stock.Cancel;
					isJavaPathValid = false;
				} else {
					javaLocationMessage.Text = GettextCatalog.GetString ("SDK found at specified location.");
					javaLocationIcon.Stock = Gtk.Stock.Apply;
					isJavaPathValid = true;
				}
				return;
			}
			
			location = MonoDroidSdk.FindJavaSdk (pathDirs);
			if (location.IsNullOrEmpty) {
				javaLocationMessage.Text = GettextCatalog.GetString ("SDK not found. Please specify location.");
				javaLocationIcon.Stock = Gtk.Stock.Cancel;
				isAndroidPathValid = false;
			} else {
				javaLocationMessage.Text = GettextCatalog.GetString ("SDK found automatically.");
				javaLocationIcon.Stock = Gtk.Stock.Apply;
				isJavaPathValid = true;
			}
		}
		
		public void ApplyChanges ()
		{
			MonoDroidSdk.SetConfiguredSdkLocations (androidFolderEntry.Path, javaFolderEntry.Path);
			MonoDroidFramework.UpdateSdkLocations ();
		}
		
		protected virtual void OnSettingsChanged (EventArgs args)
		{
			if (SettingsChanged != null)
				SettingsChanged (this, args);
		}
		
		public event EventHandler SettingsChanged;
	}
}

