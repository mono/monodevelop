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
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;
using MonoDevelop.Services;

namespace MonoDevelop.NUnit
{
	public class NUnitAssemblyGroupFileFormat: IFileFormat
	{
		public string Name {
			get { return "NUnit assembly group"; }
		}
		
		public string GetValidFormatName (string fileName)
		{
			return Path.ChangeExtension (fileName, ".md-nunit");
		}
		
		public bool CanReadFile (string file)
		{
			return Path.GetExtension (file) == ".md-nunit";
		}
		
		public bool CanWriteFile (object obj)
		{
			return obj is NUnitAssemblyGroupProject;
		}
		
		public void WriteFile (string file, object obj, IProgressMonitor monitor)
		{
			NUnitAssemblyGroupProject project = obj as NUnitAssemblyGroupProject;
			if (project == null)
				throw new InvalidOperationException ("The provided object is not a valid Project");

			StreamWriter sw = new StreamWriter (file);
			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString("Saving project: {0}"), file), 1);
				XmlDataSerializer ser = new XmlDataSerializer (Runtime.ProjectService.DataContext);
				ser.SerializationContext.BaseFile = file;
				ser.Serialize (sw, project, typeof(NUnitAssemblyGroupProject));
			} catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not save project: {0}"), file), ex);
			} finally {
				monitor.EndTask ();
				sw.Close ();
			}
		}
		
		public object ReadFile (string file, IProgressMonitor monitor)
		{
			XmlTextReader reader = new XmlTextReader (new StreamReader (file));
			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading project: {0}"), file), 1);
				
				reader.MoveToContent ();
				
				XmlDataSerializer ser = new XmlDataSerializer (Runtime.ProjectService.DataContext);
				ser.SerializationContext.BaseFile = file;
				
				CombineEntry entry = (CombineEntry) ser.Deserialize (reader, typeof(NUnitAssemblyGroupProject));
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
	}
}

