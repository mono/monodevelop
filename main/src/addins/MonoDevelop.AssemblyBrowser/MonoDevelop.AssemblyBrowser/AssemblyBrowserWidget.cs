//
// AssemblyBrowserWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;
using System.Text;
using System.Xml;
using Gtk;

using Mono.Cecil;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Mono.TextEditor.Theatrics;
using MonoDevelop.SourceEditor;
using XmlDocIdLib;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.AssemblyBrowser
{
	[System.ComponentModel.Category("MonoDevelop.AssemblyBrowser")]
	[System.ComponentModel.ToolboxItem(true)]
	partial class AssemblyBrowserWidget : Gtk.Bin
	{
		Gtk.Button buttonBack;
		Gtk.Button buttonForeward;
		Gtk.ComboBox comboboxVisibilty;
		MonoDevelop.Components.SearchEntry searchentry1;
		Gtk.ComboBox languageCombobox;

		public AssemblyBrowserTreeView TreeView {
			get;
			private set;
		}
		
		public bool PublicApiOnly {
			get {
				return TreeView.PublicApiOnly;
			}
		}
		
		Ambience ambience = AmbienceService.GetAmbience ("text/x-csharp");
		public Ambience Ambience {
			get { return ambience; }
		}
		
		DocumentationPanel documentationPanel = new DocumentationPanel ();
		readonly TextEditor inspectEditor;

		public class AssemblyBrowserTreeView : ExtensibleTreeView
		{
			bool publicApiOnly = true;

			public bool PublicApiOnly {
				get {
					return publicApiOnly;
				}
				set {
					if (publicApiOnly == value)
						return;
					publicApiOnly = value;
					var root = GetRootNode ();
					if (root != null)
						RefreshNode (root);
				}
			}

			public AssemblyBrowserTreeView (NodeBuilder[] builders, TreePadOption[] options) : base (builders, options)
			{
			}
		}

		public AssemblyBrowserWidget ()
		{
			this.Build ();

			buttonBack = new Gtk.Button (new Gtk.Image (ImageService.GetPixbuf ("md-breadcrumb-prev")));
			buttonBack.Clicked += OnNavigateBackwardActionActivated;

			buttonForeward = new Gtk.Button (new Gtk.Image (ImageService.GetPixbuf ("md-breadcrumb-next")));
			buttonForeward.Clicked += OnNavigateForwardActionActivated;

			comboboxVisibilty = ComboBox.NewText ();
			comboboxVisibilty.InsertText (0, GettextCatalog.GetString ("Only public members"));
			comboboxVisibilty.InsertText (1, GettextCatalog.GetString ("All members"));
			comboboxVisibilty.Active = 0;
			comboboxVisibilty.Changed += delegate {
				TreeView.PublicApiOnly = comboboxVisibilty.Active == 0;
				FillInspectLabel ();
			};

			searchentry1 = new MonoDevelop.Components.SearchEntry ();
			searchentry1.Ready = true;
			searchentry1.HasFrame = true;
			searchentry1.WidthRequest = 200;
			searchentry1.Visible = true;
			searchentry1.EmptyMessage = GettextCatalog.GetString ("Search for types or members");
			searchentry1.InnerEntry.Changed += SearchEntryhandleChanged;

			CheckMenuItem checkMenuItem = this.searchentry1.AddFilterOption (0, GettextCatalog.GetString ("Types"));
			checkMenuItem.Active = true;
			checkMenuItem.Toggled += delegate {
				if (checkMenuItem.Active) {
					searchMode = AssemblyBrowserWidget.SearchMode.Type;
					CreateColumns ();
					StartSearch ();
				}
			};
			
			CheckMenuItem checkMenuItem1 = this.searchentry1.AddFilterOption (1, GettextCatalog.GetString ("Members"));
			checkMenuItem1.Toggled += delegate {
				if (checkMenuItem1.Active) {
					searchMode = AssemblyBrowserWidget.SearchMode.Member;
					CreateColumns ();
					StartSearch ();
				}
			};

			languageCombobox = Gtk.ComboBox.NewText ();
			languageCombobox.AppendText (GettextCatalog.GetString ("Summary"));
			languageCombobox.AppendText (GettextCatalog.GetString ("IL"));
			languageCombobox.AppendText (GettextCatalog.GetString ("C#"));
			languageCombobox.Active = PropertyService.Get ("AssemblyBrowser.InspectLanguage", 2);
			languageCombobox.Changed += LanguageComboboxhandleChanged;

			loader = new CecilLoader (true);
			loader.IncludeInternalMembers = true;
			TreeView = new AssemblyBrowserTreeView (new NodeBuilder[] { 
				new ErrorNodeBuilder (),
				new ProjectNodeBuilder (this),
				new AssemblyNodeBuilder (this),
				new ModuleReferenceNodeBuilder (),
				new AssemblyReferenceNodeBuilder (this),
				new AssemblyReferenceFolderNodeBuilder (this),
				new AssemblyResourceFolderNodeBuilder (),
				new ResourceNodeBuilder (),
				new NamespaceBuilder (this),
				new DomTypeNodeBuilder (this),
				new DomMethodNodeBuilder (this),
				new DomFieldNodeBuilder (this),
				new DomEventNodeBuilder (this),
				new DomPropertyNodeBuilder (this),
				new BaseTypeFolderNodeBuilder (this),
				new BaseTypeNodeBuilder (this)
				}, new TreePadOption [0]);
			TreeView.Tree.Selection.Mode = Gtk.SelectionMode.Single;
			TreeView.Tree.CursorChanged += HandleCursorChanged;
			TreeView.ShadowType = ShadowType.None;
			TreeView.BorderWidth = 1;
			TreeView.ShowBorderLine = false;
			TreeView.Zoom = 1.0;
			treeViewPlaceholder.Add (TreeView);

//			this.descriptionLabel.ModifyFont (Pango.FontDescription.FromString ("Sans 9"));
//			this.documentationLabel.ModifyFont (Pango.FontDescription.FromString ("Sans 12"));
//			this.documentationLabel.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 225));
//			this.documentationLabel.Wrap = true;
			
			var options = new MonoDevelop.Ide.Gui.CommonTextEditorOptions () {
				ShowFoldMargin = false,
				ShowIconMargin = false,
				ShowLineNumberMargin = false,
				HighlightCaretLine = true,
			};
			inspectEditor = new TextEditor (new TextDocument (), options);
			inspectEditor.ButtonPressEvent += HandleInspectEditorButtonPressEvent;
			
			this.inspectEditor.Document.ReadOnly = true;
//			this.inspectEditor.Document.SyntaxMode = new Mono.TextEditor.Highlighting.MarkupSyntaxMode ();
			this.inspectEditor.TextViewMargin.GetLink = delegate(Mono.TextEditor.MarginMouseEventArgs arg) {
				var loc = inspectEditor.PointToLocation (arg.X, arg.Y);
				int offset = inspectEditor.LocationToOffset (loc);
				var referencedSegment = ReferencedSegments != null ? ReferencedSegments.FirstOrDefault (seg => seg.Segment.Contains (offset)) : null;
				if (referencedSegment == null)
					return null;
				if (referencedSegment.Reference is TypeDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((TypeDefinition)referencedSegment.Reference);
				
				if (referencedSegment.Reference is MethodDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((MethodDefinition)referencedSegment.Reference);
				
				if (referencedSegment.Reference is PropertyDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((PropertyDefinition)referencedSegment.Reference);
				
				if (referencedSegment.Reference is FieldDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((FieldDefinition)referencedSegment.Reference);
				
				if (referencedSegment.Reference is EventDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((EventDefinition)referencedSegment.Reference);
				
				if (referencedSegment.Reference is FieldDefinition)
					return new XmlDocIdGenerator ().GetXmlDocPath ((FieldDefinition)referencedSegment.Reference);

				if (referencedSegment.Reference is TypeReference) {
					return new XmlDocIdGenerator ().GetXmlDocPath ((TypeReference)referencedSegment.Reference);
				}
				return referencedSegment.Reference.ToString ();
			};
			this.inspectEditor.LinkRequest += InspectEditorhandleLinkRequest;
			documentationScrolledWindow.Add (inspectEditor);

			this.hpaned1.ExposeEvent += HPaneExpose;
			hpaned1 = hpaned1.ReplaceWithWidget (new HPanedThin (), true);
			hpaned1.Position = 271;

			this.notebook1.SetTabLabel (this.documentationScrolledWindow, new Label (GettextCatalog.GetString ("Documentation")));
			this.notebook1.SetTabLabel (this.searchWidget, new Label (GettextCatalog.GetString ("Search")));
			notebook1.Page = 0;
			//this.searchWidget.Visible = false;
				
			typeListStore = new Gtk.ListStore (typeof(Gdk.Pixbuf), // type image
			                                   typeof(string), // name
			                                   typeof(string), // namespace
			                                   typeof(string), // assembly
				                               typeof(IMember)
			                                  );
			
			memberListStore = new Gtk.ListStore (typeof(Gdk.Pixbuf), // member image
			                                   typeof(string), // name
			                                   typeof(string), // Declaring type full name
			                                   typeof(string), // assembly
				                               typeof(IMember)
			                                  );
			CreateColumns ();
//			this.searchEntry.Changed += SearchEntryhandleChanged;
			this.searchTreeview.RowActivated += SearchTreeviewhandleRowActivated;
			this.notebook1.ShowTabs = false;
			this.ShowAll ();
		}

		internal void SetToolbar (DocumentToolbar toolbar)
		{
			toolbar.Add (buttonBack);

			toolbar.Add (buttonForeward);

			toolbar.Add (new VSeparator ());

			Gtk.Label la = new Label (GettextCatalog.GetString ("Visibility"));
			toolbar.Add (la);

			toolbar.Add (comboboxVisibilty);

			la = new Label ("");
			toolbar.Add (la, true);

			toolbar.Add (searchentry1);

			la = new Label (GettextCatalog.GetString ("Language"));
			toolbar.Add (la);

			toolbar.Add (languageCombobox);

			toolbar.ShowAll ();
		}
		
		[CommandHandler (EditCommands.Copy)]
		protected void OnCopyCommand ()
		{
			inspectEditor.RunAction (Mono.TextEditor.ClipboardActions.Copy);
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		protected void OnSelectAllCommand ()
		{
			inspectEditor.RunAction (Mono.TextEditor.SelectionActions.SelectAll);
		}
		
		void HandleInspectEditorButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button != 3)
				return;
			var menuSet = new CommandEntrySet ();
			menuSet.AddItem (EditCommands.SelectAll);
			menuSet.AddItem (EditCommands.Copy);
			IdeApp.CommandService.ShowContextMenu (menuSet, this);
		}

		void SearchTreeviewhandleRowActivated (object o, RowActivatedArgs args)
		{
			TreeIter selectedIter;
			if (searchTreeview.Selection.GetSelected (out selectedIter)) {
				var member = (IUnresolvedEntity)(searchMode != SearchMode.Type ? memberListStore.GetValue (selectedIter, 4) : typeListStore.GetValue (selectedIter, 4));
				
				var nav = SearchMember (member);
				if (nav != null) {
					notebook1.Page = 0;
				}
			}
		}

		void SearchEntryhandleChanged (object sender, EventArgs e)
		{
			StartSearch ();
		}
		
		void LanguageComboboxhandleChanged (object sender, EventArgs e)
		{
			this.notebook1.Page = 0;
			PropertyService.Set ("AssemblyBrowser.InspectLanguage", this.languageCombobox.Active);
			FillInspectLabel ();
		}

		void InspectEditorhandleLinkRequest (object sender, Mono.TextEditor.LinkEventArgs args)
		{
			var loader = (AssemblyLoader)this.TreeView.GetSelectedNode ().GetParentDataItem (typeof(AssemblyLoader), true);

			if (args.Button == 2 || (args.Button == 1 && (args.ModifierState & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)) {
				AssemblyBrowserViewContent assemblyBrowserView = new AssemblyBrowserViewContent ();
				foreach (var cu in definitions) {
					assemblyBrowserView.Load (cu.UnresolvedAssembly.AssemblyName);
				}
				IdeApp.Workbench.OpenDocument (assemblyBrowserView, true);
				((AssemblyBrowserWidget)assemblyBrowserView.Control).Open (args.Link);
			} else {
				this.Open (args.Link, loader);
			}
		}
		
		public IEntity ActiveMember  {
			get;
			set;
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			TreeView.GrabFocus ();
		}
		
		ITreeNavigator SearchMember (IUnresolvedEntity member)
		{
			return SearchMember (GetIdString (member));
		}
			
		ITreeNavigator SearchMember (string helpUrl)
		{
			return SearchMember (TreeView.GetRootNode (), helpUrl);
		}
		
		static void AppendTypeReference (StringBuilder result, ITypeReference type)
		{
			if (type is ByReferenceTypeReference) {
				var brtr = (ByReferenceTypeReference)type;
				AppendTypeReference (result, brtr.ElementType);
				return;
			}

			if (type is ArrayTypeReference) {
				var array = (ArrayTypeReference)type;
				AppendTypeReference (result, array.ElementType);
				result.Append ("[");
				result.Append (new string (',', array.Dimensions - 1));
				result.Append ("]");
				return;
			}

			if (type is PointerTypeReference) {
				var ptr = (PointerTypeReference)type;
				AppendTypeReference (result, ptr.ElementType);
				result.Append ("*");
				return;
			}

			if (type is GetClassTypeReference) {
				var r = (GetClassTypeReference)type;
				var n = r.FullTypeName.TopLevelTypeName;
				result.Append (n.Namespace + "." + n.Name);
				return;
			}

			if (type is IUnresolvedTypeDefinition) {
				result.Append (((IUnresolvedTypeDefinition)type).FullName);
			}
		}
		
		static void AppendHelpParameterList (StringBuilder result, IList<IUnresolvedParameter> parameters)
		{
			if (parameters == null || parameters.Count == 0)
				return;
			result.Append ('(');
			if (parameters != null) {
				for (int i = 0; i < parameters.Count; i++) {
					if (i > 0)
						result.Append (',');
					var p = parameters [i];
					if (p == null)
						continue;
					AppendTypeReference (result, p.Type);
					if (p.IsRef)
						result.Append ("&");
					if (p.IsOut) {
						result.Append ("@");
					}
				}
			}
			result.Append (')');
		}
		
		static string GetIdString (IUnresolvedEntity member)
		{
			StringBuilder sb;
			
			switch (member.EntityType) {
			case EntityType.TypeDefinition:
				var type = member as IUnresolvedTypeDefinition;
				if (type.TypeParameters.Count == 0)
					return "T:" + type.FullName;
				return "T:" + type.FullName + "`" + type.TypeParameters.Count;
			case EntityType.Method:
				var method = (IUnresolvedMethod)member;
				sb = new StringBuilder ();
				sb.Append ("M:");
				sb.Append (method.DeclaringTypeDefinition.ReflectionName);
				sb.Append (".");
				sb.Append (method.Name);
				if (method.TypeParameters.Count > 0) {
					sb.Append ("`");
					sb.Append (method.TypeParameters.Count);
				}
				AppendHelpParameterList (sb, method.Parameters);
				return sb.ToString ();
			case EntityType.Constructor:
				var constructor = (IUnresolvedMethod)member;
				sb = new StringBuilder ();
				sb.Append ("M:");
				sb.Append (constructor.DeclaringTypeDefinition.FullName);
				sb.Append (".#ctor");
				AppendHelpParameterList (sb, constructor.Parameters);
				return sb.ToString ();
			case EntityType.Destructor: // todo
				return "todo";
			case EntityType.Property:
				sb = new StringBuilder ();
				sb.Append ("P:");
				sb.Append (member.DeclaringTypeDefinition.ReflectionName);
				sb.Append (".");
				sb.Append (member.Name);
				return sb.ToString ();
			case EntityType.Indexer:
				var indexer = (IUnresolvedProperty)member;
				sb = new StringBuilder ();
				sb.Append ("P:");
				sb.Append (indexer.DeclaringTypeDefinition.ReflectionName);
				sb.Append (".Item");
				AppendHelpParameterList (sb, indexer.Parameters);
				return sb.ToString ();
			case EntityType.Field:
				sb = new StringBuilder ();
				sb.Append ("F:");
				sb.Append (member.DeclaringTypeDefinition.ReflectionName);
				sb.Append (".");
				sb.Append (member.Name);
				return sb.ToString ();
			case EntityType.Event:
				sb = new StringBuilder ();
				sb.Append ("E:");
				sb.Append (member.DeclaringTypeDefinition.ReflectionName);
				sb.Append (".");
				sb.Append (member.Name);
				return sb.ToString ();
			case EntityType.Operator: // todo
				return "todo";
			}
			return "unknown entity: " + member;
		}

		static string GetIdString (MethodDefinition methodDefinition)
		{
			var sb = new StringBuilder ();
			sb.Append ("M:");
			sb.Append (methodDefinition.FullName);
			if (methodDefinition.HasGenericParameters) {
				sb.Append ("`");
				sb.Append (methodDefinition.GenericParameters.Count);
			}
//			AppendHelpParameterList (sb, method.Parameters);
			return sb.ToString ();
		}

		static string GetIdString (TypeDefinition typeDefinition)
		{
			if (!typeDefinition.HasGenericParameters)
				return "T:" + typeDefinition.FullName;
			return "T:" + typeDefinition.FullName + "`" + typeDefinition.GenericParameters.Count;
		}		

		bool IsMatch (ITreeNavigator nav, string helpUrl, bool searchType)
		{
			var member = nav.DataItem as IUnresolvedEntity;
			if (member == null)
				return false;
			
			if (searchType) {
				if (member is IUnresolvedTypeDefinition)
					return GetIdString (member) == helpUrl;
			} else {
				if (member is IUnresolvedMember) {
					return GetIdString (member) == helpUrl;
				}
			}
			return false;
		}
			
		static bool SkipChildren (ITreeNavigator nav, string helpUrl, bool searchType)
		{
			if (nav.DataItem is IUnresolvedMember)
				return true;
			if (nav.DataItem is BaseTypeFolder)
				return true;
			if (nav.DataItem is Project)
				return true;
			if (nav.DataItem is AssemblyReferenceFolder)
				return true;
			if (nav.DataItem is AssemblyResourceFolder)
				return true;
			string strippedUrl = helpUrl;
			if (strippedUrl.Length > 2 && strippedUrl [1] == ':')
				strippedUrl = strippedUrl.Substring (2);
			int idx = strippedUrl.IndexOf ('~');
			if (idx > 0) 
				strippedUrl = strippedUrl.Substring (0, idx);
			
			var type = nav.DataItem as IUnresolvedTypeDefinition;
			if (type != null && !strippedUrl.StartsWith (type.FullName, StringComparison.Ordinal))
				return true;
			if (nav.DataItem is Namespace && !strippedUrl.StartsWith (((Namespace)nav.DataItem).Name, StringComparison.Ordinal))
				return true;
			return false;
		}
		
		ITreeNavigator SearchMember (ITreeNavigator nav, string helpUrl)
		{
			if (nav == null)
				return null;
			bool searchType = helpUrl.StartsWith ("T:");
			do {
				if (IsMatch (nav, helpUrl, searchType)) {
					inspectEditor.ClearSelection ();
					nav.ExpandToNode ();
					nav.Selected = nav.Expanded = true;
					return nav;
				}
				if (!SkipChildren (nav, helpUrl, searchType) && nav.HasChildren ()) {
					nav.MoveToFirstChild ();
					ITreeNavigator result = SearchMember (nav, helpUrl);
					if (result != null)
						return result;
					
					if (!nav.MoveToParent ()) {
						return null;
					}
					try {
						if (nav.DataItem is TypeDefinition && PublicApiOnly) {
							nav.MoveToFirstChild ();
							result = SearchMember (nav, helpUrl);
							if (result != null)
								return result;
							nav.MoveToParent ();
						}
					} catch (Exception) {
					}
				}
			} while (nav.MoveNext());
			return null;
		}
		
		enum SearchMode 
		{
			Type   = 0,
			Member = 1,
			Disassembler = 2,
			Decompiler = 3
		}
		
		SearchMode searchMode = SearchMode.Type;
		Gtk.ListStore memberListStore;
		Gtk.ListStore typeListStore;
		
		void CreateColumns ()
		{
			foreach (TreeViewColumn column in searchTreeview.Columns) {
				searchTreeview.RemoveColumn (column);
			}
			TreeViewColumn col;
			Gtk.CellRenderer crp, crt;
			switch (searchMode) {
			case SearchMode.Member:
			case SearchMode.Disassembler:
			case SearchMode.Decompiler:
				col = new TreeViewColumn ();
				col.Title = GettextCatalog.GetString ("Member");
				crp = new Gtk.CellRendererPixbuf ();
				crt = new Gtk.CellRendererText ();
				col.PackStart (crp, false);
				col.PackStart (crt, true);
				col.AddAttribute (crp, "pixbuf", 0);
				col.AddAttribute (crt, "text", 1);
				searchTreeview.AppendColumn (col);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Declaring Type"), new Gtk.CellRendererText (), "text", 2);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Assembly"), new Gtk.CellRendererText (), "text", 3);
				col.Resizable = true;
				searchTreeview.Model = memberListStore;
				break;
			case SearchMode.Type:
				col = new TreeViewColumn ();
				col.Title = GettextCatalog.GetString ("Type");
				crp = new Gtk.CellRendererPixbuf ();
				crt = new Gtk.CellRendererText ();
				col.PackStart (crp, false);
				col.PackStart (crt, true);
				col.AddAttribute (crp, "pixbuf", 0);
				col.AddAttribute (crt, "text", 1);
				searchTreeview.AppendColumn (col);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Namespace"), new Gtk.CellRendererText (), "text", 2);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Assembly"), new Gtk.CellRendererText (), "text", 3);
				col.Resizable = true;
				searchTreeview.Model = typeListStore;
				break;
			}
		}
		System.ComponentModel.BackgroundWorker searchBackgoundWorker = null;

		public void StartSearch ()
		{
			string query = searchentry1.Query;
			if (searchBackgoundWorker != null && searchBackgoundWorker.IsBusy)
				searchBackgoundWorker.CancelAsync ();
			
			if (string.IsNullOrEmpty (query)) {
				notebook1.Page = 0;
				return;
			}
			
			this.notebook1.Page = 1;
			
			switch (searchMode) {
			case SearchMode.Member:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching member..."));
				break;
			case SearchMode.Disassembler:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching string in disassembled code..."));
				break;
			case SearchMode.Decompiler:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching string in decompiled code..."));
				break;
			case SearchMode.Type:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching type..."));
				break;
			}
			memberListStore.Clear ();
			typeListStore.Clear ();
			
			searchBackgoundWorker = new BackgroundWorker ();
			searchBackgoundWorker.WorkerSupportsCancellation = true;
			searchBackgoundWorker.WorkerReportsProgress = false;
			searchBackgoundWorker.DoWork += SearchDoWork;
			searchBackgoundWorker.RunWorkerCompleted += delegate {
				searchBackgoundWorker = null;
			};
			
			searchBackgoundWorker.RunWorkerAsync (query);
		}
	
		void SearchDoWork (object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = sender as BackgroundWorker;
			try {
				string pattern = e.Argument.ToString ().ToUpper ();
				int types = 0, curType = 0;
				foreach (var unit in this.definitions) {
					types += unit.UnresolvedAssembly.TopLevelTypeDefinitions.Count ();
				}
				var memberDict = new Dictionary<AssemblyLoader, List<IUnresolvedMember>> ();
				switch (searchMode) {
				case SearchMode.Member:
					foreach (var unit in this.definitions) {
						var members = new List<IUnresolvedMember> ();
						foreach (var type in unit.UnresolvedAssembly.TopLevelTypeDefinitions) {
							if (worker.CancellationPending)
								return;
							curType++;
							foreach (var member in type.Members) {
								if (worker.CancellationPending)
									return;
								if (member.Name.ToUpper ().Contains (pattern)) {
									members.Add (member);
								}
							}
						}
						memberDict [unit] = members;
					}
					Gtk.Application.Invoke (delegate {
						IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
						foreach (var kv in memberDict) {
							foreach (var member in kv.Value) {
								if (worker.CancellationPending)
									return;
								memberListStore.AppendValues (ImageService.GetPixbuf (member.GetStockIcon (), Gtk.IconSize.Menu),
								                              member.Name,
								                              member.DeclaringTypeDefinition.FullName,
								                              kv.Key.Assembly.FullName,
								                              member);
							}
						}
					}
					);
					break;
				case SearchMode.Disassembler:
					Gtk.Application.Invoke (delegate {
						IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching string in disassembled code..."));
					}
					);
					foreach (var unit in this.definitions) {
						foreach (var type in unit.UnresolvedAssembly.TopLevelTypeDefinitions) {
							if (worker.CancellationPending)
								return;
							curType++;
							foreach (var method in type.Methods) {
								if (worker.CancellationPending)
									return;
//								if (DomMethodNodeBuilder.Disassemble (rd => rd.DisassembleMethod (method)).ToUpper ().Contains (pattern)) {
//									members.Add (method);
//								}
							}
						}
					}
					Gtk.Application.Invoke (delegate {
						IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
						foreach (var kv in memberDict) {
							foreach (var member in kv.Value) {
								if (worker.CancellationPending)
									return;
								memberListStore.AppendValues ("", //iImageService.GetPixbuf (member.StockIcon, Gtk.IconSize.Menu),
								                              member.Name,
								                              member.DeclaringTypeDefinition.FullName,
								                              kv.Key.Assembly.FullName,
								                              member);
							}
						}
					}
					);
					break;
				case SearchMode.Decompiler:
					foreach (var unit in this.definitions) {
						foreach (var type in unit.UnresolvedAssembly.TopLevelTypeDefinitions) {
							if (worker.CancellationPending)
								return;
							curType++;
							foreach (var method in type.Methods) {
								if (worker.CancellationPending)
									return;
/*								if (DomMethodNodeBuilder.Decompile (domMethod, false).ToUpper ().Contains (pattern)) {
									members.Add (method);*/
							}
						}
					}
					Gtk.Application.Invoke (delegate {
						IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
						foreach (var kv in memberDict) {
							foreach (var member in kv.Value) {
								if (worker.CancellationPending)
									return;
								memberListStore.AppendValues ("", //ImageService.GetPixbuf (member.StockIcon, Gtk.IconSize.Menu),
								                              member.Name,
								                              member.DeclaringTypeDefinition.FullName,
								                              kv.Key.Assembly.FullName,
								                              member);
							}
						}
					}
					);
					break;
				case SearchMode.Type:
					var typeDict = new Dictionary<AssemblyLoader, List<IUnresolvedTypeDefinition>> ();
					foreach (var unit in this.definitions) {
						var typeList = new List<IUnresolvedTypeDefinition> ();
						foreach (var type in unit.UnresolvedAssembly.TopLevelTypeDefinitions) {
							if (worker.CancellationPending)
								return;
							if (type.FullName.ToUpper ().IndexOf (pattern) >= 0)
								typeList.Add (type);
						}
						typeDict [unit] = typeList;
					}
					Gtk.Application.Invoke (delegate {
						foreach (var kv in typeDict) {
							foreach (var type in kv.Value) {
								if (worker.CancellationPending)
									return;
								typeListStore.AppendValues (ImageService.GetPixbuf (type.GetStockIcon (), Gtk.IconSize.Menu),
								                            type.Name,
								                            type.Namespace,
								                            kv.Key.Assembly.FullName,
								                            type);
							}
						}
					});
					
					break;
				}
			} finally {
				Gtk.Application.Invoke (delegate {
					IdeApp.Workbench.StatusBar.EndProgress ();
				});
			}
		}
		
		static bool preformat = false;
		internal static string FormatText (string text)
		{
			if (preformat)
				return text;
			StringBuilder result = new StringBuilder ();
			bool wasWhitespace = false;
			foreach (char ch in text) {
				switch (ch) {
					case '\n':
					case '\r':
						break;
					case '<':
						result.Append ("&lt;");
						break;
					case '>':
						result.Append ("&gt;");
						break;
					case '&':
						result.Append ("&amp;");
						break;
					default:
						if (wasWhitespace && Char.IsWhiteSpace (ch))
							break;
						wasWhitespace = Char.IsWhiteSpace (ch);
						result.Append (ch);
						break;
				}
			}
			return result.ToString ();
		}
		
		static void OutputChilds (StringBuilder sb, XmlNode node)
		{
			foreach (XmlNode child in node.ChildNodes) {
				OutputNode (sb, child);
			}
		}
		static void OutputNode (StringBuilder sb, XmlNode node)
		{
			if (node is XmlText) {
				sb.Append (FormatText (node.InnerText));
			} else if (node is XmlElement) {
				XmlElement el = node as XmlElement;
				switch (el.Name) {
					case "block":
						switch (el.GetAttribute ("type")) {
						case "note":
							sb.AppendLine ("<i>Note:</i>");
							break;
						case "behaviors":
							sb.AppendLine ("<b>Operation</b>");
							break;
						case "overrides":
							sb.AppendLine ("<b>Note to Inheritors</b>");
							break;
						case "usage":
							sb.AppendLine ("<b>Usage</b>");
							break;
						case "default":
							sb.AppendLine ();
							break;
						default:
							sb.Append ("<b>");
							sb.Append (el.GetAttribute ("type"));
							sb.AppendLine ("</b>");
							break;
						}
						OutputChilds (sb, node);
						return;
					case "c":
						preformat = true;
						sb.Append ("<tt>");
						OutputChilds (sb, node);
						sb.Append ("</tt>");
						preformat = false;
						return;
					case "code":
						preformat = true;
						sb.Append ("<tt>");
						OutputChilds (sb, node);
						sb.Append ("</tt>");
						preformat = false;
						return;
					case "exception":
						OutputChilds (sb, node);
						return;
					case "list":
						switch (el.GetAttribute ("type")) {
						case "table": // todo: table.
						case "bullet":
							foreach (XmlNode child in node.ChildNodes) {
								sb.Append ("    <b>*</b> ");
								OutputNode (sb, child);
							}
							break;
						case "number":
							int i = 1;
							foreach (XmlNode child in node.ChildNodes) {
								sb.Append ("    <b>" + i++ +"</b> ");
								OutputNode (sb, child);
							}
							break;
						default:
							OutputChilds (sb, node);
							break;
						}
						return;
					case "para":
						OutputChilds (sb, node);
						sb.AppendLine ();
						return;
					case "paramref":
						sb.Append (el.GetAttribute ("name"));
						return;
					case "permission":
						sb.Append (el.GetAttribute ("cref"));
						return;
					case "see":
						sb.Append ("<u>");
						sb.Append (el.GetAttribute ("langword"));
						sb.Append (el.GetAttribute ("cref"));
						sb.Append (el.GetAttribute ("internal"));
						sb.Append (el.GetAttribute ("topic"));
						sb.Append ("</u>");
						return;
					case "seealso":
						sb.Append ("<u>");
						sb.Append (el.GetAttribute ("langword"));
						sb.Append (el.GetAttribute ("cref"));
						sb.Append (el.GetAttribute ("internal"));
						sb.Append (el.GetAttribute ("topic"));
						sb.Append ("</u>");
						return;
				}
			}
			
			OutputChilds (sb, node);
		}
		
		static string TransformDocumentation (XmlNode docNode)
		{ 
			// after 3 hours to try it with xsl-t I decided to do the transformation in code.
			if (docNode == null)
				return null;
			StringBuilder result = new StringBuilder ();
			XmlNode node = docNode.SelectSingleNode ("summary");
			if (node != null) {
				OutputChilds (result, node);
				result.AppendLine ();
			}
			
			XmlNodeList nodes = docNode.SelectNodes ("param");
			if (nodes != null && nodes.Count > 0) {
				result.Append ("<big><b>Parameters</b></big>");
				foreach (XmlNode paraNode in nodes) {
					result.AppendLine ();
					result.AppendLine ("  <i>" + paraNode.Attributes["name"].InnerText +  "</i>");
					result.Append ("    ");
					OutputChilds (result, paraNode);
				}
				result.AppendLine ();
			}
			
			node = docNode.SelectSingleNode ("value");
			if (node != null) {
				result.AppendLine ("<big><b>Value</b></big>");
				OutputChilds (result, node);
				result.AppendLine ();
			}
			
			node = docNode.SelectSingleNode ("returns");
			if (node != null) {
				result.AppendLine ("<big><b>Returns</b></big>");
				OutputChilds (result, node);
				result.AppendLine ();
			}
				
			node = docNode.SelectSingleNode ("remarks");
			if (node != null) {
				result.AppendLine ("<big><b>Remarks</b></big>");
				OutputChilds (result, node);
				result.AppendLine ();
			}
				
			node = docNode.SelectSingleNode ("example");
			if (node != null) {
				result.AppendLine ("<big><b>Example</b></big>");
				OutputChilds (result, node);
				result.AppendLine ();
			}
				
			node = docNode.SelectSingleNode ("seealso");
			if (node != null) {
				result.AppendLine ("<big><b>See also</b></big>");
				OutputChilds (result, node);
				result.AppendLine ();
			}
			
			return result.ToString ();
		}
		
		List<ReferenceSegment> ReferencedSegments = new List<ReferenceSegment>();
		List<UnderlineMarker> underlineMarkers = new List<UnderlineMarker> ();
		
		public void ClearReferenceSegment ()
		{
			ReferencedSegments = null;
			underlineMarkers.ForEach (m => inspectEditor.Document.RemoveMarker (m));
			underlineMarkers.Clear ();
		}
		
		public void SetReferencedSegments (List<ReferenceSegment> refs)
		{
			ReferencedSegments = refs;
			if (ReferencedSegments == null)
				return;
			foreach (var seg in refs) {
				DocumentLine line = inspectEditor.GetLineByOffset (seg.Offset);
				if (line == null)
					continue;
				// FIXME: ILSpy sometimes gives reference segments for punctuation. See http://bugzilla.xamarin.com/show_bug.cgi?id=2918
				string text = inspectEditor.GetTextAt (seg);
				if (text != null && text.Length == 1 && !(char.IsLetter (text [0]) || text [0] == '…'))
					continue;
				var marker = new UnderlineMarker (new Cairo.Color (0, 0, 1.0), 1 + seg.Offset - line.Offset, 1 + seg.EndOffset - line.Offset);
				marker.Wave = false;
				underlineMarkers.Add (marker);
				inspectEditor.Document.AddMarker (line, marker);
			}
		}

		void FillInspectLabel ()
		{
			ITreeNavigator nav = TreeView.GetSelectedNode ();
			if (nav == null)
				return;
			IAssemblyBrowserNodeBuilder builder = nav.TypeNodeBuilder as IAssemblyBrowserNodeBuilder;
			if (builder == null) {
				this.inspectEditor.Document.Text = "";
				return;
			}
			
			ClearReferenceSegment ();
			inspectEditor.Document.ClearFoldSegments ();
			switch (this.languageCombobox.Active) {
			case 0:
				inspectEditor.Options.ShowFoldMargin = false;
				this.inspectEditor.Document.MimeType = "text/x-csharp";
				this.documentationPanel.Markup = builder.GetDocumentationMarkup (nav);
				break;
			case 1:
				inspectEditor.Options.ShowFoldMargin = true;
				this.inspectEditor.Document.MimeType = "text/x-ilasm";
				SetReferencedSegments (builder.Disassemble (inspectEditor.GetTextEditorData (), nav));
				break;
			case 2:
				inspectEditor.Options.ShowFoldMargin = true;
				this.inspectEditor.Document.MimeType = "text/x-csharp";
				SetReferencedSegments (builder.Decompile (inspectEditor.GetTextEditorData (), nav, PublicApiOnly));
				break;
			default:
				inspectEditor.Options.ShowFoldMargin = false;
				this.inspectEditor.Document.Text = "Invalid combobox value: " + this.languageCombobox.Active;
				break;
			}
			this.inspectEditor.QueueDraw ();
		}
			
		void CreateOutput ()
		{
			ITreeNavigator nav = TreeView.GetSelectedNode ();
			
			if (nav != null) {
				IMember member = nav.DataItem as IMember;
				string documentation = GettextCatalog.GetString ("No documentation available.");
				if (member != null) {
					try {
						XmlNode node = member.GetMonodocDocumentation ();
						if (node != null) {
							documentation = TransformDocumentation (node) ?? documentation;
							/*
							StringWriter writer = new StringWriter ();
							XmlTextWriter w = new XmlTextWriter (writer);
							node.WriteTo (w);
							System.Console.WriteLine ("---------------------------");
							System.Console.WriteLine (writer);*/
							
						}
					} catch (Exception) {
					}
				}
//				this.documentationLabel.Markup = documentation;
/*				IAssemblyBrowserNodeBuilder builder = nav.TypeNodeBuilder as IAssemblyBrowserNodeBuilder;
				if (builder != null) {
					this.descriptionLabel.Markup  = builder.GetDescription (nav);
				} else {
					this.descriptionLabel.Markup = "";
				}*/
				
			}
			FillInspectLabel ();
		}
			
		/*int oldSize = -1;
		void VPaneExpose (object sender, Gtk.ExposeEventArgs args)
		{
			int size = this.vpaned1.Allocation.Height - 96;
			if (size == oldSize)
				return;
			this.vpaned1.Position = oldSize = size;
		}*/
		int oldSize2 = -1;
		void HPaneExpose (object sender, Gtk.ExposeEventArgs args)
		{
			int size = this.Allocation.Width;
			if (size == oldSize2)
				return;
			oldSize2 = size;
			this.hpaned1.Position = Math.Min (350, this.Allocation.Width * 2 / 3);
		}
		
		public void Open (string url, AssemblyLoader currentAssembly = null)
		{
			ITreeNavigator nav = SearchMember (url);
			if (definitions == null) // we've been disposed
				return;
			if (nav == null) {
				if (currentAssembly != null) {
					var cecilObject = loader.GetCecilObject (currentAssembly.UnresolvedAssembly);
					if (cecilObject != null) {
						foreach (var reference in cecilObject.MainModule.AssemblyReferences) {
							string fileName = currentAssembly.LookupAssembly (reference.FullName);
							if (string.IsNullOrEmpty (fileName))
								continue;
							AddReferenceByFileName (fileName, true);
							nav = SearchMember (url);
							if (nav != null)
								break;
						}
					}

				} else {
					foreach (var definition in definitions.ToArray ()) {
						var cecilObject = loader.GetCecilObject (definition.UnresolvedAssembly);
						if (cecilObject == null)
							continue;
						foreach (var assemblyNameReference in cecilObject.MainModule.AssemblyReferences) {
							AddReferenceByAssemblyName (assemblyNameReference);
						}
					}
				}
				nav = SearchMember (url);
			}
			if (nav == null) {
				LoggingService.LogError ("Can't open: " + url + " (not found).");
			}
		}
		
		public void SelectAssembly (string fileName)
		{
			AssemblyDefinition cu = null;
			foreach (var unit in definitions) {
				if (unit.UnresolvedAssembly.AssemblyName == fileName)
					cu = loader.GetCecilObject (unit.UnresolvedAssembly);
			}
			if (cu == null)
				return;
			
			ITreeNavigator nav = TreeView.GetRootNode ();
			do {
				if (nav.DataItem == cu) {
					nav.ExpandToNode ();
					nav.Selected = true;
					return;
				}
			} while (nav.MoveNext());
		}
		
		void Dispose (ITreeNavigator nav)
		{
			if (nav == null)
				return;
			IDisposable d = nav.DataItem as IDisposable;
			if (d != null) 
				d.Dispose ();
			if (nav.HasChildren ()) {
				nav.MoveToFirstChild ();
				do {
					Dispose (nav);
				} while (nav.MoveNext ());
				nav.MoveToParent ();
			}
		}
		
		protected override void OnDestroyed ()
		{
			ClearReferenceSegment ();
			if (searchBackgoundWorker != null && searchBackgoundWorker.IsBusy) {
				searchBackgoundWorker.CancelAsync ();
				searchBackgoundWorker.Dispose ();
				searchBackgoundWorker = null;
			}
			
			if (this.TreeView != null) {
				//	Dispose (TreeView.GetRootNode ());
				TreeView.Tree.CursorChanged -= HandleCursorChanged;
				this.TreeView.Clear ();
				this.TreeView = null;
			}
			
			if (definitions != null) {
				foreach (var def in definitions)
					def.Dispose ();
				definitions.Clear ();
				definitions = null;
			}
			
			ActiveMember = null;
			if (memberListStore != null) {
				memberListStore.Dispose ();
				memberListStore = null;
			}
			
			if (typeListStore != null) {
				typeListStore.Dispose ();
				typeListStore = null;
			}
			
			if (documentationPanel != null) {
				documentationPanel.Destroy ();
				documentationPanel = null;
			}
			if (inspectEditor != null) {
				inspectEditor.TextViewMargin.GetLink = null;
				inspectEditor.LinkRequest -= InspectEditorhandleLinkRequest;
				inspectEditor.Destroy ();
			}
			
			if (this.UIManager != null) {
				this.UIManager.Dispose ();
				this.UIManager = null;
			}

			this.loader = null;
			this.languageCombobox.Changed -= LanguageComboboxhandleChanged;
//			this.searchInCombobox.Changed -= SearchInComboboxhandleChanged;
//			this.searchEntry.Changed -= SearchEntryhandleChanged;
			this.searchTreeview.RowActivated -= SearchTreeviewhandleRowActivated;
			hpaned1.ExposeEvent -= HPaneExpose;
			if (NavigateBackwardAction != null) {
				this.NavigateBackwardAction.Dispose ();
				this.NavigateBackwardAction = null;
			}

			if (NavigateForwardAction != null) {
				this.NavigateForwardAction.Dispose ();
				this.NavigateForwardAction = null;
			}
			base.OnDestroyed ();
		}
		
		static AssemblyDefinition ReadAssembly (string fileName)
		{
			ReaderParameters parameters = new ReaderParameters ();
//			parameters.AssemblyResolver = new SimpleAssemblyResolver (Path.GetDirectoryName (fileName));
			using (var stream = new System.IO.MemoryStream (System.IO.File.ReadAllBytes (fileName))) {
				return AssemblyDefinition.ReadAssembly (stream, parameters);
			}
		}
		
		CecilLoader loader;
		internal CecilLoader CecilLoader {
			get {
				return loader;
			}
		} 
		
		List<AssemblyLoader> definitions = new List<AssemblyLoader> ();
		List<Project> projects = new List<Project> ();
		
		public AssemblyLoader AddReferenceByAssemblyName (AssemblyNameReference reference, bool selectReference = false)
		{
			return AddReferenceByAssemblyName (reference.Name, selectReference);
		}
		
		public AssemblyLoader AddReferenceByAssemblyName (string assemblyFullName, bool selectReference = false)
		{
			string assemblyFile = Runtime.SystemAssemblyService.DefaultAssemblyContext.GetAssemblyLocation (assemblyFullName, null);
			if (assemblyFile == null || !System.IO.File.Exists (assemblyFile)) {
				foreach (var wrapper in definitions) {
					assemblyFile = wrapper.LookupAssembly (assemblyFullName);
					if (assemblyFile != null && System.IO.File.Exists (assemblyFile))
						break;
				}
			}
			if (assemblyFile == null || !System.IO.File.Exists (assemblyFile))
				return null;
			
			return AddReferenceByFileName (assemblyFile, selectReference);
		}
		
		public AssemblyLoader AddReferenceByFileName (string fileName, bool selectReference = false)
		{
			var result = definitions.FirstOrDefault (d => d.FileName == fileName);
			if (result != null)
				return result;
			result = new AssemblyLoader (this, fileName);
			
			definitions.Add (result);
			result.LoadingTask.ContinueWith (delegate {
				Application.Invoke (delegate {
					if (definitions == null)
						return;
					ITreeBuilder builder;
					if (definitions.Count + projects.Count == 1) {
						builder = TreeView.LoadTree (result);
					} else {
						builder = TreeView.AddChild (result);
					}
					builder.Selected = builder.Expanded = selectReference;
				});
			}
			);
			return result;
		}
		
		public void AddProject (Project project, bool selectReference = false)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			if (projects.Contains (project))
				return;
			projects.Add (project);
			ITreeBuilder builder;
			if (definitions.Count + projects.Count == 1) {
				builder = TreeView.LoadTree (project);
			} else {
				builder = TreeView.AddChild (project);
			}
			builder.Selected = builder.Expanded = selectReference;
		}

		[CommandHandler (SearchCommands.FindNext)]
		public void FindNext ()
		{
			SearchAndReplaceWidget.FindNext (this.inspectEditor);
		}

		[CommandHandler (SearchCommands.FindPrevious)]
		public void FindPrevious ()
		{
			SearchAndReplaceWidget.FindPrevious (this.inspectEditor);
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void ShowSearchWidget ()
		{
			if (searchAndReplaceWidget == null) {
				popupWidgetFrame = new MonoDevelop.Components.RoundedFrame ();
				//searchAndReplaceWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (widget.TextEditor.ColorStyle.Default.BackgroundColor));
				popupWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (Style.Background (StateType.Normal)));
				popupWidgetFrame.Show ();
				
				popupWidgetFrame.Child = searchAndReplaceWidget = new SearchAndReplaceWidget (inspectEditor, popupWidgetFrame);
				searchAndReplaceWidget.Destroyed += (sender, e) => {
					DestroyFrames ();
					if (inspectEditor.IsRealized)
						inspectEditor.GrabFocus ();
				};
				searchAndReplaceWidget.UpdateSearchPattern ();
				inspectEditor.AddAnimatedWidget (popupWidgetFrame, 300, Mono.TextEditor.Theatrics.Easing.ExponentialInOut, Blocking.Downstage, inspectEditor.Allocation.Width - 400, -searchAndReplaceWidget.Allocation.Height);
				searchAndReplaceWidget.IsReplaceMode = false;
			}
			
			searchAndReplaceWidget.Focus ();
		}

		MonoDevelop.Components.RoundedFrame popupWidgetFrame;

		GotoLineNumberWidget gotoLineNumberWidget;
		SearchAndReplaceWidget searchAndReplaceWidget;
		void DestroyFrames ()
		{
			if (popupWidgetFrame != null) {
				popupWidgetFrame.Destroy ();
				popupWidgetFrame = null;
				gotoLineNumberWidget = null;
				searchAndReplaceWidget = null;
			}
		}

		[CommandHandler (SearchCommands.GotoLineNumber)]
		public void ShowGotoLineNumberWidget ()
		{
			if (gotoLineNumberWidget == null) {
				DestroyFrames ();
				popupWidgetFrame = new MonoDevelop.Components.RoundedFrame ();
				//searchAndReplaceWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (widget.TextEditor.ColorStyle.Default.BackgroundColor));
				popupWidgetFrame.SetFillColor (MonoDevelop.Components.CairoExtensions.GdkColorToCairoColor (Style.Background (StateType.Normal)));
				popupWidgetFrame.Show ();
				
				popupWidgetFrame.Child = gotoLineNumberWidget = new GotoLineNumberWidget (inspectEditor, popupWidgetFrame);
				gotoLineNumberWidget.Destroyed += (sender, e) => {
					DestroyFrames ();
					if (inspectEditor.IsRealized)
						inspectEditor.GrabFocus ();
				};
				inspectEditor.AddAnimatedWidget (popupWidgetFrame, 300, Mono.TextEditor.Theatrics.Easing.ExponentialInOut, Blocking.Downstage, inspectEditor.Allocation.Width - 400, -gotoLineNumberWidget.Allocation.Height);
			}
			
			gotoLineNumberWidget.Focus ();
		}

	
		#region NavigationHistory
		Stack<ITreeNavigator> navigationBackwardHistory = new Stack<ITreeNavigator> ();
		Stack<ITreeNavigator> navigationForwardHistory = new Stack<ITreeNavigator> ();
		ITreeNavigator currentItem = null;
		bool inNavigationOperation = false;
		void HandleCursorChanged (object sender, EventArgs e)
		{
			if (!inNavigationOperation) {
				if (currentItem != null)
					navigationBackwardHistory.Push (currentItem);
				currentItem = TreeView.GetSelectedNode ();
				ActiveMember = currentItem.DataItem as IEntity;
				navigationForwardHistory.Clear ();
			}
			notebook1.Page = 0;
			UpdateNavigationActions ();
			CreateOutput ();
		}
		
		void UpdateNavigationActions ()
		{
			if (buttonBack != null) {
				buttonBack.Sensitive = navigationBackwardHistory.Count != 0;
				buttonForeward.Sensitive = navigationForwardHistory.Count != 0;
			}
		}
		
		protected virtual void OnNavigateBackwardActionActivated (object sender, System.EventArgs e)
		{
			if (navigationBackwardHistory.Count == 0)
				return;
			inNavigationOperation = true;
			ITreeNavigator item = navigationBackwardHistory.Pop ();
			item.Selected = true;
			navigationForwardHistory.Push (currentItem);
			currentItem = item;
			inNavigationOperation = false;
			UpdateNavigationActions ();
		}
	
		protected virtual void OnNavigateForwardActionActivated (object sender, System.EventArgs e)
		{
			if (navigationForwardHistory.Count == 0)
				return;
			inNavigationOperation = true;
			ITreeNavigator item = navigationForwardHistory.Pop ();
			item.Selected = true;
			navigationBackwardHistory.Push (currentItem);
			currentItem = item;
			inNavigationOperation = false;
			UpdateNavigationActions ();
		}
		#endregion
	}
}

