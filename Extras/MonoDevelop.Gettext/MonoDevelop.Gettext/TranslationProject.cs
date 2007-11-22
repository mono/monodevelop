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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Gettext.Editor;
using MonoDevelop.Deployment;

namespace MonoDevelop.Gettext
{	
	public class TranslationProject : CombineEntry, IDeployable
	{
		[ItemProperty]
		List<Translation> translations = new List<Translation> ();
		
		[ItemProperty]
		List<TranslationProjectInformation> projectInformations = new List<TranslationProjectInformation> ();
		
		public ReadOnlyCollection<Translation> Translations {
			get { return translations.AsReadOnly (); }
		}
		
		public ReadOnlyCollection<TranslationProjectInformation> TranslationProjectInformations {
			get { return projectInformations.AsReadOnly (); }
		}
		
		public TranslationProject ()
		{
			this.NeedsBuilding = true;
			isDirty = true;
		}
		
		public TranslationProjectInformation GetProjectInformation (CombineEntry entry, bool force)
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
		
		public bool IsIncluded (CombineEntry entry)
		{
			TranslationProjectInformation info = GetProjectInformation (entry, false);
			if (info != null)
				return info.IsIncluded;
			return true;
		}
		
		public override void InitializeFromTemplate (XmlElement template)
		{
			foreach (XmlNode node in template.ChildNodes) {
				XmlElement el = node as XmlElement;
				if (el == null) 
					continue;
				if (node.Name == "Configuration") {
					TranslationProjectConfiguration config = (TranslationProjectConfiguration)this.CreateConfiguration (el.GetAttribute ("name"));
					config.OutputType  = (TranslationOutputType)Enum.Parse (typeof(TranslationOutputType), el.GetAttribute ("outputType"));
					config.PackageName = el.GetAttribute ("packageName");
					config.RelPath     = el.GetAttribute ("relPath");
					config.AbsPath     = el.GetAttribute ("absPath");
					this.Configurations.Add (config);
				}
			}
		}
		
