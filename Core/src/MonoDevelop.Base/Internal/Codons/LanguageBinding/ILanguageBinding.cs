// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;

using MonoDevelop.Internal.Templates;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Project
{
	/// <summary>
	/// The <code>ILanguageBinding</code> interface is the base interface
	/// of all language bindings avaiable.
	/// </summary>
	public interface ILanguageBinding
	{
		/// <returns>
		/// The language for this language binding.
		/// </returns>
		string Language {
			get;
		}
		
		/// <returns>
		/// True, if this language binding can compile >fileName<
		/// </returns>
		bool CanCompile(string fileName);
		
		ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor);
		
		void GenerateMakefile (Project project, Combine parentCombine);
		
		ICloneable CreateCompilationParameters (XmlElement projectOptions);

		/// <summary>
		/// Used by Comment and Uncomment operations and by Centaurus Addin.
		/// </summary>		
		string CommentTag { get; }
	}
}
