//  DefaultSearchResult.cs
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
	public class DefaultSearchResult : ISearchResult
	{
		ProvidedDocumentInformation providedDocumentInformation;
		int    offset;
		int    length;
		
		public string FileName {
			get {
				return providedDocumentInformation.FileName;
			}
		}
		
		public ProvidedDocumentInformation ProvidedDocumentInformation {
			set {
				providedDocumentInformation = value;
			}
		}
		
		public int Offset {
			get {
				return offset;
			}
		}
		
		public int Length {
			get {
				return length;
			}
		}
		
		public virtual string TransformReplacePattern(string pattern)
		{
			return pattern;
		}
		
		public IDocument CreateDocument()
		{
			return providedDocumentInformation.CreateDocument();
		}
		
		public DefaultSearchResult(int offset, int length)
		{
			this.offset   = offset;
			this.length   = length;
		}
		
		public override string ToString()
		{
			return String.Format("[DefaultLocation: FileName={0}, Offset={1}, Length={2}]",
			                     FileName,
			                     Offset,
			                     Length);
		}
	}
}
