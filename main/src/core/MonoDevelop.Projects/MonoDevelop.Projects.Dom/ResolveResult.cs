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
		
		//IMember     resolvedMember;
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
		
//		public IMember ResolvedMember {
//			get {
//				return resolvedMember;
//			}
//			set {
//				resolvedMember = value;
//			}
//		}
		
		public ResolveResult ()
		{
		}
		public ResolveResult (bool staticResolve)
		{
			this.staticResolve = staticResolve;
		}
		
		public abstract IEnumerable<object> CreateResolveResult (ProjectDom dom);
		

	}
	
	public class MemberResolveResult : ResolveResult
	{
		public MemberResolveResult ()
		{
		}
		public MemberResolveResult (bool staticResolve) : base (staticResolve)
		{
		}
		
		internal static void AddType (ProjectDom dom, List<object> result, IReturnType returnType, bool showStatic)
		{
			IType type = dom.GetType (returnType);
			if (type == null)
				return;
			foreach (IMember member in type.Members) {
				if (member is IType || !(showStatic ^ member.IsStatic))
					result.Add (member);
			}
			if (type.BaseType != null && type.FullName != "System.Object")
				AddType (dom, result, type.BaseType, showStatic);
		}
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom)
		{
			List<object> result = new List<object> ();
			AddType (dom, result, ResolvedType, StaticResolve);
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[MemberResolveResult: CallingType={0}, CallingMember={1}, ResolvedType={2}]",
			                      CallingType,
			                      CallingMember,
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
		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom)
		{
			return null;
		}
		
		public override string ToString ()
		{
			return String.Format ("[MethodResolveResult: #methods={0}]", methods.Count);
		}
	}
	
	public class ThisResolveResult : ResolveResult
	{
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom)
		{
			List<object> result = new List<object> ();
			MemberResolveResult.AddType (dom, result, new DomReturnType (CallingType), StaticResolve);
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[ThisResolveResult]");
		}
	}
	
	public class BaseResolveResult : ResolveResult
	{
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom)
		{
			List<object> result = new List<object> ();
			MemberResolveResult.AddType (dom, result, CallingType.BaseType, StaticResolve);
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

		
		public override IEnumerable<object> CreateResolveResult (ProjectDom dom)
		{
			List<object> result = new List<object> ();
			foreach (object o in dom.GetNamespaceContents (ns, true, true)) {
				result.Add (o);
			}
			return result;
		}
	}
}
