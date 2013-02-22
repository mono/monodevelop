// 
// AspNetEditorExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.AspNet;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Html;
using MonoDevelop.DesignerSupport;

//I initially aliased this as SE, which (unintentionally) looked a little odd with the XDOM types :-)
using S = MonoDevelop.Xml.StateEngine; 
using MonoDevelop.AspNet.StateEngine;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Completion;

namespace MonoDevelop.AspNet.Gui
{
	public class AspNetEditorExtension : BaseHtmlEditorExtension
	{
		AspNetParsedDocument aspDoc;
		AspNetAppProject project;
		DocumentReferenceManager refman;
		
		bool HasDoc { get { return aspDoc != null; } }
		 
		Regex DocTypeRegex = new Regex (@"(?:PUBLIC|public)\s+""(?<fpi>[^""]*)""\s+""(?<uri>[^""]*)""");
		
		#region Setup and teardown
		
		protected override S.RootState CreateRootState ()
		{
			return new AspNetFreeState ();
		}
		
		#endregion
		
		protected override void OnParsedDocumentUpdated ()
		{
			base.OnParsedDocumentUpdated ();
			aspDoc = CU as AspNetParsedDocument;
			
			var newProj = base.Document.Project as AspNetAppProject;
			if (newProj == null)
				return;
				//throw new InvalidOperationException ("Document has no project");
			
			if (project != newProj) {
				project = newProj;
				refman = new DocumentReferenceManager (project);
			}
			
			if (HasDoc)
				refman.Doc = aspDoc;
			
			documentBuilder = HasDoc ? LanguageCompletionBuilderService.GetBuilder (aspDoc.Info.Language) : null;
			
			if (documentBuilder != null) {
				var usings = refman.GetUsings ();
				documentInfo = new DocumentInfo (document.Compilation, aspDoc, usings, refman.GetDoms ());
				documentInfo.ParsedDocument = documentBuilder.BuildDocument (documentInfo, Editor);
				documentInfo.CodeBesideClass = CreateCodeBesideClass (documentInfo, refman);
/*				var domWrapper = new AspProjectDomWrapper (documentInfo);
				if (localDocumentInfo != null)
					localDocumentInfo.HiddenDocument.HiddenContext = domWrapper;*/
			}
		}
		
		static IUnresolvedTypeDefinition CreateCodeBesideClass (DocumentInfo info, DocumentReferenceManager refman)
		{
			var v = new MemberListVisitor (refman);
			info.AspNetDocument.RootNode.AcceptVisit (v);
			var t = new ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultUnresolvedTypeDefinition (info.ClassName);
			var dom = refman.TypeCtx.Compilation;
			var baseType = ReflectionHelper.ParseReflectionName (info.BaseType).Resolve (dom);
			foreach (var m in CodeBehind.GetDesignerMembers (v.Members.Values, baseType, null)) {
				t.Members.Add (new ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultUnresolvedField (t, m.Name) {
					Accessibility = Accessibility.Protected,
					ReturnType = m.Type.ToTypeReference ()
				});
			}
			return t;
		}
		
		ILanguageCompletionBuilder documentBuilder;
		LocalDocumentInfo localDocumentInfo;
		DocumentInfo documentInfo;
		
		
		protected override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext,
		                                                            bool forced, ref int triggerWordLength)
		{
			ITextBuffer buf = this.Buffer;
			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			char currentChar = completionContext.TriggerOffset < 1? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 1);
			//char previousChar = completionContext.TriggerOffset < 2? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 2);
			
			
			//directive names
			if (Tracker.Engine.CurrentState is AspNetDirectiveState) {
				var directive = Tracker.Engine.Nodes.Peek () as AspNetDirective;
				if (HasDoc && directive != null && directive.Region.BeginLine == completionContext.TriggerLine &&
				    directive.Region.BeginColumn + 3 == completionContext.TriggerLineOffset)
				{
					return DirectiveCompletion.GetDirectives (aspDoc.Type);
				}
				return null;
			} else if (Tracker.Engine.CurrentState is S.XmlNameState && Tracker.Engine.CurrentState.Parent is AspNetDirectiveState) {
				var directive = Tracker.Engine.Nodes.Peek () as AspNetDirective;
				if (HasDoc && directive != null && directive.Region.BeginLine == completionContext.TriggerLine &&
				    directive.Region.BeginColumn + 4 == completionContext.TriggerLineOffset && char.IsLetter (currentChar))
				{
					triggerWordLength = 1;
					return DirectiveCompletion.GetDirectives (aspDoc.Type);
				}
				return null;
			}
			
