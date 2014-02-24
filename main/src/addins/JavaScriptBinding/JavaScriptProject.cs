//
// JavaScriptProject.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Projects;
using TypeScriptBinding.Hosting;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Text.RegularExpressions;

namespace JavaScriptBinding
{
	[DataInclude(typeof(JavaScriptProjectConfiguration))]
	public class JavaScriptProject : Project
	{
		public override string ProjectType {
			get {
				return "JavaScript";
			}
		}

		public override string[] SupportedLanguages {
			get { return new [] { "JavaScript", "TypeScript" }; }
		}

		public JavaScriptProject ()
		{
		}

		public JavaScriptProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			AddNewConfiguration ("Debug"); 
			AddNewConfiguration ("Release"); 
		}

		Jurassic.ScriptEngine engine;

		ScriptingHost host;

		protected override BuildResult DoBuild (MonoDevelop.Core.IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			if (engine == null) {

				using (var operation = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (GettextCatalog.GetString ("Prepare type script engine"), null)) {
					operation.BeginTask (GettextCatalog.GetString ("Prepare type script engine"), 1);
					engine = new Jurassic.ScriptEngine ();
					engine.EnableExposedClrTypes = true;

					host = new ScriptingHost (engine, this.BaseIntermediateOutputPath);
					engine.SetGlobalValue ("host", host); 

					using (var stream = new StreamReader (typeof(JavaScriptProject).Assembly.GetManifestResourceStream ("tsc.js"))) {
						engine.Execute (stream.ReadToEnd ());
					}
					operation.EndTask ();
				}
			}
			host.ResetIO ();
			foreach (var file in Files) {
				if (file.BuildAction == BuildAction.Compile) {
					if (file.FilePath.Extension != ".ts")
						continue;
					host.ExecutingFilePath = file.FilePath.ParentDirectory;
					host.Arguments = engine.Array.New (new string[] {
						file.FilePath.FileName
					});
					engine.Execute (@"
Environment = host.Environment;
var batch = new TypeScript.BatchCompiler(host);
batch.batchCompile();
");
				}
			}

			var result = new BuildResult ();

			result.CompilerOutput = host.OutWriter + Environment.NewLine + host.ErrorWriter;
			using (var sr = new StringReader (result.CompilerOutput)) {
				while (true) {
					string curLine = sr.ReadLine ();
				
					if (curLine == null)
						break;

					curLine = curLine.Trim ();
					if (curLine.Length == 0)
						continue;

					var error = CreateErrorFromString ("", curLine);

					if (error != null)
						result.Append (error);
				}
			}
			return result;
		}

		public sealed override SolutionItemConfiguration CreateConfiguration (string name)
		{
			return new JavaScriptProjectConfiguration (name);
		}

		static readonly Regex regexError = new Regex (@"^(\s*(?<file>.+[^)])(\((?<line>\d*)(,(?<column>\d*[\+]*))?\))?:\s+)*(?<level>\w+)\s+(?<number>..\d+):\s*(?<message>.*)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		static BuildError CreateErrorFromString (string basePath, string error_string)
		{
			Match match = regexError.Match(error_string);
			if (!match.Success) 
				return null;

			BuildError error = new BuildError ();
			FilePath filename = match.Result ("${file}");
			if (filename.IsNullOrEmpty) {
				filename = FilePath.Empty;
			} else if (!filename.IsAbsolute && basePath != null) {
				filename = filename.ToAbsolute (basePath);
			}
			error.FileName = filename;

			string line = match.Result ("${line}");
			error.Line = !string.IsNullOrEmpty (line) ? Int32.Parse (line) : 0;

			string col = match.Result ("${column}");
			if (!string.IsNullOrEmpty (col)) 
				error.Column = col == "255+" ? -1 : Int32.Parse (col);

			error.IsWarning   = match.Result ("${level}") == "warning";
			error.ErrorNumber = match.Result ("${number}");
			error.ErrorText   = match.Result ("${message}");
			return error;
		}
	}
}
