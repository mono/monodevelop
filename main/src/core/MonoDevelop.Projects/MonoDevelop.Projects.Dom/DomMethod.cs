//
// DomMethod.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom
{
	public class DomMethod : AbstractMember, IMethod
	{
		protected MethodModifier methodModifier;
		protected List<IParameter> parameters = null;
		protected List<IReturnType> genericParameters = null;

		static readonly ReadOnlyCollection<IParameter> emptyParameters = new ReadOnlyCollection<IParameter> (new IParameter [0]);
		static readonly ReadOnlyCollection<IReturnType> emptyGenericParameters = new ReadOnlyCollection<IReturnType> (new IReturnType [0]);

		public MethodModifier MethodModifier {
			get {
				return methodModifier;
			}
			set {
				methodModifier = value;
			}
		}
		
		public bool IsConstructor {
			get {
				return (methodModifier & MethodModifier.IsConstructor) == MethodModifier.IsConstructor;
			}
		}
		
		public bool IsExtension {
			get {
				return (methodModifier & MethodModifier.IsExtension) == MethodModifier.IsExtension;
			}
		}
		
		public bool IsFinalizer {
			get {
				return (methodModifier & MethodModifier.IsFinalizer) == MethodModifier.IsFinalizer;
			}
		}
		
		public virtual ReadOnlyCollection<IParameter> Parameters {
			get {
				return parameters != null ? parameters.AsReadOnly () : emptyParameters;
			}
		}
		
		public ReadOnlyCollection<IReturnType> GenericParameters {
			get {
				return genericParameters != null ? genericParameters.AsReadOnly () : emptyGenericParameters;
			}
		}
		
		// get's re-used for property parameters
		internal static void AppendHelpParameterList (StringBuilder result, ReadOnlyCollection<IParameter> parameters)
		{
			result.Append ('(');
			if (parameters != null) {
				for (int i = 0; i < parameters.Count; i++) {
					if (i > 0)
						result.Append (',');
					if (parameters[i].ReturnType == null) {
						result.Append ("System.Void");
					} else {
						result.Append (parameters[i].ReturnType.FullName);
					}
				}
			}
			result.Append (')');
		}
		
		public override string HelpUrl {
			get {
				StringBuilder result = new StringBuilder ();
				if (this.IsConstructor) {
					result.Append ("C:");
					result.Append (DeclaringType.FullName);
				} else {
					result.Append ("M:");
					result.Append (FullName);
				}
				AppendHelpParameterList (result, Parameters);
				return result.ToString ();
			}
		}
		
		static readonly string[] iconTable = {Stock.Method, Stock.PrivateMethod, Stock.ProtectedMethod, Stock.InternalMethod};
		public override string StockIcon {
			get {
				return iconTable [ModifierToOffset (Modifiers)];
			}
		}
		
		public DomMethod ()
		{
		}
		
		public DomMethod (string name, Modifiers modifiers, MethodModifier methodModifier, DomLocation location, DomRegion bodyRegion)
		{
			this.Name           = name;
			this.modifiers      = modifiers;
			this.location       = location;
			this.bodyRegion     = bodyRegion;
			this.methodModifier = methodModifier;
		}
		
		public DomMethod (string name, Modifiers modifiers, MethodModifier methodModifier, DomLocation location, DomRegion bodyRegion, IReturnType returnType)
		{
			this.Name           = name;
			this.modifiers      = modifiers;
			this.location       = location;
			this.bodyRegion     = bodyRegion;
			this.returnType     = returnType;
			this.methodModifier = methodModifier;
		}
/*
		public override bool IsAccessibleFrom (ProjectDom dom, IType calledType, IMember member)
		{
			return IsExtension || base.IsAccessibleFrom (dom, calledType, member);
		}*/
		
		static Dictionary<string, bool> extensionTable = new Dictionary<string, bool> ();
		
		public bool Extends (ProjectDom dom, IType type)
		{
			if (!IsExtension)
				return false;
			string extensionTableKey = Parameters[0].ReturnType.FullName + "/" + type.FullName;
			lock (extensionTable) {
				if (extensionTable.ContainsKey (extensionTableKey))
					return extensionTable[extensionTableKey];
				
				IType extensionType = dom.GetType (Parameters[0].ReturnType, true);
				
				if (extensionType == null) {
					bool result = Parameters[0].ReturnType.FullName == type.FullName;
					extensionTable.Add (extensionTableKey, result);
					return result;
				}
				
				foreach (IType e in dom.GetInheritanceTree (type)) {
					if (extensionType.Equals (e)) {
						extensionTable.Add (extensionTableKey, true);
						return true;
					}
				}
				
				extensionTable.Add (extensionTableKey, false);
				return false;
			}
		}
		
		public void Add (IParameter parameter)
		{
			if (parameters == null) 
				parameters = new List<IParameter> ();
			parameters.Add (parameter);
		}
		
		public void Add (IEnumerable<IParameter> parameters)
		{
			if (parameters == null)
				return;
			foreach (IParameter parameter in parameters) {
				Add (parameter);
			}
		}
		
		public void AddGenericParameter (IReturnType genPara)
		{
			if (genericParameters == null) 
				genericParameters = new List<IReturnType> ();
			genericParameters.Add (genPara);
		}
		
		XmlNode FindMatch (XmlNodeList nodes)
		{
			List<IParameter> p = parameters ?? new List<IParameter> ();
			foreach (XmlNode node in nodes) {
				XmlNodeList paramList = node.SelectNodes ("Parameters/*");
				if (p.Count == 0 && paramList.Count == 0) 
					return node;
				if (p.Count != paramList.Count) 
					continue;
				bool matched = true;
				for (int i = 0; i < p.Count; i++) {
					if (p[i].ReturnType.FullName != paramList[i].Attributes["Type"].Value) {
						matched = false;
						break;
					}
				}
				if (matched)
					return node;
			}
			return null;
		}
		
		public override System.Xml.XmlNode GetMonodocDocumentation ()
		{
			if (DeclaringType.HelpXml != null) {
				System.Xml.XmlNodeList nodes = DeclaringType.HelpXml.SelectNodes ("/Type/Members/Member[@MemberName='" + Name + "']");
				XmlNode node = nodes.Count == 1 ? nodes[0] : FindMatch (nodes);
				if (node != null) {
					System.Xml.XmlNode result = node.SelectSingleNode ("Docs");
					return result;
				}
			}
			return null;
		}
		

		
		public override int CompareTo (object obj)
		{
			if (obj is IMethod)
				return Name.CompareTo (((IMethod)obj).Name);
			return -1;
		}
		
		public static IMethod Resolve (IMethod source, ITypeResolver typeResolver)
		{
			DomMethod result = new DomMethod ();
			AbstractMember.Resolve (source, result, typeResolver);
			result.MethodModifier = source.MethodModifier;
			if (source.Parameters != null) {
				foreach (IParameter parameter in source.Parameters)
					result.Add (DomParameter.Resolve (parameter, typeResolver));
			}
			
			if (source.GenericParameters != null && source.GenericParameters.Count > 0) {
				foreach (IReturnType returnType in source.GenericParameters) {
					result.AddGenericParameter (DomReturnType.Resolve (returnType, typeResolver));
				}
			}
			
			return result;
		}
		
		
		public override string ToString ()
		{
			return string.Format ("[DomMethod:Name={0}, Modifiers={1}, #Parameters={2}, #GenParameters={3}, ReturnType={4}, Location={5}]",
			                      Name,
			                      Modifiers,
			                      Parameters.Count,
			                      GenericParameters.Count,
			                      ReturnType,
			                      Location);
		}
		
		public override object AcceptVisitor (IDomVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
	}
}
