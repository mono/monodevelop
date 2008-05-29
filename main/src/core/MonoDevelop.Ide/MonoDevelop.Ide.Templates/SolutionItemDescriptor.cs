//
// SolutionItemDescriptor.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.Templates
{
	/// <summary>
	/// This class is used inside the combine templates for projects.
	/// </summary>
	internal class SolutionItemDescriptor: ISolutionItemDescriptor
	{
		string name;
//		string relativePath;
		string typeName;
		XmlElement template;
		
		protected SolutionItemDescriptor (XmlElement element)
		{
			name = element.GetAttribute ("name");
//			relativePath = element.GetAttribute ("directory");
			typeName = element.GetAttribute ("type");
			template = element;
		}
		
		public SolutionEntityItem CreateItem (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
		{
			Type type = Type.GetType (typeName);
			
			if (type == null) {
				MessageService.ShowError (GettextCatalog.GetString ("Can't create project with type : {0}", typeName));
				return null;
			}
			
			SolutionEntityItem item = (SolutionEntityItem) Activator.CreateInstance (type);
			item.InitializeFromTemplate (template);
			
			string newProjectName = StringParserService.Parse (name, new string[,] { 
				{"ProjectName", projectCreateInformation.ProjectName}
			});
			
			item.Name = newProjectName;
			item.FileName = Path.Combine (projectCreateInformation.ProjectBasePath, newProjectName);
			
			return item;
		}
		
		public void InitializeItem (ProjectCreateInformation projectCreateInformation, string defaultLanguage, SolutionEntityItem item)
		{
		}
		
		public static SolutionItemDescriptor CreateDescriptor (XmlElement element)
		{
			return new SolutionItemDescriptor (element);
		}
	}
}
