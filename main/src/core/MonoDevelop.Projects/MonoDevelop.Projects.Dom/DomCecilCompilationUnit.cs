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
using MonoDevelop.Projects.Dom;
using Mono.Cecil;

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
		
		public DomCecilCompilationUnit (AssemblyDefinition assemblyDefinition) : this (true, true, assemblyDefinition)
		{
		}
		
		public DomCecilCompilationUnit (bool keepDefinitions, bool loadInternals, AssemblyDefinition assemblyDefinition) : base (assemblyDefinition.Name.FullName)
		{
			if (keepDefinitions)
				this.assemblyDefinition = assemblyDefinition;
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
		
		public static ICompilationUnit Load (string fileName)
		{
			return Load (fileName, true);
		}
		public static ICompilationUnit Load (string fileName, bool keepDefinitions)
		{
			return Load (fileName, true, true);
		}
		
		public static ICompilationUnit Load (string fileName, bool keepDefinitions, bool loadInternals)
		{
			if (String.IsNullOrEmpty (fileName))
				return new CompilationUnit (fileName);
			DomCecilCompilationUnit result = new DomCecilCompilationUnit (keepDefinitions, loadInternals, AssemblyFactory.GetAssembly (fileName));
			System.GC.Collect ();
			return result;
		}
		
		public static bool IsInternal (MonoDevelop.Projects.Dom.Modifiers mods)
		{
			return (mods & MonoDevelop.Projects.Dom.Modifiers.Internal) == MonoDevelop.Projects.Dom.Modifiers.Internal ||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.Private) == MonoDevelop.Projects.Dom.Modifiers.Private ||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.ProtectedOrInternal) == MonoDevelop.Projects.Dom.Modifiers.ProtectedOrInternal/* ||
			       (mods & MonoDevelop.Projects.Dom.Modifiers.SpecialName) == MonoDevelop.Projects.Dom.Modifiers.SpecialName*/;
		}
		
		void AddModuleDefinition (bool keepDefinitions, bool loadInternal, ModuleDefinition moduleDefinition)
		{
			foreach (TypeDefinition type in moduleDefinition.Types) {
				if (!loadInternal && IsInternal (DomCecilType.GetModifiers (type.Attributes)))
					continue;
				DomCecilType loadType = new DomCecilType (keepDefinitions, loadInternal, type);
				
				Add (loadType);
			}
		}
	}
}
