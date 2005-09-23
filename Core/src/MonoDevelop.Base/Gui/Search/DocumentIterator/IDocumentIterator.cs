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
	}
}
