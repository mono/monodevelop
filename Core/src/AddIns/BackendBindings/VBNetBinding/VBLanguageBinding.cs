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

using MonoDevelop.Gui;
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Services;

namespace VBBinding
{
	public class VBLanguageBinding : ILanguageBinding
	{
		public const string LanguageName = "VBNet";
		
		VBBindingCompilerServices   compilerServices  = new VBBindingCompilerServices();
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public VBLanguageBinding ()
		{
			Runtime.ProjectService.DataContext.IncludeType (typeof(VBCompilerParameters));
		}
		
		public bool CanCompile(string fileName)
		{
			Debug.Assert(compilerServices != null);
			return compilerServices.CanCompile(fileName);
		}
		
		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			Debug.Assert(compilerServices != null);
			return compilerServices.Compile (projectFiles, references, configuration, monitor);
		}
		
		public void GenerateMakefile (Project project, Combine parentCombine)
		{
			compilerServices.GenerateMakefile (project, parentCombine);
		}
		
		public ICloneable CreateCompilationParameters (XmlElement projectOptions)
		{
			return new VBCompilerParameters ();
		}

		public string CommentTag
		{
			get { return "'"; }
		}
	}
}
