//
// AppQueryRunner.cs
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
using System.Linq;
using MonoDevelop.Components.AutoTest.Operations;
using MonoDevelop.Components.AutoTest.Results;

#if MAC
using AppKit;
#endif

namespace MonoDevelop.Components.AutoTest
{
	class AppQueryRunner
	{
		readonly List<Operation> operations;
		readonly string sourceQuery;

		public AppQueryRunner (List<Operation> operations)
		{
			this.operations = operations;

			sourceQuery = GetQueryString (operations);
		}

		public (AppResult RootNode, AppResult[] AllResults) Execute ()
		{
			var (rootNode, resultSet) = ResultSetFromWindows ();

			foreach (var subquery in operations) {
				// Some subqueries can select different results
				resultSet = subquery.Execute (resultSet);

				if (resultSet == null || resultSet.Count == 0) {
					return (rootNode, Array.Empty<AppResult> ());
				}
			}

			return (rootNode, resultSet.ToArray ());
		}

		(AppResult, List<AppResult>) ResultSetFromWindows ()
		{
			Gtk.Window [] windows = Gtk.Window.ListToplevels ();

			// null for AppResult signifies root node
			var rootNode = new GtkWidgetResult (null) { SourceQuery = sourceQuery };
			List<AppResult> fullResultSet = new List<AppResult> ();

			// Build the tree and full result set recursively
			AppResult lastChild = null;
			foreach (var window in windows) {
				AppResult node = new GtkWidgetResult (window) { SourceQuery = sourceQuery };
				fullResultSet.Add (node);

				if (rootNode.FirstChild == null) {
					rootNode.FirstChild = node;
					lastChild = node;
				} else {
					// Add the new node into the chain
					lastChild.NextSibling = node;
					node.PreviousSibling = lastChild;
					lastChild = node;
				}

				// Create the children list and link them onto the node
				AppResult children = GenerateChildrenForContainer ((Gtk.Container)window, fullResultSet);
				node.FirstChild = children;
			}

#if MAC
			NSWindow [] nswindows = NSApplication.SharedApplication.Windows;
			if (nswindows != null) {
				foreach (var window in nswindows) {
					AppResult node = new NSObjectResult (window) { SourceQuery = sourceQuery };
					AppResult nsWindowLastNode = null;
					fullResultSet.Add (node);

					if (rootNode.FirstChild == null) {
						rootNode.FirstChild = node;
						lastChild = node;
					} else {
						lastChild.NextSibling = node;
						node.PreviousSibling = lastChild;
						lastChild = node;
					}

					foreach (var child in window.ContentView.Subviews) {
						AppResult childNode = new NSObjectResult (child) { SourceQuery = sourceQuery };
						fullResultSet.Add (childNode);

						if (node.FirstChild == null) {
							node.FirstChild = childNode;
							nsWindowLastNode = childNode;
						} else {
							nsWindowLastNode.NextSibling = childNode;
							childNode.PreviousSibling = nsWindowLastNode;
							nsWindowLastNode = childNode;
						}

						if (child.Subviews != null) {
							AppResult children = GenerateChildrenForNSView (child, fullResultSet);
							childNode.FirstChild = children;
						}
					}

					NSToolbar toolbar = window.Toolbar;
					AppResult toolbarNode = new NSObjectResult (toolbar) { SourceQuery = sourceQuery };

					if (node.FirstChild == null) {
						node.FirstChild = toolbarNode;
						nsWindowLastNode = toolbarNode;
					} else {
						nsWindowLastNode.NextSibling = toolbarNode;
						toolbarNode.PreviousSibling = nsWindowLastNode;
						nsWindowLastNode = toolbarNode;
					}

					if (toolbar != null) {
						AppResult lastItemNode = null;
						foreach (var item in toolbar.Items) {
							if (item.View != null) {
								AppResult itemNode = new NSObjectResult (item.View) { SourceQuery = sourceQuery };
								fullResultSet.Add (itemNode);

								if (toolbarNode.FirstChild == null) {
									toolbarNode.FirstChild = itemNode;
									lastItemNode = itemNode;
								} else {
									lastItemNode.NextSibling = itemNode;
									itemNode.PreviousSibling = lastItemNode;
									lastItemNode = itemNode;
								}

								if (item.View.Subviews != null) {
									AppResult children = GenerateChildrenForNSView (item.View, fullResultSet);
									itemNode.FirstChild = children;
								}
							}
						}
					}
				}
			}
#endif
			return (rootNode, fullResultSet);
		}


		AppResult GenerateChildrenForContainer (Gtk.Container container, List<AppResult> resultSet)
		{
			AppResult firstChild = null, lastChild = null;

			foreach (var child in container.Children) {
				AppResult node = new GtkWidgetResult (child) { SourceQuery = sourceQuery };
				resultSet.Add (node);

				// FIXME: Do we need to recreate the tree structure of the AppResults?
				if (firstChild == null) {
					firstChild = node;
					lastChild = node;
				} else {
					lastChild.NextSibling = node;
					node.PreviousSibling = lastChild;
					lastChild = node;
				}

				if (child is Gtk.Container) {
					AppResult children = GenerateChildrenForContainer ((Gtk.Container)child, resultSet);
					node.FirstChild = children;
				}
			}

			return firstChild;
		}

#if MAC
		AppResult GenerateChildrenForNSView (NSView view, List<AppResult> resultSet)
		{
			AppResult firstChild = null, lastChild = null;

			foreach (var child in view.Subviews) {
				AppResult node = new NSObjectResult (child) { SourceQuery = sourceQuery };
				resultSet.Add (node);

				if (firstChild == null) {
					firstChild = node;
					lastChild = node;
				} else {
					lastChild.NextSibling = node;
					node.PreviousSibling = lastChild;
					lastChild = node;
				}

				if (child.Subviews != null) {
					AppResult children = GenerateChildrenForNSView (child, resultSet);
					node.FirstChild = children;
				}
			}

			if (view is NSSegmentedControl || view.GetType ().IsSubclassOf (typeof (NSSegmentedControl))) {
				var segmentedControl = (NSSegmentedControl)view;
				for (int i = 0; i < segmentedControl.SegmentCount; i++) {
					var node = new NSObjectResult (view, i);
					resultSet.Add (node);
					if (firstChild == null) {
						firstChild = node;
						lastChild = node;
					} else {
						lastChild.NextSibling = node;
						node.PreviousSibling = lastChild;
						lastChild = node;
					}
				}
			}

			return firstChild;
		}
#endif

		public static string GetQueryString (List<Operation> operations)
		{
			var strings = operations.Select (x => x.ToString ()).ToArray ();
			var operationChain = string.Join (".", strings);

			return string.Format ("c => c.{0};", operationChain);
		}
	}
}
