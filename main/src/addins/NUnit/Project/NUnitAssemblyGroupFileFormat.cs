//
// NUnitAssemblyGroupFileFormat.cs
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
using System.IO;
using System.Xml;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.Formats.MD1;

namespace MonoDevelop.NUnit
{
	public class NUnitAssemblyGroupFileFormat: IFileFormat
	{
		public string Name {
			get { return "NUnit assembly group"; }
		}
		
		public string GetValidFormatName (object obj, string fileName)
		{
			return Path.ChangeExtension (fileName, ".md-nunit");
		}
		
		public bool CanReadFile (string file, Type expectedType)
		{
			return expectedType.IsAssignableFrom (typeof(NUnitAssemblyGroupProject)) && Path.GetExtension (file) == ".md-nunit";
		}
		
		public bool CanWriteFile (object obj)
		{
			return false;
		}
		
		public void WriteFile (string file, object obj, IProgressMonitor monitor)
		{
			WriteFile (file, file, obj, monitor);
		}
		
		public void ExportFile (string file, object obj, IProgressMonitor monitor)
		{
			WriteFile (((NUnitAssemblyGroupProject)obj).FileName, file, obj, monitor);
		}
		
		public List<string> GetItemFiles (object obj)
		{
			return new List<string> ();
		}
		
		void WriteFile (string file, string outFile, object obj, IProgressMonitor monitor)
		{
		}
		
		public object ReadFile (string file, Type expectedType, IProgressMonitor monitor)
		{
			XmlTextReader reader = new XmlTextReader (new StreamReader (file));
			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading project: {0}"), file), 1);
				
				reader.MoveToContent ();
				
				XmlDataSerializer ser = new XmlDataSerializer (MD1ProjectService.DataContext);
				ser.SerializationContext.BaseFile = file;
				
				SolutionEntityItem entry = (SolutionEntityItem) ser.Deserialize (reader, typeof(NUnitAssemblyGroupProject));
				entry.FileName = file;
				return entry;
			}
			catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not load project: {0}"), file), ex);
				throw;
			}
			finally {
				monitor.EndTask ();
				reader.Close ();
			}
		}

		public void ConvertToFormat (object obj)
		{
		}
		
		public bool SupportsMixedFormats {
			get { return false; }
		}

	}
}

