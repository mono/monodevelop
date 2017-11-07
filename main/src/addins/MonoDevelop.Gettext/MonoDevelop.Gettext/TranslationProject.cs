//
// TranslationProject.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Deployment;
using MonoDevelop.Ide;
using MonoDevelop.Core.Execution;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Gettext
{	
	class TranslationProject : Project, IDeployable
	{
		[ItemProperty("packageName")]
		string packageName = null;
		
		[ItemProperty("outputType")]
		TranslationOutputType outputType;
			
		[ItemProperty(Name = "relPath", DefaultValue = "")]
		string relPath = String.Empty;
		
		bool isDirty;
		
		public string PackageName {
			get { return packageName; }
			set { packageName = value; }
		}
		
		public string RelPath {
			get { return relPath; }
			set { relPath = value; }
		}
		
		public TranslationOutputType OutputType {
			get { return outputType; }
			set { outputType = value; }
		}
		
		TranslationCollection translations;
		
		[ItemProperty]
		List<TranslationProjectInformation> projectInformations = new List<TranslationProjectInformation> ();
		
		[ItemProperty ("translations")]
		public TranslationCollection Translations {
			get { return translations; }
		}
		
		public ReadOnlyCollection<TranslationProjectInformation> TranslationProjectInformations {
			get { return projectInformations.AsReadOnly (); }
		}
		
		public TranslationProject ()
		{
			Initialize (this);
			translations = new TranslationCollection (this);
		}

		protected override IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			List<FilePath> col = base.OnGetItemFiles (includeReferencedFiles).ToList();
			if (includeReferencedFiles) {
				foreach (Translation tr in translations)
					col.Add (tr.PoFile);
			}
			return col;
		}
		
		public TranslationProjectInformation GetProjectInformation (SolutionFolderItem entry, bool force)
		{
			foreach (TranslationProjectInformation info in this.projectInformations) {
				if (info.ProjectName == entry.Name)
					return info;
			}
			if (force) {
				TranslationProjectInformation newInfo = new TranslationProjectInformation (entry.Name);
				this.projectInformations.Add (newInfo);
				return newInfo;
			}
			return null;
		}
		
		public bool IsIncluded (SolutionFolderItem entry)
		{
			TranslationProjectInformation info = GetProjectInformation (entry, false);
			if (info != null)
				return info.IsIncluded;
			return true;
		}
		
		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			//NOTE: we don't really need multiple configurations for this project type, since nothing actually uses them
			//but it makes the solution configuration mapping look more consistent
			//Perhaps in future there will be some per-config settings
			foreach (string config in new [] { "Debug", "Release"})
				Configurations.Add (new TranslationProjectConfiguration (config));

			OutputType  = (TranslationOutputType)Enum.Parse (typeof(TranslationOutputType), template.GetAttribute ("outputType"));
			PackageName = template.GetAttribute ("packageName");
			RelPath     = template.GetAttribute ("relPath");
		}
		
		string GetFileName (string isoCode)
		{
			return Path.Combine (base.BaseDirectory, isoCode + ".po");
		}
		
		public class MatchLocation
		{
			string originalString;
			string originalPluralString;
			int    line;
			
			public string OriginalString {
				get { return originalString; }
			}
			
			public string OriginalPluralString {
				get { return originalPluralString; }
			}
			
			public int Line {
				get { return line; }
			}
			
			public MatchLocation (string originalString, string originalPluralString, int line)
			{
				this.originalString = originalString;
				this.originalPluralString = originalPluralString;
				this.line = line;
			}
			
			public MatchLocation (string originalString, int line) : this (originalString, null, line)
			{
			}
		}
		

		public Translation AddNewTranslation (string isoCode, ProgressMonitor monitor)
		{
			try {
				Translation tr = new Translation (this, isoCode);
				translations.Add (tr);
				string templateFile    = Path.Combine (this.BaseDirectory, "messages.po");
				string translationFile = GetFileName (isoCode);
				if (!File.Exists (templateFile)) 
					CreateDefaultCatalog (monitor);
				File.Copy (templateFile, translationFile);
				
				monitor.ReportSuccess (String.Format (GettextCatalog.GetString ("Language '{0}' successfully added."), isoCode));
				monitor.Step (1);
				SaveAsync (monitor);
				return tr;
			} catch (Exception e) {
				monitor.ReportError (String.Format ( GettextCatalog.GetString ("Language '{0}' could not be added: "), isoCode), e);
				return null;
			} finally {
				monitor.EndTask ();
			}
		}
		
		public Translation GetTranslation (string isoCode)
		{
			foreach (Translation translation in this.translations) {
				if (translation.IsoCode == isoCode) 
					return translation;
			}
			return null;
		}
		
		public void RemoveTranslation (string isoCode)
		{
			Translation translation = GetTranslation (isoCode);
			if (translation != null)
				this.translations.Remove (translation);
		}
		
		internal void NotifyTranslationAdded (Translation tr)
		{
			if (!Loading)
				isDirty = true; 
			OnTranslationAdded (EventArgs.Empty);
		}
		
		internal void NotifyTranslationRemoved (Translation tr)
		{
			isDirty = true;
			OnTranslationRemoved (EventArgs.Empty);
		}
		
		protected override SolutionItemConfiguration OnCreateConfiguration (string name, ConfigurationKind kind)
		{
			return new TranslationProjectConfiguration (name);
		}
		
		internal string GetOutputDirectory (ConfigurationSelector configuration)
		{
			if (this.ParentSolution.StartupItem == null) 
				return BaseDirectory;
			if (this.ParentSolution.StartupItem is DotNetProject) {
				return Path.Combine (Path.GetDirectoryName (((DotNetProject)ParentSolution.StartupItem).GetOutputFileName (configuration)), RelPath);
			}
			return Path.Combine (this.ParentSolution.StartupItem.BaseDirectory, RelPath);
		}
		
		void CreateDefaultCatalog (ProgressMonitor monitor)
		{
			IFileScanner[] scanners = TranslationService.GetFileScanners ();
			
			Catalog catalog = new Catalog (this);
			List<Project> projects = new List<Project> ();
			foreach (Project p in ParentSolution.GetAllProjects ()) {
				if (IsIncluded (p))
					projects.Add (p);
			}
			foreach (Project p in projects) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Scanning project {0}...", p.Name));
				foreach (ProjectFile file in p.Files) {
					if (!File.Exists (file.FilePath))
						continue;
					if (file.Subtype == Subtype.Code) {
						string mimeType = DesktopService.GetMimeTypeForUri (file.FilePath);
						foreach (IFileScanner fs in scanners) {
							if (fs.CanScan (this, catalog, file.FilePath, mimeType))
								fs.UpdateCatalog (this, catalog, monitor, file.FilePath);
						}
					}
				}
				if (monitor.CancellationToken.IsCancellationRequested)
					return;
				monitor.Step (1);
			}
			catalog.Save (Path.Combine (this.BaseDirectory, "messages.po"));
		}
		
		public void UpdateTranslations (ProgressMonitor monitor, bool sort)
		{
			UpdateTranslations (monitor, sort, translations.ToArray ());
		}
		
		public void UpdateTranslations (ProgressMonitor monitor, bool sort, params Translation[] translations)
		{
			monitor.BeginTask (null, Translations.Count + 1);
			
			try {
				List<Project> projects = new List<Project> ();
				foreach (Project p in ParentSolution.GetAllProjects ()) {
					if (IsIncluded (p))
						projects.Add (p);
				}
				monitor.BeginTask (GettextCatalog.GetString ("Updating message catalog"), projects.Count);
				CreateDefaultCatalog (monitor);
				monitor.Log.WriteLine (GettextCatalog.GetString ("Done"));
			} finally { 
				monitor.EndTask ();
				monitor.Step (1);
			}
			if (monitor.CancellationToken.IsCancellationRequested) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Operation cancelled."));
				return;
			}
			
			Dictionary<string, bool> isIncluded = new Dictionary<string, bool> ();
			foreach (Translation translation in translations) {
				isIncluded[translation.IsoCode] = true;
			}
			foreach (Translation translation in this.Translations) {
				if (!isIncluded.ContainsKey (translation.IsoCode))
					continue;
				string poFileName  = translation.PoFile;
				monitor.BeginTask (GettextCatalog.GetString ("Updating {0}", translation.PoFile), 1);
				try {
					var pb = new ProcessArgumentBuilder ();
					pb.Add ("--update");
					pb.AddQuoted (poFileName);
					pb.Add ("--verbose");
					if (sort)
						pb.Add ("--sort-output");
					pb.AddQuoted (this.BaseDirectory.Combine ("messages.po"));
					
					var process = Runtime.ProcessService.StartProcess (Translation.GetTool ("msgmerge"),
						pb.ToString (), this.BaseDirectory, monitor.Log, monitor.Log, null);
					process.WaitForOutput ();
				}
				catch (System.ComponentModel.Win32Exception) {
					var msg = GettextCatalog.GetString ("Did not find msgmerge. Please ensure that gettext tools are installed.");
					monitor.ReportError (msg, null);
				}
				catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Could not update file {0}", translation.PoFile), ex);
				}
				finally {
					monitor.EndTask ();
					monitor.Step (1);
				}
				if (monitor.CancellationToken.IsCancellationRequested) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Operation cancelled."));
					return;
				}
			}
		}
		public void RemoveEntry (string msgstr)
		{
			foreach (Translation translation in this.Translations) {
				string poFileName  = translation.PoFile;
				Catalog catalog = new Catalog (this);
				catalog.Load (new MonoDevelop.Core.ProgressMonitor (), poFileName);
				CatalogEntry entry = catalog.FindItem (msgstr);
				if (entry != null) {
					catalog.RemoveItem (entry);
					catalog.Save (poFileName);
				}
			}
		}
		
		protected async override Task<BuildResult> OnBuild (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			var toBuild = Translations.Where (t => t.NeedsBuilding(configuration)).ToArray ();
			BuildResult results = new BuildResult ("", 1, 0);
			string outputDirectory = GetOutputDirectory (configuration);
			if (!string.IsNullOrEmpty (outputDirectory)) {
				await Task.Run (delegate {
					foreach (Translation translation in toBuild) {
						if (translation.NeedsBuilding (configuration)) {
							BuildResult res = translation.Build (monitor, configuration);
							results.Append (res);
						}
					}
					isDirty = false;
				});
			}
			return results;
		}
		
		protected async override Task<BuildResult> OnClean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			isDirty = true;
			monitor.Log.WriteLine (GettextCatalog.GetString ("Removing all .mo files."));
			string outputDirectory = GetOutputDirectory (configuration);
			if (string.IsNullOrEmpty (outputDirectory))
				return BuildResult.CreateSuccess ();

			var toClean = Translations.Select (t => t.GetOutFile (configuration)).ToArray ();
			await Task.Run (delegate {
				foreach (string moFileName in toClean) {
					if (File.Exists (moFileName))
						File.Delete (moFileName);
				}
			});
			return BuildResult.CreateSuccess ();
		}

