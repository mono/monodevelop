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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using MonoDevelop.Core;
using MonoDevelop.DesignerSupport; 
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.AspNet.Html;
using MonoDevelop.AspNet.Html.Parser;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.WebForms.Parser;
using S = MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.WebForms.Dom;
using MonoDevelop.Xml.Parser;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Projects;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Projection;

namespace MonoDevelop.AspNet.WebForms
{
	class WebFormsEditorExtension : BaseHtmlEditorExtension
	{
		static readonly System.Text.RegularExpressions.Regex DocTypeRegex = new System.Text.RegularExpressions.Regex (@"(?:PUBLIC|public)\s+""(?<fpi>[^""]*)""\s+""(?<uri>[^""]*)""");

		WebFormsParsedDocument aspDoc;
		DotNetProject project;
		WebFormsTypeContext refman = new WebFormsTypeContext ();

		ILanguageCompletionBuilder documentBuilder;
		Projection localDocumentProjection;
		DocumentInfo documentInfo;

		ICompletionWidget defaultCompletionWidget;
		DocumentContext defaultDocumentContext;
		TextEditor defaultEditor;
		TextEditor projectedEditor;
		
		bool HasDoc { get { return aspDoc != null; } }

		public WebFormsTypeContext ReferenceManager {
			get {
				return refman;
			}
		}
		
		#region Setup and teardown
		
		protected override XmlRootState CreateRootState ()
		{
			return new WebFormsRootState ();
		}
		
		#endregion
		
		protected override void OnParsedDocumentUpdated ()
		{
			base.OnParsedDocumentUpdated ();
			aspDoc = CU as WebFormsParsedDocument;
			if (HasDoc)
				refman.Doc = aspDoc;

			var newProj = base.DocumentContext.Project as DotNetProject;
			if (newProj != null) {
				project = newProj;
				refman.Project = newProj;
			}

			if (HasDoc) {
				documentInfo = new DocumentInfo (aspDoc, refman.GetUsings ());
//				localDocumentProjection = new MonoDevelop.AspNet.WebForms.CSharp.CSharpProjector ().CreateProjection (documentInfo, Editor, true).Result;
//				projectedEditor = localDocumentProjection.CreateProjectedEditor (DocumentContext);
//				Editor.SetOrUpdateProjections (DocumentContext, new [] { localDocumentProjection  });
			}
		}
		
		static INamedTypeSymbol CreateCodeBesideClass (DocumentInfo info, WebFormsTypeContext refman)
		{
			//TODO: Roslyn port
//			var memberList = new WebFormsMemberListBuilder (refman, info.AspNetDocument.XDocument);
//			memberList.Build ();
//			var t = new ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultUnresolvedTypeDefinition (info.ClassName);
//			var dom = refman.Compilation;
//			var baseType = ReflectionHelper.ParseReflectionName (info.BaseType).Resolve (dom);
//			foreach (var m in WebFormsCodeBehind.GetDesignerMembers (memberList.Members.Values, baseType, null)) {
//				t.Members.Add (new ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultUnresolvedField (t, m.Name) {
//					Accessibility = Accessibility.Protected,
//					ReturnType = m.Type.ToTypeReference ()
//				});
//			}
//			return t;
			return null;
		}

