//  FileDocumentInformation.cs
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
using System.Text;
using System.IO;
using System.Collections;

namespace MonoDevelop.Ide.Gui.Search
{
	internal class FileDocumentInformation: IDocumentInformation
	{
		string fileName;
		int currentOffset;
		ForwardTextFileIterator iterator;
		
		public FileDocumentInformation(string fileName, int currentOffset)
		{
			this.fileName      = fileName;
			this.currentOffset = currentOffset;
		}
		
		public string FileName {
			get {
				return fileName;
			}
		}
		
		public int CurrentOffset {
			get {
				return currentOffset;
			}
			set {
				currentOffset = value;
			}
		}
		
		public int EndOffset {
			get {
				return currentOffset;
			}
		}
		
		public ITextIterator GetTextIterator ()
		{
			if (iterator == null)
				iterator = new ForwardTextFileIterator (this, fileName);
			return iterator;
		}
		
		public string GetLineTextAtOffset (int offset)
		{
			FileStream stream = new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.None);
			try {
				ExtendedStreamReader streamReader = new ExtendedStreamReader (new StreamReader (stream));
				streamReader.Position = offset;
				
				int lastPos;
				int b;
				do {
					lastPos = streamReader.Position;
					b = streamReader.ReadBack ();
				} while (b != -1 && b != 10 && b != 13);
				
				streamReader.Position = lastPos;
				StringBuilder sb = new StringBuilder ();
				b = streamReader.Read ();
				while (b != -1 && b != 10 && b != 13) {
					sb.Append ((char)b);
					b = streamReader.Read ();
				}
				return sb.ToString ();
			} finally {
				stream.Close ();
			}
		}
	}
}
