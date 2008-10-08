// Python25Compiler.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.CodeGeneration;

using PyBinding.Runtime;

namespace PyBinding.Compiler
{
	public class Python25Compiler : IPythonCompiler
	{
		static readonly string m_CompileFormat =
			"-c \"import py_compile; py_compile.compile('{0}','{1}c');\"";
		static readonly string m_OptimizedCompileFormat =
			"-O -c \"import py_compile; py_compile.compile('{0}','{1}o');\"";
		
		IPythonRuntime m_Runtime = null;
		Regex m_WarningRegex;
		Regex m_ErrorRegex;
		
		public IPythonRuntime Runtime {
			get {
				return this.m_Runtime;
			}
			set {
				this.m_Runtime = value;
			}
		}
		
		public Python25Compiler ()
		{
			m_WarningRegex = new Regex ("(?<file>.*):(?<line>\\d+): Warning: (?<message>.*)");
			m_ErrorRegex = new Regex ("  File \"(?<file>.*)\", line (?<line>\\d+)");
		}
		
		public void Compile (PythonProject project,
		                     string fileName,
		                     PythonConfiguration config,
		                     BuildResult result)
		{
			if (String.IsNullOrEmpty (fileName))
				throw new ArgumentNullException ("fileName");
			else if (config == null)
				throw new ArgumentNullException ("config");
			else if (result == null)
				throw new ArgumentNullException ("result");
			else if (Runtime == null)
				throw new InvalidOperationException ("No supported runtime!");
			
			// Get our relative path within the project
			if (!fileName.StartsWith (project.BaseDirectory)) {
				Console.WriteLine ("File is not within our project!");
				return;
			}
			
			// Get our output file path
			int len = project.BaseDirectory.Length;
			if (len < fileName.Length && fileName[len] == Path.DirectorySeparatorChar) {
				len++;
			}
			string outFile = Path.Combine (config.OutputDirectory, fileName.Substring (len));
			
			// Create the destination directory
			FileInfo fileInfo = new FileInfo (outFile);
			if (!fileInfo.Directory.Exists) {
				fileInfo.Directory.Create ();
			}
			
			// Create and start our process to generate the byte code
			Process process = BuildCompileProcess (fileName, outFile, config.Optimize);
			process.Start ();
			process.WaitForExit ();
			
			// Parse errors and warnings
			string output = process.StandardError.ReadToEnd ();
			
			// Extract potential Warnings
			foreach (Match m in m_WarningRegex.Matches (output)) {
				string lineNum  = m.Groups[m_WarningRegex.GroupNumberFromName ("line")].Value;
				string message  = m.Groups[m_WarningRegex.GroupNumberFromName ("message")].Value;
				
				result.AddWarning (fileName, Int32.Parse (lineNum), 0, String.Empty, message);
			}
			
			// Extract potential SyntaxError
			foreach (Match m in m_ErrorRegex.Matches (output)) {
				string lineNum = m.Groups[m_ErrorRegex.GroupNumberFromName ("line")].Value;
				result.AddError (fileName, Int32.Parse (lineNum), 0, String.Empty, "SyntaxError");
			}
		}
		
		Process BuildCompileProcess (string fileName, string outFile, bool optimize)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo ();
			startInfo.FileName = this.Runtime.Path;
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;
			
			if (optimize) {
				startInfo.Arguments =
					String.Format (m_OptimizedCompileFormat, fileName, outFile);
			}
			else {
				startInfo.Arguments =
					String.Format (m_CompileFormat, fileName, outFile);
			}
			
			Process process = new Process ();
			process.StartInfo = startInfo;
			
			return process;
		}
	}
}
