// 
// AssemblyProperties.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmai.com>
// 
// Copyright (c) 2010 Nikhil Sarda
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Text;

using Gtk;

using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CodeMetrics
{
	public class ProjectProperties : IProperties
	{
		Project project;
		
		public Project Project {
			get {
				return project;
			}
		}
		
		public Dictionary<string, NamespaceProperties> Namespaces {
			get; internal set;
		}
		
		public Dictionary<string, ClassProperties> Classes {
			get; internal set;
		}
		
		public Dictionary<string, StructProperties> Structs {
			get; internal set;
		}
		
		public Dictionary<string, EnumProperties> Enums {
			get; internal set;
		}
		
		public Dictionary<string, DelegateProperties> Delegates {
			get; internal set;
		}
		
		public Dictionary<string, InterfaceProperties> Interfaces {
			get; internal set;
		}
		
		public string FullName {
			get; internal set;
		}
		
		public int CyclometricComplexity {
			get; internal set;
		}
		
		public int ClassCoupling {
			get; internal set;
		}
		
		public int StartLine {
			get; private set;
		}
		
		public int EndLine {
			get; private set;
		}
		
		public ulong LOCReal {
			get; internal set;
		}
		
		public ulong LOCComments {
			get;internal set;
		}
		
		public string FilePath {
			get; set;
		}
		
		public ProjectProperties(Project p)
		{
			Namespaces = new Dictionary<string, NamespaceProperties>();
			Classes = new Dictionary<string, ClassProperties>();
			Structs = new Dictionary<string, StructProperties>();
			Enums = new Dictionary<string, EnumProperties>();
			Delegates = new Dictionary<string, DelegateProperties>();
			Interfaces = new Dictionary<string, InterfaceProperties>();
			project = p;
		}
		
		internal void AddInstance (ITypeDefinition cls)
		{
			// Do not include classes inherited from assemblies
			if (cls.BodyRegion.Begin == cls.BodyRegion.End)
				return;
			StringBuilder key = new StringBuilder();
			key.Append(cls.FullName);
			
			switch (cls.Kind)
			{
			case TypeKind.Class:
				AddClass(cls, key);
				break;
			case TypeKind.Delegate:
				AddDelegate(cls, key);
				break;
			case TypeKind.Enum:
				AddEnum(cls, key);
				break;
			case TypeKind.Interface:
				AddInterface(cls, key);
				break;
			case TypeKind.Struct:
				AddStruct(cls, key);
				break;
			}
			
		}
		
		private void AddClass (ITypeDefinition cls, StringBuilder key)
		{
			if(cls.Namespace=="") {
				lock(Classes)
				{
					foreach(var typeArg in cls.TypeParameters) {
						foreach(var constraint in typeArg.DirectBaseTypes) {
							key.Append(constraint.Name);
						}
					}
					if(Classes.ContainsKey(key.ToString()))
						return;
					Classes.Add(key.ToString(), new ClassProperties(cls));
				}
			} else {
				AddNamespace(key, cls);
			}
		}
		
		private void AddStruct (ITypeDefinition strct, StringBuilder key)
		{
			if(strct.Namespace=="") {
				lock(Structs)
				{
					foreach(var typeArg in strct.TypeParameters) {
						foreach(var constraint in typeArg.DirectBaseTypes) {
							key.Append(constraint.Name);
						}
					}
					if(Structs.ContainsKey(key.ToString()))
						return;
					Structs.Add(key.ToString(), new StructProperties(strct));
				}
			} else {
				AddNamespace(key, strct);
			}
		}
		
		private void AddInterface (ITypeDefinition interfce, StringBuilder key)
		{
			if(interfce.Namespace=="") {
				lock(Interfaces)
				{
					foreach(var typeArg in interfce.TypeParameters) {
						foreach(var constraint in typeArg.DirectBaseTypes) {
							key.Append(constraint.Name);
						}
					}
					if(Interfaces.ContainsKey(key.ToString()))
						return;
					Interfaces.Add(key.ToString(), new InterfaceProperties(interfce));
				}
			} else {
				AddNamespace(key, interfce);
			}
		}
		
		private void AddEnum (ITypeDefinition enm, StringBuilder key)
		{
			if(enm.Namespace=="") {
				lock(Enums)
				{
					foreach(var typeArg in enm.TypeParameters) {
						foreach(var constraint in typeArg.DirectBaseTypes) {
							key.Append(constraint.Name);
						}
					}
					if(Enums.ContainsKey(key.ToString()))
						return;
					Enums.Add(key.ToString(), new EnumProperties(enm));
				}
			} else {
				AddNamespace(key,enm);
			}
		}
		
		private void AddDelegate (ITypeDefinition dlgte, StringBuilder key)
		{
			if(dlgte.Namespace=="") {
				lock(Delegates)
				{
					foreach(var typeArg in dlgte.TypeParameters) {
						foreach(var constraint in typeArg.DirectBaseTypes) {
							key.Append(constraint.Name);
						}
					}
					if(Delegates.ContainsKey(key.ToString()))
						return;
					Delegates.Add(key.ToString(), new DelegateProperties(dlgte));
				}
			} else {
				AddNamespace(key, dlgte);
			}
		}
		
		private void AddNamespace (StringBuilder key, ITypeDefinition cls)
		{
			lock(Namespaces)
			{
				if (InstanceExists (key.ToString()))
					return;
			
				if(!Namespaces.ContainsKey(cls.Namespace))
					Namespaces.Add(cls.Namespace, new NamespaceProperties(cls.Namespace));
			
				Namespaces[cls.Namespace].AddInstance(cls);
			}
		}
		
		/// <summary>
		/// This method is used to return a reference to the namespace with the specified name
		/// </summary>
		/// <param name="FullName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="NamespaceProperties"/>
		/// </returns>
		internal NamespaceProperties GetNamespaceReference (string FullName)
		{
			if(Namespaces.ContainsKey(FullName))
				return Namespaces[FullName];
			return null;
		}
		
		/// <summary>
		/// This method is used to get the reference to the Method whose properties we need to update. Need a faster way to do things instead of brute force lookup.
		/// </summary>
		/// <param name="FullName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="Parameters">
		/// A <see cref="List<System.String>"/>
		/// </param>
		/// <returns>
		/// A <see cref="MethodProperties"/>
		/// </returns>
		internal MethodProperties GetMethodReference (string FullName, List<string> Parameters)
		{
			StringBuilder MethodKey = new StringBuilder();
			MethodKey.Append(FullName+" ");
			foreach(string paramName in Parameters)
				MethodKey.Append(paramName+" ");
			foreach(var cls in Classes)
			{
				if(cls.Value.Methods.ContainsKey(MethodKey.ToString()))
					return cls.Value.Methods[MethodKey.ToString()];
				var ret = RecursiveFindRefMeth(cls.Value, MethodKey.ToString());
				if(ret!=null)
					return ret;
			}
			foreach(var namesp in Namespaces)
			{
				foreach(var cls in namesp.Value.Classes)
				{
					if(cls.Value.Methods.ContainsKey(MethodKey.ToString()))
						return cls.Value.Methods[MethodKey.ToString()];
					var ret = RecursiveFindRefMeth (cls.Value, MethodKey.ToString());
					if(ret!=null)
						return ret;
				}
			}
			return null;
		}

		private MethodProperties RecursiveFindRefMeth (ClassProperties cls, string key)
		{
			foreach(var innercls in cls.InnerClasses)
			{
				if(innercls.Value.Methods.ContainsKey(key))
					return innercls.Value.Methods[key];
				var ret = RecursiveFindRefMeth(innercls.Value, key);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		/// <summary>
		/// This method is used to get a class reference from the name. Brute force lookup for now, need a better way.
		/// </summary>
		/// <param name="cls">
		/// A <see cref="ClassProperties"/>
		/// </param>
		/// <param name="key">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="MethodProperties"/>
		/// </returns>
		internal ClassProperties GetClassReference (string FullName)
		{
			if(Classes.ContainsKey(FullName))
				return Classes[FullName];
			foreach(var cls in Classes)
			{
				var ret = RecursiveFindRefCls(cls.Value, FullName);
				if(ret!=null)
					return ret;
			}
			foreach(var namesp in Namespaces)
			{
				if(namesp.Value.Classes.ContainsKey(FullName))
					return namesp.Value.Classes[FullName];
				foreach(var cls in namesp.Value.Classes)
				{
					var ret = RecursiveFindRefCls (cls.Value, FullName);
					if(ret!=null)
						return ret;
				}
			}
			return null;
		}
		
		private ClassProperties RecursiveFindRefCls (ClassProperties cls, string key)
		{
			if(cls.InnerClasses.ContainsKey(key))
				return cls.InnerClasses[key];
			foreach (var innercls in cls.InnerClasses)
			{
				var ret = RecursiveFindRefCls (innercls.Value, key);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="key">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="namesp">
		/// A <see cref="NamespaceProperties"/>
		/// </param>
		/// <returns>
		/// A <see cref="EnumProperties"/>
		/// </returns>
		internal EnumProperties GetEnumReference (string FullName, NamespaceProperties namesp)
		{
			if(namesp == null) {
				if(this.Enums.ContainsKey(FullName)) {
					return this.Enums[FullName];
				} else {
					return null;
				}
			}
			if(namesp.Enums.ContainsKey(FullName))
				return namesp.Enums[FullName];
			foreach(var cls in namesp.Classes)
			{
				if(cls.Value.InnerEnums.ContainsKey(FullName))
					return cls.Value.InnerEnums[FullName];
				var ret = RecursiveGetEnumReference(cls.Value, FullName);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		private EnumProperties RecursiveGetEnumReference (ClassProperties cls, string key)
		{
			foreach (var innercls in cls.InnerClasses)
			{
				if(innercls.Value.InnerEnums.ContainsKey(key))
					return innercls.Value.InnerEnums[key];
				var ret = RecursiveGetEnumReference (innercls.Value, key);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="FullName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="namesp">
		/// A <see cref="NamespaceProperties"/>
		/// </param>
		/// <returns>
		/// A <see cref="InterfaceProperties"/>
		/// </returns>
		internal InterfaceProperties GetInterfaceReference (string FullName, NamespaceProperties namesp)
		{
			if(namesp == null)
				if(this.Interfaces.ContainsKey(FullName))
					return this.Interfaces[FullName];
			if(namesp.Interfaces.ContainsKey(FullName))
				return namesp.Interfaces[FullName];
			foreach(var cls in namesp.Classes)
			{
				if(cls.Value.InnerInterfaces.ContainsKey(FullName))
					return cls.Value.InnerInterfaces[FullName];
				var ret = RecursiveGetInterfaceReference(cls.Value, FullName);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		private InterfaceProperties RecursiveGetInterfaceReference (ClassProperties cls, string key)
		{
			if(cls.InnerInterfaces.ContainsKey(key))
				return cls.InnerInterfaces[key];
			foreach (var innercls in cls.InnerClasses)
			{
				var ret = RecursiveGetInterfaceReference (innercls.Value, key);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="FullName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="namesp">
		/// A <see cref="NamespaceProperties"/>
		/// </param>
		/// <returns>
		/// A <see cref="DelegateProperties"/>
		/// </returns>
		internal DelegateProperties GetDelegateReference (string FullName, NamespaceProperties namesp)
		{
			if(namesp == null)
				if(this.Delegates.ContainsKey(FullName))
					return this.Delegates[FullName];
			if(namesp.Delegates.ContainsKey(FullName))
				return namesp.Delegates[FullName];
			foreach(var cls in namesp.Classes)
			{
				if(cls.Value.InnerDelegates.ContainsKey(FullName))
					return cls.Value.InnerDelegates[FullName];
				var ret = RecursiveGetDelegateReference(cls.Value, FullName);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		private DelegateProperties RecursiveGetDelegateReference (ClassProperties cls, string key)
		{
			if(cls.InnerDelegates.ContainsKey(key))
				return cls.InnerDelegates[key];
			foreach (var innercls in cls.InnerClasses)
			{
				var ret = RecursiveGetDelegateReference (innercls.Value, key);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="FullName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="namesp">
		/// A <see cref="NamespaceProperties"/>
		/// </param>
		/// <returns>
		/// A <see cref="StructProperties"/>
		/// </returns>
		internal StructProperties GetStructReference (string FullName, NamespaceProperties namesp)
		{
			if(namesp == null)
				if(this.Structs.ContainsKey(FullName))
					return this.Structs[FullName];
			if(namesp.Structs.ContainsKey(FullName))
				return namesp.Structs[FullName];
			foreach(var cls in namesp.Classes)
			{
				if(cls.Value.InnerStructs.ContainsKey(FullName))
					return cls.Value.InnerStructs[FullName];
				var ret = RecursiveGetStructReference(cls.Value, FullName);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		private StructProperties RecursiveGetStructReference (ClassProperties cls, string key)
		{
			if(cls.InnerClasses.ContainsKey(key))
				return cls.InnerStructs[key];
			foreach (var innercls in cls.InnerClasses)
			{
				var ret = RecursiveGetStructReference (innercls.Value, key);
				if(ret!=null)
					return ret;
			}
			return null;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="clsName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		internal bool InstanceExists (string clsName)
		{
			if(Classes.ContainsKey(clsName))
				return true;
			foreach(var cls in this.Classes) {
				if(RecursiveInstanceExists(cls.Value, clsName))
					return true;
			}
			foreach(var namesp in this.Namespaces) {
				if(namesp.Value.Classes.ContainsKey(clsName))
					return true;
				foreach(var cls in namesp.Value.Classes) {
					if(RecursiveInstanceExists(cls.Value, clsName))
						return true;
				}
			}
			return false;
		}
		
		private bool RecursiveInstanceExists(ClassProperties cls, string clsName)
		{
			if(cls.InnerClasses.ContainsKey(clsName))
				return true;
			foreach(var innercls in cls.InnerClasses) {
				if(RecursiveInstanceExists(innercls.Value, clsName))
					return true;
			}
			return false;
		}
	}
}

