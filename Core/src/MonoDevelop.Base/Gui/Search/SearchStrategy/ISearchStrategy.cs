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
	/// This interface is the basic interface which all 
	/// search algorithms must implement.
	/// </summary>
	internal interface ISearchStrategy
	{
		/// <remarks>
		/// Only with a call to this method the search strategy must
		/// update their pattern information. This method will be called 
		/// before the FindNext function.
		/// </remarks>
		void CompilePattern(SearchOptions options);
		
		/// <remarks>
		/// The find next method should search the next occurrence of the 
		/// compiled pattern in the text using the textIterator and options.
		/// </remarks>
		ISearchResult FindNext (ITextIterator textIterator, SearchOptions options, bool reverseSearch);
		
		// Returns true if this strategy can do reverse searchs with the given parameters
		bool SupportsReverseSearch (ITextIterator textIterator, SearchOptions options);
	}
}
