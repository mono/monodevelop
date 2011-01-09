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
using MonoDevelop.MacDev.Plist;
using System.Linq;
using System.IO;
using MonoDevelop.Core.ProgressMonitoring;
using System.Xml;

namespace MonoDevelop.MacDev
{
	public static class MacBuildUtilities
	{
		public static IEnumerable<FilePair> GetIBFilePairs (IEnumerable<ProjectFile> allItems, string outputRoot)
		{
			return allItems.OfType<ProjectFile> ()
				.Where (pf => pf.BuildAction == BuildAction.Page && pf.FilePath.Extension == ".xib")
				.Select (pf => {
					string[] splits = ((string)pf.ProjectVirtualPath).Split (Path.DirectorySeparatorChar);
					FilePath name = splits.Last ();
					if (splits.Length > 1 && splits[0].EndsWith (".lproj"))
						name = new FilePath (splits[0]).Combine (name);
					return new FilePair (pf.FilePath, name.ChangeExtension (".nib").ToAbsolute (outputRoot));
				});
		}
		
		public static bool NeedsBuilding (FilePair fp)
		{
			return fp.NeedsBuilding ();
		}
		
		public static BuildResult CompileXibFiles (IProgressMonitor monitor, IEnumerable<ProjectFile> files,
		                                           FilePath outputRoot)
		{
			var result = new BuildResult ();
			var ibfiles = GetIBFilePairs (files, outputRoot).Where (NeedsBuilding).ToList ();
			
			if (ibfiles.Count > 0) {
				monitor.BeginTask (GettextCatalog.GetString ("Compiling interface definitions"), 0);	
				foreach (var file in ibfiles) {
					file.EnsureOutputDirectory ();
					var args = new ProcessArgumentBuilder ();
					args.AddQuoted (file.Input);
					args.Add ("--compile");
					args.AddQuoted (file.Output);
					var psi = new ProcessStartInfo ("ibtool", args.ToString ());
					monitor.Log.WriteLine (psi.FileName + " " + psi.Arguments);
					psi.WorkingDirectory = outputRoot;
					string errorOutput;
					int code;
					try {
					code = ExecuteCommand (monitor, psi, out errorOutput);
					} catch (System.ComponentModel.Win32Exception ex) {
						LoggingService.LogError ("Error running ibtool", ex);
						result.AddError (null, 0, 0, null, "ibtool not found. Please ensure the Apple SDK is installed.");
						return result;
					}
					if (code != 0) {
						//FIXME: parse the plist that ibtool returns
						result.AddError (null, 0, 0, null, "ibtool returned error code " + code);
					}
				}
				monitor.EndTask ();
			}
			return result;
		}
		
		public static BuildResult UpdateCodeBehind (IProgressMonitor monitor, XibCodeBehind generator, 
		                                            IEnumerable<ProjectFile> items)
		{
			var result = new BuildResult ();
			var writer = MonoDevelop.DesignerSupport.CodeBehindWriter.CreateForProject (monitor, generator.Project);
			if (!writer.SupportsPartialTypes) {
				monitor.ReportWarning ("Cannot generate designer code, because CodeDom " +
						"provider does not support partial classes.");
				return result;
			}
			
			var files = generator.GetDesignerFilesNeedBuilding (items, false).ToList ();
			if (files.Count == 0)
				return result;
			
			monitor.BeginTask (GettextCatalog.GetString ("Updating CodeBehind files"), 0);
			
			foreach (var f in files) {
				try {
					generator.GenerateDesignerCode (writer, f.Key, f.Value);
					var relPath = f.Value.FilePath.ToRelative (generator.Project.BaseDirectory);
					monitor.Log.WriteLine (GettextCatalog.GetString ("Updated {0}", relPath));
				} catch (Exception ex) {
					result = result ?? new BuildResult ();
					result.AddError (f.Key.FilePath, 0, 0, null, ex.Message);
					LoggingService.LogError (String.Format ("Error generating code for xib file '{0}'", f.Key.FilePath), ex);
				}
			}
			
			writer.WriteOpenFiles ();
			
			monitor.EndTask ();
			return result;
		}
		
		
		
		//copied from MoonlightBuildExtension
		public static int ExecuteCommand (IProgressMonitor monitor, System.Diagnostics.ProcessStartInfo startInfo, out string errorOutput)
		{
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			
			errorOutput = string.Empty;
			int exitCode = -1;
			
			var swError = new StringWriter ();
			var chainedError = new LogTextWriter ();
			chainedError.ChainWriter (monitor.Log);
			chainedError.ChainWriter (swError);
			
			var operationMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				var p = Runtime.ProcessService.StartProcess (startInfo, monitor.Log, chainedError, null);
				operationMonitor.AddOperation (p); //handles cancellation
				
				p.WaitForOutput ();
				errorOutput = swError.ToString ();
				exitCode = p.ExitCode;
				p.Dispose ();
				
				if (monitor.IsCancelRequested) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Build cancelled"));
					monitor.ReportError (GettextCatalog.GetString ("Build cancelled"), null);
					if (exitCode == 0)
						exitCode = -1;
				}
			} finally {
				chainedError.Close ();
				swError.Close ();
				operationMonitor.Dispose ();
			}
			
			return exitCode;
		}
		
		public static BuildResult CreateMergedPlist (IProgressMonitor monitor, 
			ProjectFile template, string outPath,
			Func<PlistDocument,BuildResult> merge)
		{
			var result = new BuildResult ();
			
			var doc = new PlistDocument ();
			if (template != null) {
				try {
					doc.LoadFromXmlFile (template.FilePath);
				} catch (Exception ex) {
					if (ex is XmlException)
						result.AddError (template.FilePath, ((XmlException)ex).LineNumber,
						                 ((XmlException)ex).LinePosition, null, ex.Message);
					else
						result.AddError (template.FilePath, 0, 0, null, ex.Message);
					monitor.ReportError (GettextCatalog.GetString ("Could not load file '{0}': {1}",
					                                               template.FilePath, ex.Message), null);
					return result;
				}
			}
			
			try {
				if (result.Append (merge (doc)).ErrorCount > 0)
					return result;
			} catch (Exception ex) {
				result.AddError ("Error merging Info.plist: " + ex.Message);
				LoggingService.LogError ("Error merging Info.plist", ex);
				return result;
			}
			
			try {
				EnsureDirectoryForFile (outPath);
				doc.WriteToFile (outPath);
			} catch (Exception ex) {
				result.AddError (outPath, 0, 0, null, ex.Message);
				monitor.ReportError (GettextCatalog.GetString ("Could not write file '{0}'", outPath), ex);
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
				var err = GettextCatalog.GetString ("Error: Unable to find '" + tool + "' tool.");
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
			return !File.Exists (Output) || File.GetLastWriteTime (Input) > File.GetLastWriteTime (Output);
		}
		
		public void EnsureOutputDirectory ()
		{
			if (!Directory.Exists (Output.ParentDirectory))
				Directory.CreateDirectory (Output.ParentDirectory);
		}
	}
}

