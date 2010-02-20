// 
// MeeGoDevicePicker.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.MeeGo
{
	partial class MeeGoDevicePicker : Dialog
	{
		public MeeGoDevicePicker (Window parentWindow)
		{
			TransientFor = parentWindow;
			Modal = true;
			this.Build ();
			
			userEntry.Text = PropertyService.Get<string> ("MeeGoDevice.User") ?? "";
			passwordEntry.Text = PropertyService.Get<string> ("MeeGoDevice.Password") ?? "";
			addressEntry.Text = PropertyService.Get<string> ("MeeGoDevice.Address") ?? "";
			
			addressEntry.Changed += delegate {
				buttonOk.Sensitive = !string.IsNullOrEmpty (addressEntry.Text);
			};
			buttonOk.Sensitive = !string.IsNullOrEmpty (addressEntry.Text);
		}
		
		MeeGoDevice GetDevice ()
		{
			PropertyService.Set ("MeeGoDevice.User", userEntry.Text);
			PropertyService.Set ("MeeGoDevice.Password", passwordEntry.Text);
			PropertyService.Set ("MeeGoDevice.Address", addressEntry.Text);
			
			return new MeeGoDevice (addressEntry.Text, userEntry.Text, passwordEntry.Text);
		}
		
		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}
		
		public static MeeGoDevice GetDevice (Window parentWindow)
		{
			using (var dialog = new MeeGoDevicePicker (parentWindow ?? MessageService.RootWindow)) {
				int result = dialog.Run ();
				if (result != (int)ResponseType.Ok)
					return null;
				return dialog.GetDevice ();
			}
		}
	}
}

