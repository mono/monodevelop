// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using Microsoft.VisualBasic;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using VBBinding.Parser;

namespace VBBinding
{
	public class VBLanguageBinding : IDotNetLanguageBinding
	{
		public const string LanguageName = "VBNet";
		
		VBBindingCompilerServices   compilerServices  = new VBBindingCompilerServices();
		VBCodeProvider provider;
		TParser parser = new TParser ();
		
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
		
		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
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
				provider = new VBCodeProvider ();
			return provider;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".vb";
		}
		
		public IParser Parser {
			get { return parser; }
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
