// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.Collections;
using MonoDevelop.EditorBindings.Search;

namespace MonoDevelop.TextEditor.Document
{
	public class ForwardTextIteratorBuilder : ITextIteratorBuilder
	{
		public ITextIterator BuildTextIterator(ProvidedDocumentInformation info)
		{
			Debug.Assert(info != null);
			return new ForwardTextIterator(info.TextBuffer, info.EndOffset);
		}
	}
}
