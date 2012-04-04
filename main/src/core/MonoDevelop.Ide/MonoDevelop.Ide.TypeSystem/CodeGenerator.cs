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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.TypeSystem
{
	public abstract class CodeGenerator
	{
		static Dictionary<string, MimeTypeExtensionNode> generators = new Dictionary<string, MimeTypeExtensionNode> ();
		
		public bool AutoIndent {
			get;
			set;
		}
		
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

		public virtual PolicyContainer PolicyParent {
			get;
			set;
		}

		public ICompilation Compilation {
			get;
			set;
		}

		public static CodeGenerator CreateGenerator (Ide.Gui.Document doc)
		{
			MimeTypeExtensionNode node;
			if (!generators.TryGetValue (doc.Editor.MimeType, out node))
				return null;

			var result = (CodeGenerator)node.CreateInstance ();
			result.UseSpaceIndent = doc.Editor.Options.TabsToSpaces;
			result.EolMarker = doc.Editor.EolMarker;
			result.TabSize = doc.Editor.Options.TabSize;
			result.Compilation = doc.Compilation;
			return result;
		}
		
		public static CodeGenerator CreateGenerator (TextEditorData editor, ICompilation compilation)
		{
			MimeTypeExtensionNode node;
			if (!generators.TryGetValue (editor.MimeType, out node))
				return null;

			var result = (CodeGenerator)node.CreateInstance ();
			result.UseSpaceIndent = editor.Options.TabsToSpaces;
			result.EolMarker = editor.EolMarker;
			result.TabSize = editor.Options.TabSize;
			result.Compilation = compilation;
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
			AutoIndent = true;
		}

		public static void AddGenerator (MimeTypeExtensionNode node)
		{
			generators [node.MimeType] = node;
		}

		public static void RemoveGenerator (MimeTypeExtensionNode node)
		{
			generators.Remove (node.MimeType);
		}

		protected void SetIndentTo (IUnresolvedTypeDefinition implementingType)
		{
			if (IndentLevel < 0)
				IndentLevel = AutoIndent ? CodeGenerationService.CalculateBodyIndentLevel (implementingType) : 0;
		}

		public string CreateInterfaceImplementation (ITypeDefinition implementingType, IUnresolvedTypeDefinition implementingPart, IType interfaceType, bool explicitly, bool wrapRegions = true)
		{
			SetIndentTo (implementingPart);
			StringBuilder result = new StringBuilder ();
			List<IMember> implementedMembers = new List<IMember> ();
			foreach (var def in interfaceType.GetAllBaseTypes ().Where (bt => bt.Kind == TypeKind.Interface)) {
				if (result.Length > 0) {
					AppendLine (result);
					AppendLine (result);
				}
				string implementation = InternalCreateInterfaceImplementation (implementingType, implementingPart, def, explicitly, implementedMembers);
				if (string.IsNullOrWhiteSpace (implementation))
					continue;
				if (wrapRegions) {
					result.Append (WrapInRegions (def.Name + " implementation", implementation));
				} else {
					result.Append (implementation);
				}
			}
			return result.ToString ();
		}

		static bool CompareMethods (IMethod interfaceMethod, IMethod typeMethod)
		{
			if (typeMethod.IsExplicitInterfaceImplementation)
				return typeMethod.ImplementedInterfaceMembers.Any (m => m.Equals (interfaceMethod));
			return SignatureComparer.Ordinal.Equals (interfaceMethod, typeMethod);
		}

		public static List<KeyValuePair<IMember, bool>> CollectMembersToImplement (ITypeDefinition implementingType, IType interfaceType, bool explicitly)
		{
			var def = interfaceType.GetDefinition ();
			List<KeyValuePair<IMember, bool>> toImplement = new List<KeyValuePair<IMember, bool>> ();
			bool alreadyImplemented;
			// Stub out non-implemented events defined by @iface
			foreach (var ev in interfaceType.GetEvents (e => !e.IsSynthetic && e.DeclaringTypeDefinition.ReflectionName == def.ReflectionName)) {
				bool needsExplicitly = explicitly;
				alreadyImplemented = implementingType.GetAllBaseTypeDefinitions ().Any (x => x.Kind != TypeKind.Interface && x.Events.Any (y => y.Name == ev.Name));

				if (!alreadyImplemented)
					toImplement.Add (new KeyValuePair<IMember, bool> (ev, needsExplicitly));
			}

			// Stub out non-implemented methods defined by @iface
			foreach (var method in interfaceType.GetMethods (d => !d.IsSynthetic && d.DeclaringTypeDefinition.ReflectionName == def.ReflectionName)) {
				bool needsExplicitly = explicitly;
				alreadyImplemented = false;
				
				foreach (var cmet in implementingType.GetMethods ()) {
					if (CompareMethods (method, cmet)) {
						if (!needsExplicitly && !cmet.ReturnType.Equals (method.ReturnType))
							needsExplicitly = true;
						else
							alreadyImplemented |= !needsExplicitly /*|| cmet.InterfaceImplementations.Any (impl => impl.InterfaceType.Equals (interfaceType))*/;
					}
				}
				if (!alreadyImplemented) 
					toImplement.Add (new KeyValuePair<IMember, bool> (method, needsExplicitly));
			}

			// Stub out non-implemented properties defined by @iface
			foreach (var prop in interfaceType.GetProperties (p => !p.IsSynthetic && p.DeclaringTypeDefinition.ReflectionName == def.ReflectionName)) {
				bool needsExplicitly = explicitly;
				alreadyImplemented = false;
				foreach (var t in implementingType.GetAllBaseTypeDefinitions ()) {
					if (t.Kind == TypeKind.Interface)
						continue;
					foreach (IProperty cprop in t.Properties) {
						if (cprop.Name == prop.Name) {
							if (!needsExplicitly && !cprop.ReturnType.Equals (prop.ReturnType))
								needsExplicitly = true;
							else
								alreadyImplemented |= !needsExplicitly/* || cprop.InterfaceImplementations.Any (impl => impl.InterfaceType.Resolve (ctx).Equals (interfaceType))*/;
						}
					}
				}
				if (!alreadyImplemented)
					toImplement.Add (new KeyValuePair<IMember, bool> (prop, needsExplicitly));
			}
			return toImplement;
		}

		protected string InternalCreateInterfaceImplementation (ITypeDefinition implementingType, IUnresolvedTypeDefinition part, IType interfaceType, bool explicitly, List<IMember> implementedMembers)
		{
			StringBuilder result = new StringBuilder ();
			var toImplement = CollectMembersToImplement (implementingType, interfaceType, explicitly);

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
						isExplicit = CompareMethods (othermethod, method);
					} else {
						isExplicit = true;
					}
				}
				result.Append (CreateMemberImplementation (implementingType, part, pair.Key, isExplicit).Code);
				implementedMembers.Add (pair.Key);
			}

			return result.ToString ();
		}

		public abstract string WrapInRegions (string regionName, string text);
		public abstract CodeGeneratorMemberResult CreateMemberImplementation (ITypeDefinition implementingType, IUnresolvedTypeDefinition part, IUnresolvedMember member, bool explicitDeclaration);
		public abstract CodeGeneratorMemberResult CreateMemberImplementation (ITypeDefinition implementingType, IUnresolvedTypeDefinition part, IMember member, bool explicitDeclaration);

		public abstract string CreateFieldEncapsulation (IUnresolvedTypeDefinition implementingType, IField field, string propertyName, Accessibility modifiers, bool readOnly);

		public abstract void AddGlobalNamespaceImport (MonoDevelop.Ide.Gui.Document doc, string nsName);
		public abstract void AddLocalNamespaceImport (MonoDevelop.Ide.Gui.Document doc, string nsName, TextLocation caretLocation);

		public abstract string GetShortTypeString (MonoDevelop.Ide.Gui.Document doc, IType type);

		public abstract void CompleteStatement (MonoDevelop.Ide.Gui.Document doc);
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