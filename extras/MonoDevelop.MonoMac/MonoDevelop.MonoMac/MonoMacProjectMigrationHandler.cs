// 
// MonoMacProjectMigrationHandler.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright 2011, Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.Formats.MSBuild;
using MonoDevelop.Ide;
using MonoDevelop.MacDev.PlistEditor;
using MonoDevelop.Projects;

namespace MonoDevelop.MonoMac
{
	public class MonoMacProjectMigrationHandler : IDotNetSubtypeMigrationHandler
	{
		public IEnumerable<string> FilesToBackup (string filename)
		{
			// Backup the project file only
			return new string[] { filename };
		}
		
		public bool Migrate (IProjectLoadProgressMonitor monitor, MSBuildProject project, string fileName, string language)
		{
			// Migrate 'Page' build action to 'InterfaceDefinition' (see Bug 147 - XIB files need Build Action other than Page)
			// NOTE: Work around mono bug by calling ToList() before iterating: http://bugzilla.xamarin.com/show_bug.cgi?id=520
			foreach (var item in project.GetAllItems ().Where (item => item.Name == "Page").ToList ()) {
				var newElement = project.doc.CreateElement (BuildAction.InterfaceDefinition);
				newElement.InnerXml = item.Element.InnerXml;
				foreach (System.Xml.XmlAttribute attribute in item.Element.Attributes)
					newElement.SetAttribute (attribute.Name, attribute.Value);
				item.Element.ParentNode.ReplaceChild (newElement, item.Element);
			}
			
			return true;
		}
	}
}
