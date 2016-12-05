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
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Autotools
{
	public class MakefileProjectServiceExtension : SolutionExtension
	{
		protected override void OnReadSolution (ProgressMonitor monitor, MonoDevelop.Projects.MSBuild.SlnFile file)
		{
			base.OnReadSolution (monitor, file);

			//Resolve project references
			try {
				MakefileData.ResolveProjectReferences (Solution.RootFolder, monitor);
			} catch (Exception e) {
				LoggingService.LogError (GettextCatalog.GetString (
					"Error resolving Makefile based project references for solution {0}", Solution.Name), e);
				monitor.ReportError (GettextCatalog.GetString (
					"Error resolving Makefile based project references for solution {0}", Solution.Name), e);
			}

			// All done, dispose myself
			Dispose ();
		}
	}

	public class MakefileProjectExtension: ProjectExtension
	{
		MakefileData data;

		public MakefileProjectExtension ()
		{
		}

		public MakefileData MakefileData {
			get { return data; }
			set { data = value; }
		}

		protected override void OnReadProject (ProgressMonitor monitor, MonoDevelop.Projects.MSBuild.MSBuildProject msproject)
		{
			base.OnReadProject (monitor, msproject);
			var ext = msproject.GetMonoDevelopProjectExtension ("MonoDevelop.Autotools.MakefileInfo");
			if (ext == null)
				return;

			data = MakefileData.Read (ext);
			if (data == null)
				return;

			monitor.BeginTask (GettextCatalog.GetString ("Updating project from Makefile"), 1);
			try { 
				data.OwnerProject = Project;
				if (data.SupportsIntegration)
					data.UpdateProject (monitor, false);
				monitor.Step (1);
			} catch (Exception e) {
				monitor.ReportError (GettextCatalog.GetString (
					"\tError loading Makefile for project {0}", Project.Name), e);
			} finally {
				monitor.EndTask ();
			}
		}

		protected override void OnWriteProject (ProgressMonitor monitor, MonoDevelop.Projects.MSBuild.MSBuildProject msproject)
		{
			base.OnWriteProject (monitor, msproject);

			if (data == null)
				return;

			msproject.SetMonoDevelopProjectExtension ("MonoDevelop.Autotools.MakefileInfo", data.Write ());

			if (!data.SupportsIntegration)
				return;
			
			try {
				data.UpdateMakefile (monitor);
			} catch (Exception e) {
				LoggingService.LogError (GettextCatalog.GetString ("Error saving to Makefile ({0}) for project {1}",
					data.AbsoluteMakefileName, Project.Name, e));
				monitor.ReportError (GettextCatalog.GetString (
					"Error saving to Makefile ({0}) for project {1}", data.AbsoluteMakefileName, Project.Name), e);
			}
		}

		protected override IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> col = base.OnGetItemFiles (includeReferencedFiles).ToList ();
			
			if (data == null || !data.SupportsIntegration || string.IsNullOrEmpty (data.AbsoluteMakefileName))
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


		//FIXME: Check whether autogen.sh is required or not
		protected async override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			if (data == null || !data.SupportsIntegration || String.IsNullOrEmpty (data.BuildTargetName))
				return await base.OnBuild (monitor, configuration, operationContext);

			//FIXME: Gen autofoo ? autoreconf?

			string output = String.Empty;
			int exitCode = 0;
			monitor.BeginTask (GettextCatalog.GetString ("Building {0}", Project.Name), 1);
			try
			{
				string baseDir = Project.BaseDirectory;
				string args = string.Format ("-j {0} {1}", data.ParallelProcesses, data.BuildTargetName);
	
				using (var swOutput = new StringWriter ()) {
					using (var chainedOutput = new LogTextWriter ()) {
						chainedOutput.ChainWriter (monitor.Log);
						chainedOutput.ChainWriter (swOutput);

						using (ProcessWrapper process = Runtime.ProcessService.StartProcess ("make",
								args,
								baseDir, 
								chainedOutput, 
								chainedOutput,
							null)) {

							await process.Task;

							chainedOutput.UnchainWriter (monitor.Log);
							chainedOutput.UnchainWriter (swOutput);

							exitCode = process.ExitCode;
							output = swOutput.ToString ();
							monitor.Step ( 1 );
						}
					}
				}
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

			BuildResult cr = ParseOutput (tf, output, Project.BaseDirectory, regexError, regexWarning);
			if (exitCode != 0 && cr.FailedBuildCount == 0)
				cr.AddError (GettextCatalog.GetString ("Build failed. See Build Output panel."));

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

		protected async override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			if (data == null || !data.SupportsIntegration || String.IsNullOrEmpty (data.CleanTargetName)) {
				return await base.OnClean (monitor, configuration, operationContext); 
			}

			monitor.BeginTask ( GettextCatalog.GetString( "Cleaning project"), 1);
			try
			{
				string baseDir = Project.BaseDirectory;
	
				ProcessWrapper process = Runtime.ProcessService.StartProcess ( "make", 
						data.CleanTargetName,
						baseDir, 
						monitor.Log, 
						monitor.Log, 
						null );

				await process.Task;

				if ( process.ExitCode > 0 )
					throw new Exception ( GettextCatalog.GetString ("An unspecified error occurred while running '{0}'", "make " + data.CleanTargetName) );

				monitor.Step ( 1 );
			}
			catch ( Exception e )
			{
				monitor.ReportError ( GettextCatalog.GetString ("Project could not be cleaned: "), e );
				var res = new BuildResult ();
				res.AddError (GettextCatalog.GetString ("Project could not be cleaned: ") + e.Message);
				return res;
			}
			finally 
			{
				monitor.EndTask ();
			}
			monitor.ReportSuccess ( GettextCatalog.GetString ( "Project successfully cleaned"));
			return BuildResult.CreateSuccess ();
		}

		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (data != null && data.SupportsIntegration && !String.IsNullOrEmpty (data.ExecuteTargetName))
				return true;
			return base.OnGetCanExecute (context, configuration, runConfiguration);
		}


		protected async override Task OnExecute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			if (data == null || !data.SupportsIntegration || String.IsNullOrEmpty (data.ExecuteTargetName)) {
				await base.OnExecute (monitor, context, configuration, runConfiguration);
				return;
			}

			OperationConsole console = context.ConsoleFactory.CreateConsole (
				OperationConsoleFactory.CreateConsoleOptions.Default.WithTitle (Project.Name));
			monitor.BeginTask (GettextCatalog.GetString ("Executing {0}", Project.Name), 1);
			try
			{
				ProcessWrapper process = Runtime.ProcessService.StartProcess ("make",
					data.ExecuteTargetName,
					Project.BaseDirectory,
					console.Out,
					console.Error,
					null);

				await process.Task;

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
