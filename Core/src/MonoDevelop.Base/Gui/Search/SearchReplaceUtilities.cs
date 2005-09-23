// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.Gui.Search
{
	internal sealed class SearchReplaceUtilities
	{
		public static bool IsTextAreaSelected {
			get {
				return WorkbenchSingleton.Workbench.ActiveWorkbenchWindow != null && WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent is ITextBuffer;
			}
		}
		
		public static bool IsWordSeparator (char c)
		{
			return Char.IsWhiteSpace (c) || (Char.IsPunctuation (c) && c != '_');
		}
		
/*		public static bool IsWholeWordAt(SourceEditorBuffer document, int offset, int length)
		{
			return (offset - 1 < 0 || IsWordSeparator (document.GetCharAt(offset - 1))) &&
			       (offset + length + 1 >= document.Length || IsWordSeparator (document.GetCharAt(offset + length)));
		}
*/
		public static bool IsWholeWordAt (ITextIterator it, int length)
		{
			char c = it.GetCharRelative (-1);
			if (c != char.MinValue && !IsWordSeparator (c)) return false;
			
			c = it.GetCharRelative (length);
			return (c == char.MinValue || IsWordSeparator (c));
		}

		/*public static int CalcCurrentOffset(IDocument document) 
		{
//			TODO:
//			int endOffset = document.Caret.Offset % document.TextLength;
//			return endOffset;
			return 0;
		}*/
		
		public static ISearchStrategy CreateSearchStrategy(SearchStrategyType type)
		{
			switch (type) {
				case SearchStrategyType.None:
					return null;
				case SearchStrategyType.Normal:
					return new BruteForceSearchStrategy(); // new KMPSearchStrategy();
				case SearchStrategyType.RegEx:
					return new RegExSearchStrategy();
				case SearchStrategyType.Wildcard:
					return new WildcardSearchStrategy();
				default:
					throw new System.NotImplementedException("CreateSearchStrategy for type " + type);
			}
		}
		
		
		public static IDocumentIterator CreateDocumentIterator(DocumentIteratorType type)
		{
			switch (type) {
				case DocumentIteratorType.None:
					return null;
				case DocumentIteratorType.CurrentDocument:
					return new CurrentDocumentIterator();
				case DocumentIteratorType.Directory:
					return new DirectoryDocumentIterator(SearchReplaceInFilesManager.SearchOptions.SearchDirectory, 
					                                     SearchReplaceInFilesManager.SearchOptions.FileMask, 
					                                     SearchReplaceInFilesManager.SearchOptions.SearchSubdirectories);
				case DocumentIteratorType.AllOpenFiles:
					return new AllOpenDocumentIterator();
				case DocumentIteratorType.WholeCombine:
					return new WholeProjectDocumentIterator();
				default:
					throw new System.NotImplementedException("CreateDocumentIterator for type " + type);
			}
		}
	}
	
}
