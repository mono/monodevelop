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
		protected List<IParameter> parameters = null;
		protected List<ITypeParameter> genericParameters = null;

		static readonly ReadOnlyCollection<IParameter> emptyParameters = new ReadOnlyCollection<IParameter> (new IParameter [0]);
		static readonly ReadOnlyCollection<ITypeParameter> emptyGenericParameters = new ReadOnlyCollection<ITypeParameter> (new ITypeParameter [0]);
		
		public override MemberType MemberType {
			get {
				return MemberType.Method;
			}
		}
		
		public virtual MethodModifier MethodModifier {
			get;
			set;
		}
		
		public bool IsConstructor {
			get {
				return (MethodModifier & MethodModifier.IsConstructor) == MethodModifier.IsConstructor;
			}
		}
		
		public virtual bool IsExtension {
			get {
				return (MethodModifier & MethodModifier.IsExtension) == MethodModifier.IsExtension;
			}
		}
		
		public virtual bool WasExtended {
			get {
				return (MethodModifier & MethodModifier.WasExtended) == MethodModifier.WasExtended;
			}
		}
		
		public bool IsFinalizer {
			get {
				return (MethodModifier & MethodModifier.IsFinalizer) == MethodModifier.IsFinalizer;
			}
		}
		
		public virtual ReadOnlyCollection<IParameter> Parameters {
			get {
				return parameters != null ? parameters.AsReadOnly () : emptyParameters;
			}
		}
		
		public ReadOnlyCollection<ITypeParameter> TypeParameters {
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
		static readonly string[] extensionIconTable = {Stock.ExtensionMethod, Stock.PrivateExtensionMethod, Stock.ProtectedExtensionMethod, Stock.InternalExtensionMethod};
		public override string StockIcon {
			get {
				if (WasExtended)
					return extensionIconTable [ModifierToOffset (Modifiers)];
				return iconTable [ModifierToOffset (Modifiers)];
			}
		}
		
		public DomMethod ()
		{
		}
		
		public DomMethod (string name, Modifiers modifiers, MethodModifier MethodModifier, DomLocation location, DomRegion bodyRegion)
		{
			this.Name           = name;
			this.Modifiers      = modifiers;
			this.Location       = location;
			this.BodyRegion     = bodyRegion;
			this.MethodModifier = MethodModifier;
		}
		
		public DomMethod (string name, Modifiers modifiers, MethodModifier MethodModifier, DomLocation location, DomRegion bodyRegion, IReturnType returnType)
		{
			this.Name           = name;
			this.Modifiers      = modifiers;
			this.Location       = location;
			this.BodyRegion     = bodyRegion;
			this.ReturnType     = returnType;
			this.MethodModifier = MethodModifier;
		}
		
		public static IMethod CreateInstantiatedGenericMethod (IMethod method, IList<IReturnType> genericArguments, IList<IReturnType> methodArguments)
		{
		//	System.Console.WriteLine("----");
		//	Console.WriteLine ("instantiate: " + method);
			GenericMethodInstanceResolver resolver = new GenericMethodInstanceResolver ();
			if (genericArguments != null) {
				for (int i = 0; i < method.TypeParameters.Count && i < genericArguments.Count; i++) 
					resolver.Add (new DomReturnType (method.TypeParameters[i].Name), genericArguments[i]);
			}
			IMethod result = (IMethod)method.AcceptVisitor (resolver, method);
			resolver = new GenericMethodInstanceResolver ();
			if (methodArguments != null) {
				// The stack should contain <TEMPLATE> / RealType pairs
				Stack<KeyValuePair<IReturnType, IReturnType>> returnTypeStack = new Stack<KeyValuePair<IReturnType, IReturnType>> ();
				for (int i = 0; i < method.Parameters.Count && i < methodArguments.Count; i++) {
//					Console.WriteLine ("parameter:" + method.Parameters[i]);
					returnTypeStack.Push (new KeyValuePair<IReturnType, IReturnType> (method.Parameters[i].ReturnType, methodArguments[i]));
					while (returnTypeStack.Count > 0) {
						KeyValuePair<IReturnType, IReturnType> curReturnType = returnTypeStack.Pop ();
						//Console.WriteLine ("key:" + curReturnType.Key + "/ val:" + curReturnType.Value);
						bool found = false;
						for (int j = 0; j < method.TypeParameters.Count; j++) {
							if (method.TypeParameters[j].Name == curReturnType.Key.FullName) {
								found = true;
								break;
							}
						}
						if (found) {
							resolver.Add (curReturnType.Key, curReturnType.Value);
							continue;
						}
						//Console.WriteLine ("key:" + curReturnType.Key);
						//Console.WriteLine ("value:" + curReturnType.Value);
						for (int k = 0; k < System.Math.Min (curReturnType.Key.GenericArguments.Count, curReturnType.Value.GenericArguments.Count); k++) {
							//Console.WriteLine ("add " + curReturnType.Key.GenericArguments[k] + " " + curReturnType.Value.GenericArguments[k]);
							returnTypeStack.Push (new KeyValuePair<IReturnType, IReturnType> (curReturnType.Key.GenericArguments[k], 
							                                                                  curReturnType.Value.GenericArguments[k]));
						}
					}
				}
			}
//			System.Console.WriteLine("before:" + result);
			result = (IMethod)result.AcceptVisitor (resolver, result);
			((DomMethod)result).DeclaringType = method.DeclaringType;
			((DomMethod)result).MethodModifier = method.MethodModifier;
			
//			System.Console.WriteLine("after:" + result);
			return result;
		}
		
		internal class GenericMethodInstanceResolver: CopyDomVisitor<IMethod>
		{
			public Dictionary<string, IReturnType> typeTable = new Dictionary<string,IReturnType> ();
			
			public void Add (IReturnType parameterType, IReturnType type)
			{
				if (type == null || string.IsNullOrEmpty (type.FullName))
					return;
				string name = parameterType.Name;
				if (!typeTable.ContainsKey (name)) {
					DomReturnType newType = new DomReturnType (type.FullName);
					newType.ArrayDimensions     = Math.Max (0, type.ArrayDimensions - parameterType.ArrayDimensions);
					newType.PointerNestingLevel = Math.Max (0, type.PointerNestingLevel - parameterType.PointerNestingLevel);
					newType.Type  = type.Type; // May be anonymous type
					for (int i = 0; i < newType.ArrayDimensions; i++)
							newType.SetDimension (i, parameterType.GetDimension (i));
					typeTable[name] = newType;
				}
			}
			
			public override IDomVisitable Visit (IMethod source, IMethod data)
			{
				DomMethod result = CreateInstance (source, data);
				Visit (source, result, data);
				
				foreach (ITypeParameter tp in source.TypeParameters) {
					if (!typeTable.ContainsKey (tp.Name))
						result.AddTypeParameter (Visit (tp, data));
				}
				
				result.MethodModifier = source.MethodModifier;
				if (source.Parameters != null) {
					foreach (IParameter parameter in source.Parameters)
						result.Add ((IParameter) parameter.AcceptVisitor (this, data));
				}
				
				return result;
			}
				
			public override IDomVisitable Visit (IReturnType type, IMethod typeToInstantiate)
			{
				DomReturnType copyFrom = (DomReturnType) type;
				IReturnType res;
				if (typeTable.TryGetValue (copyFrom.DecoratedFullName, out res)) {
					if (type.ArrayDimensions > 0) {
						DomReturnType drr = new DomReturnType (res.FullName);
						drr.PointerNestingLevel = type.PointerNestingLevel;
						drr.ArrayDimensions = type.ArrayDimensions;
						drr.Type  = type.Type; // May be anonymous type
						for (int i = 0; i < type.ArrayDimensions; i++)
							drr.SetDimension (i, type.GetDimension (i));
						return drr;
					}
					return res;
				}
				return base.Visit (type, typeToInstantiate);
			}
		}
		
/*
		public override bool IsAccessibleFrom (ProjectDom dom, IType calledType, IMember member)
		{
			return IsExtension || base.IsAccessibleFrom (dom, calledType, member);
		}*/
		
		static Dictionary<string, IMethod> extensionTable = new Dictionary<string, IMethod> ();
		
		public IMethod Extends (ProjectDom dom, IType type)
		{ 
			if (dom == null || type == null || Parameters.Count == 0 || !IsExtension) {
				return null;
			}
//			Console.WriteLine ("Test Method: " + Name);
			string extensionTableKey = FullName + "." + Parameters[0].ReturnType.ToInvariantString () + "/" + type.FullName;
//			Console.WriteLine ("table key:" + extensionTableKey);
			lock (extensionTable) {
				if (extensionTable.ContainsKey (extensionTableKey))
					return extensionTable[extensionTableKey];
				if (type.BaseType != null && type.BaseType.FullName == "System.Array" && Parameters[0].ReturnType.ArrayDimensions > 0) {
					IReturnType elementType = null;
					foreach (IReturnType returnType in type.BaseTypes) {
						if (returnType.FullName == "System.Collections.Generic.IList" && returnType.GenericArguments.Count > 0) {
							elementType = returnType.GenericArguments[0];
							break;
						}
					}
					if (elementType != null) {
						IMethod instMethod = DomMethod.CreateInstantiatedGenericMethod (this, new IReturnType[]{}, new IReturnType[] { elementType });DomMethod.CreateInstantiatedGenericMethod (this, new IReturnType[]{}, new IReturnType[] { elementType });
						instMethod = new ExtensionMethod (type , instMethod, null, null);
						extensionTable.Add (extensionTableKey, instMethod);
						return instMethod;
					}
				}
				
				foreach (IType baseType in dom.GetInheritanceTree (type)) {
					IMethod instMethod = DomMethod.CreateInstantiatedGenericMethod (this, new IReturnType[]{}, new IReturnType[] { new DomReturnType (baseType) });
					string baseTypeFullName = baseType is InstantiatedType ? ((InstantiatedType)baseType).UninstantiatedType.FullName : baseType.FullName;
//					Console.WriteLine (instMethod.Parameters[0].ReturnType.FullName + " === " + baseTypeFullName);
					if (instMethod.Parameters[0].ReturnType.FullName == baseTypeFullName) {
						//ExtensionMethod result = new ExtensionMethod (baseType, this, null, null);
						instMethod = new ExtensionMethod (type , instMethod, null, null);
						extensionTable.Add (extensionTableKey, instMethod);
//						Console.WriteLine (result.Parameters[0]);
						return instMethod;
					}
				}
//				Console.WriteLine ("null");
				extensionTable.Add (extensionTableKey, null);
				return null;
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
		
		public void AddTypeParameter (ITypeParameter genPara)
		{
			if (genericParameters == null) 
				genericParameters = new List<ITypeParameter> ();
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
		
		public override string ToString ()
		{
			return string.Format ("[DomMethod:Name={0}, Modifiers={1}, #Parameters={2}, #TypeParameters={3}, ReturnType={4}, Location={5}]",
			                      Name,
			                      Modifiers,
			                      Parameters.Count,
			                      TypeParameters.Count,
			                      ReturnType,
			                      Location);
		}
		
		public override S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
	}
}
