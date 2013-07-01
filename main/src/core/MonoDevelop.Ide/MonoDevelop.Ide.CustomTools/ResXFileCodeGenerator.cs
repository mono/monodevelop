//
// ResXFileCodeGenerator.cs
//
// Author:
//   Kenneth Skovhede <kenneth@hexad.dk>
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2013 Kenneth Skovhede
// Copyright (C) 2013 Xamarin Inc.
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
using System.IO;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.CustomTools;
using System.Resources.Tools;
using System.CodeDom.Compiler;

namespace MonoDevelop.Ide.CustomTools
{
	public class ResXFileCodeGenerator : ISingleFileCustomTool
	{
		public IAsyncOperation Generate (IProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return new ThreadAsyncOperation (delegate {
				var dnp = file.Project as DotNetProject;
				if (dnp == null) {
					var err = "ResXFileCodeGenerator can only be used with .NET projects";
					result.Errors.Add (new CompilerError (null, 0, 0, null, err));
					return;
				}

				var provider = dnp.LanguageBinding.GetCodeDomProvider ();
				if (provider == null) {
					var err = "ResXFileCodeGenerator can only be used with languages that support CodeDOM";
					result.Errors.Add (new CompilerError (null, 0, 0, null, err));
					return;
				}

				var outputfile = file.FilePath.ChangeExtension (".Designer." + provider.FileExtension);
				var ns = CustomToolService.GetFileNamespace (file, outputfile);
				var cn = provider.CreateValidIdentifier (file.FilePath.FileNameWithoutExtension);

				string[] unmatchable;
				var ccu = StronglyTypedResourceBuilder.Create (file.FilePath, cn, ns, provider, true, out unmatchable);

				foreach (var p in unmatchable) {
					var msg = string.Format ("Could not generate property for resource ID '{0}'", p);
					result.Errors.Add (new CompilerError (file.FilePath, 0, 0, null, msg));
				}

				using (var w = new StreamWriter (outputfile, false, Encoding.UTF8))
					provider.GenerateCodeFromCompileUnit (ccu, w, new CodeGeneratorOptions ());

				result.GeneratedFilePath = outputfile;
			}, result);
		}
	}
}
