////
//// DefaultFileFormat.cs
////
//// Author:
////   Lluis Sanchez Gual
////
//// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
////
//// Permission is hereby granted, free of charge, to any person obtaining
//// a copy of this software and associated documentation files (the
//// "Software"), to deal in the Software without restriction, including
//// without limitation the rights to use, copy, modify, merge, publish,
//// distribute, sublicense, and/or sell copies of the Software, and to
//// permit persons to whom the Software is furnished to do so, subject to
//// the following conditions:
//// 
//// The above copyright notice and this permission notice shall be
//// included in all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////
//
//
//using System;
//using System.IO;
//using System.Xml;
//using MonoDevelop.Projects.Serialization;
//using MonoDevelop.Core;
//
//namespace MonoDevelop.Projects
//{
//	public class DefaultFileFormat: IFileFormat
//	{
//		public string Name {
//			get { return "MonoDevelop Solution Item"; }
//		}
//		
//		public string GetValidFormatName (object obj, string fileName)
//		{
//			return Path.ChangeExtension (fileName, ".mdse");
//		}
//		
//		public bool CanReadFile (string file)
//		{
//			return Path.GetExtension (file) == ".mdse";
//		}
//		
//		public bool CanWriteFile (object obj)
//		{
//			return obj is CombineEntry;
//		}
//		
//		public void WriteFile (string file, object obj, IProgressMonitor monitor)
//		{
//			WriteFile (file, file, obj, monitor);
//		}
//		
//		public System.Collections.Specialized.StringCollection GetExportFiles (object obj)
//		{
//			return null;
//		}
//		
//		void WriteFile (string file, string outFile, object obj, IProgressMonitor monitor)
//		{
//			StreamWriter sw = new StreamWriter (outFile);
//			try {
//				monitor.BeginTask (string.Format (GettextCatalog.GetString("Saving solution item: {0}"), file), 1);
//				XmlDataSerializer ser = new XmlDataSerializer (Services.ProjectService.DataContext);
//				ser.SerializationContext.BaseFile = file;
//				ser.Serialize (sw, obj, typeof(CombineEntry));
//			} catch (Exception ex) {
//				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not save solution item: {0}"), file), ex);
//			} finally {
//				monitor.EndTask ();
//				sw.Close ();
//			}
//		}
//		
//		public object ReadFile (string file, IProgressMonitor monitor)
//		{
//			XmlTextReader reader = new XmlTextReader (new StreamReader (file));
//			try {
//				monitor.BeginTask (string.Format (GettextCatalog.GetString ("Loading solution item: {0}"), file), 1);
//				
//				reader.MoveToContent ();
//				
//				XmlDataSerializer ser = new XmlDataSerializer (Services.ProjectService.DataContext);
//				ser.SerializationContext.BaseFile = file;
//				
//				CombineEntry entry = (CombineEntry) ser.Deserialize (reader, typeof(CombineEntry));
//				entry.FileName = file;
//				return entry;
//			}
//			catch (Exception ex) {
//				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not load solution item: {0}"), file), ex);
//				throw;
//			}
//			finally {
//				monitor.EndTask ();
//				reader.Close ();
//			}
//		}
//	}
//}
//
//