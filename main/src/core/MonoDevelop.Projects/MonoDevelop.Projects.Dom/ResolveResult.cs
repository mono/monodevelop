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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom
{
	public abstract class ResolveResult
	{
		IType       callingType;
		IMember     callingMember;
		
		IReturnType resolvedType;
		bool        staticResolve = false;
		
		public IType CallingType {
			get {
				return callingType;
			}
			set {
				callingType = value;
			}
		}

		public IReturnType ResolvedType {
			get {
				return resolvedType;
			}
			set {
				resolvedType = value;
			}
		}

		public IMember CallingMember {
			get {
				return callingMember;
			}
			set {
				callingMember = value;
			}
		}

		public bool StaticResolve {
			get {
				return staticResolve;
			}
			set {
				staticResolve = value;
			}
		}
		
		public ResolveResult ()
		{
		}
		public ResolveResult (bool staticResolve)
		{
			this.staticResolve = staticResolve;
		}
		
		public abstract IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember);
	}
	
	public class LocalVariableResolveResult : ResolveResult
	{
		LocalVariable variable;
		
		bool   isLoopVariable;
		
		public LocalVariable LocalVariable {
			get {
				return variable;
			}
		}
		
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
		IType anonymousType;
		
		public IType AnonymousType {
			get {
				return anonymousType;
			}
			set {
				anonymousType = value;
			}
		}
		
		public AnonymousTypeResolveResult (IType anonymousType)
		{
			this.anonymousType = anonymousType; 
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
		IMember resolvedMember;
		public IMember ResolvedMember {
			get {
				return resolvedMember;
			}
			set {
				resolvedMember = value;
			}
		}
		
		public MemberResolveResult (IMember resolvedMember)
		{
			this.resolvedMember = resolvedMember;
		}
		
		public MemberResolveResult (IMember resolvedMember, bool staticResolve) : base (staticResolve)
		{
			this.resolvedMember = resolvedMember;
		}
		
		internal static void AddType (ProjectDom dom, List<object> result, IType type, IMember callingMember, bool showStatic)
		{
			if (type == null)
				return;
			if (type.ClassType == ClassType.Enum) {
				foreach (IMember member in type.Fields) {
					result.Add (member);
				}
				return;
			}
			foreach (IType curType in dom.GetInheritanceTree (type)) {
				foreach (IMember member in curType.Members) {
					if (callingMember != null && !member.IsAccessibleFrom (dom, callingMember))
						continue;
					if (member is IMethod && ((IMethod)member).IsConstructor)
						continue;
					if (member is IType || !(showStatic ^ (member.IsStatic || member.IsConst)))
						result.Add (member);
				}
			}
		}
		
		internal static void AddType (ProjectDom dom, List<object> result, IReturnType returnType, IMember callingMember, bool showStatic)
		{
			if (returnType.ArrayDimensions > 0) {
				AddType (dom, result, dom.GetType ("System.Array", null, true, true), callingMember, showStatic);
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
		
		public ReadOnlyCollection<IMethod> Methods {
			get {
				return methods.AsReadOnly ();
			}
		}
		
		public MethodResolveResult (List<IMember> members)
		{
			foreach (IMember member in members) {
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
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom, IMember callingMember)
		{
			List<object> result = new List<object> ();
			if (CallingMember != null && !CallingMember.IsStatic)
				MemberResolveResult.AddType (dom, result, CallingType.BaseType, callingMember, StaticResolve);
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
