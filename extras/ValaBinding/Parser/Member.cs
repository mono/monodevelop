//
// Member.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;
using System.Text.RegularExpressions;

using MonoDevelop.Projects;

namespace MonoDevelop.ValaBinding.Parser
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
			GetInstanceType(tag);
			
			if (GetClass (tag, ctags_output)) return;
			if (GetStructure (tag, ctags_output)) return;
			if (GetUnion (tag, ctags_output)) return;
		}
        
		/// <summary>
		/// Regex for deriving the type of a variable, 
		/// and whether it's a pointer, 
		/// from an expression, e.g. 
		/// static Foo.bar<string> *blah = NULL;
		/// </summary>
		public static Regex InstanceTypeExpression = new Regex (
		  @"^\s*((public|private|protected|construct|static|friend|const|mutable|extern|struct|union|\w*\.|<[\w><:]*>)\s*)*(?<type>\w[\w\d]*)\s*(<.*>)?\s*(?<pointer>[*])?", 
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
			Match m = InstanceTypeExpression.Match (tag.Pattern);
			
			if (null == m)
				return false;
			
			instanceType = m.Groups["type"].Value;
			isPointer = m.Groups["pointer"].Success;
			
			return true;
		}
	}
}
