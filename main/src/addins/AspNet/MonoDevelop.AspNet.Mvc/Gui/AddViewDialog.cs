// 
// AddViewDialog.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.AspNet.StateEngine;
using PP = System.IO.Path;

using MonoDevelop.AspNet.Gui;
using MonoDevelop.Ide;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.Core;
using MonoDevelop.Components;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.Mvc.Gui
{
	
	
	public partial class AddViewDialog : Gtk.Dialog
	{	
		AspMvcProject project;
		IDictionary<string, IList<string>> loadedTemplateList;
		IDictionary<string, Gtk.ListStore> templateStore;
		Gtk.ListStore dataClassStore;
		string oldMaster;
		string oldEngine;
		Gtk.ListStore primaryPlaceholderStore = new Gtk.ListStore (typeof (String));
		System.CodeDom.Compiler.CodeDomProvider provider;
		TypeDataProvider classDataProvider;
		
		public AddViewDialog (AspMvcProject project)
		{
			this.project = project;
			this.Build ();
			
			provider = project.LanguageBinding.GetCodeDomProvider ();

			var viewEngines = GetProperViewEngines ();
			loadedTemplateList = new Dictionary<string, IList<string>> ();
			foreach (var engine in viewEngines) {
				viewEngineCombo.AppendText (engine);
				loadedTemplateList[engine] = project.GetCodeTemplates ("AddView", engine);
			}

			viewEngineCombo.Active = 0;
			InitializeTemplateStore (loadedTemplateList);
			
			ContentPlaceHolders = new List<string> ();
			string siteMaster = project.VirtualToLocalPath ("~/Views/Shared/Site.master", null);
			if (project.Files.GetFile (siteMaster) != null)
				masterEntry.Text = "~/Views/Shared/Site.master";
			
			primaryPlaceholderCombo.Model = primaryPlaceholderStore;
			
			UpdateTypePanelSensitivity (null, null);
			UpdateMasterPanelSensitivity (null, null);
			Validate ();
		}

		IEnumerable<string> GetProperViewEngines ()
		{
			yield return "Aspx";
			if (MvcVersion.CompareTo ("2.0.0.0") > 0)
				yield return "Razor";
		}

		void InitializeTemplateStore (IDictionary<string, IList<string>> tempList)
		{
			templateStore = new Dictionary<string, Gtk.ListStore> ();

			foreach (var engine in tempList.Keys) {
				bool foundEmptyTemplate = false;
				templateStore[engine] = new Gtk.ListStore (typeof (string));

				foreach (string file in tempList[engine]) {
					string name = PP.GetFileNameWithoutExtension (file);
					if (!foundEmptyTemplate) {
						if (name == "Empty") {
							templateStore[engine].InsertWithValues (0, name);
							foundEmptyTemplate = true;
						} else
							templateStore[engine].AppendValues (name);
					}
				}

				if (!foundEmptyTemplate)
					throw new Exception ("The Empty.tt template is missing.");
			}

			UpdateTemplateList ();
		}

		void UpdateTemplateList ()
		{
			templateCombo.Model = templateStore[ActiveViewEngine];
			oldEngine = ActiveViewEngine;
			templateCombo.Active = 0;
		}
		
		protected virtual void Validate (object sender, EventArgs e)
		{
			Validate ();
		}
		
		void Validate ()
		{
			buttonOk.Sensitive = IsValid ();
		}
	
		protected virtual void UpdateMasterPanelSensitivity (object sender, EventArgs e)
		{
			bool canHaveMaster = !IsPartialView;
			masterCheck.Sensitive = canHaveMaster;
			masterPanel.Sensitive = canHaveMaster && HasMaster;
			placeholderBox.Sensitive = masterPanel.Sensitive && ActiveViewEngine != "Razor";
			MasterChanged (null, null);
			Validate ();
		}
		
		protected virtual void UpdateTypePanelSensitivity (object sender, EventArgs e)
		{
			bool enabled = typePanel.Sensitive = stronglyTypedCheck.Active;

			if (enabled && classDataProvider == null) {
				classDataProvider = new TypeDataProvider (project);
				dataClassStore = new Gtk.ListStore (typeof (string));
				foreach (var item in classDataProvider.TypeNamesList)
					dataClassStore.AppendValues (item);
				dataClassCombo.Model = dataClassStore;
				if (classDataProvider.TypeNamesList.Count > 0)
					dataClassCombo.Active = 0;
			}

			Validate ();
		}
		
		public override void Dispose ()
		{
			Destroy ();
			base.Dispose ();
		}
		
		public bool IsValid ()
		{
			if (!IsValidIdentifier (ViewName))
				return false;

			if (!IsPartialView && HasMaster && ActiveViewEngine != "Razor") {
				if (String.IsNullOrEmpty (MasterFile) || !System.IO.File.Exists (project.VirtualToLocalPath (oldMaster, null)))
					return false;
				//PrimaryPlaceHolder can be empty
				//Layout Page can be empty in Razor Views - it's usually set in _ViewStart.cshtml file
			}
			
			if (IsStronglyTyped && String.IsNullOrEmpty(ViewDataTypeString))
			    return false;
			
			return true;
		}
		
		bool IsValidIdentifier (string identifier)
		{
			return !String.IsNullOrEmpty (identifier) && provider.IsValidIdentifier (identifier);
		}
	
		protected virtual void ShowMasterSelectionDialog (object sender, System.EventArgs e)
		{
			string pattern, title;
			if (ActiveViewEngine == "Razor") {
				pattern = "*.cshtml";
				title = MonoDevelop.Core.GettextCatalog.GetString ("Select a Layout file...");
			} else {
				pattern = "*.master";
				title = MonoDevelop.Core.GettextCatalog.GetString ("Select a Master Page...");
			}
			var dialog = new MonoDevelop.Ide.Projects.ProjectFileSelectorDialog (project, null, pattern)
			{
				Title = title,
				TransientFor = this,
			};
			try {
				if (MessageService.RunCustomDialog (dialog) == (int) Gtk.ResponseType.Ok)
					masterEntry.Text = project.LocalToVirtualPath (dialog.SelectedFile.FilePath);
			} finally {
				dialog.Destroy ();
			}
		}
		
		protected virtual void MasterChanged (object sender, EventArgs e)
		{
			if (IsPartialView || !HasMaster)
				return;
			
			if (masterEntry.Text == oldMaster)
				return;
			oldMaster = masterEntry.Text;
			
			primaryPlaceholderStore.Clear ();
			ContentPlaceHolders.Clear ();
			
			string realPath = project.VirtualToLocalPath (oldMaster, null);
			if (!File.Exists (realPath))
				return;
			
			var pd = TypeSystemService.ParseFile (project, realPath)
				as AspNetParsedDocument;
			
			if (pd != null) {
				try {
					ContentPlaceHolders.AddRange (pd.XDocument.GetAllPlaceholderIds ());
					
					for (int i = 0; i < ContentPlaceHolders.Count; i++) {
						string placeholder = ContentPlaceHolders[i];
						primaryPlaceholderStore.AppendValues (placeholder);
						
						if (placeholder.Contains ("main") || placeholder.Contains ("Main") 
							|| placeholder.Contains ("content") || placeholder.Contains ("Content"))
							primaryPlaceholderCombo.Active = i;
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Unhandled exception getting master regions for '" + realPath + "'", ex);
				}
			}
			
			Validate ();
		}

		protected virtual void ViewEngineChanged (object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty (oldEngine))
				return;
			if (oldEngine != ActiveViewEngine) {
				UpdateTemplateList ();
				UpdateMasterPanelSensitivity (null, null);
			}
		}

		protected virtual void DataClassChanged (object sender, EventArgs e)
		{
			Validate ();
		}
		
		#region Public properties
		
		public Type ViewDataType {
			get {
				return dataClassCombo.Active >= 0 ? (Type)classDataProvider.TypesList[dataClassCombo.Active] : System.Type.GetType(dataClassCombo.ActiveText, false);
			}
		}

		public string ViewDataTypeString {
			get {
				return dataClassCombo.ActiveText;
			}
		}
		
		public string MasterFile {
			get {
				return masterEntry.Text;
			}
		}
		
		public bool HasMaster {
			get {
				return masterCheck.Active;
			}
		}
		
		public string PrimaryPlaceHolder {
			get {
				return primaryPlaceholderCombo.ActiveText;
			}
		}
		
		public List<string> ContentPlaceHolders {
			get; private set;
		}
		
		public string TemplateFile {
			get {
				return loadedTemplateList[ActiveViewEngine][templateCombo.Active];
			}
		}
		
		public string ViewName {
			get {
				return nameEntry.Text;
			}
			set {
				nameEntry.Text = value ?? "";
			}
		}
		
		public bool IsPartialView {
			get { return partialCheck.Active; }
		}
		
		public bool IsStronglyTyped {
			get { return stronglyTypedCheck.Active; }
		}

		public string ActiveViewEngine {
			get { return viewEngineCombo.ActiveText; }
		}

		public string MvcVersion {
			get {
				return project.GetAspNetMvcVersion ();
			}
		}
		
		#endregion

		class TypeDataProvider
		{
			public List<ITypeDefinition> TypesList { get; private set; }
			public List<string> TypeNamesList { get; private set; }
			Ambience ambience;

			public TypeDataProvider (MonoDevelop.Projects.DotNetProject project)
			{
				TypeNamesList = new List<string> ();
				var ctx = TypeSystemService.GetCompilation (project);
				TypesList = new List<ITypeDefinition> (ctx.MainAssembly.GetAllTypeDefinitions ());
				this.ambience = AmbienceService.GetAmbience (project.LanguageName);
				foreach (var typeDef in TypesList) {
					TypeNamesList.Add (ambience.GetString ((IEntity)typeDef, OutputFlags.IncludeGenerics | OutputFlags.UseFullName | OutputFlags.IncludeMarkup));
				}
			}
		}
	}
}

		
