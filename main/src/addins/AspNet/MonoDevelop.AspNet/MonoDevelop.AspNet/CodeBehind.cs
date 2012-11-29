// 
// CodeBehind.cs:
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2007 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.DesignerSupport;

using MonoDevelop.AspNet.Parser;
using System.CodeDom;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.AspNet
{
	
	
	public static class CodeBehind
	{
		public static string GetCodeBehindClassName (ProjectFile file)
		{
			AspNetAppProject proj = file.Project as AspNetAppProject;
			if (proj == null)
				return null;
			return proj.GetCodebehindTypeName (file.Name);
		}
		
		static void AddFail (List<CodeBehindWarning> errors, AspNetParsedDocument document, Error err)
		{
			errors.Add (new CodeBehindWarning (GettextCatalog.GetString (
					"Parser failed with error {0}. CodeBehind members for this file will not be added.", err.Message),
					document.FileName, err.Region.BeginLine, err.Region.BeginColumn));
		}
		
		public static System.CodeDom.CodeCompileUnit GenerateCodeBehind (AspNetAppProject project,
		                                                                 string filename,
		                                                                 AspNetParsedDocument document, 
		                                                                 List<CodeBehindWarning> errors)
		{
			string className = document.Info.InheritedClass;
			
			if (document.HasErrors) {
				AddFail (errors, document, document.Errors.Where (x => x.ErrorType == ErrorType.Error).First ());
				return null;
			}
			
			if (string.IsNullOrEmpty (className))
				return null;
			
			var refman = new DocumentReferenceManager (project) { Doc = document };
			var memberList = new MemberListVisitor (refman);
			document.RootNode.AcceptVisit (memberList);
			
			var err = memberList.Errors.Where (x => x.ErrorType == ErrorType.Error).FirstOrDefault ();
			if (err != null) {
				AddFail (errors, document, err);
				return null;
			}
			
			//initialise the generated type
			var ccu = new CodeCompileUnit ();
			var namespac = new CodeNamespace ();
			ccu.Namespaces.Add (namespac); 
			var typeDecl = new System.CodeDom.CodeTypeDeclaration () {
				IsClass = true,
				IsPartial = true,
			};
			namespac.Types.Add (typeDecl);
			
			//name the class and namespace
			int namespaceSplit = className.LastIndexOf ('.');
			if (namespaceSplit > -1) {
				namespac.Name = project.StripImplicitNamespace (className.Substring (0, namespaceSplit));
				typeDecl.Name = className.Substring (namespaceSplit + 1);
			} else {
				typeDecl.Name = className;
			}
			
			string masterTypeName = null;
			if (!String.IsNullOrEmpty (document.Info.MasterPageTypeName)) {
				masterTypeName = document.Info.MasterPageTypeName;
			} else if (!String.IsNullOrEmpty (document.Info.MasterPageTypeVPath)) {
				try {
					ProjectFile resolvedMaster = project.ResolveVirtualPath (document.Info.MasterPageTypeVPath, document.FileName);
					AspNetParsedDocument masterParsedDocument = null;
					if (resolvedMaster != null)
						masterParsedDocument = TypeSystemService.ParseFile (project, resolvedMaster.FilePath) as AspNetParsedDocument;
					if (masterParsedDocument != null && !String.IsNullOrEmpty (masterParsedDocument.Info.InheritedClass)) {
						masterTypeName = masterParsedDocument.Info.InheritedClass;
					} else {
						errors.Add (new CodeBehindWarning (String.Format ("Could not find type for master '{0}'",
						                                                  document.Info.MasterPageTypeVPath),
						                                   document.FileName));
					}
				} catch (Exception ex) {
					errors.Add (new CodeBehindWarning (String.Format ("Could not find type for master '{0}'",
					                                                  document.Info.MasterPageTypeVPath),
					                                   document.FileName));
					LoggingService.LogWarning ("Error resolving master page type", ex);
				}
			}
			
			if (masterTypeName != null) {
				var masterProp = new CodeMemberProperty () {
					Name = "Master",
					Type = new CodeTypeReference (masterTypeName),
					HasGet = true,
					HasSet = false,
					Attributes = System.CodeDom.MemberAttributes.Public | System.CodeDom.MemberAttributes.New 
						| System.CodeDom.MemberAttributes.Final,
				};
				masterProp.GetStatements.Add (new System.CodeDom.CodeMethodReturnStatement (
						new System.CodeDom.CodeCastExpression (masterTypeName, 
							new System.CodeDom.CodePropertyReferenceExpression (
								new System.CodeDom.CodeBaseReferenceExpression (), "Master"))));
				typeDecl.Members.Add (masterProp);
			}
			
			//shortcut building the existing members type map
			if (memberList.Members.Count == 0)
				return ccu;
			
			var dom = refman.TypeCtx.Compilation;
			var cls = ReflectionHelper.ParseReflectionName (className).Resolve (dom);
			var members = GetDesignerMembers (memberList.Members.Values, cls, filename);
			
			//add fields for each control in the page
			
			foreach (var member in members) {
				var type = new CodeTypeReference (member.Type.FullName);
				typeDecl.Members.Add (new CodeMemberField (type, member.Name) { Attributes = MemberAttributes.Family });
			}
			return ccu;
		}
		
		/// <summary>Filters out members whose names conflict with existing accessible members</summary>
		/// <param name="members">Full list of CodeBehind members</param>
		/// <param name="cls">The class to which these members' partial class will be added.</param>
		/// <param name="designerFile">Members in this file will be ignored.</param>
		/// <param name="resolveDom">The ITypeResolveContext to use for resolving base types.</param>
		/// <param name="internalDom">The ITypeResolveContext to use for checking whether members are accessible as internal.</param>
		/// <returns>The filtered list of non-conflicting members.</returns>
		// TODO: check compatibilty with existing members
		public static IEnumerable<CodeBehindMember> GetDesignerMembers (
			IEnumerable<CodeBehindMember> members, IType cls, string designerFile)
		{
			var existingMembers = new HashSet<string> ();
			while (cls != null) {
				if (cls.GetDefinition () == null)
					break;
				foreach (var member in cls.GetMembers ()) {
					if (member.Accessibility == Accessibility.Private || member.Accessibility == Accessibility.Internal)
						continue;
					if (member.Region.FileName == designerFile)
						continue;
					existingMembers.Add (member.Name);
				}
				if (!cls.DirectBaseTypes.Any (t => t.Kind != TypeKind.Interface))
					break;
				cls = cls.DirectBaseTypes.First(t => t.Kind != TypeKind.Interface);
			}
			return members.Where (m => !existingMembers.Contains (m.Name));
		}
	}
}
