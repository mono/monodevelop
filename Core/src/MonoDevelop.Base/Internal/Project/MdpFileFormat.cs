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
using System.IO;
using System.Xml;
using MonoDevelop.Internal.Serialization;
using MonoDevelop.Services;

namespace MonoDevelop.Internal.Project
{
	public class MdpFileFormat: IFileFormat
	{
		public string Name {
			get { return "MonoDevelop Project"; }
		}
		
		public string GetValidFormatName (string fileName)
		{
			return Path.ChangeExtension (fileName, ".mdp");
		}
		
		public bool CanReadFile (string file)
		{
			return String.Compare (Path.GetExtension (file), ".mdp", true) == 0;
		}
		
		public bool CanWriteFile (object obj)
		{
			return obj is Project;
		}
		
		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
			Project project = node as Project;
			if (project == null)
				throw new InvalidOperationException ("The provided object is not a Project");

			StreamWriter sw = new StreamWriter (file);
			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString("Saving project: {0}"), file), 1);
				XmlDataSerializer ser = new XmlDataSerializer (Runtime.ProjectService.DataContext);
				ser.SerializationContext.BaseFile = file;
				ser.Serialize (sw, project, typeof(Project));
			} catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not save project: {0}"), file), ex);
			} finally {
				monitor.EndTask ();
				sw.Close ();
			}
		}
		
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			XmlTextReader reader = new XmlTextReader (new StreamReader (fileName));
			reader.MoveToContent ();

			string version = reader.GetAttribute ("version");
			if (version == null) version = reader.GetAttribute ("fileversion");
			
			DataSerializer serializer = new DataSerializer (Runtime.ProjectService.DataContext, fileName);
			IProjectReader projectReader = null;
			
			if (version == "1.0" || version == "1") {
				string tempFile = Path.GetTempFileName();
				
				ConvertXml.Convert(fileName,
				                   Runtime.Properties.DataDirectory + Path.DirectorySeparatorChar +
				                   "ConversionStyleSheets" + Path.DirectorySeparatorChar +
				                   "ConvertPrjx10to11.xsl",
				                   tempFile);
				reader.Close ();
				StreamReader sr = new StreamReader (tempFile);
				string fdata = sr.ReadToEnd ();
				sr.Close ();
				File.Delete (tempFile);
				reader = new XmlTextReader (new StringReader (fdata));
				projectReader = new ProjectReaderV1 (serializer);
			}
			else if (version == "1.1") {
				projectReader = new ProjectReaderV1 (serializer);
			}
			else if (version == "2.0") {
				projectReader = new ProjectReaderV2 (serializer);
			}
			
			if (version != "2.0")
				monitor.ReportWarning (string.Format (GettextCatalog.GetString ("The file '{0}' is using an old project file format. It will be automatically converted to the current format."), fileName));
			
			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading project: {0}"), fileName), 1);
				if (projectReader != null)
					return projectReader.ReadProject (reader);
				else
					throw new UnknownProjectVersionException (fileName, version);
			} catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not load project: {0}"), fileName), ex);
				throw;
			} finally {
				monitor.EndTask ();
				reader.Close ();
			}
		}
	}
}