			bool isAspExprState =  Tracker.Engine.CurrentState is AspNetExpressionState;
			
			//non-xml tag completion
			if (currentChar == '<' && !(isAspExprState || Tracker.Engine.CurrentState is S.XmlFreeState)) {
				var list = new CompletionDataList ();
				AddAspBeginExpressions (list);
				return list;
			}
			
			if (!HasDoc || aspDoc.Info.DocType == null) {
				//FIXME: get doctype from master page
				DocType = null;
			} else {
				DocType = new MonoDevelop.Xml.StateEngine.XDocType (TextLocation.Empty);
				var matches = DocTypeRegex.Match (aspDoc.Info.DocType);
				DocType.PublicFpi = matches.Groups["fpi"].Value;
				DocType.Uri = matches.Groups["uri"].Value;
			}
			
			if (Tracker.Engine.CurrentState is HtmlScriptBodyState) {
				var el = Tracker.Engine.Nodes.Peek () as S.XElement;
				if (el != null) {
					var att = GetHtmlAtt (el, "runat");
					if (att != null && "server".Equals (att.Value, StringComparison.OrdinalIgnoreCase)) {
						if (documentBuilder != null) {
							// TODO: C# completion
						}
					}
					/*
					else {
						att = GetHtmlAtt (el, "language");
						if (att == null || "javascript".Equals (att.Value, StringComparison.OrdinalIgnoreCase)) {
						    att = GetHtmlAtt (el, "type");
							if (att == null || "text/javascript".Equals (att.Value, StringComparison.OrdinalIgnoreCase)) {
								// TODO: JS completion
							}
						}
					}*/
				}
				
			}
			
			return base.HandleCodeCompletion (completionContext, forced, ref triggerWordLength);
		}
		
		//case insensitive, no prefix
		static S.XAttribute GetHtmlAtt (S.XElement el, string name)
		{
			return el.Attributes
				.Where (a => a.IsNamed && !a.Name.HasPrefix && a.Name.Name.Equals (name, StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault ();
		}
		
		public void InitializeCodeCompletion (char ch)
		{
			int caretOffset = Document.Editor.Caret.Offset;
			int start = caretOffset - Tracker.Engine.CurrentStateLength;
			if (Document.Editor.GetCharAt (start) == '=') 
				start++;
			string sourceText = Document.Editor.GetTextBetween (start, caretOffset);
			if (ch != '\0')
				sourceText += ch;
			string textAfterCaret = Document.Editor.GetTextBetween (caretOffset, Math.Min (Document.Editor.Length, Math.Max (caretOffset, Tracker.Engine.Position + Tracker.Engine.CurrentStateLength - 2)));
			
			var loc = new MonoDevelop.AspNet.Parser.Internal.Location ();
			var docLoc = Document.Editor.Document.OffsetToLocation (start);
			loc.EndLine = loc.BeginLine = docLoc.Line;
			loc.EndColumn = loc.BeginColumn = docLoc.Column;
			if (documentBuilder == null){
				localDocumentInfo = null;
				return;
			}
			localDocumentInfo = documentBuilder.BuildLocalDocument (documentInfo, Editor, sourceText, textAfterCaret, true);
			
			var viewContent = new MonoDevelop.Ide.Gui.HiddenTextEditorViewContent ();
			viewContent.Project = Document.Project;
			viewContent.ContentName = localDocumentInfo.ParsedLocalDocument.FileName;
			
			viewContent.Text = localDocumentInfo.LocalDocument;
			viewContent.GetTextEditorData ().Caret.Offset = localDocumentInfo.CaretPosition;

			var workbenchWindow = new MonoDevelop.Ide.Gui.HiddenWorkbenchWindow ();
			workbenchWindow.ViewContent = viewContent;
			localDocumentInfo.HiddenDocument = new HiddenDocument (workbenchWindow) {
				HiddenParsedDocument = localDocumentInfo.ParsedLocalDocument
			};
		}
		
		
		public override ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			//completion for ASP.NET expressions
			// TODO: Detect <script> state here !!!
			if (documentBuilder != null && Tracker.Engine.CurrentState is AspNetExpressionState) {
				InitializeCodeCompletion ('\0');
				return documentBuilder.HandlePopupCompletion (defaultDocument, documentInfo, localDocumentInfo);
			}
			return base.CodeCompletionCommand (completionContext);
		}
		
		ICompletionWidget defaultCompletionWidget;
		MonoDevelop.Ide.Gui.Document defaultDocument;
//		AspProjectDomWrapper domWrapper;
		public override void Initialize ()
		{
			base.Initialize ();
			defaultCompletionWidget = CompletionWidget;
			defaultDocument = document;
			defaultDocument.Editor.Caret.PositionChanged += delegate {
				OnCompletionContextChanged (CompletionWidget, EventArgs.Empty);
			};
		}
		
		
		public override ICompletionDataList HandleCodeCompletion (
		    CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			if (localDocumentInfo == null)
				return base.HandleCodeCompletion (completionContext, completionChar, ref triggerWordLength);
			localDocumentInfo.HiddenDocument.Editor.InsertAtCaret (completionChar.ToString ());
			return documentBuilder.HandleCompletion (defaultDocument, completionContext, documentInfo, localDocumentInfo, completionChar, ref triggerWordLength);
		}

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			Tracker.UpdateEngine ();
			bool isAspExprState = Tracker.Engine.CurrentState is AspNetExpressionState;
			if (documentBuilder == null || !isAspExprState)
				return base.KeyPress (key, keyChar, modifier);
			InitializeCodeCompletion ('\0');
			document = localDocumentInfo.HiddenDocument;
			CompletionWidget = documentBuilder.CreateCompletionWidget (defaultDocument, localDocumentInfo);
			bool result;
			try {
				result = base.KeyPress (key, keyChar, modifier);
				if (PropertyService.Get ("EnableParameterInsight", true) && (keyChar == ',' || keyChar == ')') && CanRunParameterCompletionCommand ()) {
					RunParameterCompletionCommand ();
				}
			} finally {
				document = defaultDocument;
				CompletionWidget = defaultCompletionWidget;
			}
			return result;
		}
		
		public override bool GetParameterCompletionCommandOffset (out int cpos)
		{
			/*if (Tracker.Engine.CurrentState is AspNetExpressionState && documentBuilder != null && localDocumentInfo != null) {
				var result = documentBuilder.GetParameterCompletionCommandOffset (defaultDocument, documentInfo, localDocumentInfo, out cpos);
				return result;
			}*/
			return base.GetParameterCompletionCommandOffset (out cpos);
		}
		
		public override ParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
