//
// FileFormatManager.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	public class FileFormatManager
	{
		List<FileFormat> fileFormats = new List<FileFormat> ();
		
		public void RegisterFileFormat (IFileFormat format, string id, string name)
		{
			FileFormat f = new FileFormat (format, id, name);
			fileFormats.Add (f);
		}
		
		public void UnregisterFileFormat (IFileFormat format)
		{
			foreach (FileFormat f in fileFormats) {
				if (f.Format == format) {
					fileFormats.Remove (f);
					return;
				}
			}
		}
		
		public FileFormat[] GetFileFormats (string fileName, Type expectedType)
		{
			List<FileFormat> list = new List<FileFormat> ();
			foreach (FileFormat f in fileFormats)
				if (f.Format.CanReadFile (fileName, expectedType))
					list.Add (f);
			return list.ToArray ();
		}
		
		public FileFormat[] GetFileFormatsForObject (object obj)
		{
			List<FileFormat> list = new List<FileFormat> ();
			foreach (FileFormat f in fileFormats)
				if (f.Format.CanWriteFile (obj))
					list.Add (f);
			return list.ToArray ();
		}
		
		public FileFormat[] GetAllFileFormats ()
		{
			List<FileFormat> list = new List<FileFormat> ();
			list.AddRange (fileFormats);
			return list.ToArray ();
		}
		
		public FileFormat GetFileFormat (string id)
		{
			foreach (FileFormat f in fileFormats)
				if (f.Id == id)
					return f;
			return null;
		}
	}
}
