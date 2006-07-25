
using System;
using System.CodeDom;

using MonoDevelop.Projects.Parser;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class BindingService
	{
		//TODO: currently case-sensitive, so some languages may not like this
		const bool ignoreCase = false;
		
		internal BindingService ()
		{
		}
		
		//TODO: modifying file view in-place
		public void AddMemberToClass (IClass cls, CodeTypeMember member, bool throwIfExists)
		{		
			
			//check for identical property names
			foreach (IProperty prop in cls.Properties) {
				if (string.Compare (prop.Name, member.Name, ignoreCase) == 0) {
					CodeMemberProperty memProp = member as CodeMemberProperty;
					
					if (throwIfExists || (memProp == null))
						throw new MemberExistsException (cls.Name, member, MemberType.Property);
					
					if (memProp.Type.BaseType != prop.ReturnType.FullyQualifiedName)
						throw new InvalidOperationException ("Return type does not match");
					
					return;
				}
			}
				
			//check for identical method names
			foreach (IMethod meth in cls.Methods) {
				if (string.Compare (meth.Name, member.Name, ignoreCase) == 0) {
					CodeMemberMethod memMeth = member as CodeMemberMethod;
					
					if (throwIfExists || (memMeth == null))
						throw new MemberExistsException (cls.Name, member, MemberType.Method);
					
					if (memMeth.ReturnType.BaseType != meth.ReturnType.FullyQualifiedName)
						throw new InvalidOperationException ("Return type does not match");
					
					return;
				}
			}
			
			//check for identical event names
			foreach (IEvent ev in cls.Events) {
				if (string.Compare (ev.Name, member.Name, ignoreCase) == 0) {
					CodeMemberEvent memEv = member as CodeMemberEvent;
					
					if (throwIfExists || (memEv == null))
						throw new MemberExistsException (cls.Name, member, MemberType.Event);
					
					if (memEv.Type.BaseType != ev.ReturnType.FullyQualifiedName)
						throw new InvalidOperationException ("Return type does not match");

					return;
				}
			}
				
			//check for identical field names
			foreach (IField field in cls.Fields) {
				if (string.Compare (field.Name, member.Name, ignoreCase) == 0) {
					CodeMemberField memField = member as CodeMemberField;
					
					if (throwIfExists || (memField == null))
						throw new MemberExistsException (cls.Name, member, MemberType.Method);
					
					if (memField.Type.BaseType != field.ReturnType.FullyQualifiedName)
						throw new InvalidOperationException ("Return type does not match");
					
					return;
				}
			}
			
			MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.CodeRefactorer.AddMember (cls, member);
		}
		
		
		
		
	}
}
