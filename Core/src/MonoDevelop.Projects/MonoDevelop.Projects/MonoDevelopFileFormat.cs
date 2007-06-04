//
// MonoDevelopFileFormat.cs
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
using System.IO;
using System.Xml;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	internal class MonoDevelopFileFormat: IFileFormat
	{
		public string Name {
			get { return "MonoDevelop Project Model"; }
		}
		
		public string GetValidFormatName (object obj, string fileName)
		{
			if (obj is Project)
				return Path.ChangeExtension (fileName, ".mdp");
			else
				return Path.ChangeExtension (fileName, ".mds");
		}
		
		public bool CanReadFile (string file)
		{
			return String.Compare (Path.GetExtension (file), ".mdp", true) == 0 ||
			       String.Compare (Path.GetExtension (file), ".mds", true) == 0;
		}
		
		public bool CanWriteFile (object obj)
		{
			return (obj is Project) || (obj is Combine);
		}
		
		public System.Collections.Specialized.StringCollection GetExportFiles (object obj)
		{
			return null;
		}

		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
			string tmpfilename = null;
			try {
				try {
					if (File.Exists (file))
						tmpfilename = Path.GetTempFileName ();
				} catch (IOException) {
				}

				if (tmpfilename == null) {
					WriteFileInternal (file, file, node, monitor);
				} else {
					WriteFileInternal (file, tmpfilename, node, monitor);
					File.Delete (file);
					File.Move (tmpfilename, file);
				}
			} catch {
				if (tmpfilename != String.Empty)
					File.Delete (tmpfilename);
				throw;
			}
		}

		void WriteFileInternal (string actualFile, string outFile, object node, IProgressMonitor monitor)
		{
			if (node is Project) {
				Project project = (Project) node;
				StreamWriter sw = new StreamWriter (outFile);
				try {
					monitor.BeginTask (GettextCatalog.GetString("Saving project: {0}", actualFile), 1);
					XmlDataSerializer ser = new XmlDataSerializer (Services.ProjectService.DataContext);
					ser.SerializationContext.BaseFile = actualFile;
					ser.Serialize (sw, project, typeof(Project));
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Could not save project: {0}", actualFile), ex);
					throw;
				} finally {
					monitor.EndTask ();
					sw.Close ();
				}
			}
			else {
				Combine combine = (Combine) node;
				StreamWriter sw = new StreamWriter (outFile);
				try {
					monitor.BeginTask (GettextCatalog.GetString ("Saving solution: {0}", actualFile), 1);
					XmlTextWriter tw = new XmlTextWriter (sw);
					tw.Formatting = Formatting.Indented;
					DataSerializer serializer = new DataSerializer (Services.ProjectService.DataContext, actualFile);
					CombineWriterV2 combineWriter = new CombineWriterV2 (serializer, monitor);
					combineWriter.WriteCombine (tw, combine);
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Could not save solution: {0}", actualFile), ex);
					throw;
				} finally {
					monitor.EndTask ();
					sw.Close ();
				}
			}
		}
		
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			if (String.Compare (Path.GetExtension (fileName), ".mdp", true) == 0)
				return ReadProjectFile (fileName, monitor);
			else
				return ReadCombineFile (fileName, monitor);
		}
	
		object ReadCombineFile (string file, IProgressMonitor monitor)
		{
			XmlTextReader reader = new XmlTextReader (new StreamReader (file));
			reader.MoveToContent ();
			
			string version = reader.GetAttribute ("version");
			if (version == null) version = reader.GetAttribute ("fileversion");
			
			DataSerializer serializer = new DataSerializer (Services.ProjectService.DataContext, file);
			ICombineReader combineReader = null;
			
			if (version == "1.0" || version == "1") {
				combineReader = new CombineReaderV1 (serializer, monitor);
				monitor.ReportWarning (GettextCatalog.GetString ("The file '{0}' is using an old solution file format. It will be automatically converted to the current format.", file));
			}
			else if (version == "2.0")
				combineReader = new CombineReaderV2 (serializer, monitor);
			
			try {
				if (combineReader != null)
					return combineReader.ReadCombine (reader);
				else
					throw new UnknownProjectVersionException (file, version);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load solution: {0}", file), ex);
				throw;
			} finally {
				reader.Close ();
			}
		}
		
		object ReadProjectFile (string fileName, IProgressMonitor monitor)
		{
		/*
			XmlTextReader reader = null;
			try {
				reader = new XmlTextReader (new StreamReader (fileName));
				reader.MoveToContent ();

				string version = reader.GetAttribute ("version");
				if (version == null) version = reader.GetAttribute ("fileversion");
				
				DataSerializer serializer = new DataSerializer (Services.ProjectService.DataContext, fileName);
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
					monitor.ReportWarning (GettextCatalog.GetString ("The file '{0}' is using an old project file format. It will be automatically converted to the current format.", fileName));
				
				monitor.BeginTask (GettextCatalog.GetString ("Loading project: {0}", fileName), 1);
				if (projectReader != null)
					return projectReader.ReadProject (reader);
				else
					throw new UnknownProjectVersionException (fileName, version);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load project: {0}", fileName), ex);
				throw;
			} finally {
				monitor.EndTask ();
				if (reader != null)
					reader.Close ();
			}*/
			return null;
		}
	}
}
