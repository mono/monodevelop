// 
// NativeReference.cs
//  
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Collections.Generic;

namespace MonoDevelop.MacDev.NativeReferences
{
	[DataItem]
	public class NativeReference : ProjectItem
	{
		[ProjectPathItemProperty ("Include")]
		public FilePath Path { get; set; }
		
		[ItemProperty ("IsCxx")]
		public bool IsCxx { get; set; }
		
		[ItemProperty ("Kind")]
		public NativeReferenceKind Kind { get; set; }
		
		public NativeReference ()
		{
		}
		
		public NativeReference (FilePath path)
		{
			this.Path = path;
			switch (path.Extension) {
			case ".a":
				Kind = NativeReferenceKind.Static;
				break;
			case ".framework":
				Kind = NativeReferenceKind.Framework;
				break;
			case ".lib":
				Kind = NativeReferenceKind.Dynamic;
				break;
			}
		}
		
		public override bool Equals (object obj)
		{
			var nr = obj as NativeReference;
			//fixme: check include paths too?
			return nr != null && nr.Path == Path && nr.IsCxx == IsCxx && nr.Kind == Kind;
		}
		
		public override int GetHashCode ()
		{
			int hash = IsCxx.GetHashCode () ^ Kind.GetHashCode ();
			if (Path != null)
				hash ^= Path.GetHashCode ();
			return hash;
		}
	}
	
	public enum NativeReferenceKind
	{
		Static,
		Dynamic,
		Framework
	}
	
	public class NativeReferenceFolder
	{
		public NativeReferenceFolder (DotNetProject project)
		{
			Project = project;
		}
		
		public DotNetProject Project { get; private set; }
	}
}

