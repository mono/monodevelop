// 
// TextTemplatingFilePreprocessor.cs
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
using System.CodeDom.Compiler;
using System.IO;
using Mono.TextTemplating;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CustomTools;
using MonoDevelop.Projects;

namespace MonoDevelop.TextTemplating
{
	public class TextTemplatingFilePreprocessor : ISingleFileCustomTool
	{
		public IAsyncOperation Generate (IProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return new ThreadAsyncOperation (delegate {
				using (var host = new ProjectFileTemplatingHost (file, IdeApp.Workspace.ActiveConfiguration)) {

					string outputFile;
					Generate (host, file, out outputFile);

					result.GeneratedFilePath = outputFile;
					result.Errors.AddRange (host.Errors);

					foreach (var err in host.Errors)
						monitor.Log.WriteLine (err);
				}
			}, result);
		}

		static void Generate (TemplateGenerator host, ProjectFile file, out string outputFile)
		{
			outputFile = null;

			string content;
			try {
				content = File.ReadAllText (file.FilePath);
			}
			catch (IOException ex) {
				host.Errors.Add (new CompilerError {
					ErrorText = "Could not read input file '" + file.FilePath + "':\n" + ex
				});
				return;
			}

			var pt = ParsedTemplate.FromText (content, host);
			if (pt.Errors.HasErrors) {
				host.Errors.AddRange (pt.Errors);
				return;
			}

			var settings = TemplatingEngine.GetSettings (host, pt);
			if (pt.Errors.HasErrors) {
				host.Errors.AddRange (pt.Errors);
				return;
			}

			outputFile = file.FilePath.ChangeExtension (settings.Provider.FileExtension);
			settings.Name = settings.Provider.CreateValidIdentifier (file.FilePath.FileNameWithoutExtension);
			settings.Namespace = CustomToolService.GetFileNamespace (file, outputFile);
			settings.IncludePreprocessingHelpers = string.IsNullOrEmpty (settings.Inherits);
			settings.IsPreprocessed = true;

			var ccu = TemplatingEngine.GenerateCompileUnit (host, content, pt, settings);
			host.Errors.AddRange (pt.Errors);
			if (pt.Errors.HasErrors) {
				return;
			}

			try {
				using (var writer = new StreamWriter (outputFile, false, System.Text.Encoding.UTF8)) {
					settings.Provider.GenerateCodeFromCompileUnit (ccu, writer, new CodeGeneratorOptions ());
				}
			}
			catch (IOException ex) {
				host.Errors.Add (new CompilerError {
					ErrorText = "Could not write output file '" + outputFile + "':\n" + ex
				});
			}
		}
	}
}

