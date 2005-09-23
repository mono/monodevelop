// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Gui.Search
{
	/// <summary>
	/// This interface describes the result a search strategy must
	/// return with a call to find next.
	/// </summary>
	public interface ISearchResult
	{
		/// <value>
		/// Returns the file name of the search result. This
		/// value is null till the ProvidedDocumentInformation 
		/// property is set.
		/// </value>
		string FileName {
			get;
		}
		
		IDocumentInformation DocumentInformation {
			get;
		}
		
		/// <value>
		/// The position of the pattern match in the text iterator
		/// </value>
		int Position {
			get;
		}
		
		/// <value>
		/// The offset of the pattern match in the document
		/// </value>
		int DocumentOffset {
			get;
		}
		
		int Line { get; }
		
		int Column {get; }
		
		/// <value>
		/// The length of the pattern match.
		/// </value>
		int Length {
			get;
		}
		
		/// <remarks>
		/// Replace operations must transform the replace pattern with this
		/// method.
		/// </remarks>
		string TransformReplacePattern(string pattern);
	}
}
