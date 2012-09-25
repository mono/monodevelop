//
// UnitTest.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.NUnit
{
	public abstract class UnitTest: IDisposable
	{
		string name;
		IResultsStore resultsStore;
		UnitTestResult lastResult;
		UnitTest parent;
		TestStatus status;
		Hashtable options;
		IWorkspaceObject ownerSolutionItem;
		SolutionEntityItem ownerSolutionEntityItem;
		UnitTestResultsStore results;
		
		public string FixtureTypeNamespace {
			get;
			set;
		}
		
		public string FixtureTypeName {
			get;
			set;
		}

		protected UnitTest (string name)
		{
			this.name = name;
		}
		
		protected UnitTest (string name, IWorkspaceObject ownerSolutionItem)
		{
			this.name = name;
			this.ownerSolutionItem = ownerSolutionItem;
			ownerSolutionEntityItem = ownerSolutionItem as SolutionEntityItem;
			if (ownerSolutionEntityItem != null)
				ownerSolutionEntityItem.DefaultConfigurationChanged += OnConfugurationChanged;
		}
		
		public virtual void Dispose ()
		{
			if (ownerSolutionEntityItem != null)
				ownerSolutionEntityItem.DefaultConfigurationChanged -= OnConfugurationChanged;
		}
		
		internal void SetParent (UnitTest t)
		{
			parent = t;
		}
		
		public virtual string ActiveConfiguration {
			get {
				if (ownerSolutionEntityItem != null) {
					if (ownerSolutionEntityItem.DefaultConfiguration == null)
						return "";
					return ownerSolutionEntityItem.DefaultConfiguration.Id;
				} else if (Parent != null) {
					return Parent.ActiveConfiguration;
				} else {
					return "default";
				}
			}
		}
		
		public virtual string[] GetConfigurations ()
		{
			if (ownerSolutionEntityItem != null) {
				string[] res = new string [ownerSolutionEntityItem.Configurations.Count];
				for (int n=0; n<ownerSolutionEntityItem.Configurations.Count; n++)
					res [n] = ownerSolutionEntityItem.Configurations [n].Id;
				return res;
			} else if (Parent != null) {
				return Parent.GetConfigurations ();
			} else {
				return new string [] { "default" };
			}
		}
		
		public ICloneable GetOptions (Type optionsType)
		{
			return GetOptions (optionsType, ActiveConfiguration);
		}
		
		public bool HasOptions (Type optionsType, string configuration)
		{
			return GetOptions (optionsType, configuration, false) != null;
		}
		
		public void ResetOptions (Type optionsType, string configuration)
		{
			if (GetOptions (optionsType, configuration, false) == null)
				return;
				
			if (options == null || !options.ContainsKey (configuration))
				return;

			Hashtable configOptions = (Hashtable) options [configuration];
			if (configOptions != null)
				configOptions.Remove (optionsType);
			SaveOptions ();
		}
		
		public ICloneable GetOptions (Type optionsType, string configuration)
		{
			return GetOptions (optionsType, configuration, true);
		}
		
		public ICollection GetAllOptions (string configuration)
		{
			Hashtable localOptions = GetOptionsTable (configuration);
			if (localOptions == null || localOptions.Count == 0) {
				if (Parent != null)
					return Parent.GetAllOptions (configuration);
				else
					return new object[0];
			}
			if (Parent == null)
				return localOptions.Values;

			ICollection parentOptions = Parent.GetAllOptions (configuration);
			if (parentOptions.Count == 0)
				return localOptions.Values;

			Hashtable t = new Hashtable ();
			foreach (object ob in parentOptions)
				t [ob.GetType()] = ob;

			foreach (ICloneable ob in localOptions.Values)
				t [ob.GetType()] = ob.Clone ();

			return t.Values;
		}

		ICloneable GetOptions (Type optionsType, string configuration, bool createDefault)
		{
			Hashtable configOptions = GetOptionsTable (configuration);
			
			if (configOptions != null) {
				ICloneable ob = (ICloneable) configOptions [optionsType];
				if (ob != null)
					return (ICloneable) ob.Clone ();
			}
			if (!createDefault)
				return null;
			if (parent != null)
				return parent.GetOptions (optionsType, configuration);
			else
				return (ICloneable) Activator.CreateInstance (optionsType);
		}
		
		Hashtable GetOptionsTable (string configuration)
		{
			Hashtable configOptions = null;
			
			if (options == null || !options.ContainsKey (configuration)) {
				ICollection col = OnLoadOptions (configuration);
				if (col != null && col.Count > 0) {
					if (options == null)
						options = new Hashtable ();
					configOptions = (Hashtable) options [configuration];
					if (configOptions == null) {
						configOptions = new Hashtable ();
						options [configuration] = configOptions;
					}
					foreach (object op in col)
						configOptions [op.GetType ()] = op;
				}
			} else
				configOptions = (Hashtable) options [configuration];
			return configOptions;
		}
		
		public virtual void SetOptions (ICloneable ops, string configuration)
		{
			if (options == null)
				options = new Hashtable ();
				
			Hashtable configOptions = (Hashtable) options [configuration];
			if (configOptions == null) {
				configOptions = new Hashtable ();
				options [configuration] = configOptions;
			}
			
			configOptions [ops.GetType ()] = ops.Clone ();
			SaveOptions ();
		}
		
		void SaveOptions ()
		{
			if (options == null) {
				OnSaveOptions (null);
				return;
			}
			
			ArrayList list = new ArrayList ();
			foreach (DictionaryEntry e in options) {
				OptionsData d = new OptionsData ((string) e.Key, ((Hashtable) e.Value).Values);
				list.Add (d);
			}
			
			OnSaveOptions ((OptionsData[]) list.ToArray (typeof(OptionsData)));
		}
		
		public UnitTestResultsStore Results {
			get {
				if (results == null) {
					results = new UnitTestResultsStore (this, GetResultsStore ());
				}
				return results;
			}
		}
		
		public UnitTestResult GetLastResult ()
		{
			return lastResult;
		}
		
		public void ResetLastResult ()
		{
			lastResult = null;
			OnTestStatusChanged ();
		}
		
		public UnitTestCollection GetRegressions (DateTime fromDate, DateTime toDate)
		{
			UnitTestCollection list = new UnitTestCollection ();
			FindRegressions (list, fromDate, toDate);
			return list;
		}
		
		public virtual int CountTestCases ()
		{
			return 1;
		}
		
		public virtual SourceCodeLocation SourceCodeLocation {
			get { return null; }
		}
		
		public UnitTest Parent {
			get { return parent; }
		}
		
		public UnitTest RootTest {
			get {
				if (parent != null)
					return parent.RootTest;
				else
					return this;
			}
		}
		
		public virtual string Name {
			get { return name; }
		}
		
		public virtual string Title {
			get { return Name; }
		}
		
		public TestStatus Status {
			get { return status; }
			set {
				status = value;
				OnTestStatusChanged ();
			}
		}

		public string TestId {
			get;
			protected set;
		}
		
		public string FullName {
			get {
				if (parent != null)
					return parent.FullName + "." + Name;
				else
					return Name;
			}
		}
		
		protected IWorkspaceObject OwnerSolutionItem {
			get { return ownerSolutionItem; }
		}
		
		public IWorkspaceObject OwnerObject {
			get {
				if (ownerSolutionItem != null)
					return ownerSolutionItem;
				else if (parent != null)
					return parent.OwnerObject;
				else
					return null;
			}
		}
		
		internal string StoreRelativeName {
			get {
				if (resultsStore != null || Parent == null)
					return "";
				else if (Parent.resultsStore != null)
					return Name;
				else
					return Parent.StoreRelativeName + "." + Name;
			}
		}
		
		// Forces the reloading of tests, if they have changed
		public virtual IAsyncOperation Refresh ()
		{
			AsyncOperation op = new AsyncOperation ();
			op.SetCompleted (true);
			return op;
		}
		
		public UnitTestResult Run (TestContext testContext)
		{
			testContext.Monitor.BeginTest (this);
			UnitTestResult res = null;
			object ctx = testContext.ContextData;
			
			try {
				Status = TestStatus.Running;
				res = OnRun (testContext);
			} catch (global::NUnit.Framework.SuccessException) {
				res = UnitTestResult.CreateSuccess();
			} catch (global::NUnit.Framework.IgnoreException ex) {
				res = UnitTestResult.CreateIgnored(ex.Message);
			} catch (global::NUnit.Framework.InconclusiveException ex) {
				res = UnitTestResult.CreateInconclusive(ex.Message);
			} catch (Exception ex) {
				res = UnitTestResult.CreateFailure (ex);
			} finally {
				Status = TestStatus.Ready;
				testContext.Monitor.EndTest (this, res);
			}
			RegisterResult (testContext, res);
			testContext.ContextData = ctx;
			return res;
		}
		
		public bool CanRun (IExecutionHandler executionContext)
		{
			if (executionContext == null)
				executionContext = Runtime.ProcessService.DefaultExecutionHandler;
			return OnCanRun (executionContext);
		}
		
		protected abstract UnitTestResult OnRun (TestContext testContext);
		
		protected virtual bool OnCanRun (IExecutionHandler executionContext)
		{
			return true;
		}
		
		public void RegisterResult (TestContext context, UnitTestResult result)
		{
			// Avoid registering results twice
			if (lastResult != null && lastResult.TestDate == context.TestDate)
				return;

			result.TestDate = context.TestDate;
//			if ((int)result.Status == 0)
//				result.Status = ResultStatus.Ignored;
			lastResult = result;
			IResultsStore store = GetResultsStore ();
			if (store != null)
				store.RegisterResult (ActiveConfiguration, this, result);
			OnTestStatusChanged ();
		}
		
		IResultsStore GetResultsStore ()
		{
			if (resultsStore != null)
				return resultsStore;
			if (Parent != null)
				return Parent.GetResultsStore ();
			else
				return null;
		}
		
		protected IResultsStore ResultsStore {
			get { return resultsStore; }
			set { resultsStore = value; }
		}
		
		public virtual void SaveResults ()
		{
			IResultsStore store = GetResultsStore ();
			if (store != null)
				store.Save ();
		}
		
		internal virtual void FindRegressions (UnitTestCollection list, DateTime fromDate, DateTime toDate)
		{
			UnitTestResult res1 = Results.GetLastResult (fromDate);
			UnitTestResult res2 = Results.GetLastResult (toDate);
			if ((res1 == null || res1.IsSuccess) && (res2 != null && !res2.IsSuccess))
				list.Add (this);
		}
		
		protected virtual void OnSaveOptions (OptionsData[] data)
		{
			IConfigurationTarget ce;
			string path;
			
			GetOwnerSolutionItem (this, out ce, out path);
			
			if (ce == null)
				throw new InvalidOperationException ("Options can't be saved.");
			
			foreach (OptionsData d in data) {
				IExtendedDataItem edi = (IExtendedDataItem) ce.Configurations [d.Configuration];
				if (edi == null)
					continue;
				UnitTestOptionsSet oset = (UnitTestOptionsSet) edi.ExtendedProperties ["UnitTestInformation"];
				if (oset == null) {
					oset = new UnitTestOptionsSet ();
					edi.ExtendedProperties ["UnitTestInformation"] = oset;
				}
				
				UnitTestOptionsEntry te = oset.FindEntry (path);

				if (d.Options.Count > 0) {
					if (te == null) {
						te = new UnitTestOptionsEntry ();
						te.Path = path;
						oset.Tests.Add (te);
					}
					te.Options.Clear ();
					te.Options.AddRange (d.Options);
				} else if (te != null) {
					oset.Tests.Remove (te);
				}
			}
			
			ce.Save (new NullProgressMonitor ());
		}
		
		protected virtual ICollection OnLoadOptions (string configuration)
		{
			IConfigurationTarget ce;
			string path;
			
			GetOwnerSolutionItem (this, out ce, out path);
			
			if (ce == null)
				return null;
			
			IExtendedDataItem edi = (IExtendedDataItem) ce.Configurations [configuration];
			if (edi == null)
				return null;

			UnitTestOptionsSet oset = (UnitTestOptionsSet) edi.ExtendedProperties ["UnitTestInformation"];
			if (oset == null)
				return null;
			
			UnitTestOptionsEntry te = oset.FindEntry (path);
			if (te != null)
				return te.Options;
			else
				return null;
		}
		
		void GetOwnerSolutionItem (UnitTest t, out IConfigurationTarget c, out string path)
		{
			if (OwnerSolutionItem is SolutionEntityItem) {
				c = OwnerSolutionItem as SolutionEntityItem;
				path = "";
			} else if (parent != null) {
				parent.GetOwnerSolutionItem (t, out c, out path);
				if (c == null) return;
				if (path.Length > 0)
					path += "/" + t.Name;
				else
					path = t.Name;
			} else {
				c = null;
				path = null;
			}
		}
		
		void OnConfugurationChanged (object ob, ConfigurationEventArgs args)
		{
			OnActiveConfigurationChanged ();
		}
		
		protected virtual void OnActiveConfigurationChanged ()
		{
			OnTestChanged ();
		}
		
		protected virtual void OnTestChanged ()
		{
			Gtk.Application.Invoke (delegate {
				if (TestChanged != null)
					TestChanged (this, EventArgs.Empty);
			});
		}
		
		protected virtual void OnTestStatusChanged ()
		{
			Gtk.Application.Invoke (delegate {
				if (TestStatusChanged != null)
					TestStatusChanged (this, EventArgs.Empty);
			});
		}
		
		public event EventHandler TestChanged;
		public event EventHandler TestStatusChanged;
	}
	
	public class SourceCodeLocation
	{
		string fileName;
		int line;
		int column;
		
		public SourceCodeLocation (string fileName, int line, int column)
		{
			this.fileName = fileName;
			this.line = line;
			this.column = column;
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public int Line {
			get { return line; }
		}
		
		public int Column {
			get { return column; }
		}
	}

	public class OptionsData
	{
		string configuration;
		ICollection options;
		
		public OptionsData (string configuration, ICollection options)
		{
			this.configuration = configuration;
			this.options = options;
		}
		
		public string Configuration {
			get { return configuration; }
		}
		
		public ICollection Options {
			get { return options; }
		}
	}
	
	
	class UnitTestOptionsSet
	{
		[ExpandedCollection]
		[ItemProperty ("Test", ValueType = typeof(UnitTestOptionsEntry))]
		public ArrayList Tests = new ArrayList ();
		
		public UnitTestOptionsEntry FindEntry (string testPath)
		{
			foreach (UnitTestOptionsEntry t in Tests)
				if (t.Path == testPath) return t;
			return null;
		}
	}
	
	class UnitTestOptionsEntry
	{
		[ItemProperty ("Path")]
		public string Path;

		[ItemProperty ("Options")]
		[ExpandedCollection]
		public ArrayList Options = new ArrayList ();
	}
}

