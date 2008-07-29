//  DefaultClass.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using MonoDevelop.Projects.Utility;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public class DefaultClass : AbstractNamedEntity, IClass, IComparable
	{
		protected ClassType        classType;
		protected IRegion          region;
		protected IRegion          bodyRegion;
		protected object           declaredIn;
		protected ICompilationUnit compilationUnit;
		
		protected GenericParameterList genericParamters;
		protected ReturnTypeList     baseTypes    = new ReturnTypeList();

		protected ClassCollection    innerClasses;
		protected FieldCollection    fields;
		protected PropertyCollection properties;
		protected MethodCollection   methods;
		protected EventCollection    events;
		protected IndexerCollection  indexer;
		SolutionItem sourceProject;
		
		string ns = string.Empty;
		
		public DefaultClass ()
		{
			innerClasses = new ClassCollection ();
			fields       = new FieldCollection (this);
			properties   = new PropertyCollection (this);
			methods      = new MethodCollection (this);
			events       = new EventCollection (this);
			indexer      = new IndexerCollection (this);
		}

		public DefaultClass (ICompilationUnit compilationUnit): this ()
		{
			this.compilationUnit = compilationUnit;
		}

		public string FullyQualifiedName {
			get {
				string fq;
				if (ns != null && ns.Length > 0)
					fq = string.Concat (ns, ".", Name);
				else
					fq = Name;
					
				if (declaredIn is IClass)
					return string.Concat (((IClass) declaredIn).FullyQualifiedName, ".", fq); 
				else
					return fq;
			}
			set {
				int i = value.IndexOf ("[");
				if (i != -1)
					i = value.LastIndexOf (".", i);
				else
					i = value.LastIndexOf (".");

				if (i == -1) {
					Name = value;
					Namespace = "";
				}
				else {
					Name = value.Substring (i+1);
					Namespace = value.Substring (0, i);
				}
			}
		}

		public virtual string Namespace {
			get { return ns; }
			set { ns = value; }
//			set { ns = GetSharedString (value); }
		}

		public virtual ICompilationUnit CompilationUnit {
			get { return compilationUnit; }
		}

		public SolutionItem SourceProject {
			get { return sourceProject; }
			internal set { sourceProject = value; }
		}
		
		public virtual ClassType ClassType {
			get {
				return classType;
			}
			set {
				classType = value;
			}
		}

		public virtual IRegion Region {
			get {
				return region;
			}
			set {
				region = value;
			}
		}
		
		public virtual IRegion BodyRegion {
			get {
				return bodyRegion;
			}
			set {
				bodyRegion = value;
			}
		}

		public object DeclaredIn {
			get {
				return declaredIn;
			}
			set {
				declaredIn = value;
			}
		}
		
		public virtual IClass[] Parts {
			get { return new IClass[] { this }; }
		}
		
		/// <summary>
		/// Contains a list of formal parameters to a generic type. 
		/// <p>If this property returns null or an empty collection, the type is
		/// not generic.</p>
		/// </summary>
		public virtual GenericParameterList GenericParameters {
			get {
				return genericParamters;
			}
			set {
				genericParamters = value;
			}
		}

		public virtual ReturnTypeList BaseTypes {
			get {
				return baseTypes;
			}
		}
		
		public virtual ClassCollection InnerClasses {
			get {
				return innerClasses;
			}
		}

		public virtual FieldCollection Fields {
			get {
				return fields;
			}
		}

		public virtual PropertyCollection Properties {
			get {
				return properties;
			}
		}

		public IndexerCollection Indexer {
			get {
				return indexer;
			}
		}

		public virtual MethodCollection Methods {
			get {
				return methods;
			}
		}

		public virtual EventCollection Events {
			get {
				return events;
			}
		}


		public override int CompareTo (object ob)
		{
			int cmp;
			IClass value = (IClass) ob;
			
			if(0 != (cmp = base.CompareTo(value))) {
				return cmp;
			}
			
			if (FullyQualifiedName != null) {
				if (value.FullyQualifiedName == null)
					return -1;
				cmp = FullyQualifiedName.CompareTo(value.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			} else if (value.FullyQualifiedName != null)
				return 1;
			
			if (Region != null) {
				if (value.Region == null)
					return -1;
				cmp = Region.CompareTo(value.Region);
				if (cmp != 0)
					return cmp;
			} else
				if (value.Region != null)
					return 1;
			
			cmp = DiffUtility.Compare(BaseTypes, value.BaseTypes);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(InnerClasses, value.InnerClasses);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Fields, value.Fields);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Properties, value.Properties);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Indexer, value.Indexer);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Methods, value.Methods);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Events, value.Events);
			if (cmp != 0)
				return cmp;
			
			if (value.GenericParameters == GenericParameters)
				return 0;	// They are the same classes or are both null - 
							// which counts as 'being same'
			else
				return DiffUtility.Compare(GenericParameters, value.GenericParameters);
		}
		
		public override bool Equals (object ob)
		{
			IClass other = ob as IClass;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			return base.GetHashCode() + FullyQualifiedName.GetHashCode ();
		}
		
		protected override bool CanBeSubclass {
			get {
				return true;
			}
		}
		
		internal static string GetInstantiatedTypeName (string typeName, ReturnTypeList genericArguments)
		{
			if (genericArguments == null || genericArguments.Count == 0)
				return typeName;
			
			StringBuilder sb = new StringBuilder (typeName);
				
			sb.Append ('[');
			for (int n=0; n<genericArguments.Count; n++) {
				if (n > 0) sb.Append (',');
				IReturnType rt = genericArguments [n];
				sb.Append (DefaultReturnType.ToString (rt));
			}
			sb.Append (']');
			return sb.ToString ();
		}
		
		internal static IClass CreateInstantiatedGenericType (IClass template, ReturnTypeList genericArguments)
		{
			string name = GetInstantiatedTypeName (template.Name, genericArguments);
			GenericTypeInstanceResolver res = new GenericTypeInstanceResolver ();
			
			// Fill a dictionary with the name->type relations
			res.TypeValues = new Hashtable ();
			for (int n=0; n<template.GenericParameters.Count; n++)
				res.TypeValues [template.GenericParameters [n].Name] = genericArguments [n];
			
			// Replace the type name placeholders
			DefaultClass cls = PersistentClass.Resolve (template, res);
			cls.Name = name;
			
			// Not generic anymore
			cls.GenericParameters = null;
			
			return cls;
		}
		
		class GenericTypeInstanceResolver: ITypeResolver
		{
			public Hashtable TypeValues;
			
			public IReturnType Resolve (IReturnType type)
			{
				// If the type is useing a type placeholder name, replace it
				IReturnType paramType = (IReturnType) TypeValues [type.FullyQualifiedName];
				if (paramType == null) {
					// It's not the placeholder type, but it may be a generic type which has
					// the placeholder type as parameter
					if (type.GenericArguments == null || type.GenericArguments.Count == 0)
						return type;
					
					DefaultReturnType rt = new DefaultReturnType ();
					rt.FullyQualifiedName = type.FullyQualifiedName;
					rt.ByRef = type.ByRef;
					rt.PointerNestingLevel = type.PointerNestingLevel;
					rt.ArrayDimensions = type.ArrayDimensions;
					rt.GenericArguments = new ReturnTypeList ();
					foreach (IReturnType gp in type.GenericArguments)
						rt.GenericArguments.Add (Resolve (gp));
					return rt;
				}
				else {
					// The placeholder type and the parameter type have to be merged.
					// For example 'type' my be T[,], and 'paramType' may be 'int[]'.
					// The result is int[][,].
					DefaultReturnType rt = new DefaultReturnType ();
					rt.FullyQualifiedName = paramType.FullyQualifiedName;
					rt.ByRef = paramType.ByRef;
					rt.PointerNestingLevel = type.PointerNestingLevel + paramType.PointerNestingLevel;
					
					if (type.ArrayDimensions.Length > 0 && paramType.ArrayDimensions.Length > 0) {
						int[] dims = new int [paramType.ArrayDimensions.Length + type.ArrayDimensions.Length];
						int i = 0;
						for (int n=0; n < paramType.ArrayDimensions.Length; n++)
							dims [i++] = paramType.ArrayDimensions [n];
						for (int n=0; n < type.ArrayDimensions.Length; n++)
							dims [i++] = type.ArrayDimensions [n];
						rt.ArrayDimensions = dims;
					}
					else if (type.ArrayDimensions.Length == 0 && paramType.ArrayDimensions.Length > 0)
						rt.ArrayDimensions = paramType.ArrayDimensions;
					else if (type.ArrayDimensions.Length > 0 && paramType.ArrayDimensions.Length == 0)
						rt.ArrayDimensions = type.ArrayDimensions;
					
					// Generic parameters don't need to be merged since the type placehodlers can't have them
					if (paramType.GenericArguments != null && paramType.GenericArguments.Count > 0) {
						rt.GenericArguments = new ReturnTypeList();
						foreach (IReturnType ga in paramType.GenericArguments)
							rt.GenericArguments.Add (ga);
					}
					return rt;
				}
			}
		}
	}
}
