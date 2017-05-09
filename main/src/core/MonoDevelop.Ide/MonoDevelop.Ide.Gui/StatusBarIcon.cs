//
// StatusBarIcon.cs
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
	public class StatusBarIconClickedEventArgs : EventArgs
	{
		public StatusBarIconClickedEventArgs ()
		{
			Button = Xwt.PointerButton.Left;
			Modifiers = Xwt.ModifierKeys.None;
		}

		public Xwt.PointerButton Button { get; set; }
		public Xwt.ModifierKeys Modifiers { get; set; }
	}

	/// <summary>
	/// The MonoDevelop status bar.
	/// </summary>
	public interface StatusBarIcon : IDisposable
	{
		/// <summary>
		/// The title of the status icon. Used for accessibility
		/// </summary>
		/// <value>The title.</value>
		string Title { get; set; }

		/// <summary>
		/// Tooltip of the status icon
		/// </summary>
		string ToolTip { get; set; }

		/// <summary>
		/// The accessibility help message for the button
		/// </summary>
		/// <value>The help.</value>
		string Help { get; set; }

		/// <summary>
		/// The clicked event to subscribe mouse clicks on the icon.
		/// </summary>
		event EventHandler<StatusBarIconClickedEventArgs> Clicked;
		
		/// <summary>
		/// The icon
		/// </summary>
		Xwt.Drawing.Image Image { get; set; }
		
		/// <summary>
		/// Sets alert mode. The icon will flash for the provided number of seconds.
		/// </summary>
		void SetAlertMode (int seconds);
	}
	
}
