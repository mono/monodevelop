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
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport
{
	
	
	public static class BindingService
	{
		//TODO: currently case-sensitive, so some languages may not like this
		const bool ignoreCase = false;
		
		public static IMember GetCompatibleMemberInClass (ProjectDom ctx, IType cls, CodeTypeMember member)
		{
			//check for identical property names
			foreach (IProperty prop in cls.Properties) {
				if (string.Compare (prop.Name, member.Name, ignoreCase) == 0) {
					EnsureClassExists (ctx, prop.ReturnType.FullName, GetValidRegion (prop));
					CodeMemberProperty memProp = member as CodeMemberProperty;
					if (memProp == null || !IsTypeCompatible (ctx, prop.ReturnType.FullName, memProp.Type.BaseType))
						throw new MemberExistsException (cls.FullName, MemberType.Property, member, GetValidRegion (prop), cls.CompilationUnit.FileName);
					return prop;
				}
			}
				
			//check for identical method names
			foreach (IMethod meth in cls.Methods) {
				if (string.Compare (meth.Name, member.Name, ignoreCase) == 0) {
					EnsureClassExists (ctx, meth.ReturnType.FullName, GetValidRegion (meth));
					CodeMemberMethod memMeth = member as CodeMemberMethod;
					if (memMeth == null || !IsTypeCompatible (ctx, meth.ReturnType.FullName, memMeth.ReturnType.BaseType))
						throw new MemberExistsException (cls.FullName, MemberType.Method, member, GetValidRegion (meth), cls.CompilationUnit.FileName);
					return meth;
				}
			}
			
			//check for identical event names
			foreach (IEvent ev in cls.Events) {
				if (string.Compare (ev.Name, member.Name, ignoreCase) == 0) {
					EnsureClassExists (ctx, ev.ReturnType.FullName, GetValidRegion (ev));
					CodeMemberEvent memEv = member as CodeMemberEvent;
					if (memEv == null || !IsTypeCompatible (ctx, ev.ReturnType.FullName, memEv.Type.BaseType))
						throw new MemberExistsException (cls.FullName, MemberType.Event, member, GetValidRegion (ev), cls.CompilationUnit.FileName);
					return ev;
				}
			}
				
			//check for identical field names
			foreach (IField field in cls.Fields) {
				if (string.Compare (field.Name, member.Name, ignoreCase) == 0) {
					EnsureClassExists (ctx, field.ReturnType.FullName, GetValidRegion (field));
					CodeMemberField memField = member as CodeMemberField;
					if (memField == null || !IsTypeCompatible (ctx, field.ReturnType.FullName, memField.Type.BaseType))
						throw new MemberExistsException (cls.FullName, MemberType.Field, member, GetValidRegion (field), cls.CompilationUnit.FileName);
					return field;
				}
			}
			
			//walk down into base classes, if any
			foreach (IReturnType baseType in cls.BaseTypes) {
				IType c = ctx.GetType (baseType);
				if (c == null)
					throw new TypeNotFoundException (baseType.FullName, cls.BodyRegion, cls.CompilationUnit.FileName);
				IMember mem = GetCompatibleMemberInClass (ctx, c, member);
				if (mem != null)
					return mem;
			}
			
			//return null if no match
			return null;
		}
		
		static DomRegion GetValidRegion (IMember member)
		{
			if (member.BodyRegion == null || member.DeclaringType.CompilationUnit.FileName == null)
				return member.DeclaringType.BodyRegion;
			return member.BodyRegion;
		}
		
		static IType EnsureClassExists (ProjectDom ctx, string className, DomRegion location)
		{
			IType cls = ctx.GetType (className);
			if (cls == null)
				throw new TypeNotFoundException (className, location, null);
			return cls;
		}
		
		static bool IsTypeCompatible (ProjectDom ctx, string existingType, string checkType)
		{
			if (existingType == checkType)
				return true;
			IType cls = EnsureClassExists (ctx, checkType, DomRegion.Empty);
			foreach (IReturnType baseType in cls.BaseTypes) {
				if (IsTypeCompatible (ctx, existingType, baseType.FullName))
				    return true;
			}
			return false;
		}
		
		public static IMember AddMemberToClass (Project project, IType cls, IType specificPartToAffect, CodeTypeMember member, bool throwIfExists)
		{
			bool isChildClass = false;
			foreach (IType c in cls.Parts)
				if (c == specificPartToAffect)
					isChildClass = true;
			if (!isChildClass)
				throw new ArgumentException ("Class specificPartToAffect is not a part of class cls");
			
			ProjectDom dom = ProjectDomService.GetProjectDom (project);
			IMember existingMember = GetCompatibleMemberInClass (dom, cls, member);
			
			if (existingMember == null)
				return GetCodeGenerator (project).AddMember (specificPartToAffect, member);
			
			if (throwIfExists)
				throw new MemberExistsException (cls.Name, member, MemberType.Method, existingMember.BodyRegion, cls.CompilationUnit.FileName);
			
			return existingMember;
		}
		
		public static CodeRefactorer GetCodeGenerator (Project project)
		{			
			CodeRefactorer cr = new CodeRefactorer (project.ParentSolution);
			cr.TextFileProvider = OpenDocumentFileProvider.Instance;
			return cr;
		}
		
		//TODO: check accessibility
		public static string[] GetCompatibleMethodsInClass (IType cls, CodeMemberMethod testMethod)
		{
			List<string> list = new List<string> ();
			
			foreach (IMethod method in cls.Methods) {
				if (method.Parameters.Count != testMethod.Parameters.Count)
					continue;
				
				if (method.ReturnType.FullName != testMethod.ReturnType.BaseType)
					continue;
				
				//compare each parameter
				bool mismatch = false;
				for (int i = 0; i < testMethod.Parameters.Count; i++)
					if (method.Parameters[i].ReturnType.FullName != testMethod.Parameters[i].Type.BaseType)
						mismatch = true;
				
				if (!mismatch)
					list.Add (method.Name);
			}
			
			return list.ToArray ();
		}
		
		
		public static string[] GetCompatibleMembersInClass (IType cls, CodeTypeMember testMember)
		{
			if (testMember is CodeMemberMethod)
				return GetCompatibleMethodsInClass (cls, (CodeMemberMethod) testMember);
			
			return new string[0];
		}
		
		
		public static bool IdentifierExistsInClass (ProjectDom parserContext, IType cls, string identifier)
		{
			foreach (IMember member in cls.Members)
				if (member.Name == identifier)
					return true;
			
			return VisibleIdentifierExistsInBaseClasses (parserContext.GetInheritanceTree (cls), identifier);
		}
		
		static bool VisibleIdentifierExistsInBaseClasses (IEnumerable<IType> classes, string identifier)
		{
			foreach (IType cls in classes) {
				foreach (IEnumerable en in new IEnumerable[] {cls.Methods, cls.Properties, cls.Events, cls.Fields})
					foreach (IMember item in en)
						if (!item.IsPrivate && item.Name == identifier)
							return true;
				foreach (IType innerClass in cls.InnerTypes)
					if (!innerClass.IsPrivate && innerClass.Name == identifier)
						return true;
			}
			return false;
		}
		
		public static string GenerateIdentifierUniqueInClass (ProjectDom parserContext, IType cls, string trialIdentifier)
		{
			string trialValue = trialIdentifier;
			
			for (int suffix = 1; suffix <= int.MaxValue; suffix++)
			{
				if (!IdentifierExistsInClass (parserContext, cls, trialValue))
					return trialValue;
				
				trialValue = trialIdentifier + suffix.ToString ();
			}
			
			throw new Exception ("Tried identifiers up to " + trialValue + " and all already existed");
		}
		
		
		//opens the code view with the desired method, creating it if it doesn't already exist
		public static void CreateAndShowMember (Project project, IType cls, IType specificPartToAffect, CodeTypeMember member)
		{
			//only adds the method if it doesn't already exist
			IMember mem = AddMemberToClass (project, cls, specificPartToAffect, member, false);
			
			//some tests in case code refactorer returns bad values
			int beginline = specificPartToAffect.Location.Line;			
			if (!mem.BodyRegion.IsEmpty && mem.BodyRegion.Start.Line >= beginline && mem.Location.Line <= specificPartToAffect.BodyRegion.End.Line)
				beginline = mem.BodyRegion.Start.Line;
			
			//jump to the member or class
			IdeApp.Workbench.OpenDocument (specificPartToAffect.CompilationUnit.FileName, beginline, 1, true);
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
				else if (pi.IsOut) newPar.Direction = FieldDirection.Out;
				newMethod.Parameters.Add (newPar);
			}
			
			return newMethod;
		}
		
		public static System.CodeDom.CodeMemberMethod MDDomToCodeDomMethod (IEvent ev, ProjectDom context)
		{
			if (ev.ReturnType == null)
				return null;
			IType cls = context.GetType (ev.ReturnType);
			if (cls == null)
				return null;
			foreach (IMethod m in cls.Methods)
				if (m.Name == "Invoke")
					return MDDomToCodeDomMethod (m);
			return null;
		}
		
		public static System.CodeDom.CodeMemberMethod MDDomToCodeDomMethod (IMethod mi)
		{
			CodeMemberMethod newMethod = new CodeMemberMethod ();
			newMethod.Name = mi.Name;
			newMethod.ReturnType = new System.CodeDom.CodeTypeReference (mi.ReturnType.FullName);
			
			newMethod.Attributes = System.CodeDom.MemberAttributes.Private;
			switch (mi.Modifiers) {
			case Modifiers.Internal:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.Assembly;
				break;
			case Modifiers.ProtectedAndInternal:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.FamilyAndAssembly;
				break;
			case Modifiers.Protected:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.Family;
				break;
			case Modifiers.ProtectedOrInternal:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.FamilyAndAssembly;
				break;
			case Modifiers.Public:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.Public;
				break;
			case Modifiers.Static:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.Static;
				break;
			}
			
			foreach (IParameter p in mi.Parameters) {
				CodeParameterDeclarationExpression newPar = new CodeParameterDeclarationExpression (p.ReturnType.FullName, p.Name);
				if (p.IsRef) newPar.Direction = FieldDirection.Ref;
				else if (p.IsOut) newPar.Direction = FieldDirection.Out;
				else newPar.Direction = FieldDirection.In;
				
				newMethod.Parameters.Add (newPar);
			}
			
			return newMethod;
		}
	}
}
