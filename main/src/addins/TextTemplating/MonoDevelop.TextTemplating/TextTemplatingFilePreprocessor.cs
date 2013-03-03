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
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.TextTemplating
{
	public class TextTemplatingFilePreprocessor : ISingleFileCustomTool
	{
		public IAsyncOperation Generate (IProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return new ThreadAsyncOperation (delegate {
				var host = new ProjectFileTemplatingHost (file);
				
				var dnp = file.Project as DotNetProject;
				if (dnp == null) {
					var msg = "Precompiled T4 templates are only supported in .NET projects";
					result.Errors.Add (new CompilerError (file.Name, -1, -1, null, msg));
					monitor.Log.WriteLine (msg);
					return;
				}
				
				var provider = dnp.LanguageBinding.GetCodeDomProvider ();
				if (provider == null) {
					var msg = "Precompiled T4 templates are only supported for .NET languages with CodeDOM providers";
					result.Errors.Add (new CompilerError (file.Name, -1, -1, null, msg));
					monitor.Log.WriteLine (msg);
					return;
				};
				
				var outputFile = file.FilePath.ChangeExtension (provider.FileExtension);
				var encoding = System.Text.Encoding.UTF8;
				string langauge;
				string[] references;
				string className = provider.CreateValidIdentifier (file.FilePath.FileNameWithoutExtension);
				
				string classNamespace = GetNamespaceHint (file, outputFile);
				LogicalSetData ("NamespaceHint", classNamespace, result.Errors);

				host.PreprocessTemplate (file.FilePath, className, classNamespace, outputFile, encoding, out langauge, out references);
				
				result.GeneratedFilePath = outputFile;
				result.Errors.AddRange (host.Errors);
				foreach (var err in host.Errors)
					monitor.Log.WriteLine (err.ToString ());
			}, result);
		}
		
		internal static string GetNamespaceHint (ProjectFile file, string outputFile)
		{
			string ns = file.CustomToolNamespace;
			if (string.IsNullOrEmpty (ns) && !string.IsNullOrEmpty (outputFile)) {
				var dnp = file.Project as DotNetProject;
					if (dnp != null)
						ns = dnp.GetDefaultNamespace (outputFile);
			}
			return ns;
		}
		
		static bool warningLogged;
		
		internal static void LogicalSetData (string name, object value,
			System.CodeDom.Compiler.CompilerErrorCollection errors)
		{
			if (warningLogged)
				return;
			
			//FIXME: CallContext.LogicalSetData not implemented in Mono
			try {
				System.Runtime.Remoting.Messaging.CallContext.LogicalSetData (name, value);
			} catch (NotImplementedException) {
				LoggingService.LogWarning ("T4: CallContext.LogicalSetData not implemented in this Mono version");
				warningLogged = true;
			}
		}
	}
}

