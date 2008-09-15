//
// Member.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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
using System.Text.RegularExpressions;

using MonoDevelop.Projects;

namespace CBinding.Parser
{
	public class Member : LanguageItem
	{
		public string InstanceType {
			get{ return instanceType; }
		}
		protected string instanceType;
		
		public bool IsPointer {
			get{ return isPointer; }
		}
		protected bool isPointer;
		
		public Member (Tag tag, Project project, string ctags_output) : base (tag, project)
		{
			GetInstanceType (tag);
			
			if (GetClass (tag, ctags_output)) return;
			if (GetStructure (tag, ctags_output)) return;
			if (GetUnion (tag, ctags_output)) return;
		}
		
		/// <summary>
		/// Regex for deriving the type of a variable, 
		/// and whether it's a pointer, 
		/// from an expression, e.g. 
		/// static Foo::bar<string> *blah = NULL;
		/// </summary>
		public static Regex InstanceTypeExpression = new Regex (
		  @"^\s*((static|friend|const|mutable|extern|struct|union|\w*::|<[\w><:]*>)\s*)*(?<type>\w[\w\d]*)\s*(<.*>)?\s*(?<pointer>[*])?", 
		  RegexOptions.Compiled);
		
		/// <summary>
		/// Populates an instance's instanceType and isPointer fields 
		/// by matching its pattern against InstanceTypeExpression
		/// </summary>
		/// <param name="tag">
		/// The partially-populated tag of an instance
		/// <see cref="Tag"/>
		/// </param>
		/// <returns>
		/// Whether the regex was successfully matched
		/// <see cref="System.Boolean"/>
		/// </returns>
		protected bool GetInstanceType (Tag tag) {
			/*Match m = InstanceTypeExpression.Match (tag.Pattern);
			
			if (null == m)
				return false;
			
			instanceType = m.Groups["type"].Value;
			isPointer = m.Groups["pointer"].Success;
			
			return true;*/
			// TODO: Fix this back up, without using the tag pattern (we reference tags by line number now)
			return false;
		}
	}
}
