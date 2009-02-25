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
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Xsl;

using Gtk;
using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Components;


namespace MonoDevelop.AssemblyBrowser
{
	[System.ComponentModel.Category("MonoDevelop.AssemblyBrowser")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class AssemblyBrowserWidget : Gtk.Bin
	{
		ExtensibleTreeView treeView;
		
		public ExtensibleTreeView TreeView {
			get {
				return treeView;
			}
		}
		Mono.TextEditor.TextEditor inspectEditor = new Mono.TextEditor.TextEditor ();
		public AssemblyBrowserWidget ()
		{
			this.Build();
			treeView = new ExtensibleTreeView (new NodeBuilder[] { 
				new ErrorNodeBuilder (),
				new AssemblyNodeBuilder (),
				new ModuleReferenceNodeBuilder (),
				new ModuleDefinitionNodeBuilder (),
				new ReferenceFolderNodeBuilder (this),
				new ResourceFolderNodeBuilder (),
				new ResourceNodeBuilder (),
				new NamespaceBuilder (),
				new DomTypeNodeBuilder (/*this*/),
				new DomMethodNodeBuilder (),
				new DomFieldNodeBuilder (),
				new DomEventNodeBuilder (),
				new DomPropertyNodeBuilder (),
				new BaseTypeFolderNodeBuilder (),
				new DomReturnTypeNodeBuilder (),
				new ReferenceNodeBuilder (this),
				}, new TreePadOption [] {
					new TreePadOption ("PublicApiOnly", GettextCatalog.GetString ("Show public members only"), PropertyService.Get ("AssemblyBrowser.ShowPublicOnly", true)),
				});
			treeView.Tree.Selection.Mode = Gtk.SelectionMode.Single;
			treeView.Tree.CursorChanged += HandleCursorChanged;
				
			scrolledwindow2.AddWithViewport (treeView);
			scrolledwindow2.ShowAll ();
			
//			this.descriptionLabel.ModifyFont (Pango.FontDescription.FromString ("Sans 9"));
			this.documentationLabel.ModifyFont (Pango.FontDescription.FromString ("Sans 12"));
			this.documentationLabel.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 225));
			this.documentationLabel.Wrap = true;
			
			Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions ();
			options.FontName = "Monospace 10";
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowLineNumberMargin = false;
			options.ShowSpaces = false;
			options.ShowTabs = false;
			options.HighlightCaretLine = true;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			this.inspectEditor.Options = options;
			
			PropertyService.PropertyChanged += HandlePropertyChanged;
			this.inspectEditor.Document.ReadOnly = true;
			this.inspectEditor.Document.SyntaxMode = new Mono.TextEditor.Highlighting.MarkupSyntaxMode ();
			this.inspectEditor.LinkRequest += delegate (object sender, Mono.TextEditor.LinkEventArgs args) {
				if (args.Button == 2 || (args.Button == 1 && (args.ModifierState & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)) {
					AssemblyBrowserViewContent assemblyBrowserView = new AssemblyBrowserViewContent ();
					foreach (DomCecilCompilationUnit cu in definitions) {
						assemblyBrowserView.Load (cu.FileName);
					}
					IdeApp.Workbench.OpenDocument (assemblyBrowserView, true);
					((AssemblyBrowserWidget)assemblyBrowserView.Control).Open (args.Link);
				} else {
					this.Open (args.Link);
				}
			};
			this.scrolledwindow3.Child = inspectEditor;
			this.scrolledwindow3.ShowAll ();
			
//			this.inspectLabel.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 250));
			
//			this.vpaned1.ExposeEvent += VPaneExpose;
			this.hpaned1.ExposeEvent += HPaneExpose;
			this.notebook1.SwitchPage += delegate {
				// Hack for the switch page select all bug.
//				this.inspectLabel.Selectable = false;
			};
			this.notebook1.GetNthPage (0).Hide ();
			treeView.Tree.CursorChanged += delegate {
				CreateOutput ();
			};
			this.languageCombobox.AppendText (GettextCatalog.GetString ("IL"));
			this.languageCombobox.AppendText (GettextCatalog.GetString ("C#"));
			this.languageCombobox.Active = PropertyService.Get ("AssemblyBrowser.InspectLanguage", 0);
			this.languageCombobox.Changed += delegate {
				PropertyService.Set ("AssemblyBrowser.InspectLanguage", this.languageCombobox.Active);
				FillInspectLabel ();
			};
			
			this.searchInCombobox.AppendText (GettextCatalog.GetString ("Types"));
			this.searchInCombobox.AppendText (GettextCatalog.GetString ("Members"));
			this.searchInCombobox.AppendText (GettextCatalog.GetString ("Disassembler"));
			this.searchInCombobox.AppendText (GettextCatalog.GetString ("Decompiler"));
			this.searchInCombobox.Active = 0;
			this.searchInCombobox.Changed += delegate {
				this.searchMode = (SearchMode)this.searchInCombobox.Active;
				CreateColumns ();
				StartSearch ();
			};
				
			this.notebook1.SetTabLabel (this.documentationScrolledWindow, new Label (GettextCatalog.GetString ("Documentation")));
			this.notebook1.SetTabLabel (this.vboxInspect, new Label (GettextCatalog.GetString ("Inspect")));
			this.notebook1.SetTabLabel (this.searchWidget, new Label (GettextCatalog.GetString ("Search")));
			//this.searchWidget.Visible = false;
				
			typeListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), // type image
			                                   typeof (string),     // name
			                                   typeof (string),     // namespace
			                                   typeof (string),     // assembly
				                               typeof (IMember)
			                                  );
			
			memberListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), // member image
			                                   typeof (string),     // name
			                                   typeof (string),     // Declaring type full name
			                                   typeof (string),     // assembly
				                               typeof (IMember)
			                                  );
			CreateColumns ();
			this.searchEntry.Changed += delegate {
				StartSearch ();
			};
			this.searchTreeview.RowActivated += delegate {
				Gtk.TreeIter selectedIter;
				if (searchTreeview.Selection.GetSelected (out selectedIter)) {
					MonoDevelop.Projects.Dom.IMember member = (MonoDevelop.Projects.Dom.IMember)(searchMode != SearchMode.Type ? memberListStore.GetValue (selectedIter, 4) : typeListStore.GetValue (selectedIter, 4));
					ITreeNavigator nav = SearchMember (member);
					if (nav != null) {
						nav.ExpandToNode ();
						nav.Selected = true;
					}
					if (searchMode == SearchMode.Disassembler) {
						this.notebook1.Page = 0;
//						int idx = DomMethodNodeBuilder.Disassemble ((DomCecilMethod)member, false).ToUpper ().IndexOf (searchEntry.Text.ToUpper ());
//						this.inspectLabel.Selectable = true;
//						this.inspectLabel.SelectRegion (idx, idx + searchEntry.Text.Length);
					}
					if (searchMode == SearchMode.Decompiler) {
						this.notebook1.Page = 1;
//						int idx = DomMethodNodeBuilder.Decompile ((DomCecilMethod)member, false).ToUpper ().IndexOf (searchEntry.Text.ToUpper ());
//						this.inspectLabel.Selectable = true;
//						this.inspectLabel.SelectRegion (idx, idx + searchEntry.Text.Length);
					}
				}
			};
			
			this.Realized += delegate {
				treeView.GrabFocus ();
			};
		}
		
		public override void Destroy ()
		{
			if (memberListStore != null) {
				memberListStore.Dispose ();
				memberListStore = null;
			}
			
			if (typeListStore != null) {
				typeListStore.Dispose ();
				typeListStore = null;
			}
			
			PropertyService.PropertyChanged -= HandlePropertyChanged;
			base.Destroy ();
		}

		void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "ColorScheme")
				((Mono.TextEditor.TextEditorOptions)this.inspectEditor.Options).ColorScheme = PropertyService.Get ("ColorScheme", "Default");
		}
		
		ITreeNavigator SearchMember (IMember member)
		{
			return SearchMember (member.HelpUrl);
		}
			
		ITreeNavigator SearchMember (string helpUrl)
		{
			return SearchMember (treeView.GetRootNode (), helpUrl);
		}
		
		static bool IsMatch (ITreeNavigator nav, string helpUrl)
		{
			IMember member = nav.DataItem as IMember;
			return member != null && member.HelpUrl == helpUrl;
		}
			
		static bool SkipChildren (ITreeNavigator nav, string helpUrl)
		{
			string strippedUrl = helpUrl;
			int idx = strippedUrl.IndexOf ('~');
			if (idx > 0) 
				strippedUrl = strippedUrl.Substring (0, idx);
			
			if (nav.DataItem is IType && !strippedUrl.Contains ((nav.DataItem as IType).FullName)) 
				return true;
			if (nav.DataItem is Namespace && !strippedUrl.Contains (((Namespace)nav.DataItem).Name))
				return true;
			return false;
		}
		
		ITreeNavigator SearchMember (ITreeNavigator nav, string helpUrl)
		{
			do {
				if (IsMatch (nav, helpUrl))
					return nav;
				if (!SkipChildren (nav, helpUrl) && nav.HasChildren ()) {
					nav.MoveToFirstChild ();
					ITreeNavigator result = SearchMember (nav, helpUrl);
					if (result != null)
						return result;
					nav.MoveToParent ();
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
		Thread searchThread = null;
		public void StartSearch ()
		{
			if (searchThread != null)
				searchThread.Abort ();
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
			
			searchThread = new Thread (StartSearchThread);
			searchThread.IsBackground = true;
			searchThread.Priority = ThreadPriority.Lowest;
			searchThread.Start ();
		}
	
		void StartSearchThread ()
		{
			try {
				string pattern = searchEntry.Text.ToUpper ();
				int types = 0, curType = 0;
				foreach (DomCecilCompilationUnit unit in this.definitions) {
					types += unit.Types.Count;
				}
				List<IMember> members = new List<IMember> ();
				switch (searchMode) {
				case SearchMode.Member:
					foreach (DomCecilCompilationUnit unit in this.definitions) {
						foreach (IType type in unit.Types) {
							curType++;
							members.Clear ();
							foreach (IMember member in type.Members) {
								if (member.Name.ToUpper ().Contains (pattern)) {
									members.Add (member);
								}
							}
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
								foreach (MonoDevelop.Projects.Dom.IMember member in members) {
									memberListStore.AppendValues (MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (member.StockIcon, Gtk.IconSize.Menu),
									                              member.Name,
									                              type.FullName,
									                              unit.AssemblyDefinition.Name.FullName,
									                              member);
								}
							});
						}
					}
					break;
				case SearchMode.Disassembler:
					IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching string in disassembled code..."));
					foreach (DomCecilCompilationUnit unit in this.definitions) {
						foreach (IType type in unit.Types) {
							curType++;
							members.Clear ();
							foreach (IMethod method in type.Methods) {
								DomCecilMethod domMethod = method as DomCecilMethod;
								if (domMethod == null)
									continue;
								if (DomMethodNodeBuilder.Disassemble (domMethod, false).ToUpper ().Contains (pattern)) {
									members.Add (method);
								}
							}
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
								foreach (MonoDevelop.Projects.Dom.IMember member in members) {
									memberListStore.AppendValues (MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (member.StockIcon, Gtk.IconSize.Menu),
									                              member.Name,
									                              type.FullName,
									                              unit.AssemblyDefinition.Name.FullName,
									                              member);
								}
							});
						}
					}
					break;
				case SearchMode.Decompiler:
					foreach (DomCecilCompilationUnit unit in this.definitions) {
						foreach (IType type in unit.Types) {
							curType++;
							members.Clear ();
							foreach (IMethod method in type.Methods) {
								DomCecilMethod domMethod = method as DomCecilMethod;
								if (domMethod == null)
									continue;
								if (DomMethodNodeBuilder.Decompile (domMethod, false).ToUpper ().Contains (pattern)) {
									members.Add (method);
								}
							}
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
								foreach (MonoDevelop.Projects.Dom.IMember member in members) {
									memberListStore.AppendValues (MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (member.StockIcon, Gtk.IconSize.Menu),
									                              member.Name,
									                              type.FullName,
									                              unit.AssemblyDefinition.Name.FullName,
									                              member);
								}
							});
						}
					}
					break;
				case SearchMode.Type:
					foreach (DomCecilCompilationUnit unit in this.definitions) {
						foreach (IType type in unit.Types) {
							curType++;
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
								if (type.FullName.ToUpper ().IndexOf (pattern) >= 0) {
									typeListStore.AppendValues (MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (type.StockIcon, Gtk.IconSize.Menu),
									                            type.Name,
									                            type.Namespace,
									                            unit.AssemblyDefinition.Name.FullName,
									                            type);
								}
							});
						}
					}
					break;
				}
			} finally {
				DispatchService.GuiSyncDispatch (delegate {
					MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.EndProgress ();
				});
				searchThread = null;
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
		
		void FillInspectLabel ()
		{
			ITreeNavigator nav = treeView.GetSelectedNode ();
			if (nav == null)
				return;
			IAssemblyBrowserNodeBuilder builder = nav.TypeNodeBuilder as IAssemblyBrowserNodeBuilder;
			if (builder == null) {
				this.inspectEditor.Document.Text = "";
				return;
			}
			//this.inspectLabel.Selectable = false;
			switch (this.languageCombobox.Active) {
			case 0:
				this.inspectEditor.Document.Text = builder.GetDisassembly (nav);
				break;
			case 1:
				this.inspectEditor.Document.Text = builder.GetDecompiledCode (nav);
				break;
			default:
				this.inspectEditor.Document.Text = "Invalid combobox value: " + this.languageCombobox.Active;
				break;
			}
			this.inspectEditor.QueueDraw ();
//			this.inspectLabel.Selectable = true;
		}
			
		void CreateOutput ()
		{
			ITreeNavigator nav = treeView.GetSelectedNode ();
			
			if (nav != null) {
				IMember member = nav.DataItem as IMember;
				string documentation = GettextCatalog.GetString ("No documentation available.");
				if (member != null) {
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
				}
				this.documentationLabel.Markup = documentation;
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
			
		public void Open (string url)
		{
			GLib.Timeout.Add (100, delegate {
				ITreeNavigator nav = SearchMember (url);
				if (nav == null) {
					foreach (DomCecilCompilationUnit definition in definitions.ToArray ()) {
						foreach (AssemblyNameReference assemblyNameReference in definition.AssemblyDefinition.MainModule.AssemblyReferences) {
							string assemblyFile = Runtime.SystemAssemblyService.GetAssemblyLocation (assemblyNameReference.FullName);
							if (assemblyFile != null && System.IO.File.Exists (assemblyFile))
								AddReference (assemblyFile);
						}
					}
					nav = SearchMember (url);
				}
				if (nav != null) {
					nav.ExpandToNode ();
					nav.Selected = true;
				} else {
					LoggingService.LogError ("Can't open: " + url + " (not found).");
				}
				return false;
			});
		}
		
		public void SelectAssembly (string fileName)
		{
			DomCecilCompilationUnit cu = null;
			foreach (DomCecilCompilationUnit unit in definitions) {
				if (unit.FileName == fileName)
					cu = unit;
			}
			if (cu == null)
				return;
			
			ITreeNavigator nav = treeView.GetRootNode ();
			do {
				if (nav.DataItem == cu.AssemblyDefinition) {
					nav.ExpandToNode ();
					nav.Selected = true;
					return;
				}
			} while (nav.MoveNext());
					
		}
		
		List<DomCecilCompilationUnit> definitions = new List<DomCecilCompilationUnit> ();
		public AssemblyDefinition AddReference (string fileName)
		{
			foreach (DomCecilCompilationUnit unit in definitions) {
				if (unit.FileName == fileName) 
					return unit.AssemblyDefinition;
			}
			DomCecilCompilationUnit newUnit = DomCecilCompilationUnit.Load (fileName);
			definitions.Add (newUnit);
			ITreeBuilder builder;
			if (definitions.Count == 1) {
				builder = treeView.LoadTree (newUnit.AssemblyDefinition);
				
			} else {
				builder = treeView.AddChild (newUnit.AssemblyDefinition);
			}
			builder.MoveToFirstChild ();
			builder.Expanded = true;
			return newUnit.AssemblyDefinition;
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void ShowSearchWidget ()
		{
			//this.searchWidget.Visible = true;
			this.notebook1.Page = 2;
			this.searchEntry.GrabFocus ();
		}
	
		#region NavigationHistory
		Stack<ITreeNavigator> navigationBackwardHistory = new Stack<ITreeNavigator> ();
		Stack<ITreeNavigator> navigationForwardHistory = new Stack<ITreeNavigator> ();
		ITreeNavigator currentItem = null;
		bool inNavigationOperation = false;
		void HandleCursorChanged(object sender, EventArgs e)
		{
			if (!inNavigationOperation) {
				if (currentItem != null)
					navigationBackwardHistory.Push (currentItem);
				currentItem = treeView.GetSelectedNode ();
				navigationForwardHistory.Clear ();
			}
			UpdateNavigationActions ();
		}
		
		void UpdateNavigationActions ()
		{
			NavigateBackwardAction.Sensitive = navigationBackwardHistory.Count != 0;
			NavigateForwardAction.Sensitive = navigationForwardHistory.Count != 0;
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
