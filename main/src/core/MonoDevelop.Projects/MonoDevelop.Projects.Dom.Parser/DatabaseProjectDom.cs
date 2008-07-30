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
using MonoDevelop.Projects.Dom.Database;

namespace MonoDevelop.Projects.Dom.Parser
{
	public class DatabaseProjectDom : ProjectDom
	{
		MonoDevelop.Projects.Dom.Database.CodeCompletionDatabase codeCompletionDatabase;
		long compilationUnitId;
		List<long> referencedAssemblyIds = new List<long> ();
			
		public override IEnumerable<IType> Types {
			get {
				return codeCompletionDatabase.GetTypeList (new long[] { compilationUnitId });
			}
		}

		public long CompilationUnitId {
			get {
				return compilationUnitId;
			}
			set {
				compilationUnitId = value;
			}
		}
		
		public override void AddReference (ProjectDom dom)
		{
			DatabaseProjectDom dpd = dom as DatabaseProjectDom;
			if (dpd != null) {
				if (dpd.codeCompletionDatabase == ProjectDomService.AssemblyDatabase) {
					referencedAssemblyIds.Add (dpd.CompilationUnitId);
					return;
				}
			}
			base.AddReference (dom);
		}
		
		public DatabaseProjectDom (MonoDevelop.Projects.Dom.Database.CodeCompletionDatabase codeCompletionDatabase)
		{
			this.codeCompletionDatabase = codeCompletionDatabase;
			this.compilationUnitId      = -1;
		}
		
		internal override void GetNamespaceContentsInternal (List<IMember> result, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			codeCompletionDatabase.GetNamespaceContents (result, compilationUnitId, subNamespaces, caseSensitive);
		}
		
		public override List<IMember> GetNamespaceContents (IEnumerable<string> subNamespaces, bool includeReferences, bool caseSensitive)
		{
			List<IMember> result = new List<IMember> ();
			GetNamespaceContentsInternal (result, subNamespaces, caseSensitive);
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
			foreach (IType type in codeCompletionDatabase.GetTypes (subNamespaces, new long[] {compilationUnitId}, fullName, caseSensitive)) {
				if (genericParameterCount < 0 || 
				    (genericParameterCount == 0 && type.TypeParameters == null) || 
				    (type.TypeParameters != null && type.TypeParameters.Count == genericParameterCount)) {
					return type;
				}
			}
			return null;
		}
		
		public override IType GetType (IEnumerable<string> subNamespaces, string fullName, int genericParameterCount, bool caseSensitive, bool searchDeep)
		{
			if (String.IsNullOrEmpty (fullName))
				return null;
				
			IType result = GetType (subNamespaces, fullName, genericParameterCount, caseSensitive);
			if (result == null && searchDeep) {
				foreach (ProjectDom reference in references) {
					result = reference.GetType (subNamespaces, fullName, genericParameterCount, caseSensitive, false);
					if (result != null)
						return result;
				}
				
				foreach (IType type in ProjectDomService.AssemblyDatabase.GetTypes (subNamespaces, referencedAssemblyIds, fullName, caseSensitive)) {
					if (genericParameterCount < 0 || (genericParameterCount == 0 && type.TypeParameters == null) ||  (type.TypeParameters != null && type.TypeParameters.Count == genericParameterCount)) {
						return type;
					}
				}
			}
			
			return result;
		}

		public override bool NeedCompilation (string fileName)
		{
			DateTime parseTime = codeCompletionDatabase.GetCompilationUnitParseTime (fileName);
			DateTime writeTime = System.IO.File.GetLastWriteTime (fileName);
			return parseTime >= writeTime;
		}
		
		public override void UpdateFromParseInfo (ICompilationUnit unit, string fileName)
		{
			codeCompletionDatabase.UpdateCompilationUnit (unit, fileName);
		}
	}
}
