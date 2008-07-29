//
// MemberExistsException.cs: Thrown if identifier already exists when 
//    binding CodeBehind members.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
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

using MonoDevelop.Projects.Parser;

namespace MonoDevelop.DesignerSupport
{	
	public class MemberExistsException : ErrorInFileException
	{
		string className;
		string memberName;
		
		MemberType existingMemberType = MemberType.Member;
		MemberType newMemberType = MemberType.Member;
		
		public MemberExistsException (string className, string memberName)
			: base (null)
		{
			this.className = className;
			this.memberName = memberName;
		}
		
		public MemberExistsException (string className, string memberName, MemberType existingMemberType, MemberType newMemberType, IRegion errorLocation)
			: base (errorLocation)
		{
			this.className = className;
			this.memberName = memberName;
			this.existingMemberType = existingMemberType;
			this.newMemberType = newMemberType;
		}
		
		public MemberExistsException (string className, MemberType newMemberType, System.CodeDom.CodeTypeMember existingMember, IRegion errorLocation)
			: this (className, existingMember.Name, newMemberType, GetMemberTypeFromCodeTypeMember (existingMember), errorLocation)
		{
		}
		
		public MemberExistsException (string className, System.CodeDom.CodeTypeMember newMember, MemberType existingMemberType, IRegion errorLocation)
			: this (className, newMember.Name, GetMemberTypeFromCodeTypeMember (newMember), existingMemberType, errorLocation)
		{
		}
		
		protected static MemberType GetMemberTypeFromCodeTypeMember (System.CodeDom.CodeTypeMember mem)
		{
			if (mem is System.CodeDom.CodeMemberEvent)
				return MemberType.Event;
			else if (mem is System.CodeDom.CodeMemberProperty)
				return MemberType.Property;
			else if (mem is System.CodeDom.CodeMemberField)
				return MemberType.Field;
			else if (mem is System.CodeDom.CodeMemberMethod)
				return MemberType.Method;
			return MemberType.Member;
		}
		
		public string ClassName {
			get { return className; }
			set { className = value; }
		}
		
		public string MemberName {
			get { return memberName; }
			set { memberName = value; }
		}
		
		public MemberType ExistingMemberType {
			get { return existingMemberType; }
			set { existingMemberType = value; }
		}
		
		public MemberType NewMemberType {
			get { return newMemberType; }
			set { newMemberType = value; }
		}
		
		public override string ToString ()
		{
			string str;
			if (NewMemberType == ExistingMemberType)
				str = MonoDevelop.Core.GettextCatalog.GetString ("Cannot add {0} '{1}' to class '{2}', " + 
					"because there is already a {3} with that name with an incompatible return type.");
			else
				str = MonoDevelop.Core.GettextCatalog.GetString ("Cannot add {0} '{1}' to class '{2}', " + 
					"because there is already a {3} with that name.");
			
			return string.Format (str, newMemberType.ToString ().ToLower (), memberName, className, 
				existingMemberType.ToString ().ToLower ());
		}
	}
	
	public enum MemberType
	{
		Member,
		Field,
		Property,
		Method,
		Event
	}
}
