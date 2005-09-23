//
// NUnitAssemblyGroupProject.cs
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
using System.IO;
using System.Xml;
using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Internal.Serialization;

namespace MonoDevelop.NUnit
{
	[DataInclude (typeof(NUnitAssemblyGroupProjectConfiguration))]
	public class NUnitAssemblyGroupProject: CombineEntry
	{
		RootTest rootTest;
		
		public NUnitAssemblyGroupProject ()
		{
		}
		
		public override void InitializeFromTemplate (XmlElement element)
		{
			Configurations.Add (CreateConfiguration ("Default"));
		}

		public UnitTest RootTest {
			get {
				if (rootTest == null) {
					rootTest = new RootTest (this);
				}
				return rootTest;
			}
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			NUnitAssemblyGroupProjectConfiguration conf = new NUnitAssemblyGroupProjectConfiguration ();
			conf.Name = name;
			return conf;
		}
		
		public override void Clean ()
		{
		}
		
		public override ICompilerResult Build (IProgressMonitor monitor)
		{
			return null;
		}
		
		public override void Execute (IProgressMonitor monitor, ExecutionContext context)
		{
		}
		
		public override bool NeedsBuilding {
			get { return false; }
			set {}
		}
	}
	
	public class NUnitAssemblyGroupProjectConfiguration: AbstractConfiguration
	{
		TestAssemblyCollection assemblies;
		
		public NUnitAssemblyGroupProjectConfiguration ()
		{
			assemblies = new TestAssemblyCollection (this);
		}
		
		public override void CopyFrom (IConfiguration other)
		{
			base.CopyFrom (other);
			
			NUnitAssemblyGroupProjectConfiguration conf = other as NUnitAssemblyGroupProjectConfiguration;
			if (conf != null) {
				assemblies.Clear ();
				foreach (TestAssembly ta in conf.Assemblies) {
					TestAssembly copy = new TestAssembly (ta.Path);
					assemblies.Add (copy);
				}
			}
		}
		
		[ItemProperty ("Assemblies")]
		[ItemProperty ("Assembly", ValueType=typeof(TestAssembly), Scope=1)]
		public TestAssemblyCollection Assemblies {
			get { return assemblies; }
		}
		
		internal void OnAssembliesChanged ()
		{
			if (AssembliesChanged != null)
				AssembliesChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler AssembliesChanged;
	}
	
	class RootTest: UnitTestGroup
	{
		NUnitAssemblyGroupProject project;
		string resultsPath;
		NUnitAssemblyGroupProjectConfiguration lastConfig;
		
		public RootTest (NUnitAssemblyGroupProject project): base (project.Name, project)
		{
			this.project = project;
			resultsPath = Path.Combine (project.BaseDirectory, "test-results");
			ResultsStore = new XmlResultsStore (resultsPath, Path.GetFileName (project.FileName));
			
			lastConfig = (NUnitAssemblyGroupProjectConfiguration) project.ActiveConfiguration;
			if (lastConfig != null)
				lastConfig.AssembliesChanged += new EventHandler (OnAssembliesChanged);
		}

		public override void Dispose ()
		{
			if (lastConfig != null)
				lastConfig.AssembliesChanged -= new EventHandler (OnAssembliesChanged);
		}
		
		internal string ResultsPath {
			get { return resultsPath; }
		}
		
		void OnAssembliesChanged (object sender, EventArgs args)
		{
			UpdateTests ();
		}
		
		protected override void OnActiveConfigurationChanged ()
		{
			if (lastConfig != null)
				lastConfig.AssembliesChanged -= new EventHandler (OnAssembliesChanged);

			lastConfig = (NUnitAssemblyGroupProjectConfiguration) project.ActiveConfiguration;
			if (lastConfig != null)
				lastConfig.AssembliesChanged += new EventHandler (OnAssembliesChanged);

			UpdateTests ();
			base.OnActiveConfigurationChanged ();
		}
		
		protected override void OnCreateTests ()
		{
			NUnitAssemblyGroupProjectConfiguration conf = (NUnitAssemblyGroupProjectConfiguration) project.GetConfiguration (ActiveConfiguration);
			if (conf != null) {
				foreach (TestAssembly t in conf.Assemblies)
					Tests.Add (t);
			}
		}
	}
}

