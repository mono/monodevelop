// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;

namespace MonoDevelop.Gui.Search
{
	/// <summary>
	/// The basic interface for search operations in a document.
	/// </summary>
	internal interface IFind
	{
		/// <value>
		/// An object that implements a search algorithm.
		/// </value>
		ISearchStrategy SearchStrategy {
			get;
			set;
		}
		
		/// <value>
		/// Gets the current document information
		/// </value>
		IDocumentInformation CurrentDocumentInformation {
			get;
		}
		
		/// <value>
		/// An object that provides a document loading approach.
		/// </value>
		IDocumentIterator DocumentIterator {
			get;
			set;
		}
		
		/// <remarks>
		/// Does a replace in the current document information. This
		/// is the only method which should be used for doing replacements
		/// in a searched document.
		/// </remarks>
		void Replace (ISearchResult result, string pattern);
		
		/// <remarks>
		/// Finds next pattern.
		/// <remarks>
		/// <returns>
		/// null if the pattern wasn't found. If it returns null the current document
		/// information will be null too otherwise it will point to the document in which
		/// the search pattern was found.
		/// </returns>
		ISearchResult FindNext(SearchOptions options);
		
		ISearchResult FindPrevious(SearchOptions options);
		
		/// <remarks>
		/// Resets the find object to the original state.
		/// </remarks>
		void Reset();
		
		void Cancel ();
		
		int SearchedFileCount { get; }
		
		int MatchCount { get; }
	}
}