		protected override Task<ICompletionDataList> HandleCodeCompletion (
			CodeCompletionContext completionContext, bool forced, CancellationToken token)
		{
			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			char currentChar = completionContext.TriggerOffset < 1? ' ' : Editor.GetCharAt (completionContext.TriggerOffset - 1);
			//char previousChar = completionContext.TriggerOffset < 2? ' ' : buf.GetCharAt (completionContext.TriggerOffset - 2);

			//directive names
			if (Tracker.Engine.CurrentState is WebFormsDirectiveState) {
				var directive = Tracker.Engine.Nodes.Peek () as WebFormsDirective;
				if (HasDoc && directive != null && directive.Region.BeginLine == completionContext.TriggerLine &&
				    directive.Region.BeginColumn + 3 == completionContext.TriggerLineOffset)
				{
					return Task.FromResult ((ICompletionDataList)WebFormsDirectiveCompletion.GetDirectives (aspDoc.Type));
				}
				return null;
			}
			if (Tracker.Engine.CurrentState is XmlNameState && Tracker.Engine.CurrentState.Parent is WebFormsDirectiveState) {
				var directive = Tracker.Engine.Nodes.Peek () as WebFormsDirective;
				if (HasDoc && directive != null && directive.Region.BeginLine == completionContext.TriggerLine &&
				    directive.Region.BeginColumn + 4 == completionContext.TriggerLineOffset && char.IsLetter (currentChar))
				{
					completionContext.TriggerWordLength = 1;
					completionContext.TriggerOffset -= 1;
					return Task.FromResult ((ICompletionDataList)WebFormsDirectiveCompletion.GetDirectives (aspDoc.Type));
				}
				return null;
			}
			
			bool isAspExprState =  Tracker.Engine.CurrentState is WebFormsExpressionState;
			
			//non-xml tag completion
			if (currentChar == '<' && !(isAspExprState || Tracker.Engine.CurrentState is XmlRootState)) {
				var list = new CompletionDataList ();
				AddAspBeginExpressions (list);
				return Task.FromResult ((ICompletionDataList)list);
			}
			
			if (!HasDoc || aspDoc.Info.DocType == null) {
				//FIXME: get doctype from master page
				DocType = null;
			} else {
				DocType = new XDocType (DocumentLocation.Empty);
				var matches = DocTypeRegex.Match (aspDoc.Info.DocType);
				DocType.PublicFpi = matches.Groups["fpi"].Value;
				DocType.Uri = matches.Groups["uri"].Value;
			}
			
			if (Tracker.Engine.CurrentState is HtmlScriptBodyState) {
				var el = Tracker.Engine.Nodes.Peek () as XElement;
				if (el != null) {
					if (el.IsRunatServer ()) {
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
			
			return base.HandleCodeCompletion (completionContext, forced, token);
		}
		
		//case insensitive, no prefix
		static XAttribute GetHtmlAtt (XElement el, string name)
		{
			return el.Attributes.FirstOrDefault (a => a.IsNamed && !a.Name.HasPrefix && a.Name.Name.Equals (name, StringComparison.OrdinalIgnoreCase));
		}
		
		public void InitializeCodeCompletion (char ch)
		{
			int caretOffset = Editor.CaretOffset;
			int start = caretOffset - Tracker.Engine.CurrentStateLength;
			if (Editor.GetCharAt (start) == '=') 
				start++;
			string sourceText = Editor.GetTextBetween (start, caretOffset);
			if (ch != '\0')
				sourceText += ch;
			string textAfterCaret = Editor.GetTextBetween (caretOffset, Math.Min (Editor.Length, Math.Max (caretOffset, Tracker.Engine.Position + Tracker.Engine.CurrentStateLength - 2)));

			if (documentBuilder == null) {
				localDocumentProjection = null;
				return;
			}

//			var viewContent = new MonoDevelop.Ide.Gui.HiddenTextEditorViewContent ();
//			viewContent.Project = DocumentContext.Project;
//			viewContent.ContentName = localDocumentInfo.ParsedLocalDocument.FileName;
//			
//			viewContent.Text = localDocumentInfo.LocalDocument;
//			viewContent.Editor.CaretOffset = localDocumentInfo.CaretPosition;
//
//			var workbenchWindow = new MonoDevelop.Ide.Gui.HiddenWorkbenchWindow ();
//			workbenchWindow.ViewContent = viewContent;
//			localDocumentInfo.HiddenDocument = new HiddenDocument (workbenchWindow) {
//				HiddenParsedDocument = localDocumentInfo.ParsedLocalDocument
//			};
		}
		
	/*		
		public override Task<ICompletionDataList> CodeCompletionCommand (CodeCompletionContext completionContext)
		{
		//completion for ASP.NET expressions
			// TODO: Detect <script> state here !!!
			if (documentBuilder != null && Tracker.Engine.CurrentState is WebFormsExpressionState) {
				InitializeCodeCompletion ('\0');
				return documentBuilder.HandlePopupCompletion (defaultEditor, defaultDocumentContext, documentInfo, localDocumentInfo);
			}
			return base.CodeCompletionCommand (completionContext);
		}*/

		protected override void Initialize ()
		{
			base.Initialize ();
			defaultCompletionWidget = CompletionWidget;
			defaultDocumentContext = DocumentContext;
			defaultEditor = Editor;
			defaultDocumentContext.Saved += AsyncUpdateDesignerFile;
			OnParsedDocumentUpdated ();
		}

		void AsyncUpdateDesignerFile (object sender, EventArgs e)
		{
			if (project == null)
				return;

			var file = project.GetProjectFile (FileName);
			if (file == null)
				return;

			var designerFile = WebFormsCodeBehind.GetDesignerFile (file);
			if (designerFile == null)
				return;

			System.Threading.ThreadPool.QueueUserWorkItem (r => {
				using (var monitor = MonoDevelop.Ide.IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
					GettextCatalog.GetString ("Updating ASP.NET Designer File..."), null, false))
				{
					var writer = CodeBehindWriter.CreateForProject (monitor, project);
					var result = WebFormsCodeBehind.UpdateDesignerFile (writer, project, file, designerFile);
					//don't worry about reporting error's here for now
					//if the user wants to see errors, they can compile
					if (!result.Failed)
						writer.WriteOpenFiles ();
				}
			});
		}

		// TODO: Roslyn port
//		
//		public override ICompletionDataList HandleCodeCompletionAsync (
//		    CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
//		{
//			if (localDocumentInfo == null)
//				return base.HandleCodeCompletionAsync (completionContext, completionChar, ref triggerWordLength);
//			localDocumentInfo.HiddenDocument.Editor.InsertAtCaret (completionChar.ToString ());
//			return documentBuilder.HandleCompletion (defaultEditor, defaultDocumentContext, completionContext, documentInfo, localDocumentInfo, completionChar, ref triggerWordLength);
//		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			Tracker.UpdateEngine ();
			bool isAspExprState = Tracker.Engine.CurrentState is WebFormsExpressionState;
			if (documentBuilder == null || !isAspExprState)
				return base.KeyPress (descriptor);
			InitializeCodeCompletion ('\0');
//			DocumentContext = localDocumentInfo.HiddenDocument;
//			Editor = localDocumentInfo.HiddenDocument.Editor;
//			CompletionWidget = documentBuilder.CreateCompletionWidget (localDocumentInfo.HiddenDocument.Editor, localDocumentInfo.HiddenDocument, localDocumentInfo);
			bool result;
			try {
				result = base.KeyPress (descriptor);
				if (PropertyService.Get ("EnableParameterInsight", true) && (descriptor.KeyChar == ',' || descriptor.KeyChar == ')') && CanRunParameterCompletionCommand ()) {
					RunParameterCompletionCommand ();
				}
			} finally {
				DocumentContext = defaultDocumentContext;
				Editor = defaultEditor;
				CompletionWidget = defaultCompletionWidget;
			}
			return result;
		}

// TODO: Roslyn port
//		public override ParameterHintingResult HandleParameterCompletionAsync (CodeCompletionContext completionContext, char completionChar)
//		{
// /*			if (Tracker.Engine.CurrentState is AspNetExpressionState && documentBuilder != null && localDocumentInfo != null) {
//				return documentBuilder.HandleParameterCompletion (defaultDocument, completionContext, documentInfo, localDocumentInfo, completionChar);
//			}*/
//			
//			return base.HandleParameterCompletionAsync (completionContext, completionChar);
//		}

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

		protected override async Task<CompletionDataList> GetElementCompletions (CancellationToken token)
		{
			var list = new CompletionDataList ();
			XName parentName = GetParentElementName (0);

			INamedTypeSymbol controlClass = null;

			await refman.CreateCompilation (token);

			if (parentName.HasPrefix) {
				controlClass = refman.GetControlType (parentName.Prefix, parentName.Name);
			} else {
				XName grandparentName = GetParentElementName (1);
				if (grandparentName.IsValid && grandparentName.HasPrefix)
					controlClass = refman.GetControlType (grandparentName.Prefix, grandparentName.Name);
			}

			//we're just in HTML
			if (controlClass == null) {
				//root element?
				if (!parentName.IsValid) {
					if (aspDoc.Info.Subtype == WebSubtype.WebControl) {
						await AddHtmlTagCompletionData (list, Schema, new XName ("div"), token);
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
					list.AddRange (await base.GetElementCompletions (token));
				}
				return list;
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
				await AddHtmlTagCompletionData (list, Schema, new XName ("body"), token);
				return list;
			}
			
			//children of properties
			if (childrenAsProperties && (!parentName.HasPrefix || defaultProp != null)) {
				string propName = defaultProp ?? parentName.Name;
				IPropertySymbol property =
					controlClass.GetMembers ().OfType<IPropertySymbol> ()
						.FirstOrDefault (x => string.Equals (propName, x.Name, StringComparison.OrdinalIgnoreCase));
				
				if (property == null)
					return list;
				
				//sanity checks on attributes
				switch (GetPersistenceMode (property)) {
				case System.Web.UI.PersistenceMode.Attribute:
				case System.Web.UI.PersistenceMode.EncodedInnerDefaultProperty:
					return list;
					
				case System.Web.UI.PersistenceMode.InnerDefaultProperty:
					if (!parentName.HasPrefix)
						return list;
					break;
					
				case System.Web.UI.PersistenceMode.InnerProperty:
					if (parentName.HasPrefix)
						return list;
					break;
				}
				
				//check if allows freeform ASP/HTML content
				if (property.GetReturnType ().GetFullName () == "System.Web.UI.ITemplate") {
					AddAspBeginExpressions (list);
					AddMiscBeginTags (list);
					await AddHtmlTagCompletionData (list, Schema, new XName ("body"), token);
					list.AddRange (refman.GetControlCompletionData ());
					return list;
				}

				//FIXME:unfortunately ASP.NET doesn't seem to have enough type information / attributes
				//to be able to resolve the correct child types here
				//so we assume it's a list and have a quick hack to find arguments of strongly typed ILists
				
				ITypeSymbol collectionType = property.GetReturnType ();
				if (collectionType == null) {
					list.AddRange (refman.GetControlCompletionData ());
					return list;
				}

				string addStr = "Add";
				IMethodSymbol meth = collectionType.GetMembers ().OfType<IMethodSymbol> ().FirstOrDefault (m => m.Parameters.Length == 1 && m.Name == addStr);

				if (meth != null) {
					var argType = meth.Parameters [0].Type as INamedTypeSymbol;
					INamedTypeSymbol type = refman.GetTypeByMetadataName ("System.Web.UI.Control");
					if (argType != null && type != null && argType.IsDerivedFromClass (type)) {
						list.AddRange (refman.GetControlCompletionData (argType));
						return list;
					}
				}
				
				list.AddRange (refman.GetControlCompletionData ());
				return list;
			}
			
			//properties as children of controls
			if (parentName.HasPrefix && childrenAsProperties) {
				foreach (IPropertySymbol prop in GetUniqueMembers<IPropertySymbol> (controlClass.GetMembers ().OfType <IPropertySymbol> ()))
					if (GetPersistenceMode (prop) != System.Web.UI.PersistenceMode.Attribute)
						list.Add (prop.Name, prop.GetStockIcon (), Ambience.GetSummaryMarkup (prop));
			}
			return list;
		}
		
		protected override async Task<CompletionDataList> GetAttributeCompletions (
			IAttributedXObject attributedOb,
			Dictionary<string, string> existingAtts, CancellationToken token)
		{
			var list = (await base.GetAttributeCompletions (attributedOb, existingAtts, token)) ?? new CompletionDataList ();
			if (attributedOb is XElement) {
				
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
					await refman.CreateCompilation (token);
					AddAspAttributeCompletionData (list, attributedOb.Name, existingAtts);
				}
				
			} else if (attributedOb is WebFormsDirective) {
				return WebFormsDirectiveCompletion.GetAttributes (project, attributedOb.Name.FullName, existingAtts);
			}
			return list.Count > 0? list : null;
		}
		
		protected override async Task<CompletionDataList> GetAttributeValueCompletions (IAttributedXObject ob, XAttribute att, CancellationToken token)
		{
			var list = (await base.GetAttributeValueCompletions (ob, att, token)) ?? new CompletionDataList ();
			if (ob is XElement) {
				if (ob.Name.HasPrefix) {
					string id = ob.GetId ();
					if (string.IsNullOrEmpty (id) || string.IsNullOrEmpty (id.Trim ()))
						id = null;
					await refman.CreateCompilation (token);
					AddAspAttributeValueCompletionData (list, ob.Name, att.Name, id);
				}
			} else if (ob is WebFormsDirective) {
				return WebFormsDirectiveCompletion.GetAttributeValues (project, DocumentContext.Name, ob.Name.FullName, att.Name.FullName);
			}
			return list.Count > 0? list : null;
		}
		
		ClrVersion ProjClrVersion {
			get { return project == null? ClrVersion.Net_4_5 : project.TargetFramework.ClrVersion; }
		}
		
		CompletionDataList HandleExpressionCompletion (WebFormsExpression expr)
		{
			if (!(expr is WebFormsBindingExpression || expr is WebFormsRenderExpression))
				return null;

			INamedTypeSymbol codeBehindClass;
			if (!GetCodeBehind (out codeBehindClass))
				return null;
			
			//list just the class's properties, not properties on base types
			var list = new CompletionDataList ();
			list.AddRange (from p in codeBehindClass.GetMembers ().OfType<IPropertySymbol> ()
				where p.DeclaredAccessibility == Accessibility.Public
				select new AspAttributeCompletionData (p));
			list.AddRange (from p in codeBehindClass.GetMembers ().OfType<IFieldSymbol> ()
				where p.DeclaredAccessibility == Accessibility.Protected || p.DeclaredAccessibility == Accessibility.Public
				select new AspAttributeCompletionData (p));
			
			return list.Count > 0? list : null;
		}
		
		bool GetCodeBehind (out INamedTypeSymbol codeBehindClass)
		{
			if (HasDoc && !string.IsNullOrEmpty (aspDoc.Info.InheritedClass)) {
				codeBehindClass = refman.GetTypeByMetadataName (aspDoc.Info.InheritedClass);
				return codeBehindClass != null;
			}

			codeBehindClass = null;
			return false;
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

			//valid on 4.0+ runtime only
			if (ProjClrVersion != ClrVersion.Net_4_0) {
				list.Add ("%:", "md-literal", GettextCatalog.GetString ("ASP.NET HTML encoded expression"));
			}
		}
		
		void AddAspAttributeCompletionData (CompletionDataList list, XName name, Dictionary<string, string> existingAtts)
		{
			Debug.Assert (name.IsValid);
			Debug.Assert (name.HasPrefix);

			INamedTypeSymbol controlClass = refman.GetControlType (name.Prefix, name.Name);
			if(controlClass != null)
				AddControlMembers (list, controlClass, existingAtts);
		}

		void AddControlMembers (CompletionDataList list, INamedTypeSymbol controlClass, Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (var prop in GetUniqueMembers<IPropertySymbol> (GetAllMembers<IPropertySymbol> (controlClass)))
				if (prop.DeclaredAccessibility == Accessibility.Public && (existingAtts == null || !existingAtts.ContainsKey (prop.Name)))
					if (GetPersistenceMode (prop) == System.Web.UI.PersistenceMode.Attribute)
						list.Add (new AspAttributeCompletionData (prop));

			//similarly add events
			foreach (var eve in GetUniqueMembers<IEventSymbol> (GetAllMembers<IEventSymbol> (controlClass))) {
				string eveName = "On" + eve.Name;
				if (eve.DeclaredAccessibility == Accessibility.Public && (existingAtts == null || !existingAtts.ContainsKey (eveName)))
					list.Add (new AspAttributeCompletionData (eve, eveName));
			}
		}

		IEnumerable<T> GetAllMembers<T> (INamedTypeSymbol type)
		{
			INamedTypeSymbol currentType = type;
			while (currentType != null) {
				foreach (T member in currentType.GetMembers ().OfType<T> ())
					yield return member;

				currentType = currentType.BaseType;
			}
		}

		void AddAspAttributeValueCompletionData (CompletionDataList list, XName tagName, XName attName, string id)
		{
			Debug.Assert (tagName.IsValid && tagName.HasPrefix);
			Debug.Assert (attName.IsValid && !attName.HasPrefix);

			INamedTypeSymbol controlClass = refman.GetControlType (tagName.Prefix, tagName.Name);
			if (controlClass == null)
				return;
			
			//find the codebehind class
			INamedTypeSymbol codeBehindClass;
			GetCodeBehind (out codeBehindClass);

			//if it's an event, suggest compatible methods 
			if (codeBehindClass != null && attName.Name.StartsWith ("On", StringComparison.Ordinal)) {
				string eventName = attName.Name.Substring (2);
				
				foreach (IEventSymbol ev in controlClass.GetAccessibleMembersInThisAndBaseTypes<IEventSymbol> (controlClass)) {
					if (ev.Name == eventName) {
						var domMethod = BindingService.MDDomToCodeDomMethod (ev);
						if (domMethod == null)
							return;
						
						foreach (IMethodSymbol meth 
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
						        CodeBehind.GetNonDesignerClassLocation (codeBehindClass))
						    );
						return;
					}
				}
			}
			
			//if it's a property and is an enum or bool, suggest valid values
			foreach (IPropertySymbol prop in GetAllMembers<IPropertySymbol> (controlClass)) {
				if (prop.Name != attName.Name)
					continue;
				
				//boolean completion
				if (prop.GetReturnType ().Equals (refman.GetTypeByMetadataName ("System.Boolean"))) {
					AddBooleanCompletionData (list);
					return;
				}
				//color completion
				if (prop.GetReturnType ().Equals (refman.GetTypeByMetadataName ("System.Drawing.Color"))) {
					var conv = new System.Drawing.ColorConverter ();
					foreach (System.Drawing.Color c in conv.GetStandardValues (null)) {
						if (c.IsSystemColor)
							continue;
						string hexcol = string.Format ("#{0:x2}{1:x2}{2:x2}", c.R, c.G, c.B);
						list.Add (c.Name, hexcol);
					}
					return;
				}
				
				//enum completion
				var retCls = prop.GetReturnType () as INamedTypeSymbol;
				if (retCls != null && retCls.TypeKind == TypeKind.Enum) {
					foreach (var enumVal in GetAllMembers<IFieldSymbol> (retCls))
						if (enumVal.DeclaredAccessibility == Accessibility.Public && enumVal.IsStatic)
							list.Add (enumVal.Name, "md-literal", Ambience.GetSummaryMarkup (enumVal));
					return;
				}
			}
		}

