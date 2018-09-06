// MSBuildDotNetProjectExtensionNode.cs
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
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Projects.Extensions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Extensions
{
	[ExtensionNode (ExtensionAttributeType=typeof(ExportDotNetProjectTypeAttribute))]
	public class DotNetProjectTypeNode: ProjectTypeNode
	{
		[NodeAttribute (Required=true)]
		string language = null;
		
		public string Language {
			get { return language; }
		}

		[NodeAttribute]
		bool isDefaultGuid = true;

		/// <summary>
		/// Indicates if the Guid specified is the default for the language.
		/// </summary>
		public bool IsDefaultGuid {
			get { return isDefaultGuid; }
		}

		public DotNetProjectTypeNode ()
		{
		}

		protected override void Read (NodeElement elem)
		{
			base.Read (elem);
			if (!string.IsNullOrEmpty (language) && string.IsNullOrEmpty (TypeAlias))
				TypeAlias = language;
		}

		public override Type ItemType {
			get {
				if (!string.IsNullOrEmpty (ItemTypeName))
					return base.ItemType;
				else
					return typeof(DotNetProject);
			}
		}
	}
}
