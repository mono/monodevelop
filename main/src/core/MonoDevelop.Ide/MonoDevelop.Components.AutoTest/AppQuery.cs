//
// AppQuery.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.Text;
using Gtk;
using MonoDevelop.Components.AutoTest.Operations;
using MonoDevelop.Components.AutoTest.Results;
using System.Linq;
using System.Xml;
using MonoDevelop.Core;

#if MAC
using AppKit;
#endif

namespace MonoDevelop.Components.AutoTest
{
	public class AppQuery : MarshalByRefObject
	{
		AppResult rootNode;
		List<Operation> operations = new List<Operation> ();

		public AutoTestSessionDebug SessionDebug { get; set; }

		AppResult GenerateChildrenForContainer (Gtk.Container container, List<AppResult> resultSet)
		{
			AppResult firstChild = null, lastChild = null;

			foreach (var child in container.Children) {
				AppResult node = new GtkWidgetResult (child) { SourceQuery = ToString () };
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
				AppResult node = new NSObjectResult (child) { SourceQuery = ToString () };
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

		List<AppResult> ResultSetFromWindows ()
		{
			Gtk.Window[] windows = Gtk.Window.ListToplevels ();

			// null for AppResult signifies root node
			rootNode = new GtkWidgetResult (null) { SourceQuery = ToString () };
			List<AppResult> fullResultSet = new List<AppResult> ();

			// Build the tree and full result set recursively
			AppResult lastChild = null;
			foreach (var window in windows) {
				AppResult node = new GtkWidgetResult (window) { SourceQuery = ToString () };
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
				AppResult children = GenerateChildrenForContainer ((Gtk.Container) window, fullResultSet);
				node.FirstChild = children;
			}

#if MAC
			NSWindow[] nswindows = NSApplication.SharedApplication.Windows;
			if (nswindows != null) {
				foreach (var window in nswindows) {
					AppResult node = new NSObjectResult (window) { SourceQuery = ToString () };
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
						AppResult childNode = new NSObjectResult (child) { SourceQuery = ToString () };
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
					AppResult toolbarNode = new NSObjectResult (toolbar) { SourceQuery = ToString () };

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
								AppResult itemNode = new NSObjectResult (item.View) { SourceQuery = ToString () };
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
			return fullResultSet;
		}

		public AppQuery ()
		{
		}

		public AppResult[] Execute ()
		{
			List<AppResult> resultSet = ResultSetFromWindows ();

			foreach (var subquery in operations) {
				// Some subqueries can select different results
				resultSet = subquery.Execute (resultSet);

				if (resultSet == null || resultSet.Count == 0) {
					return new AppResult[0];
				}
			}

			AppResult[] results = new AppResult[resultSet.Count];
			resultSet.CopyTo (results);

			return results;
		}

		public AppQuery Marked (string mark)
		{
			operations.Add (new MarkedOperation (mark));
			return this;
		}

		public AppQuery CheckType (Type desiredType, string name = null)
		{
			operations.Add (new TypeOperation (desiredType, name));
			return this;
		}

		public AppQuery Button ()
		{
			return CheckType (typeof(Gtk.Button), "Button");
		}

		public AppQuery Textfield ()
		{
			return CheckType (typeof(Gtk.Entry), "Textfield");
		}

		public AppQuery CheckButton ()
		{
			return CheckType (typeof(Gtk.CheckButton), "CheckButton");
		}

		public AppQuery RadioButton ()
		{
			return CheckType (typeof(Gtk.RadioButton), "RadioButton");
		}

		public AppQuery TreeView ()
		{
			return CheckType (typeof(Gtk.TreeView), "TreeView");
		}

		public AppQuery Window ()
		{
			return CheckType (typeof(Gtk.Window), "Window");
		}

		public AppQuery TextView ()
		{
			return CheckType (typeof(Gtk.TextView), "TextView");
		}

		public AppQuery Notebook ()
		{
			return CheckType (typeof(Gtk.Notebook), "Notebook");
		}

		public AppQuery Text (string text)
		{
			operations.Add (new TextOperation (text));
			return this;
		}

		public AppQuery Contains (string text)
		{
			operations.Add (new TextOperation (text, false));
			return this;
		}

		public AppQuery Selected ()
		{
			operations.Add (new SelectedOperation ());
			return this;
		}

		public AppQuery Model (string column = null)
		{
			operations.Add (new ModelOperation (column));
			return this;
		}

		public AppQuery Sensitivity (bool sensitivity)
		{
			operations.Add (new PropertyOperation ("Sensitive", sensitivity));
			return this;
		}

		public AppQuery Visibility (bool visibility)
		{
			operations.Add (new PropertyOperation ("Visible", visibility));
			return this;
		}

		public AppQuery Property (string propertyName, object desiredValue)
		{
			operations.Add (new PropertyOperation (propertyName, desiredValue));
			return this;
		}

		public AppQuery Toggled (bool toggled)
		{
			operations.Add (new PropertyOperation ("Active", toggled));
			return this;
		}

		public AppQuery NextSiblings ()
		{
			operations.Add (new NextSiblingsOperation ());
			return this;
		}

		public AppQuery Index (int index)
		{
			operations.Add (new IndexOperation (index));
			return this;
		}

		public AppQuery Children (bool recursive = true)
		{
			operations.Add (new ChildrenOperation (recursive));
			return this;
		}

		public override string ToString ()
		{
			var operationChain = string.Join (".", operations.Select (x => x.ToString ()));
			return string.Format ("c => c.{0};", operationChain);
		}		
	}
}

