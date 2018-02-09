﻿//
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
		List<BuildOutputNode> treeRootNodes;

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

		public IEnumerable<BuildOutputNode> GetRootNodes ()
		{
			foreach (var proj in projects) {
				if (proj.RootNodes?.Count > 0) {
					foreach (var node in proj.RootNodes) {
						yield return node;
					}
				}
			}
		}

		internal void RaiseOutputChanged ()
		{
			OutputChanged?.Invoke (this, EventArgs.Empty);
		}


		public List<BuildOutputNode> GetTreeRootNodes (bool includeDiagnostics) 
		{
			if (includeDiagnostics) {
				return GetRootNodes ().ToList ();
			} else {
				// If not including diagnostics, we need to filter the nodes,
				// but instead of doing so now for all, we do it on the fly,
				// as nodes are requested
				var nodes = new List<BuildOutputNode> ();
				foreach (var root in GetRootNodes ()) {
					nodes.Add (new FilteredBuildOutputNode (root, null, includeDiagnostics));
				}
				return nodes;
			}
		}

		public BuildOutputDataSource ToTreeDataSource (bool includeDiagnostics)
		{
			foreach (var p in projects) {
				p.Process ();
			}
			treeRootNodes = GetTreeRootNodes (includeDiagnostics);
			return new BuildOutputDataSource (treeRootNodes);
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
					currentCustomProject.AddNode (BuildOutputNodeType.Project, "Custom project", true, pspe.TimeStamp);
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
				currentCustomProject.AddNode (BuildOutputNodeType.Message, message, false, DateTime.Now);
			}
		}
	}

	class BuildOutputDataSource : ITreeDataSource
	{
		static readonly Xwt.Drawing.Image buildIcon = ImageService.GetIcon(Ide.Gui.Stock.StatusBuild, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image messageIcon = ImageService.GetIcon (Ide.Gui.Stock.MessageLog, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image errorIcon = ImageService.GetIcon (Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image projectIcon = ImageService.GetIcon (Ide.Gui.Stock.Project, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image targetIcon = ImageService.GetIcon(Ide.Gui.Stock.Event, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image taskIcon = ImageService.GetIcon (Ide.Gui.Stock.Execute, Gtk.IconSize.Menu);
		static readonly Xwt.Drawing.Image warningIcon = ImageService.GetIcon (Ide.Gui.Stock.Warning, Gtk.IconSize.Menu);

		readonly List<BuildOutputNode> rootNodes;
		public DataField<Xwt.Drawing.Image> ImageField = new DataField<Xwt.Drawing.Image> (0);
		public DataField<string> LabelField = new DataField<string> (1);

		public BuildOutputDataSource (List<BuildOutputNode> rootNodes)
		{
			this.rootNodes = rootNodes;
		}

		public Type [] ColumnTypes => new Type [] { typeof (Xwt.Drawing.Image), typeof (string) };

		public event EventHandler<TreeNodeEventArgs> NodeInserted;
		public event EventHandler<TreeNodeChildEventArgs> NodeDeleted;
		public event EventHandler<TreeNodeEventArgs> NodeChanged;
		public event EventHandler<TreeNodeOrderEventArgs> NodesReordered;

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
					case BuildOutputNodeType.Project:
						return projectIcon;
					case BuildOutputNodeType.Target:
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

					if (toplevel) {
						markup.AppendFormat ("<b>{0}</b>", GLib.Markup.EscapeText (node.Message));
					} else {
						markup.Append (node.Message);
					}

					// Timing information
					if (node.HasChildren) {
						markup.AppendFormat (" <i>{0}</i>",
						                     GLib.Markup.EscapeText (node.EndTime.Subtract (node.StartTime).ToString (@"hh\:mm\:ss\.fff")));
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
	}
}
