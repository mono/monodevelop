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
using System.Xml;

using MonoDevelop.Core;
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
		
		int updateLevel = 0;
		Dictionary<string, Catalog> catalogs = new Dictionary<string, Catalog> (); 
		public void BeginUpdate ()
		{
			updateLevel++;
			if (updateLevel == 1) {
				catalogs.Clear ();
			}
		}
		
		public void EndUpdate ()
		{
			updateLevel--;
			if (updateLevel == 0) {
				foreach (KeyValuePair<string, Catalog> catalog in catalogs) {
					catalog.Value.Save (catalog.Key);
				}
			}
		}
		public void AddTranslationStrings (Translation translation, string fileName, List<TranslationProject.MatchLocation> matches)
		{
			string relativeFileName = MonoDevelop.Core.Runtime.FileService.AbsoluteToRelativePath (this.BaseDirectory, fileName);
			string poFileName = GetFileName (translation);
			if (!catalogs.ContainsKey (poFileName))
				catalogs[poFileName] = new Catalog (poFileName);
			Catalog catalog = catalogs[poFileName];
			
			foreach (CatalogEntry entry in catalog) {
				foreach (string reference in entry.References) {
					if (reference.StartsWith (relativeFileName + ":"))
						entry.RemoveReference (reference);
				}
			}
			
			foreach (MatchLocation match in matches) {
				CatalogEntry entry = catalog.FindItem (match.OriginalString);
				if (entry == null) {
					entry = new CatalogEntry (catalog, match.OriginalString, match.OriginalPluralString);
					if (!String.IsNullOrEmpty (match.OriginalPluralString))
						entry.SetTranslations (new string[] {"", ""});
					catalog.AddItem (entry);
				}
				entry.AddReference (relativeFileName + ":" + match.Line);
			}
		}
		
		public void AddTranslationStrings (string fileName, List<TranslationProject.MatchLocation> matches)
		{
			foreach (Translation translation in this.Translations) {
				AddTranslationStrings (translation, fileName, matches);
			}
		}
		
		public void AddNewTranslation (string isoCode, IProgressMonitor monitor)
		{
			try {
				translations.Add (new Translation (isoCode));
				File.WriteAllText (GetFileName (isoCode), "");
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
		
		public void RemoveEntry (string msgstr)
		{
			foreach (Translation translation in this.Translations) {
				string poFileName  = GetFileName (translation);
				Catalog catalog = new Catalog (poFileName);
				CatalogEntry entry   = catalog.FindItem (msgstr);
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
					
					System.Diagnostics.Process process = new System.Diagnostics.Process ();
					process.StartInfo.FileName = "msgfmt";
					process.StartInfo.Arguments = poFileName + " -o " + moFileName;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.RedirectStandardError = true;
					process.Start ();
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
				string moDirectory = Path.Combine (translation.IsoCode, "LC_MESSAGES");
				string moFileName  = Path.Combine (moDirectory, config.PackageName + ".mo");
				result.Add (new DeployFile (this, poFileName, moFileName)); 
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
