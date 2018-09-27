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
using System.Linq;
using System.Text;
using Xwt;
using MonoDevelop.Core;
using System.Threading;

namespace MonoDevelop.Ide.BuildOutputView
{
	enum BuildOutputNodeType
	{
		Unknown,
		Build,
		Project,
		Target,
		TargetSkipped,
		Task,
		Error,
		Warning,
		Message,
		Diagnostics,
		Parameters,
		BuildSummary
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

		public virtual string Configuration { get; set; }
		public virtual string Platform { get; set; }

		public virtual string File { get; set; }
		public virtual string Project { get; set; }
		public virtual int LineNumber { get; set; }

		public virtual int ErrorCount { get; set; }
		public virtual int WarningCount { get; set; }

		public virtual BuildOutputNode Previous { get; set; }
		public virtual BuildOutputNode Next { get; set; }

		static string [] KnownTools = new string[] {
			"AL",
			"Csc",
			"Exec",
			"Fsc"
		};

		public virtual bool IsCommandLine { get; private set; }

		List<BuildOutputNode> children;
		public virtual IReadOnlyList<BuildOutputNode> Children => children;

		public BuildOutputNode AddChild (BuildOutputNode child)
		{
			if (children == null) {
				children = new List<BuildOutputNode> ();
			}

			if (child.NodeType == BuildOutputNodeType.Message && NodeType == BuildOutputNodeType.Task && KnownTools.Contains (Message)) {
				child.IsCommandLine = true;
			}

			var parent = children.LastOrDefault ();
			if (parent != null) {
				parent.Next = child;
				child.Previous = parent;
			}

			children.Add (child);

			child.Parent = this;
			return child;
		}


