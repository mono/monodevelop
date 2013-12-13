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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.MD1
{
	class MD1FileFormat: IFileFormat
	{
		public bool SupportsMixedFormats {
			get { return true; }
		}

		public FilePath GetValidFormatName (object obj, FilePath fileName)
		{
			if (obj is WorkspaceItem && !(obj is Solution))
				return Path.ChangeExtension (fileName, ".mdw");
			throw new InvalidOperationException ();
		}

		public bool CanReadFile (FilePath file, Type expectedType)
		{
			string ext = Path.GetExtension (file).ToLower ();
			return ext == ".mdw" && expectedType.IsAssignableFrom (typeof(WorkspaceItem));
		}
		
		public bool CanWriteFile (object obj)
		{
			return (obj is WorkspaceItem && !(obj is Solution));
		}

		public List<FilePath> GetItemFiles (object obj)
		{
			return new List<FilePath> ();
		}

		public void WriteFile (FilePath file, object node, IProgressMonitor monitor)
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
				if (tmpfilename != String.Empty && File.Exists (tmpfilename))
					File.Delete (tmpfilename);
				throw;
			}
		}

		void WriteFileInternal (FilePath actualFile, FilePath outFile, object node, IProgressMonitor monitor)
		{
			WriteWorkspaceItem (actualFile, outFile, (WorkspaceItem) node, monitor);
		}

		void WriteWorkspaceItem (FilePath actualFile, FilePath outFile, WorkspaceItem item, IProgressMonitor monitor)
		{
			Workspace ws = item as Workspace;
			if (ws != null) {
				monitor.BeginTask (null, ws.Items.Count);
				try {
					foreach (WorkspaceItem it in ws.Items) {
						it.Save (monitor);
						monitor.Step (1);
					}
				} finally {
					monitor.EndTask ();
				}
			}
			
			StreamWriter sw = new StreamWriter (outFile);
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Saving item: {0}", actualFile), 1);
				XmlTextWriter tw = new XmlTextWriter (sw);
				tw.Formatting = Formatting.Indented;
				XmlDataSerializer ser = new XmlDataSerializer (MD1ProjectService.DataContext);
				ser.SerializationContext.BaseFile = actualFile;
				ser.SerializationContext.ProgressMonitor = monitor;
				ser.Serialize (sw, item, typeof(WorkspaceItem));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not save item: {0}", actualFile), ex);
				throw;
			} finally {
				monitor.EndTask ();
				sw.Close ();
			}
		}

		public object ReadFile (FilePath fileName, Type expectedType, IProgressMonitor monitor)
		{
			string ext = Path.GetExtension (fileName).ToLower ();
			if (ext != ".mdw")
				throw new ArgumentException ();

			object readObject = null;

			ProjectExtensionUtil.BeginLoadOperation ();
			try {
				readObject = ReadWorkspaceItemFile (fileName, monitor);
			} finally {
				ProjectExtensionUtil.EndLoadOperation ();
			}
			
			IWorkspaceFileObject fo = readObject as IWorkspaceFileObject;
			if (fo != null)
				fo.ConvertToFormat (MD1ProjectService.FileFormat, false);
			return readObject;
		}

		object ReadWorkspaceItemFile (FilePath fileName, IProgressMonitor monitor)
		{
			XmlTextReader reader = new XmlTextReader (new StreamReader (fileName));
			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading workspace item: {0}"), fileName), 1);
				reader.MoveToContent ();
				XmlDataSerializer ser = new XmlDataSerializer (MD1ProjectService.DataContext);
				ser.SerializationContext.BaseFile = fileName;
				ser.SerializationContext.ProgressMonitor = monitor;
				WorkspaceItem entry = (WorkspaceItem) ser.Deserialize (reader, typeof(WorkspaceItem));
				entry.ConvertToFormat (MD1ProjectService.FileFormat, false);
				entry.FileName = fileName;
				return entry;
			}
			catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not load solution item: {0}"), fileName), ex);
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

		public IEnumerable<string> GetCompatibilityWarnings (object obj)
		{
			yield break;
		}
		
		public bool SupportsFramework (MonoDevelop.Core.Assemblies.TargetFramework framework)
		{
			return true;
		}
	}
}