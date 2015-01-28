// 
// TextTemplatingFileGenerator.cs
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
using MonoDevelop.Ide.CustomTools;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.TextTemplating
{
	public class TextTemplatingFileGenerator : ISingleFileCustomTool
	{
		public IAsyncOperation Generate (IProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return new ThreadAsyncOperation (delegate {
				using (var host = new ProjectFileTemplatingHost (file, IdeApp.Workspace.ActiveConfiguration)) {
					host.AddMonoDevelopHostImport ();
					var defaultOutputName = file.FilePath.ChangeExtension (".cs"); //cs extension for VS compat
					
					string ns = CustomToolService.GetFileNamespace (file, defaultOutputName);
					LogicalSetData ("NamespaceHint", ns);
					
					host.ProcessTemplate (file.FilePath, defaultOutputName);
					result.GeneratedFilePath = host.OutputFile;
					result.Errors.AddRange (host.Errors);
					
					foreach (var err in host.Errors)
						monitor.Log.WriteLine (err);
				}
			}, result);
		}

		static bool warningLogged;

		internal static void LogicalSetData (string name, object value)
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

