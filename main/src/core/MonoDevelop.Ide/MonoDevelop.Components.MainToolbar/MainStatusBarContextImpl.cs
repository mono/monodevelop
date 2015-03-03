//
// MainStatusBarContextImpl.cs
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
using MonoDevelop.Ide;

namespace MonoDevelop.Components.MainToolbar
{
	
	/// <summary>
	/// The MonoDevelop status bar.
	/// </summary>
	
	class MainStatusBarContextImpl: StatusBarContextImpl, StatusBar
	{
		public StatusBar MainContext {
			get {
				return this;
			}
		}
		public MainStatusBarContextImpl (StatusBarContextHandler statusHandler): base (statusHandler)
		{
			StatusChanged = true;
		}
		
		public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			return statusBar.ShowStatusIcon (pixbuf);
		}
		
		public StatusBarContext CreateContext ()
		{
			return statusBar.CreateContext ();
		}
		
		public void ShowReady ()
		{
			statusBar.ShowReady ();
		}
		
		public void SetMessageSourcePad (Pad pad)
		{
			StatusSourcePad = pad;
			if (statusHandler.IsCurrentContext (this))
				statusBar.SetMessageSourcePad (pad);
		}
		
		protected override void OnMessageChanged ()
		{
			StatusSourcePad = null;
		}
	}
}
