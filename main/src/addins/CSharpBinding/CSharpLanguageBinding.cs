//  CSharpLanguageBinding.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
		public const string LanguageName = "C#";
		
		CSharpBindingCompilerManager   compilerManager  = new CSharpBindingCompilerManager();
		CSharpCodeProvider provider;
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.CanCompile(fileName);
		}
		
		public BuildResult Compile (ProjectItemCollection projectItems, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.Compile (projectItems, configuration, monitor);
		}
		
		public ICloneable CreateCompilationParameters (XmlElement projectOptions)
		{
			CSharpCompilerParameters pars = new CSharpCompilerParameters ();
			if (projectOptions != null) {
				string debugAtt = projectOptions.GetAttribute ("DefineDebug");
				if (string.Compare ("True", debugAtt, true) == 0)
					pars.DefineSymbols = "DEBUG";
			}
			return pars;
		}
		
		public string CommentTag
		{
			get { return "//"; }
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
		/*
		TParser parser = new TParser ();
			
		public IParser Parser {
			get { return parser; }
		}
		
		public IRefactorer Refactorer {
			get { return refactorer; }
		}*/
		public IParser Parser {
			get { return null; }
		}
		
		CSharpRefactorer refactorer = new CSharpRefactorer ();
		public IRefactorer Refactorer {
			get { return refactorer; }
		}
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion[] { ClrVersion.Net_1_1, ClrVersion.Net_2_0, ClrVersion.Clr_2_1 };
		}
	}
}
