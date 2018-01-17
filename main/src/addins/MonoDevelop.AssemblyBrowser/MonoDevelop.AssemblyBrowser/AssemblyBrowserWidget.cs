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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using XmlDocIdLib;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Ide.Gui.Content;
using System.IO;
using MonoDevelop.Core.Text;

namespace MonoDevelop.AssemblyBrowser
{
	enum SearchMemberState {
		TypesAndMembers,
		Types,
		Members
	}
	[System.ComponentModel.Category("MonoDevelop.AssemblyBrowser")]
	[System.ComponentModel.ToolboxItem(true)]
	partial class AssemblyBrowserWidget : Gtk.Bin
	{
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
			set {
				comboboxVisibilty.Active = value ? 0 : 1;
			}
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
					if (root != null) {
						do {
							RefreshNode (root);
						} while (root.MoveNext ());
					}
				}
			}

			public AssemblyBrowserTreeView (NodeBuilder[] builders, TreePadOption[] options) : base (builders, options)
			{
			}
		}

		static string GetLink (ReferenceSegment referencedSegment, out bool? isNotPublic)
		{
			isNotPublic = null;
			if (referencedSegment == null)
				return null;

			var td = referencedSegment.Reference as TypeDefinition;
			if (td != null) {
				isNotPublic = !td.IsPublic;
				return new XmlDocIdGenerator ().GetXmlDocPath ((TypeDefinition)referencedSegment.Reference);
			}
			var md = referencedSegment.Reference as MethodDefinition;
			if (md != null) {
				isNotPublic = !md.IsPublic;
				return new XmlDocIdGenerator ().GetXmlDocPath ((MethodDefinition)referencedSegment.Reference);
			}

			var pd = referencedSegment.Reference as PropertyDefinition;
			if (pd != null) {
				isNotPublic = (pd.GetMethod == null || !pd.GetMethod.IsPublic) &&  
					(pd.SetMethod == null || !pd.SetMethod.IsPublic);
				return new XmlDocIdGenerator ().GetXmlDocPath ((PropertyDefinition)referencedSegment.Reference);
			}

			var fd = referencedSegment.Reference as FieldDefinition;
			if (fd != null) {
				isNotPublic = !fd.IsPublic;
				return new XmlDocIdGenerator ().GetXmlDocPath ((FieldDefinition)referencedSegment.Reference);
			}

			var ed = referencedSegment.Reference as EventDefinition;
			if (ed != null) {
				return new XmlDocIdGenerator ().GetXmlDocPath ((EventDefinition)referencedSegment.Reference);
			}

			var tref = referencedSegment.Reference as MemberReference;
			if (tref != null) {
				return new XmlDocIdGenerator ().GetXmlDocPath (tref);
			}

			return referencedSegment.Reference.ToString ();
		}


		static readonly CustomEditorOptions assemblyBrowserEditorOptions = new CustomEditorOptions(DefaultSourceEditorOptions.PlainEditor) {
			TabsToSpaces = false
		};
			
		public AssemblyBrowserWidget ()
		{
			this.Build ();

			comboboxVisibilty = ComboBox.NewText ();
			comboboxVisibilty.InsertText (0, GettextCatalog.GetString ("Only public members"));
			comboboxVisibilty.InsertText (1, GettextCatalog.GetString ("All members"));
			comboboxVisibilty.Active = Math.Min (1, Math.Max (0, PropertyService.Get ("AssemblyBrowser.MemberSelection", 0)));
			comboboxVisibilty.Changed += delegate {
				TreeView.PublicApiOnly = comboboxVisibilty.Active == 0;
				PropertyService.Set ("AssemblyBrowser.MemberSelection", comboboxVisibilty.Active);
				FillInspectLabel (); 
			};

			searchentry1 = new MonoDevelop.Components.SearchEntry ();
			searchentry1.Ready = true;
			searchentry1.HasFrame = true;
			searchentry1.WidthRequest = 200;
			searchentry1.Visible = true;
			UpdateSearchEntryMessage ();
			searchentry1.InnerEntry.Changed += SearchEntryhandleChanged;

			CheckMenuItem checkMenuItem = this.searchentry1.AddFilterOption (0, GettextCatalog.GetString ("Types and Members"));
			checkMenuItem.Active = PropertyService.Get ("AssemblyBrowser.SearchMemberState", SearchMemberState.TypesAndMembers) == SearchMemberState.TypesAndMembers;
			checkMenuItem.Toggled += delegate {
				if (checkMenuItem.Active) {
					searchMode = AssemblyBrowserWidget.SearchMode.TypeAndMembers;
					CreateColumns ();
					StartSearch ();
				}
				if (checkMenuItem.Active)
					PropertyService.Set ("AssemblyBrowser.SearchMemberState", SearchMemberState.TypesAndMembers);
				UpdateSearchEntryMessage ();
			};

			CheckMenuItem checkMenuItem2 = this.searchentry1.AddFilterOption (0, GettextCatalog.GetString ("Types"));
			checkMenuItem2.Active = PropertyService.Get ("AssemblyBrowser.SearchMemberState", SearchMemberState.TypesAndMembers) == SearchMemberState.Types;
			checkMenuItem2.Toggled += delegate {
				if (checkMenuItem2.Active) {
					searchMode = AssemblyBrowserWidget.SearchMode.Type;
					CreateColumns ();
					StartSearch ();
				}
				if (checkMenuItem.Active)
					PropertyService.Set ("AssemblyBrowser.SearchMemberState", SearchMemberState.Types);
				UpdateSearchEntryMessage ();
			};
			
			CheckMenuItem checkMenuItem1 = this.searchentry1.AddFilterOption (1, GettextCatalog.GetString ("Members"));
			checkMenuItem.Active = PropertyService.Get ("AssemblyBrowser.SearchMemberState", SearchMemberState.TypesAndMembers) == SearchMemberState.Members;
			checkMenuItem1.Toggled += delegate {
				if (checkMenuItem1.Active) {
					searchMode = AssemblyBrowserWidget.SearchMode.Member;
					CreateColumns ();
					StartSearch ();
				}
				if (checkMenuItem.Active)
					PropertyService.Set ("AssemblyBrowser.SearchMemberState", SearchMemberState.Members);
				UpdateSearchEntryMessage ();

			};

			languageCombobox = Gtk.ComboBox.NewText ();
			languageCombobox.AppendText (GettextCatalog.GetString ("Summary"));
			languageCombobox.AppendText (GettextCatalog.GetString ("IL"));
			languageCombobox.AppendText (GettextCatalog.GetString ("C#"));
			languageCombobox.Active = Math.Min (2, Math.Max (0, PropertyService.Get ("AssemblyBrowser.Language", 0)));
			languageCombobox.Changed += LanguageComboboxhandleChanged;
#pragma warning disable 618
			TreeView = new AssemblyBrowserTreeView (new NodeBuilder[] { 
				new ErrorNodeBuilder (),
				new ProjectNodeBuilder (this),
				new AssemblyNodeBuilder (this),
				new ModuleReferenceNodeBuilder (),
				new AssemblyReferenceNodeBuilder (this),
				//new AssemblyReferenceFolderNodeBuilder (this),
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
			TreeView.PublicApiOnly = comboboxVisibilty.Active == 0;
			TreeView.AllowsMultipleSelection = false;
			TreeView.SelectionChanged += HandleCursorChanged;

			treeViewPlaceholder.Add (TreeView);

//			this.descriptionLabel.ModifyFont (Pango.FontDescription.FromString ("Sans 9"));
//			this.documentationLabel.ModifyFont (Pango.FontDescription.FromString ("Sans 12"));
//			this.documentationLabel.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 225));
//			this.documentationLabel.Wrap = true;
			

			inspectEditor = TextEditorFactory.CreateNewEditor ();
			inspectEditor.ContextMenuPath = "/MonoDevelop/AssemblyBrowser/EditorContextMenu";
			inspectEditor.Options = assemblyBrowserEditorOptions;

			//inspectEditor.ButtonPressEvent += HandleInspectEditorButtonPressEvent;
			
			this.inspectEditor.IsReadOnly = true;
//			this.inspectEditor.Document.SyntaxMode = new Mono.TextEditor.Highlighting.MarkupSyntaxMode ();
//			this.inspectEditor.LinkRequest += InspectEditorhandleLinkRequest;

			documentationScrolledWindow.PackStart (inspectEditor, true, true, 0);

			this.ExposeEvent += HPaneExpose;
			hpaned1 = hpaned1.ReplaceWithWidget (new HPanedThin (), true);
			hpaned1.Position = 271;

			this.notebook1.SetTabLabel (this.documentationScrolledWindow, new Label (GettextCatalog.GetString ("Documentation")));
			this.notebook1.SetTabLabel (this.searchWidget, new Label (GettextCatalog.GetString ("Search")));
			notebook1.Page = 0;
			//this.searchWidget.Visible = false;
				
			resultListStore = new Gtk.ListStore (typeof(IUnresolvedEntity));

			CreateColumns ();
//			this.searchEntry.Changed += SearchEntryhandleChanged;
			this.searchTreeview.RowActivated += SearchTreeviewhandleRowActivated;
			this.notebook1.ShowTabs = false;
			this.ShowAll ();
		}

		void UpdateSearchEntryMessage ()
		{
			switch (PropertyService.Get ("AssemblyBrowser.SearchMemberState", SearchMemberState.TypesAndMembers)) {
			case SearchMemberState.TypesAndMembers:
				searchentry1.EmptyMessage = GettextCatalog.GetString ("Search for types and members");
				break;
			case SearchMemberState.Types:
				searchentry1.EmptyMessage = GettextCatalog.GetString ("Search for types");
				break;
			case SearchMemberState.Members:
				searchentry1.EmptyMessage = GettextCatalog.GetString ("Search for members");
				break;
			default:
				throw new ArgumentOutOfRangeException ();
			}

		}

		internal void SetToolbar (DocumentToolbar toolbar)
		{
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
			EditActions.ClipboardCopy (inspectEditor);
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		protected void OnSelectAllCommand ()
		{
			EditActions.SelectAll (inspectEditor);
		}
		
		void HandleInspectEditorButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button != 3)
				return;
			var menuSet = new CommandEntrySet ();
			menuSet.AddItem (EditCommands.SelectAll);
			menuSet.AddItem (EditCommands.Copy);
			IdeApp.CommandService.ShowContextMenu (this, args.Event, menuSet, this);
		}

		void SearchTreeviewhandleRowActivated (object o, RowActivatedArgs args)
		{
			TreeIter selectedIter;
			if (searchTreeview.Selection.GetSelected (out selectedIter)) {
				var member = (IUnresolvedEntity)resultListStore.GetValue (selectedIter, 0);

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
			PropertyService.Set ("AssemblyBrowser.Language", this.languageCombobox.Active);
			FillInspectLabel ();
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
		
		ITreeNavigator SearchMember (IUnresolvedEntity member, bool expandNode = true)
		{
			return SearchMember (GetIdString (member), expandNode);
		}
			
		ITreeNavigator SearchMember (string helpUrl, bool expandNode = true)
		{
			var nav = SearchMember (TreeView.GetRootNode (), helpUrl, expandNode);
			if (nav != null)
				return nav;
			// Constructor may be a generated default without implementation.
			var ctorIdx = helpUrl.IndexOf (".#ctor", StringComparison.Ordinal);
			if (helpUrl.StartsWith ("M:", StringComparison.Ordinal) && ctorIdx > 0) {
				return SearchMember ("T" + helpUrl.Substring (1, ctorIdx - 1), expandNode);
			}
			return null;
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
				result.Append (n.Namespace);
				result.Append ('.');
				result.Append (n.Name);
				return;
			}

			if (type is IUnresolvedTypeDefinition) {
				result.Append (((IUnresolvedTypeDefinition)type).FullName);
			}

			if (type is TypeParameterReference) {
				result.Append ('`');
				result.Append (((TypeParameterReference)type).Index);
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
					if (p.IsRef || p.IsOut) {
						result.Append ("@");
					}
				}
			}
			result.Append (')');
		}

		internal string GetIdString (IUnresolvedEntity member)
		{
			var result = UnresolvedIdStringProvider.GetIdString (member);
			// Console.WriteLine (result);
			return result;
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
			int startIndex = 0;
			int endIndex = helpUrl.Length;
			if (helpUrl.Length > 2 && helpUrl [1] == ':') {
				startIndex = 2;
			}
			int idx = helpUrl.IndexOf ('~', startIndex);
			if (idx > 0)
				endIndex = idx;
			var type = nav.DataItem as IUnresolvedTypeDefinition;
			if (type != null && helpUrl.IndexOf (type.FullName, startIndex, Math.Min (endIndex - startIndex, type.FullName.Length), StringComparison.Ordinal) == -1)
				return true;
			var @namespace = nav.DataItem as Namespace;
			if (@namespace != null && helpUrl.IndexOf (@namespace.Name, startIndex, Math.Min (endIndex - startIndex, @namespace.Name.Length), StringComparison.Ordinal) == -1)
				return true;
			return false;
		}

		bool expandedMember = true;
		ITreeNavigator SearchMember (ITreeNavigator nav, string helpUrl, bool expandNode = true)
		{
			if (nav == null)
				return null;
			bool searchType = helpUrl.StartsWith ("T:", StringComparison.Ordinal);
			do {
				if (IsMatch (nav, helpUrl, searchType)) {
					inspectEditor.ClearSelection ();
					nav.ExpandToNode ();
					if (expandNode)
						nav.Expanded = true;
					nav.Selected = true;
					nav.ScrollToNode ();
					expandedMember = true;
					return nav;
				}
				if (!SkipChildren (nav, helpUrl, searchType) && nav.HasChildren ()) {
					nav.MoveToFirstChild ();
					ITreeNavigator result = SearchMember (nav, helpUrl, expandNode);
					if (result != null)
						return result;
					
					if (!nav.MoveToParent ()) {
						return null;
					}
				}
			} while (nav.MoveNext());
			return null;
		}
		
		enum SearchMode 
		{
			Type   = 0,
			Member = 1,
			TypeAndMembers = 2
		}
		
		SearchMode searchMode = SearchMode.Type;
		Gtk.ListStore resultListStore;
		
		void CreateColumns ()
		{
			foreach (TreeViewColumn column in searchTreeview.Columns) {
				searchTreeview.RemoveColumn (column);
			}
			TreeViewColumn col;
			Gtk.CellRenderer crp, crt;
			switch (searchMode) {
			case SearchMode.Member:
				col = new TreeViewColumn ();
				col.Title = GettextCatalog.GetString ("Member");
				col.FixedWidth = 400;
				col.Sizing = TreeViewColumnSizing.Fixed;
				crp = new CellRendererImage ();
				crt = new Gtk.CellRendererText ();
				col.PackStart (crp, false);
				col.PackStart (crt, true);
				col.SetCellDataFunc (crp, RenderImage);
				col.SetCellDataFunc (crt, RenderText);
				col.SortColumnId = 1;
				searchTreeview.AppendColumn (col);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Declaring Type"), crt = new Gtk.CellRendererText ());
				col.FixedWidth = 300;
				col.Sizing = TreeViewColumnSizing.Fixed;
				col.SetCellDataFunc (crt, RenderDeclaringTypeOrNamespace);
				col.SortColumnId = 2;
				col.Resizable = true;
				searchTreeview.Model = resultListStore;
				break;
			case SearchMode.TypeAndMembers:
				col = new TreeViewColumn ();
				col.Title = GettextCatalog.GetString ("Results");
				crp = new CellRendererImage ();
				crt = new Gtk.CellRendererText ();
				col.PackStart (crp, false);
				col.PackStart (crt, true);
				col.SetCellDataFunc (crp, RenderImage);
				col.SetCellDataFunc (crt, RenderText);
				col.SortColumnId = 1;

				searchTreeview.AppendColumn (col);
				col.FixedWidth = 400;
				col.Sizing = TreeViewColumnSizing.Fixed;
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Parent"), crt = new Gtk.CellRendererText ());
				col.SetCellDataFunc (crt, RenderDeclaringTypeOrNamespace);
				col.SortColumnId = 2;

				col.FixedWidth = 300;
				col.Sizing = TreeViewColumnSizing.Fixed;
				col.Resizable = true;
				searchTreeview.Model = resultListStore;
				break;
			case SearchMode.Type:
				col = new TreeViewColumn ();
				col.Title = GettextCatalog.GetString ("Type");
				crp = new CellRendererImage ();
				crt = new Gtk.CellRendererText ();
				col.PackStart (crp, false);
				col.PackStart (crt, true);
				col.SetCellDataFunc (crp, RenderImage);
				col.SetCellDataFunc (crt, RenderText);
				col.SortColumnId = 1;
				searchTreeview.AppendColumn (col);
				col.FixedWidth = 400;
				col.Sizing = TreeViewColumnSizing.Fixed;
				col.Resizable = true;

				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Namespace"), crt = new Gtk.CellRendererText ());
				col.SetCellDataFunc (crt, RenderDeclaringTypeOrNamespace);
				col.SortColumnId = 2;
				col.FixedWidth = 300;
				col.Sizing = TreeViewColumnSizing.Fixed;
				col.Resizable = true;
				searchTreeview.Model = resultListStore;
				break;
			}
		}

		void RenderDeclaringTypeOrNamespace (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			var ct = (Gtk.CellRendererText)cell;
			var entity = tree_model.GetValue (iter, 0) as IUnresolvedEntity;
			if (entity != null) {
				if (entity.DeclaringTypeDefinition != null) {
					ct.Text = entity.DeclaringTypeDefinition.FullName;
					return;
				}
				ct.Text = entity.Namespace;
			}
		}

		void RenderText (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			var ct = (Gtk.CellRendererText)cell;
			var entity = tree_model.GetValue (iter, 0) as IUnresolvedEntity;
			if (entity != null)
				ct.Text = entity.Name;
		}

		void RenderImage (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			var ct = (CellRendererImage)cell;
			var entity = tree_model.GetValue (iter, 0) as IUnresolvedEntity;
			if (entity != null)
				ct.Image = ImageService.GetIcon (entity.GetStockIcon (), Gtk.IconSize.Menu);
		}

		CancellationTokenSource searchTokenSource = new CancellationTokenSource ();

		public void StartSearch ()
		{
			string query = searchentry1.Query;
			searchTokenSource.Cancel ();
			searchTokenSource = new CancellationTokenSource ();

			if (string.IsNullOrEmpty (query)) {
				notebook1.Page = 0;
				return;
			}
			
			this.notebook1.Page = 1;
			
			switch (searchMode) {
			case SearchMode.Member:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching member..."));
				break;
			case SearchMode.Type:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching type..."));
				break;
				case SearchMode.TypeAndMembers:
		       	IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching types and members..."));
				break;
			}
			resultListStore.Clear ();
			var token = searchTokenSource.Token;
			var publicOnly = PublicApiOnly;
			var defArray = definitions.ToArray ();
			Task.Run (delegate {
				var memberList = SearchDoWork (defArray, query, publicOnly, token);
				if (memberList == null || token.IsCancellationRequested)
					return;
				Runtime.RunInMainThread (delegate {
					if (token.IsCancellationRequested)
						return;
					var updater = new Updater (this, memberList, token);
					updater.Update ();
				});
			});
		}

		class Updater
		{
			readonly AssemblyBrowserWidget assemblyBrowserWidget;
			readonly List<IUnresolvedEntity> memberList;
			readonly CancellationToken token;
			int i = 0;

			public Updater (AssemblyBrowserWidget assemblyBrowserWidget, List<IUnresolvedEntity> memberList, CancellationToken token)
			{
				this.assemblyBrowserWidget = assemblyBrowserWidget;
				this.memberList = memberList;
				this.token = token;
			}

			public void Update ()
			{
				GLib.Idle.Add (IdleHandler);
			}

			bool IdleHandler ()
			{
				if (token.IsCancellationRequested || i >= memberList.Count) {
					IdeApp.Workbench.StatusBar.EndProgress ();
					IdeApp.Workbench.StatusBar.ShowReady ();
					return false;
				}

				assemblyBrowserWidget.searchTreeview.FreezeChildNotify ();
				for (int j = 0; j < 100 && i < memberList.Count; j++) {
					assemblyBrowserWidget.resultListStore.AppendValues (memberList [i++]);
				}
				assemblyBrowserWidget.searchTreeview.ThawChildNotify ();
				return true;
			}
		}

		List<IUnresolvedEntity> SearchDoWork (AssemblyLoader[] definitions, string pattern, bool publicOnly, CancellationToken cancellationToken)
		{
			var result = new List<IUnresolvedEntity> ();
			int types = 0, curType = 0;
			foreach (var unit in definitions) {
				types += unit.UnresolvedAssembly.TopLevelTypeDefinitions.Count ();
			}
			var matcher = StringMatcher.GetMatcher (pattern, true);

			switch (searchMode) {
			case SearchMode.Member:
				foreach (var unit in definitions) {
					foreach (var type in unit.UnresolvedAssembly.TopLevelTypeDefinitions) {
						if (cancellationToken.IsCancellationRequested)
							return null;
						if (!type.IsPublic && publicOnly)
							continue;
						curType++;
						foreach (var member in type.Members) {
							if (cancellationToken.IsCancellationRequested)
								return null;
							if (!member.IsPublic && publicOnly)
								continue;
							if (matcher.IsMatch (member.Name)) {
								result.Add (member);
							}
						}
					}
				}

				break;
			case SearchMode.Type:
				foreach (var unit in definitions) {
					var typeList = new List<IUnresolvedTypeDefinition> ();
					foreach (var type in unit.UnresolvedAssembly.TopLevelTypeDefinitions) {
						if (cancellationToken.IsCancellationRequested)
							return null;
						if (!type.IsPublic && publicOnly)
							continue;
						if (matcher.IsMatch (type.FullName))
							result.Add (type);
					}
				}
				break;
			case SearchMode.TypeAndMembers:
				foreach (var unit in definitions) {
					foreach (var type in unit.UnresolvedAssembly.TopLevelTypeDefinitions) {
						if (cancellationToken.IsCancellationRequested)
							return null;
						if (!type.IsPublic && publicOnly)
							continue;
						var parent = type.FullName;
						if (matcher.IsMatch (parent))
							result.Add (type);

						foreach (var member in type.Members) {
							if (cancellationToken.IsCancellationRequested)
								return null;
							if (!member.IsPublic && publicOnly)
								continue;
							if (matcher.IsMatch (member.Name))
								result.Add (member);
						}

					}
				}
				break;
			}

			return result;
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
								sb.Append ("    <b>").Append (i++).Append ("</b> ");
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
					result.Append ("  <i>").Append (paraNode.Attributes["name"].InnerText).AppendLine ("</i>");
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
		List<ITextSegmentMarker> underlineMarkers = new List<ITextSegmentMarker> ();
		
		public void ClearReferenceSegment ()
		{
			ReferencedSegments = null;
			underlineMarkers.ForEach (m => inspectEditor.RemoveMarker (m));
			underlineMarkers.Clear ();
		}
		
		internal void SetReferencedSegments (List<ReferenceSegment> refs)
		{
			ReferencedSegments = refs;
			if (ReferencedSegments == null)
				return;
			foreach (var _seg in refs) {
				var seg = _seg;
				var line = inspectEditor.GetLineByOffset (seg.Offset);
				if (line == null)
					continue;
				// FIXME: ILSpy sometimes gives reference segments for punctuation. See http://bugzilla.xamarin.com/show_bug.cgi?id=2918
				string text = inspectEditor.GetTextAt (seg.Offset, seg.Length);
				if (text != null && text.Length == 1 && !(char.IsLetter (text [0]) || text [0] == '…'))
					continue;
				var marker = TextMarkerFactory.CreateLinkMarker (inspectEditor, seg.Offset, seg.Length, delegate (LinkRequest request) {
					bool? isNotPublic;
					var link = GetLink (seg, out isNotPublic);
					if (link == null)
						return;
					if (isNotPublic.HasValue) {
						if (isNotPublic.Value) {
							PublicApiOnly = false;
						}
					} else {
						// unable to determine if the member is public or not (in case of member references) -> try to search
						var nav = SearchMember (link, false);
						if (nav == null)
							PublicApiOnly = false;
					}
					var loader = (AssemblyLoader)this.TreeView.GetSelectedNode ().GetParentDataItem (typeof(AssemblyLoader), true);
					// args.Button == 2 || (args.Button == 1 && (args.ModifierState & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)
					if (request == LinkRequest.RequestNewView) {
						AssemblyBrowserViewContent assemblyBrowserView = new AssemblyBrowserViewContent ();
						foreach (var cu in definitions) {
							assemblyBrowserView.Load (cu.UnresolvedAssembly.AssemblyName);
						}
						IdeApp.Workbench.OpenDocument (assemblyBrowserView, true);
						((AssemblyBrowserWidget)assemblyBrowserView.Control).Open (link);
					} else {
						this.Open (link, loader);
					}
				});
				marker.OnlyShowLinkOnHover = true;
				underlineMarkers.Add (marker);
				inspectEditor.AddMarker (marker);
			}
		}

		void FillInspectLabel ()
		{
			ITreeNavigator nav = TreeView.GetSelectedNode ();
			if (nav == null)
				return;
			IAssemblyBrowserNodeBuilder builder = nav.TypeNodeBuilder as IAssemblyBrowserNodeBuilder;
			if (builder == null) {
				this.inspectEditor.Text = "";
				return;
			}
			
			ClearReferenceSegment ();
			inspectEditor.SetFoldings (Enumerable.Empty<IFoldSegment> ());
			switch (this.languageCombobox.Active) {
			case 0:
				inspectEditor.Options = assemblyBrowserEditorOptions;
				this.inspectEditor.MimeType = "text/x-csharp";
				SetReferencedSegments (builder.Decompile (inspectEditor, nav, new DecompileFlags { PublicOnly = PublicApiOnly, MethodBodies = false }));
				break;
			case 1:
				inspectEditor.Options = assemblyBrowserEditorOptions;
				this.inspectEditor.MimeType = "text/x-ilasm";
				SetReferencedSegments (builder.Disassemble (inspectEditor, nav));
				break;
			case 2:
				inspectEditor.Options = assemblyBrowserEditorOptions;
				this.inspectEditor.MimeType = "text/x-csharp";
				SetReferencedSegments (builder.Decompile (inspectEditor, nav, new DecompileFlags { PublicOnly = PublicApiOnly, MethodBodies = true }));
				break;
			default:
				inspectEditor.Options = assemblyBrowserEditorOptions;
				this.inspectEditor.Text = "Invalid combobox value: " + this.languageCombobox.Active;
				break;
			}
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
		
		internal void Open (string url, AssemblyLoader currentAssembly = null, bool expandNode = true)
		{
			Task.WhenAll (this.definitions.Select (d => d.LoadingTask).ToArray ()).ContinueWith (d => {
				Application.Invoke ((o, args) => {
					suspendNavigation = false;
					ITreeNavigator nav = SearchMember (url, expandNode);
					if (definitions == null) // we've been disposed
						return;
					if (nav != null)
						return;
					try {
						if (currentAssembly != null) {
							OpenFromAssembly (url, currentAssembly);
						} else {
							OpenFromAssemblyNames (url);
						}
					} catch (Exception e) {
						LoggingService.LogError ("Error while opening the assembly browser with id:" + url, e);
					}
				});
			});
		}

		void OpenFromAssembly (string url, AssemblyLoader currentAssembly, bool expandNode = true)
		{
			var cecilObject = currentAssembly.CecilLoader.GetCecilObject (currentAssembly.UnresolvedAssembly);
			if (cecilObject == null)
				return;

			int i = 0;
			System.Action loadNext = null;
			var references = cecilObject.MainModule.AssemblyReferences;
			loadNext = () => {
				var reference = references [i];
				string fileName = currentAssembly.LookupAssembly (reference.FullName);
				if (string.IsNullOrEmpty (fileName)) {
					LoggingService.LogWarning ("Assembly browser: Can't find assembly: " + reference.FullName + ".");
					if (++i == references.Count)
						LoggingService.LogError ("Assembly browser: Can't find: " + url + ".");
					else
						loadNext ();
					return;
				}
				var result = AddReferenceByFileName (fileName);
				if (result == null)
					return;
				result.LoadingTask.ContinueWith (t2 => {
					if (definitions == null) // disposed
						return;
					Application.Invoke ((o, args) => {
						var nav = SearchMember (url, expandNode);
						if (nav == null) {
							if (++i == references.Count)
								LoggingService.LogError ("Assembly browser: Can't find: " + url + ".");
							else
								loadNext ();
						}
					});
				}, TaskScheduler.Current);
			};
		}

		void OpenFromAssemblyNames (string url)
		{
			var tasks = new List<Task> ();
			foreach (var definition in definitions.ToArray ()) {
				var cecilObject = definition.CecilLoader.GetCecilObject (definition.UnresolvedAssembly);
				if (cecilObject == null) {
					LoggingService.LogWarning ("Assembly browser: Can't find assembly: " + definition.UnresolvedAssembly.FullAssemblyName + ".");
					continue;
				}
				foreach (var assemblyNameReference in cecilObject.MainModule.AssemblyReferences) {
					var result = AddReferenceByAssemblyName (assemblyNameReference);
					if (result == null) {
						LoggingService.LogWarning ("Assembly browser: Can't find assembly: " + assemblyNameReference.FullName + ".");
					} else {
						tasks.Add (result.LoadingTask);
					}
				}
			}
			if (tasks.Count == 0) {
				var nav = SearchMember (url);
				if (nav == null) {
					LoggingService.LogError ("Assembly browser: Can't find: " + url + ".");
				}
				return;
			};
			Task.Factory.ContinueWhenAll (tasks.ToArray (), tarr => {
				var exceptions = tarr.Where (t => t.IsFaulted).Select (t => t.Exception).ToArray ();
				if (exceptions != null) {
					var ex = new AggregateException (exceptions).Flatten ();
					if (ex.InnerExceptions.Count > 0) {
						foreach (var inner in ex.InnerExceptions) {
							LoggingService.LogError ("Error while loading assembly in the browser.", inner);
						}
						throw ex;
					}
				}
				if (definitions == null) // disposed
					return;
				Application.Invoke ((o, args) => {
					var nav = SearchMember (url);
					if (nav == null) {
						LoggingService.LogError ("Assembly browser: Can't find: " + url + ".");
					}
				});
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current);
		}
		
		internal void SelectAssembly (AssemblyLoader loader)
		{
			AssemblyDefinition cu = loader.CecilLoader.GetCecilObject (loader.UnresolvedAssembly);
			Application.Invoke ((o, args) => {
				ITreeNavigator nav = TreeView.GetRootNode ();
				if (nav == null)
					return;

				if (expandedMember) {
					expandedMember = false;
					return;
				}

				do {
					if (nav.DataItem == cu || (nav.DataItem as AssemblyLoader)?.Assembly == cu) {
						nav.ExpandToNode ();
						nav.Selected = true;
						nav.ScrollToNode ();
						return;
					}
				} while (nav.MoveNext ());
			});
		}
		
		void Dispose<T> (ITreeNavigator nav) where T:class, IDisposable
		{
			if (nav == null)
				return;
			T d = nav.DataItem as T;
			if (d != null) 
				d.Dispose ();
			if (nav.HasChildren ()) {
				nav.MoveToFirstChild ();
				do {
					Dispose<T> (nav);
				} while (nav.MoveNext ());
				nav.MoveToParent ();
			}
		}

		void DisposeAssemblyDefinitions (ITreeNavigator nav)
		{
			// Dispose top level nodes
			if (nav == null || !nav.HasChildren ())
				return;
			
			nav.MoveToFirstChild ();
			do {
				if (nav.DataItem is AssemblyDefinition d)
					d.Dispose ();
			} while (nav.MoveNext ());
			nav.MoveToParent ();
		}
		
		protected override void OnDestroyed ()
		{
			ClearReferenceSegment ();
			searchTokenSource.Cancel ();

			if (this.TreeView != null) {
				//	Dispose<IDisposable> (TreeView.GetRootNode ());
				DisposeAssemblyDefinitions (TreeView.GetRootNode ());
				TreeView.SelectionChanged -= HandleCursorChanged;
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
			resultListStore = null;

			if (documentationPanel != null) {
				documentationPanel.Destroy ();
				documentationPanel = null;
			}
//			if (inspectEditor != null) {
//				inspectEditor.TextViewMargin.GetLink = null;
//				inspectEditor.LinkRequest -= InspectEditorhandleLinkRequest;
//				inspectEditor.Destroy ();
//			}
			
			if (this.UIManager != null) {
				this.UIManager.Dispose ();
				this.UIManager = null;
			}

			this.languageCombobox.Changed -= LanguageComboboxhandleChanged;
//			this.searchInCombobox.Changed -= SearchInComboboxhandleChanged;
//			this.searchEntry.Changed -= SearchEntryhandleChanged;
			this.searchTreeview.RowActivated -= SearchTreeviewhandleRowActivated;
			hpaned1.ExposeEvent -= HPaneExpose;
			base.OnDestroyed ();
		}
		

		List<AssemblyLoader> definitions = new List<AssemblyLoader> ();
		List<Project> projects = new List<Project> ();
		
		internal AssemblyLoader AddReferenceByAssemblyName (AssemblyNameReference reference)
		{
			return AddReferenceByAssemblyName (reference.Name);
		}
		
		internal AssemblyLoader AddReferenceByAssemblyName (string assemblyFullName)
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
			
			return AddReferenceByFileName (assemblyFile);
		}
		
		internal AssemblyLoader AddReferenceByFileName (string fileName)
		{
			var result = definitions.FirstOrDefault (d => d.FileName == fileName);
			if (result != null) {
//				// Select the result.
//				if (selectReference) {
//					ITreeNavigator navigator = TreeView.GetNodeAtObject (result);
//					if (navigator != null) {
//						navigator.Selected = true;
//					} else {
//						LoggingService.LogWarning (result + " could not be found.");
//					}
//				}

				return result;
			}
			if (!File.Exists (fileName))
				return null;
			result = new AssemblyLoader (this, fileName);
			
			definitions.Add (result);
			result.LoadingTask = result.LoadingTask.ContinueWith (task => {
				Application.Invoke ((o, args) => {
					if (definitions == null)
						return;
					try {
						ITreeBuilder builder;
						if (definitions.Count + projects.Count == 1) {
							builder = TreeView.LoadTree (result);
						} else {
							builder = TreeView.AddChild (result);
						}
						if (TreeView.GetSelectedNode () == null)
							builder.Selected = builder.Expanded = true;
					} catch (Exception e) {
						LoggingService.LogError ("Error while adding assembly to the assembly list", e);
					}
				});
				return task.Result;
			}
			                                                     );
			return result;
		}
		
		public void AddProject (Project project, bool selectReference = true)
		{
			if (project == null)
				throw new ArgumentNullException ("project");

			if (projects.Contains (project)) {
				// Select the project.
				if (selectReference) {
					ITreeNavigator navigator = TreeView.GetNodeAtObject (project);

					if (navigator != null)
						navigator.Selected = true;
				}

				return;
			}
			projects.Add (project);
			ITreeBuilder builder;
			if (definitions.Count + projects.Count == 1) {
				builder = TreeView.LoadTree (project);
			} else {
				builder = TreeView.AddChild (project);
			}
			builder.Selected = builder.Expanded = selectReference;
		}

		//MonoDevelop.Components.RoundedFrame popupWidgetFrame;

		#region NavigationHistory
		internal bool suspendNavigation;
		void HandleCursorChanged (object sender, EventArgs e)
		{
			if (!suspendNavigation) {
				var selectedEntity = TreeView.GetSelectedNode ()?.DataItem as IUnresolvedEntity;
				if (selectedEntity != null)
					NavigationHistoryService.LogActiveDocument ();
			}
			notebook1.Page = 0;
			CreateOutput ();
		}

		public NavigationPoint BuildNavigationPoint ()
		{
			var node = TreeView.GetSelectedNode ();
			var selectedEntity = node?.DataItem as IUnresolvedEntity;
			AssemblyLoader loader = null;
			if (selectedEntity != null) {
				loader = (AssemblyLoader)this.TreeView.GetSelectedNode ().GetParentDataItem (typeof (AssemblyLoader), true);
				return new AssemblyBrowserNavigationPoint (definitions, loader, GetIdString (selectedEntity));
			}
			loader = node?.DataItem as AssemblyLoader;
			if (loader != null)
				return new AssemblyBrowserNavigationPoint (definitions, loader, null);
			return null;
		}
		#endregion

		internal void EnsureDefinitionsLoaded (List<AssemblyLoader> definitions)
		{
			if (definitions == null)
				throw new ArgumentNullException (nameof (definitions));
			foreach (var def in definitions) {
				if (!this.definitions.Contains (def)) {
					this.definitions.Add (def);
					Application.Invoke ((o, args) => {
						if (definitions.Count + projects.Count == 1) {
							TreeView.LoadTree (def.LoadingTask.Result);
						} else {
							TreeView.AddChild (def.LoadingTask.Result);
						}
					});
				}
			}
		}
	}
}

