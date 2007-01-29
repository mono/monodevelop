//
// BindingService.cs: Utility methods for binding CodeBehind members.
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
using System.CodeDom;
using System.Reflection;
using System.Collections.Generic;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class BindingService
	{
		//TODO: currently case-sensitive, so some languages may not like this
		const bool ignoreCase = false;
		
		private static ITextFileProvider openedFileProvider = new OpenDocumentFileProvider ();
		
		private BindingService ()
		{
		}
		
		
		public static IMember GetCompatibleMemberInClass (IClass cls, CodeTypeMember member)
		{
			//check for identical property names
			foreach (IProperty prop in cls.Properties) {
				if (string.Compare (prop.Name, member.Name, ignoreCase) == 0) {
					CodeMemberProperty memProp = member as CodeMemberProperty;
					
					if (memProp == null)
						throw new MemberExistsException (cls.Name, member, MemberType.Property);
					
					if (memProp.Type.BaseType != prop.ReturnType.FullyQualifiedName)
						throw new InvalidOperationException ("Return type does not match");
					
					return prop;
				}
			}
				
			//check for identical method names
			foreach (IMethod meth in cls.Methods) {
				if (string.Compare (meth.Name, member.Name, ignoreCase) == 0) {
					CodeMemberMethod memMeth = member as CodeMemberMethod;
					
					if (memMeth == null)
						throw new MemberExistsException (cls.Name, member, MemberType.Method);
					
					if (memMeth.ReturnType.BaseType != meth.ReturnType.FullyQualifiedName)
						throw new InvalidOperationException ("Return type does not match");
					
					return meth;
				}
			}
			
			//check for identical event names
			foreach (IEvent ev in cls.Events) {
				if (string.Compare (ev.Name, member.Name, ignoreCase) == 0) {
					CodeMemberEvent memEv = member as CodeMemberEvent;
					
					if (memEv == null)
						throw new MemberExistsException (cls.Name, member, MemberType.Event);
					
					if (memEv.Type.BaseType != ev.ReturnType.FullyQualifiedName)
						throw new InvalidOperationException ("Return type does not match");

					return ev;
				}
			}
				
			//check for identical field names
			foreach (IField field in cls.Fields) {
				if (string.Compare (field.Name, member.Name, ignoreCase) == 0) {
					CodeMemberField memField = member as CodeMemberField;
					
					if (memField == null)
						throw new MemberExistsException (cls.Name, member, MemberType.Method);
					
					if (memField.Type.BaseType != field.ReturnType.FullyQualifiedName)
						throw new InvalidOperationException ("Return type does not match");
					
					return field;
				}
			}
			
			//return null if no match
			return null;
		}
		
		public static IMember AddMemberToClass (IClass cls, CodeTypeMember member, bool throwIfExists)
		{
			IMember existingMember = GetCompatibleMemberInClass (cls, member);
			
			if (existingMember == null)
				return GetCodeGenerator ().AddMember (cls, member);
			
			if (throwIfExists)
				throw new MemberExistsException (cls.Name, member, MemberType.Method);
			
			return existingMember;
		}
		
		public static CodeRefactorer GetCodeGenerator ()
		{			
			CodeRefactorer cr = new CodeRefactorer (IdeApp.ProjectOperations.CurrentOpenCombine, IdeApp.ProjectOperations.ParserDatabase);
			cr.TextFileProvider = openedFileProvider;
			return cr;
		}
		
		//copied from MonoDevelop.GtkCore.GuiBuilder
		private class OpenDocumentFileProvider: ITextFileProvider
		{
			public IEditableTextFile GetEditableTextFile (string filePath)
			{
				foreach (Document doc in IdeApp.Workbench.Documents) {
					//FIXME: look in other views
					if (doc.FileName == filePath) {
						IEditableTextFile ef = doc.GetContent<IEditableTextFile> ();
						if (ef != null) return ef;
					}
				}
				return null;
			}
		}
		
		//TODO: check accessibility
		public static string[] GetCompatibleMethodsInClass (IClass cls, CodeMemberMethod testMethod)
		{
			List<string> list = new List<string> ();
			
			foreach (IMethod method in cls.Methods) {
				if (method.Parameters.Count != testMethod.Parameters.Count)
					continue;
				
				if (method.ReturnType.FullyQualifiedName != testMethod.ReturnType.BaseType)
					continue;
				
				//compare each parameter
				bool mismatch = false;
				for (int i = 0; i < testMethod.Parameters.Count; i++)
					if (method.Parameters[i].ReturnType.FullyQualifiedName != testMethod.Parameters[i].Type.BaseType)
						mismatch = true;
				
				if (!mismatch)
					list.Add (method.Name);
			}
			
			return list.ToArray ();
		}
		
		
		public static string[] GetCompatibleMembersInClass (IClass cls, CodeTypeMember testMember)
		{
			if (testMember is CodeMemberMethod)
				return GetCompatibleMethodsInClass (cls, (CodeMemberMethod) testMember);
			
			return new string[0];
		}
		
		
		public static bool IdentifierExistsInClass (IClass cls, string identifier)
		{
			bool found = false;
			
			foreach (IMethod method in cls.Methods)
				if (method.Name == identifier)
					found = true;
			
			foreach (IProperty property in cls.Properties)
				if (property.Name == identifier)
					found = true;
			
			foreach (IEvent ev in cls.Events)
				if (ev.Name == identifier)
					found = true;
			
			foreach (IField field in cls.Fields)
				if (field.Name == identifier)
					found = true;
			
			return found;
		}
		
		
		public static string GenerateIdentifierUniqueInClass (IClass cls, string trialIdentifier)
		{
			string trialValue = trialIdentifier;
			
			for (int suffix = 1; suffix <= int.MaxValue; suffix++)
			{
				if (!IdentifierExistsInClass (cls, trialValue))
					return trialValue;
				
				trialValue = trialIdentifier + suffix.ToString ();
			}
			
			throw new Exception ("Tried identifiers up to " + trialValue + " and all already existed");
		}
		
		
		//opens the code view with the desired method, creating it if it doesn't already exist
		public static void CreateAndShowMember (IClass cls, CodeTypeMember member)
		{
			//only adds the method if it doesn't already exist
			IMember mem = AddMemberToClass (cls, member, false);
			
			//FIXME: code refactorer returns a blank mem.Region.FileName and negative beginLine, so we can only jump to the class
			if (string.IsNullOrEmpty (mem.Region.FileName))
				Gtk.Application.Invoke ( delegate {
					IdeApp.Workbench.OpenDocument (cls.Region.FileName, cls.Region.BeginLine, 1, true);
				});
			else
				Gtk.Application.Invoke ( delegate {
					IdeApp.Workbench.OpenDocument (mem.Region.FileName, mem.Region.BeginLine, 1, true);
				});
		}
		
		public static System.CodeDom.CodeTypeMember ReflectionToCodeDomMember (MemberInfo memberInfo)
		{
			if (memberInfo is MethodInfo)
				return ReflectionToCodeDomMethod ((MethodInfo) memberInfo);
			
			throw new NotImplementedException ();
		}
		
		public static System.CodeDom.CodeMemberMethod ReflectionToCodeDomMethod (MethodInfo mi)
		{
			CodeMemberMethod newMethod = new CodeMemberMethod ();
			newMethod.Name = mi.Name;
			newMethod.ReturnType = new System.CodeDom.CodeTypeReference (mi.ReturnType.FullName);
			
			newMethod.Attributes = System.CodeDom.MemberAttributes.Private;
			switch (mi.Attributes) {
				case System.Reflection.MethodAttributes.Assembly:
					newMethod.Attributes |= System.CodeDom.MemberAttributes.Assembly;
					break;
				case System.Reflection.MethodAttributes.FamANDAssem:
					newMethod.Attributes |= System.CodeDom.MemberAttributes.FamilyAndAssembly;
					break;
				case System.Reflection.MethodAttributes.Family:
					newMethod.Attributes |= System.CodeDom.MemberAttributes.Family;
					break;
				case System.Reflection.MethodAttributes.FamORAssem:
					newMethod.Attributes |= System.CodeDom.MemberAttributes.FamilyAndAssembly;
					break;
				case System.Reflection.MethodAttributes.Public:
					newMethod.Attributes |= System.CodeDom.MemberAttributes.Public;
					break;
				case System.Reflection.MethodAttributes.Static:
					newMethod.Attributes |= System.CodeDom.MemberAttributes.Static;
					break;
			}
			
			ParameterInfo[] pinfos = mi.GetParameters ();
			foreach (ParameterInfo pi in pinfos) {
				CodeParameterDeclarationExpression newPar = new CodeParameterDeclarationExpression (pi.ParameterType.FullName, pi.Name);
				if (pi.IsIn) newPar.Direction = FieldDirection.In;
				if (pi.IsOut) newPar.Direction = FieldDirection.Out;
				newMethod.Parameters.Add (newPar);
			}
			
			return newMethod;
		}
	}
}
