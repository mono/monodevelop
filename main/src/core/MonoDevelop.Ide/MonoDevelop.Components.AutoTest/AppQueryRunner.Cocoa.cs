//
// AppQueryRunner.Cocoa.cs
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
using System.Diagnostics;

#if MAC
using AppKit;
using MonoDevelop.Components.AutoTest.Results;
#endif

namespace MonoDevelop.Components.AutoTest
{
	partial class AppQueryRunner
	{
#if MAC
		void ProcessNSWindows (AppResult rootNode, ref AppResult lastChild)
		{
			NSWindow [] nswindows = NSApplication.SharedApplication.Windows;
			if (nswindows != null) {
				foreach (var window in nswindows) {
					ProcessNSWindow (window, rootNode, ref lastChild);
				}
			}
		}

		void ProcessNSWindow (NSWindow window, AppResult rootNode, ref AppResult lastChild)
		{
			AppResult node = new NSObjectResult (window) { SourceQuery = sourceQuery };
			AppResult nsWindowLastNode = null;
			fullResultSet.Add (node);

			AddChild (rootNode, node, ref lastChild);

			foreach (var child in window.ContentView.Subviews) {
				AppResult childNode = new NSObjectResult (child) { SourceQuery = sourceQuery };
				fullResultSet.Add (childNode);

				AddChild (node, childNode, ref nsWindowLastNode);
				GenerateChildrenForNSView (childNode, child);
			}

			NSToolbar toolbar = window.Toolbar;
			AppResult toolbarNode = new NSObjectResult (toolbar) { SourceQuery = sourceQuery };

			AddChild (node, toolbarNode, ref nsWindowLastNode);

			if (toolbar == null) {
				return;
			}

			AppResult lastItemNode = null;
			foreach (var item in toolbar.Items) {
				if (item.View == null)
					continue;

				AppResult itemNode = new NSObjectResult (item.View) { SourceQuery = sourceQuery };
				fullResultSet.Add (itemNode);

				AddChild (itemNode, toolbarNode, ref lastItemNode);

				GenerateChildrenForNSView (itemNode, item.View);
			}
		}

		void GenerateChildrenForNSView (AppResult parent, NSView view)
		{
			var subviews = view.Subviews;
			if (subviews == null)
				return;

			AppResult lastChild = null;

			foreach (var child in view.Subviews) {
				AppResult node = new NSObjectResult (child) { SourceQuery = sourceQuery };
				fullResultSet.Add (node);

				AddChild (parent, node, ref lastChild);

				GenerateChildrenForNSView (node, child);
			}

			if (view is NSSegmentedControl segmentedControl) {
				for (int i = 0; i < segmentedControl.SegmentCount; i++) {
					var node = new NSObjectResult (view, i);
					fullResultSet.Add (node);

					AddChild (parent, node, ref lastChild);
				}
			}
		}
#endif
	}
}
