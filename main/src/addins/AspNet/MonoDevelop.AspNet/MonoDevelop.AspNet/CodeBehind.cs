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

		public static ProjectFile GetDesignerFile (ProjectFile file)
		{
			var project = file.Project as AspNetAppProject;

			var type = AspNetAppProject.DetermineWebSubtype (file.FilePath);
			if (type != WebSubtype.WebForm && type != WebSubtype.WebControl && type != WebSubtype.MasterPage)
				return null;

			var dfName = project.LanguageBinding.GetFileName (file.FilePath + ".designer");
			return project.Files.GetFile (dfName);
		}

		public static BuildResult UpdateDesignerFile (
			CodeBehindWriter writer,
			AspNetAppProject project,
			ProjectFile file, ProjectFile designerFile
		)
		{
			var result = new BuildResult ();

			//parse the ASP.NET file
			var parsedDocument = TypeSystemService.ParseFile (project, file.FilePath) as AspNetParsedDocument;
			if (parsedDocument == null) {
				result.AddError (string.Format ("Failed to parse file '{0}'", file.Name));
				return result;
			}

			//TODO: ensure type system is up to date

			CodeCompileUnit ccu;
			result.Append (GenerateCodeBehind (project, designerFile.FilePath, parsedDocument, out ccu));
			if (ccu != null) {
				writer.WriteFile (designerFile.FilePath, ccu);
			}

			return result;
		}

		static void AddErrorsToResult (BuildResult result, string filename, IList<Error> errors)
		{
			foreach (var err in errors) {
				if (err.ErrorType == ErrorType.Warning)
					result.AddWarning (filename, err.Region.BeginLine, err.Region.BeginColumn, null, err.Message);
				else
					result.AddError (filename, err.Region.BeginLine, err.Region.BeginColumn, null, err.Message);
			}
		}
		
		public static BuildResult GenerateCodeBehind (
			AspNetAppProject project,
			string filename,
			AspNetParsedDocument document,
			out CodeCompileUnit ccu)
		{
			ccu = null;
			var result = new BuildResult ();
			string className = document.Info.InheritedClass;
			AddErrorsToResult (result, filename, document.Errors);
			if (result.ErrorCount > 0)
				return result;
			
			if (string.IsNullOrEmpty (className))
				return result;
			
			var refman = new DocumentReferenceManager (project) { Doc = document };
			var memberList = new MemberListBuilder (refman, document.XDocument);
			memberList.Build ();

			AddErrorsToResult (result, filename, memberList.Errors);
			if (result.ErrorCount > 0)
				return result;
			
			//initialise the generated type
			ccu = new CodeCompileUnit ();
			var namespac = new CodeNamespace ();
			ccu.Namespaces.Add (namespac); 
			var typeDecl = new CodeTypeDeclaration {
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
					if (masterParsedDocument != null && !String.IsNullOrEmpty (masterParsedDocument.Info.InheritedClass))
						masterTypeName = masterParsedDocument.Info.InheritedClass;
				} catch (Exception ex) {
					LoggingService.LogWarning ("Error resolving master page type", ex);
				}
				if (string.IsNullOrEmpty (masterTypeName)) {
					var msg = string.Format ("Could not find type for master '{0}'", document.Info.MasterPageTypeVPath);
					result.AddError (filename, msg);
					return result;
				}
			}
			
			if (masterTypeName != null) {
				var masterProp = new CodeMemberProperty {
					Name = "Master",
					Type = new CodeTypeReference (masterTypeName),
					HasGet = true,
					HasSet = false,
					Attributes = MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final,
				};
				masterProp.GetStatements.Add (new CodeMethodReturnStatement (
						new CodeCastExpression (masterTypeName, 
							new CodePropertyReferenceExpression (
								new CodeBaseReferenceExpression (), "Master"))));
				typeDecl.Members.Add (masterProp);
			}
			
			//shortcut building the existing members type map
			if (memberList.Members.Count == 0)
				return result;
			
			var dom = refman.TypeCtx.Compilation;
			var cls = ReflectionHelper.ParseReflectionName (className).Resolve (dom);
			var members = GetDesignerMembers (memberList.Members.Values, cls, filename);
			
			//add fields for each control in the page
			
			foreach (var member in members) {
				var type = new CodeTypeReference (member.Type.FullName);
				typeDecl.Members.Add (new CodeMemberField (type, member.Name) { Attributes = MemberAttributes.Family });
			}
			return result;
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
