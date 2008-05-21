// MD1ProjectService.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects.Formats.MD1
{
	public class MD1ProjectService
	{
		static DataContext dataContext;
		static XmlMapAttributeProvider attributeProvider;
		
		const string MD1SerializationMapsExtensionPath = "/MonoDevelop/ProjectModel/MD1SerializationMaps";
		
		public static DataContext DataContext {
			get {
				if (dataContext == null) {
					dataContext = new DataContext (attributeProvider);
					Services.ProjectService.InitializeDataContext (dataContext);
				}
				return dataContext;
			}
		}
		
		static MD1ProjectService ()
		{
			attributeProvider = new XmlMapAttributeProvider ();
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/MD1SerializationMaps", OnMapsChanged);
			
			Services.ProjectService.DataContextChanged += delegate {
				// Regenerate the data context
				dataContext = null;
			};
		}
		
		static void OnMapsChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				SerializationMapNode node = (SerializationMapNode) args.ExtensionNode;
				attributeProvider.AddMap (node.Addin, node.FileContent, node.FileId);
			}
			else {
				SerializationMapNode node = (SerializationMapNode) args.ExtensionNode;
				attributeProvider.RemoveMap (node.FileId);
			}
			
			// Regenerate the data context
			dataContext = null;
		}
		
		public static FileFormat FileFormat {
			get {
				return Services.ProjectService.FileFormats.GetFileFormat ("MD1");
			}
		}
		
		public static string GetItemFileName (SolutionItem item)
		{
			if (item is SolutionEntityItem)
				return ((SolutionEntityItem)item).FileName;
			return (string) item.ExtendedProperties ["FileName"];
		}
		
		public static void SetItemFileName (SolutionItem item, string fileName)
		{
			if (item is SolutionEntityItem)
				((SolutionEntityItem)item).FileName = fileName;
			else
				item.ExtendedProperties ["FileName"] = fileName;
		}
		
		public static void InitializeHandler (SolutionItem item)
		{
			if (item is DotNetProject) {
				DotNetProject p = (DotNetProject) item;
				item.SetItemHandler (new MD1DotNetProjectHandler (p));
				p.FileName = FileFormat.GetValidFileName (p, p.FileName);
			}
			else if (item is SolutionEntityItem) {
				SolutionEntityItem it = (SolutionEntityItem) item;
				item.SetItemHandler (new MD1SolutionEntityItemHandler (it));
				it.FileName = FileFormat.GetValidFileName (it, it.FileName);
			}
			else {
				item.SetItemHandler (new MD1SolutionItemHandler (item));
			}
		}
	}
}
