//
// IShellView.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Ide.Codons;
using MonoDevelop.Components.Docking;
using System.Collections.Generic;
using MonoDevelop.Components.DockNotebook;

namespace MonoDevelop.Ide.Gui
{
	interface IShellView
	{
		void Initialize ();
		void Close ();

		void ShowCommandBar (string barId);
		void HideCommandBar (string barId);

		StatusBar StatusBar { get; }
		bool FullScreen { get; set; }

		ICustomXmlSerializer Memento { get; set; }

		void ShowView (IViewContent content, bool bringToFront, DockNotebook notebook = null);
		void CloseAllViews();

		void AddPad (PadCodon content);
		void ShowPad (PadCodon content);

		DockItem GetDockItem (PadCodon padContent);
		string CurrentLayout { get; set; }
		IList<string> Layouts { get; }
		void DeleteLayout (string name);

		event EventHandler ActiveWorkbenchWindowChanged;
	}
}

