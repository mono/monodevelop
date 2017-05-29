// 
// CecilToolboxItemLoader.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using MC = Mono.Cecil;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	abstract class CecilToolboxItemLoader : IToolboxLoader
	{
		static string[] filetypes = {"dll", "exe"};
		
		public string[] FileTypes {
			get {
				return filetypes;
			}
		}
		
		public IList<ItemToolboxNode> Load (LoaderContext ctx, string filename)
		{
			Mono.Linker.AssemblyResolver resolver = new Mono.Linker.AssemblyResolver ();
			using (MC.AssemblyDefinition assem = MC.AssemblyDefinition.ReadAssembly (filename, new MC.ReaderParameters { AssemblyResolver = resolver })) {
				//Grab types that should be in System.dll
				Version runtimeVersion;
				switch (assem.MainModule.Runtime) {
				case MC.TargetRuntime.Net_1_0:
				case MC.TargetRuntime.Net_1_1:
					runtimeVersion = new Version (1, 0, 5000, 0); break;
				case MC.TargetRuntime.Net_2_0:
					runtimeVersion = new Version (2, 0, 0, 0); break;
				default:
					throw new NotSupportedException ("Runtime '" + assem.MainModule.Runtime + "' is not supported.");
				}

				MC.AssemblyNameReference sysAssem = new MC.AssemblyNameReference ("System", runtimeVersion);
				MC.TypeReference toolboxItemType
					= new MC.TypeReference ("System.ComponentModel", "ToolboxItemAttribute", assem.MainModule, sysAssem, false);
				MC.TypeReference categoryType
					= new MC.TypeReference ("ToolboxItemAttribute", "CategoryAttribute", assem.MainModule, sysAssem, false);

				if (resolver.Resolve (toolboxItemType) == null)
					return null;

				List<ItemToolboxNode> list = new List<ItemToolboxNode> ();

				foreach (MC.ModuleDefinition module in assem.Modules) {
					foreach (MC.TypeDefinition typedef in module.Types) {
						//only show visible concrete classes
						if (typedef.IsAbstract || !typedef.IsClass || !typedef.IsPublic)
							continue;

						MC.TypeDefinition toolboxItem = null;
						string category = null;

						//find the ToolboxItem and Category attributes
						foreach (MC.CustomAttribute att in AllCustomAttributes (resolver, typedef)) {

							//find the value of the toolbox attribute
							if (toolboxItem == null
								&& IsEqualOrSubclass (resolver, att.Constructor.DeclaringType, toolboxItemType)) {

								//check if it's ToolboxItemAttribute(false) (or null)

								IList args = GetArgumentsToBaseConstructor (att, toolboxItemType);
								if (args != null) {
									if (args.Count != 1)
										throw new InvalidOperationException ("Malformed toolboxitem attribute");
									object o = args [0];
									if (o == null || (o is bool && ((bool)o) == false)) {
										break;
									} else {
										//w have a type name for the toolbox item. try to resolve it
										string typeName = (string)o;
										toolboxItem = null;
										try {
											resolver.Resolve (TypeReferenceFromString (module, typeName));
										} catch (Exception ex) {
											System.Diagnostics.Debug.WriteLine (ex);
										}
										if (toolboxItem == null) {
											System.Diagnostics.Debug.WriteLine (
												"CecilToolboxItemScanner: Error resolving type "
												+ typeName);
											break;
										}
									}
								}
							}

							//find the value of the category attribute
							if (category == null
								&& IsEqualOrSubclass (resolver, att.Constructor.DeclaringType, categoryType)) {

								IList args = GetArgumentsToBaseConstructor (att, categoryType);
								if (args != null && args.Count == 1)
									category = (string)args [0];
								else
									throw new InvalidOperationException ("Malformed category attribute");
							}

							if (toolboxItem != null && category != null)
								break;
						}

						if (toolboxItem == null)
							continue;

						Load (assem, typedef, toolboxItem, category);
					}
				}
				return list;
			}
		}
		
		protected static MC.TypeReference TypeReferenceFromString (MC.ModuleDefinition module, string fullname)
		{
			string[] split = fullname.Split (',');
			
			int lastTypeDot = split[0].LastIndexOf ('.');
			System.Diagnostics.Debug.Assert (lastTypeDot > 0);
			System.Diagnostics.Debug.Assert (split.Length >= 2);
			
			string namespc = split[0].Substring (0, lastTypeDot).Trim ();
			string type = split[0].Substring (lastTypeDot + 1).Trim ();
			string assem = split[1].Trim ();
			
			string culture = string.Empty;
			Version version = new Version ();
			for (int i = 2; i < split.Length; i++) {
				string val = split[i].Trim ();
				if (val.StartsWith ("Version="))
					version = new Version (val.Substring (8));
				else if (val.StartsWith ("Culture="))
					culture = val.Substring (8);
				//PublicKeyToken=
			}
			
			MC.AssemblyNameReference itemAssem = new MC.AssemblyNameReference (assem, version) {
				Culture = culture,
			};
			return new MC.TypeReference (type, namespc, module, itemAssem, false);
		}
		
		protected static IList GetArgumentsToBaseConstructor (MC.CustomAttribute att, MC.TypeReference baseType)
		{
			if (att.AttributeType.FullName == baseType.FullName)
				return att.ConstructorArguments.Select (arg => arg.Value).ToList ();
			else
				//FIXME: Implement
				throw new NotImplementedException ();
		}
		
		protected static IEnumerable<MC.CustomAttribute> AllCustomAttributes (Mono.Linker.AssemblyResolver resolver, MC.TypeDefinition typedef)
		{
			MC.TypeDefinition currentType = typedef;
			while (true) {
				foreach (MC.CustomAttribute attr in currentType.CustomAttributes)
					yield return attr;
				if (currentType.BaseType.FullName == "System.Object")
					yield break;
				currentType = resolver.Resolve (currentType.BaseType);
			}
		}
					
		protected static bool IsEqualOrSubclass (Mono.Linker.AssemblyResolver resolver, MC.TypeReference inspect, MC.TypeReference baseClass)
		{
			MC.TypeDefinition currentType = resolver.Resolve (inspect);
			
			while (true) {
				if (baseClass.FullName == currentType.FullName)
					return true;
				if (currentType.BaseType.FullName == "System.Object")
					return false;
				currentType = resolver.Resolve (currentType.BaseType);
			}
		}
		
		protected static bool ImplementsInterface (Mono.Linker.AssemblyResolver resolver, MC.TypeReference inspect, MC.TypeReference interf)
		{
			MC.TypeDefinition currentType = resolver.Resolve (inspect);
			foreach (MC.InterfaceImplementation childDefinition in currentType.Interfaces) {
				var child = childDefinition.InterfaceType;
				if (child.FullName == interf.FullName)
					return true;
				if (ImplementsInterface (resolver, child, interf))
					return true;
			}
			if (ImplementsInterface (resolver, currentType.BaseType, interf))
				return true;
			return false;
		}
		
		protected abstract ItemToolboxNode Load (
		    MC.AssemblyDefinition assem, MC.TypeDefinition componentType,
		    MC.TypeDefinition itemType, string category);
	}
}
