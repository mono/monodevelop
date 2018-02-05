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
using MonoDevelop.Core;
using Xwt;
using System.Linq;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutputProcessor : IDisposable
	{
		List<BuildOutputNode> rootNodes = new List<BuildOutputNode> ();
		BuildOutputNode currentNode;

		public BuildOutputProcessor (string fileName, bool removeFileOnDispose)
		{
			FileName = fileName;
			RemoveFileOnDispose = removeFileOnDispose;
		}

		public IReadOnlyList<BuildOutputNode> RootNodes => rootNodes;

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