#region Deployment
		public DeployFileCollection GetDeployFiles (ConfigurationSelector configuration)
		{
			DeployFileCollection result = new DeployFileCollection ();
			foreach (Translation translation in this.Translations) {
				if (OutputType == TranslationOutputType.SystemPath) {
					string moDirectory = Path.Combine ("locale", translation.IsoCode);
					moDirectory = Path.Combine (moDirectory, "LC_MESSAGES");
					string moFileName  = Path.Combine (moDirectory, PackageName + ".mo");
					result.Add (new DeployFile (this, translation.GetOutFile (configuration), moFileName, TargetDirectory.CommonApplicationDataRoot));
				} else {
					string moDirectory = Path.Combine (RelPath, translation.IsoCode);
					moDirectory = Path.Combine (moDirectory, "LC_MESSAGES");
					string moFileName  = Path.Combine (moDirectory, PackageName + ".mo");
					result.Add (new DeployFile (this, translation.GetOutFile (configuration), moFileName, TargetDirectory.ProgramFiles));
				}
			}
			return result;
		}
#endregion
		
		protected override bool OnGetNeedsBuilding (ConfigurationSelector configuration)
		{
			if (isDirty)
				return true;
			foreach (Translation translation in this.Translations) {
				if (translation.NeedsBuilding (configuration))
					return true;
			}
			return false;
		}
		
		protected virtual void OnTranslationAdded (EventArgs e)
		{
			if (TranslationAdded != null)
				TranslationAdded (this, e);
		}
		
		public event EventHandler TranslationAdded;
		
		protected virtual void OnTranslationRemoved (EventArgs e)
		{
			if (TranslationRemoved != null)
				TranslationRemoved (this, e);
		}
		
		public event EventHandler TranslationRemoved;
	}
	
	enum TranslationOutputType {
		RelativeToOutput,
		SystemPath
	}
	
	class TranslationProjectConfiguration : ProjectConfiguration
	{
		public TranslationProjectConfiguration (string id): base (id)
		{
		}
	}
	
	class TranslationProjectInformation
	{
		[ItemProperty]
		string projectName;
		
		[ItemProperty]
		bool isIncluded;
		
		public string ProjectName {
			get { return projectName; }
			set { projectName = value; }
		}
		
		public bool IsIncluded {
			get { return isIncluded; }
			set { isIncluded = value; }
		}
		
		public TranslationProjectInformation ()
		{
		}
		
		public TranslationProjectInformation (string projectName)
		{
			this.projectName = projectName;
		}
	}
}
