//
// CSharpBackendBinding.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

using CSharpBinding.Parser;

namespace CSharpBinding
{
	public class CSharpBackendBinding : AbstractBackendBinding
	{
		public override string CommentTag {
			get { return "//"; }
		}
		
		public ClrVersion[] SupportedClrVersions {
			get {
				return new ClrVersion[] { ClrVersion.Net_1_1, ClrVersion.Net_2_0 };
			}
		}
		
		TParser parser = new TParser ();
		public override IParser Parser {
			get { return parser; }
		}
		
		CSharpRefactorer refactorer = new CSharpRefactorer ();
		public override IRefactorer Refactorer {
			get { return refactorer; }
		}
		
		public CSharpBackendBinding() : base (true)
		{
		}
		
#region running projects
		
		public override void StartProject (IProject project, IProgressMonitor monitor, ExecutionContext context)
		{
			((MSBuildProject)project).Start (monitor, context);
		}
		
#endregion
		
		public override IProject LoadProject (string fileName)
		{
			return MSBuildProject.Load (fileName);
		}
		
		string ToOutput (string outputType)
		{
			return outputType.ToLower ();
		}
		
		string GetExtension (string outputType)
		{
			if (outputType.ToLower () == "exe" || outputType.ToLower () == "winexe") {
				return ".exe";
			}
			return ".dll";
		}
		
		public override CompilerResult Compile (IProject prj, IProgressMonitor monitor)
		{
			MSBuildProject project = prj as MSBuildProject;
			string responseFileName = Path.GetTempFileName ();
			Console.WriteLine (responseFileName);
			StreamWriter writer = new StreamWriter (responseFileName);
			try {
				writer.WriteLine ("/noconfig");
				writer.WriteLine ("/nologo");
				writer.WriteLine ("/codepage:utf8");
				writer.WriteLine ("/t:{0}", ToOutput (project.OutputType));
				if (!String.IsNullOrEmpty (project.DefineConstants))
					writer.WriteLine ("/d:{0}", project.DefineConstants);
				
				string assemblyName = project.AssemblyName + GetExtension (project.OutputType);
				if (String.IsNullOrEmpty (project.OutputPath)) {
					writer.WriteLine ("\"/out:{0}\"", assemblyName);
				} else {
					string path = SolutionProject.NormalizePath (project.OutputPath);
					if (!Directory.Exists (Path.Combine (project.BasePath, path))) {
						Directory.CreateDirectory (Path.Combine (project.BasePath, path));
					}
					writer.WriteLine ("\"/out:{0}\"", Path.Combine(path, assemblyName));
				}
				
				foreach (ProjectItem item in project.Items) {
					ReferenceProjectItem referenceItem = item as ReferenceProjectItem;
					if (referenceItem != null) {
						string reference = referenceItem.Include;
						if (!String.IsNullOrEmpty (referenceItem.HintPath)) {
							string fileName = Path.Combine (project.BasePath, SolutionProject.NormalizePath (referenceItem.HintPath));
							if (File.Exists (fileName))
								reference = fileName;
						}
						writer.WriteLine ("\"/r:{0}\"", reference);
					}
				}
				
				foreach (ProjectItem item in project.Items) {
					Console.WriteLine ("item:" + item);
					if (item is ProjectFile) {
						writer.WriteLine ("\"{0}\"", item.Include);
					}
				}
			} catch (Exception e) {
				Console.WriteLine ("error:" + e);
			} finally {
				writer.Close();
			}
			
			string output = Path.GetTempFileName ();
			string error = Path.GetTempFileName ();
			
			StreamWriter outWriter = new StreamWriter (output);
			StreamWriter errWriter = new StreamWriter (error);
			try {
				ProcessWrapper pw = Runtime.ProcessService.StartProcess ("gmcs", "\"@" + responseFileName + "\"", project.BasePath, outWriter, errWriter, delegate {});
				pw.WaitForExit ();
			} finally {
				errWriter.Close ();
				outWriter.Close ();
			}
			
			CompilerResult result = ParseOutput (output, error);
			
			Runtime.FileService.DeleteFile (error);
			Runtime.FileService.DeleteFile (output);
			//Runtime.FileService.DeleteFile (responseFileName);
			
			return result;
		}
		
		CompilerResult ParseOutput (string stdout, string stderr)
		{
			StringBuilder compilerOutput = new StringBuilder();
			CompilerResults cr = new CompilerResults (new TempFileCollection ());
			foreach (string fileName in new string[] { stdout, stderr }) {
				StreamReader reader = File.OpenText (fileName);
				try {
					while (true) {
						string curLine = reader.ReadLine();
						if (curLine == null) {
							break;
						}
						compilerOutput.Append (curLine);
						compilerOutput.Append (Environment.NewLine);
						curLine = curLine.Trim();
						if (curLine.Length == 0) {
							continue;
						}
						CompilerError error = CreateErrorFromString (curLine);
						if (error != null)
							cr.Errors.Add (error);
					}
				} finally {
					reader.Close ();
				}
			}
			return new CompilerResult (cr, compilerOutput.ToString ());
		}
		
#region Snatched from our codedom code.
		static Regex regexError = new Regex (@"^(\s*(?<file>.*)\((?<line>\d*)(,(?<column>\d*[\+]*))?\)(:|)\s+)*(?<level>\w+)\s*(?<number>.*):\s(?<message>.*)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static CompilerError CreateErrorFromString(string error_string)
		{
			// When IncludeDebugInformation is true, prevents the debug symbols stats from braeking this.
			if (error_string.StartsWith ("WROTE SYMFILE") ||
			    error_string.StartsWith ("OffsetTable") ||
			    error_string.StartsWith ("Compilation succeeded") ||
			    error_string.StartsWith ("Compilation failed"))
				return null;

			CompilerError error = new CompilerError();

			Match match=regexError.Match(error_string);
			if (!match.Success) return null;
			if (String.Empty != match.Result("${file}"))
				error.FileName=match.Result("${file}");
			if (String.Empty != match.Result("${line}"))
				error.Line=Int32.Parse(match.Result("${line}"));
			if (String.Empty != match.Result("${column}")) {
				if (match.Result("${column}") == "255+")
					error.Column = -1;
				else
					error.Column=Int32.Parse(match.Result("${column}"));
			}
			if (match.Result("${level}")=="warning")
				error.IsWarning=true;
			error.ErrorNumber=match.Result("${number}");
			error.ErrorText=match.Result("${message}");
			return error;
		}
#endregion
	}
}
