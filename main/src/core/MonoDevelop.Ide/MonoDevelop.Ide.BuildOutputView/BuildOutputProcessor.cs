//
// BuildOutputProcessor.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2017 Microsoft Corp. (http://microsoft.com)
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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.BuildOutputView
{
	enum BuildOutputNodeType
	{
		Build,
		Project,
		Target,
		Task,
		Error,
		Warning,
		Message,
		Diagnostics
	}

	class BuildOutputNode
	{
		public BuildOutputNodeType NodeType { get; set; }
		public string Message { get; set; }
		public BuildOutputNode Parent { get; set; }
		public IList<BuildOutputNode> Children { get; } = new List<BuildOutputNode> ();
		public bool HasErrors { get; set; }
		public bool HasWarnings { get; set; }
		public bool HasData { get; set; }
	}

	class BuildOutputProcessor : IDisposable
	{
		List<BuildOutputNode> rootNodes = new List<BuildOutputNode> ();
		BuildOutputNode currentNode;

		public BuildOutputProcessor (string fileName, bool removeFileOnDispose)
		{
			FileName = fileName;
			RemoveFileOnDispose = removeFileOnDispose;
		}

		public string FileName { get; }

		protected bool NeedsProcessing { get; set; } = true;

		protected bool RemoveFileOnDispose { get; set; }

		protected void Clear ()
		{
			currentNode = null;
			rootNodes = new List<BuildOutputNode> ();
			NeedsProcessing = true;
		}

		public virtual void Process ()
		{
			NeedsProcessing = false;
		}

		public void AddNode (BuildOutputNodeType nodeType, string message, bool isStart)
		{
			var node = new BuildOutputNode { NodeType = nodeType, Message = message };
			if (currentNode == null) {
				rootNodes.Add (node);
			} else {
				currentNode.Children.Add (node);
				node.Parent = currentNode;
			}

			if (isStart) {
				currentNode = node;
			}

			if (nodeType == BuildOutputNodeType.Error || nodeType == BuildOutputNodeType.Message || nodeType == BuildOutputNodeType.Warning) {
				var p = node;
				while (p != null) {
					if (nodeType == BuildOutputNodeType.Error) {
						p.HasErrors = true;
					} else if (nodeType == BuildOutputNodeType.Warning) {
						p.HasWarnings = true;
					} else if (nodeType == BuildOutputNodeType.Message) {
						p.HasData = true;
					}

					p = p.Parent;
				}
			}
		}

		public void EndCurrentNode (string message)
		{
			currentNode = currentNode?.Parent;
		}

		private async Task ProcessChildren (TreeStore store, TreeIter parentIter, BuildOutputNode node, bool includeDiagnostics)
		{
			foreach (var child in node.Children) {
				await ProcessNode (store, parentIter, child, includeDiagnostics);
			}
		}

		private async Task ProcessNode (TreeStore store, TreeIter parentIter, BuildOutputNode node, bool includeDiagnostics)
		{
			// For non-diagnostics mode, only return nodes with data
			if (!includeDiagnostics && (node.NodeType == BuildOutputNodeType.Diagnostics ||
										(!node.HasData && !node.HasErrors && !node.HasWarnings))) {
				return;
			}

			TreeIter it = TreeIter.Zero;
			await Runtime.RunInMainThread (() => {
				if (parentIter.Equals (TreeIter.Zero)) {
					it = store.AppendValues (node);
				} else {
					it = store.AppendValues (parentIter, node);
				}
			});

			if (node.Children.Count > 0) {
				await ProcessChildren (store, it, node, includeDiagnostics);
			}
		}

		public async Task ToTreeStore (TreeStore store, bool includeDiagnostics)
		{
			foreach (var node in rootNodes) {
				await ProcessNode (store, TreeIter.Zero, node, includeDiagnostics);
			}
		}

		bool disposed = false;

		~BuildOutputProcessor ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
		}

		void Dispose (bool disposing)
		{
			if (!disposed) {
				if (RemoveFileOnDispose && File.Exists (FileName)) {
					File.Delete (FileName);
				}

				disposed = true;
				if (disposing) {
					GC.SuppressFinalize (this);
				}
			}
		}
	}
}
