// MSBuildItemTypeExtensionNode.cs
//
// Author:sdfsdf
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
using System.IO;
using System.Xml;
using Mono.Addins;
using MonoDevelop.Projects.Formats.MSBuild;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Projects.Extensions
{
	[ExtensionNode (ExtensionAttributeType=typeof(ExportSolutionItemTypeAttribute))]
	public abstract class SolutionItemTypeNode: ExtensionNode
	{
		[NodeAttribute (Required=true)]
		string guid = null;
		
		[NodeAttribute]
		string extension = null;
		
		[NodeAttribute]
		string import = null;
		
		[NodeAttribute]
		string type = null;

		[NodeAttribute ("alias")]
		public string Alias { get; protected set; }

		public SolutionItemTypeNode ()
		{
		}
		
		public SolutionItemTypeNode (string guid, string extension, string import)
		{
			this.guid = guid;
			this.extension = extension;
			this.import = import;
		}
		
		public string Guid {
			get { return guid; }
		}

		public string Extension {
			get {
				return extension;
			}
		}

		public string Import {
			get {
				return import;
			}
		}

		internal string ItenTypeName {
			get { return type; }
		}

		public virtual Type ItemType {
			get { return Addin.GetType (type, true); }
		}
		
		public virtual bool CanHandleFile (string fileName, string typeGuid)
		{
			if (typeGuid != null && string.Compare (typeGuid, guid, true) == 0)
				return true;
			if (!string.IsNullOrEmpty (extension) && System.IO.Path.GetExtension (fileName) == "." + extension)
				return true;
			return false;
		}

		SolutionItemFactory factory;
		
		public virtual async Task<SolutionItem> CreateSolutionItem (ProgressMonitor monitor, string fileName, string typeGuid)
		{
			if (typeof(SolutionItemFactory).IsAssignableFrom (ItemType)) {
				if (factory == null)
					factory = (SolutionItemFactory) Activator.CreateInstance (ItemType);
				return await factory.CreateItem (fileName, typeGuid);
			}
			return (SolutionItem) Activator.CreateInstance (ItemType);
		}

		public virtual bool CanCreateSolutionItem (string type, ProjectCreateInformation info, System.Xml.XmlElement projectOptions)
		{
			return type.Equals (Guid, StringComparison.OrdinalIgnoreCase) || type == Alias;
		}

		public virtual SolutionItem CreateSolutionItem (string type, ProjectCreateInformation info, System.Xml.XmlElement projectOptions)
		{
			var item = CreateSolutionItem (new ProgressMonitor (), null, Guid).Result;
			item.InitializeNew (info, projectOptions);
			return item;
		}
	}
}
