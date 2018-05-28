//
// BuildOutput.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Ide.Editor;
using Microsoft.Build.Logging;
using Microsoft.Build.Framework;
using Gtk;
using Xwt;
using System.Linq;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutput : IDisposable
	{
		BuildOutputProgressMonitor progressMonitor;
		ImmutableList<BuildOutputProcessor> projects = ImmutableList<BuildOutputProcessor>.Empty;
		public event EventHandler OutputChanged;

		public ProgressMonitor GetProgressMonitor ()
		{
			if (progressMonitor == null) {
				progressMonitor = new BuildOutputProgressMonitor (this);
			}

			return progressMonitor;
		}

		public void Load (string filePath, bool removeFileOnDispose)
		{
			if (!File.Exists (filePath)) {
				return;
			}

			switch (Path.GetExtension (filePath)) {
			case ".binlog":
				AddProcessor (new MSBuildOutputProcessor (filePath, removeFileOnDispose));
				break;
			default:
				LoggingService.LogError ($"Unknown file type {filePath}");
				break;
			}
		}

		private class BuildOutputEventsSource : EventArgsDispatcher
		{
			public void ProcessFile (string filePath)
			{
				var eventsSource = new BinaryLogReplayEventSource ();
				eventsSource.AnyEventRaised += (sender, e) => Dispatch (e);
				eventsSource.Replay (filePath);
			}
		}

		public Task Save (string filePath)
		{
			return Task.Run (() => {
				var eventsSource = new BuildOutputEventsSource ();
				var logger = new BinaryLogger {
					CollectProjectImports = BinaryLogger.ProjectImportsCollectionMode.None,
					Parameters = filePath,
					Verbosity = LoggerVerbosity.Diagnostic
				};

				try {
					logger.Initialize (eventsSource);

					foreach (var proj in projects) {
						switch (proj) {
						case MSBuildOutputProcessor msbop:
							eventsSource.ProcessFile (proj.FileName);
							break;
						case BuildOutputProcessor bop:
							// FIXME
							break;
						default:
							continue;
						}
					}
				} catch (Exception ex) {
					LoggingService.LogError ($"Can't write to {filePath}: {ex.Message})");
				} finally {
					logger.Shutdown ();
				}
			});
		}

		internal void AddProcessor (BuildOutputProcessor processor)
		{
			projects = projects.Add (processor);
		}

		internal void Clear ()
		{
			projects = projects.Clear ();
			RaiseOutputChanged ();
		}

		internal void RaiseOutputChanged ()
		{
			OutputChanged?.Invoke (this, EventArgs.Empty);
		}

		public List<BuildOutputNode> GetRootNodes (bool includeDiagnostics)
		{
			if (includeDiagnostics) {
				return GetProjectRootNodes ().ToList ();
			} else {
				// If not including diagnostics, we need to filter the nodes,
				// but instead of doing so now for all, we do it on the fly,
				// as nodes are requested
				var nodes = new List<BuildOutputNode> ();
				foreach (var root in GetProjectRootNodes ()) {
					nodes.Add (new FilteredBuildOutputNode (root, includeDiagnostics));
				}
				return nodes;
			}
		}

		IEnumerable<BuildOutputNode> GetProjectRootNodes ()
		{
			int errorCount = 0, warningCount = 0;
			Dictionary<string, AggregatedBuildOutputNode> result = new Dictionary<string, AggregatedBuildOutputNode> ();
			foreach (var proj in projects) {
				foreach (var node in proj.RootNodes) {
					AggregatedBuildOutputNode aggregated = null;
					if (result.TryGetValue (node.Message, out aggregated)) {
						aggregated.AddNode (node);
					} else {
						result [node.Message] = new AggregatedBuildOutputNode (node);
					}

					errorCount += node.Children.Sum (x => x.ErrorCount);
					warningCount += node.Children.Sum (x => x.WarningCount);
				}
			}

			if (result.Values.Count > 0) {
				// Add summary node
				var message = errorCount > 0 ? GettextCatalog.GetString ("Build failed") : GettextCatalog.GetString ("Build succeeded");
				var summaryNode = new BuildOutputNode {
					NodeType = BuildOutputNodeType.BuildSummary,
					Message = message,
					FullMessage = message,
					HasErrors = errorCount > 0,
					HasWarnings = warningCount > 0,
					HasData = false,
					ErrorCount = errorCount,
					WarningCount = warningCount
				};

				return result.Values.Concat (summaryNode);
			}

			return result.Values;
		}

		public void ProcessProjects (bool showDiagnostics, IDictionary<string, string> metadata) 
		{
			foreach (var p in projects) {
				p.Process ();
			}

			metadata ["Verbosity"] = showDiagnostics ? "Diagnostics" : "Normal";
			metadata ["BuildCount"] = projects.Count.ToString ();
			metadata ["OnDiskSize"] = projects.Sum (x => new FileInfo (x.FileName).Length).ToString ();
			metadata ["RootNodesCount"] = projects.Sum (x => x.RootNodes.Count).ToString ();
		}

		bool disposed = false;

		~BuildOutput ()
		{
			Dispose (false);
		}

		void Dispose (bool disposing)
		{
			if (!disposed) {
				foreach (var p in projects) {
					p.Dispose ();
				}

				disposed = true;
				if (disposing) {
					GC.SuppressFinalize (this);
				}
			}
		}

		public void Dispose ()
		{
			Dispose (true);
		}
	}

	class BuildOutputProgressMonitor : ProgressMonitor
	{
		BuildOutputProcessor currentCustomProject;
		Dictionary<int, string> binlogSessions = new Dictionary<int, string> ();

		public BuildOutput BuildOutput { get; }

		public BuildOutputProgressMonitor (BuildOutput output)
		{
			BuildOutput = output;
		}

		protected override void OnWriteLogObject (object logObject)
		{
			switch (logObject) {
			case BuildSessionStartedEvent pspe:
				if (File.Exists (pspe.LogFile)) {
					binlogSessions [pspe.SessionId] = pspe.LogFile;
				} else {
					currentCustomProject = new BuildOutputProcessor (pspe.LogFile, false);
					currentCustomProject.AddNode (BuildOutputNodeType.Project,
					                              GettextCatalog.GetString ("Custom project"),
					                              GettextCatalog.GetString ("Custom project started building"),
					                              true, pspe.TimeStamp);
				}
				break;
			case BuildSessionFinishedEvent psfe:
				if (currentCustomProject != null) {
					currentCustomProject.EndCurrentNode (null, psfe.TimeStamp);
					BuildOutput.AddProcessor (currentCustomProject);
					currentCustomProject = null;
				} else if (binlogSessions.TryGetValue (psfe.SessionId, out string logFile)) {
					BuildOutput.Load (logFile, true);
					binlogSessions.Remove (psfe.SessionId);
				}
				BuildOutput.RaiseOutputChanged ();
				break;
			}
		}

		protected override void OnWriteLog (string message)
		{
			if (currentCustomProject != null) {
				currentCustomProject.AddNode (BuildOutputNodeType.Message, message, message, false, DateTime.Now);
			}
		}
	}

	class BuildOutputDataSource : ITreeDataSource
	{

		public IReadOnlyList<BuildOutputNode> RootNodes => this.rootNodes;
		readonly List<BuildOutputNode> rootNodes;
		public DataField<BuildOutputNode> BuildOutputNodeField = new DataField<BuildOutputNode> (0);

		public BuildOutputDataSource (List<BuildOutputNode> rootNodes)
		{
			this.rootNodes = rootNodes;
		}

		#region ITreeDataSource implementation

		public Type [] ColumnTypes => new Type [] { typeof (Xwt.Drawing.Image), typeof (string) };

		public event EventHandler<TreeNodeEventArgs> NodeInserted;
		public event EventHandler<TreeNodeChildEventArgs> NodeDeleted;
		public event EventHandler<TreeNodeEventArgs> NodeChanged;
		public event EventHandler<TreeNodeOrderEventArgs> NodesReordered;
		public event EventHandler Cleared;

		public TreePosition GetChild (TreePosition pos, int index)
		{
			var node = pos as BuildOutputNode;
			if (node != null) {
				if (node.Children != null && node.Children.Count > index) {
					return node.Children [index];
				}
			} else {
				if (rootNodes.Count > index) {
					return rootNodes [index];
				}
			}
			return null;
		}

		public int GetChildrenCount (TreePosition pos)
		{
			var node = pos as BuildOutputNode;
			if (node != null) {
				return node.Children?.Count ?? 0;
			} else {
				return rootNodes?.Count ?? 0;
			}
		}

		public TreePosition GetParent (TreePosition pos)
		{
			var node = pos as BuildOutputNode;
			return node?.Parent;
		}

		public object GetValue (TreePosition pos, int column)
		{
			var node = pos as BuildOutputNode;
			if (column == 0 && node != null) {
				return node;
			}
			return null;
		}

		public void SetValue (TreePosition pos, int column, object value)
		{
			throw new NotImplementedException ();
		}

		#endregion

		public void RaiseNodeChanged (BuildOutputNode node)
		{
			if (node != null) {
				NodeChanged?.Invoke (this, new TreeNodeEventArgs (node));
			}
		}
	}

	class BuildOutputDataSearch
	{
		readonly IReadOnlyList<BuildOutputNode> rootNodes;

		public BuildOutputDataSearch (IReadOnlyList<BuildOutputNode> rootNodes)
		{
			this.rootNodes = rootNodes;
		}

		#region Search functionality

		readonly List<BuildOutputNode> currentSearchMatches = new List<BuildOutputNode> ();
		string currentSearchPattern;
		int currentMatchIndex = -1;

		/// <summary>
		/// This is the relative index position from 1 to MatchesCount
		/// </summary>
		/// <value>The current position.</value>
		public int CurrentAbsoluteMatchIndex => currentMatchIndex + 1;

		/// <summary>
		/// Gets all matches.
		/// </summary>
		/// <value>All matches.</value>
		public IReadOnlyList<BuildOutputNode> AllMatches => currentSearchMatches;

		/// <summary>
		/// Gets the matches count. 
		/// </summary>
		/// <value>The matches count.</value>
		public int MatchesCount => currentSearchMatches.Count;

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.Ide.BuildOutputView.BuildOutputDataSource"/> search is wrapped.
		/// </summary>
		/// <value><c>true</c> if search wrapped; otherwise, <c>false</c>.</value>
		public bool SearchWrapped { get; private set; }

		public Task<BuildOutputNode> FirstMatch (string pattern)
		{
			// Initialize search data
			currentSearchMatches.Clear ();

			currentSearchPattern = pattern;
			currentMatchIndex = -1;

			return Task.Run (() => {
				if (!string.IsNullOrEmpty (pattern)) {
					// Perform search
					foreach (var root in rootNodes) {
						root.Search (currentSearchMatches, currentSearchPattern);
					}

					if (currentSearchMatches.Count > 0) {
						currentMatchIndex = 0;
						return currentSearchMatches [0];
					}
				}

				return null;
			});
		}

		public BuildOutputNode PreviousMatch ()
		{
			if (currentSearchMatches.Count == 0 ||
				String.IsNullOrEmpty (currentSearchPattern) || currentMatchIndex == -1) {
				return null;
			}

			if (currentMatchIndex > 0) {
				currentMatchIndex--;
				SearchWrapped = false;
			} else {
				currentMatchIndex = currentSearchMatches.Count - 1;
				SearchWrapped = true;
			}

			return currentSearchMatches [currentMatchIndex];
		}

		public BuildOutputNode NextMatch ()
		{
			if (currentSearchMatches.Count == 0 ||
			    String.IsNullOrEmpty (currentSearchPattern) || currentMatchIndex == -1) {
				return null;
			}

			if (currentMatchIndex < currentSearchMatches.Count - 1) {
				currentMatchIndex++;
				SearchWrapped = false;
			} else {
				currentMatchIndex = 0;
				SearchWrapped = true;
			}

			return currentSearchMatches [currentMatchIndex];
		}

		#endregion
	}
}
