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
		Diagnostics,
		Parameters
	}

	class BuildOutputNode : TreePosition
	{
		const string ParametersNodeName = "Parameters";

		public virtual BuildOutputNodeType NodeType { get; set; }
		public virtual string Message { get; set; }
		public virtual string FullMessage { get; set; }
		public virtual DateTime StartTime { get; set; }
		public virtual DateTime EndTime { get; set; }
		public BuildOutputNode Parent { get; set; }
		public virtual bool HasErrors { get; set; } = false;
		public virtual bool HasWarnings { get; set; } = false;
		public virtual bool HasData { get; set; } = false;

		List<BuildOutputNode> children;
		public virtual IReadOnlyList<BuildOutputNode> Children => children;

		public BuildOutputNode AddChild (BuildOutputNode child)
		{
			if (children == null) {
				children = new List<BuildOutputNode> ();
			}

			children.Add (child);

			return child;
		}

		public BuildOutputNode FindChild (string message)
		{
			if (Children != null) {
				foreach (var child in Children) {
					if (child.Message == message) {
						return child;
					}
				}
			}

			return null;
		}

		public void AddParameter (string message, string fullMessage)
		{
			var parametersNode = FindChild (ParametersNodeName);
			if (parametersNode == null) {
				parametersNode = new BuildOutputNode {
					NodeType = BuildOutputNodeType.Parameters,
					Message = ParametersNodeName,
					FullMessage = ParametersNodeName,
					Parent = this
				};

				if (children == null) {
					children = new List<BuildOutputNode> ();
				}

				children.Insert (0, parametersNode);
			}

			parametersNode.AddChild (new BuildOutputNode {
				NodeType = BuildOutputNodeType.Diagnostics,
				Message = message,
				FullMessage = fullMessage,
				Parent = parametersNode
			});
		}

		public bool HasChildren => Children != null && Children.Count > 0;
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
		public override string FullMessage { get => masterNode.FullMessage; set => masterNode.FullMessage = value; }
		public override DateTime StartTime { get => masterNode.StartTime; set => masterNode.StartTime = value; }
		public override DateTime EndTime { get => masterNode.EndTime; set => masterNode.EndTime = value; }
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

	static class BuildOutputNodeExtensions
	{
		public static BuildOutputNode SearchFirstNode (this IEnumerable<BuildOutputNode> sender, BuildOutputNodeType type, string search)
		{
			BuildOutputNode tmp;
			foreach (var item in sender) {
				tmp = item.SearchFirstNode (type, search);
				if (tmp != null) {
					return tmp;
				}
			}
			return null;
		}

		public static void Search (this BuildOutputNode node, List<BuildOutputNode> matches, string pattern)
		{
			if ((node.Message?.IndexOf (pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) {
				matches.Add (node);
			}

			if (node.HasChildren) {
				foreach (var child in node.Children) {
					Search (child, matches, pattern);
				}
			}
		}

		public static BuildOutputNode SearchFirstNode (this BuildOutputNode buildOutputNode, BuildOutputNodeType type, string search)
		{
			if (type == buildOutputNode.NodeType) {
				if (search == buildOutputNode.Message) {
					return buildOutputNode;
				} else {
					//We don't want deep recursive into children, change to next item
					return null;
				}
			}

			//iterating into children
			if (buildOutputNode.Children != null) {
				BuildOutputNode tmp;
				for (int i = 0; i < buildOutputNode.Children.Count; ++i) {
					tmp = SearchFirstNode (buildOutputNode.Children [i], type, search);
					if (tmp != null) {
						return tmp;
					}
				}
			}
			return null;
		}

		public static string GetDurationAsString (this BuildOutputNode node)
		{
			var duration = node.EndTime.Subtract (node.StartTime);
			if (duration.TotalHours >= 1) {
				return GettextCatalog.GetString ("{0}:{1:d2} hours", duration.Hours, duration.Minutes);
			} else if (duration.TotalSeconds >= 1) {
				return GettextCatalog.GetString ("{0}:{1:d2} min", duration.Minutes, duration.Seconds);
			}

			return String.Empty;
		}
	}
}