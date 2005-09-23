// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using Gtk;

using MonoDevelop.Gui;
using MonoDevelop.Core;
using MonoDevelop.Services;
using MonoDevelop.Gui.HtmlControl;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;

using AssemblyAnalyser = ICSharpCode.AssemblyAnalyser.AssemblyAnalyser;

namespace MonoDevelop.AssemblyAnalyser
{
	public class AssemblyAnalyserView : AbstractViewContent
	{
		public static AssemblyAnalyserView AssemblyAnalyserViewInstance = null;
		
		AssemblyAnalyserControl assemblyAnalyserControl;
		
		AppDomain        analyserDomain  = null;
		AssemblyAnalyser currentAnalyser = null;

		public override string TabPageLabel {
			get { return GettextCatalog.GetString ("Assembly Analyzer"); }
		}

		public override string ContentName {
			get { return GettextCatalog.GetString ("Assembly Analyzer"); }
		}

		public override Widget Control {
			get {
				return assemblyAnalyserControl;
			}
		}

		public override bool IsViewOnly {
			get {
				return true;
			}
		}
		
		public override bool IsReadOnly {
			get {
				return false;
			}
		}

		public AssemblyAnalyserView()
		{
			AssemblyAnalyserViewInstance = this;
			assemblyAnalyserControl = new AssemblyAnalyserControl ();
			IProjectService projectService = (IProjectService) ServiceManager.GetService (typeof (IProjectService));
			projectService.StartBuild += new EventHandler (ProjectServiceStartBuild);
			projectService.EndBuild += new EventHandler (ProjectServiceEndBuild);
			RefreshProjectAssemblies ();
		}
		
		public void RefreshProjectAssemblies()
		{
			if (currentAnalyser == null) {
				currentAnalyser = CreateRemoteAnalyser ();
			}

			IProjectService projectService = (IProjectService) ServiceManager.GetService (typeof (IProjectService));
			ArrayList projectCombineEntries = Combine.GetAllProjects (projectService.CurrentOpenCombine);
			assemblyAnalyserControl.ClearContents ();

			foreach (ProjectCombineEntry projectEntry in projectCombineEntries) {
				string outputAssembly = projectService.GetOutputAssemblyName (projectEntry.Project);
				assemblyAnalyserControl.AnalyzeAssembly (currentAnalyser, outputAssembly);
			}

			assemblyAnalyserControl.PrintAllResolutions ();
		}
		
		public override void Load (string file)
		{
		}
		
		public override void Dispose ()
		{
			DisposeAnalyser ();
			
			IProjectService projectService = (IProjectService) ServiceManager.GetService (typeof (IProjectService));
			projectService.StartBuild -= new EventHandler (ProjectServiceStartBuild);
			projectService.EndBuild   -= new EventHandler (ProjectServiceEndBuild);
			
			IStatusBarService statusBarService = (IStatusBarService) ServiceManager.GetService (typeof (IStatusBarService));
			
			statusBarService.SetMessage (GettextCatalog.GetString ("Ready"));
			AssemblyAnalyserViewInstance = null;
		}
		
		void DisposeAnalyser()
		{
			currentAnalyser = null;
			AppDomain.Unload (analyserDomain);
			analyserDomain = null;
		}
		
		void ProjectServiceStartBuild (object sender, EventArgs e)
		{
			assemblyAnalyserControl.ClearContents ();
			DisposeAnalyser ();
		}
		
		void ProjectServiceEndBuild (object sender, EventArgs e)
		{
			Console.WriteLine ("refresh assemblies");
			//this.RefreshProjectAssemblies ();
		}
		
		AssemblyAnalyser CreateRemoteAnalyser ()
		{
			AppDomainSetup setup = new AppDomainSetup ();
			Evidence evidence = new Evidence (AppDomain.CurrentDomain.Evidence);
			setup.ApplicationName = "Analyser";
			//setup.ApplicationBase = Application.StartupPath;

			analyserDomain = AppDomain.CreateDomain ("AnalyserDomain", evidence, setup);
			return (AssemblyAnalyser) analyserDomain.CreateInstanceAndUnwrap (
				typeof (AssemblyAnalyser).Assembly.FullName, 
				typeof (AssemblyAnalyser).FullName,
				false, BindingFlags.Default,null,null,null,null,null);
		}
	}
}
