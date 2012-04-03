// 
// CodeMetricsServices.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2009 Nikhil Sarda
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Gtk;

using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;

//add reference to configure.in file

namespace MonoDevelop.CodeMetrics
{
	public class NamespaceProperties : IProperties
	{
		public Dictionary<string, ClassProperties> Classes {
			get; internal set;
		}
		
		public Dictionary<string, EnumProperties> Enums {
			get; internal set;
		}
		
		public Dictionary<string, StructProperties> Structs {
			get; internal set;
		}
		
		public Dictionary<string, DelegateProperties> Delegates {
			get; internal set;
		}
		
		public Dictionary<string, InterfaceProperties> Interfaces {
			get; internal set;
		}
		
		public string FullName {
			get; private set;
		}
		
		public int CyclometricComplexity {
			get; private set;
		}
		
		public int ClassCoupling {
			get; private set;
		}
		
		public ulong LOCReal {
			get; internal set;
		}
		
		public ulong LOCComments {
			get; internal set;
		}
		
		public int NumberOfTypes {
			get; private set;
		}
		
		public int FieldCount {
			get; private set;
		}
		
		public int MethodCount {
			get; private set;
		}
		
		public int StartLine {
			get; private set;
		}
		
		public int EndLine {
			get; private set;
		}
		
		//TODO Need to implement these
		public int EfferentCoupling {
			get; private set;
		}
		
		public int AfferentCoupling {
			get; private set;
		}
		
		public string FilePath {
			get; set;
		}
		
		public NamespaceProperties(string name)
		{
			Classes = new Dictionary<string, ClassProperties>();
			Interfaces = new Dictionary<string, InterfaceProperties>();
			Structs = new Dictionary<string, StructProperties>();
			Delegates = new Dictionary<string, DelegateProperties>();
			Enums = new Dictionary<string, EnumProperties>();
			
			FullName = name;
			CyclometricComplexity = 0;
			ClassCoupling = 0;
			LOCReal = 0;
			LOCComments = 0;
			NumberOfTypes = 0;
			MethodCount = 0;
			FieldCount = 0;
			EfferentCoupling = 0;
			AfferentCoupling = 0;
			FilePath="";
		}
		
		internal void AddInstance (ITypeDefinition type)
		{
			// Do not include classes that have somehow already been included
			StringBuilder key = new StringBuilder("");
			key.Append(type.FullName);
			
			foreach(var typeArg in type.TypeParameters) {
				foreach(var constraint in typeArg.DirectBaseTypes) {
					key.Append(" " + constraint);
				}
			}
	
			switch(type.Kind)
			{
			case TypeKind.Class:
				AddClass(type, key);
				break;
			case TypeKind.Delegate:
				AddDelegate(type, key);
				break;
			case TypeKind.Enum:
				AddEnum(type, key);
				break;
			case TypeKind.Interface:
				AddInterface(type, key);
				break;
			case TypeKind.Struct:
				AddStruct(type, key);
				break;
			}
		}
		
		private void AddClass (ITypeDefinition cls, StringBuilder key)
		{
			lock(Classes)
			{
				foreach(var typeArg in cls.TypeParameters) {
					foreach(var constraint in typeArg.DirectBaseTypes) {
						key.Append(constraint.ToString ());
					}
				}
				if(Classes.ContainsKey(key.ToString()))
					return;
				Classes.Add(key.ToString(), new ClassProperties(cls));
			}	
		}
		
		private void AddStruct (ITypeDefinition strct, StringBuilder key)
		{
			lock(Structs)
			{
				foreach(var typeArg in strct.TypeParameters) {
					foreach(var constraint in typeArg.DirectBaseTypes) {
						key.Append(constraint.ToString ());
					}
				}
				if(Structs.ContainsKey(key.ToString()))
					return;
				Structs.Add(key.ToString(), new StructProperties(strct));
			}
		}
		
		private void AddInterface (ITypeDefinition interfce, StringBuilder key)
		{
			lock(Interfaces)
			{
				foreach(var typeArg in interfce.TypeParameters) {
					foreach(var constraint in typeArg.DirectBaseTypes) {
						key.Append(constraint.ToString ());
					}
				}
				if(Interfaces.ContainsKey(key.ToString()))
					return;
				Interfaces.Add(key.ToString(), new InterfaceProperties(interfce));
			}
		}
		
		private void AddEnum (ITypeDefinition enm, StringBuilder key)
		{
			lock(Enums)
			{
				foreach(var typeArg in enm.TypeParameters) {
					foreach(var constraint in typeArg.DirectBaseTypes) {
						key.Append(constraint.ToString ());
					}
				}
				if(Enums.ContainsKey(key.ToString()))
					return;
				Enums.Add(key.ToString(), new EnumProperties(enm));
			}
		}
		
		private void AddDelegate (ITypeDefinition dlgte, StringBuilder key)
		{
			lock(Delegates)
			{
				foreach(var typeArg in dlgte.TypeParameters) {
					foreach(var constraint in typeArg.DirectBaseTypes) {
						key.Append(constraint.ToString ());
					}
				}
				if(Delegates.ContainsKey(key.ToString()))
					return;
				Delegates.Add(key.ToString(), new DelegateProperties(dlgte));
			}
		}
		
		internal void ProcessClasses()
		{
			foreach(var cls in this.Classes)
			{
				cls.Value.ProcessInnerClasses();
				this.CyclometricComplexity += cls.Value.CyclometricComplexity;
				this.ClassCoupling += cls.Value.ClassCoupling;
				this.LOCReal += cls.Value.LOCReal;
				this.LOCComments += cls.Value.LOCComments;
				this.FieldCount += cls.Value.FieldCount;
				this.MethodCount += cls.Value.MethodCount;
				this.NumberOfTypes++;
			}
		}
		
	}
}

			
