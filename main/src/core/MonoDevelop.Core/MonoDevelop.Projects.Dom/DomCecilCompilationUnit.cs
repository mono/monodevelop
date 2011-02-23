//
// DomCecilCompilationUnit.cs
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
using System.IO;
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Projects.Dom;
using Mono.Cecil;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom
{
	
	public class DomCecilCompilationUnit : CompilationUnit
	{
		AssemblyDefinition assemblyDefinition;
		public AssemblyDefinition AssemblyDefinition {
			get {
				return assemblyDefinition;
			}
		}
		
		public class Module 
		{
			public ModuleDefinition ModuleDefinition {
				get;
				set;
			}
			
			List<IType> types = new List<IType> ();
			public List<IType> Types {
				get {
					return types;
				}
			}
			
			public Module (ModuleDefinition moduleDefinition)
			{
				this.ModuleDefinition = moduleDefinition;
			}
		}
		
		List<Module> modules = new List<Module> ();
		public IEnumerable<Module> Modules {
			get {
				return modules;
			}
		}
		
//		
//		public DomCecilCompilationUnit (AssemblyDefinition assemblyDefinition) : this (true, true, assemblyDefinition)
//		{
//		}
		
		public DomCecilCompilationUnit (AssemblyDefinition assemblyDefinition, bool loadInternals, bool instantiateTypeParameter) : base (assemblyDefinition.Name.FullName)
		{
			this.assemblyDefinition = assemblyDefinition;
		
			foreach (ModuleDefinition moduleDefinition in assemblyDefinition.Modules) {
				AddModuleDefinition (moduleDefinition, loadInternals, instantiateTypeParameter);
			}
			foreach (CustomAttribute attr in assemblyDefinition.CustomAttributes) {
				Add (new DomCecilAttribute (attr));
			}
		}
		
/*		public static DomCecilCompilationUnit Load (string fileName)
		{
			return Load (fileName, true);
		}
		public static DomCecilCompilationUnit Load (string fileName, bool keepDefinitions)
		{
			return Load (fileName, true, true);
		}
		public static DomCecilCompilationUnit Load (string fileName, bool keepDefinitions, bool loadInternals)
		{
			return Load (fileName, true, true, false);
		}
		*/
		
		public static DomCecilCompilationUnit Load (string fileName, bool loadInternals, bool instantiateTypeParameter)
		{
			if (String.IsNullOrEmpty (fileName))
				return null;
			//FIXME: should pass a custom resolver to the AssemblyDefinition so that it resolves from the correct GAC
			using (var stream = new MemoryStream (File.ReadAllBytes (fileName))) {
				DomCecilCompilationUnit result = new DomCecilCompilationUnit (AssemblyDefinition.ReadAssembly (stream), loadInternals, instantiateTypeParameter);
				result.fileName = fileName;
				return result;
			}
		}
		
		public static bool IsInternal (MonoDevelop.Projects.Dom.Modifiers mods)
		{
			return (mods & MonoDevelop.Projects.Dom.Modifiers.Internal) == MonoDevelop.Projects.Dom.Modifiers.Internal ||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.Private) == MonoDevelop.Projects.Dom.Modifiers.Private ||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.ProtectedAndInternal) == MonoDevelop.Projects.Dom.Modifiers.ProtectedAndInternal;
		}
		
		void AddModuleDefinition (ModuleDefinition moduleDefinition, bool loadInternal, bool instantiateTypeParameter)
		{
			InstantiatedParamResolver resolver = new InstantiatedParamResolver ();
			Module module = new Module (moduleDefinition);
			foreach (TypeDefinition type in moduleDefinition.Types) {
				if (!loadInternal && IsInternal (DomCecilType.GetModifiers (type.Attributes)))
					continue;
//				if (type.Name == "SimplePropertyDescriptor")
//					System.Console.WriteLine(type.Attributes + "/" + DomCecilType.GetModifiers (type.Attributes) + "/" + IsInternal (DomCecilType.GetModifiers (type.Attributes)));
				DomCecilType loadType = new DomCecilType (type, loadInternal);
				if (instantiateTypeParameter) {
					resolver.Visit (loadType, null);
					resolver.ClearTypes ();
				}
				Add (loadType);
				module.Types.Add (loadType);
			}
			this.modules.Add (module);
		}
		
		class InstantiatedParamResolver : AbstractDomVisitor<object, object>
		{
			Dictionary<string, IType> argTypes = new Dictionary<string, IType> ();
			
			public InstantiatedParamResolver ()
			{
			}
			
			public void ClearTypes ()
			{
				this.argTypes.Clear ();
			}
			
			public override object Visit (IType type, object data)
			{
				foreach (TypeParameter p in type.TypeParameters)
					argTypes[p.Name] = type;
				foreach (IMember member in type.Members) {
					CheckReturnType (member.ReturnType);
					member.AcceptVisitor (this, data);
				}
				return null;
			}
			
			public override object Visit (IEvent evt, object data)
			{
				return null;
			}
			
			public override object Visit (IField field, object data)
			{
				return null;
			}

			public override object Visit (IMethod method, object data)
			{
				foreach (IParameter param in method.Parameters) {
					CheckReturnType (param.ReturnType);
				}
				return null;
			}

			public override object Visit (IProperty property, object data)
			{
				foreach (IParameter param in property.Parameters) {
					CheckReturnType (param.ReturnType);
				}
				return null;
			}
			
			void CheckReturnType (IReturnType type)
			{
				if (type == null) 
					return;
				IType resultType;
				if (argTypes.TryGetValue (type.FullName, out resultType)) {
					DomReturnType returnType = (DomReturnType)type;
//					Console.Write ("Convert:" + returnType);
					string returnTypeName = returnType.FullName;
					returnType.SetType (resultType);
					returnType.Parts.Add (new ReturnTypePart (returnTypeName, null));
//					Console.WriteLine (" to:" + returnType);
				}
			}
		}
	}
}