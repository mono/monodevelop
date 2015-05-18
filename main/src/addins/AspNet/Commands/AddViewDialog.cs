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
using System.IO;
using System.Collections.Generic;
using PP = System.IO.Path;

using Gtk;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.WebForms;
using MonoDevelop.Projects;
using MonoDevelop.AspNet.WebForms.Dom;

namespace MonoDevelop.AspNet.Commands
{
	class AddViewDialog : Dialog
	{
		readonly DotNetProject project;
		readonly AspNetAppProjectFlavor aspFlavor;
		IDictionary<string, IList<string>> loadedTemplateList;
		IDictionary<string, ListStore> templateStore;
		ListStore dataClassStore;
		string oldMaster;
		string oldEngine;
		ListStore primaryPlaceholderStore = new ListStore (typeof (String));
		System.CodeDom.Compiler.CodeDomProvider provider;
		TypeDataProvider classDataProvider;

		Button buttonCancel, buttonOk, masterButton;
		Label placeholderLabel;
		ComboBox viewEngineCombo, templateCombo;
		ComboBoxEntry placeholderCombo, dataClassCombo;
		Entry nameEntry;
		Entry masterEntry;
		CheckButton partialCheck, stronglyTypedCheck, masterCheck;
		Alignment typePanel, masterPanel;

		public AddViewDialog (DotNetProject project)
		{
			this.project = project;
			aspFlavor = project.GetService<AspNetAppProjectFlavor> ();

			Build ();
			
			provider = project.LanguageBinding.GetCodeDomProvider ();

			var viewEngines = GetProperViewEngines ();
			loadedTemplateList = new Dictionary<string, IList<string>> ();
			foreach (var engine in viewEngines) {
				viewEngineCombo.AppendText (engine);
				loadedTemplateList[engine] = aspFlavor.GetCodeTemplates ("AddView", engine);
			}

			viewEngineCombo.Active = 0;
			InitializeTemplateStore (loadedTemplateList);

			ContentPlaceHolders = new List<string> ();
			string siteMaster = aspFlavor.VirtualToLocalPath ("~/Views/Shared/Site.master", null);
			if (project.Files.GetFile (siteMaster) != null)
				masterEntry.Text = "~/Views/Shared/Site.master";
			
			placeholderCombo.Model = primaryPlaceholderStore;
			
			UpdateTypePanelSensitivity (null, null);
			UpdateMasterPanelSensitivity (null, null);
			Validate ();
		}

