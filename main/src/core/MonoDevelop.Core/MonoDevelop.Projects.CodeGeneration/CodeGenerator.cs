// 
// CodeGenerator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Mono.Addins;

namespace MonoDevelop.Projects.CodeGeneration
{
	public abstract class CodeGenerator
	{
		static Dictionary<string, MimeTypeExtensionNode> generators = new Dictionary<string, MimeTypeExtensionNode> ();
		
		public static CodeGenerator CreateGenerator (string mimeType)
		{
			MimeTypeExtensionNode node;
			if (!generators.TryGetValue (mimeType, out node))
				return null;
			return (CodeGenerator)node.CreateInstance ();
		}
		
		public static bool HasGenerator (string mimeType)
		{
			return generators.ContainsKey (mimeType);
		}
		
		static CodeGenerator ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/CodeGenerators", delegate (object sender, ExtensionNodeEventArgs args) {
				var node = (MimeTypeExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					generators[node.MimeType] = node;
					break;
				case ExtensionChange.Remove:
					generators.Remove (node.MimeType);
					break;
				}
			});
		}
		
		public int IndentLevel {
			get;
			set;
		}
		
		public string CreateInterfaceImplementation (IType implementingType, IType interfaceType, bool explicitly)
		{
			StringBuilder result = new StringBuilder ();
			foreach (IType baseInterface in interfaceType.SourceProjectDom.GetInheritanceTree (interfaceType)) {
				if (baseInterface.FullName == DomReturnType.Object.FullName)
					break;
				if (result.Length > 0) {
					result.AppendLine ();
					result.AppendLine ();
				}
				string implementation = InternalCreateInterfaceImplementation (implementingType, baseInterface, explicitly);
				result.Append (WrapInRegions (baseInterface.Name + " implementation", implementation));
			}
			return result.ToString ();
		}
		
		protected string InternalCreateInterfaceImplementation (IType implementingType, IType interfaceType, bool explicitly)
		{
			StringBuilder result = new StringBuilder ();
			
			ProjectDom dom = implementingType.SourceProjectDom;
			
			List<KeyValuePair<IMember, bool>> toImplement = new List<KeyValuePair<IMember, bool>> ();
			bool alreadyImplemented;
			
			// Stub out non-implemented events defined by @iface
			foreach (IEvent ev in interfaceType.Events) {
				if (ev.IsSpecialName)
					continue;
				bool needsExplicitly = explicitly;
				
				alreadyImplemented = dom.GetInheritanceTree (implementingType).Any (x => x.ClassType != ClassType.Interface && x.Events.Any (y => y.Name == ev.Name));
				
				if (!alreadyImplemented)
					toImplement.Add (new KeyValuePair<IMember, bool> (ev, needsExplicitly));
			}
			
			// Stub out non-implemented methods defined by @iface
			foreach (IMethod method in interfaceType.Methods) {
				if (method.IsSpecialName)
					continue;
				bool needsExplicitly = explicitly;
				alreadyImplemented = false;
				foreach (IType t in dom.GetInheritanceTree (implementingType)) {
					if (t.ClassType == ClassType.Interface)
						continue;
					foreach (IMethod cmet in t.Methods) {
						if (cmet.Name == method.Name && Equals (cmet.Parameters, method.Parameters)) {
							if (!needsExplicitly && !cmet.ReturnType.Equals (method.ReturnType))
								needsExplicitly = true;
							else
								alreadyImplemented |= !needsExplicitly || (interfaceType.FullName == GetExplicitPrefix (cmet.ExplicitInterfaces));
						}
					}
				}
				
				if (!alreadyImplemented) 
					toImplement.Add (new KeyValuePair<IMember, bool> (method, needsExplicitly));
			}
			
			// Stub out non-implemented properties defined by @iface
			foreach (IProperty prop in interfaceType.Properties) {
				if (prop.IsSpecialName)
					continue;
				bool needsExplicitly = explicitly;
				alreadyImplemented = false;
				foreach (IType t in dom.GetInheritanceTree (implementingType)) {
					if (t.ClassType == ClassType.Interface)
						continue;
					foreach (IProperty cprop in t.Properties) {
						if (cprop.Name == prop.Name) {
							if (!needsExplicitly && !cprop.ReturnType.Equals (prop.ReturnType))
								needsExplicitly = true;
							else
								alreadyImplemented |= !needsExplicitly || (interfaceType.FullName == GetExplicitPrefix (cprop.ExplicitInterfaces));
						}
					}
				}
				if (!alreadyImplemented)
					toImplement.Add (new KeyValuePair<IMember, bool> (prop, needsExplicitly));
			}
			bool first = true;
			foreach (var pair in toImplement) {
				if (!first) {
					result.AppendLine ();
					result.AppendLine ();
				} else {
					first = false;
				}
				result.Append (CreateMemberImplementation (pair.Key, pair.Value));
			}
			
			return result.ToString ();
		}
		
		static string GetExplicitPrefix (IEnumerable<IReturnType> explicitInterfaces)
		{	
			if (explicitInterfaces != null) {
				foreach (IReturnType retType in explicitInterfaces) {
					return retType.FullName;
				}
			}
			return null;
		}
		
		public abstract string WrapInRegions (string regionName, string text);
		public abstract string CreateMemberImplementation (IMember member, bool explicitDeclaration);
	}
}