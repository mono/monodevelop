// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This interface is used for the folding capabilities
	/// of the textarea.
	/// </summary>
	public interface IFoldingStrategy
	{
		/// <remarks>
		/// Calculates the fold level of a specific line.
		/// </remarks>
		ArrayList GenerateFoldMarkers(IDocument document, string fileName, object parseInformation);
	}
}
