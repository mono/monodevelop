//
// DatabaseProjectDom.cs
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
using System.Collections.Generic;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom.Serialization
{
	internal class DatabaseProjectDom : ProjectDom
	{
		SerializationCodeCompletionDatabase database;

		public override IEnumerable<IType> Types {
			get {
				foreach (ClassEntry entry in database.GetAllClasses ()) {
					yield return new DomTypeProxy (database, entry);
				}
			}
		}

		public override IEnumerable<IType> GetTypes (string fileName)
		{
			return database.GetFileContents (fileName);
		}
		
		public override void UpdateFromParseInfo (ICompilationUnit unit)
		{
			database.UpdateTypeInformation (unit.Types, unit.FileName);
		}
		
		internal override void GetNamespaceContentsInternal (List<IMember> result, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			if (subNamespaces == null) {
				database.GetNamespaceContents (result, "", caseSensitive);
				return;
			}
			foreach (string subNamespace in subNamespaces) {
				database.GetNamespaceContents (result, subNamespace, caseSensitive);
			}
		}

		public override bool NeedCompilation (string fileName)
		{
			FileEntry entry = database.GetFile (fileName);
			return entry != null ? entry.IsModified : true;
		}
		
		public override IEnumerable<IReturnType> GetSubclasses (IType type)
		{
			foreach (IType subType in database.GetSubclasses (type.FullName, new string [] {})) {
				yield return new DomReturnType (subType);
			}
		}
		
		protected override IType GetType (IEnumerable<string> subNamespaces, string fullName, int genericParameterCount, bool caseSensitive)
		{
			List<string> namespaces = subNamespaces != null ? new List<string> (subNamespaces) : new List<string> ();
			int    idx = fullName.LastIndexOf ('.');
			string typeName;
			if (idx >= 0) {
				namespaces.Add (fullName.Substring (0, idx));
				typeName = fullName.Substring (idx + 1);
			} else {
				typeName = fullName;
			}
			List<IMember> members = new List<IMember> ();
			GetNamespaceContentsInternal (members, namespaces, caseSensitive);
			IType result = null;
			foreach (IMember member in members) {
				IType type = member as IType;
				if (type != null && type.Name == typeName && 
				    (genericParameterCount < 0 ||
				     (genericParameterCount == 0 && type.TypeParameters == null) ||
				     (type.TypeParameters != null && type.TypeParameters.Count == genericParameterCount))) {
					if (result == null) {
						result = type;
					} else {
						result = CompoundType.Merge (result, type);
					}
				}
			}
			return result;
		}
		
		public DatabaseProjectDom (SerializationCodeCompletionDatabase database)
		{
			this.database = database;
		}
		
		public override void Unload ()
		{
			database.Write ();
			database.Dispose ();
		}
	}
}