		string GetFileName (Translation translation)
		{
			return GetFileName (translation.IsoCode);
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
		
		public void AddNewTranslation (string isoCode, IProgressMonitor monitor)
		{
			try {
				translations.Add (new Translation (isoCode));
				string templateFile    = Path.Combine (this.BaseDirectory, "messages.po");
				string translationFile = GetFileName (isoCode);
				if (!File.Exists (templateFile)) 
					CreateDefaultCatalog (monitor);
				File.Copy (templateFile, translationFile);
				
				monitor.ReportSuccess (String.Format (GettextCatalog.GetString ("Language '{0}' successfully added."), isoCode));
				monitor.Step (1);
				isDirty = true; 
				this.Save (monitor);
				OnTranslationAdded (EventArgs.Empty);
			} catch (Exception e) {
				monitor.ReportError (String.Format ( GettextCatalog.GetString ("Language '{0}' could not be added: "), isoCode), e);
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
			if (translation != null) {
				this.translations.Remove (translation);
				OnTranslationRemoved (EventArgs.Empty);
				isDirty = true;
			}
		}
		
		public override IConfiguration CreateConfiguration (string name)
		{
			return new TranslationProjectConfiguration (name);
		}
		
		string OutputDirectory {
			get {
				TranslationProjectConfiguration config = this.ActiveConfiguration as TranslationProjectConfiguration;
				if (config == null) 
					return null;
				if (config.OutputType == TranslationOutputType.SystemPath) {
					return config.AbsPath;
				} else {
					if (this.ParentCombine.StartupEntry == null) 
						return null;
					if (this.ParentCombine.StartupEntry is DotNetProject) {
						return Path.Combine (Path.GetDirectoryName (((DotNetProject)ParentCombine.StartupEntry).GetOutputFileName ()), config.RelPath);
					}
					return Path.Combine (this.ParentCombine.StartupEntry.BaseDirectory, config.RelPath);
				}
			}
		}
		static CatalogEntry GetItem (Catalog catalog, string original, string plural)
		{
			CatalogEntry result = catalog.FindItem (original);
			if (result == null) {
				result = new CatalogEntry (catalog, original, plural);
				if (!String.IsNullOrEmpty (plural))
					result.SetTranslations (new string[] {"", ""});
				catalog.AddItem (result);
			}
			return result;
		}
		
		int GetLineCount (string text, int startIndex, int endIndex)
		{
			int result = 0;
			for (int i = startIndex; i < endIndex; i++) {
				if (text[i] == '\n')
					result++;
			}
			return result;
		}
		
		static Regex xmlTranslationPattern = new Regex(@"_[^""]*=\s*""([^""]*)""", RegexOptions.Compiled);
		void UpdateXmlTranslations (Catalog catalog, IProgressMonitor monitor, string fileName)
		{
			string text = File.ReadAllText (fileName);
			string relativeFileName = MonoDevelop.Core.FileService.AbsoluteToRelativePath (this.BaseDirectory, fileName);
			string fileNamePrefix   = relativeFileName + ":";
			if (!String.IsNullOrEmpty (text)) {
				int lineNumber = 0;
				int oldIndex  = 0;
				foreach (Match match in xmlTranslationPattern.Matches (text)) {
					CatalogEntry entry = GetItem (catalog, match.Groups[1].Value, null);
					lineNumber += GetLineCount (text, oldIndex, match.Index);
					oldIndex = match.Index;
					entry.AddReference (fileNamePrefix + lineNumber);
				}
			}
		}

		static Regex translationPattern = new Regex(@"GetString\s*\(\s*""([^""]*)""\s*\)", RegexOptions.Compiled);
		static Regex pluralTranslationPattern = new Regex(@"GetPluralString\s*\(\s*""([^""]*)""\s*,\s*""([^""]*)""\s*,.*\)", RegexOptions.Compiled);
		void UpdateTranslations (Catalog catalog, IProgressMonitor monitor, string fileName)
		{
			string text = File.ReadAllText (fileName);
			string relativeFileName = MonoDevelop.Core.FileService.AbsoluteToRelativePath (this.BaseDirectory, fileName);
			string fileNamePrefix   = relativeFileName + ":";
			if (!String.IsNullOrEmpty (text)) {
				int lineNumber = 0;
				int oldIndex  = 0;
				foreach (Match match in translationPattern.Matches (text)) {
					CatalogEntry entry = GetItem (catalog, match.Groups[1].Value, null);
					lineNumber += GetLineCount (text, oldIndex, match.Index);
					oldIndex = match.Index;
					entry.AddReference (fileNamePrefix + lineNumber);
				}
				lineNumber = oldIndex  = 0;
				foreach (Match match in pluralTranslationPattern.Matches (text)) {
					CatalogEntry entry = GetItem (catalog, match.Groups[1].Value, match.Groups[2].Value);
					lineNumber += GetLineCount (text, oldIndex, match.Index);
					oldIndex = match.Index;
					entry.AddReference (fileNamePrefix + lineNumber);
				}
			}
		}

		void UpdateTranslation (Catalog catalog, string fileName, IProgressMonitor monitor)
		{
	       switch (Path.GetExtension (fileName)) {
	       case ".xml":
	               UpdateXmlTranslations (catalog, monitor, fileName);
	               break;
	       default:
	               UpdateTranslations (catalog, monitor, fileName);
	               break;
	       }
		}
		
		void CreateDefaultCatalog (IProgressMonitor monitor)
		{
			Catalog catalog = new Catalog ();
			List<Project> projects = new List<Project> ();
			foreach (Project p in RootCombine.GetAllProjects ()) {
				if (IsIncluded (p))
					projects.Add (p);
			}
			foreach (Project p in projects) {
				monitor.Log.WriteLine (String.Format (GettextCatalog.GetString ("Scanning project {0}..."),  p.Name));
				monitor.Step (1);
				foreach (ProjectFile file in p.ProjectFiles) {
					if (!File.Exists (file.FilePath))
						continue;
					string mimeType = Gnome.Vfs.MimeType.GetMimeTypeForUri (file.FilePath);
					if (file.Subtype == Subtype.Code  || file.BuildAction == BuildAction.Compile || mimeType == "application/xml" || mimeType.StartsWith ("text"))
						UpdateTranslation (catalog, file.FilePath, monitor);
				}
			}
			catalog.Save (Path.Combine (this.BaseDirectory, "messages.po"));
		}
		
		public void UpdateTranslations (IProgressMonitor monitor)
		{
			List<Project> projects = new List<Project> ();
			foreach (Project p in RootCombine.GetAllProjects ()) {
				if (IsIncluded (p))
					projects.Add (p);
			}
			monitor.BeginTask (GettextCatalog.GetString ("Updating Translations "), projects.Count);
			CreateDefaultCatalog (monitor);
			foreach (Translation translation in this.Translations) {
				string poFileName  = GetFileName (translation);
				Runtime.ProcessService.StartProcess ("msgmerge",
				                                     " -U " + poFileName + "  " + this.BaseDirectory + "/messages.po",
				                                     this.BaseDirectory,
				                                     monitor.Log,
				                                     monitor.Log,
				                                     null).WaitForExit ();
			}
			monitor.EndTask ();
		}
		public void RemoveEntry (string msgstr)
		{
			foreach (Translation translation in this.Translations) {
				string poFileName  = GetFileName (translation);
				Catalog catalog = new Catalog ();
				catalog.Load (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), poFileName);
				CatalogEntry entry = catalog.FindItem (msgstr);
				if (entry != null) {
					catalog.RemoveItem (entry);
					catalog.Save (poFileName);
				}
			}
		}
				
		protected override ICompilerResult OnBuild (IProgressMonitor monitor)
		{
			CompilerResults results = new CompilerResults (null);
			TranslationProjectConfiguration config = (TranslationProjectConfiguration)this.ActiveConfiguration;
			string outputDirectory = OutputDirectory;
			if (!string.IsNullOrEmpty (outputDirectory)) {
				foreach (Translation translation in this.Translations) {
					string poFileName  = GetFileName (translation);
					string moDirectory = Path.Combine (Path.Combine (outputDirectory, translation.IsoCode), "LC_MESSAGES");
					if (!Directory.Exists (moDirectory))
						Directory.CreateDirectory (moDirectory);
					string moFileName  = Path.Combine (moDirectory, config.PackageName + ".mo");
					
					ProcessWrapper process = Runtime.ProcessService.StartProcess ("msgfmt",
				                                     poFileName + " -o " + moFileName,
				                                     this.BaseDirectory,
				                                     monitor.Log,
				                                     monitor.Log,
				                                     null);
					process.WaitForExit ();

					if (process.ExitCode == 0) {
						monitor.Log.WriteLine (GettextCatalog.GetString ("Translation {0}: Compilation succeeded.", translation.IsoCode));
					} else {
						string error   = process.StandardError.ReadToEnd ();
						string message = String.Format (GettextCatalog.GetString ("Translation {0}: Compilation failed. Reason: {1}"), translation.IsoCode, error);
						monitor.Log.WriteLine (message);
						results.Errors.Add (new CompilerError (this.Name, 0, 0, null, message));
					}
				}
				isDirty = false;
				this.NeedsBuilding = false;
			}
			return new DefaultCompilerResult (results, "");
		}
		
		protected override void OnClean (IProgressMonitor monitor)
		{
			isDirty = true;
			this.NeedsBuilding = true;
			monitor.Log.WriteLine (GettextCatalog.GetString ("Removing all .mo files."));
			TranslationProjectConfiguration config = (TranslationProjectConfiguration)this.ActiveConfiguration;
			string outputDirectory = OutputDirectory;
			if (string.IsNullOrEmpty (outputDirectory))
				return;
			foreach (Translation translation in this.Translations) {
				string moDirectory = Path.Combine (Path.Combine (outputDirectory, translation.IsoCode), "LC_MESSAGES");
				string moFileName  = Path.Combine (moDirectory, config.PackageName + ".mo");
				if (File.Exists (moFileName)) 
					File.Delete (moFileName);
			}
		}
		
		protected override void OnExecute (IProgressMonitor monitor, ExecutionContext context)
		{
		}
		
#region Deployment
		public DeployFileCollection GetDeployFiles ()
		{
			DeployFileCollection result = new DeployFileCollection ();
			TranslationProjectConfiguration config = (TranslationProjectConfiguration)this.ActiveConfiguration;
			foreach (Translation translation in this.Translations) {
				string poFileName  = GetFileName (translation);
				string moDirectory = Path.Combine ("locale", translation.IsoCode);
				moDirectory = Path.Combine (moDirectory, "LC_MESSAGES");
				string moFileName  = Path.Combine (moDirectory, config.PackageName + ".mo");
				result.Add (new DeployFile (this, poFileName, moFileName, TargetDirectory.CommonApplicationDataRoot)); 
			}
			return result;
		}
#endregion
		
		bool isDirty = true;
		protected override bool OnGetNeedsBuilding ()
		{
			return this.isDirty;
		}
		
		protected override void OnSetNeedsBuilding (bool val)
		{
			isDirty = val;
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
	
	public enum TranslationOutputType {
		RelativeToOutput,
		SystemPath
	}
	
	public class TranslationProjectConfiguration : IConfiguration
	{
		[ItemProperty("name")]
		string name = null;
		
		[ItemProperty("packageName")]
		string packageName = null;
		
		[ItemProperty("outputType")]
		TranslationOutputType outputType;
			
		[ItemProperty("relPath")]
		string relPath = null;
		
		[ItemProperty("absPath")]
		string absPath = null;
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public string PackageName {
			get { return packageName; }
			set { packageName = value; }
		}
		
		public string RelPath {
			get { return relPath; }
			set { relPath = value; }
		}
		
		public string AbsPath {
			get { return absPath; }
			set { absPath = value; }
		}
		
		public TranslationOutputType OutputType {
			get { return outputType; }
			set { outputType = value; }
		}
		
		public TranslationProjectConfiguration ()
		{
		}
		
		public TranslationProjectConfiguration (string name)
		{
			this.name = name;
		}

		public object Clone ()		
		{
			IConfiguration conf = (IConfiguration) MemberwiseClone ();
			conf.CopyFrom (this);
			return conf;
		}
		
		public virtual void CopyFrom (IConfiguration configuration)
		{
		}
	}
	
	public class TranslationProjectInformation
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
