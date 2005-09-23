//
// CombineEntryDescriptor.cs
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
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui;

namespace MonoDevelop.Internal.Templates
{
	/// <summary>
	/// This class is used inside the combine templates for projects.
	/// </summary>
	internal class CombineEntryDescriptor: ICombineEntryDescriptor
	{
		string name;
//		string relativePath;
		string typeName;
		XmlElement template;
		
		protected CombineEntryDescriptor (XmlElement element)
		{
			name = element.GetAttribute ("name");
//			relativePath = element.GetAttribute ("directory");
			typeName = element.GetAttribute ("type");
			template = element;
		}
		
		public string CreateEntry (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
		{
			StringParserService stringParserService = Runtime.StringParserService;
			
			Type type = Type.GetType (typeName);
			
			if (type == null) {
				Runtime.MessageService.ShowError(String.Format (GettextCatalog.GetString ("Can't create project with type : {0}"), typeName));
				return String.Empty;
			}
			
			CombineEntry entry = (CombineEntry) Activator.CreateInstance (type);
			entry.InitializeFromTemplate (template);
			
			string newProjectName = stringParserService.Parse (name, new string[,] { 
				{"ProjectName", projectCreateInformation.ProjectName}
			});
			
			entry.Name = newProjectName;
			
			IFileFormat fileFormat = Runtime.ProjectService.FileFormats.GetFileFormatForObject (entry);
			if (fileFormat == null)
				throw new InvalidOperationException ("Can't find a file format for the type: " + type);

			string fileName = fileFormat.GetValidFormatName (Path.Combine (projectCreateInformation.ProjectBasePath, newProjectName));
			
			using (IProgressMonitor monitor = Runtime.TaskService.GetSaveProgressMonitor ()) {
				if (File.Exists (fileName)) {
					if (Runtime.MessageService.AskQuestion(String.Format (GettextCatalog.GetString ("Project file {0} already exists, do you want to overwrite\nthe existing file ?"), fileName),  GettextCatalog.GetString ("File already exists"))) {
						entry.Save (fileName, monitor);
					}
				} else {
					entry.Save (fileName, monitor);
				}
			}
			
			return fileName;
		}
		
		public static CombineEntryDescriptor CreateDescriptor (XmlElement element)
		{
			return new CombineEntryDescriptor (element);
		}
	}
}
