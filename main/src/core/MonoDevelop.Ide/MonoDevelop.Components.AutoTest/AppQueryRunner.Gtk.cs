//
// AppQueryRunner.Gtk.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Collections.Generic;
using MonoDevelop.Components.AutoTest.Results;

namespace MonoDevelop.Components.AutoTest
{
	partial class AppQueryRunner
	{
		void ProcessGtkWindows (AppResult rootNode, ref AppResult lastChild)
		{
			Gtk.Window [] windows = Gtk.Window.ListToplevels ();

			foreach (var window in windows) {
				ProcessGtkWindow (window, rootNode, ref lastChild);
			}
		}

		void ProcessGtkWindow (Gtk.Window window, AppResult rootNode, ref AppResult lastChild)
		{
			AppResult node = AddGtkResult (window);
			if (node == null)
				return;

			AddChild (rootNode, node, ref lastChild);

			// Create the children list and link them onto the node
			GenerateChildrenForContainer (node, window);
		}

		void GenerateChildrenForContainer (AppResult parent, Gtk.Widget widget)
		{
			if (!(widget is Gtk.Container container))
				return;

			AppResult lastChild = null;

			foreach (var child in container.Children) {
				AppResult node = AddGtkResult (child);
				if (node == null)
					continue;

				AddChild (parent, node, ref lastChild);
				GenerateChildrenForContainer (node, child);
			}
		}

		AppResult AddGtkResult (Gtk.Widget widget)
		{
			if (!includeHidden && widget.Visible == false) {
				return null;
			}

			AppResult node = new GtkWidgetResult (widget) { SourceQuery = sourceQuery };
			return AddToResultSet (node);
		}
	}
}
