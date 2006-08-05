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

namespace MonoDevelop.DesignerSupport
{
	
	
	public class MemberExistsException : Exception
	{
		string className;
		string memberName;
		MemberType existingMemberType = MemberType.Member;
		MemberType newMemberType = MemberType.Member;
		
		
		public MemberExistsException (string className, string memberName)
		{
			this.className = className;
			this.memberName = memberName;
		}
		
		public MemberExistsException (string className, string memberName, MemberType existingMemberType, MemberType newMemberType)
			: this (className, memberName)
		{
			this.existingMemberType = existingMemberType;
			this.newMemberType = newMemberType;
		}
		
		public MemberExistsException (string className, System.CodeDom.CodeTypeMember newMember, MemberType existingMemberType)
			: this (className, newMember.Name)
		{
			this.existingMemberType = existingMemberType;
			
			if (newMember is System.CodeDom.CodeMemberEvent)
				this.newMemberType = MemberType.Event;
			else if (newMember is System.CodeDom.CodeMemberProperty)
				this.newMemberType = MemberType.Property;
			else if (newMember is System.CodeDom.CodeMemberField)
				this.newMemberType = MemberType.Field;
			else if (newMember is System.CodeDom.CodeMemberMethod)
				this.newMemberType = MemberType.Method;
		}
		
		public string ClassName {
			get { return className; }
			set { className = value; }
		}
		
		string MemberName {
			get { return memberName; }
			set { memberName = value; }
		}
		
		MemberType ExistingMemberType {
			get { return existingMemberType; }
			set { existingMemberType = value; }
		}
		
		MemberType NewMemberType {
			get { return newMemberType; }
			set { newMemberType = value; }
		}
		
		public override string ToString ()
		{
			return "Cannot add new " + Enum.GetName (typeof (MemberType), newMemberType).ToLower () +
				" named \"" + memberName + "\"to class \"" + className + "\", " + "because there is already a \""
				+ Enum.GetName (typeof (MemberType), existingMemberType).ToLower () + "\" with that name.";
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
