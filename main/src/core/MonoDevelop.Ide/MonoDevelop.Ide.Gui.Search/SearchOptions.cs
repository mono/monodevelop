// SearchOptions.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Search
{
	public enum SearchStrategyType {
		None,
		Normal,
		RegEx,
		Wildcard
	}
	
	public enum DocumentIteratorType {
		None,
		Directory,
		AllOpenFiles,
		WholeCombine,
		CurrentProject
	}
		
	public class SearchOptions
	{
		string propertyName;
		
		public bool IgnoreCase {
			get;
			set;
		}
		
		public bool SearchWholeWordOnly {
			get;
			set;
		}
		
		public string SearchPattern {
			get;
			set;
		}
		
		public string ReplacePattern {
			get;
			set;
		}
		
		public DocumentIteratorType DocumentIteratorType {
			get;
			set;
		}
		
		public SearchStrategyType SearchStrategyType {
			get;
			set;
		}
		
		public string FileMask {
			get;
			set;
		}

		public string SearchDirectory {
			get;
			set;
		}
		
		public bool SearchSubdirectories {
			get;
			set;
		}

		protected SearchOptions ()
		{
		}

		internal ISearchStrategy CreateStrategy ()
		{
			switch (SearchStrategyType) {
				case SearchStrategyType.Normal:
					return new BruteForceSearchStrategy();
				case SearchStrategyType.RegEx:
					return new RegExSearchStrategy();
				case SearchStrategyType.Wildcard:
					return new WildcardSearchStrategy();
			}
			return null;
		}
		
		internal IDocumentIterator CreateIterator ()
		{
			switch (DocumentIteratorType) {
				case DocumentIteratorType.Directory:
					SearchOptions options = SearchReplaceInFilesManager.GetDefaultSearchOptions ();
					return new DirectoryDocumentIterator (options.SearchDirectory, options.FileMask, options.SearchSubdirectories);
				case DocumentIteratorType.AllOpenFiles:
					return new AllOpenDocumentIterator();
				case DocumentIteratorType.WholeCombine:
					return new WholeCombineDocumentIterator();
				case DocumentIteratorType.CurrentProject:
					return new CurrentProjectDocumentIterator();
			}
			return null;
		}
		
		internal void Store ()
		{
			Properties properties = (Properties)PropertyService.Get (propertyName, new Properties ());
			properties.Set ("IgnoreCase", this.IgnoreCase);
			properties.Set ("SearchWholeWordOnly", this.SearchWholeWordOnly);
			properties.Set ("SearchPattern", this.SearchPattern);
			properties.Set ("ReplacePattern", this.ReplacePattern);
			properties.Set ("DocumentIteratorType", this.DocumentIteratorType);
			properties.Set ("SearchStrategyType", this.SearchStrategyType);
			properties.Set ("FileMask", this.FileMask);
			properties.Set ("SearchDirectory", this.SearchDirectory);
			properties.Set ("SearchSubdirectories", this.SearchSubdirectories);
		}

		internal static SearchOptions CreateOptions (string propertyName)
		{
			SearchOptions result = new SearchOptions ();
			Properties properties = (Properties)PropertyService.Get (propertyName, new Properties ());
			result.propertyName = propertyName;
			result.IgnoreCase = properties.Get ("IgnoreCase", false);
			result.SearchWholeWordOnly = properties.Get ("SearchWholeWordOnly", false);
			result.SearchPattern = properties.Get ("SearchPattern", String.Empty);
			result.ReplacePattern = properties.Get ("ReplacePattern", String.Empty);
			result.DocumentIteratorType = properties.Get ("DocumentIteratorType", DocumentIteratorType.WholeCombine);
			result.SearchStrategyType = properties.Get ("SearchStrategyType", SearchStrategyType.Normal);
			result.FileMask = properties.Get ("FileMask", String.Empty);
			result.SearchDirectory = properties.Get ("SearchDirectory", String.Empty);
			result.SearchSubdirectories = properties.Get ("SearchSubdirectories", true);
			return result;
		}
		
	}
}