		static IEnumerable<T> GetUniqueMembers<T> (IEnumerable<T> members) where T : ISymbol
		{
			var existingItems = new Dictionary<string,bool> ();
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
		static System.Web.UI.PersistenceMode GetPersistenceMode (IPropertySymbol prop)
		{
			foreach (var att in prop.GetAttributes ()) {
				if (att.AttributeClass.GetFullName () == "System.Web.UI.PersistenceModeAttribute") {
					var expr = att.ConstructorArguments.FirstOrDefault ();
					if (expr.IsNull) {
						LoggingService.LogWarning ("Unknown expression type {0} in Attribute parameter", expr);
						return System.Web.UI.PersistenceMode.Attribute;
					}
					
					return (System.Web.UI.PersistenceMode) expr.Value;
				}
				if (att.AttributeClass.GetFullName () == "System.Web.UI.TemplateContainerAttribute") {
					return System.Web.UI.PersistenceMode.InnerProperty;
				}
			}
			return System.Web.UI.PersistenceMode.Attribute;
		}

		static bool AreChildrenAsProperties (INamedTypeSymbol type, out string defaultProperty)
		{
			bool childrenAsProperties = false;
			defaultProperty = "";
			
			AttributeData att = GetAttributes (type, "System.Web.UI.ParseChildrenAttribute").FirstOrDefault ();

			if (att == null)
				return childrenAsProperties;

			var posArgs = att.ConstructorArguments;
			if (posArgs.Length == 0)
				return childrenAsProperties;
			
			if (posArgs.Length > 0) {
				var expr = posArgs [0];
				if (expr.IsNull) {
					LoggingService.LogWarning ("Unknown expression type {0} in Attribute parameter", expr);
					return false;
				}
				
				if (expr.Value is bool) {
					childrenAsProperties = (bool)expr.Value;
				} else {
					//TODO: implement this
					LoggingService.LogWarning ("ASP.NET completion does not yet handle ParseChildrenAttribute (Type)");
					return false;
				}
			}
			
			if (posArgs.Length > 1) {
				var expr = posArgs [1];
				if (expr.IsNull || !(expr.Value is string)) {
					LoggingService.LogWarning ("Unknown expression '{0}' in IAttribute parameter", expr);
					return false;
				}
				defaultProperty = (string)expr.Value;
			}
			
			var namedArgs = att.NamedArguments;
			if (namedArgs.Length > 0) {
				if (namedArgs.Any (p => p.Key == "ChildrenAsProperties")) {
					var expr = namedArgs.First (p => p.Key == "ChildrenAsProperties").Value;
					if (expr.IsNull) {
						LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", expr);
						return false;
					}
					childrenAsProperties = (bool)expr.Value;
				}
				if (namedArgs.Any (p => p.Key == "DefaultProperty")) {
					var expr = namedArgs.First (p => p.Key == "DefaultProperty").Value;
					if (expr.IsNull) {
						LoggingService.LogWarning ("Unknown expression type {0} in IAttribute parameter", expr);
						return false;
					}
					defaultProperty = (string)expr.Value;
				}
				if (namedArgs.Any (p => p.Key == "ChildControlType")) {
					//TODO: implement this
					LoggingService.LogWarning ("ASP.NET completion does not yet handle ParseChildrenAttribute (Type)");
					return false;
				}
			}
			
			return childrenAsProperties;
		}
		
		static IEnumerable<AttributeData> GetAttributes (INamedTypeSymbol type, string attName)
		{
			foreach (AttributeData att in type.GetAttributes()) {
				if (att.AttributeClass.GetFullName () == attName)
					yield return att;
			}
			
			foreach (INamedTypeSymbol t2 in type.GetAllBaseClasses ()) {
				foreach (AttributeData att in t2.GetAttributes ())
					if (att.AttributeClass.GetFullName () == attName)
						yield return att;
			}
		}
		#endregion
	}
}
