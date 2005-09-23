// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using MonoDevelop.EditorBindings.Search;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// Builds a text iterator object.
	/// </summary>
	public interface ITextIteratorBuilder
	{
		ITextIterator BuildTextIterator(ProvidedDocumentInformation info);
	}
}
