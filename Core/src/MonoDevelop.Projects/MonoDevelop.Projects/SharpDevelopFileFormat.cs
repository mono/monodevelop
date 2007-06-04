//
// SharpDevelopFileFormat.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Projects
{
	internal class SharpDevelopFileFormat: IFileFormat
	{
		public string Name {
			get { return "SharpDevelop 1.0 Project Model"; }
		}
		
		public string GetValidFormatName (object obj, string fileName)
		{
			if (obj is Combine)
				return Path.ChangeExtension (fileName, ".cmbx");
			else
				return Path.ChangeExtension (fileName, ".prjx");
		}
		
		public bool CanReadFile (string file)
		{
			return string.Compare (Path.GetExtension (file), ".cmbx", true) == 0 ||
			       string.Compare (Path.GetExtension (file), ".prjx", true) == 0;
		}
		
		public bool CanWriteFile (object obj)
		{
			return (obj is Combine) || (obj is Project);
		}
		
		public StringCollection GetExportFiles (object obj)
		{
			return null;
		}
		
		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
/*			if (node is Combine)
				CmbxFileFormat.WriteFile (file, node, monitor);
			else
				PrjxFileFormat.WriteFile (file, node, monitor);*/
		}
		
		public object ReadFile (string file, IProgressMonitor monitor)
		{
/*			if (string.Compare (Path.GetExtension (file), ".cmbx", true) == 0)
				return CmbxFileFormat.ReadFile (file, monitor);
			else
				return PrjxFileFormat.ReadFile (file, monitor);*/
			return null;
		}
	}
}
