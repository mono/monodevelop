// 
// BuildUtilities.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.Diagnostics;
using MonoDevelop.MacDev.PlistEditor;
using System.Linq;
using System.IO;
using MonoDevelop.Core.ProgressMonitoring;
using System.Xml;

namespace MonoDevelop.MacDev
{
	public static class MacBuildUtilities
	{
		public static bool NeedsBuilding (FilePair fp)
		{
			return fp.NeedsBuilding ();
		}
		
		public static int ExecuteBuildCommand (IProgressMonitor monitor, System.Diagnostics.ProcessStartInfo startInfo)
		{
			return ExecuteBuildCommand (monitor, startInfo, null, null);
		}
		
		/// <summary>Executes a build command, writing output to the monitor.</summary>
		/// <returns>Whether the command executed successfully.</returns>
		/// <param name='monitor'>Progress monitor for writing output and handling cancellation.</param>
		/// <param name='startInfo'>Process start info. Redirection will be enabled if necessary.</param>
		/// <param name='stdout'>Text writer for stdout. May be null.</param>
		/// <param name='stderr'>Text writer for stderr. May be null.</param>
		public static int ExecuteBuildCommand (IProgressMonitor monitor, ProcessStartInfo startInfo,
			TextWriter stdout, TextWriter stderr)
		{
			monitor.Log.WriteLine (startInfo.FileName + " " + startInfo.Arguments);
			
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			
			int exitCode = -1;
			
			TextWriter outWriter = monitor.Log, errWriter = monitor.Log;
			LogTextWriter chainedOut = null, chainedErr = null;
			
			if (stdout != null) {
				chainedOut = new LogTextWriter ();
				chainedOut.ChainWriter (outWriter);
				chainedOut.ChainWriter (stdout);
				outWriter = chainedOut;
			}
			
			if (stderr != null) {
				chainedErr = new LogTextWriter ();
				chainedErr.ChainWriter (errWriter);
				chainedErr.ChainWriter (stderr);
				errWriter = chainedErr;
			}
			
			var operationMonitor = new AggregatedOperationMonitor (monitor);
			
			IProcessAsyncOperation p = null;
			try {
				p = Runtime.ProcessService.StartProcess (startInfo, outWriter, errWriter, null);
				operationMonitor.AddOperation (p); //handles cancellation
				p.WaitForCompleted ();
				exitCode = p.ExitCode;
			} finally {
				if (chainedErr != null)
					chainedErr.Dispose ();
				if (chainedOut != null)
					chainedOut.Dispose ();
				if (p != null)
					p.Dispose ();
				operationMonitor.Dispose ();
			}
			
			if (exitCode != 0)
				monitor.Log.WriteLine ("{0} exited with code {1}", Path.GetFileName (startInfo.FileName), exitCode);
			
			return exitCode;
		}
		
		public static BuildResult CreateMergedPlist (IProgressMonitor monitor, 
			ProjectFile template, string outPath,
			Func<PDictionary,BuildResult> merge)
		{
			var result = new BuildResult ();
			
			PDictionary doc;
			if (template != null) {
				try {
					doc = PDictionary.FromFile (template.FilePath);
				} catch (Exception ex) {
					result.AddError ("Error reading plist template: " + ex.Message, template.FilePath);
					monitor.ReportError (string.Format ("Error reading plist template '{0}'", template.FilePath), ex);
					return result;
				}
			} else {
				doc = new PDictionary ();
			}
			
			try {
				if (result.Append (merge (doc)).ErrorCount > 0)
					return result;
			} catch (Exception ex) {
				string message = string.Format ("Error merging plist file '{0}'", outPath);
				result.AddError (message + ": " + ex.Message);
				monitor.ReportError (message, ex);
				return result;
			}
			
			try {
				EnsureDirectoryForFile (outPath);
				doc.Save (outPath);
			} catch (Exception ex) {
				string message = string.Format ("Error saving plist file '{0}'", outPath);
				result.AddError (message + ": " + ex.Message);
				monitor.ReportError (message, ex);
			}
			return result;
		}
		
		public static void EnsureDirectoryForFile (string filename)
		{
			string dir = Path.GetDirectoryName (filename);
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);
		}
		
		public static ProcessStartInfo GetTool (string tool, DotNetProject project, IProgressMonitor monitor,
		                                   out BuildResult error)
		{
			var toolPath = project.TargetRuntime.GetToolPath (project.TargetFramework, tool);
			if (String.IsNullOrEmpty (toolPath)) {
				var err = GettextCatalog.GetString ("Error: Unable to find '{0}' tool.", tool);
				monitor.ReportError (err, null);
				error = new BuildResult ();
				error.AddError (null, 0, 0, null, err);
				return null;
			}
			
			error = null;
			return new ProcessStartInfo (toolPath) {
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			};
		}
	}
	
	public struct FilePair
	{
		public FilePair (FilePath input, FilePath output)
		{
			this.Input = input;
			this.Output = output;
		}
		
		public FilePath Input, Output;
		
		public bool NeedsBuilding ()
		{
			var output = new FileInfo (Output);
			if (!output.Exists)
				return true;
			
			var input = new FileInfo (Input);
			return input.LastWriteTimeUtc > output.LastWriteTimeUtc;
		}
		
		public void EnsureOutputDirectory ()
		{
			if (!Directory.Exists (Output.ParentDirectory))
				Directory.CreateDirectory (Output.ParentDirectory);
		}
	}
}