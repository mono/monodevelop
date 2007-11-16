//  ReverseDocumentIterator.cs
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

namespace MonoDevelop.Ide.Gui.Search
{
	/// <summary>
	/// Represents a bi-directional iterator which could move froward/backward
	/// in a document queue. 
	/// </summary>
	internal class ReverseDocumentIterator : IDocumentIterator 
	{
		IDocumentIterator documentIterator;
		
		public string CurrentFileName {
			get {
				return documentIterator.CurrentFileName;
			}
		}
		
		public IDocumentInformation Current {
			get {
				return documentIterator.Current;
			}
		}
		
		public ReverseDocumentIterator(IDocumentIterator documentIterator)
		{
			this.documentIterator = documentIterator;
		}
		
		public bool MoveForward()
		{
			return documentIterator.MoveBackward();
		}
		
		public bool MoveBackward()
		{
			return documentIterator.MoveBackward();
		}
		
		public void Reset()
		{
			documentIterator.Reset();
		}
	}
}
