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

// TODO: Roslyn port. (Maybe move to the ASP.NET binding).
using System;
using System.CodeDom;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.DesignerSupport
{
	
	
	public static class BindingService
	{
		//TODO: currently case-sensitive, so some languages may not like this
		const bool ignoreCase = false;

//		public static IUnresolvedMember GetCompatibleMemberInClass (ICompilation ctx, ITypeDefinition cls, CodeTypeMember member)
//		{
//			// TODO Type system conversion
////			//check for identical property names
////			foreach (var prop in cls.Properties) {
////				if (string.Compare (prop.Name, member.Name, StringComparison.OrdinalIgnoreCase) == 0) {
////					string rt = prop.ReturnType.ReflectionName;
////					EnsureClassExists (ctx, rt, GetValidRegion (prop));
////					CodeMemberProperty memProp = member as CodeMemberProperty;
////					if (memProp == null || !IsTypeCompatible (ctx, rt, memProp.Type.BaseType))
////						throw new MemberExistsException (cls.FullName, MemberType.Property, member, GetValidRegion (prop), cls.Region.FileName);
////					return prop;
////				}
////			}
////				
////			//check for identical method names
////			foreach (var meth in cls.Methods) {
////				if (string.Compare (meth.Name, member.Name, StringComparison.OrdinalIgnoreCase) == 0) {
////					string rt = meth.ReturnType.ReflectionName;
////					EnsureClassExists (ctx, rt, GetValidRegion (meth));
////					CodeMemberMethod memMeth = member as CodeMemberMethod;
////					if (memMeth == null || !IsTypeCompatible (ctx, rt, memMeth.ReturnType.BaseType))
////						throw new MemberExistsException (cls.FullName, MemberType.Method, member, GetValidRegion (meth), cls.Region.FileName);
////					return meth;
////				}
////			}
////			
////			//check for identical event names
////			foreach (var ev in cls.Events) {
////				if (string.Compare (ev.Name, member.Name, StringComparison.OrdinalIgnoreCase) == 0) {
////					string rt = ev.ReturnType.ReflectionName;
////					EnsureClassExists (ctx, rt, GetValidRegion (ev));
////					CodeMemberEvent memEv = member as CodeMemberEvent;
////					if (memEv == null || !IsTypeCompatible (ctx, rt, memEv.Type.BaseType))
////						throw new MemberExistsException (cls.FullName, MemberType.Event, member, GetValidRegion (ev), cls.Region.FileName);
////					return ev;
////				}
////			}
////				
////			//check for identical field names
////			foreach (var field in cls.Fields) {
////				if (string.Compare (field.Name, member.Name, StringComparison.OrdinalIgnoreCase) == 0) {
////					string rt = field.ReturnType.ReflectionName;
////					EnsureClassExists (ctx, rt, GetValidRegion (field));
////					CodeMemberField memField = member as CodeMemberField;
////					if (memField == null || !IsTypeCompatible (ctx, rt, memField.Type.BaseType))
////						throw new MemberExistsException (cls.FullName, MemberType.Field, member, GetValidRegion (field), cls.Region.FileName);
////					return field;
////				}
////			}
////			
////			//walk down into base classes, if any
////			foreach (var baseType in cls.GetAllBaseTypeDefinitions ()) {
////				IMember mem = GetCompatibleMemberInClass (ctx, baseType, member);
////				if (mem != null)
////					return mem;
////			}
//			
//			//return null if no match
//			return null;
//		}
//		
//		static DomRegion GetValidRegion (IMember member)
//		{
//			if (member.BodyRegion.IsEmpty || member.DeclaringTypeDefinition.Region.FileName == FilePath.Null)
//				return member.DeclaringTypeDefinition.Region;
//			return member.BodyRegion;
//		}
//		
//		static DomRegion GetValidRegion (IUnresolvedMember member)
//		{
//			if (member.BodyRegion.IsEmpty || member.DeclaringTypeDefinition.Region.FileName == FilePath.Null)
//				return member.DeclaringTypeDefinition.Region;
//			return member.BodyRegion;
//		}
//		
//		static IType EnsureClassExists (ICompilation ctx, string className, DomRegion location)
//		{
//			string ns;
//			string name;
//			int idx = className.LastIndexOf (".");
//			if (idx < 0) {
//				ns = "";
//				name = className;
//			} else {
//				ns = className.Substring (0, idx);
//				name = className.Substring (idx + 1);
//			}
//			var cls = ctx.MainAssembly.GetTypeDefinition (ns, name, 0);
//			if (cls == null)
//				throw new TypeNotFoundException (className, location, null);
//			return cls;
//		}
//		
//		static bool IsTypeCompatible (ICompilation ctx, string existingType, string checkType)
//		{
//			if (existingType == checkType)
//				return true;
//			IType cls = EnsureClassExists (ctx, checkType, DomRegion.Empty);
//			foreach (var baseType in cls.GetAllBaseTypeDefinitions ()) {
//				if (IsTypeCompatible (ctx, existingType, baseType.FullName))
//				    return true;
//			}
//			return false;
//		}
//		
//		public static void AddMemberToClass (Project project, INamedTypeSymbol cls, Location specificPartToAffect, CodeTypeMember member, bool throwIfExists)
//		{
//			bool isChildClass = false;
//			foreach (var c in cls.Locations)
//				if (c == specificPartToAffect)
//					isChildClass = true;
//			if (!isChildClass)
//				throw new ArgumentException ("Class specificPartToAffect is not a part of class cls");
//			
//			var dom = TypeSystemService.GetCompilation (project);
//			var existingMember = GetCompatibleMemberInClass (dom, cls, member);
//			
//			if (existingMember == null)
//				return CodeGenerationService.AddCodeDomMember (project, specificPartToAffect, member);
//			
//			
//			if (throwIfExists)
//				throw new MemberExistsException (cls.Name, member, MemberType.Method, existingMember.BodyRegion, cls.Region.FileName);
//			
//			return existingMember;
//		}
//		
////		public static CodeRefactorer GetCodeGenerator (Project project)
////		{			
////			CodeRefactorer cr = new CodeRefactorer (project.ParentSolution);
////			cr.TextFileProvider = MonoDevelop.Ide.TextFileProvider.Instance;
////			return cr;
////		}
		
		public static IEnumerable<IMethodSymbol> GetCompatibleMethodsInClass (ITypeSymbol cls, IEventSymbol eve)
		{
			IMethodSymbol eveMeth = GetMethodSignature (eve);
			if (eveMeth == null)
				return new IMethodSymbol[0];
			return GetCompatibleMethodsInClass (cls, eveMeth);
		}

		static IEnumerable<INamedTypeSymbol> GetBaseTypes (ITypeSymbol type)
		{
			var current = type.BaseType;
			while (current != null) {
				yield return current;
				current = current.BaseType;
			}
		}
		
		//TODO: check accessibility
		public static IEnumerable<IMethodSymbol> GetCompatibleMethodsInClass (ITypeSymbol cls, IMethodSymbol matchMeth)
		{
			ITypeSymbol[] pars = new ITypeSymbol[matchMeth.Parameters.Length];
			List<ITypeSymbol>[] baseTypes = new List<ITypeSymbol>[matchMeth.Parameters.Length];
			for (int i = 0; i < matchMeth.Parameters.Length; i++) {
				pars[i] = matchMeth.Parameters[i].Type;
				baseTypes[i] = new List<ITypeSymbol> ( (GetBaseTypes (pars[i])));
			}

			var matchMethType = matchMeth.ReturnType;
			
			foreach (IMethodSymbol method in cls.GetAccessibleMembersInThisAndBaseTypes<IMethodSymbol> (cls)) {
				if (method.DeclaredAccessibility == Accessibility.Private || method.Parameters.Length != pars.Length || !method.ReturnType.Equals (matchMethType) 
					|| method.DeclaredAccessibility == Accessibility.Internal)
					continue;
				
				bool allCompatible = true;
				
				//compare each parameter
				for (int i = 0; i < pars.Length; i++) {
					var pt = method.Parameters[i].Type;
					bool parCompatible = pars[i].Equals (pt);
					if (!parCompatible && !baseTypes[i].Any (t => t.Equals (pt))) {
						allCompatible = false;
						break;
					}
				}
				
				if (allCompatible)
					yield return method;
			}
		}
		
		public static bool IdentifierExistsInClass (ITypeSymbol cls, string identifier)
		{
			return cls.GetMembers ().Any (m => m.Name == identifier);
		}
		
		public static string GenerateIdentifierUniqueInClass (ITypeSymbol cls, string trialIdentifier)
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
		
//		static DomRegion GetRegion (INamedElement el)
//		{
//			if (el is IEntity)
//				return ((IEntity)el).BodyRegion;
//			return ((IUnresolvedEntity)el).BodyRegion;
//		}
//		//opens the code view with the desired method, creating it if it doesn't already exist
//		public static void CreateAndShowMember (Project project, ITypeDefinition cls, IUnresolvedTypeDefinition specificPartToAffect, CodeTypeMember member)
//		{
//			//only adds the method if it doesn't already exist
//			var mem = AddMemberToClass (project, cls, specificPartToAffect, member, false);
//			
//			//some tests in case code refactorer returns bad values
//			int beginline = specificPartToAffect.BodyRegion.BeginLine;
//			var region = GetRegion (mem);
//			if (!region.IsEmpty && region.BeginLine >= beginline && region.BeginLine <= specificPartToAffect.BodyRegion.EndLine)
//				beginline = region.BeginLine;
//			
//			//jump to the member or class
//			IdeApp.Workbench.OpenDocument (specificPartToAffect.Region.FileName, beginline, 1);
//		}
//		
//		public static System.CodeDom.CodeTypeMember ReflectionToCodeDomMember (MemberInfo memberInfo)
//		{
//			if (memberInfo is MethodInfo)
//				return ReflectionToCodeDomMethod ((MethodInfo) memberInfo);
//			
//			throw new NotImplementedException ();
//		}
//		
//		public static System.CodeDom.CodeMemberMethod ReflectionToCodeDomMethod (MethodInfo mi)
//		{
//			CodeMemberMethod newMethod = new CodeMemberMethod ();
//			newMethod.Name = mi.Name;
//			newMethod.ReturnType = new System.CodeDom.CodeTypeReference (mi.ReturnType.FullName);
//			
//			newMethod.Attributes = System.CodeDom.MemberAttributes.Private;
//			switch (mi.Attributes) {
//				case System.Reflection.MethodAttributes.Assembly:
//					newMethod.Attributes |= System.CodeDom.MemberAttributes.Assembly;
//					break;
//				case System.Reflection.MethodAttributes.FamANDAssem:
//					newMethod.Attributes |= System.CodeDom.MemberAttributes.FamilyAndAssembly;
//					break;
//				case System.Reflection.MethodAttributes.Family:
//					newMethod.Attributes |= System.CodeDom.MemberAttributes.Family;
//					break;
//				case System.Reflection.MethodAttributes.FamORAssem:
//					newMethod.Attributes |= System.CodeDom.MemberAttributes.FamilyAndAssembly;
//					break;
//				case System.Reflection.MethodAttributes.Public:
//					newMethod.Attributes |= System.CodeDom.MemberAttributes.Public;
//					break;
//				case System.Reflection.MethodAttributes.Static:
//					newMethod.Attributes |= System.CodeDom.MemberAttributes.Static;
//					break;
//			}
//			
//			ParameterInfo[] pinfos = mi.GetParameters ();
//			foreach (ParameterInfo pi in pinfos) {
//				CodeParameterDeclarationExpression newPar = new CodeParameterDeclarationExpression (pi.ParameterType.FullName, pi.Name);
//				if (pi.IsIn) newPar.Direction = FieldDirection.In;
//				else if (pi.IsOut) newPar.Direction = FieldDirection.Out;
//				newMethod.Parameters.Add (newPar);
//			}
//			
//			return newMethod;
//		}
		
		public static IMethodSymbol GetMethodSignature (IEventSymbol ev)
		{
			if (ev.Type == null)
				return null;
			ITypeSymbol cls = ev.Type;
			if (cls.TypeKind == TypeKind.Unknown)
				return null;
			foreach (var m in cls.GetAccessibleMembersInThisAndBaseTypes<IMethodSymbol> (cls))
				if (m.Name == "Invoke")
					return m;
			return null;
		}
		
//		//TODO: handle generics
//		public static IUnresolvedMethod CodeDomToMDDomMethod (CodeMemberMethod method)
//		{
//			var meth = new DefaultUnresolvedMethod (null, method.Name);
//			meth.ReturnType = new DefaultUnresolvedTypeDefinition (method.ReturnType.BaseType);
//			
//			CodeDomModifiersToMDDom (meth, method.Attributes);
//			
//			foreach (CodeParameterDeclarationExpression dec in method.Parameters) {
//				var paramType = new DefaultUnresolvedTypeDefinition (dec.Type.BaseType);
//				var par = new  DefaultUnresolvedParameter (paramType, dec.Name);
//				if (dec.Direction == FieldDirection.Ref)
//					par.IsRef = true;
//				else if (dec.Direction == FieldDirection.Out)
//					par.IsOut = true;
//				meth.Parameters.Add (par);
//			}
//			
//			return meth;
//		}

		public static CodeMemberMethod MDDomToCodeDomMethod (IEventSymbol eve)
		{
			IMethodSymbol meth = GetMethodSignature (eve);
			return meth != null ? MDDomToCodeDomMethod (meth) : null;
		}
		
//		static void CodeDomModifiersToMDDom (DefaultUnresolvedMethod method, MemberAttributes modifiers)
//		{
//			if ((modifiers & MemberAttributes.FamilyOrAssembly) != 0) {
//				method.Accessibility = Accessibility.ProtectedOrInternal;
//			}
//			else if ((modifiers & MemberAttributes.FamilyOrAssembly) != 0) {
//				method.Accessibility = Accessibility.ProtectedAndInternal;
//			}
//			else if ((modifiers & MemberAttributes.Family) != 0) {
//				method.Accessibility = Accessibility.Protected;
//			}
//			else if ((modifiers & MemberAttributes.Assembly) != 0) {
//				method.Accessibility = Accessibility.Internal;
//			}
//			else if ((modifiers & MemberAttributes.Public) != 0) {
//				method.Accessibility = Accessibility.Public;
//			}
//			else  if ((modifiers & MemberAttributes.Private) != 0) {
//				method.Accessibility = Accessibility.Private;
//			}
//			
//			if ((modifiers & MemberAttributes.Abstract) != 0) {
//				method.IsAbstract = true;
//			}
//			else if ((modifiers & MemberAttributes.Final) != 0) {
//				method.IsSealed = true;
//			}
//			else if ((modifiers & MemberAttributes.Static) != 0) {
//				method.IsStatic = true;
//			}
//			else if ((modifiers & MemberAttributes.Override) != 0) {
//				method.IsOverride = true;
//			}
////			else if ((modifiers & MemberAttributes.Const) != 0) { // methods are never const.
////				initialState = (initialState & ~ScopeMask) | Modifiers.Const;
////			}
//		}
		
		static MemberAttributes ApplyMDDomModifiersToCodeDom (ISymbol entity, MemberAttributes initialState)
		{
			switch (entity.DeclaredAccessibility) {
			case Accessibility.ProtectedOrInternal:
				initialState = (initialState & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyOrAssembly;
				break;
			case Accessibility.ProtectedAndInternal:
				initialState = (initialState & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyAndAssembly;
				break;
			case Accessibility.Protected:
				initialState = (initialState & ~MemberAttributes.AccessMask) | MemberAttributes.Family;
				break;
			case Accessibility.Internal:
				initialState = (initialState & ~MemberAttributes.AccessMask) | MemberAttributes.Assembly;
				break;
			case Accessibility.Public:
				initialState = (initialState & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
				break;
			case Accessibility.Private:
				initialState = (initialState & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
				break;
			}
			
			
			if (entity.IsAbstract) {
				initialState = (initialState & ~MemberAttributes.ScopeMask) | MemberAttributes.Abstract;
			}
			else if (entity.IsSealed) {
				initialState = (initialState & ~MemberAttributes.ScopeMask) | MemberAttributes.Final;
			}
			else if (entity.IsStatic) {
				initialState = (initialState & ~MemberAttributes.ScopeMask) | MemberAttributes.Static;
			}
			else if (entity.IsOverride) {
				initialState = (initialState & ~MemberAttributes.ScopeMask) | MemberAttributes.Override;
			}
			if (entity is IFieldSymbol) {
				var f = (IFieldSymbol)entity;
				if (f.IsReadOnly && f.IsStatic)
					initialState = (initialState & ~MemberAttributes.ScopeMask) | MemberAttributes.Const;
			}
			
			return initialState;
		}
		
		
		public static System.CodeDom.CodeMemberMethod MDDomToCodeDomMethod (IMethodSymbol mi)
		{
			CodeMemberMethod newMethod = new CodeMemberMethod ();
			newMethod.Name = mi.Name;
			string returnType = mi.ReturnType.GetFullMetadataName () ?? "System.Void";
			newMethod.ReturnType = new System.CodeDom.CodeTypeReference (returnType);
			newMethod.Attributes = ApplyMDDomModifiersToCodeDom (mi, newMethod.Attributes);
			
			foreach (IParameterSymbol p in mi.Parameters) {
				var newPar = new CodeParameterDeclarationExpression (returnType, p.Name);
				if (p.RefKind == RefKind.Ref)
					newPar.Direction = FieldDirection.Ref;
				else if (p.RefKind == RefKind.Out)
					newPar.Direction = FieldDirection.Out;
				else
					newPar.Direction = FieldDirection.In;
				
				newMethod.Parameters.Add (newPar);
			}
			
			return newMethod;
		}
	}
}
