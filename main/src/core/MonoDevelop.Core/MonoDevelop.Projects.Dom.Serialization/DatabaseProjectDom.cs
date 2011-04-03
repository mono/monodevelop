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
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom.Serialization
{
	internal class DatabaseProjectDom : ProjectDom
	{
		ParserDatabase dbProvider;
		SerializationCodeCompletionDatabase database;

		public DatabaseProjectDom (ParserDatabase dbProvider, SerializationCodeCompletionDatabase database)
		{
			this.dbProvider = dbProvider;
			this.database = database;
			database.SourceProjectDom = this;
		}

		public SerializationCodeCompletionDatabase Database {
			get { return database; }
		}

		public override IEnumerable<IType> Types {
			get {
				return database.GetClassList ();
			}
		}

		public override IEnumerable<IAttribute> Attributes {
			get {
				return database.GetGlobalAttributes ();
			}
		}

		public override IEnumerable<IType> GetTypes (FilePath fileName)
		{
			return database.GetFileContents (fileName);
		}

		public override IList<Tag> GetSpecialComments (FilePath fileName)
		{
			return database.GetSpecialComments (fileName);
		}

		public override void UpdateTagComments (FilePath fileName, IList<Tag> tags)
		{
			database.UpdateTagComments (tags, fileName);
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

		public override bool NamespaceExists (string name, bool searchDeep, bool caseSensitive)
		{
			if (database.NamespaceExists (name, caseSensitive)) return true;
			foreach (ReferenceEntry re in database.References) {
				SerializationCodeCompletionDatabase cdb = dbProvider.GetDatabase (re.Uri);
				if (cdb == null) continue;
				if (cdb.NamespaceExists (name, caseSensitive)) return true;
			}
			return false;
		}


		public override bool NeedCompilation (FilePath fileName)
		{
			FileEntry entry = database.GetFile (fileName);
			return entry != null ? entry.IsModified : true;
		}
		
		protected override IEnumerable<IType> InternalGetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
		{
			return dbProvider.GetSubclassesTree (database, type, searchDeep, namespaces);
		}
		
		internal override IEnumerable<string> OnGetReferences ()
		{
			foreach (ReferenceEntry re in database.References)
				yield return re.Uri;
		}
		
		internal override void Unload ()
		{
			database.Write ();
			if (!database.Disposed)
				database.Dispose ();
			base.Unload ();
			// database = null;
		}

		internal override void OnProjectReferenceAdded (ProjectReference pref)
		{
			ProjectCodeCompletionDatabase db = (ProjectCodeCompletionDatabase) database;
			db.UpdateFromProject ();
			this.UpdateReferences ();
		}

		internal override void OnProjectReferenceRemoved (ProjectReference pref)
		{
			ProjectCodeCompletionDatabase db = (ProjectCodeCompletionDatabase) database;
			db.UpdateFromProject ();
			this.UpdateReferences ();
		}

		internal override void CheckModifiedFiles ()
		{
			database.CheckModifiedFiles ();
		}

		internal override void Flush ()
		{
			if (database != null)
				database.Flush ();
		}
		
		public override string GetDocumentation (IMember member)
		{
			if (database == null)
				return "";
			return database.GetDocumentation (member);
		}
		
		public override TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit unit, bool isFromFile)
		{
			if (string.IsNullOrEmpty (unit.FileName))
				throw new ArgumentException ("Compilation unit has no file name set.", "unit");
			ProjectCodeCompletionDatabase db = database as ProjectCodeCompletionDatabase;
			if (db != null)
				return db.UpdateFromParseInfo (unit, unit.FileName, isFromFile);
			
			SimpleCodeCompletionDatabase sdb = database as SimpleCodeCompletionDatabase;
			if (sdb != null)
				return sdb.UpdateFromParseInfo (unit);
			
			return null;
		}
		
		public override IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
		{
			var result = dbProvider.GetClass (database, typeName, genericArguments, deepSearchReferences, caseSensitive);
			if (result == null)
				result = GetTemporaryType (typeName, genericArguments, deepSearchReferences, caseSensitive);
			return result;
		}

		public override IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive)
		{
			if (genericArgumentsCount > 0)
				typeName += "`" + genericArgumentsCount;
			var result = dbProvider.GetClass (database, typeName, null, deepSearchReferences, caseSensitive);
			if (result == null)
				result = GetTemporaryType (typeName, genericArgumentsCount, deepSearchReferences, caseSensitive);
			return result;
		}

		public override string ToString ()
		{
			return string.Format("[DatabaseProjectDom: {0}]", System.IO.Path.GetFileName (Database.DataFile));
		}
		
		protected override void ForceUpdateBROKEN ()
		{
			database.ForceUpdateBROKEN ();
		}
		
		internal override ProjectDomStats GetStats ()
		{
			ProjectDomStats s = database.GetStats ();
			s.Add (base.GetStats ());
			return s;
		}
	}
}
