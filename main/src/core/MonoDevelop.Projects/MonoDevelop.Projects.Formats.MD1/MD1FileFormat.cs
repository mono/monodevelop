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
	internal class MD1FileFormat: IFileFormat
	{
		public string Name {
			get { return "MonoDevelop 1.0"; }
		}
		
		public bool SupportsMixedFormats {
			get { return true; }
		}
		
		public string GetValidFormatName (object obj, string fileName)
		{
			if (obj is Project)
				return Path.ChangeExtension (fileName, ".mdp");
			else if (obj is Solution)
				return Path.ChangeExtension (fileName, ".mds");
			else if (obj is WorkspaceItem)
				return Path.ChangeExtension (fileName, ".mdw");
			else
				return Path.ChangeExtension (fileName, ".mdse");
		}
		
		public bool CanReadFile (string file, Type expectedType)
		{
			string ext = Path.GetExtension (file).ToLower ();
			
			if (ext == ".mds" && expectedType.IsAssignableFrom (typeof(Solution)))
				return true;
			else if (ext == ".mdp" && expectedType.IsAssignableFrom (typeof(Project)))
				return true;
			else if (ext == ".mdw" && expectedType.IsAssignableFrom (typeof(WorkspaceItem)))
				return true;
			return ext == ".mdse" && expectedType.IsAssignableFrom (typeof(SolutionEntityItem));
		}
		
		public bool CanWriteFile (object obj)
		{
			return (obj is SolutionEntityItem) || (obj is WorkspaceItem);
		}
		
		public List<string> GetItemFiles (object obj)
		{
			List<string> list = new List<string> ();
			if (obj is Solution) {
				Solution sol = (Solution) obj;
				list.Add (sol.FileName);
				foreach (SolutionFolder f in sol.GetAllSolutionItems<SolutionFolder> ()) {
					string fn = f.ExtendedProperties ["FileName"] as string;
					if (!string.IsNullOrEmpty (fn))
						list.Add (fn);
				}
			}
			return list;
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
				if (tmpfilename != String.Empty && File.Exists (tmpfilename))
					File.Delete (tmpfilename);
				throw;
			}
		}

		void WriteFileInternal (string actualFile, string outFile, object node, IProgressMonitor monitor)
		{
			if (node is Project) {
				WriteProject (actualFile, outFile, (Project) node, monitor);
			}
			else if (node is Solution) {
				WriteSolution (actualFile, outFile, (Solution) node, monitor);
			}
			else if (node is WorkspaceItem) {
				WriteWorkspaceItem (actualFile, outFile, (WorkspaceItem) node, monitor);
			}
			else {
				WriteSolutionEntityItem (actualFile, outFile, node, monitor);
			}
		}
		
		void WriteProject (string actualFile, string outFile, Project project, IProgressMonitor monitor)
		{
			StreamWriter sw = new StreamWriter (outFile);
			try {
				monitor.BeginTask (GettextCatalog.GetString("Saving project: {0}", actualFile), 1);
				XmlDataSerializer ser = new XmlDataSerializer (MD1ProjectService.DataContext);
				ser.SerializationContext.BaseFile = actualFile;
				ser.SerializationContext.ProgressMonitor = monitor;
				ser.Serialize (sw, project, typeof(Project));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not save project: {0}", actualFile), ex);
				throw;
			} finally {
				monitor.EndTask ();
				sw.Close ();
			}
		}
		
		void WriteSolution (string actualFile, string outFile, Solution solution, IProgressMonitor monitor)
		{
			StreamWriter sw = new StreamWriter (outFile);
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Saving solution: {0}", actualFile), 1);
				XmlTextWriter tw = new XmlTextWriter (sw);
				tw.Formatting = Formatting.Indented;
				DataSerializer serializer = new DataSerializer (MD1ProjectService.DataContext, actualFile);
				CombineWriterV2 combineWriter = new CombineWriterV2 (serializer, monitor, typeof(Solution));
				combineWriter.WriteCombine (tw, solution);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not save solution: {0}", actualFile), ex);
				throw;
			} finally {
				monitor.EndTask ();
				sw.Close ();
			}
		}
		
		void WriteWorkspaceItem (string actualFile, string outFile, WorkspaceItem item, IProgressMonitor monitor)
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
		
		void WriteSolutionEntityItem (string actualFile, string outFile, object node, IProgressMonitor monitor)
		{
			StreamWriter sw = new StreamWriter (outFile);
			try {
				monitor.BeginTask (string.Format (GettextCatalog.GetString("Saving solution item: {0}"), actualFile), 1);
				XmlDataSerializer ser = new XmlDataSerializer (MD1ProjectService.DataContext);
				ser.SerializationContext.BaseFile = actualFile;
				ser.SerializationContext.ProgressMonitor = monitor;
				ser.Serialize (sw, node, typeof(SolutionEntityItem));
			} catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not save solution item: {0}"), actualFile), ex);
			} finally {
				monitor.EndTask ();
				sw.Close ();
			}
		}
		
		public object ReadFile (string fileName, Type expectedType, IProgressMonitor monitor)
		{
			object readObject = null;
			
			ProjectExtensionUtil.BeginLoadOperation ();
			try {
				string ext = Path.GetExtension (fileName).ToLower ();
				
				if (ext == ".mdp") {
					object project = ReadProjectFile (fileName, monitor);
					if (project is DotNetProject)
						((DotNetProject)project).SetItemHandler (new MD1DotNetProjectHandler ((DotNetProject) project));
					readObject = project;
				}
				else if (ext == ".mds") {
					readObject = ReadCombineFile (fileName, monitor);
				}
				else if (ext == ".mdw") {
					readObject = ReadWorkspaceItemFile (fileName, monitor);
				}
				else {
					XmlTextReader reader = new XmlTextReader (new StreamReader (fileName));
					try {
						monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading solution item: {0}"), fileName), 1);
						reader.MoveToContent ();
						XmlDataSerializer ser = new XmlDataSerializer (MD1ProjectService.DataContext);
						ser.SerializationContext.BaseFile = fileName;
						ser.SerializationContext.ProgressMonitor = monitor;
						SolutionEntityItem entry = (SolutionEntityItem) ser.Deserialize (reader, typeof(SolutionEntityItem));
						entry.FileName = fileName;
						MD1ProjectService.InitializeHandler (entry);
						readObject = entry;
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
			} finally {
				ProjectExtensionUtil.EndLoadOperation ();
			}
			
			IWorkspaceFileObject fo = readObject as IWorkspaceFileObject;
			if (fo != null)
				fo.ConvertToFormat (MD1ProjectService.FileFormat, false);
			return readObject;
		}
	
		object ReadCombineFile (string file, IProgressMonitor monitor)
		{
			XmlTextReader reader = new XmlTextReader (new StreamReader (file));
			reader.MoveToContent ();
			
			string version = reader.GetAttribute ("version");
			if (version == null) version = reader.GetAttribute ("fileversion");
			
			DataSerializer serializer = new DataSerializer (MD1ProjectService.DataContext, file);
			ICombineReader combineReader = null;
			
			if (version == "2.0" || version == "2.1")
				combineReader = new CombineReaderV2 (serializer, monitor, typeof(Solution));
			
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
			XmlTextReader reader = null;
			try {
				reader = new XmlTextReader (new StreamReader (fileName));
				reader.MoveToContent ();

				string version = reader.GetAttribute ("version");
				if (version == null) version = reader.GetAttribute ("fileversion");
				
				DataSerializer serializer = new DataSerializer (MD1ProjectService.DataContext, fileName);
				IProjectReader projectReader = null;
				
				monitor.BeginTask (GettextCatalog.GetString ("Loading project: {0}", fileName), 1);
				
				if (version == "2.0" || version == "2.1") {
					projectReader = new ProjectReaderV2 (serializer);
					return projectReader.ReadProject (reader);
				}
				else
					throw new UnknownProjectVersionException (fileName, version);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load project: {0}", fileName), ex);
				throw;
			} finally {
				monitor.EndTask ();
				if (reader != null)
					reader.Close ();
			}
		}

		object ReadWorkspaceItemFile (string fileName, IProgressMonitor monitor)
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
			SolutionItem item = obj as SolutionItem;
			if (item != null)
				MD1ProjectService.InitializeHandler (item);
		}

		public IEnumerable<string> GetCompatibilityWarnings (object obj)
		{
			yield break;
		}
	}
}
