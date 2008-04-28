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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.AspNet;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Html;
using MonoDevelop.DesignerSupport;

using MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.StateEngine;

namespace MonoDevelop.AspNet.Gui
{
	
	
	public class AspNetEditorExtension : CompletionTextEditorExtension, IPathedDocument
	{
		DocumentStateTracker<Parser<AspNetFreeState>> tracker;
		
		public AspNetEditorExtension () : base ()
		{
		}
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			string[] supportedExtensions = {".aspx", ".ascx", ".master"};
			return (doc.Project is AspNetAppProject) && Array.IndexOf (supportedExtensions, System.IO.Path.GetExtension (doc.Title)) > -1;
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			tracker = new DocumentStateTracker<Parser<AspNetFreeState>> (Editor);
			
			//ensure that the schema service is initialised, or code completion may take a couple of seconds to trigger
			HtmlSchemaService.Initialise (false);
		}
		
		protected Document GetAspNetDocument ()
		{
			return new Document (Buffer, Document.Project, FileName);
		}
		
		protected ITextBuffer Buffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return Document.GetContent<ITextBuffer> ();
			}
		}
		
		protected IEditableTextBuffer EditableBuffer {
			get {
				if (Document == null)
					throw new InvalidOperationException ("Editor extension not yet initialized");
				return Document.GetContent<IEditableTextBuffer> ();
			}
		}
			
		public override ICompletionDataProvider CodeCompletionCommand (ICodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			string txt = Editor.GetText (pos - 1, pos);
			int triggerWordLength = 0;
			ICompletionDataProvider cp = null;
			if (txt.Length > 0)
				cp = HandleCodeCompletion ((CodeCompletionContext) completionContext, true, ref triggerWordLength);
			
			return cp;
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			int pos = completionContext.TriggerOffset;
			if (pos > 0 && Editor.GetCharAt (pos - 1) == completionChar) {
				return HandleCodeCompletion ((CodeCompletionContext) completionContext, false, ref triggerWordLength);
			}
			return null;
		}
		
		ICompletionDataProvider HandleCodeCompletion (CodeCompletionContext completionContext, bool forced, ref int triggerWordLength)
		{
			tracker.UpdateEngine ();
			
			//FIXME: lines in completionContext are zero-indexed, but ILocation and buffer are 1-indexed. This could easily cause bugs.
			int line = completionContext.TriggerLine + 1, col = completionContext.TriggerLineOffset;
			
			ITextBuffer buf = this.Buffer;
			
			// completionChar may be a space even if the current char isn't, when ctrl-space is fired t
			int currentPosition = buf.CursorPosition - 1;
			char currentChar = buf.GetCharAt (currentPosition);
			char previousChar = buf.GetCharAt (currentPosition - 1);
			
			LoggingService.LogDebug ("Attempting ASP.NET completion for state '{0}', previousChar='{1}', currentChar='{2}', forced='{3}'", tracker.Engine.CurrentState, previousChar, currentChar, forced);
			
			//doctype completion
			if (line <= 5 && currentChar ==' ' && previousChar == 'E') {
				int start = currentPosition - 9;
				if (start >= 0) {
					string readback = Buffer.GetText (start, currentPosition);
					if (string.Compare (readback, "<!DOCTYPE", System.StringComparison.InvariantCulture) == 0)
						return new DocTypeCompletionDataProvider ();
				}
			}
			
			//decide whether completion will be activated, to avoid unnecessary
			//parsing, which hurts editor responsiveness
			if (!forced) {
				switch (currentChar) {
				case '>':
				case '<':
					break;
				default:
					if (char.IsLetterOrDigit (currentChar) &&
					    (previousChar == '"' || previousChar =='\'' || previousChar == '<' || char.IsWhiteSpace (previousChar)))
						break;
					else
						return null;
				}
			}
			
			Document doc;
			try {
				doc = GetAspNetDocument ();
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error in ASP.NET parser", ex);
				return null;
			}
			
			//lazily load the schema to avoid a multi-second interruption when a schema
			//is first used. While loading, fall back to the default schema (which is pre-loaded) so that
			//the user still gets completion
			HtmlSchema schema = null;
			if (!string.IsNullOrEmpty (doc.Info.DocType)) {
				schema = HtmlSchemaService.GetSchema (doc.Info.DocType, true);
			}
			if (schema == null)
				schema = HtmlSchemaService.DefaultDocType;
			LoggingService.LogDebug ("AspNetCompletion using completion for doctype {0}", schema.Name);
			
			//determine the code at the current location
			Node n = doc.RootNode.GetNodeAtPosition (line, col);
			LoggingService.LogDebug ("AspNetCompletion({0},{1}): {2}", line, col, n==null? "(not found)" : n.ToString ());
			
			//tag completion
			if (currentChar == '<') {
				CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
				TagNode parent = null;
				if (n != null)
					parent = n.Parent as TagNode;
				
				AddHtmlTagCompletionData (cp, schema, parent);
				AddAspTags (cp, doc, parent == null? null : parent.TagName);
				AddParentCloseTags (cp, parent);
				AddAspBeginExpressions (cp, doc);
				
				if (line < 4 && string.IsNullOrEmpty (doc.Info.DocType))
					cp.AddCompletionData (new CodeCompletionData ("!DOCTYPE", "md-literal"));
				return cp;
			}
			
			TagNode tn = n as TagNode;
			
			//determine whether we're in an attribute's quotes
			bool isInQuotes = false;
			if (tn != null && tn.LocationContainsPosition (line, col) == 0) {
				int startPosition = buf.GetPositionFromLineColumn (tn.Location.BeginLine, tn.Location.BeginColumn);
				if (startPosition > -1)
					isInQuotes = IsInOpenQuotes (buf, startPosition + 1, currentPosition);
			}
			
			//closing tag completion
			if (!isInQuotes && currentPosition - 1 > 0 && currentChar == '>') {
				//get previous node in document
				int linePrev, colPrev;
				buf.GetLineColumnFromPosition (currentPosition - 1, out linePrev, out colPrev);
				TagNode tnPrev = doc.RootNode.GetNodeAtPosition (linePrev, colPrev) as TagNode;
				LoggingService.LogDebug ("AspNetCompletionPrev({0},{1}): {2}", linePrev, colPrev, tnPrev==null? "(not found)" : tnPrev.ToString ());
				
				if (tnPrev != null && !string.IsNullOrEmpty (tnPrev.TagName) && !tnPrev.IsClosed) {
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
					cp.AddCompletionData (
					    new MonoDevelop.XmlEditor.Completion.XmlTagCompletionData (
					        String.Concat ("</", tnPrev.TagName, ">"), 0, true)
					    );
					return cp;
				}
			}
			
			//attributes names within tags
			if (tracker.Engine.CurrentState is XmlTagState && forced || 
			    tracker.Engine.CurrentState is XmlAttributeState)
			{
				XmlTagState tagState = (XmlTagState) ((tracker.Engine.CurrentState is XmlAttributeState)? 
					tracker.Engine.CurrentState.Parent : 
					tracker.Engine.CurrentState);
				
				//attributes
				if (!string.IsNullOrEmpty (tagState.Name.Name)
				    && (forced || (char.IsWhiteSpace (previousChar) && char.IsLetter (currentChar))))
				{
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
					if (!forced)
						triggerWordLength = 1;
					
					if (string.IsNullOrEmpty (tagState.Name.Namespace)) {
						AddHtmlAttributeCompletionData (cp, schema, tagState.Name.Name, null);
					} else {
						AddAspAttributeCompletionData (cp, doc, tagState.Name.Namespace, tagState.Name.Name, null);
					}
					
					if (true)
						cp.AddCompletionData (new CodeCompletionData ("runat=\"server\"", "md-literal",
						    GettextCatalog.GetString ("Required for ASP.NET controls.\n") +
						    GettextCatalog.GetString (
						        "Indicates that this tag should be able to be\n" +
						        "manipulated programmatically on the web server.")
						    ));
					
//					if (tn.Attributes["id"] == null)
//						cp.AddCompletionData (
//						    new CodeCompletionData ("id", "md-literal",
//						        GettextCatalog.GetString ("Unique identifier.\n") +
//						        GettextCatalog.GetString (
//						            "An identifier that is unique within the document.\n" + 
//						            "If the tag is a server control, this will be used \n" +
//						            "for the corresponding variable name in the CodeBehind.")
//						    ));
					return cp;
					
				//attribute values
				//determine whether to trigger completion within attribute values quotes
				}
			}
			
			if (tracker.Engine.CurrentState is XmlAttributeValueState) {
				XmlAttributeValueState valstate = (XmlAttributeValueState) tracker.Engine.CurrentState;
				
				if ((forced && (currentChar == '"' || currentChar == '\'') 
				    && currentPosition + 1 < buf.Length && buf.GetCharAt (currentPosition + 1) == currentChar)
				    || ((previousChar == '"' || previousChar == '\'') 
				        && currentPosition + 1 < buf.Length && buf.GetCharAt (currentPosition + 1) == previousChar))
				{
					if (!forced)
						triggerWordLength = 1;
					
					string att = valstate.AttributeName;
					IXmlName tagName = valstate.TagName;
					if (!string.IsNullOrEmpty (att) && !string.IsNullOrEmpty (tagName.Name))
					{
						CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
						if (string.IsNullOrEmpty (tagName.Namespace))
							AddHtmlAttributeValueCompletionData (cp, schema, valstate.TagName.Name, att);
						else
							AddAspAttributeValueCompletionData (cp, doc, valstate.TagName.Namespace, valstate.TagName.Name, att, null);
						return cp;
					}
				}
			}
			
			return null; 
		}
		
		#region String processing
		
		static bool IsInOpenQuotes (ITextBuffer buffer, int startOffset, int endOffset)
		{
			char openChar = '\0';
			for (int i = startOffset; i <= endOffset; i++) {
				char c = buffer.GetCharAt (i);
				if (c == '"' || c == '\'')
					openChar = (openChar == c)? '\0' : c;
			}
			return (openChar != '\0');
		}
		
		static string GetAttributeName (ITextBuffer buf, int offset)
		{
			int start = -1, end = -1;
			int i = offset;
			for (; i > 0; i--) {
				char c = buf.GetCharAt (i);
				if (!char.IsWhiteSpace (c) && c != '=') {
					end = i;
					break;
				}
			}
			i--;
			for (; i > 0; i--) {
				char c = buf.GetCharAt (i);
				if (!char.IsLetterOrDigit (c)) {
					start = i;
					break;
				}
			}
			
			if (end - start > 0)
				return (buf.GetText (start + 1, end + 1));
			return null;
		}
		
		#endregion
		
		#region HTML data
		
		void AddHtmlTagCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, TagNode parentTag)
		{
			if (schema == null || schema.CompletionProvider == null)
				return;
			
			ICompletionData[] data = null;
			if (parentTag == null || string.IsNullOrEmpty (parentTag.TagName)) {
				data = schema.CompletionProvider.GetElementCompletionData ();
			} else {
				data = schema.CompletionProvider.GetChildElementCompletionData (parentTag.TagName);
			}
			
			foreach (ICompletionData datum in data)
				provider.AddCompletionData (datum);			
		}
		
		void AddHtmlAttributeCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, string tagName, Dictionary<string, bool> existingAtts)
		{
			System.Diagnostics.Debug.Assert (!string.IsNullOrEmpty (tagName));
			System.Diagnostics.Debug.Assert (schema != null);
			System.Diagnostics.Debug.Assert (schema.CompletionProvider != null);
			
			//add atts only if they're not aready in the tag
			foreach (ICompletionData datum in schema.CompletionProvider.GetAttributeCompletionData (tagName))
				if (existingAtts == null || existingAtts.ContainsKey (datum.Text[0]))
					provider.AddCompletionData (datum);
		}
		
		void AddHtmlAttributeValueCompletionData (CodeCompletionDataProvider provider, HtmlSchema schema, string tagName, string attributeName)
		{
			System.Diagnostics.Debug.Assert (!string.IsNullOrEmpty (tagName));
			System.Diagnostics.Debug.Assert (schema.CompletionProvider != null);
			
			foreach (ICompletionData datum in schema.CompletionProvider.GetAttributeValueCompletionData (tagName, attributeName))
				provider.AddCompletionData (datum);
		}
		
		#endregion
		
		#region ASP.NET data
		
		void AddAspBeginExpressions (CodeCompletionDataProvider provider, Document doc)
		{
			provider.AddCompletionData (
			    new CodeCompletionData ("%",  "md-literal", GettextCatalog.GetString ("ASP.NET render block"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("%=", "md-literal", GettextCatalog.GetString ("ASP.NET render expression"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("%@", "md-literal", GettextCatalog.GetString ("ASP.NET directive"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("%#", "md-literal", GettextCatalog.GetString ("ASP.NET databinding expression"))
			    );
			provider.AddCompletionData (
			    new CodeCompletionData ("%--", "md-literal", GettextCatalog.GetString ("ASP.NET server-side comment"))
			    );
			
			//valid on 2.0 runtime only
			if (doc.Project == null || doc.Project.ClrVersion == ClrVersion.Net_2_0
			    || doc.Project.ClrVersion == ClrVersion.Default) {
				provider.AddCompletionData (
				    new CodeCompletionData ("%$", "md-literal", GettextCatalog.GetString ("ASP.NET resource expression"))
				    );
			}
		}
		
		void AddAspTags (CodeCompletionDataProvider provider, Document doc, string parentTag)
		{
			foreach (MonoDevelop.Projects.Parser.IClass cls in doc.ReferenceManager.ListControlClasses ())
				provider.AddCompletionData (new CodeCompletionData ("asp:" + cls.Name, Gtk.Stock.GoForward, cls.Documentation));
		}
		
		void AddAspAttributeCompletionData (CodeCompletionDataProvider provider, Document doc, string prefix, string name, Dictionary<string, string> existingAtts)
		{
			System.Diagnostics.Debug.Assert (!string.IsNullOrEmpty (name));
			System.Diagnostics.Debug.Assert (!string.IsNullOrEmpty (prefix));
			System.Diagnostics.Debug.Assert (doc != null);
			
			//get a parser context
			MonoDevelop.Projects.Parser.IParserContext ctx = null;
			if (doc.Project != null)
				ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (doc.Project);
			else
				//FIXME use correct runtime
				ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetAssemblyParserContext ("System.Web");
			if (ctx == null) {
				LoggingService.LogWarning ("Could not obtain parser context in AddAspAttributeCompletionData");
				return;
			}
			
			MonoDevelop.Projects.Parser.IClass controlClass = doc.ReferenceManager.GetControlType (prefix, name);
			if (controlClass == null) {
				controlClass = ctx.GetClass ("System.Web.UI.WebControls.WebControl");
				if (controlClass == null) {
					LoggingService.LogWarning ("Could not obtain IClass for System.Web.UI.WebControls.WebControl");
					return;
				}
			}
			
			AddControlMembers (provider, ctx, doc, controlClass, existingAtts);
		}
		
		void AddControlMembers (CodeCompletionDataProvider provider, MonoDevelop.Projects.Parser.IParserContext ctx, Document doc, MonoDevelop.Projects.Parser.IClass controlClass, Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (MonoDevelop.Projects.Parser.IProperty prop in GetUniqueMembers<MonoDevelop.Projects.Parser.IProperty> (GetAllProperties (ctx, controlClass)))
				if (prop.IsPublic && (existingAtts == null || existingAtts.ContainsKey (prop.Name)))
					provider.AddCompletionData (new CodeCompletionData (prop.Name, "md-property", prop.Documentation));
			
			//similarly add events
			foreach (MonoDevelop.Projects.Parser.IEvent eve in GetUniqueMembers<MonoDevelop.Projects.Parser.IEvent> (GetAllEvents (ctx, controlClass))) {
				string eveName = "On" + eve.Name;
				if (eve.IsPublic && (existingAtts == null || existingAtts.ContainsKey (eveName)))
					provider.AddCompletionData (new CodeCompletionData (eveName, "md-event", eve.Documentation));
			}
		}
		
		void AddAspAttributeValueCompletionData (CodeCompletionDataProvider provider, Document doc, string tagNamePrefix, string tagName, string attrib, Dictionary<string, string> existingAtts)
		{
			if (string.IsNullOrEmpty (attrib) || string.IsNullOrEmpty (tagName) || string.IsNullOrEmpty (tagNamePrefix))
				return;
			
			MonoDevelop.Projects.Parser.IClass controlClass = doc.ReferenceManager.GetControlType (tagNamePrefix, tagName);
			if (controlClass == null) {
				//FIXME: respect runtime version
				MonoDevelop.Projects.Parser.IParserContext sysWebContext = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetAssemblyParserContext ("System.Web");
				if (sysWebContext == null)
					return;
				
				controlClass = sysWebContext.GetClass ("System.Web.UI.WebControls.WebControl");
				if (controlClass == null)
					LoggingService.LogWarning ("Could not obtain IClass for System.Web.UI.WebControls.WebControl");
			}
			
			//find the codebehind class
			MonoDevelop.Projects.Parser.IClass codeBehindClass = null;
			MonoDevelop.Projects.Parser.IParserContext projectContext = null;
			if (doc.Project != null)
				projectContext = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (doc.Project);
			if (projectContext != null && !string.IsNullOrEmpty (doc.Info.InheritedClass))
				codeBehindClass = projectContext.GetClass (doc.Info.InheritedClass);
			
			//if it's an event, suggest compatible methods 
			if (codeBehindClass != null && attrib.StartsWith ("On")) {
				string eventName = attrib.Substring (2);
				foreach (MonoDevelop.Projects.Parser.IEvent ev in GetAllEvents (projectContext, controlClass)) {
					if (ev.Name == eventName) {
						System.CodeDom.CodeMemberMethod domMethod = BindingService.MDDomToCodeDomMethod (ev, projectContext);
						if (domMethod == null)
							return;
						
						foreach (string meth in BindingService.GetCompatibleMethodsInClass (codeBehindClass, domMethod))
							provider.AddCompletionData (new CodeCompletionData (meth, "md-method",
							    "A compatible method in the CodeBehind class"));
						
						string suggestedIdentifier = ev.Name;
						if (existingAtts != null && !existingAtts.ContainsKey ("id") && !string.IsNullOrEmpty (existingAtts["id"]))
							suggestedIdentifier = existingAtts["id"] + "_" + suggestedIdentifier;
							
						domMethod.Name = BindingService.GenerateIdentifierUniqueInClass (codeBehindClass, suggestedIdentifier);
						provider.AddCompletionData (
						    new SuggestedHandlerCompletionData (doc.Project, domMethod, codeBehindClass,
						        MonoDevelop.AspNet.CodeBehind.GetNonDesignerClass (codeBehindClass))
						    );
						return;
					}
				}
			}
			
			//FIXME: respect runtime version
			if (projectContext == null)
				projectContext = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetAssemblyParserContext ("System.Web");
			if (projectContext == null)
				return;
			
			//if it's a property and is an enum or bool, suggest valid values
			foreach (MonoDevelop.Projects.Parser.IProperty prop in GetAllProperties (projectContext, controlClass)) {
				if (prop.Name != attrib)
					continue;
				
				//boolean completion
				if (prop.ReturnType.FullyQualifiedName == "System.Boolean") {
					AddBooleanCompletionData (provider);
					return;
				}
				
				//color completion
				if (prop.ReturnType.FullyQualifiedName == "System.Drawing.Color") {
					System.Drawing.ColorConverter conv = new System.Drawing.ColorConverter ();
					foreach (System.Drawing.Color c in conv.GetStandardValues (null))
						provider.AddCompletionData (new CodeCompletionData (c.Name, "md-literal"));
					return;
				}
				
				//enum completion
				MonoDevelop.Projects.Parser.IClass retCls = projectContext.GetClass (prop.ReturnType.FullyQualifiedName, true, false);
				if (retCls != null && retCls.ClassType == MonoDevelop.Projects.Parser.ClassType.Enum) {
					foreach (MonoDevelop.Projects.Parser.IField enumVal in retCls.Fields)
						if (enumVal.IsPublic && enumVal.IsStatic)
							provider.AddCompletionData (new CodeCompletionData (enumVal.Name, "md-literal", enumVal.Documentation));
					return;
				}
			}
		}
		
		IEnumerable<T> GetUniqueMembers<T> (IEnumerable<T> members) where T : MonoDevelop.Projects.Parser.IMember
		{
			Dictionary <string, bool> existingItems = new Dictionary<string,bool> ();
			foreach (T item in members) {
				if (existingItems.ContainsKey (item.Name))
					continue;
				existingItems[item.Name] = true;
				yield return item;
			}
		}
		
		IEnumerable<MonoDevelop.Projects.Parser.IProperty> GetAllProperties (MonoDevelop.Projects.Parser.IParserContext ctx, MonoDevelop.Projects.Parser.IClass cls)
		{
			foreach (MonoDevelop.Projects.Parser.IProperty prop in cls.Properties)
				yield return prop;
			
			foreach (MonoDevelop.Projects.Parser.IReturnType rt in cls.BaseTypes) {
				MonoDevelop.Projects.Parser.IClass baseCls = ctx.GetClass (rt.FullyQualifiedName);
				if (baseCls != null)
					foreach (MonoDevelop.Projects.Parser.IProperty prop in GetAllProperties (ctx, baseCls))
					    yield return prop;
			}
		}
		
		IEnumerable<MonoDevelop.Projects.Parser.IEvent> GetAllEvents (MonoDevelop.Projects.Parser.IParserContext ctx, MonoDevelop.Projects.Parser.IClass cls)
		{
			foreach (MonoDevelop.Projects.Parser.IEvent ev in cls.Events)
				yield return ev;
			
			foreach (MonoDevelop.Projects.Parser.IReturnType rt in cls.BaseTypes) {
				MonoDevelop.Projects.Parser.IClass baseCls = ctx.GetClass (rt.FullyQualifiedName, true, false);
				if (baseCls != null)
					foreach (MonoDevelop.Projects.Parser.IEvent ev in GetAllEvents (ctx, baseCls))
					    yield return ev;
			}
		}
		
		void AddBooleanCompletionData (CodeCompletionDataProvider provider)
		{
			provider.AddCompletionData (new CodeCompletionData ("true", "md-literal"));
			provider.AddCompletionData (new CodeCompletionData ("false", "md-literal"));
		}
		
		#endregion
		
		//walk up parents to root node, and add close tags for unclosed parents
		void AddParentCloseTags (CodeCompletionDataProvider provider, Node parentTag)
		{
			Node node = parentTag;
			while (node != null) {
				TagNode tag = node as TagNode;
				if (tag != null && !tag.IsClosed)
					provider.AddCompletionData (new CodeCompletionData (
					    "/" + tag.TagName + ">",
					    Gtk.Stock.GoBack,
					    "Closing tag for '" + tag.TagName + "'")
					);
				node = node.Parent;
			}
		}
		
		public override void CursorPositionChanged ()
		{
			UpdatePath ();
		}

		
		#region IPathedDocument
		
		string[] currentPath;
		int selectedPathIndex;

		public void SelectPath (int depth)
		{
			throw new NotImplementedException();
		}

		public void SelectPathContents (int depth)
		{
			throw new NotImplementedException();
		}

		
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		
		protected void OnPathChanged (string[] oldPath, int oldSelectedIndex)
		{
			if (PathChanged != null)
				PathChanged (this, new DocumentPathChangedEventArgs (oldPath, oldSelectedIndex));
		}
		
		void UpdatePath ()
		{
			this.tracker.UpdateEngine ();
			List<string> path = new List<string> ();
			
			//if current state is a name, walk onwards to complete it
			State s = this.tracker.Engine.CurrentState;
			if (s is XmlTagNameState) {
				XmlTagNameState tns = (XmlTagNameState) s.DeepCopy (false);
				int pos = this.tracker.Engine.Position;
				while (true) {
					char c = Editor.GetCharAt (pos);
					//text editor may update cursor before inserting chars, so avoid exceptions
					if (tns.StartLocation == pos && !char.IsLetter (c))
						break;
					State ret = tns.PushChar (c, pos);
					pos++;
					if (tns.Complete) {
						path.Add (tns.FullName);
						//skip beyond the parent XmlTagState, as it's incomplete
						while (s != null && !(s is XmlTagState)) {
							s = s.Parent;
						}
						s = s.Parent;
						break;
					}
					if (ret != null && ret != tns)
						break;
				}
			}
			
			//walk up named parents, addin to list
			while (s != null) {
				if (s is XmlTagState)
					path.Add (((XmlTagState)s).Name.FullName);
				s = s.Parent;
			}
			
			path.Reverse ();
			
			string[] oldPath = currentPath;
			int oldIndex = selectedPathIndex;
			currentPath = path.ToArray ();
			selectedPathIndex = currentPath.Length - 1;
			
			OnPathChanged (oldPath, oldIndex);
		}
		
		public string[] CurrentPath {
			get { return currentPath; }
		}
		
		public int SelectedIndex {
			get { return selectedPathIndex; }
		}
		
		#endregion
	}
}
