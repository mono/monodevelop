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
		Dictionary<string, string> xmlDocumentation = null;
		public AssemblyDefinition AssemblyDefinition {
			get {
				return assemblyDefinition;
			}
		}
//		
//		public DomCecilCompilationUnit (AssemblyDefinition assemblyDefinition) : this (true, true, assemblyDefinition)
//		{
//		}
		
		public DomCecilCompilationUnit (bool keepDefinitions, string xmlFileName, bool loadInternals, AssemblyDefinition assemblyDefinition) : base (assemblyDefinition.Name.FullName)
		{
			if (keepDefinitions)
				this.assemblyDefinition = assemblyDefinition;
			if (xmlFileName != null && File.Exists (xmlFileName)) {
				xmlDocumentation = new Dictionary<string, string> ();
				try {
					using (XmlReader reader = new XmlTextReader (xmlFileName)) {
						while (reader.Read ()) {
							if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "member") {
								string memberName = reader.GetAttribute ("name");
								string innerXml   = reader.ReadInnerXml ();
								xmlDocumentation[memberName] = innerXml.Trim ();
							}
						}
					}
				} catch (Exception e) {
					LoggingService.LogWarning ("Can't load xml documentation: " + e.Message);
					xmlDocumentation = null;
				}
			}
			foreach (ModuleDefinition moduleDefinition in assemblyDefinition.Modules) {
				AddModuleDefinition (keepDefinitions, loadInternals, moduleDefinition);
			}
			
		}
		
		public void CleanCecilDefinitions ()
		{
			assemblyDefinition = null;
			foreach (IType type in Types) {
				DomCecilType cecilType = type as DomCecilType;
				if (cecilType != null) 
					cecilType.CleanCecilDefinitions ();
			}
			System.GC.Collect ();
		}
		
		public static DomCecilCompilationUnit Load (string fileName)
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
		
		public static DomCecilCompilationUnit Load (string fileName, bool keepDefinitions, bool loadInternals, bool loadXmlDocumentation)
		{
			if (String.IsNullOrEmpty (fileName))
				return null;
			string xmlFileName = null;
			if (loadXmlDocumentation)
				xmlFileName = System.IO.Path.ChangeExtension (fileName, ".xml");
			//FIXME: should assign a custom resolver to the AssemblyDefinition so that it resolves from the correct GAC
			DomCecilCompilationUnit result = new DomCecilCompilationUnit (keepDefinitions, xmlFileName, loadInternals, AssemblyFactory.GetAssembly (fileName));
			result.fileName = fileName;
			return result;
		}
		
		public static bool IsInternal (MonoDevelop.Projects.Dom.Modifiers mods)
		{
			return (mods & MonoDevelop.Projects.Dom.Modifiers.Internal) == MonoDevelop.Projects.Dom.Modifiers.Internal ||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.Private) == MonoDevelop.Projects.Dom.Modifiers.Private ||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.ProtectedAndInternal) == MonoDevelop.Projects.Dom.Modifiers.ProtectedAndInternal
/*					||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.ProtectedOrInternal) == MonoDevelop.Projects.Dom.Modifiers.ProtectedOrInternal ||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.SpecialName) == MonoDevelop.Projects.Dom.Modifiers.SpecialName*/;
		}
		
		void AddModuleDefinition (bool keepDefinitions, bool loadInternal, ModuleDefinition moduleDefinition)
		{
			InstantiatedParamResolver resolver = new InstantiatedParamResolver (xmlDocumentation);
			foreach (TypeDefinition type in moduleDefinition.Types) {
				// filter nested types, they're handled in DomCecilType.
				if ((type.Attributes & TypeAttributes.NestedPublic) == TypeAttributes.NestedPublic ||
				    (type.Attributes & TypeAttributes.NestedFamily) == TypeAttributes.NestedFamily || 
				    (type.Attributes & TypeAttributes.NestedAssembly) == TypeAttributes.NestedAssembly ||
				    (type.Attributes & TypeAttributes.NestedPrivate) == TypeAttributes.NestedPrivate || 
				    (type.Attributes & TypeAttributes.NestedFamANDAssem) == TypeAttributes.NestedFamANDAssem)
					continue;
				if (!loadInternal && IsInternal (DomCecilType.GetModifiers (type.Attributes)))
					continue;
//				if (type.Name == "SimplePropertyDescriptor")
//					System.Console.WriteLine(type.Attributes + "/" + DomCecilType.GetModifiers (type.Attributes) + "/" + IsInternal (DomCecilType.GetModifiers (type.Attributes)));
				DomCecilType loadType = new DomCecilType (keepDefinitions, loadInternal, type);
				
				Add ((IType)resolver.Visit (loadType, null));
			}
		}
		
		class InstantiatedParamResolver: CopyDomVisitor<object>
		{
			Dictionary<string, string> xmlDocumentation;
			Dictionary<string, IType> argTypes;
			
			public InstantiatedParamResolver (Dictionary<string, string> xmlDocumentation)
			{
				this.xmlDocumentation = xmlDocumentation;
			}
			void AddDocumentation (IMember member)
			{
				if (xmlDocumentation == null || member == null)
					return;
				string doc;
				if (xmlDocumentation.TryGetValue (member.HelpUrl, out doc))
					member.Documentation = doc;
			}
				
			public override INode Visit (IType type, object data)
			{
				if (type.TypeParameters.Count > 0) {
					var oldTypes = argTypes;
					argTypes = oldTypes != null ? new Dictionary<string, IType> (oldTypes) : new Dictionary<string, IType> ();
					foreach (TypeParameter p in type.TypeParameters)
						argTypes[p.Name] = type;
					argTypes = oldTypes;
				}
				IType result = (IType)base.Visit (type, data);
				AddDocumentation (result);
				foreach (IMember member in result.Members) {
					AddDocumentation (member);
				}
				return result;
			}
			
			public override INode Visit (IReturnType type, object data)
			{
				if (argTypes != null) {
					IType res;
					if (argTypes.TryGetValue (type.FullName, out res)) {
						DomReturnType rt = new DomReturnType (res);
						rt.Parts.Add (new ReturnTypePart (type.FullName, null));
						rt.PointerNestingLevel = type.PointerNestingLevel;
						rt.SetDimensions (type.GetDimensions ());
						return rt;
					}
				}
				return base.Visit (type, data);
			}
		}
	}
}
