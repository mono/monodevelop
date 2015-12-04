//
// ResXFileCodeGenerator.cs
//
// Author:
//   Kenneth Skovhede <kenneth@hexad.dk>
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//   Bernhard Johannessen <bernhard@voytsje.com>
//   Matthew Diamond <matthewdiamond96@gmail.com>
//
// Copyright (C) 2013 Kenneth Skovhede
// Copyright (C) 2013 Xamarin Inc.
// Copyright (C) 2014 Bernhard Johannessen
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
using System.CodeDom;
using System.Linq;
using MonoDevelop.Core.Assemblies;
using System.Resources;
using System.Collections.Generic;
using System.Collections;

namespace MonoDevelop.Ide.CustomTools
{
	public class ResXFileCodeGenerator : ISingleFileCustomTool
	{
		public IAsyncOperation Generate (IProgressMonitor monitor, ProjectFile file, SingleFileCustomToolResult result)
		{
			return new ThreadAsyncOperation (GenerateFile (file, result, true), result);
		}

		public static Action GenerateFile (ProjectFile file, SingleFileCustomToolResult result, bool internalClass)
		{
			return delegate {
				var dnp = file.Project as DotNetProject;
				if (dnp == null) {
					var err = "ResXFileCodeGenerator can only be used with .NET projects";
					result.Errors.Add (new CompilerError (null, 0, 0, null, err));
					return;
				}

				var provider = dnp.LanguageBinding.GetCodeDomProvider ();
				if (provider == null) {
					const string err = "ResXFileCodeGenerator can only be used with languages that support CodeDOM";
					result.Errors.Add (new CompilerError (null, 0, 0, null, err));
					return;
				}

				var outputfile = file.FilePath.ChangeExtension (".Designer." + provider.FileExtension);
				var codeNamespace = CustomToolService.GetFileNamespace (file, outputfile);
				var name = provider.CreateValidIdentifier (file.FilePath.FileNameWithoutExtension);
				var resourcesNamespace = dnp.GetDefaultNamespace (outputfile);

				var rd = new Dictionary<object, object> ();
				using (var r = new ResXResourceReader (file.FilePath)) {
					r.BasePath = file.FilePath.ParentDirectory;
					foreach (DictionaryEntry e in r) {
						rd.Add (e.Key, e.Value);
					}
				}

				string[] unmatchable;
				var ccu = StronglyTypedResourceBuilder.Create (rd, name, codeNamespace, resourcesNamespace, provider, internalClass, out unmatchable);
				
				if (TargetsPcl2Framework (dnp)) {
					FixupPclTypeInfo (ccu);
				}

				foreach (var p in unmatchable) {
					var msg = string.Format ("Could not generate property for resource ID '{0}'", p);
					result.Errors.Add (new CompilerError (file.FilePath, 0, 0, null, msg));
				}

				using (var w = new StreamWriter (outputfile, false, Encoding.UTF8))
					provider.GenerateCodeFromCompileUnit (ccu, w, new CodeGeneratorOptions ());

				result.GeneratedFilePath = outputfile;
			};
		}

		static bool TargetsPcl2Framework (DotNetProject dnp)
		{
			if (dnp.TargetFramework.Id.Identifier != TargetFrameworkMoniker.ID_PORTABLE)
				return false;
			var asms = dnp.AssemblyContext.GetAssemblies (dnp.TargetFramework);
			return asms.Any (a => a.Package != null && a.Package.IsFrameworkPackage && a.Name == "System.Runtime");
		}

		//works with .NET 4.5.1 and Mono 3.4.0
		static void FixupPclTypeInfo (CodeCompileUnit ccu)
		{
			try {
				ccu.Namespaces [0].Imports.Add (new CodeNamespaceImport ("System.Reflection"));
				var assignment = ccu.Namespaces [0].Types [0]
					.Members.OfType<CodeMemberProperty> ().Single (t => t.Name == "ResourceManager")
					.GetStatements.OfType<CodeConditionStatement> ().Single ()
					.TrueStatements.OfType<CodeVariableDeclarationStatement> ().Single ();
				var initExpr = (CodeObjectCreateExpression) assignment.InitExpression;
				var typeofExpr = (CodePropertyReferenceExpression) initExpr.Parameters [1];
				typeofExpr.TargetObject = new CodeMethodInvokeExpression (typeofExpr.TargetObject, "GetTypeInfo");
			} catch (Exception ex) {
				LoggingService.LogWarning ("Failed to fixup resgen output for PCL", ex);
			}
		}
	}
}
