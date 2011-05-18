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
		
		
		public static CodeGenerator CreateGenerator (string mimeType, bool useSpaceIndent, int tabSize, string eolMarker)
		{
			MimeTypeExtensionNode node;
			if (!generators.TryGetValue (mimeType, out node))
				return null;
			if (eolMarker == null)
				throw new ArgumentNullException ("eolMarker");
			if (eolMarker.Length == 0 || eolMarker.Length > 2)
				throw new ArgumentException ("invalid eolMarker");
			if (tabSize <= 0)
				throw new ArgumentException ("tabSize <= 0");
			
			var result = (CodeGenerator)node.CreateInstance ();
			result.UseSpaceIndent = useSpaceIndent;
			result.EolMarker = eolMarker;
			result.TabSize = tabSize;
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
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/CodeGenerators", delegate (object sender, ExtensionNodeEventArgs args) {
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
			generators[node.MimeType] = node;
		}
		
		public static void RemoveGenerator (MimeTypeExtensionNode node)
		{
			generators.Remove (node.MimeType);
		}
		
		static int CalculateBodyIndentLevel (IType declaringType)
		{
			int indentLevel = 0;
			IType t = declaringType;
			do {
				indentLevel++;
				t = t.DeclaringType;
			} while (t != null);
			DomLocation lastLoc = DomLocation.Empty;
			foreach (IUsing us in declaringType.CompilationUnit.Usings.Where (u => u.IsFromNamespace && u.ValidRegion.Contains (declaringType.Location))) {
				if (lastLoc == us.Region.Start)
					continue;
				lastLoc = us.Region.Start;
				indentLevel++;
			}
			return indentLevel;
		}
		
		protected void SetIndentTo (IType implementingType)
		{
			if (IndentLevel < 0)
				IndentLevel = CalculateBodyIndentLevel (implementingType);
		}
		
		public string CreateInterfaceImplementation (IType implementingType, IType interfaceType, bool explicitly, bool wrapRegions = true)
		{
			SetIndentTo (implementingType);
			StringBuilder result = new StringBuilder ();
			List<IMember> implementedMembers = new List<IMember> ();
			foreach (IType baseInterface in interfaceType.SourceProjectDom.GetInheritanceTree (interfaceType)) {
				if (baseInterface.ClassType != ClassType.Interface)
					continue;
				if (result.Length > 0) {
					AppendLine (result);
					AppendLine (result);
				}
				string implementation = InternalCreateInterfaceImplementation (implementingType, baseInterface, explicitly, implementedMembers);
				if (wrapRegions) {
					result.Append (WrapInRegions (baseInterface.Name + " implementation", implementation));
				} else {
					result.Append (implementation);
				}
			}
			return result.ToString ();
		}
		
		protected string InternalCreateInterfaceImplementation (IType implementingType, IType interfaceType, bool explicitly, List<IMember> implementedMembers)
		{
			StringBuilder result = new StringBuilder ();
			
			ProjectDom dom = implementingType.SourceProjectDom;
			
			List<KeyValuePair<IMember, bool >> toImplement = new List<KeyValuePair<IMember, bool>> ();
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
					AppendLine (result);
					AppendLine (result);
				} else {
					first = false;
				}
				bool isExplicit = pair.Value;
				foreach (IMember member in implementedMembers.Where (m => m.Name == pair.Key.Name && m.MemberType == pair.Key.MemberType)) {
					if (member.MemberType == MemberType.Method) {
						isExplicit = member.ReturnType.ToInvariantString () != pair.Key.ReturnType.ToInvariantString ();
						if (member.Parameters.Count == pair.Key.Parameters.Count && pair.Key.Parameters.Count > 0) {
							for (int i = 0; i < member.Parameters.Count; i++) {
								if (member.Parameters [i].ReturnType.ToInvariantString () != pair.Key.Parameters [i].ReturnType.ToInvariantString ()) {
									isExplicit = true;
									break;
								}
							}
						}
					} else {
						isExplicit = true;
					}
				}
				
				result.Append (CreateMemberImplementation (implementingType, pair.Key, isExplicit).Code);
				implementedMembers.Add (pair.Key);
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
		public abstract CodeGeneratorMemberResult CreateMemberImplementation (IType implementingType, IMember member, bool explicitDeclaration);
		public abstract string CreateFieldEncapsulation (IType implementingType, IField field, string propertyName, Modifiers modifiers, bool readOnly);
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
			this.BodyRegions = bodyRegions ??  new CodeGeneratorBodyRegion[0];
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