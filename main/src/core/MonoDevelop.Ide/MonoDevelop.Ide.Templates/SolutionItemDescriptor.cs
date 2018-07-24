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
using System.Threading.Tasks;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins;

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
		RuntimeAddin addin;
		
		protected SolutionItemDescriptor (RuntimeAddin addin, XmlElement element)
		{
			this.addin = addin;
			name = element.GetAttribute ("name");
//			relativePath = element.GetAttribute ("directory");
			typeName = element.GetAttribute ("type");
			template = element;
		}
		
		public SolutionItem CreateItem (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
		{
			Type type = addin.GetType (typeName, false);
			
			if (type == null) {
				MessageService.ShowError (GettextCatalog.GetString ("Can't create project with type : {0}", typeName));
				return null;
			}

			// TODO NPM: should use the project service
			SolutionItem item = (SolutionItem) Activator.CreateInstance (type);
			item.InitializeFromTemplate (projectCreateInformation, template);
			
			string newProjectName = StringParserService.Parse (name, new string[,] { 
				{"ProjectName", projectCreateInformation.ProjectName}
			});
			
			item.Name = newProjectName;
			item.FileName = Path.Combine (projectCreateInformation.ProjectBasePath, newProjectName);
			
			return item;
		}
		
		public Task InitializeItem (SolutionFolderItem policyParent, ProjectCreateInformation projectCreateInformation, string defaultLanguage, SolutionItem item)
		{
			return Task.CompletedTask;
		}
		
		public static SolutionItemDescriptor CreateDescriptor (RuntimeAddin addin, XmlElement element)
		{
			return new SolutionItemDescriptor (addin, element);
		}
	}
}
