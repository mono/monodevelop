// FileFormat.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class FileFormat
	{
		string id;
		string name;
		IFileFormat format;
		
		public string Id {
			get {
				return id;
			}
		}

		public string Name {
			get {
				return name;
			}
		}
		
		public bool CanDefault { get; private set; }
		
		public string GetValidFileName (object obj, string fileName)
		{
			return format.GetValidFormatName (obj, fileName);
		}
		
		public IEnumerable<string> GetCompatibilityWarnings (object obj)
		{
			IWorkspaceFileObject wfo = obj as IWorkspaceFileObject;
			if (wfo != null && !wfo.SupportsFormat (this)) {
				return new string[] {GettextCatalog.GetString ("The project '{0}' is not supported by {1}", wfo.Name, Name) };
			}
			IEnumerable<string> res = format.GetCompatibilityWarnings (obj);
			return res ?? new string [0];
		}
		
		public bool CanWrite (object obj)
		{
			IWorkspaceFileObject wfo = obj as IWorkspaceFileObject;
			if (wfo != null && !wfo.SupportsFormat (this))
				return false;
			return format.CanWriteFile (obj);
		}
		
		public bool SupportsMixedFormats {
			get { return format.SupportsMixedFormats; }
		}
		
		public bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return format.SupportsFramework (framework);
		}
		
		internal IFileFormat Format {
			get { return format; }
		}
		
		internal FileFormat (IFileFormat format, string id, string name)
			: this (format, id, name, false)
		{
		}
		
		internal FileFormat (IFileFormat format, string id, string name, bool canDefault)
		{
			this.id = id;
			this.name = name ?? id;
			this.format = format;
			this.CanDefault = canDefault;
		}
	}
}
