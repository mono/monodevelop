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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Mono.Addins;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.TextEditor;
using MonoDevelop.Core.AddIns;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.TypeSystem
{
	public abstract class CodeGenerator
	{
		static Dictionary<string, MimeTypeExtensionNode> generators = new Dictionary<string, MimeTypeExtensionNode> ();
		
		public bool UseSpaceIndent {
			get;
			set;
		}
		
		public string EolMarker {
			get;
			set;
		}
		
		public int TabSize {
			get;
			set;
		}
		
		public static CodeGenerator CreateGenerator (TextEditorData data)
		{
			MimeTypeExtensionNode node;
			if (!generators.TryGetValue (data.MimeType, out node))
				return null;
			
			var result = (CodeGenerator)node.CreateInstance ();
			result.UseSpaceIndent = data.Options.TabsToSpaces;
			result.EolMarker = data.EolMarker;
			result.TabSize = data.Options.TabSize;
			return result;
		}
		
		protected void AppendLine (StringBuilder sb)
		{
			sb.Append (EolMarker);
		}
		
		protected string GetIndent (int indentLevel)
		{
			if (UseSpaceIndent) 
				return new string (' ', indentLevel * TabSize);
				
			return new string ('\t', indentLevel);
		}
		
		public static bool HasGenerator (string mimeType)
		{
			return generators.ContainsKey (mimeType);
		}
		
		static CodeGenerator ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/CodeGenerators", delegate (object sender, ExtensionNodeEventArgs args) {
				var node = (MimeTypeExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					AddGenerator (node);
					break;
				case ExtensionChange.Remove:
					RemoveGenerator (node);
					break;
				}
			});
		}
		
		public int IndentLevel {
			get;
			set;
		}
		
		public CodeGenerator ()
		{
			IndentLevel = -1;
		}
		
		public static void AddGenerator (MimeTypeExtensionNode node)
		{
			Console.WriteLine ("add generator :"+ node.MimeType);
			generators [node.MimeType] = node;
		}
		
		public static void RemoveGenerator (MimeTypeExtensionNode node)
		{
			generators.Remove (node.MimeType);
		}
		
		protected void SetIndentTo (ITypeDefinition implementingType)
		{
			if (IndentLevel < 0)
				IndentLevel = CodeGenerationService.CalculateBodyIndentLevel (implementingType);
		}
		
		public string CreateInterfaceImplementation (ITypeResolveContext ctx, ITypeDefinition implementingType, IType interfaceType, bool explicitly, bool wrapRegions = true)
		{
			SetIndentTo (implementingType);
			StringBuilder result = new StringBuilder ();
			List<IMember > implementedMembers = new List<IMember> ();
			foreach (var baseInterface in interfaceType.GetAllBaseTypes (interfaceType.GetDefinition ().ProjectContent)) {
				if (baseInterface.GetDefinition ().ClassType != ClassType.Interface)
					continue;
				if (result.Length > 0) {
					AppendLine (result);
					AppendLine (result);
				}
				string implementation = InternalCreateInterfaceImplementation (ctx, implementingType, baseInterface, explicitly, implementedMembers);
				if (wrapRegions) {
					result.Append (WrapInRegions (baseInterface.Name + " implementation", implementation));
				} else {
					result.Append (implementation);
				}
			}
			return result.ToString ();
		}
		
		protected string InternalCreateInterfaceImplementation (ITypeResolveContext ctx, ITypeDefinition implementingType, IType interfaceType, bool explicitly, List<IMember> implementedMembers)
		{
			StringBuilder result = new StringBuilder ();
			
			var dom = implementingType.GetProjectContent ();
			
			List<KeyValuePair<IMember, bool >> toImplement = new List<KeyValuePair<IMember, bool>> ();
			bool alreadyImplemented;
			
			// Stub out non-implemented events defined by @iface
			foreach (var ev in interfaceType.GetEvents (dom)) {
				if (ev.IsSynthetic)
					continue;
				bool needsExplicitly = explicitly;
				
				alreadyImplemented = implementingType.GetAllBaseTypes (dom).Any (x => x.GetDefinition ().ClassType != ClassType.Interface && x.GetEvents (dom).Any (y => y.Name == ev.Name));
				
				if (!alreadyImplemented)
					toImplement.Add (new KeyValuePair<IMember, bool> (ev, needsExplicitly));
			}
			
			// Stub out non-implemented methods defined by @iface
			foreach (var method in interfaceType.GetMethods (dom)) {
				if (method.IsSynthetic)
					continue;
				bool needsExplicitly = explicitly;
				alreadyImplemented = false;
				foreach (var t in implementingType.GetAllBaseTypeDefinitions (dom)) {
					if (t.ClassType == ClassType.Interface)
						continue;
					foreach (var cmet in t.GetMethods (dom)) {
						if (cmet.Name == method.Name && Equals (cmet.Parameters, method.Parameters)) {
							if (!needsExplicitly && !cmet.ReturnType.Equals (method.ReturnType))
								needsExplicitly = true;
							else
								alreadyImplemented |= !needsExplicitly || cmet.InterfaceImplementations.Any (impl => impl.InterfaceType.Resolve (ctx).Equals (interfaceType));
						}
					}
				}
				if (!alreadyImplemented) 
					toImplement.Add (new KeyValuePair<IMember, bool> (method, needsExplicitly));
			}
			
			// Stub out non-implemented properties defined by @iface
			foreach (var prop in interfaceType.GetProperties (dom)) {
				if (prop.IsSynthetic)
					continue;
				bool needsExplicitly = explicitly;
				alreadyImplemented = false;
				foreach (IType t in implementingType.GetBaseTypes (dom)) {
					if (t.GetDefinition ().ClassType == ClassType.Interface)
						continue;
					foreach (IProperty cprop in t.GetProperties (dom)) {
						if (cprop.Name == prop.Name) {
							if (!needsExplicitly && !cprop.ReturnType.Equals (prop.ReturnType))
								needsExplicitly = true;
							else
								alreadyImplemented |= !needsExplicitly || cprop.InterfaceImplementations.Any (impl => impl.InterfaceType.Resolve (ctx).Equals (interfaceType));
						}
					}
				}
				if (!alreadyImplemented)
					toImplement.Add (new KeyValuePair<IMember, bool> (prop, needsExplicitly));
			}
			bool first = true;
			foreach (var pair in toImplement) {
				if (!first) {
					AppendLine (result);
					AppendLine (result);
				} else {
					first = false;
				}
				bool isExplicit = pair.Value;
				foreach (var member in implementedMembers.Where (m => m.Name == pair.Key.Name && m.EntityType == pair.Key.EntityType)) {
					if (member is IMethod && pair.Key is IMethod) {
						var method = (IMethod)member;
						var othermethod = (IMethod)pair.Key;
						isExplicit = member.ReturnType.Equals (othermethod.ReturnType);
						if (method.Parameters.Count == othermethod.Parameters.Count && othermethod.Parameters.Count > 0) {
							for (int i = 0; i < method.Parameters.Count; i++) {
								if (!method.Parameters [i].Type.Equals (othermethod.Parameters [i].Type)) {
									isExplicit = true;
									break;
								}
							}
						}
					} else {
						isExplicit = true;
					}
				}
				
				result.Append (CreateMemberImplementation (ctx, implementingType, pair.Key, isExplicit).Code);
				implementedMembers.Add (pair.Key);
			}
			
			return result.ToString ();
		}
		
		public abstract string WrapInRegions (string regionName, string text);
		public abstract CodeGeneratorMemberResult CreateMemberImplementation (ITypeResolveContext ctx, ITypeDefinition implementingType, IMember member, bool explicitDeclaration);
		public abstract string CreateFieldEncapsulation (ITypeDefinition implementingType, IField field, string propertyName, Accessibility modifiers, bool readOnly);
		
		public abstract void AddGlobalNamespaceImport (MonoDevelop.Ide.Gui.Document doc, string nsName);
		public abstract void AddLocalNamespaceImport (MonoDevelop.Ide.Gui.Document doc, string nsName, AstLocation caretLocation);

		public abstract string GetShortTypeString (MonoDevelop.Ide.Gui.Document doc, IType type);
	}
	
	public class CodeGeneratorMemberResult
	{
		public CodeGeneratorMemberResult (string code) : this (code, null)
		{
		}
		
		public CodeGeneratorMemberResult (string code, int bodyStartOffset, int bodyEndOffset)
		{
			this.Code = code;
			this.BodyRegions = new CodeGeneratorBodyRegion[] {
				new CodeGeneratorBodyRegion (bodyStartOffset, bodyEndOffset)
			};
		}
		
		public CodeGeneratorMemberResult (string code, IList<CodeGeneratorBodyRegion> bodyRegions)
		{
			this.Code = code;
			this.BodyRegions = bodyRegions ?? new CodeGeneratorBodyRegion[0];
		}
		
		public string Code { get; private set; }

		public IList<CodeGeneratorBodyRegion> BodyRegions { get; private set; }
	}
	
	public class CodeGeneratorBodyRegion
	{
		public CodeGeneratorBodyRegion (int startOffset, int endOffset)
		{
			this.StartOffset = startOffset;
			this.EndOffset = endOffset;
		}

		public int StartOffset { get; private set; }

		public int EndOffset { get; private set; }
		
		public int Length {
			get {
				return EndOffset - StartOffset;
			}
		}
		
		public bool IsValid {
			get {
				return StartOffset >= 0 && Length >= 0;
			}
		}
	}
}