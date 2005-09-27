// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Projects.Parser
{
	/// <summary>
	/// 
	/// 
	/// </summary>
	public class ParseInformation : IParseInformation
	{
		ICompilationUnitBase validCompilationUnit;
		ICompilationUnitBase dirtyCompilationUnit;
		
		public ICompilationUnitBase ValidCompilationUnit {
			get {
				return validCompilationUnit;
			}
			set {
				validCompilationUnit = value;
			}
		}
		
		public ICompilationUnitBase DirtyCompilationUnit {
			get {
				return dirtyCompilationUnit;
			}
			set {
				dirtyCompilationUnit = value;
			}
		}
		
		public ICompilationUnitBase BestCompilationUnit {
			get {
				return validCompilationUnit == null ? dirtyCompilationUnit : validCompilationUnit;
			}
		}
		
		public ICompilationUnitBase MostRecentCompilationUnit {
			get {
				return dirtyCompilationUnit == null ? validCompilationUnit : dirtyCompilationUnit;
			}
		}
	}
}
