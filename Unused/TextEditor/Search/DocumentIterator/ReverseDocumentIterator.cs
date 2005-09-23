// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// Represents a bi-directional iterator which could move froward/backward
	/// in a document queue. 
	/// </summary>
	public class ReverseDocumentIterator : IDocumentIterator 
	{
		IDocumentIterator documentIterator;
		
		public string CurrentFileName {
			get {
				return documentIterator.CurrentFileName;
			}
		}
		
		public ProvidedDocumentInformation Current {
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
