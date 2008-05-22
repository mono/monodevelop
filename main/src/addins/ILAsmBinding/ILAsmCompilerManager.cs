//  ILAsmCompilerManager.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using Gtk;

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;

namespace ILAsmBinding
{
	/// <summary>
	/// Description of ILAsmCompilerManager.	
	/// </summary>
	public class ILAsmCompilerManager
	{
		public bool CanCompile(string fileName)
		{
			return Path.GetExtension (fileName).ToLower () == ".il";
		}
		
		public BuildResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
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
			bool pres = DoCompilation (parameters.ToString (), tf, ref output, ref error);
			BuildResult result = ParseOutput(tf, output, error);
			if (result.CompilerOutput.Trim () != "")
				monitor.Log.WriteLine (result.CompilerOutput);
			
			if (!pres && result.ErrorCount == 0)
				result.AddError (GettextCatalog.GetString ("Compilation failed."));
			
			File.Delete(output);
			File.Delete(error);
			return result;
		}

		private bool DoCompilation (string outstr, TempFileCollection tf, ref string output, ref string error)
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
			return p.ExitCode == 0;
        }
		
		string GetCompilerName ()
		{
			return "ilasm";
		}
		
		BuildResult ParseOutput (TempFileCollection tf, string stdout, string stderr)
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
			return new BuildResult (cr, compilerOutput.ToString ());
		}

		static Regex regexError = new Regex (@"^(\s*(?<file>.*)\s\((?<line>\d*)(,\s(?<column>\d*[\+]*))?\)\s(:|)\s+)*(?<level>\w+)\s*:\s*(?<message>.*)",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		
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
			CompilerError err = new CompilerError();

			Match match=regexError.Match (error);
			if (!match.Success) return null;
			if (String.Empty != match.Result("${file}"))
				err.FileName=match.Result("${file}");
			if (String.Empty != match.Result("${line}"))
				err.Line=Int32.Parse(match.Result("${line}"));
			if (String.Empty != match.Result("${column}")) {
				if (match.Result("${column}") == "255+")
					err.Column = -1;
				else
					err.Column=Int32.Parse(match.Result("${column}"));
			}
			if (match.Result("${level}").ToLower () == "warning")
				err.IsWarning=true;
			err.ErrorText=match.Result("${message}");
			return err;
		}
	}
}

