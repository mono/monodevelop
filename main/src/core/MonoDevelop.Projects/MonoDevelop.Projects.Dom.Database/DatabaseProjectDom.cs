// DatabaseProjectDom.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom.Database
{
	public class DatabaseProjectDom : ProjectDom
	{
		MonoDevelop.Projects.Dom.Database.CodeCompletionDatabase codeCompletionDatabase;
		long projectId;
		List<long> referencedAssemblyIds = new List<long> ();
		List<long> referencedProjectIds = new List<long> ();
		
		List<long> projectIds = null;
		IEnumerable<long> ProjectIds {
			get {
				if (projectIds == null) {
					projectIds = new List<long> (referencedProjectIds);
					projectIds.Add (projectId);
				}
				return projectIds;
			}
		}
		
		public override IEnumerable<IType> Types {
			get {
				foreach (IType type in codeCompletionDatabase.GetTypeList (new long[] { projectId })) {
					type.SourceProjectDom = this;
					yield return type;
				}
			}
		}
		
		public long ProjectId {
			get {
				return projectId;
			}
			set {
				projectId = value;
				projectIds = null;
			}
		}
		
		public override void AddReference (ProjectDom dom)
		{
			DatabaseProjectDom dpd = dom as DatabaseProjectDom;
			if (dpd != null) {
				if (dpd.codeCompletionDatabase == ProjectDomService.AssemblyDatabase) {
					referencedAssemblyIds.Add (dpd.ProjectId);
					return;
				}
				if (dpd.codeCompletionDatabase == codeCompletionDatabase) {
					referencedProjectIds.Add (dpd.ProjectId);
					projectIds = null;
					return;
				}
			}
			base.AddReference (dom);
		}
		
		public DatabaseProjectDom (MonoDevelop.Projects.Dom.Database.CodeCompletionDatabase codeCompletionDatabase)
		{
			this.codeCompletionDatabase = codeCompletionDatabase;
			this.projectId              = -1;
		}
		
		internal override void GetNamespaceContentsInternal (List<IMember> result, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			codeCompletionDatabase.GetNamespaceContents (result, ProjectIds, subNamespaces, caseSensitive);
		}
		
		public override IEnumerable<IReturnType> GetSubclasses (IType type)
		{
			foreach (IReturnType result in codeCompletionDatabase.GetSubclasses (type, new long [] { projectId })) {
				yield return result;
			}
			
			foreach (ProjectDom reference in references) {
				foreach (IReturnType result in reference.GetSubclasses (type)) {
					yield return result;
				}
			}
			
			foreach (IReturnType result in ProjectDomService.AssemblyDatabase.GetSubclasses (type, referencedAssemblyIds)) {
				yield return result;
			}
		}
		
		
		public override List<IMember> GetNamespaceContents (IEnumerable<string> subNamespaces, bool includeReferences, bool caseSensitive)
		{
			List<IMember> result = new List<IMember> ();
			if (includeReferences) {
				codeCompletionDatabase.GetNamespaceContents (result, ProjectIds, subNamespaces, caseSensitive);
			} else {
				GetNamespaceContentsInternal (result, subNamespaces, caseSensitive);
			}
			if (includeReferences) {
				foreach (ProjectDom reference in references) {
					reference.GetNamespaceContentsInternal (result, subNamespaces, caseSensitive);
				}
				ProjectDomService.AssemblyDatabase.GetNamespaceContents (result, referencedAssemblyIds, subNamespaces, caseSensitive);
			}
			return result;
		}
		
		protected override IType GetType (IEnumerable<string> subNamespaces, string fullName, int genericParameterCount, bool caseSensitive)
		{
			IType result = null;
			foreach (IType type in codeCompletionDatabase.GetTypes (subNamespaces, new long[] { projectId }, fullName, caseSensitive)) {
				if (genericParameterCount < 0 || 
				    (genericParameterCount == 0 && type.TypeParameters == null) || 
				    (type.TypeParameters != null && type.TypeParameters.Count == genericParameterCount)) {
					type.SourceProjectDom = this;
					if (result == null) {
						result = type;
					} else {
						result = CompoundType.Merge (result, type);
					}
				}
			}
			return result;
		}
		
		public override IType GetType (IEnumerable<string> subNamespaces, string fullName, int genericParameterCount, bool caseSensitive, bool searchDeep)
		{
			if (String.IsNullOrEmpty (fullName))
				return null;
			
			IType result = null;
			if (searchDeep) {
				foreach (IType type in codeCompletionDatabase.GetTypes (subNamespaces, ProjectIds, fullName, caseSensitive)) {
					if (genericParameterCount < 0 || (genericParameterCount == 0 && type.TypeParameters == null) ||  (type.TypeParameters != null && type.TypeParameters.Count == genericParameterCount)) {
						type.SourceProjectDom = this;
						if (result == null) {
							result = type;
						} else {
							result = CompoundType.Merge (result, type);
						}
					}
				}
			} else {
				result = GetType (subNamespaces, fullName, genericParameterCount, caseSensitive);
			}
			if (result == null && searchDeep) {
				foreach (IType type in ProjectDomService.AssemblyDatabase.GetTypes (subNamespaces, referencedAssemblyIds, fullName, caseSensitive)) {
					if (genericParameterCount < 0 || (genericParameterCount == 0 && type.TypeParameters == null) ||  (type.TypeParameters != null && type.TypeParameters.Count == genericParameterCount)) {
						if (result == null) {
							result = type;
						} else {
							result = CompoundType.Merge (result, type);
						}
					}
				}
				
				foreach (ProjectDom reference in references) {
					result = reference.GetType (subNamespaces, fullName, genericParameterCount, caseSensitive, false);
					if (result != null)
						return result;
				}
			}
			
			return result;
		}

		public override bool NeedCompilation (string fileName)
		{
			DateTime parseTime = codeCompletionDatabase.GetCompilationUnitParseTime (fileName);
			if (parseTime.Ticks == 0)
				return true;
			DateTime writeTime = System.IO.File.GetLastWriteTime (fileName);
			return parseTime < writeTime;
		}
		
		public override void UpdateFromParseInfo (ICompilationUnit unit, string fileName)
		{
			codeCompletionDatabase.UpdateCompilationUnit (unit, projectId, fileName);
		}
	}
}
