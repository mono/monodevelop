// 
// MonoTouchSdk.cs
//  
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using MonoDevelop.Core;
using System.IO;
using Gtk;

namespace MonoDevelop.IPhone
{
	public class MonoTouchSdk
	{
		static DateTime lastMTExeWrite = DateTime.MinValue;
		
		public MonoTouchSdk (FilePath devRoot)
		{
			this.SdkDir = devRoot.Combine ("MonoTouch");
			Init ();
		}
		
		public FilePath SdkDir { get; private set; }
		public FilePath BinDir { get { return SdkDir.Combine ("usr/bin"); } }
		public FilePath LibDir { get { return SdkDir.Combine ("usr/lib"); } }
		
		void Init ()
		{
			IsInstalled = File.Exists (BinDir.Combine ("mtouch"));
			if (IsInstalled) {
				lastMTExeWrite = File.GetLastWriteTime (BinDir.Combine ("mtouch"));
				IsEvaluation = !File.Exists (BinDir.Combine ("arm-darwin-mono"));
				Version = ReadVersion ();
			} else {
				lastMTExeWrite = DateTime.MinValue;
				IsEvaluation = false;
				Version = new IPhoneSdkVersion ();
			}
		}
		
		IPhoneSdkVersion ReadVersion ()
		{
			var versionFile = SdkDir.Combine ("Version");
			if (File.Exists (versionFile)) {
				try {
					return IPhoneSdkVersion.Parse (File.ReadAllText (versionFile).Trim ());
				} catch (Exception ex) {
					LoggingService.LogError ("Failed to read MonoTouch version", ex);
				}
			}
			return new IPhoneSdkVersion ();
		}
		
		public bool IsInstalled { get; private set; }
		public bool IsEvaluation { get; private set; }
		public IPhoneSdkVersion Version { get; private set; }
		
		public void ShowEvaluationDialog ()
		{
			if (!IsEvaluation)
				return;
			
			var dialog = new Dialog ();
			dialog.Title = GettextCatalog.GetString ("Evaluation Version");
			
			dialog.VBox.PackStart (
			 	new Label (GettextCatalog.GetString (
					"<b><big>Feature Not Available In Evaluation Version</big></b>"))
				{
					Xalign = 0.5f,
					UseMarkup = true
				}, true, false, 12);
			
			var align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f) { LeftPadding = 12, RightPadding = 12 };
			dialog.VBox.PackStart (align, true, false, 12);
			align.Add (new Label (GettextCatalog.GetString (
				"You should upgrade to the full version of MonoTouch to target and deploy\n" +
				" to the device, and to enable your applications to be distributed."))
				{
					Xalign = 0.5f,
					Justify = Justification.Center
				});
			
			align = new Gtk.Alignment (0.5f, 0.5f, 1.0f, 1.0f) { LeftPadding = 12, RightPadding = 12 };
			dialog.VBox.PackStart (align, true, false, 12);
			var buyButton = new Button (
				new Label (GettextCatalog.GetString ("<big>Buy MonoTouch</big>")) { UseMarkup = true } );
			buyButton.Clicked += delegate {
				System.Diagnostics.Process.Start ("http://monotouch.net");
				dialog.Respond (ResponseType.Accept);
			};
			align.Add (buyButton);
			
			dialog.AddButton (GettextCatalog.GetString ("Continue evaluation"), ResponseType.Close);
			dialog.ShowAll ();
			
			MonoDevelop.Ide.MessageService.ShowCustomDialog (dialog);
		}
		
		internal void CheckCaches ()
		{
			DateTime lastWrite = DateTime.MinValue;
			try {
				lastWrite = File.GetLastWriteTime (BinDir.Combine ("mtouch"));
				if (lastWrite == lastMTExeWrite)
					return;
			} catch (IOException) {
			}
			
			Init ();
		}
	}
}