		void Build ()
		{
			DefaultWidth = 470;
			DefaultHeight = 380;
			BorderWidth = 6;
			Resizable = false;

			VBox.Spacing = 6;

			buttonCancel = new Button (Gtk.Stock.Cancel);
			AddActionWidget (buttonCancel, ResponseType.Cancel);
			buttonOk = new Button (Gtk.Stock.Ok);
			AddActionWidget (buttonOk, ResponseType.Ok);

			var table = new Table (3, 2, false) { ColumnSpacing = 6, RowSpacing = 6 };

			nameEntry = new Entry { WidthRequest = 350 };
			viewEngineCombo = ComboBox.NewText ();
			templateCombo = ComboBox.NewText ();

			var nameLabel = new Label (GettextCatalog.GetString ("_Name")) {
				MnemonicWidget = nameEntry,
				Xalign = 0
			};
			var templateLabel = new Label (GettextCatalog.GetString ("_Template:")) {
				MnemonicWidget = nameEntry,
				Xalign = 0
			};
			var engineLabel = new Label (GettextCatalog.GetString ("_View Engine:")) {
				MnemonicWidget = nameEntry,
				Xalign = 0
			};

			const AttachOptions expandFill = AttachOptions.Expand | AttachOptions.Fill;
			const AttachOptions fill = AttachOptions.Fill;

			table.Attach (nameLabel,       0, 1, 0, 1, fill,       0, 0, 0);
			table.Attach (nameEntry,       1, 2, 0, 1, expandFill, 0, 0, 0);
			table.Attach (templateLabel,   0, 1, 1, 2, fill,       0, 0, 0);
			table.Attach (templateCombo,   1, 2, 1, 2, expandFill, 0, 0, 0);
			table.Attach (engineLabel,     0, 1, 2, 3, fill,       0, 0, 0);
			table.Attach (viewEngineCombo, 1, 2, 2, 3, expandFill, 0, 0, 0);

			VBox.PackStart (table, false, false, 0);

			var frame = new Frame (GettextCatalog.GetString ("Options")) { BorderWidth = 2 };
			var optionsVBox = new VBox { Spacing = 6 };
			var optionsAlignment = new Alignment (0.5f, 0.5f, 1f, 1f) {
				Child = optionsVBox,
				TopPadding = 4,
				BottomPadding = 4,
				RightPadding = 4,
				LeftPadding = 4
			};
			frame.Add (optionsAlignment);

			partialCheck = new CheckButton (GettextCatalog.GetString ("_Partial view")) { UseUnderline = true };
			stronglyTypedCheck = new CheckButton (GettextCatalog.GetString ("_Strongly typed")) { UseUnderline = true };
			masterCheck = new CheckButton (GettextCatalog.GetString ("Has _master page or layout")) { UseUnderline = true };

			dataClassCombo = ComboBoxEntry.NewText ();
			masterEntry = new Entry { WidthRequest = 250 };
			placeholderCombo = ComboBoxEntry.NewText ();
			masterButton = new Button ("...");

			optionsVBox.PackStart (partialCheck);
			optionsVBox.PackStart (stronglyTypedCheck);
			typePanel = WithLabelAndLeftPadding (dataClassCombo, GettextCatalog.GetString ("_Data class:"), true, 24);
			optionsVBox.PackStart (typePanel);
			optionsVBox.PackStart (masterCheck);


			var masterLabel = new Label (GettextCatalog.GetString ("_File:")) {
				MnemonicWidget = masterEntry,
				Xalign = 0,
				UseUnderline = true
			};

			placeholderLabel = new Label (GettextCatalog.GetString ("P_rimary placeholder:")) {
				MnemonicWidget = placeholderCombo,
				Xalign = 0,
				UseUnderline = true
			};

			var masterTable = new Table (2, 3, false) { RowSpacing = 6, ColumnSpacing = 6 };

			masterTable.Attach (masterLabel,      0, 1, 0, 1, fill,       0, 0, 0);
			masterTable.Attach (masterEntry,      1, 3, 0, 1, expandFill, 0, 0, 0);
			masterTable.Attach (placeholderLabel, 0, 1, 1, 2, expandFill, 0, 0, 0);
			masterTable.Attach (placeholderCombo, 1, 2, 1, 2, fill,       0, 0, 0);
			masterTable.Attach (masterButton,     2, 3, 1, 2, fill,       0, 0, 0);

			masterPanel = new Alignment (0.5f, 0.5f, 1f, 1f) { LeftPadding = 24, Child = masterTable };
			optionsVBox.PackStart (masterPanel);

			VBox.PackStart (frame, false, false, 0);

			Child.ShowAll ();

			viewEngineCombo.Changed += ViewEngineChanged;
			templateCombo.Changed += Validate;
			nameEntry.Changed += Validate;
			partialCheck.Toggled += UpdateMasterPanelSensitivity;
			stronglyTypedCheck.Toggled += UpdateTypePanelSensitivity;
			dataClassCombo.Changed += DataClassChanged;
			masterCheck.Toggled += UpdateMasterPanelSensitivity;
			masterEntry.Changed += MasterChanged;
			masterButton.Clicked += ShowMasterSelectionDialog;
			placeholderCombo.Changed += Validate;

		}

