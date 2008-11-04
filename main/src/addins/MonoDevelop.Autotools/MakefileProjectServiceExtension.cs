//
// MakefileProjectServiceExtension.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MonoDevelop.Autotools
{
	public class MakefileProjectServiceExtension : ProjectServiceExtension
	{
		public override WorkspaceItem LoadWorkspaceItem (IProgressMonitor monitor, string fileName)
		{
			WorkspaceItem item = base.LoadWorkspaceItem (monitor, fileName);
			
			Solution sol = item as Solution;
			if (sol != null) {
				//Resolve project references
				try {
					MakefileData.ResolveProjectReferences (sol.RootFolder, monitor);
				} catch (Exception e) {
					LoggingService.LogError (GettextCatalog.GetString (
						"Error resolving Makefile based project references for solution {0}", sol.Name), e);
					monitor.ReportError (GettextCatalog.GetString (
						"Error resolving Makefile based project references for solution {0}", sol.Name), e);
				}
			}
			
			return item;
		}

		
		protected override SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName)
		{
			SolutionEntityItem entry = base.LoadSolutionItem (monitor, fileName);
			if (entry == null)
				return null;
			
			Project project = entry as Project;
			if (project == null)
				return entry;

			//Project
			MakefileData data = entry.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
			if (data == null)
				return entry;

			monitor.BeginTask (GettextCatalog.GetString ("Updating project from Makefile"), 1);
			try { 
				data.OwnerProject = project;
				if (data.IntegrationEnabled)
					data.UpdateProject (monitor, false);
				monitor.Step (1);
			} catch (Exception e) {
				monitor.ReportError (GettextCatalog.GetString (
					"Error loading Makefile for project {0}", project.Name), e);
			} finally {
				monitor.EndTask ();
			}

			entry.SetNeedsBuilding (false);
			return entry;
		}

		public override void Save (IProgressMonitor monitor, SolutionEntityItem entry)
		{
			base.Save (monitor, entry);
			
			Project project = entry as Project;
			if (project == null)
				return;
				
			MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
			if (data == null || !data.IntegrationEnabled)
				return;

			try {
				data.UpdateMakefile (monitor);
			} catch (Exception e) {
				LoggingService.LogError (GettextCatalog.GetString ("Error saving to Makefile ({0}) for project {1}",
					data.AbsoluteMakefileName, project.Name, e));
				monitor.ReportError (GettextCatalog.GetString (
					"Error saving to Makefile ({0}) for project {1}", data.AbsoluteMakefileName, project.Name), e);
			}
		}
		
		public override List<string> GetItemFiles (SolutionEntityItem entry, bool includeReferencedFiles)
		{
			List<string> col = base.GetItemFiles (entry, includeReferencedFiles);
			
			MakefileData data = entry.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
			if (data == null || !data.IntegrationEnabled || string.IsNullOrEmpty (data.AbsoluteMakefileName))
				return col;
			
			col.Add (data.AbsoluteMakefileName);
			if (!string.IsNullOrEmpty (data.RelativeConfigureInPath)) {
				string file = Path.Combine (data.AbsoluteConfigureInPath, "configure.in");
				if (!File.Exists (file))
					file = Path.Combine (data.AbsoluteConfigureInPath, "configure.ac");
				if (File.Exists (file))
					col.Add (file);
			}
			return col;
		}


		//TODO
		protected override bool GetNeedsBuilding (SolutionEntityItem entry, string configuration)
		{
			return base.GetNeedsBuilding (entry, configuration);
		}

		//FIXME: Check whether autogen.sh is required or not
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			Project project = entry as Project;
			if (project == null)
				return base.Build (monitor, entry, configuration);

			MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
			if (data == null || !data.IntegrationEnabled || String.IsNullOrEmpty (data.BuildTargetName))
				return base.Build (monitor, entry, configuration);

			//FIXME: Gen autofoo ? autoreconf?

			string output = String.Empty;
			int exitCode = 0;
			monitor.BeginTask (GettextCatalog.GetString ("Building {0}", project.Name), 1);
			try
			{
				string baseDir = project.BaseDirectory;
	
				StringWriter swOutput = new StringWriter ();
				LogTextWriter chainedOutput = new LogTextWriter ();
				chainedOutput.ChainWriter (monitor.Log);
				chainedOutput.ChainWriter (swOutput);

				ProcessWrapper process = Runtime.ProcessService.StartProcess ("make",
						data.BuildTargetName,
						baseDir, 
						chainedOutput, 
						chainedOutput,
						null);
				process.WaitForOutput ();

				exitCode = process.ExitCode;
				output = swOutput.ToString ();
				chainedOutput.Close ();
				swOutput.Close ();
				monitor.Step ( 1 );
			}
			catch ( Exception e )
			{
				monitor.ReportError ( GettextCatalog.GetString ("Project could not be built: "), e );
				return null;
			}
			finally 
			{
				monitor.EndTask ();
			}

			TempFileCollection tf = new TempFileCollection ();
			Regex regexError = data.GetErrorRegex (false);
			Regex regexWarning = data.GetWarningRegex (false);

			BuildResult cr = ParseOutput (tf, output, project.BaseDirectory, regexError, regexWarning);
			if (exitCode != 0 && cr.FailedBuildCount == 0)
				cr.AddError (GettextCatalog.GetString ("Build failed. See Build Output panel."));
			else
				entry.SetNeedsBuilding (false, configuration);

			return cr;
		}

		BuildResult ParseOutput (TempFileCollection tf, string output, string baseDir, Regex regexError, Regex regexWarning)
		{
			StringBuilder compilerOutput = new StringBuilder();

			CompilerResults cr = new CompilerResults(tf);
			
			// we have 2 formats for the error output the csc gives :
			//Regex normalError  = new Regex(@"(?<file>.*)\((?<line>\d+),(?<column>\d+)\):\s+(?<error>\w+)\s+(?<number>[\d\w]+):\s+(?<message>.*)", RegexOptions.Compiled);
			//Regex generalError = new Regex(@"(?<error>.+)\s+(?<number>[\d\w]+):\s+(?<message>.*)", RegexOptions.Compiled);
			
			Stack<string> dirs = new Stack<string> ();
			dirs.Push (baseDir);
			StringReader sr = new StringReader (output);
			while (true) {

				string curLine = sr.ReadLine();
				compilerOutput.Append(curLine);
				compilerOutput.Append('\n');
				if (curLine == null) {
					break;
				}
				curLine = curLine.Trim();
				if (curLine.Length == 0) {
					continue;
				}
			
				CompilerError error = null;
				
				if (regexError != null) {
					error = CreateCompilerErrorFromString (curLine, dirs, regexError);

					if (error != null)
						cr.Errors.Add (error);
				}

				if (regexWarning != null) {
					error = CreateCompilerErrorFromString (curLine, dirs, regexWarning);
					if (error != null) {
						cr.Errors.Add (error);
						error.IsWarning = true;
					}
				}
			}
			sr.Close();

			return new BuildResult(cr, compilerOutput.ToString());
		}

		// Snatched from our codedom code :-).
		//FIXME: Get this from the language binding.. if a known lang

		static Regex regexEnterDir = new Regex (@"make\[[0-9]*\]: ([a-zA-Z]*) directory `(.*)'");
		
		private static CompilerError CreateCompilerErrorFromString (string error_string, Stack<string> dirs, Regex regex)
		{
			// When IncludeDebugInformation is true, prevents the debug symbols stats from braeking this.
			// FIXME: This should be generic
			if (error_string.StartsWith ("WROTE SYMFILE") ||
			    error_string.StartsWith ("OffsetTable") ||
			    error_string.StartsWith ("Compilation succeeded") ||
			    error_string.StartsWith ("Compilation failed"))
				return null;

			CompilerError error = new CompilerError();

			Match match = regexEnterDir.Match (error_string);
			if (match.Success) {
				//FIXME: Not always available, make -w is required or how?
				//what to use then?
				if (match.Groups [1].Value == "Entering")
					dirs.Push (match.Groups [2].Value);
				else if (match.Groups [1].Value == "Leaving")
					dirs.Pop ();

				return null;
			}

			if (error_string.StartsWith ("make"))
				return null;

			match = regex.Match (error_string);
			if (!match.Success)
				return null;

			string str = GetValue (match, "${file}");
			if (str != null) {
				error.FileName = str;
				if (! Path.IsPathRooted (error.FileName) && dirs.Count > 0)
					error.FileName = Path.Combine (dirs.Peek (), error.FileName);
			}

			str = GetValue (match, "${line}");
			if (str != null)
				error.Line = Int32.Parse (str);

			str = GetValue (match, "${column}");
			if (str != null) {
				if (str == "255+")
					error.Column = -1;
				else
					error.Column = Int32.Parse (str);
			}

			str = GetValue (match, "${number}");
			if (str != null)
				error.ErrorNumber = match.Result("${number}");

			str = GetValue (match, "${message}");
			if (str != null)
				error.ErrorText = match.Result("${message}");

			return error;
		}

		static string GetValue (Match match, string var)
		{
			string str = match.Result (var);
			if (str != var && !String.IsNullOrEmpty (str))
				return str;
			else
				return null;
		}

		protected override void Clean (IProgressMonitor monitor, SolutionEntityItem entry, string configuration)
		{
			Project proj = entry as Project;
			if (proj == null) {
				base.Clean (monitor, entry, configuration);
				return;
			}

			MakefileData data = proj.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
			if (data == null || !data.IntegrationEnabled || String.IsNullOrEmpty (data.CleanTargetName)) {
				base.Clean (monitor, entry, configuration); 
				return;
			}

			monitor.BeginTask ( GettextCatalog.GetString( "Cleaning project"), 1);
			try
			{
				string baseDir = proj.BaseDirectory;
	
				ProcessWrapper process = Runtime.ProcessService.StartProcess ( "make", 
						data.CleanTargetName,
						baseDir, 
						monitor.Log, 
						monitor.Log, 
						null );
				process.WaitForOutput ();

				if ( process.ExitCode > 0 )
					throw new Exception ( GettextCatalog.GetString ("An unspecified error occurred while running '{0}'", "make " + data.CleanTargetName) );

				monitor.Step ( 1 );
			}
			catch ( Exception e )
			{
				monitor.ReportError ( GettextCatalog.GetString ("Project could not be cleaned: "), e );
				return;
			}
			finally 
			{
				monitor.EndTask ();
			}
			monitor.ReportSuccess ( GettextCatalog.GetString ( "Project successfully cleaned"));
		}

		protected override void Execute (IProgressMonitor monitor, SolutionEntityItem entry, ExecutionContext context, string configuration)
		{
			Project project = entry as Project;
			if (project == null) {
				base.Execute (monitor, entry, context, configuration);
				return;
			}

			MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
			if (data == null || !data.IntegrationEnabled || String.IsNullOrEmpty (data.ExecuteTargetName)) {
				base.Execute (monitor, entry, context, configuration);
				return;
			}

			IConsole console = context.ConsoleFactory.CreateConsole (true);
			monitor.BeginTask (GettextCatalog.GetString ("Executing {0}", project.Name), 1);
			try
			{
				ProcessWrapper process = Runtime.ProcessService.StartProcess ("make",
						data.ExecuteTargetName,
						project.BaseDirectory,
						console.Out,
						console.Error,
						null);
				process.WaitForOutput ();

				monitor.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}", process.ExitCode));
				monitor.Step (1);
			} catch (Exception e) {
				monitor.ReportError (GettextCatalog.GetString ("Project could not be executed: "), e);
				return;
			} finally {
				monitor.EndTask ();
				console.Dispose ();
			}
		}

	}
}
