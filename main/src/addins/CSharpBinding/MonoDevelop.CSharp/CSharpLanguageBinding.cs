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
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.CodeDom.Compiler;
using System.Threading;
using Microsoft.CSharp;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;

using MonoDevelop.CSharp.Parser;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Project;

namespace MonoDevelop.CSharp
{
	public class CSharpLanguageBinding : IDotNetLanguageBinding
	{
		CSharpCodeProvider provider;
		
		// Keep the platforms combo of CodeGenerationPanelWidget in sync with this list
		public static IList<string> SupportedPlatforms = new string[] { "anycpu", "x86", "x64", "itanium" };
	
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
		
		
		public bool IsSourceCodeFile (FilePath fileName)
		{
			return StringComparer.OrdinalIgnoreCase.Equals (Path.GetExtension (fileName), ".cs");
		}
		
		public BuildResult Compile (ProjectItemCollection projectItems, DotNetProjectConfiguration configuration, ConfigurationSelector configSelector, IProgressMonitor monitor)
		{
			return CSharpBindingCompilerManager.Compile (projectItems, configuration, configSelector, monitor);
		}
		
		public ConfigurationParameters CreateCompilationParameters (XmlElement projectOptions)
		{
			CSharpCompilerParameters pars = new CSharpCompilerParameters ();
			if (projectOptions != null) {
				string platform = projectOptions.GetAttribute ("Platform");
				if (SupportedPlatforms.Contains (platform))
					pars.PlatformTarget = platform;
				string debugAtt = projectOptions.GetAttribute ("DefineDebug");
				if (string.Compare ("True", debugAtt, StringComparison.OrdinalIgnoreCase) == 0)
					pars.AddDefineSymbol ("DEBUG");
				string releaseAtt = projectOptions.GetAttribute ("Release");
				if (string.Compare ("True", releaseAtt, StringComparison.OrdinalIgnoreCase) == 0)
					pars.Optimize = true;
			}
			return pars;
		}
	
		public ProjectParameters CreateProjectParameters (XmlElement projectOptions)
		{
			return new CSharpProjectParameters ();
		}
		
		public string SingleLineCommentTag { get { return "//"; } }
		public string BlockCommentStartTag { get { return "/*"; } }
		public string BlockCommentEndTag { get { return "*/"; } }
		
		public CodeDomProvider GetCodeDomProvider ()
		{
			if (provider == null)
				provider = new CSharpEnhancedCodeProvider ();
			return provider;
		}
		
		public FilePath GetFileName (FilePath baseName)
		{
			return baseName + ".cs";
		}
		
//		public IParser Parser {
//			get { 
//				return null; 
//			}
//		}
//		
//		CSharpRefactorer refactorer = new CSharpRefactorer ();
//		public IRefactorer Refactorer {
//			get { 
//				return refactorer; 
//			}
//		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion[] { 
				ClrVersion.Net_1_1, 
				ClrVersion.Net_2_0, 
				ClrVersion.Clr_2_1,
				ClrVersion.Net_4_0,
				ClrVersion.Net_4_5
			};
		}
	}
	
	internal static class Counters
	{
		public static Counter ResolveTime = InstrumentationService.CreateCounter ("Resolve Time", "Timing");
	}
}
