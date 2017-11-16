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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutput
	{
		readonly List<BuildOutputProcessor> projects = new List<BuildOutputProcessor> ();

		public BuildOutput ()
		{
		}

		public ProgressMonitor CreateProgressMonitor ()
		{
			return new BuildOutputProgressMonitor (this);
		}

		public void Load (string filePath)
		{
			if (!File.Exists (filePath)) {
				return;
			}

			switch (Path.GetExtension (filePath)) {
			case ".binlog":
				AddProcessor (new MSBuildOutputProcessor (filePath));
				break;
			default:
				LoggingService.LogError ($"Unknown file type {filePath}");
				break;
			}
		}

		public void Save (string filePath)
		{
		}

		internal void AddProcessor (BuildOutputProcessor processor)
		{
			projects.Add (processor); 
		}

		public (string, IList<IFoldSegment>) ToTextEditor (TextEditor editor, bool includeDiagnostics)
		{
			var buildOutput = new StringBuilder ();
			var foldingSegments = new List<IFoldSegment> ();

			foreach (var p in projects) {
				p.Process ();
				var (s, l) = p.ToTextEditor (editor, includeDiagnostics);
				if (s.Length > 0) {
					buildOutput.Append (s);
					if (l.Count > 0) {
						foldingSegments.AddRange (l);
					}
				}
			}

			return (buildOutput.ToString (), foldingSegments);
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

		protected override void OnObjectReported (object statusObject)
		{
			switch (statusObject) {
			case ProjectStartedProgressEvent pspe:
				if (File.Exists (pspe.LogFile)) {
					BuildOutput.Load (pspe.LogFile); 
				} else {
					currentCustomProject = new BuildOutputProcessor (pspe.LogFile);
					currentCustomProject.AddNode (BuildOutputNodeType.Project, "Custom project", true);
					BuildOutput.AddProcessor (currentCustomProject);
				}
				break;
			case ProjectFinishedProgressEvent psfe:
				if (currentCustomProject != null) {
					currentCustomProject.EndCurrentNode (null);
				}
				currentCustomProject = null;
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
