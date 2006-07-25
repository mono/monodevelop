
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
