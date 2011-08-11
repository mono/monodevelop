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
		// Returns a valid file name for the provided object and file (e.g. it might change
		// the extension to .csproj for the VS2005 format)
		FilePath GetValidFormatName (object obj, FilePath fileName);

		// Returns true if this file format can read the provided file to load an
		// object of the specified type
		bool CanReadFile (FilePath file, Type expectedObjectType);
		
		// Returns true if this file format can write the provided object
		bool CanWriteFile (object obj);
		
		// Makes the required changes in the object to support this file format.
		// It usually means setting the ISolutionItemHandler of the item.
		void ConvertToFormat (object obj);

		void WriteFile (FilePath file, object obj, IProgressMonitor monitor);
		object ReadFile (FilePath file, Type expectedType, IProgressMonitor monitor);
		
		// Returns the list of files where the object is stored
		List<FilePath> GetItemFiles (object obj);
		
		// Return true if the file formats supports mixing items of different formats.
		// For example, solutions using the MonoDevelop 1.0 file format can contain
		// projects stored using the MSBuild file format.
		bool SupportsMixedFormats { get; }

		// Returns a list of warnings to show to the user about compatibility issues
		// that may arise when exporting the object to this format.
		IEnumerable<string> GetCompatibilityWarnings (object obj);
		
		bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework);
	}
}