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

namespace MonoDevelop.Components.AutoTest
{
	partial class AppQueryRunner
	{
		readonly List<Operation> operations;
		readonly string sourceQuery;
		readonly List<AppResult> fullResultSet = new List<AppResult> ();

		public AppQueryRunner (List<Operation> operations)
		{
			this.operations = operations;

			sourceQuery = GetQueryString (operations);
		}

		public (AppResult RootNode, AppResult[] AllResults) Execute ()
		{
			var (rootNode, resultSet) = ResultSetFromWindows ();

			// TODO: Add a start mechanism, so we only process nodes coming in from a part of the IDE.

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
			// null for AppResult signifies root node
			var rootNode = new GtkWidgetResult (null) { SourceQuery = sourceQuery };

			// Build the tree and full result set recursively
			AppResult lastChild = null;
			ProcessGtkWindows (rootNode, ref lastChild);

#if MAC
			ProcessNSWindows (rootNode, ref lastChild);
#endif

			return (rootNode, fullResultSet);
		}

		void AddChild (AppResult parent, AppResult node, ref AppResult lastChild)
		{
			if (parent.FirstChild == null) {
				parent.FirstChild = node;
			} else {
				// Add the new node into the chain
				lastChild.NextSibling = node;
				node.PreviousSibling = lastChild;
			}
			lastChild = node;
		}

		public static string GetQueryString (List<Operation> operations)
		{
			var strings = operations.Select (x => x.ToString ()).ToArray ();
			var operationChain = string.Join (".", strings);

			return string.Format ("c => c.{0};", operationChain);
		}
	}
}
