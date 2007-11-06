//  IFind.cs
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
	/// <summary>
	/// The basic interface for search operations in a document.
	/// </summary>
	public interface IFind
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
		ProvidedDocumentInformation CurrentDocumentInformation {
			get;
		}
		
		/// <value>
		/// An object that provides a document loading approach.
		/// </value>
		IDocumentIterator DocumentIterator {
			get;
			set;
		}
		
		/// <value>
		/// The text iterator builder which builds ITextIterator objects
		/// for the find.
		/// </value>
		ITextIteratorBuilder TextIteratorBuilder {
			get;
			set;
		}
		
		/// <remarks>
		/// Does a replace in the current document information. This
		/// is the only method which should be used for doing replacements
		/// in a searched document.
		/// </remarks>
		void Replace(int offset, int length, string pattern);
		
		/// <remarks>
		/// Finds next pattern.
		/// <remarks>
		/// <returns>
		/// null if the pattern wasn't found. If it returns null the current document
		/// information will be null too otherwise it will point to the document in which
		/// the search pattern was found.
		/// </returns>
		ISearchResult FindNext(SearchOptions options);
		
		/// <remarks>
		/// Resets the find object to the original state.
		/// </remarks>
		void Reset();
	}
}
