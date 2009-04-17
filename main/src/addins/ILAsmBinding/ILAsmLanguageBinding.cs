//  ILAsmLanguageBinding.cs
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
using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace ILAsmBinding
{
	public class ILAsmLanguageBinding : IDotNetLanguageBinding
	{
		public const string LanguageName = "ILAsm";
		
		ILAsmCompilerManager  compilerManager  = new ILAsmCompilerManager();
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public string ProjectStockIcon {
			get { 
				return "md-project";
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
		
		public void GenerateMakefile (Project project, SolutionFolder parentCombine)
		{
			// Not supported
		}
		
		public ConfigurationParameters CreateCompilationParameters (XmlElement projectOptions)
		{
			return new ILAsmCompilerParameters();
		}
	
		public ProjectParameters CreateProjectParameters (XmlElement projectOptions)
		{
			return null;
		}
		
		public string CommentTag
		{
			get { return "//"; }
		}
		
		public System.CodeDom.Compiler.CodeDomProvider GetCodeDomProvider ()
		{
			return null;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".il";
		}
			
		public IParser Parser {
			get { return null; }
		}
		
		public IRefactorer Refactorer {
			get { return null; }
		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion[] { ClrVersion.Net_1_1 };
		}
	}
}
