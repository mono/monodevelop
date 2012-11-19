//
// StatusBar.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using Gtk;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;

using StockIcons = MonoDevelop.Ide.Gui.Stock;

namespace MonoDevelop.Ide
{
	
	/// <summary>
	/// The MonoDevelop status bar.
	/// </summary>
	public interface StatusBar: StatusBarContextBase
	{
		StatusBar MainContext { get; }
		/// <summary>
		/// Shows a status icon in the toolbar. The icon can be removed by disposing
		/// the StatusBarIcon instance.
		/// </summary>
		StatusBarIcon ShowStatusIcon (Gdk.Pixbuf pixbuf);
		
		/// <summary>
		/// Creates a status bar context. The returned context can be used to show status information
		/// which will be cleared when the context is disposed. When several contexts are created,
		/// the status bar will show the status of the latest created context.
		/// </summary>
		StatusBarContext CreateContext ();
		
		// Clears the status bar information
		void ShowReady ();
		
		/// <summary>
		/// Sets a pad which has detailed information about the status message. When clicking on the
		/// status bar, this pad will be activated. This source pad is reset at every ShowMessage call.
		/// </summary>
		void SetMessageSourcePad (Pad pad);
	}
	
}
