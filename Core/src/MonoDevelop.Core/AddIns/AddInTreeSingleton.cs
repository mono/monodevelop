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

using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.AddIns.Codons;

namespace MonoDevelop.Core.AddIns
{
	/// <summary>
	/// Here is the ONLY point to get an <see cref="IAddInTree"/> object.
	/// </summary>
	public class AddInTreeSingleton
	{
		static IAddInTree addInTree = null;
		readonly static string defaultCoreDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + // DON'T REPLACE
		Path.DirectorySeparatorChar + ".." +
		Path.DirectorySeparatorChar + "AddIns";
		
		static bool ignoreDefaultCoreDirectory = false;
		static string[] addInDirectories       = null;
		
		/// <summary>
		/// Returns an <see cref="IAddInTree"/> object.
		/// </summary>
		
		public static IAddInTree AddInTree {
			get {
				return addInTree;
			}
		}
		
		public static bool SetAddInDirectories(string[] addInDirectories, bool ignoreDefaultCoreDirectory)
		{
			if (addInDirectories == null || addInDirectories.Length < 1) {
				// something went wrong
				return false;
			}
			AddInTreeSingleton.addInDirectories = addInDirectories;
			AddInTreeSingleton.ignoreDefaultCoreDirectory = ignoreDefaultCoreDirectory;
			return true;
		}
		
		static StringCollection InsertAddIns (StringCollection addInFiles, out AddinError[] errors)
		{
			StringCollection retryList  = new StringCollection();
			ArrayList list = new ArrayList ();
			
			foreach (string addInFile in addInFiles) {
				
				AddIn addIn = new AddIn();
				try {
					addIn.Initialize (addInFile);
					addInTree.InsertAddIn (addIn);
				} catch (CodonNotFoundException ex) {
					retryList.Add (addInFile);
					list.Add (new AddinError (addInFile, ex, false));
				} catch (ConditionNotFoundException ex) {
					retryList.Add (addInFile);
					list.Add (new AddinError (addInFile, ex, false));
				} catch (MissingDependencyException ex) {
					// Try to load the addin later. Maybe it depends on an
					// addin that has not yet been loaded.
					retryList.Add(addInFile);
					list.Add (new AddinError (addInFile, ex, false));
				} catch (InvalidAssemblyVersionException ex) {
					retryList.Add (addInFile);
					list.Add (new AddinError (addInFile, ex, false));
				} catch (Exception ex) {
					retryList.Add (addInFile);
					list.Add (new AddinError (addInFile, ex, false));
				} 
			}
			
			errors = (AddinError[]) list.ToArray (typeof(AddinError));
			return retryList;
		}
		
		public static AddinError[] InitializeAddins ()
		{
			AssemblyLoader loader = new AssemblyLoader();
			
			try {
				loader.CheckAssembly (Assembly.GetEntryAssembly ());
			} catch (Exception ex) {
				AddinError err = new AddinError (Assembly.GetEntryAssembly ().Location, ex, true);
				return new AddinError[] { err };
			}
			
			AddinError[] errors = null;
			addInTree = new DefaultAddInTree (loader);
			
			FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
			
			StringCollection addInFiles = null;
			StringCollection retryList  = null;
			
			if (ignoreDefaultCoreDirectory == false) {
				addInFiles = fileUtilityService.SearchDirectory(defaultCoreDirectory, "*.addin.xml");
				retryList  = InsertAddIns (addInFiles, out errors);
			}
			else
				retryList = new StringCollection();
			
			if (addInDirectories != null) {
				foreach(string path in addInDirectories) {
					addInFiles = fileUtilityService.SearchDirectory(path, "*.addin.xml");
					StringCollection partialRetryList  = InsertAddIns (addInFiles, out errors);
					if (partialRetryList.Count != 0) {
						string [] retryListArray = new string[partialRetryList.Count];
						partialRetryList.CopyTo(retryListArray, 0);
						retryList.AddRange(retryListArray);
					}
				}
			}
			
			while (retryList.Count > 0) {
				StringCollection newRetryList = InsertAddIns (retryList, out errors);
				
				// break if no add-in could be inserted.
				if (newRetryList.Count == retryList.Count) {
					break;
				}
				
				retryList = newRetryList;
			}
			
			return errors;
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
