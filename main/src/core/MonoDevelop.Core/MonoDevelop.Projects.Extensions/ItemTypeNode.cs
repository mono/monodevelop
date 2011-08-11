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

namespace MonoDevelop.Projects.Extensions
{
	public abstract class ItemTypeNode: ExtensionNode
	{
		[NodeAttribute (Required=true)]
		string guid = null;
		
		[NodeAttribute]
		string extension = null;
		
		[NodeAttribute]
		string import = null;
		
		[NodeAttribute]
		string handlerType = null;
		
		public ItemTypeNode ()
		{
		}
		
		public ItemTypeNode (string guid, string extension, string import)
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
		
		public abstract bool CanHandleItem (SolutionEntityItem item);
		
		public virtual void InitializeHandler (SolutionEntityItem item)
		{
			MSBuildHandler h = CreateHandler<MSBuildHandler> (null, null);
			h.Item = item;
			item.SetItemHandler (h);
		}
		
		public virtual bool CanHandleFile (string fileName, string typeGuid)
		{
			if (typeGuid != null && string.Compare (typeGuid, guid, true) == 0)
				return true;
			if (!string.IsNullOrEmpty (extension) && System.IO.Path.GetExtension (fileName) == "." + extension)
				return true;
			return false;
		}
		
		protected T CreateHandler<T> (string fileName, string itemGuid) where T:MSBuildHandler
		{
			MSBuildHandler h = OnCreateHandler (fileName, itemGuid);
			if (!(h is T))
				throw new InvalidOperationException ("Error while creating a MSBuildHandler. Expected an object of type '" + typeof(T).FullName + ", found type '" + h.GetType ());
			return (T)h;
		}
		
		protected virtual MSBuildHandler OnCreateHandler (string fileName, string itemGuid)
		{
			MSBuildHandler h;
			if (!string.IsNullOrEmpty (handlerType)) {
				h = Addin.CreateInstance (handlerType, true) as MSBuildHandler;
				if (h == null)
					throw new InvalidOperationException ("Type '" + handlerType + "' must be a subclass of 'MonoDevelop.Projects.Formats.MSBuild.MSBuildHandler'");
				if (h is MSBuildProjectHandler)
					((MSBuildProjectHandler)h).Initialize (Guid, Import, itemGuid);
				else
					h.Initialize (Guid, itemGuid);
			} else {
				h = new MSBuildProjectHandler (Guid, Import, itemGuid);
			}
			return h;
		}
		
		public abstract SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName, MSBuildFileFormat expectedFormat, string itemGuid);
	}
}
