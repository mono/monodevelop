// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;

using MonoDevelop.Core.Properties;
using MonoDevelop.Internal.Undo;

namespace MonoDevelop.TextEditor.Document
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
		
		/// <value>
		/// This property is set by the find object and need not to be
		/// set by the search strategies. All search results that are returned
		/// by the find object do have a ProvidedDocumentInformation.
		/// </value>
		ProvidedDocumentInformation ProvidedDocumentInformation {
			set;
		}
		
		/// <value>
		/// The offset of the pattern match
		/// </value>
		int Offset {
			get;
		}
		
		/// <value>
		/// The length of the pattern match.
		/// </value>
		int Length {
			get;
		}
		
		/// <remarks>
		/// This method creates a document for the file FileName. This method works
		/// only after the ProvidedDocumentInformation is set.
		/// </remarks>
		IDocument CreateDocument();
		
		/// <remarks>
		/// Replace operations must transform the replace pattern with this
		/// method.
		/// </remarks>
		string TransformReplacePattern(string pattern);
	}
}
