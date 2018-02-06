//
// BuildOutputNode.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018 Microsoft Corp. (http://microsoft.com)
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
using Xwt;

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

	class BuildOutputNode : TreePosition
	{
		public virtual BuildOutputNodeType NodeType { get; set; }
		public virtual string Message { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public BuildOutputNode Parent { get; set; }
		public virtual bool HasErrors { get; set; }
		public virtual bool HasWarnings { get; set; }
		public virtual bool HasData { get; set; }

		List<BuildOutputNode> children;
		public virtual IReadOnlyList<BuildOutputNode> Children => children;

		public void AddChild (BuildOutputNode child)
		{
			if (children == null) {
				children = new List<BuildOutputNode> ();
			}

			children.Add (child);
		}
	}

	class FilteredBuildOutputNode : BuildOutputNode
	{
		BuildOutputNode masterNode;
		bool includeDiagnostics;
		bool hasBeenFiltered = false;

		public FilteredBuildOutputNode (BuildOutputNode master, FilteredBuildOutputNode parent, bool includeDiagnostics)
		{
			masterNode = master;
			this.includeDiagnostics = includeDiagnostics;
			Parent = parent;
		}

		public override BuildOutputNodeType NodeType { get => masterNode.NodeType; set => masterNode.NodeType = value; }
		public override string Message { get => masterNode.Message; set => masterNode.Message = value; }
		public override bool HasErrors { get => masterNode.HasErrors; set => masterNode.HasErrors = value; }
		public override bool HasWarnings { get => masterNode.HasWarnings; set => masterNode.HasWarnings = value; }

		public override IReadOnlyList<BuildOutputNode> Children {
			get {
				if (!hasBeenFiltered) {
					if ((masterNode.Children?.Count ?? 0) > 0) {
						foreach (var child in masterNode.Children) {
							if (!includeDiagnostics && (child.NodeType == BuildOutputNodeType.Diagnostics ||
														(!child.HasData && !child.HasErrors && !child.HasWarnings))) {
								continue;
							}
							AddChild (new FilteredBuildOutputNode (child, this, includeDiagnostics));
						}
					}

					hasBeenFiltered = true;
				}

				return base.Children;
			}
		}
	}
}