//  IDocumentIterator.cs
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
	/// in a document queue. Note that after move forward is called
	/// move backward needn't to function correctly either move forward or move
	/// backward is called but they're not mixed. After a reset the move operation
	/// can be switched.
	/// </summary>
	internal interface IDocumentIterator 
	{
		/// <value>
		/// Returns the current ProvidedDocumentInformation. This method
		/// usually creates a new ProvidedDocumentInformation object which can
		/// be time consuming
		/// </value>
		IDocumentInformation Current {
			get;
		}
		
		/// <value>
		/// Returns the file name of the current provided document information. This
		/// property usually is not time consuming
		/// </value>
		string CurrentFileName {
			get;
		}
		
		/// <remarks>
		/// Moves the iterator one document forward.
		/// </remarks>
		bool MoveForward();
		
		/// <remarks>
		/// Moves the iterator one document backward.
		/// </remarks>
		bool MoveBackward();
		
		/// <remarks>
		/// Resets the iterator to the start position.
		/// </remarks>
		void Reset();
		
		string GetSearchDescription (string pattern);
		string GetReplaceDescription (string pattern);
	}
}
