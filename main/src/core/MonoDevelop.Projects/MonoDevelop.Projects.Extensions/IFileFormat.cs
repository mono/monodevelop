//
// IFileFormat.cs
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
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Extensions
{
	public interface IFileFormat
	{
		string GetValidFormatName (object obj, string fileName);
		
		bool CanReadFile (string file, Type expectedObjectType);
		bool CanWriteFile (object obj);
		
		void ConvertToFormat (object obj);
		
		void WriteFile (string file, object obj, IProgressMonitor monitor);
		object ReadFile (string file, Type expectedType, IProgressMonitor monitor);
		
		// Returns the list of files where the object is stored
		List<string> GetItemFiles (object obj);
		
		bool SupportsMixedFormats { get; }
	}
}
