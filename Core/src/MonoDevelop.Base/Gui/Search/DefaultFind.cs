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
	internal class DefaultFind : IFind
	{
		ISearchStrategy searchStrategy;
		IDocumentIterator documentIterator;
		ITextIterator textIterator;
		IDocumentInformation info;
		bool cancelled;
		int searchedFiles;
		int matches;
		int lastResultPos;
		SearchMap reverseSearchMap;
		bool lastWasReverse;
		
		public IDocumentInformation CurrentDocumentInformation {
			get {
				return info;
			}
		}
		
		public ITextIterator TextIterator {
			get {
				return textIterator;
			}
		}
		
		public ISearchStrategy SearchStrategy {
			get {
				return searchStrategy;
			}
			set {
				searchStrategy = value;
			}
		}
		
		public IDocumentIterator DocumentIterator {
			get {
				return documentIterator;
			}
			set {
				documentIterator = value;
			}
		}
		
		public int SearchedFileCount {
			get { return searchedFiles; }
		}
		
		public int MatchCount {
			get { return matches; }
		}
		
		public void Reset()
		{
			documentIterator.Reset();
			textIterator = null;
			reverseSearchMap = null;
			cancelled = false;
			searchedFiles = 0;
			matches = 0;
			lastResultPos = -1;
		}
		
		public void Replace (ISearchResult result, string pattern)
		{
			if (CurrentDocumentInformation != null && TextIterator != null) {
				TextIterator.Position = result.Position;
				TextIterator.Replace (result.Length, pattern);
			}
		}
		
		public ISearchResult FindNext(SearchOptions options) 
		{
			return Find (options, false);
		}
		
		public ISearchResult FindPrevious (SearchOptions options) 
		{
			return Find (options, true);
		}
		
		public ISearchResult Find (SearchOptions options, bool reverse)
		{
			// insanity check
			Debug.Assert(searchStrategy      != null);
			Debug.Assert(documentIterator    != null);
			Debug.Assert(options             != null);
			
			while (!cancelled)
			{
				if (info != null && textIterator != null && documentIterator.CurrentFileName != null) {
					if (info.FileName != documentIterator.CurrentFileName || lastWasReverse != reverse) {
						// create new iterator, if document changed or search direction has changed.
						info = documentIterator.Current;
						textIterator = info.GetTextIterator ();
						reverseSearchMap = null;
						lastResultPos = -1;
						if (reverse)
							textIterator.MoveToEnd ();
					} 

					ISearchResult result;
					if (!reverse)
						result = searchStrategy.FindNext (textIterator, options, false);
					else {
						if (searchStrategy.SupportsReverseSearch (textIterator, options)) {
							result = searchStrategy.FindNext (textIterator, options, true);
						}
						else {
							if (reverseSearchMap == null) {
								reverseSearchMap = new SearchMap ();
								reverseSearchMap.Build (searchStrategy, textIterator, options);
							}
							if (lastResultPos == -1)
								lastResultPos = textIterator.Position;
							result = reverseSearchMap.GetPreviousMatch (lastResultPos);
							if (result != null)
								textIterator.Position = result.Position;
						}
					}
						
					if (result != null) {
						matches++;
						lastResultPos = result.Position;
						lastWasReverse = reverse;
						return result;
					}
				}
				
				if (textIterator != null) textIterator.Close ();
					
				// not found or first start -> move forward to the next document
				bool more = !reverse ? documentIterator.MoveForward () : documentIterator.MoveBackward ();
				if (more) {
					searchedFiles++;
					info = documentIterator.Current;
					textIterator = info.GetTextIterator ();
					reverseSearchMap = null;
					lastResultPos = -1;
					if (reverse)
						textIterator.MoveToEnd ();
				}
				else
					cancelled = true;

				lastWasReverse = reverse;
			}
			
			cancelled = false;
			return null;
		}
		
		public void Cancel ()
		{
			cancelled = true;
		}
	}
	
	class SearchMap
	{
		ArrayList matches = new ArrayList ();

		public void Build (ISearchStrategy strategy, ITextIterator it, SearchOptions options)
		{
			int startPos = it.Position;
			it.Reset ();

			ISearchResult res = strategy.FindNext (it, options, false);
			while (res != null) {
				matches.Add (res);
				res = strategy.FindNext (it, options, false);
			}
			it.Position = startPos;
		}
		
		public ISearchResult GetPreviousMatch (int pos)
		{
			if (matches.Count == 0) return null;
			
			for (int n = matches.Count - 1; n >= 0; n--) {
				ISearchResult m = (ISearchResult) matches [n];
				if (m.Position < pos)
					return m;
			}
			
			return null;
		}
	}
}
