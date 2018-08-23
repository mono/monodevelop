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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Microsoft.Build.Logging;
using Microsoft.Build.Framework;
using Xwt;
using System.Linq;
using System.Collections.Immutable;
using MonoDevelop.Projects.MSBuild;
using System.Threading;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutput : IDisposable
	{
		BuildOutputProgressMonitor progressMonitor;
		ImmutableList<BuildOutputProcessor> projects = ImmutableList<BuildOutputProcessor>.Empty;
		public event EventHandler OutputChanged;
		public event EventHandler ProjectStarted;
		public event EventHandler ProjectFinished;

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

		internal void RaiseProjectStarted ()
		{
			ProjectStarted?.Invoke (this, EventArgs.Empty);
		}

		internal void RaiseProjectFinished ()
		{
			ProjectFinished?.Invoke (this, EventArgs.Empty);
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
			DateTime maximum = default (DateTime);
			DateTime minimum = default (DateTime);
			foreach (var proj in projects) {
				foreach (var node in proj.RootNodes) {
					AggregatedBuildOutputNode aggregated = null;
					if (result.TryGetValue (node.Message, out aggregated)) {
						aggregated.AddNode (node);
					} else {
						result [node.Message] = new AggregatedBuildOutputNode (node);
					}

					if (maximum == default (DateTime) || node.EndTime.Ticks > maximum.Ticks) {
						maximum = node.EndTime;
					}

					if (minimum == default (DateTime) || node.StartTime.Ticks < minimum.Ticks) {
						minimum = node.StartTime;
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
					StartTime = minimum,
					EndTime = maximum,
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

		public void ProcessProjects (bool showDiagnostics, BuildOutputCounterMetadata metadata) 
		{
			foreach (var p in projects) {
				p.Process ();
			}

			metadata.Verbosity = showDiagnostics ? MSBuildVerbosity.Diagnostic : MSBuildVerbosity.Normal;
			metadata.BuildCount = projects.Count;
			metadata.OnDiskSize = projects.Sum (x => new FileInfo (x.FileName).Length);
			metadata.RootNodesCount = projects.Sum (x => x.RootNodes.Count);
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

				BuildOutput.RaiseProjectStarted ();
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

				BuildOutput.RaiseProjectFinished ();
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

		#pragma warning disable 67 // event not used
		public event EventHandler<TreeNodeEventArgs> NodeInserted;
		public event EventHandler<TreeNodeChildEventArgs> NodeDeleted;
		public event EventHandler<TreeNodeOrderEventArgs> NodesReordered;
		public event EventHandler Cleared;
		#pragma warning restore 67 //

		public event EventHandler<TreeNodeEventArgs> NodeChanged;

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
		CancellationTokenSource cancellation;

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
			cancellation?.Cancel ();
			currentSearchMatches.Clear ();

			currentSearchPattern = pattern;
			currentMatchIndex = -1;

			cancellation = new CancellationTokenSource ();
			var token = cancellation.Token;
			return Task.Run (() => {
				Console.WriteLine ($"{DateTime.Now.ToString ()}: Starting search...");
				if (!string.IsNullOrEmpty (pattern)) {
					// Perform search
					foreach (var root in rootNodes) {
						if (token.IsCancellationRequested) break;
						Console.WriteLine ($"{DateTime.Now.ToString ()}: Searching in {root.Message}");
						root.Search (currentSearchMatches, currentSearchPattern, token);
					}

					if (currentSearchMatches.Count > 0 && !token.IsCancellationRequested) {
						currentMatchIndex = 0;
						Console.WriteLine ($"{DateTime.Now.ToString ()}: Search found {currentSearchMatches.Count} matches");
						return currentSearchMatches [0];
					}
				}

				if (token.IsCancellationRequested) Console.WriteLine ($"{DateTime.Now.ToString ()}: Search canceled");
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

		public bool IsCanceled => cancellation?.IsCancellationRequested ?? false;

		public void Cancel () => cancellation?.Cancel ();

		#endregion
	}
}
