//
// IGroupingProvider.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace MonoDevelop.CodeIssues
{
	public class IssueSummary: IIssueTreeNode
	{
		public static IssueSummary FromCodeIssue(ProjectFile file, BaseCodeIssueProvider provider, CodeIssue codeIssue)
		{
			var topLevelProvider = (provider as CodeIssueProvider) ?? provider.Parent;
			if (topLevelProvider == null)
				throw new ArgumentException ("must be a CodeIssueProvider or a BaseCodeIssueProvider with Parent != null", "provider");
			var issueSummary = new IssueSummary {
				IssueDescription = codeIssue.Description,
				Region = codeIssue.Region,
				ProviderTitle = topLevelProvider.Title,
				ProviderDescription = topLevelProvider.Description,
				ProviderCategory = topLevelProvider.Category,
				Severity = topLevelProvider.GetSeverity (),
				IssueMarker = codeIssue.IssueMarker,
				File = file,
				Project = file.Project,
				InspectorIdString = codeIssue.InspectorIdString
			};
			issueSummary.Actions = codeIssue.Actions.Select (a => new ActionSummary {
				Batchable = a.SupportsBatchRunning,
				SiblingKey = a.SiblingKey,
				Title = a.Title,
				Region = a.DocumentRegion,
				IssueSummary = issueSummary
			}).ToList ();
			return issueSummary;
		}

		#region IIssueTreeNode implementation
		
		string IIssueTreeNode.Text {
			get {
				string lineDescription;
				if (Region.BeginLine == Region.EndLine) {
					lineDescription = Region.BeginLine.ToString ();
				} else {
					lineDescription = string.Format ("{0}-{1}", Region.BeginLine, Region.EndLine);
				}
				var fileName = Path.GetFileName (File.Name);
				return string.Format ("{0} [{1}:{2}]", IssueDescription, fileName, lineDescription);
			}
		}

		static readonly ICollection<IIssueTreeNode> emptyCollection = new IIssueTreeNode[0];

		ICollection<IIssueTreeNode> IIssueTreeNode.Children {
			get {
				return emptyCollection;
			}
		}

		bool IIssueTreeNode.HasVisibleChildren {
			get {
				return false;
			}
		}

		bool visible = true;
		bool IIssueTreeNode.Visible {
			get {
				return visible;
			}
				
			set {
				if (visible != value) {
					visible = value;
					OnVisibleChanged (new IssueGroupEventArgs (this));
				}
			}
		}

		ICollection<IIssueTreeNode> IIssueTreeNode.AllChildren {
			get {
				return emptyCollection;
			}
		}
		
		event EventHandler<IssueGroupEventArgs> visibleChanged;
		event EventHandler<IssueGroupEventArgs> IIssueTreeNode.VisibleChanged {
			add {
				visibleChanged += value;
			}
			remove {
				visibleChanged -= value;
			}
		}

		protected virtual void OnVisibleChanged (IssueGroupEventArgs eventArgs)
		{
			var handler = visibleChanged;
			if (handler != null) {
				handler (this, eventArgs);
			}
		}
		
		// no-op events, these never happen in this implementation
		event EventHandler<IssueGroupEventArgs> IIssueTreeNode.ChildrenInvalidated {
			add {
			}
			remove {
			}
		}

		event EventHandler<IssueTreeNodeEventArgs> IIssueTreeNode.ChildAdded {
			add {
			}
			remove {
			}
		}

		event EventHandler<IssueGroupEventArgs> IIssueTreeNode.TextChanged {
			add {
			}
			remove {
			}
		}
		
		#endregion
		
		/// <summary>
		/// The description of the issue.
		/// </summary>
		public string IssueDescription { get; set; }

		/// <summary>
		/// The region.
		/// </summary>
		public DomRegion Region { get; set; }

		/// <summary>
		/// Gets or sets the category of the issue provider.
		/// </summary>
		public string ProviderCategory { get; set; }

		/// <summary>
		/// Gets or sets the title of the issue provider.
		/// </summary>
		public string ProviderTitle { get; set; }

		/// <summary>
		/// Gets or sets the description of the issue provider.
		/// </summary>
		public string ProviderDescription { get; set; }

		/// <summary>
		/// Gets or sets the severity.
		/// </summary>
		public Severity Severity { get; set; }

		/// <summary>
		/// Gets or sets a value indicating how this issue should be marked inside the text editor.
		/// Note: There is only one code issue provider generated therfore providers need to be state less.
		/// </summary>
		public IssueMarker IssueMarker { get; set; }

		/// <summary>
		/// Gets or sets the file that this issue was found in.
		/// </summary>
		/// <value>The file.</value>
		public ProjectFile File { get; set; }

		/// <summary>
		/// Gets or sets the project this issue was found in.
		/// </summary>
		/// <value>The project.</value>
		public Project Project { get; set; }

		/// <summary>
		/// Gets or sets the type of the inspector that was the source of this issue.
		/// </summary>
		/// <value>The type of the inspector.</value>
		public string InspectorIdString { get; set; }

		IList<ActionSummary> actions;

		/// <summary>
		/// Gets or sets the actions available to fix this issue.
		/// </summary>
		/// <value>The actions.</value>
		public IList<ActionSummary> Actions {
			get {
				if (actions == null) {
					Actions = new List<ActionSummary> ();
				}
				return actions;
			}
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				actions = value;
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[IssueSummary: ProviderTitle={2}, Region={0}, ProviderCategory={1}]", Region, ProviderCategory, ProviderTitle);
		}
	}
}

