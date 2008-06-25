//
// ProjectDom.cs
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

namespace MonoDevelop.Projects.Dom.Parser
{
	public class ProjectDom
	{
		Dictionary<string, ICompilationUnit> compilationUnits = new Dictionary<string, ICompilationUnit> ();
		CodeCompletionDatabase database;
		
/*		public IEnumerable<ICompilationUnit> CompilationUnits {
			get {
				return compilationUnits.Values;
			}
		}*/
		
		IEnumerable<IType> CompilationUnitTypes {
			get {
				foreach (ICompilationUnit unit in compilationUnits.Values) {
					foreach (IType type in unit.Types) {
						yield return type;
					}
				}
			}
		}
		public IEnumerable<IType> Types {
			get {
				return database == null ? CompilationUnitTypes : database.GetClassList ();
			}
		}
	
		public IEnumerable<IType> GetTypesFrom (string fileName)
		{
			if (Types != null) {
				foreach (IType type in Types) {
					if (type.Parts != null) {
						foreach (IType part in type.Parts) {
							if (part.CompilationUnit != null && part.CompilationUnit.FileName == fileName)
								yield return part;
						}
					}
					if (type.CompilationUnit != null && type.CompilationUnit.FileName == fileName)
						yield return type;
				}
			}
		}
		
		internal MonoDevelop.Projects.Dom.CodeCompletionDatabase Database {
			get {
				return database;
			}
			set {
				database = value;
			}
		}
		
		public void UpdateFromParseInfo (ICompilationUnit unit, string fileName)
		{
			if (database != null) {
				((ProjectCodeCompletionDatabase)database).UpdateFromParseInfo (unit, fileName);
			} else {
				this.compilationUnits [fileName] = unit;
			}
		}
		
//		
//		public bool Contains (ICompilationUnit unit)
//		{
//			foreach (ICompilationUnit u in compilationUnits.Values) {
//				if (u == unit)
//					return true;
//			}
//			return false;
//		}
//		
//		public void RemoveCompilationUnit (string fileName)
//		{
//			if (compilationUnits.ContainsKey (fileName)) {
//				compilationUnits[fileName].Dispose ();
//				compilationUnits.Remove (fileName);
//			}
//		}
//		
//		public void UpdateCompilationUnit (ICompilationUnit compilationUnit)
//		{
//			if (compilationUnits.ContainsKey (compilationUnit.FileName)) {
//				compilationUnits[compilationUnit.FileName].Dispose ();
//			}
//			compilationUnits[compilationUnit.FileName] = compilationUnit;
//		}
//		
		public IType GetType (string fullName, int genericParameterCount, bool caseSensitive)
		{
			if (String.IsNullOrEmpty (fullName))
				return null;
			return database.GetClass (fullName, null, caseSensitive);
/*			foreach (ICompilationUnit unit in compilationUnits.Values) {
				IType type = unit.GetType (fullName, genericParameterCount);
				if (type != null)
					return type;
			}
			return null;
			*/
		}
		
		
		internal void FireLoaded ()
		{
			if (Loaded != null) {
				Loaded (this, EventArgs.Empty);
			}
		}
		
		public event EventHandler Loaded;
	}
}
