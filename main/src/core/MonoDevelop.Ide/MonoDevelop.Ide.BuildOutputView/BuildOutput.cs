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

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutput : IDisposable
	{
		BuildOutputProgressMonitor progressMonitor;
		readonly List<BuildOutputProcessor> projects = new List<BuildOutputProcessor> ();
		string filePath;

		public string FilePath {
			get{
				return filePath;
			}
		}

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
				this.filePath = filePath;
				break;
			default:
				LoggingService.LogError ($"Unknown file type {filePath}");
				break;
			}
		}

		internal void UpdateFilePath (FilePath fileName)
		{
			filePath = fileName;
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

		internal void RaiseOutputChanged ()
		{
			OutputChanged?.Invoke (this, EventArgs.Empty);
		}

		public async Task<(string, IList<IFoldSegment>)> ToTextEditor (TextEditor editor, bool includeDiagnostics)
		{
			var buildOutput = new StringBuilder ();
			var foldingSegments = new List<IFoldSegment> ();

			foreach (var p in projects) {
				p.Process ();
				var (s, l) = await p.ToTextEditor (editor, includeDiagnostics, buildOutput.Length);
				if (s.Length > 0) {
					buildOutput.Append (s);
					if (l.Count > 0) {
						foldingSegments.AddRange (l);
					}
				}
			}

			return (buildOutput.ToString (), foldingSegments);
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
					currentCustomProject.AddNode (BuildOutputNodeType.Project, "Custom project", true);
					BuildOutput.AddProcessor (currentCustomProject);
				}
				break;
			case ProjectFinishedProgressEvent psfe:
				if (currentCustomProject != null) {
					currentCustomProject.EndCurrentNode (null);
				}
				currentCustomProject = null;
				BuildOutput.RaiseOutputChanged ();
				break;
			}
		}

		protected override void OnWriteLog (string message)
		{
			if (currentCustomProject != null) {
				currentCustomProject.AddNode (BuildOutputNodeType.Message, message, false);
			}
		}
	}
}
