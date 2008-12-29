//  VBLanguageBinding.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
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
using System.Collections;
using System.Diagnostics;
using System.Xml;
using Microsoft.VisualBasic;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace VBBinding
{
	public class VBLanguageBinding : IDotNetLanguageBinding
	{
		public const string LanguageName = "VBNet";
		
		VBBindingCompilerServices   compilerServices  = new VBBindingCompilerServices();
		VBCodeProvider provider;
		//TParser parser = new TParser ();
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
			Debug.Assert(compilerServices != null);
			return compilerServices.CanCompile(fileName);
		}
		
		public BuildResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			Debug.Assert(compilerServices != null);
			return compilerServices.Compile (projectFiles, references, configuration, monitor);
		}
		
		public ICloneable CreateCompilationParameters (XmlElement projectOptions)
		{
			return new VBCompilerParameters ();
		}

		public string CommentTag
		{
			get { return "'"; }
		}
		
		public CodeDomProvider GetCodeDomProvider ()
		{
			if (provider == null)
				provider = new ImprovedCodeDomProvider ();
			return provider;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".vb";
		}
		
		public IParser Parser {
			get { return null; }
		}
		
		public IRefactorer Refactorer {
			get { return null; }
		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion[] { ClrVersion.Net_2_0 };
		}
		
		class ImprovedCodeDomProvider : Microsoft.VisualBasic.VBCodeProvider
		{
			[Obsolete ("Use CodeDomProvider class")]
			public override ICodeGenerator CreateGenerator ()
			{
				return new VBBinding.VBCodeGenerator ();
			}
		}
	}
}
