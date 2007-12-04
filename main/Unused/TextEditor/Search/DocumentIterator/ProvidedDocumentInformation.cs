//  ProvidedDocumentInformation.cs
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
using System.Collections;

using MonoDevelop.Core.Gui;

namespace MonoDevelop.TextEditor.Document
{
	public class ProvidedDocumentInformation
	{
		IDocument document;
		ITextBufferStrategy textBuffer;
		string              fileName;
		int                 currentOffset;
		
		public ITextBufferStrategy TextBuffer {
			get {
				return textBuffer;
			}
			set {
				textBuffer = value;
			}
		}
		
		public string FileName {
			get {
				return fileName;
			}
		}
		
		public int CurrentOffset {
			get {
//				if (document != null) {
//					return document.Caret.Offset;
//				}
				return currentOffset;
			}
			set {
//				if (document != null) {
//					document.Caret.Offset = value;
//				} else {
					currentOffset = value;
//				}
			}
		}
		
		public int EndOffset {
			get {
				if (document != null) {
					return SearchReplaceUtilities.CalcCurrentOffset(document);
				}
				return currentOffset;
			}
		}
		
		public void Replace(int offset, int length, string pattern)
		{
			if (document != null) {
				document.Replace(offset, length, pattern);
			} else {
				textBuffer.Replace(offset, length, pattern);
			}
			
			if (offset <= CurrentOffset) {
				CurrentOffset = CurrentOffset - length + pattern.Length;
			}
		}
		
		public void SaveBuffer()
		{
			if (document != null) {
				
			} else {
				StreamWriter streamWriter = File.CreateText(this.fileName);
				streamWriter.Write(textBuffer.GetText(0, textBuffer.Length));
				streamWriter.Close();
			}
		}
		
		public IDocument CreateDocument()
		{
			if (document != null) {
				return document;
			}
			return new DocumentFactory().CreateFromFile(fileName);
		}		
		
		public ProvidedDocumentInformation(IDocument document, string fileName)
		{
			this.document   = document;
			this.textBuffer = document.TextBufferStrategy;
			this.fileName   = fileName;
//			this.currentOffset = document.Caret.Offset;
		}
		
		public ProvidedDocumentInformation(ITextBufferStrategy textBuffer, string fileName, int currentOffset)
		{
			this.textBuffer    = textBuffer;
			this.fileName      = fileName;
			this.currentOffset = currentOffset;
		}
	}
}
