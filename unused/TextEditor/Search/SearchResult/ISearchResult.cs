//  ISearchResult.cs
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
using System.IO;
using System.Drawing;

using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui.Undo;

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
