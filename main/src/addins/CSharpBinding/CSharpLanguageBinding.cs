// 
// CSharpBindingCompilerManager.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.CodeDom.Compiler;
using System.Threading;
using Microsoft.CSharp;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core;

using CSharpBinding.Parser;

namespace CSharpBinding
{
	public class CSharpLanguageBinding : IDotNetLanguageBinding
	{
		CSharpCodeProvider provider;
		
		public string Language {
			get {
				return "C#";
			}
		}
		
		public string ProjectStockIcon {
			get { 
				return "md-csharp-project";
			}
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
			return string.Compare (Path.GetExtension (fileName), ".cs", true) == 0;
		}
		
		public BuildResult Compile (ProjectItemCollection projectItems, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			return CSharpBindingCompilerManager.Compile (projectItems, configuration, monitor);
		}
		
		public ConfigurationParameters CreateCompilationParameters (XmlElement projectOptions)
		{
			CSharpCompilerParameters pars = new CSharpCompilerParameters ();
			if (projectOptions != null) {
				string debugAtt = projectOptions.GetAttribute ("DefineDebug");
				if (string.Compare ("True", debugAtt, true) == 0)
					pars.DefineSymbols = "DEBUG";
			}
			return pars;
		}
	
		public ProjectParameters CreateProjectParameters (XmlElement projectOptions)
		{
			return new CSharpProjectParameters ();
		}
		
		public string CommentTag {
			get { 
				return "//"; 
			}
		}
		
		public CodeDomProvider GetCodeDomProvider ()
		{
			if (provider == null)
				provider = new CSharpEnhancedCodeProvider ();
			return provider;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".cs";
		}
		
		public IParser Parser {
			get { 
				return null; 
			}
		}
		
		CSharpRefactorer refactorer = new CSharpRefactorer ();
		public IRefactorer Refactorer {
			get { 
				return refactorer; 
			}
		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion[] { 
				ClrVersion.Net_1_1, 
				ClrVersion.Net_2_0, 
				ClrVersion.Clr_2_1
			};
		}
	}
}
