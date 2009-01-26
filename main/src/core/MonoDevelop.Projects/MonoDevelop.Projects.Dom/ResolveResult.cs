//
// ResolveResult.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom
{
	public abstract class ResolveResult
	{
		public IType CallingType {
			get;
			set;
		}

		public virtual IReturnType ResolvedType {
			get;
			set;
		}
		public virtual IReturnType UnresolvedType {
			get;
			set;
		}

		public IMember CallingMember {
			get;
			set;
		}

		public bool StaticResolve {
			get;
			set;
		}

		public string ResolvedExpression {
			get;
			set;
		}
		
		public ResolveResult () : this (false)
		{
		}
		
		public ResolveResult (bool staticResolve)
		{
			this.StaticResolve = staticResolve;
		}
		
		public abstract IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember);
	}
	
	public class LocalVariableResolveResult : ResolveResult
	{
		LocalVariable variable;
		public LocalVariable LocalVariable {
			get {
				return variable;
			}
		}
		
		bool   isLoopVariable;
		public bool IsLoopVariable {
			get {
				return isLoopVariable;
			}
		}
		
		public LocalVariableResolveResult (LocalVariable variable) : this (variable, false)
		{
		}
		public LocalVariableResolveResult (LocalVariable variable, bool isLoopVariable)
		{
			this.variable       = variable;
			this.isLoopVariable = isLoopVariable;
		}
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			List<object> result = new List<object> ();
			if (IsLoopVariable) {
				if (ResolvedType.Name == "IEnumerable" && ResolvedType.GenericArguments != null && ResolvedType.GenericArguments.Count > 0) {
					MemberResolveResult.AddType (dom, result, ResolvedType.GenericArguments [0], callingMember, StaticResolve);
				} else if (ResolvedType.Name == "IEnumerable") {
					MemberResolveResult.AddType (dom, result, DomReturnType.Object, callingMember, StaticResolve);
				} else { 
					MemberResolveResult.AddType (dom, result, dom.GetType (ResolvedType), callingMember, StaticResolve);
				}
			} else {
				MemberResolveResult.AddType (dom, result, ResolvedType, callingMember, StaticResolve);
			}
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[LocalVariableResolveResult: LocalVariable={0}, ResolvedType={1}]", LocalVariable, ResolvedType);
		}
	}
	
	public class ParameterResolveResult : ResolveResult
	{
		IParameter parameter;
		public IParameter Parameter {
			get {
				return parameter;
			}
		}
		
		public ParameterResolveResult (IParameter parameter)
		{
			this.parameter = parameter;
		}
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			List<object> result = new List<object> ();
			MemberResolveResult.AddType (dom, result, ResolvedType, callingMember, StaticResolve);
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[ParameterResolveResult: Parameter={0}]", Parameter);
		}
	}
	
	public class AnonymousTypeResolveResult : ResolveResult
	{
		public IType AnonymousType {
			get;
			set;
		}
		
		public AnonymousTypeResolveResult (IType anonymousType)
		{
			this.AnonymousType = anonymousType; 
			this.ResolvedType  = new DomReturnType (anonymousType);
		}
		
		public override string ToString ()
		{
			return String.Format ("[AnonymousTypeResolveResult: AnonymousType={0}]", AnonymousType);
		}
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			foreach (IMember member in AnonymousType.Members) {
				yield return member;
			}
		}
	}
	
	public class MemberResolveResult : ResolveResult
	{
		public IMember ResolvedMember {
			get;
			set;
		}
		
		public MemberResolveResult (IMember resolvedMember)
		{
			this.ResolvedMember = resolvedMember;
		}
		
		public MemberResolveResult (IMember resolvedMember, bool staticResolve) : base (staticResolve)
		{
			this.ResolvedMember = resolvedMember;
		}
		
		internal static void AddType (ProjectDom dom, List<object> result, IType type, IMember callingMember, bool showStatic)
		{
		//	System.Console.WriteLine("Add Type:" + type);
			if (type == null)
				return;
			
			if (showStatic && type.ClassType == ClassType.Enum) {
				foreach (IMember member in type.Fields) {
					result.Add (member);
				}
				return;
			}
			List<IType> accessibleStaticTypes = null;
			if (callingMember != null && callingMember.DeclaringType != null)
				accessibleStaticTypes = DomType.GetAccessibleExtensionTypes (dom, callingMember.DeclaringType.CompilationUnit);
/* TODO: Typed extension methods
			IList<IReturnType> genericParameters = null;
			if (type is InstantiatedType) 
				genericParameters = ((InstantiatedType)type).GenericParameters;*/

			bool includeProtected = callingMember != null ? DomType.IncludeProtected (dom, type, callingMember.DeclaringType) : false;
			
			
			foreach (IType curType in dom.GetInheritanceTree (type)) {
				if (curType.ClassType == ClassType.Interface && type.ClassType != ClassType.Interface)
					continue;
				if (accessibleStaticTypes != null) {
					foreach (IMethod extensionMethod in curType.GetExtensionMethods (accessibleStaticTypes)) {
						result.Add (extensionMethod);
					}
				}
				foreach (IMember member in curType.Members) {
					if (callingMember != null && !member.IsAccessibleFrom (dom, type, callingMember, includeProtected))
						continue;
					if (member.IsProtected && !includeProtected)
						continue;
					if (member is IMethod && (((IMethod)member).IsConstructor || ((IMethod)member).IsFinalizer))
						continue;
					if (!showStatic && member is IType)
						continue;
					if (member is IType || !(showStatic ^ (member.IsStatic || member.IsConst))) {
						result.Add (member);
					}
				}
			//	if (showStatic)
			//		break;
			}
		}
		
		internal static void AddType (ProjectDom dom, List<object> result, IReturnType returnType, IMember callingMember, bool showStatic)
		{
			if (returnType == null || returnType.FullName == "System.Void")
				return;
			if (returnType.ArrayDimensions > 0) {
				AddType (dom, result, dom.GetType ("System.Array", null, true, true), callingMember, showStatic);
				DomReturnType arrType = new DomReturnType (returnType.ToInvariantString ());
				AddType (dom, result, dom.GetType ("System.Collections.Generic.IList", new IReturnType [] { arrType }, true, true), callingMember, showStatic);
				return;
			}
			IType type = dom.GetType (returnType);
			
			AddType (dom, result, type, callingMember, showStatic);
		}
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			List<object> result = new List<object> ();
			AddType (dom, result, ResolvedType, callingMember, StaticResolve);
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[MemberResolveResult: CallingType={0}, CallingMember={1}, ResolvedMember={2}, ResolvedType={3}]",
			                      CallingType,
			                      CallingMember,
			                      ResolvedMember,
			                      ResolvedType);
		}
	}
	
	public class MethodResolveResult : ResolveResult
	{
		List<IMethod> methods = new List<IMethod> ();
		List<IReturnType> arguments = new List<IReturnType> ();
		List<IReturnType> genericArguments = new List<IReturnType> ();
		
		public ReadOnlyCollection<IMethod> Methods {
			get {
				return methods.AsReadOnly ();
			}
		}

		public IMethod MostLikelyMethod {
			get {
				if (methods.Count == 0)
					return null;
				IMethod result = methods [0];
				foreach (IMethod method in methods) {
					if (method.GenericParameters.Count == genericArguments.Count) {
					 	if (method.Parameters.Count == arguments.Count) {
					 		bool match = true;
					 		for (int i = 0; i < method.Parameters.Count; i++) {
					 			if (method.Parameters[i].ReturnType.FullName != arguments[i].FullName) {
					 				match = false;
					 			}
					 		}
					 		if (match)
					 			return method;
							result = method;
						}
					}
				}
				return result;
			}
		}
		
		public override IReturnType ResolvedType {
			get {
				IMethod method = MostLikelyMethod;
				if (method != null)
					return method.ReturnType;
				return base.ResolvedType;
			}
		}

		public ReadOnlyCollection<IReturnType> GenericArguments {
			get {
				return genericArguments.AsReadOnly ();
			}
		}
		public void AddGenericArgument (IReturnType arg)
		{
			genericArguments.Add (arg);
		}
		
		public ReadOnlyCollection<IReturnType> Arguments {
			get {
				return arguments.AsReadOnly ();
			}
		}
		public void AddArgument (IReturnType arg)
		{
			arguments.Add (arg);
		}
		
		public MethodResolveResult (List<IMember> members)
		{
			AddMethods (members);
		}

		public void AddMethods (IEnumerable members) 
		{
			foreach (object member in members) {
				if (member is IMethod)
					methods.Add ((IMethod)member);
			}			
		}
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			List<object> result = new List<object> ();
			MemberResolveResult.AddType (dom, result, ResolvedType, callingMember, StaticResolve);
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[MethodResolveResult: #methods={0}]", methods.Count);
		}
	}
	
	public class ThisResolveResult : ResolveResult
	{
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			List<object> result = new List<object> ();
			if (CallingMember != null && !CallingMember.IsStatic)
				MemberResolveResult.AddType (dom, result, new DomReturnType (CallingType), callingMember, StaticResolve);
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[ThisResolveResult]");
		}
	}
	
	public class BaseResolveResult : ResolveResult
	{
		internal class BaseMemberDecorator : DomMemberDecorator
		{
			IType fakeDeclaringType;
			public override IType DeclaringType {
				get {
					return fakeDeclaringType;
				}
			}
			public BaseMemberDecorator (IMember member, IType fakeDeclaringType) : base (member)
			{
				this.fakeDeclaringType = fakeDeclaringType;
			}
		}
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			List<object> result = new List<object> ();
			if (CallingMember != null && !CallingMember.IsStatic) {
				IType baseType = dom.SearchType (new SearchTypeRequest (CallingType.CompilationUnit, CallingType.BaseType ?? DomReturnType.Object, CallingType));
				MemberResolveResult.AddType (dom, result, baseType, new BaseMemberDecorator (CallingMember, baseType), StaticResolve);
			}
			return result;
		}
		public override string ToString ()
		{
			return String.Format ("[BaseResolveResult]");
		}
	}
	
	public class NamespaceResolveResult : ResolveResult
	{
		string ns;
		
		public string Namespace {
			get {
				return ns;
			}
		}
		
		public NamespaceResolveResult (string ns)
		{
			this.ns = ns;
		}
		
		public override string ToString ()
		{
			return String.Format ("[NamespaceResolveResult: Namespace={0}]", Namespace);
		}
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			List<object> result = new List<object> ();
			foreach (object o in dom.GetNamespaceContents (ns, true, true)) {
				result.Add (o);
			}
			return result;
		}
	}
}