/*			if (Tracker.Engine.CurrentState is AspNetExpressionState && documentBuilder != null && localDocumentInfo != null) {
				return documentBuilder.HandleParameterCompletion (defaultDocument, completionContext, documentInfo, localDocumentInfo, completionChar);
			}*/
			
			return base.HandleParameterCompletion (completionContext, completionChar);
		}

		/*public override void RunParameterCompletionCommand ()
		{
			if (localDocumentInfo == null) {
				base.RunParameterCompletionCommand ();
				return;
			}
			var doc = document;
			document = localDocumentInfo.HiddenDocument;
			var cw = CompletionWidget;
			CompletionWidget = documentBuilder.CreateCompletionWidget (localDocumentInfo);
			try {
				base.RunParameterCompletionCommand ();
			} finally {
				document = doc;
				CompletionWidget = cw;
			}
		}*/

		protected override void GetElementCompletions (CompletionDataList list)
		{
			S.XName parentName = GetParentElementName (0);
			
			//fallback
			if (!HasDoc) {
				AddAspBeginExpressions (list);
				string aspPrefix = "asp:";
				var type = ReflectionHelper.ParseReflectionName ("System.Web.UI.Control").Resolve (TypeSystemService.GetCompilation (project));
				foreach (var cls in WebTypeContext.ListSystemControlClasses (type, project))
					list.Add (new AspTagCompletionData (aspPrefix, cls));
				
				base.GetElementCompletions (list);
				return;
			}
			
			IType controlClass = null;
			
			if (parentName.HasPrefix) {
				controlClass = refman.GetControlType (parentName.Prefix, parentName.Name);
			} else {
				S.XName grandparentName = GetParentElementName (1);
				if (grandparentName.IsValid && grandparentName.HasPrefix)
					controlClass = refman.GetControlType (grandparentName.Prefix, grandparentName.Name);
			}
			
			//we're just in HTML
			if (controlClass == null) {
				//root element?
				if (!parentName.IsValid) {
					if (aspDoc.Info.Subtype == WebSubtype.WebControl) {
						AddHtmlTagCompletionData (list, Schema, new S.XName ("div"));
						AddAspBeginExpressions (list);
						list.AddRange (refman.GetControlCompletionData ());
						AddMiscBeginTags (list);
					} else if (!string.IsNullOrEmpty (aspDoc.Info.MasterPageFile)) {
						//FIXME: add the actual region names
						list.Add (new CompletionData ("asp:Content"));
					}
				} else {
					AddAspBeginExpressions (list);
					list.AddRange (refman.GetControlCompletionData ());
					base.GetElementCompletions (list);
				}
				return;
			}
			
			string defaultProp;
			bool childrenAsProperties = AreChildrenAsProperties (controlClass, out defaultProp);
			if (defaultProp != null && defaultProp.Length == 0)
				defaultProp = null;
			
			//parent permits child controls directly
			if (!childrenAsProperties) {
				AddAspBeginExpressions (list);
				list.AddRange (refman.GetControlCompletionData ());
				AddMiscBeginTags (list);
				//TODO: get correct parent for Content tags
				AddHtmlTagCompletionData (list, Schema, new S.XName ("body"));
				return;
			}
			
			//children of properties
			if (childrenAsProperties && (!parentName.HasPrefix || defaultProp != null)) {
				if (controlClass.GetProjectContent () == null) {
					LoggingService.LogWarning ("IType {0} does not have a SourceProjectDom", controlClass);
					return;
				}
				
				string propName = defaultProp ?? parentName.Name;
				IProperty property =
					controlClass.GetProperties ()
						.Where (x => string.Compare (propName, x.Name, StringComparison.OrdinalIgnoreCase) == 0)
						.FirstOrDefault ();
				
				if (property == null)
					return;
				
				//sanity checks on attributes
				switch (GetPersistenceMode (property)) {
				case System.Web.UI.PersistenceMode.Attribute:
				case System.Web.UI.PersistenceMode.EncodedInnerDefaultProperty:
					return;
					
				case System.Web.UI.PersistenceMode.InnerDefaultProperty:
					if (!parentName.HasPrefix)
						return;
					break;
					
				case System.Web.UI.PersistenceMode.InnerProperty:
					if (parentName.HasPrefix)
						return;
					break;
				}
				
				//check if allows freeform ASP/HTML content
				if (property.ReturnType.ToString () == "System.Web.UI.ITemplate") {
					AddAspBeginExpressions (list);
					AddMiscBeginTags (list);
					AddHtmlTagCompletionData (list, Schema, new S.XName ("body"));
					list.AddRange (refman.GetControlCompletionData ());
					return;
				}
				
				//FIXME:unfortunately ASP.NET doesn't seem to have enough type information / attributes
				//to be able to resolve the correct child types here
				//so we assume it's a list and have a quick hack to find arguments of strongly typed ILists
				
				IType collectionType = property.ReturnType;
				if (collectionType == null) {
					list.AddRange (refman.GetControlCompletionData ());
					return;
				}
				
				string addStr = "Add";
				IMethod meth = collectionType.GetMethods ()
					.Where (m => m.Parameters.Count == 1 && m.Name == addStr).FirstOrDefault ();
				
				if (meth != null) {
					IType argType = meth.Parameters [0].Type;
					var type = ReflectionHelper.ParseReflectionName ("System.Web.UI.Control").Resolve (argType.GetDefinition ().Compilation);
					if (argType != null && argType.IsBaseType (type)) {
						list.AddRange (refman.GetControlCompletionData (argType));
						return;
					}
				}
				
				list.AddRange (refman.GetControlCompletionData ());
				return;
			}
			
			//properties as children of controls
			if (parentName.HasPrefix && childrenAsProperties) {
				if (controlClass.GetProjectContent () == null) {
					LoggingService.LogWarning ("IType {0} does not have a SourceProjectDom", controlClass);
				}
				
				foreach (IProperty prop in GetUniqueMembers<IProperty> (controlClass.GetProperties ()))
					if (GetPersistenceMode (prop) != System.Web.UI.PersistenceMode.Attribute)
						list.Add (prop.Name, prop.GetStockIcon (), AmbienceService.GetSummaryMarkup (prop));
				return;
			}
		}
		
		protected override CompletionDataList GetAttributeCompletions (S.IAttributedXObject attributedOb,
		                                                 Dictionary<string, string> existingAtts)
		{
			var list = base.GetAttributeCompletions (attributedOb, existingAtts) ?? new CompletionDataList ();
			if (attributedOb is S.XElement) {
				
				if (!existingAtts.ContainsKey ("runat"))
					list.Add ("runat=\"server\"", "md-literal",
						GettextCatalog.GetString ("Required for ASP.NET controls.\n") +
						GettextCatalog.GetString (
							"Indicates that this tag should be able to be\n" +
							"manipulated programmatically on the web server."));
				
				if (!existingAtts.ContainsKey ("id"))
					list.Add ("id", "md-literal",
						GettextCatalog.GetString ("Unique identifier.\n") +
						GettextCatalog.GetString (
							"An identifier that is unique within the document.\n" + 
							"If the tag is a server control, this will be used \n" +
							"for the corresponding variable name in the CodeBehind."));
				
				existingAtts["ID"] = "";
				if (attributedOb.Name.HasPrefix) {
					AddAspAttributeCompletionData (list, attributedOb.Name, existingAtts);
				}
				
			} else if (attributedOb is AspNetDirective) {
				return DirectiveCompletion.GetAttributes (project, attributedOb.Name.FullName, existingAtts);
			}
			return list.Count > 0? list : null;
		}
		
		protected override CompletionDataList GetAttributeValueCompletions (S.IAttributedXObject ob, S.XAttribute att)
		{
			var list = base.GetAttributeValueCompletions (ob, att) ?? new CompletionDataList ();
			if (ob is S.XElement) {
				if (ob.Name.HasPrefix) {
					S.XAttribute idAtt = ob.Attributes[new S.XName ("id")];
					string id = idAtt == null? null : idAtt.Value;
					if (string.IsNullOrEmpty (id) || string.IsNullOrEmpty (id.Trim ()))
						id = null;
					AddAspAttributeValueCompletionData (list, ob.Name, att.Name, id);
				}
			} else if (ob is AspNetDirective) {
				return DirectiveCompletion.GetAttributeValues (project, Document.FileName, ob.Name.FullName, att.Name.FullName);
			}
			return list.Count > 0? list : null;
		}
		
		ClrVersion ProjClrVersion {
			get { return project.TargetFramework.ClrVersion; }
		}
		
		CompletionDataList HandleExpressionCompletion (AspNetExpression expr)
		{
			if (!(expr is AspNetDataBindingExpression || expr is AspNetRenderExpression))
				return null;
			IType codeBehindClass;
			ICompilation projectDatabase;
			GetCodeBehind (out codeBehindClass, out projectDatabase);
			
			if (codeBehindClass == null)
				return null;
			
			//list just the class's properties, not properties on base types
			CompletionDataList list = new CompletionDataList ();
			list.AddRange (from p in codeBehindClass.GetProperties ()
				where p.IsPublic || p.IsPublic
				select new CompletionData (p.Name, "md-property"));
			list.AddRange (from p in codeBehindClass.GetFields ()
				where p.IsProtected || p.IsPublic
				select new CompletionData (p.Name, "md-property"));
			
			return list.Count > 0? list : null;
		}
		
		void GetCodeBehind (out IType codeBehindClass, out ICompilation projectDatabase)
		{
			codeBehindClass = null;
			projectDatabase = null;
			
			if (HasDoc && !string.IsNullOrEmpty (aspDoc.Info.InheritedClass)) {
				projectDatabase = TypeSystemService.GetCompilation (project);
				if (projectDatabase != null)
					codeBehindClass = ReflectionHelper.ParseReflectionName (aspDoc.Info.InheritedClass).Resolve (projectDatabase);
			}
		}
		
		#region ASP.NET data
		
		void AddAspBeginExpressions (CompletionDataList list)
		{
			list.Add ("%",  "md-literal", GettextCatalog.GetString ("ASP.NET render block"));
			list.Add ("%=", "md-literal", GettextCatalog.GetString ("ASP.NET render expression"));
			list.Add ("%@", "md-literal", GettextCatalog.GetString ("ASP.NET directive"));
			list.Add ("%#", "md-literal", GettextCatalog.GetString ("ASP.NET databinding expression"));
			list.Add ("%--", "md-literal", GettextCatalog.GetString ("ASP.NET server-side comment"));
			
			//valid on 2.0+ runtime only
			if (ProjClrVersion != ClrVersion.Net_1_1) {
				list.Add ("%$", "md-literal", GettextCatalog.GetString ("ASP.NET resource expression"));
			}
			
			//valid on 2.0+ runtime only
			if (ProjClrVersion != ClrVersion.Net_4_0) {
				list.Add ("%:", "md-literal", GettextCatalog.GetString ("ASP.NET HTML encoded expression"));
			}
		}
		
		void AddAspAttributeCompletionData (CompletionDataList list, S.XName name, Dictionary<string, string> existingAtts)
		{
			Debug.Assert (name.IsValid);
			Debug.Assert (name.HasPrefix);
			
			var database = TypeSystemService.GetCompilation (project);
			
			if (database == null) {
				LoggingService.LogWarning ("Could not obtain project DOM in AddAspAttributeCompletionData");
				return;
			}
			
			IType controlClass = refman.GetControlType (name.Prefix, name.Name);
			if (controlClass == null) {
				controlClass = ReflectionHelper.ParseReflectionName ("System.Web.UI.WebControls.WebControl").Resolve (database);
				if (controlClass == null) {
					LoggingService.LogWarning ("Could not obtain IType for System.Web.UI.WebControls.WebControl");
					return;
				}
			}
			
			AddControlMembers (list, controlClass, existingAtts);
		}
		
		void AddControlMembers (CompletionDataList list, IType controlClass, 
		                        Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (var prop in GetUniqueMembers<IProperty> (controlClass.GetProperties ()))
				if (prop.Accessibility == Accessibility.Public && (existingAtts == null || !existingAtts.ContainsKey (prop.Name)))
				if (GetPersistenceMode (prop) == System.Web.UI.PersistenceMode.Attribute)
						list.Add (prop.Name, prop.GetStockIcon (), AmbienceService.GetSummaryMarkup (prop));
			
			//similarly add events
			foreach (var eve in GetUniqueMembers<IEvent> (controlClass.GetEvents ())) {
				string eveName = "On" + eve.Name;
				if (eve.Accessibility == Accessibility.Public && (existingAtts == null || !existingAtts.ContainsKey (eveName)))
					list.Add (eveName, eve.GetStockIcon (), AmbienceService.GetSummaryMarkup (eve));
			}
		}
		
		void AddAspAttributeValueCompletionData (CompletionDataList list, S.XName tagName, S.XName attName, string id)
		{
			Debug.Assert (tagName.IsValid && tagName.HasPrefix);
			Debug.Assert (attName.IsValid && !attName.HasPrefix);
			
			IType controlClass = HasDoc ? refman.GetControlType (tagName.Prefix, tagName.Name) : null;
			
			if (controlClass == null) {
				LoggingService.LogWarning ("Could not obtain IType for {0}", tagName.FullName);
				
				var database = WebTypeContext.GetSystemWebDom (project);
				controlClass = ReflectionHelper.ParseReflectionName ("System.Web.UI.WebControls.WebControl").Resolve (database);

				if (controlClass == null) {
					LoggingService.LogWarning ("Could not obtain IType for System.Web.UI.WebControls.WebControl");
					return;
				}
			}
			
			//find the codebehind class
			IType codeBehindClass;
			ICompilation projectDatabase;
			GetCodeBehind (out codeBehindClass, out projectDatabase);
			
			//if it's an event, suggest compatible methods 
			if (codeBehindClass != null && attName.Name.StartsWith ("On")) {
				string eventName = attName.Name.Substring (2);
				
				foreach (IEvent ev in controlClass.GetEvents ()) {
					if (ev.Name == eventName) {
						var domMethod = BindingService.MDDomToCodeDomMethod (ev);
						if (domMethod == null)
							return;
						
						foreach (IMethod meth 
						    in BindingService.GetCompatibleMethodsInClass (codeBehindClass, ev)) {
							list.Add (meth.Name, "md-method",
							    GettextCatalog.GetString ("A compatible method in the CodeBehind class"));
						}
						
						string suggestedIdentifier = ev.Name;
						if (id != null) {
							suggestedIdentifier = id + "_" + suggestedIdentifier;
						} else {
							suggestedIdentifier = tagName.Name + "_" + suggestedIdentifier;
						}
							
						domMethod.Name = BindingService.GenerateIdentifierUniqueInClass
							(codeBehindClass, suggestedIdentifier);
						domMethod.Attributes = (domMethod.Attributes & ~System.CodeDom.MemberAttributes.AccessMask)
							| System.CodeDom.MemberAttributes.Family;
						
						list.Add (
						    new SuggestedHandlerCompletionData (project, domMethod, codeBehindClass,
						        MonoDevelop.DesignerSupport.CodeBehind.GetNonDesignerClass (codeBehindClass))
						    );
						return;
					}
				}
			}
			
			if (projectDatabase == null) {
				projectDatabase = WebTypeContext.GetSystemWebDom (project);
				
				if (projectDatabase == null) {
					LoggingService.LogWarning ("Could not obtain type database in AddAspAttributeCompletionData");
					return;
				}
			}
			
			//if it's a property and is an enum or bool, suggest valid values
			foreach (IProperty prop in controlClass.GetProperties ()) {
				if (prop.Name != attName.Name)
					continue;
				
				//boolean completion
				if (prop.ReturnType.Equals (projectDatabase.FindType (KnownTypeCode.Boolean))) {
					AddBooleanCompletionData (list);
					return;
				}
				//color completion
				if (prop.ReturnType.Equals (projectDatabase.FindType (typeof(System.Drawing.Color)))) {
					System.Drawing.ColorConverter conv = new System.Drawing.ColorConverter ();
					foreach (System.Drawing.Color c in conv.GetStandardValues (null)) {
						if (c.IsSystemColor)
							continue;
						string hexcol = string.Format ("#{0:x2}{1:x2}{2:x2}", c.R, c.G, c.B);
						list.Add (c.Name, hexcol);
					}
					return;
				}
				
				//enum completion
				IType retCls = prop.ReturnType;
				if (retCls != null && retCls.Kind == TypeKind.Enum) {
					foreach (var enumVal in retCls.GetFields ())
						if (enumVal.IsPublic && enumVal.IsStatic)
							list.Add (enumVal.Name, "md-literal", AmbienceService.GetSummaryMarkup (enumVal));
					return;
				}
			}
		}
		
		static IEnumerable<T> GetUniqueMembers<T> (IEnumerable<T> members) where T : IMember
		{
			Dictionary <string, bool> existingItems = new Dictionary<string,bool> ();
			foreach (T item in members) {
				if (existingItems.ContainsKey (item.Name))
					continue;
				existingItems[item.Name] = true;
				yield return item;
			}
		}
		
		static void AddBooleanCompletionData (CompletionDataList list)
		{
			list.Add ("true", "md-literal");
			list.Add ("false", "md-literal");
		}
		
		#endregion
		
		#region Querying types' attributes
		
		static System.Web.UI.PersistenceMode GetPersistenceMode (IProperty prop)
		{
			foreach (var att in prop.Attributes) {
				if (att.AttributeType.ReflectionName == "System.Web.UI.PersistenceModeAttribute") {
					var expr = att.PositionalArguments.FirstOrDefault ();
					if (expr == null) {
						LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", expr);
						return System.Web.UI.PersistenceMode.Attribute;
					}
					
					return (System.Web.UI.PersistenceMode) expr.ConstantValue;
				}
				else if (att.AttributeType.ReflectionName == "System.Web.UI.TemplateContainerAttribute")
				{
					return System.Web.UI.PersistenceMode.InnerProperty;
				}
			}
			return System.Web.UI.PersistenceMode.Attribute;
		}
		
		static bool AreChildrenAsProperties (IType type, out string defaultProperty)
		{
			bool childrenAsProperties = false;
			defaultProperty = "";
			
			IAttribute att = GetAttributes (type, "System.Web.UI.ParseChildrenAttribute").FirstOrDefault ();
			var posArgs = att.PositionalArguments;
			if (att == null || posArgs.Count == 0)
				return childrenAsProperties;
			
			if (posArgs.Count > 0) {
				var expr = posArgs [0];
				if (expr == null) {
					LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", expr);
					return false;
				}
				
				if (expr.ConstantValue is bool) {
					childrenAsProperties = (bool)expr.ConstantValue;
				} else {
					//TODO: implement this
					LoggingService.LogWarning ("ASP.NET completion does not yet handle ParseChildrenAttribute (Type)");
					return false;
				}
			}
			
			if (posArgs.Count > 1) {
				var expr = posArgs [1];
				if (expr == null || !(expr.ConstantValue is string)) {
					LoggingService.LogWarning ("Unknown expression '{0}' in IAttribute parameter", expr);
					return false;
				}
				defaultProperty = (string)expr.ConstantValue;
			}
			
			var namedArgs = att.NamedArguments;
			if (namedArgs.Count > 0) {
				if (namedArgs.Any (p => p.Key.Name == "ChildrenAsProperties")) {
					var expr = namedArgs.First (p => p.Key.Name == "ChildrenAsProperties").Value;
					if (expr == null) {
						LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", expr);
						return false;
					}
					childrenAsProperties = (bool)expr.ConstantValue;
				}
				if (namedArgs.Any (p => p.Key.Name == "DefaultProperty")) {
					var expr = namedArgs.First (p => p.Key.Name == "DefaultProperty").Value;
					if (expr == null) {
						LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", expr);
						return false;
					}
					defaultProperty = (string)expr.ConstantValue;
				}
				if (namedArgs.Any (p => p.Key.Name == "ChildControlType")) {
					//TODO: implement this
					LoggingService.LogWarning ("ASP.NET completion does not yet handle ParseChildrenAttribute (Type)");
					return false;
				}
			}
			
			return childrenAsProperties;
		}
		
		static IEnumerable<IAttribute> GetAttributes (IType type, string attName)
		{
			foreach (var att in type.GetDefinition ().Attributes) {
				if (att.AttributeType.ReflectionName == attName)
					yield return att;
			}
			
			if (type.GetProjectContent () == null) {
				LoggingService.LogWarning ("IType {0} has null SourceProjectDom", type);
				yield break;
			}
			
			foreach (IType t2 in type.GetAllBaseTypes ()) {
				foreach (IAttribute att in t2.GetDefinition ().Attributes)
					if (att.AttributeType.ReflectionName == attName)
						yield return att;
			}
		}
		
		#endregion
		
		#region Document outline
		
		protected override void RefillOutlineStore (ParsedDocument doc, Gtk.TreeStore store)
		{
			ParentNode p = ((AspNetParsedDocument)doc).RootNode;
//			Gtk.TreeIter iter = outlineTreeStore.AppendValues (System.IO.Path.GetFileName (CU.Document.FilePath), p);
			BuildTreeChildren (store, Gtk.TreeIter.Zero, p);
		}
		
		protected override void InitializeOutlineColumns (MonoDevelop.Ide.Gui.Components.PadTreeView outlineTree)
		{
			outlineTree.TextRenderer.Xpad = 0;
			outlineTree.TextRenderer.Ypad = 0;
			outlineTree.AppendColumn ("Node", outlineTree.TextRenderer, new Gtk.TreeCellDataFunc (outlineTreeDataFunc));
		}
		
		protected override void OutlineSelectionChanged (object selection)
		{
			SelectNode ((Node)selection);
		}
		
		static void BuildTreeChildren (Gtk.TreeStore store, Gtk.TreeIter parent, ParentNode p)
		{
			foreach (Node n in p) {
				if (!(n is TagNode || n is DirectiveNode || n is ExpressionNode))
					continue;
				Gtk.TreeIter childIter;
				if (!parent.Equals (Gtk.TreeIter.Zero))
					
					childIter = store.AppendValues (parent, n);
				else
					childIter = store.AppendValues (n);
				ParentNode pChild = n as ParentNode;
				if (pChild != null)
					BuildTreeChildren (store, childIter, pChild);
			}
		}
		
		void outlineTreeDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText txtRenderer = (Gtk.CellRendererText) cell;
			Node n = (Node) model.GetValue (iter, 0);
			string name = null;
			if (n is TagNode) {
				TagNode tn = (TagNode) n;
				name = tn.TagName;
				string att = null;
				if (tn.Attributes != null) {
					att = tn.Attributes["id"] as string;
				}
				if (att != null)
					name = "<" + name + "#" + att + ">";
				else
					name = "<" + name + ">";
			} else if (n is DirectiveNode) {
				DirectiveNode dn = (DirectiveNode) n;
				name = "<%@ " + dn.Name + " %>";
			} else if (n is ExpressionNode) {
				ExpressionNode en = (ExpressionNode) n;
				string expr = en.Expression;
				if (string.IsNullOrEmpty (expr)) {
					name = "<% %>";
				} else {
					if (expr.Length > 10)
						expr = expr.Substring (0, 10) + "...";
					name = "<% " + expr + "%>";
				}
			}
			if (name != null)
				txtRenderer.Text = name;
		}
		
		void SelectNode (Node n)
		{
			ILocation start = n.Location, end;
			TagNode tn = n as TagNode;
			if (tn != null && tn.EndLocation != null)
				end = tn.EndLocation;
			else
				end = start;
			
			//FIXME: why is this offset necessary?
			int offset = n is TagNode? 1 : 0;
			EditorSelect (new DomRegion (start.BeginLine, start.BeginColumn + offset, end.EndLine, end.EndColumn + offset));
		}
		#endregion
	}
		
}
