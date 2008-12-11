// MSBuildHandler.cs
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildHandler: ISolutionItemHandler
	{
		SolutionItem item;
		string typeGuid;
		string id;
		string[] slnProjectContent;
		MSBuildFileFormat targetFormat;
		
		public MSBuildHandler (string typeGuid, string itemId)
		{
			this.typeGuid = typeGuid;
			this.id = itemId;
		}
		
		internal protected SolutionItem Item {
			get { return item; }
			set { item = value; }
		}
		
		public virtual bool SyncFileName {
			get { return true; }
		}

		public string TypeGuid {
			get {
				return typeGuid;
			}
		}

		internal string[] SlnProjectContent {
			get {
				return slnProjectContent;
			}
			set {
				slnProjectContent = value;
			}
		}
		
		public string ItemId {
			get {
				if (id == null)
					id = String.Format ("{{{0}}}", System.Guid.NewGuid ().ToString ().ToUpper ());
				return id; 
			}
			set { id = value; }
		}

		internal MSBuildFileFormat TargetFormat {
			get {
				return targetFormat;
			}
		}

		internal void SetTargetFormat (MSBuildFileFormat targetFormat)
		{
			this.targetFormat = targetFormat;
		}

		public virtual BuildResult RunTarget (IProgressMonitor monitor, string target, string configuration)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void Save (MonoDevelop.Core.IProgressMonitor monitor)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void Dispose ()
		{
		}
	}
}
