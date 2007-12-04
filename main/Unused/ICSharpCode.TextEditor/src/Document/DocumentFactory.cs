//  DocumentFactory.cs
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


namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This interface represents a container which holds a text sequence and
	/// all necessary information about it. It is used as the base for a text editor.
	/// </summary>
	public class DocumentFactory
	{
		/// <remarks>
		/// Creates a new <see cref="IDocument"/> object. Only create
		/// <see cref="IDocument"/> with this method.
		/// </remarks>
		public IDocument CreateDocument()
		{
			DefaultDocument doc = new DefaultDocument();
			doc.TextBufferStrategy    = new GapTextBufferStrategy();
			doc.FormattingStrategy    = new DefaultFormattingStrategy();
			doc.FoldingManager        = new FoldingManager(doc);
			doc.FoldingManager.FoldingStrategy       = new ParserFoldingStrategy();
			
			doc.LineManager          = new DefaultLineManager(doc, null);
			doc.BookmarkManager      = new BookmarkManager(doc.LineManager);
			return doc;
		}
		
		/// <summary>
		/// Creates a new document and loads the given file
		/// </summary>
		public IDocument CreateFromFile(string fileName)
		{
			IDocument document = CreateDocument();
			StreamReader stream = File.OpenText(fileName);
			document.TextContent = stream.ReadToEnd();
			stream.Close();
			return document;
		}
	}
}