		static Alignment WithLabelAndLeftPadding (Widget w, string labelText, bool underline, uint leftPadding)
		{
			var label = new Label (labelText) {
				MnemonicWidget = w,
				Xalign = 0,
				UseUnderline = underline
			};

			var box = new HBox (false, 6);
			box.PackStart (label);
			box.PackStart (w, true, true, 0);

			return new Alignment (0.5f, 0.5f, 1f, 1f) { LeftPadding = leftPadding, Child = box };
		}

		IEnumerable<string> GetProperViewEngines ()
		{
			yield return "Aspx";
			if (aspFlavor.SupportsRazorViewEngine)
				yield return "Razor";
		}

		void InitializeTemplateStore (IDictionary<string, IList<string>> tempList)
		{
			templateStore = new Dictionary<string, ListStore> ();

			foreach (var engine in tempList.Keys) {
				bool foundEmptyTemplate = false;
				templateStore[engine] = new ListStore (typeof (string));

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
	
		protected void UpdateMasterPanelSensitivity (object sender, EventArgs e)
		{
			bool canHaveMaster = !IsPartialView;
			masterCheck.Sensitive = canHaveMaster;
			masterPanel.Sensitive = canHaveMaster && HasMaster;
			placeholderLabel.Sensitive = placeholderCombo.Sensitive = masterPanel.Sensitive && ActiveViewEngine != "Razor";
			MasterChanged (null, null);
			Validate ();
		}
		
		protected void UpdateTypePanelSensitivity (object sender, EventArgs e)
		{
			bool enabled = typePanel.Sensitive = stronglyTypedCheck.Active;

			if (enabled && classDataProvider == null) {
				classDataProvider = new TypeDataProvider (project);
				dataClassStore = new ListStore (typeof (string));
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
				if (String.IsNullOrEmpty (MasterFile) || !File.Exists (aspFlavor.VirtualToLocalPath (oldMaster, null)))
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
	
		protected virtual void ShowMasterSelectionDialog (object sender, EventArgs e)
		{
			string pattern, title;
			if (ActiveViewEngine == "Razor") {
				pattern = "*.cshtml";
				title = GettextCatalog.GetString ("Select a Layout file...");
			} else {
				pattern = "*.master";
				title = GettextCatalog.GetString ("Select a Master Page...");
			}
			var dialog = new MonoDevelop.Ide.Projects.ProjectFileSelectorDialog (project, null, pattern)
			{
				Title = title,
				TransientFor = this,
			};
			try {
				if (MessageService.RunCustomDialog (dialog) == (int) ResponseType.Ok)
					masterEntry.Text = aspFlavor.LocalToVirtualPath (dialog.SelectedFile.FilePath);
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
			
			string realPath = aspFlavor.VirtualToLocalPath (oldMaster, null);
			if (!File.Exists (realPath))
				return;
			
			var pd = TypeSystemService.ParseFile (project, realPath).Result as WebFormsParsedDocument;
			
			if (pd != null) {
				try {
					ContentPlaceHolders.AddRange (pd.XDocument.GetAllPlaceholderIds ());
					
					for (int i = 0; i < ContentPlaceHolders.Count; i++) {
						string placeholder = ContentPlaceHolders[i];
						primaryPlaceholderStore.AppendValues (placeholder);
						
						if (placeholder.Contains ("main") || placeholder.Contains ("Main") 
							|| placeholder.Contains ("content") || placeholder.Contains ("Content"))
							placeholderCombo.Active = i;
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
				return placeholderCombo.ActiveText;
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
		
		#endregion

		class TypeDataProvider
		{
			public List<INamedTypeSymbol> TypesList { get; private set; }
			public List<string> TypeNamesList { get; private set; }

			public TypeDataProvider (MonoDevelop.Projects.DotNetProject project)
			{
				TypeNamesList = new List<string> ();
				var ctx = TypeSystemService.GetCompilationAsync (project).Result;
				TypesList = new List<INamedTypeSymbol> (ctx.GetAllTypesInMainAssembly ());
				foreach (var typeDef in TypesList) {
					TypeNamesList.Add (Ambience.EscapeText (typeDef.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat)));
				}
			}
		}
	}
}
