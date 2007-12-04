//  StringTextBufferStrategy.cs
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
using System.Diagnostics;
using MonoDevelop.TextEditor.Undo;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// Simple implementation of the ITextBuffer interface implemented using a
	/// string.
	/// Only for fall-back purposes.
	/// </summary>
	public class StringTextBufferStrategy : ITextBufferStrategy
	{
		string storedText = "";
		
		public int Length {
			get {
				return storedText.Length;
			}
		}
		
		public void Insert(int offset, string text)
		{
			if (text != null) {
				storedText = storedText.Insert(offset, text);
			}
		}
		
		public void Remove(int offset, int length)
		{
			storedText = storedText.Remove(offset, length);
		}
		
		public void Replace(int offset, int length, string text)
		{
			Remove(offset, length);
			Insert(offset, text);
		}
		
		public string GetText(int offset, int length)
		{
			if (length == 0) {
				return "";
			}
			return storedText.Substring(offset, Math.Min(length, storedText.Length - offset));
		}
		
		public char GetCharAt(int offset)
		{
			if (offset == Length) {
				return '\0';
			}
			return storedText[offset];
		}
		
		public void SetContent(string text)
		{
			storedText = text;
		}
		
		public StringTextBufferStrategy()
		{
		}
		
		StringTextBufferStrategy(string fileName)
		{
			StreamReader streamReader = File.OpenText(fileName);			
			SetContent(streamReader.ReadToEnd());
			streamReader.Close();
		}
		
		public static ITextBufferStrategy CreateTextBufferFromFile(string fileName)
		{
			if (!File.Exists(fileName)) {
				throw new System.IO.FileNotFoundException(fileName);
			}
			return new StringTextBufferStrategy(fileName);
		}
	}
}