		public Xwt.Drawing.Image GetImage ()
		{
			switch (NodeType) {
			case BuildOutputNodeType.Build:
				return Resources.BuildIcon;
			case BuildOutputNodeType.BuildSummary:
				return HasErrors ? Resources.TaskFailedIcon : Resources.TargetIcon;
			case BuildOutputNodeType.Error:
				return Resources.ErrorIconSmall;
			case BuildOutputNodeType.Parameters:
				return Resources.FolderIcon;
			case BuildOutputNodeType.Project:
				return Resources.ProjectIcon;
			case BuildOutputNodeType.Target:
			case BuildOutputNodeType.TargetSkipped:
				return Resources.TargetIcon;
			case BuildOutputNodeType.Task:
				return HasErrors ? Resources.TaskFailedIcon : Resources.TaskSuccessIcon;
			case BuildOutputNodeType.Warning:
				return Resources.WarningIconSmall;
			}
			return Resources.EmptyIcon;
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
				FullMessage = fullMessage
			});
		}

		public bool HasChildren => Children != null && Children.Count > 0;
	}

	class AggregatedBuildOutputNode : BuildOutputNode
	{
		List<BuildOutputNode> nodes = new List<BuildOutputNode> ();

		public AggregatedBuildOutputNode (BuildOutputNode node)
		{
			AddNode (node);
		}

		public void AddNode (BuildOutputNode node)
		{
			if (nodes.Count > 0 && (node.NodeType != NodeType || node.Message != Message)) {
				return;
			}

			nodes.Add (node);
			if (node.HasChildren) {
				foreach (var child in node.Children) {
					AddChild (child);
				}
			}
		}

		public override BuildOutputNodeType NodeType {
			get => nodes.FirstOrDefault ()?.NodeType ?? BuildOutputNodeType.Unknown;
			set => base.NodeType = value;
		}

		public override string Message {
			get => nodes.FirstOrDefault ()?.Message;
			set => base.Message = value;
		}

		public override string Platform {
			get => nodes.FirstOrDefault ()?.Platform;
			set => base.Platform = value;
		}

		public override string Configuration {
			get => nodes.FirstOrDefault ()?.Configuration;
			set => base.Configuration = value;
		}

		public override string FullMessage {
			get => nodes.FirstOrDefault ()?.FullMessage;
			set => base.FullMessage = value;
		}

		public override DateTime StartTime {
			get => nodes.MinValue (x => x.StartTime)?.StartTime ?? DateTime.Now;
			set => base.StartTime = value;
		}

		public override DateTime EndTime {
			get => nodes.MaxValue (x => x.EndTime)?.EndTime ?? DateTime.Now;
			set => base.EndTime = value;
		}

		public override bool HasErrors {
			get => nodes.Any (x => x.HasErrors);
			set => base.HasErrors = value;
		}

		public override bool HasWarnings {
			get => nodes.Any (x => x.HasWarnings);
			set => base.HasWarnings = value;
		}

		public override bool HasData {
			get => nodes.Any (x => x.HasData);
			set => base.HasData = value;
		}

		public override int WarningCount {
			get => nodes.Sum (x => x.WarningCount);
			set => base.WarningCount = value;
		}

		public override int ErrorCount {
			get => nodes.Sum (x => x.ErrorCount);
			set => base.ErrorCount = value;
		}

		public override bool IsCommandLine => false;
	}
	
	class FilteredBuildOutputNode : BuildOutputNode
	{
		BuildOutputNode masterNode;
		bool includeDiagnostics;
		bool hasBeenFiltered = false;

		public FilteredBuildOutputNode (BuildOutputNode master, bool includeDiagnostics)
		{
			masterNode = master;
			this.includeDiagnostics = includeDiagnostics;
		}

		public override BuildOutputNodeType NodeType { get => masterNode.NodeType; set => masterNode.NodeType = value; }
		public override string Message { get => masterNode.Message; set => masterNode.Message = value; }
		public override string FullMessage { get => masterNode.FullMessage; set => masterNode.FullMessage = value; }
		public override DateTime StartTime { get => masterNode.StartTime; set => masterNode.StartTime = value; }
		public override DateTime EndTime { get => masterNode.EndTime; set => masterNode.EndTime = value; }
		public override bool HasErrors { get => masterNode.HasErrors; set => masterNode.HasErrors = value; }
		public override bool HasWarnings { get => masterNode.HasWarnings; set => masterNode.HasWarnings = value; }
		public override bool HasData { get => masterNode.HasData; set => masterNode.HasData = value; }

		public override BuildOutputNode Next { get => masterNode.Next; set => masterNode.Next = value; }
		public override BuildOutputNode Previous { get => masterNode.Previous; set => masterNode.Previous = value; }

		public override string Configuration { get => masterNode.Configuration; set => masterNode.Configuration = value; }
		public override string Platform { get => masterNode.Platform; set => masterNode.Platform = value; }

		public override string File { get => masterNode.File; set => masterNode.File = value; }
		public override string Project { get => masterNode.Project; set => masterNode.Project = value; }
		public override int LineNumber { get => masterNode.LineNumber; set => masterNode.LineNumber = value; }

		public override int WarningCount { get => masterNode.WarningCount; set => masterNode.WarningCount = value; }
		public override int ErrorCount { get => masterNode.ErrorCount; set => masterNode.ErrorCount = value; }

		public override bool IsCommandLine => masterNode.IsCommandLine;

		public override IReadOnlyList<BuildOutputNode> Children {
			get {
				if (!hasBeenFiltered) {
					if ((masterNode.Children?.Count ?? 0) > 0) {
						for (int i = 0; i < masterNode.Children.Count; i++) {
							var child = masterNode.Children [i];
							if (!includeDiagnostics && (child.NodeType == BuildOutputNodeType.Diagnostics ||
														(!child.HasData && !child.HasErrors && !child.HasWarnings))) {
								continue;
							}
							AddChild (new FilteredBuildOutputNode (child, includeDiagnostics));
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

		public static void Search (this BuildOutputNode node, List<BuildOutputNode> matches, string pattern, CancellationToken token)
		{
			if ((node.Message?.IndexOf (pattern, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0) {
				matches.Add (node);
			}

			if (node.HasChildren) {
				foreach (var child in node.Children) {
					if (token.IsCancellationRequested)
						break;
					Search (child, matches, pattern, token);
				}
			}
		}

		public static BuildOutputNode SearchFirstNode (this BuildOutputNode buildOutputNode, BuildOutputNodeType type, string search = null)
		{
			if (type == buildOutputNode.NodeType) {
				if (search == buildOutputNode.Message || search == null) {
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

		const int NormalRoundPrecision = 1;
		const int DiagnosticRoundPrecision = 3;
		const string NormalRoundPrecisioFormat = "{0:F1}s";
		const string DiagnosticRoundPrecisionFormat = "{0:F3}s";

		public static string GetDurationAsString (this BuildOutputNode node, bool includeDiagnostics)
		{
			var duration = node.EndTime.Subtract (node.StartTime);
			if (duration.TotalHours >= 1) {
				return string.Format ("{0,7}", GettextCatalog.GetString ("{0}h {1}m", duration.Hours.ToString(), duration.Minutes.ToString ("00")));
			}

			if (duration.TotalMinutes >= 1) {
				return GettextCatalog.GetString ("{0}m {1}s", duration.Minutes.ToString(), duration.Seconds.ToString ("00"));
			}

			string precisionFormat;
			int precision;
			if (includeDiagnostics) {
				precisionFormat = DiagnosticRoundPrecisionFormat;
				precision = DiagnosticRoundPrecision;
			} else {
				precisionFormat = NormalRoundPrecisioFormat;
				precision = NormalRoundPrecision;
			}

			var value = Math.Round ((duration.Seconds + duration.Milliseconds / 1000d), precision);

			//We don't want print 0 values
			if (value == 0) {
				return null;
			}
			return string.Format (precisionFormat, value);
		}

		static void ToString (this BuildOutputNode node, bool includeChildren, StringBuilder result, string margin)
		{
			result.AppendFormat ("{0}{1}{2}", margin, node.FullMessage, Environment.NewLine);
			if (includeChildren && node.HasChildren) {
				var newMargin = $"{margin}\t";
				foreach (var child in node.Children) {
					child.ToString (includeChildren, result, newMargin);
				}
			}
		}

		public static string ToString (this BuildOutputNode node, bool includeChildren)
		{
			var result = new StringBuilder ();

			node.ToString (includeChildren, result, "");

			return result.ToString ();
		}
	}
}
