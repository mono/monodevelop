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

namespace MonoDevelop.Components.AutoTest
{
	public class AppQuery : MarshalByRefObject
	{
		AppResult rootNode;
		HashSet<AppResult> fullResultSet;
		List<Operation> operations = new List<Operation> ();

		AppResult GenerateChildrenForContainer (Gtk.Container container)
		{
			AppResult firstChild = null, lastChild = null;

			foreach (var child in container.Children) {
				AppResult node = new AppResult (child);
				fullResultSet.Add (node);

				if (firstChild == null) {
					firstChild = node;
					lastChild = node;
				} else {
					lastChild.NextSibling = node;
					node.PreviousSibling = lastChild;
					lastChild = node;
				}

				if (child is Gtk.Container) {
					AppResult children = GenerateChildrenForContainer ((Gtk.Container)child);
					node.FirstChild = children;
				}
			}

			return firstChild;
		}

		public AppQuery (Gtk.Window[] windows)
		{
			// null for AppResult signifies root node
			rootNode = new AppResult (null);
			fullResultSet = new HashSet<AppResult> ();

			// Build the tree and full result set recursively
			AppResult lastChild = null;
			foreach (var window in windows) {
				AppResult node = new AppResult (window);
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
				AppResult children = GenerateChildrenForContainer ((Gtk.Container) window);
				node.FirstChild = children;
			}
		}

		public AppResult[] Execute ()
		{
			HashSet<AppResult> resultSet = fullResultSet;
			foreach (var subquery in operations) {
				// Some subqueries can select different results
				resultSet = subquery.Execute (resultSet);
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

		public AppQuery Button ()
		{
			operations.Add (new ButtonOperation ());
			return this;
		}

		public AppQuery Textfield ()
		{
			operations.Add (new TextfieldOperation ());
			return this;
		}

		public AppQuery Text (string text)
		{
			operations.Add (new TextOperation (text));
			return this;
		}

		public AppQuery Model (string column)
		{
			return this;
		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder ();
			foreach (var subquery in operations) {
				builder.Append (subquery.ToString ());
			}

			return builder.ToString ();
		}		
	}
}

