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

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutput : IDisposable
	{
		BuildOutputProgressMonitor progressMonitor;
		readonly List<BuildOutputProcessor> projects = new List<BuildOutputProcessor> ();
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
			public BuildFinishedEventArgs ProcessFile (string filePath, bool buildHasStarted)
			{
				BuildFinishedEventArgs buildFinishedEvent = null;

				var eventsSource = new BinaryLogReplayEventSource ();
				eventsSource.AnyEventRaised += (sender, e) => {
					if (e is BuildStatusEventArgs && buildHasStarted) {
						// We only want a single BuildStarted event
						return;
					} else if (e is BuildFinishedEventArgs) {
						buildFinishedEvent = (BuildFinishedEventArgs)e;
						return;
					}

					Dispatch (e);
				};

				eventsSource.Replay (filePath);

				return buildFinishedEvent;
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
					bool buildHasStarted = false;
					BuildFinishedEventArgs buildFinishedEvent = null;
					logger.Initialize (eventsSource);

					foreach (var proj in projects) {
						switch (proj) {
						case MSBuildOutputProcessor msbop:
							buildFinishedEvent = eventsSource.ProcessFile (proj.FileName, buildHasStarted);
							break;
						case BuildOutputProcessor bop:
							// FIXME
							break;
						default:
							continue;
						}

						buildHasStarted = true;
					}

					// Emit BuildFinished event
					if (buildHasStarted && buildFinishedEvent != null) {
						eventsSource.Dispatch (buildFinishedEvent);
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
			projects.Add (processor);
		}

		internal void Clear ()
		{
			projects.Clear ();
			RaiseOutputChanged ();
		}

		internal void RaiseOutputChanged ()
		{
			OutputChanged?.Invoke (this, EventArgs.Empty);
		}

		public List<BuildOutputNode> GetRootNodes (bool includeDiagnostics)
		{
			var comparer = new BuildOutPutNodeComparer ();
			var nodes = new List<BuildOutputNode> ();

			foreach (var root in GetProjectRootNodes ()) {
				//let's check if root exists according to solution level comparer
				var solutionNode = nodes.Find (node => comparer.Equals (node, root));
				if (solutionNode != null) {
					//now we add its children to existent one since are in the same solution
					foreach (var child in root.Children)
						solutionNode.AddChild (new FilteredBuildOutputNode (child, includeDiagnostics));
				} else {
					nodes.Add (new FilteredBuildOutputNode (root, includeDiagnostics));
				}
			}
			return nodes;
		}

		IEnumerable<BuildOutputNode> GetProjectRootNodes ()
		{
			foreach (var proj in projects) {
				foreach (var node in proj.RootNodes) {
					yield return node;
				}
			}
		}

		public void ProcessProjects () 
		{
			foreach (var p in projects) {
				p.Process ();
			}
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

		/// <summary>
		/// BuildOutPutNode comparer for grouping Solutions
		/// </summary>
		class BuildOutPutNodeComparer : IEqualityComparer<BuildOutputNode>
		{
			public bool Equals (BuildOutputNode x, BuildOutputNode y)
			{
				return x.Message == y.Message && x.FullMessage == y.FullMessage;
			}

			public int GetHashCode (BuildOutputNode obj) => obj.GetHashCode ();
		}
	}

	class BuildOutputProgressMonitor : ProgressMonitor
	{
		BuildOutputProcessor currentCustomProject;

		public BuildOutput BuildOutput { get; }

		public BuildOutputProgressMonitor (BuildOutput output)
		{
			BuildOutput = output;
		}

		protected override void OnWriteLogObject (object logObject)
		{
			switch (logObject) {
			case ProjectStartedProgressEvent pspe:
				if (File.Exists (pspe.LogFile)) {
					BuildOutput.Load (pspe.LogFile, true); 
				} else {
					currentCustomProject = new BuildOutputProcessor (pspe.LogFile, false);
					currentCustomProject.AddNode (BuildOutputNodeType.Project,
					                              GettextCatalog.GetString ("Custom project"),
					                              GettextCatalog.GetString ("Custom project started building"),
					                              true, pspe.TimeStamp);
					BuildOutput.AddProcessor (currentCustomProject);
				}
				break;
			case ProjectFinishedProgressEvent psfe:
				if (currentCustomProject != null) {
					currentCustomProject.EndCurrentNode (null, psfe.TimeStamp);
				}
				currentCustomProject = null;
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
		static readonly Xwt.Drawing.Image buildIcon = ImageService.GetIcon (Ide.Gui.Stock.StatusBuild, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image messageIcon = ImageService.GetIcon (Ide.Gui.Stock.MessageLog, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image errorIcon = ImageService.GetIcon (Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image projectIcon = ImageService.GetIcon (Ide.Gui.Stock.Project, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image targetIcon = ImageService.GetIcon (Ide.Gui.Stock.Event, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image taskIcon = ImageService.GetIcon (Ide.Gui.Stock.Execute, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image warningIcon = ImageService.GetIcon (Ide.Gui.Stock.Warning, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image folderIcon = ImageService.GetIcon (Ide.Gui.Stock.OpenFolder, Gtk.IconSize.Menu);

		public IReadOnlyList<BuildOutputNode> RootNodes => this.rootNodes;
		readonly List<BuildOutputNode> rootNodes;

		public DataField<Xwt.Drawing.Image> ImageField = new DataField<Xwt.Drawing.Image> (0);
		public DataField<string> LabelField = new DataField<string> (1);

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

		const string LightTextMarkup = "<span color =\"#999999\">{0}</span>";

		public object GetValue (TreePosition pos, int column)
		{
			var node = pos as BuildOutputNode;
			if (node != null) {
				switch (column) {
				case 0: // Image
					switch (node.NodeType) {
					case BuildOutputNodeType.Build:
						return buildIcon;
					case BuildOutputNodeType.Diagnostics:
					case BuildOutputNodeType.Message:
						return messageIcon;
					case BuildOutputNodeType.Error:
						return errorIcon;
					case BuildOutputNodeType.Parameters:
						return folderIcon;
					case BuildOutputNodeType.Project:
						return projectIcon;
					case BuildOutputNodeType.Target:
					case BuildOutputNodeType.TargetSkipped:
						return targetIcon;
					case BuildOutputNodeType.Task:
						return taskIcon;
					case BuildOutputNodeType.Warning:
						return warningIcon;
					}

					return ImageService.GetIcon (Ide.Gui.Stock.Empty);
				case 1: // Text
					bool toplevel = node.Parent == null;
					StringBuilder markup = new StringBuilder ();

					switch (node.NodeType) {
					case BuildOutputNodeType.TargetSkipped:
						markup.AppendFormat (LightTextMarkup, GLib.Markup.EscapeText (node.Message));
						break;
					default:
						if (toplevel) {
							markup.AppendFormat ("<b>{0}</b>", GLib.Markup.EscapeText (node.Message));
						} else {
							markup.Append (node.Message);
						}
						break;
					}

					// Timing information
					if (node.HasChildren) {
						markup.Append ("    ");
						markup.AppendFormat (LightTextMarkup, GLib.Markup.EscapeText (node.GetDurationAsString ()));
					}

					return markup.ToString ();
				}
			}

			return null;
		}

		public void SetValue (TreePosition pos, int column, object value)
		{
			throw new NotImplementedException ();
		}

		#endregion
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
