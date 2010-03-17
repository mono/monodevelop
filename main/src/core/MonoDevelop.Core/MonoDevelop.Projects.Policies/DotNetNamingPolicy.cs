// 
// NamingPolicy.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Policies
{
	[DataItem ("DotNetNamingPolicy")]
	public class DotNetNamingPolicy : IEquatable<DotNetNamingPolicy>
	{
		public DotNetNamingPolicy ()
		{
			this.ResourceNamePolicy = ResourceNamePolicy.FileFormatDefault;
		}
		
		public DotNetNamingPolicy (DirectoryNamespaceAssociation association, ResourceNamePolicy resourceNamePolicy)
		{
			this.DirectoryNamespaceAssociation = association;
			this.ResourceNamePolicy = resourceNamePolicy;
		}
		
		[ItemProperty]
		public DirectoryNamespaceAssociation DirectoryNamespaceAssociation { get; private set; }
		
		[ItemProperty]
		public ResourceNamePolicy ResourceNamePolicy { get; private set; }
		
		public bool Equals (DotNetNamingPolicy other)
		{
			return other != null && other.DirectoryNamespaceAssociation == DirectoryNamespaceAssociation
				&& other.ResourceNamePolicy == ResourceNamePolicy;
		}
		
		internal static ResourceNamePolicy GetDefaultResourceNamePolicy (object ob)
		{
			FileFormat format = null;
			if (ob is SolutionEntityItem)
				format = ((SolutionEntityItem)ob).FileFormat;
			else if (ob is SolutionItem)
				format = ((SolutionItem)ob).ParentSolution.FileFormat;
			else if (ob is Solution)
				format = ((Solution)ob).FileFormat;
			
			if (format != null && format.Name.StartsWith ("MSBuild"))
				return ResourceNamePolicy.MSBuild;
			else
				return ResourceNamePolicy.FileName;
		}
	}
	
	public enum DirectoryNamespaceAssociation
	{
		None = 0,
		Flat,
		Hierarchical,
		PrefixedFlat,
		PrefixedHierarchical
	}
	
	public enum ResourceNamePolicy
	{
		FileFormatDefault,
		FileName,
		MSBuild
	}
}
