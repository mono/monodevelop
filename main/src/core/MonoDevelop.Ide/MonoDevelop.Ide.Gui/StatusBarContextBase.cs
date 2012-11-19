//
// StatusBarContextBase.cs
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
	
	public interface StatusBarContextBase: IDisposable
	{
		/// <summary>
		/// Shows a message with an error icon
		/// </summary>
		void ShowError (string error);
		
		/// <summary>
		/// Shows a message with a warning icon
		/// </summary>
		void ShowWarning (string warning);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (string message);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (string message, bool isMarkup);
		
		/// <summary>
		/// Shows a message in the status bar
		/// </summary>
		void ShowMessage (IconId image, string message);
		
		/// <summary>
		/// Shows a progress bar, with the provided label next to it
		/// </summary>
		void BeginProgress (string name);
		
		/// <summary>
		/// Shows a progress bar, with the provided label and icon next to it
		/// </summary>
		void BeginProgress (IconId image, string name);
		
		/// <summary>
		/// Sets the progress fraction. It can only be used after calling BeginProgress.
		/// </summary>
		void SetProgressFraction (double work);

		/// <summary>
		/// Hides the progress bar shown with BeginProgress
		/// </summary>
		void EndProgress ();
		
		/// <summary>
		/// Pulses the progress bar shown with BeginProgress
		/// </summary>
		void Pulse ();
		
		/// <summary>
		/// When set, the status bar progress will be automatically pulsed at short intervals
		/// </summary>
		bool AutoPulse { get; set; }
	}
	
}
