//  DocumentEventArgs.cs
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

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This delegate is used for document events.
	/// </summary>
	public delegate void DocumentEventHandler(object sender, DocumentEventArgs e);
	
	/// <summary>
	/// This class contains more information on a document event
	/// </summary>
	public class DocumentEventArgs : EventArgs
	{
		IDocument document;
		int       offset;
		int       length;
		string    text;
		
		/// <returns>
		/// always a valid Document which is related to the Event.
		/// </returns>
		public IDocument Document {
			get {
				return document;
			}
		}
		
		/// <returns>
		/// -1 if no offset was specified for this event
		/// </returns>
		public int Offset {
			get {
				return offset;
			}
		}
		
		/// <returns>
		/// null if no text was specified for this event
		/// </returns>
		public string Text {
			get {
				return text;
			}
		}
		
		/// <returns>
		/// -1 if no length was specified for this event
		/// </returns>
		public int Length {
			get {
				return length;
			}
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document) : this(document, -1, -1, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset) : this(document, offset, -1, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset, int length) : this(document, offset, length, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset, int length, string text)
		{
			this.document = document;
			this.offset   = offset;
			this.length   = length;
			this.text     = text;
		}
	}
}
