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

namespace MonoDevelop.Projects.Policies
{
	
	[DataItem ("DotNetNamingPolicy")]
	public class DotNetNamingPolicy : IEquatable<DotNetNamingPolicy>
	{
		public DotNetNamingPolicy ()
		{
		}
		
		public DotNetNamingPolicy (DirectoryNamespaceAssociation association, bool vsStyleResourceNames)
		{
			this.DirectoryNamespaceAssociation = association;
			this.VSStyleResourceNames = vsStyleResourceNames;
		}
		
		[ItemProperty]
		public DirectoryNamespaceAssociation DirectoryNamespaceAssociation { get; private set; }
		
		[ItemProperty]
		public bool VSStyleResourceNames { get; private set; }
		
		public bool Equals (DotNetNamingPolicy other)
		{
			return other != null && other.DirectoryNamespaceAssociation == DirectoryNamespaceAssociation
				&& other.VSStyleResourceNames == VSStyleResourceNames;
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
}
