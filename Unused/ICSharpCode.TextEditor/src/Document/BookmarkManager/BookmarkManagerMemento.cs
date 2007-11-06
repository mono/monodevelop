//  BookmarkManagerMemento.cs
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
using System.Xml;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This class is used for storing the state of a bookmark manager 
	/// </summary>
	public class BookmarkManagerMemento
	{
		ArrayList bookmarks = new ArrayList();
		
		/// <value>
		/// Contains all bookmarks as int values
		/// </value>
		public ArrayList Bookmarks {
			get {
				return bookmarks;
			}
			set {
				bookmarks = value;
			}
		}
		
		/// <summary>
		/// Validates all bookmarks if they're in range of the document.
		/// (removing all bookmarks &lt; 0 and bookmarks &gt; max. line number
		/// </summary>
		public void CheckMemento(IDocument document)
		{
			for (int i = 0; i < bookmarks.Count; ++i) {
				int mark = (int)bookmarks[i];
				if (mark < 0 || mark >= document.TotalNumberOfLines) {
					bookmarks.RemoveAt(i);
					--i;
				}
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="BookmarkManagerMemento"/>
		/// </summary>
		public BookmarkManagerMemento()
		{
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="BookmarkManagerMemento"/>
		/// </summary>
		public BookmarkManagerMemento(XmlElement element)
		{
			foreach (XmlElement el in element.ChildNodes) {
				bookmarks.Add(Int32.Parse(el.Attributes["line"].InnerText));
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="BookmarkManagerMemento"/>
		/// </summary>
		public BookmarkManagerMemento(ArrayList bookmarks)
		{
			this.bookmarks = bookmarks;
		}
		
		/// <summary>
		/// Converts a xml element to a <see cref="BookmarkManagerMemento"/> object
		/// </summary>
		public object FromXmlElement(XmlElement element)
		{
			return new BookmarkManagerMemento(element);
		}
		
		/// <summary>
		/// Converts this <see cref="BookmarkManagerMemento"/> to a xml element
		/// </summary>
		public XmlElement ToXmlElement(XmlDocument doc)
		{
			XmlElement bookmarknode  = doc.CreateElement("Bookmarks");
			
			foreach (int line in bookmarks) {
				XmlElement markNode = doc.CreateElement("Mark");
				
				XmlAttribute lineAttr = doc.CreateAttribute("line");
				lineAttr.InnerText = line.ToString();
				markNode.Attributes.Append(lineAttr);
						
				bookmarknode.AppendChild(markNode);
			}
			
			return bookmarknode;
		}
	}
}
