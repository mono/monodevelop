//  DefaultFind.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Collections;
using System.Diagnostics;

using MonoDevelop.Core.Gui;
using MonoDevelop.EditorBindings.Search;

namespace MonoDevelop.TextEditor.Document
{
	public class DefaultFind : IFind
	{
		ISearchStrategy             searchStrategy      = null;
		IDocumentIterator           documentIterator    = null;
		ITextIterator               textIterator        = null;
		ITextIteratorBuilder        textIteratorBuilder = null;
		ProvidedDocumentInformation info = null;
		
		public ProvidedDocumentInformation CurrentDocumentInformation {
			get {
				return info;
			}
		}
		
		public ITextIteratorBuilder TextIteratorBuilder {
			get {
				return textIteratorBuilder;
			}
			set {
				textIteratorBuilder = value;
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
		
		ISearchResult CreateNamedSearchResult(SearchResult pos)
		{
			if (info == null || pos == null)
				return null;
			
			DefaultSearchResult res = new DefaultSearchResult (pos.Position, pos.Length);
			res.ProvidedDocumentInformation = info;
			return res;
		}
		
		public void Reset()
		{
			documentIterator.Reset();
			textIterator = null;
		}
		
		public void Replace(int offset, int length, string pattern)
		{
			if (CurrentDocumentInformation != null && TextIterator != null) {
				CurrentDocumentInformation.Replace(offset, length, pattern);
				CurrentDocumentInformation.SaveBuffer();
				TextIterator.InformReplace(offset, length, pattern.Length);
			}
		}
		
		public ISearchResult FindNext(SearchOptions options) 
		{
			// insanity check
			Debug.Assert(searchStrategy      != null);
			Debug.Assert(documentIterator    != null);
			Debug.Assert(textIteratorBuilder != null);
			Debug.Assert(options             != null);
			
			if (info != null && textIterator != null && documentIterator.CurrentFileName != null) {
				if (info.FileName != documentIterator.CurrentFileName) { // create new iterator, if document changed
					info         = documentIterator.Current;
					textIterator = textIteratorBuilder.BuildTextIterator(info);
				} else { // old document -> initialize iterator position to caret pos
					textIterator.Position = info.CurrentOffset;
				}
				
				ISearchResult result = CreateNamedSearchResult(searchStrategy.FindNext(textIterator, options));
				if (result != null) {
					info.CurrentOffset = textIterator.Position;
					return result;
				}
			}
			
			// not found or first start -> move forward to the next document
			if (documentIterator.MoveForward()) {
				info = documentIterator.Current;
				// document is valid for searching -> set iterator & fileName
				if (info != null && info.TextBuffer != null && info.EndOffset >= 0 && info.EndOffset < info.TextBuffer.Length) {
					textIterator = textIteratorBuilder.BuildTextIterator(info);
				} else {
					textIterator = null;
				}
				
				return FindNext(options);
			}
			return null;
		}
	}
}
