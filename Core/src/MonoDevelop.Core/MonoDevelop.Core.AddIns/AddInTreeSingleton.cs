// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using System.Collections.Specialized;
using System.IO;

using MonoDevelop.Core;

namespace MonoDevelop.Core.AddIns
{
	/// <summary>
	/// Here is the ONLY point to get an <see cref="IAddInTree"/> object.
	/// </summary>
	public class AddInTreeSingleton
	{
		static DefaultAddInTree addInTree = null;		
		
		/// <summary>
		/// Returns an <see cref="IAddInTree"/> object.
		/// </summary>
		
		public static IAddInTree AddInTree {
			get {
				return addInTree;
			}
		}
		
		internal static bool CheckAssemblyLoadConflicts {
			get { return addInTree.Loader.CheckAssemblyConflicts; }
			set { addInTree.Loader.CheckAssemblyConflicts = value; }
		}
		
		public static bool SetAddInDirectories(string[] addInDirectories, bool ignoreDefaultCoreDirectory)
		{
			if (addInDirectories == null || addInDirectories.Length < 1) {
				// something went wrong
				return false;
			}
			return true;
		}

		internal static AddinError InsertAddIn (string addInFile)
		{
			AddIn addIn = new AddIn();
			try {
				addIn.Initialize (addInFile);
				addInTree.InsertAddIn (addIn);
			} catch (CodonNotFoundException ex) {
				return new AddinError (addInFile, ex, false);
			} catch (ConditionNotFoundException ex) {
				return new AddinError (addInFile, ex, false);
			} catch (MissingDependencyException ex) {
				// Try to load the addin later. Maybe it depends on an
				// addin that has not yet been loaded.
				return new AddinError (addInFile, ex, false);
			} catch (InvalidAssemblyVersionException ex) {
				return new AddinError (addInFile, ex, false);
			} catch (Exception ex) {
				return new AddinError (addInFile, ex, false);
			}
			return null;
		}
		
		public static void Initialize ()
		{
			AssemblyLoader loader = new AssemblyLoader();
			addInTree = new DefaultAddInTree (loader);
		}
		
	}
	
	public class AddinError
	{
		string addinFile;
		Exception exception;
		bool fatal;
		
		public AddinError (string addin, Exception exception, bool fatal)
		{
			this.addinFile = addin;
			this.exception = exception;
			this.fatal = fatal;
		}
		
		public string AddinFile {
			get { return addinFile; }
		}
		
		public Exception Exception {
			get { return exception; }
		}
		
		public bool Fatal {
			get { return fatal; }
		}
	}
}
