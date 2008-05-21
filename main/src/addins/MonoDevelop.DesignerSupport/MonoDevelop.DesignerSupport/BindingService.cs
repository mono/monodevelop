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

using MonoDevelop.Projects;
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
		
		private BindingService ()
		{
		}
		
		public static IMember GetCompatibleMemberInClass (IClass cls, CodeTypeMember member)
		{
			IParserContext ctx = IdeApp.Workspace.ParserDatabase.GetProjectParserContext ((MonoDevelop.Projects.Project) cls.SourceProject);
			return GetCompatibleMemberInClass (ctx, cls, member);
		}
		
		public static IMember GetCompatibleMemberInClass (IParserContext ctx, IClass cls, CodeTypeMember member)
		{
			//check for identical property names
			foreach (IProperty prop in cls.Properties) {
				if (string.Compare (prop.Name, member.Name, ignoreCase) == 0) {
					EnsureClassExists (ctx, prop.ReturnType.FullyQualifiedName, GetValidRegion (prop));
					CodeMemberProperty memProp = member as CodeMemberProperty;
					if (memProp == null || !IsTypeCompatible (ctx, prop.ReturnType.FullyQualifiedName, memProp.Type.BaseType))
						throw new MemberExistsException (cls.FullyQualifiedName, MemberType.Property, member, GetValidRegion (prop));
					return prop;
				}
			}
				
			//check for identical method names
			foreach (IMethod meth in cls.Methods) {
				if (string.Compare (meth.Name, member.Name, ignoreCase) == 0) {
					EnsureClassExists (ctx, meth.ReturnType.FullyQualifiedName, GetValidRegion (meth));
					CodeMemberMethod memMeth = member as CodeMemberMethod;
					if (memMeth == null || !IsTypeCompatible (ctx, meth.ReturnType.FullyQualifiedName, memMeth.ReturnType.BaseType))
						throw new MemberExistsException (cls.FullyQualifiedName, MemberType.Method, member, GetValidRegion (meth));
					return meth;
				}
			}
			
			//check for identical event names
			foreach (IEvent ev in cls.Events) {
				if (string.Compare (ev.Name, member.Name, ignoreCase) == 0) {
					EnsureClassExists (ctx, ev.ReturnType.FullyQualifiedName, GetValidRegion (ev));
					CodeMemberEvent memEv = member as CodeMemberEvent;
					if (memEv == null || !IsTypeCompatible (ctx, ev.ReturnType.FullyQualifiedName, memEv.Type.BaseType))
						throw new MemberExistsException (cls.FullyQualifiedName, MemberType.Event, member, GetValidRegion (ev));
					return ev;
				}
			}
				
			//check for identical field names
			foreach (IField field in cls.Fields) {
				if (string.Compare (field.Name, member.Name, ignoreCase) == 0) {
					EnsureClassExists (ctx, field.ReturnType.FullyQualifiedName, GetValidRegion (field));
					CodeMemberField memField = member as CodeMemberField;
					if (memField == null || !IsTypeCompatible (ctx, field.ReturnType.FullyQualifiedName, memField.Type.BaseType))
						throw new MemberExistsException (cls.FullyQualifiedName, MemberType.Field, member, GetValidRegion (field));
					return field;
				}
			}
			
			//walk down into base classes, if any
			foreach (IReturnType baseType in cls.BaseTypes) {
				IClass c = ctx.GetClass (baseType.FullyQualifiedName);
				if (c == null)
					throw new TypeNotFoundException (baseType.FullyQualifiedName, cls.Region);
				IMember mem = GetCompatibleMemberInClass (ctx, c, member);
				if (mem != null)
					return mem;
			}
			
			//return null if no match
			return null;
		}
		
		static IRegion GetValidRegion (IMember member)
		{
			if (member.Region.FileName == null)
				member.Region.FileName = member.DeclaringType.Region.FileName;
			return member.Region;
		}
		
		static IClass EnsureClassExists (IParserContext ctx, string className, IRegion location)
		{
			IClass cls = ctx.GetClass (className);
			if (cls == null)
				throw new TypeNotFoundException (className, location);
			return cls;
		}
		
		static bool IsTypeCompatible (IParserContext ctx, string existingType, string checkType)
		{
			if (existingType == checkType)
				return true;
			IClass cls = EnsureClassExists (ctx, checkType, null);
			foreach (IReturnType baseType in cls.BaseTypes) {
				if (IsTypeCompatible (ctx, existingType, baseType.FullyQualifiedName))
				    return true;
			}
			return false;
		}
		
		public static IMember AddMemberToClass (SolutionItem entry, IClass cls, CodeTypeMember member, bool throwIfExists)
		{
			return AddMemberToClass (entry, cls, cls.Parts[0], member, throwIfExists);
		}
		
		public static IMember AddMemberToClass (SolutionItem entry, IClass cls, IClass specificPartToAffect, CodeTypeMember member, bool throwIfExists)
		{
			bool isChildClass = false;
			foreach (IClass c in cls.Parts)
				if (c == specificPartToAffect)
					isChildClass = true;
			if (!isChildClass)
				throw new ArgumentException ("Class specificPartToAffect is not a part of class cls");
			
			IMember existingMember = GetCompatibleMemberInClass (cls, member);
			
			if (existingMember == null)
				return GetCodeGenerator (entry).AddMember (specificPartToAffect, member);
			
			if (throwIfExists)
				throw new MemberExistsException (cls.Name, member, MemberType.Method, existingMember.Region);
			
			return existingMember;
		}
		
		public static CodeRefactorer GetCodeGenerator (SolutionItem entry)
		{			
			CodeRefactorer cr = new CodeRefactorer (entry.ParentSolution, IdeApp.Workspace.ParserDatabase);
			cr.TextFileProvider = OpenDocumentFileProvider.Instance;
			return cr;
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
		public static void CreateAndShowMember (SolutionItem project, IClass cls, CodeTypeMember member)
		{
			//only adds the method if it doesn't already exist
			IMember mem = AddMemberToClass (project, cls, member, false);
			
			//some tests in case code refactorer returns bad values
			int beginline = cls.Region.BeginLine;			
			if (mem.Region != null && mem.Region.BeginLine >= beginline && mem.Region.BeginLine <= cls.Region.EndLine)
				beginline = mem.Region.BeginLine;
			
			//jump to the member or class
			IdeApp.Workbench.OpenDocument (cls.Region.FileName, beginline, 1, true);
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
		
		public static System.CodeDom.CodeMemberMethod MDDomToCodeDomMethod (IEvent ev, IParserContext context)
		{
			IClass cls = context.GetClass (ev.ReturnType.FullyQualifiedName, ev.ReturnType.GenericArguments, true, false);
			foreach(IMethod m in cls.Methods)
				if (m.Name == "Invoke")
					return MDDomToCodeDomMethod (m);
			return null;
		}
		
		public static System.CodeDom.CodeMemberMethod MDDomToCodeDomMethod (IMethod mi)
		{
			CodeMemberMethod newMethod = new CodeMemberMethod ();
			newMethod.Name = mi.Name;
			newMethod.ReturnType = new System.CodeDom.CodeTypeReference (mi.ReturnType.FullyQualifiedName);
			
			newMethod.Attributes = System.CodeDom.MemberAttributes.Private;
			switch (mi.Modifiers) {
			case ModifierEnum.Internal:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.Assembly;
				break;
			case ModifierEnum.ProtectedAndInternal:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.FamilyAndAssembly;
				break;
			case ModifierEnum.Protected:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.Family;
				break;
			case ModifierEnum.ProtectedOrInternal:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.FamilyAndAssembly;
				break;
			case ModifierEnum.Public:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.Public;
				break;
			case ModifierEnum.Static:
				newMethod.Attributes |= System.CodeDom.MemberAttributes.Static;
				break;
			}
			
			foreach (IParameter p in mi.Parameters) {
				CodeParameterDeclarationExpression newPar = new CodeParameterDeclarationExpression (p.ReturnType.FullyQualifiedName, p.Name);
				if (p.IsRef) newPar.Direction = FieldDirection.Ref;
				else if (p.IsOut) newPar.Direction = FieldDirection.Out;
				else newPar.Direction = FieldDirection.In;
				
				newMethod.Parameters.Add (newPar);
			}
			
			return newMethod;
		}
	}
}
