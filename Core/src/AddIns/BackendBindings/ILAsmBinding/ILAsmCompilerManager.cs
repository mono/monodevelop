// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using Gtk;

using MonoDevelop.Gui.Components;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;

namespace ILAsmBinding
{
	/// <summary>
	/// Description of ILAsmCompilerManager.	
	/// </summary>
	public class ILAsmCompilerManager
	{
		FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
		
		public bool CanCompile(string fileName)
		{
			return Path.GetExtension (fileName).ToLower () == ".il";
		}
		
		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			// FIXME: response file?
			StringBuilder parameters = new StringBuilder();
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
						case BuildAction.Compile:
							if (CanCompile (finfo.Name)) {
								parameters.Append (finfo.Name);
								parameters.Append (" ");
							}
							break;
						default:
							break;
					}
				}
			}
			
			parameters.Append("/out:");
			parameters.Append(configuration.CompiledOutputName);
			parameters.Append(" ");
			
			switch (configuration.CompileTarget) {
				case CompileTarget.Library:
					parameters.Append("/dll ");
					break;
				case CompileTarget.Exe:
					parameters.Append("/exe ");
					break;
				default:
					throw new System.NotSupportedException("Unsupported compilation target : " + configuration.CompileTarget);
			}
			
			if (configuration.DebugMode)
				parameters.Append("/debug ");
				
			string output = String.Empty;
			string error = String.Empty;
			TempFileCollection tf = new TempFileCollection();
			DoCompilation (parameters.ToString (), tf, ref output, ref error);
			ICompilerResult result = ParseOutput(tf, output, error);
			if (result.CompilerOutput.Trim () != "")
				monitor.Log.WriteLine (result.CompilerOutput);
			
			File.Delete(output);
			File.Delete(error);
			return result;
		}

		private void DoCompilation (string outstr, TempFileCollection tf, ref string output, ref string error)
		{
			output = Path.GetTempFileName ();
			error = Path.GetTempFileName ();

			string arguments = String.Format ("-c \"{0} {1} > {2} 2> {3}\"", GetCompilerName (), outstr, output, error);
			ProcessStartInfo si = new ProcessStartInfo ("/bin/sh", arguments);
			si.RedirectStandardOutput = true;
			si.RedirectStandardError = true;
			si.UseShellExecute = false;
			Process p = new Process ();
			p.StartInfo = si;
			p.Start ();
			p.WaitForExit ();
        }
		
		string GetCompilerName ()
		{
			return "ilasm";
		}
		
		ICompilerResult ParseOutput (TempFileCollection tf, string stdout, string stderr)
		{
			StringBuilder compilerOutput = new StringBuilder ();
			CompilerResults cr = new CompilerResults (tf);
			
			foreach (string s in new string[] { stdout, stderr })
			{
				StreamReader sr = File.OpenText (s);
				while (true) {
					string curLine = sr.ReadLine ();
					compilerOutput.Append (curLine);
					compilerOutput.Append ('\n');

					if (curLine == null)
						break;

					curLine = curLine.Trim ();

					if (curLine.Length == 0)
						continue;
				
					CompilerError error = CreateErrorFromString (curLine);
					
					if (error != null)
						cr.Errors.Add (error);
				}
				sr.Close ();
			}
			return new DefaultCompilerResult (cr, compilerOutput.ToString ());
		}

		private static string efile, etext = String.Empty;
		// FIXME: ilasm seems to use > 1 line per error
		private static CompilerError CreateErrorFromString (string error)
		{
			if (error.StartsWith ("Assembling ")) {
				int start = error.IndexOf ('\'');
				int length = error.IndexOf ('\'', start + 1) - start;
				efile = error.Substring (start, length);
			}
			if (error.StartsWith ("syntax error, ")) {
				etext = error;
			}
			if (error.StartsWith ("Error at: ")) {
				string[] info = error.Substring ("Error at: ".Length).Split (' ');
				CompilerError cerror = new CompilerError();
				int col = 0;
				int line = 0;
				try {
					line = int.Parse (info[1].Trim ('(', ')'));
					col = int.Parse (info[3].Trim ('(', ')'));
				} catch {}
				cerror.Line = line;
				cerror.Column = col;
				cerror.ErrorText = etext;
				cerror.FileName = efile;
				return cerror;
			}
			return null;
		}
	}
}

