// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Xml;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Projects
{
	public interface ILanguageBinding
	{
		/// <returns>
		/// The language for this language binding.
		/// </returns>
		string Language { get; }
		
		/// <summary>
		/// Used by Comment and Uncomment operations and by Centaurus Addin.
		/// </summary>		
		string CommentTag { get; }
		
		/// <returns>
		/// True, if this language binding can compile >fileName<
		/// </returns>
		bool IsSourceCodeFile (string fileName);
		
		IParser Parser { get; }
		IRefactorer Refactorer { get; }
		
		string GetFileName (string baseName);
	}
}
