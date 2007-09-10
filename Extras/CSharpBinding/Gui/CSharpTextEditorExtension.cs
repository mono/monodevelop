using System;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui.Completion;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.FormattingStrategy;

using CSharpBinding.Parser;
using CSharpBinding.FormattingStrategy;

using MonoDevelop.Projects.Ambience;
using Ambience_ = MonoDevelop.Projects.Ambience.Ambience;

namespace CSharpBinding
{
	public class CSharpTextEditorExtension: CompletionTextEditorExtension
	{
		CSharpIndentEngine engine;
		
		public CSharpTextEditorExtension () : base ()
		{
			engine = new CSharpIndentEngine ();
		}
		
		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			return System.IO.Path.GetExtension (doc.Title) == ".cs";
		}
		
		IClass LookupClass (ICompilationUnit unit, int line, int column)
		{
			int classStartLine = int.MaxValue;
			IClass result = null;
			foreach (IClass c in unit.Classes) {
				if (c.BodyRegion.IsInside (line, column)) {
					classStartLine = c.Region.BeginLine;
					result = c;
				}
			}
			return result;
		}
		
		void AppendSummary (StringBuilder sb)
		{
			Debug.Assert (sb != null);
			sb.Append ("/ <summary>\n");
			sb.Append (engine.ThisLineIndent);
			sb.Append ("/// \n");
			sb.Append (engine.ThisLineIndent);
			sb.Append ("/// </summary>");
		}
		
		string GenerateBody (IClass c, int line)
		{
			int startLine = int.MaxValue;
			object result = null;
			
			foreach (IMethod m in c.Methods) {
				if (m.Region.BeginLine < startLine && m.Region.BeginLine > line) {
					startLine = m.Region.BeginLine;
					result = m;
				}
			}
			foreach (IProperty p in c.Properties) {
				if (p.Region.BeginLine < startLine && p.Region.BeginLine > line) {
					startLine = p.Region.BeginLine;
					result = p;
				}
			}
			
			StringBuilder builder = new StringBuilder ();
			
			IMethod method = result as IMethod;
			if (method != null) {
				AppendSummary (builder);
				
				if (method.Parameters != null) {
					foreach (IParameter para in method.Parameters) {
						builder.Append (Environment.NewLine);
						builder.Append (engine.ThisLineIndent);
						builder.Append ("/// <param name=\"");
						builder.Append (para.Name);
						builder.Append ("\">\n");
						builder.Append (engine.ThisLineIndent);
						builder.Append ("/// A <see cref=\"");
						builder.Append (para.ReturnType.FullyQualifiedName);
						builder.Append ("\"/>\n");
						builder.Append (engine.ThisLineIndent);
						builder.Append ("///</param>");
					}
				}
				if (method.ReturnType != null && method.ReturnType.FullyQualifiedName != "System.Void") {
					builder.Append (Environment.NewLine);
					builder.Append (engine.ThisLineIndent);
					builder.Append("/// <returns>\n");
					builder.Append (engine.ThisLineIndent);
					builder.Append ("/// A <see cref=\"");
					builder.Append (method.ReturnType.FullyQualifiedName);
					builder.Append ("\"/>\n");
					builder.Append (engine.ThisLineIndent);
					builder.Append ("///</returns>");
				}

			}
			IProperty property = result as IProperty;
			if (property != null) {
				builder.Append ("/ <value>\n");
				builder.Append (engine.ThisLineIndent);
				builder.Append ("/// \n");
				builder.Append (engine.ThisLineIndent);
				builder.Append ("/// </value>");
			}
			
			return builder.ToString ();
		}
		
		bool IsInsideClassBody (IClass insideClass, int line, int column)
		{
			foreach (IMethod m in insideClass.Methods) {
				if (m.BodyRegion.IsInside (line, column)) {
					return false;
				}
			}
			
			foreach (IProperty p in insideClass.Properties) {
				if (p.BodyRegion.IsInside (line, column)) {
					return false;
				}
			}
			foreach (IIndexer p in insideClass.Indexer) {
				if (p.BodyRegion.IsInside (line, column)) {
					return false;
				}
			}
			return true;
		}
		
		bool MayNeedComment (int line, int cursor)
		{
			bool inComment = Editor.GetCharAt (cursor - 1) == '/' && Editor.GetCharAt (cursor - 2) == '/';
			
			if (inComment) {
				for (int l = line - 1; l >= 0; l--) {
					string text = Editor.GetLineText (l).Trim (); 
					if (text.StartsWith ("///"))
						return false;
					if (!String.IsNullOrEmpty (text))
						break;
				}
				for (int l = line + 1; l < line + 100; l++) {
					string text = Editor.GetLineText (l).Trim (); 
					if (text.StartsWith ("///"))
						return false;
					if (!String.IsNullOrEmpty (text))
						break;
				}
				return true;
			}
			return false;
		}
		
		public override bool KeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			bool reindent = false, insert = true;
			int nInserted, bufLen, cursor;
			string newIndent;
			char ch, c;
			
			// This code is for Smart Indent, no-op for any other indent style
			if (TextEditorProperties.IndentStyle != IndentStyle.Smart)
				return base.KeyPress (key, modifier);
			
			switch (key) {
			case Gdk.Key.greater:
				
				cursor = Editor.SelectionStartPosition;
				c = '>';
				if (IsInsideDocumentationComment (Editor.SelectionStartPosition)) {
					int lin, col;
					Editor.GetLineColumnFromPosition (Editor.CursorPosition, out lin, out col);
					string lineText = Editor.GetLineText (lin);
					int startIndex = Math.Min (col - 1, lineText.Length - 1);
					
					while (startIndex >= 0 && lineText[startIndex] != '<') {
						--startIndex;
						if (lineText[startIndex] == '/') { // already closed.
							startIndex = -1;
							break;
						}
					}
					if (startIndex >= 0) {
						int endIndex = startIndex;
						while (endIndex <= col && endIndex < lineText.Length && !Char.IsWhiteSpace (lineText[endIndex])) {
							endIndex++;
						}
						string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring (startIndex + 1, endIndex - startIndex - 1) : null;
						if (!String.IsNullOrEmpty (tag) && commentTags.IndexOf (tag) >= 0) {
							Editor.InsertText (cursor, "></" + tag + ">");
							Editor.CursorPosition = cursor + 1; 
							insert = false;
						}
					}
				}
				break;
				
			case Gdk.Key.KP_Divide:
			case Gdk.Key.slash:
				cursor = Editor.SelectionStartPosition;
				c = '/';
				if (cursor < 2)
					break;
				int lin, col;
				Editor.GetLineColumnFromPosition (Editor.CursorPosition, out lin, out col);
				
				if (MayNeedComment (lin, cursor)) {
					StringBuilder generatedComment = new StringBuilder ();
					bool generateStandardComment = true;
					IParserContext pctx = GetParserContext ();
					ICompilationUnit unit = pctx.GetParseInformation (this.FileName).BestCompilationUnit as ICompilationUnit;
					if (unit != null) {
						IClass insideClass = LookupClass (unit, lin, col);
						if (insideClass != null) {
							if (!IsInsideClassBody (insideClass, lin, col))
								break;
							string body = GenerateBody (insideClass, lin);
							if (!String.IsNullOrEmpty (body)) {
								generatedComment.Append (body);
								generateStandardComment = false;
							}
						}
					}
					if (generateStandardComment) {
						AppendSummary (generatedComment);
					}
					
					Editor.InsertText (cursor, generatedComment.ToString ());
					reindent = true; 
					insert = false;
				}
				break;
			case Gdk.Key.KP_Enter:
			case Gdk.Key.Return:
				if (Editor.SelectionEndPosition > Editor.SelectionStartPosition)
					return base.KeyPress (key, modifier);
				
				reindent = true;
				insert = false;
				c = '\n';
				break;
			case Gdk.Key.Tab:
				if (Editor.SelectionEndPosition > Editor.SelectionStartPosition) {
					// user is conducting an "indent region"
					engine.Reset ();
					return base.KeyPress (key, modifier);
				}
				
				c = '\t';
				break;
			default:
				if ((c = (char) Gdk.Keyval.ToUnicode ((uint) key)) == 0) {
					cursor = Editor.SelectionStartPosition;
					bufLen = Editor.TextLength;
					
					if (base.KeyPress (key, modifier)) {
						if (bufLen != Editor.TextLength && cursor < engine.Cursor) {
							// text buffer changed somewhere before our cursor
							engine.Reset ();
						}
						
						return true;
					}
					
					return false;
				}
				break;
			}
			
			cursor = Editor.CursorPosition;
			
			if (cursor < engine.Cursor)
				engine.Reset ();
			
			// get the engine caught up
			for (int i = engine.Cursor; i < cursor; i++) {
				if ((ch = Editor.GetCharAt (i)) == 0)
					break;
				
				engine.Push (ch);
			}
			
			if (c == '\n') {
				// Pass off to base.KeyPress() so the '\n' gets added to the Undo stack, etc
				nInserted = 0;
				if (base.KeyPress (key, modifier)) {
					// if the char inserted is not '\n', then it means the user used
					// <Enter> to accept an auto-completion choice.
					nInserted = Editor.CursorPosition - cursor;
					if (nInserted <= 0 || Editor.GetCharAt (cursor) != '\n')
						return true;
				}
				
				bool inDoc = engine.IsInsideDocLineComment;
				bool emptyDocComment = inDoc && Editor.GetLineText (engine.LineNumber).Trim ().Length == 3;
				engine.Push ('\n');
				nInserted--;
				cursor++;
				
				if (nInserted > 0) {
					// TODO: prevent our base class from auto-indenting(?) for us
					Editor.DeleteText (cursor, nInserted);
				}
				
				ch = Editor.GetCharAt (cursor);
				if (ch == 0 || ch == '\n') {
					// the simple case
					newIndent = engine.NewLineIndent;
					Editor.InsertText (cursor, newIndent);
					for (int i = 0; i < newIndent.Length; i++)
						engine.Push (newIndent[i]);
					cursor += newIndent.Length;
					
					if (engine.IsInsideMultiLineComment) {
						Editor.InsertText (cursor, "* ");
						engine.Push ('*');
						engine.Push (' ');
					}
					
					if (inDoc && !emptyDocComment) {
						Editor.InsertText (cursor, "/// ");
						engine.Push ('/');
						engine.Push ('/');
						engine.Push ('/');
						engine.Push (' ');
					}
					
					// we handled the <Return>
					return true;
				}
				
				// need more context... fall thru
			} else {
				if (c == '\t') {
					// Tab is a special case... depending on the context, the user may be
					// requesting a re-indent, tab-completing, or may just be wanting to
					// insert a literal tab.
					if (!engine.IsInsideVerbatimString) {
						bufLen = Editor.TextLength;
						
						if (base.KeyPress (key, modifier)) {
							nInserted = Editor.TextLength - bufLen;
							
							if (nInserted >= 1)
								ch = Editor.GetCharAt (cursor);
							else
								ch = '\0';
							
							if (nInserted > 1 || (ch != '\0' && ch != '\t')) {
								// tab-completion
								return true;
							}
							
							if (nInserted == 1 && ch == '\t') {
								// base class inserted a tab, delete it
								Editor.DeleteText (cursor, nInserted);
							}
						}
						
						reindent = true;
						insert = false;
					}
				}
				
				if (insert) {
					bool recalibrate = cursor > Editor.SelectionStartPosition;
					
					if (!base.KeyPress (key, modifier))
						return false;
					
					if (recalibrate || cursor >= Editor.CursorPosition)
						engine.Reset ();
					
					cursor = Editor.CursorPosition;
					
					for (int i = engine.Cursor; i < cursor; i++) {
						if ((ch = Editor.GetCharAt (i)) == 0)
							break;
						
						engine.Push (ch);
					}
				}
				
				//engine.Debug ();
				
				if (!(reindent || engine.NeedsReindent))
					return true;
			}
			
			// Get more context but w/o changing our IndentEngine state
			CSharpIndentEngine ctx = (CSharpIndentEngine) engine.Clone ();
			string line = Editor.GetLineText (ctx.LineNumber);
			for (int i = ctx.LineOffset; i < line.Length; i++) {
				ctx.Push (line[i]);
				if (ctx.NeedsReindent)
					break;
			}
			
			// Okay, we should have enough context now
			
			// Measure the current indent
			int nlwsp = 0;
			while (nlwsp < line.Length && Char.IsWhiteSpace (line[nlwsp]))
				nlwsp++;
			
			int pos = Editor.GetPositionFromLineColumn (ctx.LineNumber, 1);
			string curIndent = line.Substring (0, nlwsp);
			int offset;
			
			if (cursor > pos + curIndent.Length)
				offset = cursor - (pos + curIndent.Length);
			else
				offset = 0;
			
			if (!engine.LineBeganInsideMultiLineComment ||
			    (nlwsp < line.Length && line[nlwsp] == '*')) {
				// Possibly replace the indent
				newIndent = ctx.ThisLineIndent;
				
				if (newIndent != curIndent) {
					Editor.DeleteText (pos, nlwsp);
					Editor.InsertText (pos, newIndent);
					
					// Engine state is now invalid
					engine.Reset ();
				}
				
				pos += newIndent.Length;
			} else {
				pos += curIndent.Length;
			}
			
			pos += offset;
			Editor.CursorPosition = pos;
			Editor.Select (pos, pos);
			
			return true;
		}
		
		public override IParameterDataProvider HandleParameterCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			if (completionChar == '(') {
				IParserContext pctx = GetParserContext ();
				
				// Get the text from the begining of the line
				int lin, col;
				Editor.GetLineColumnFromPosition (Editor.CursorPosition, out lin, out col);
				int lineStart = Editor.GetPositionFromLineColumn (lin, 1);
				string line = Editor.GetText (lineStart, Editor.CursorPosition - 1);
				
				// Find the expression before the '('
				ExpressionFinder expressionFinder = new ExpressionFinder (null);
				string ex = expressionFinder.FindExpression (line, line.Length - 1).Expression;
				if (ex == null)
					return null;

				// This is a bit of a hack, but for the resolver to properly resolve a constructor
				// call needs the new keyword and the brackets, so let's provide them
				int i = Editor.CursorPosition - 2 - ex.Length;
				if (GetPreviousToken ("new", ref i, true))
					ex = "new " + ex + "()";
				
				// Find the language item at that position
				Resolver res = new Resolver (pctx);
				ILanguageItem it = res.ResolveIdentifier (pctx, ex, lin, col - 1, FileName, Editor.Text);
				
				// Create the parameter data provider if a method is found.
				if (it is IMethod) {
					IMethod met = (IMethod) it;
					if (met.IsConstructor)
						return new CSharpParameterDataProvider (Editor, met.DeclaringType);
					else
						return new CSharpParameterDataProvider (Editor, met.DeclaringType, met.Name);
				}
				else if (it is IClass) {
					return new CSharpParameterDataProvider (Editor, (IClass)it);
				}
			}
			return null;
		}
		
		bool IsInsideDocumentationComment (int cursor)
		{
			int lin, col;
			Editor.GetLineColumnFromPosition (cursor, out lin, out col);
			
			return Editor.GetLineText (lin).Trim ().StartsWith ("///");
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext ctx, char charTyped)
		{
			if (charTyped == '#') {
				int lin, col;
				Editor.GetLineColumnFromPosition (Editor.CursorPosition, out lin, out col);
				if (col == 2)
					return GetDirectiveCompletionData ();
			}
			// Xml documentation code completion.
			if (charTyped == '<' && IsInsideDocumentationComment (Editor.CursorPosition)) 
				return GetXmlDocumentationCompletionData ();
			
			if (charTyped != '.' && charTyped != ' ')
				return null;
			
			int caretLineNumber = ctx.TriggerLine + 1;
			int caretColumn = ctx.TriggerLineOffset + 1;

			ExpressionFinder expressionFinder = new ExpressionFinder (null);
			
			// Code completion of "new"
			
			int i = ctx.TriggerOffset;
			if (charTyped == ' ' && GetPreviousToken ("new", ref i, false)) {
				string token = GetPreviousToken (ref i, true);
				if (token == "=" || token == "throw") {
				
					IParserContext pctx = GetParserContext ();
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (pctx, GetAmbience ());
					Resolver res = new Resolver (pctx);
					
					IReturnType rt;
					string ex;
					caretColumn -= (i - ctx.TriggerOffset);
					
					if (token == "throw") {
						rt = new DefaultReturnType ("System.Exception");
						ex = "System.Exception";
					}
					else {
						ex = expressionFinder.FindExpression (Editor.GetText (0, i), i - 2).Expression;
						
						// Find the type of the variable that will hold the object
						rt = res.internalResolve (ex, caretLineNumber, caretColumn, FileName, Editor.Text);
						if (rt == null) {
							cp.Dispose ();
							return null;
						}
					}
					cp.AddResolveResults (res.IsAsResolve (ex, caretLineNumber, caretColumn, FileName, Editor.Text, true));
					
					// Add the variable type itself to the results list (IsAsResolve only returns subclasses)
					IClass cls = pctx.GetClass (rt.FullyQualifiedName, rt.GenericArguments);
					if (cls != null && cls.ClassType != ClassType.Interface) {
						cp.AddResolveResult (cls);
						cp.DefaultCompletionString = GetAmbience ().Convert (cls, ConversionFlags.None);
					}
					
					return cp;
				}
			}

			// Check for 'overridable' completion
			
			i = ctx.TriggerOffset;
			if (charTyped == ' ' && GetPreviousToken ("override", ref i, false)) {
				// Look for modifiers, in order to find the beginning of the declaration
				int firstMod = i;
				for (int n=0; n<3; n++) {
					string mod = GetPreviousToken (ref i, true);
					if (mod == "public" || mod == "protected" || mod == "private" || mod == "internal" || mod == "sealed") {
						firstMod = i;
					}
					else if (mod == "static") {
						// static methods are not overridable
						return null;
					}
					else
						break;
				}
				int line, column;
				Editor.GetLineColumnFromPosition (Editor.CursorPosition, out line, out column);
				
				IParserContext pctx = GetParserContext ();
				Resolver res = new Resolver (pctx);
							
				IClass cls = res.GetCallingClass (line, column, FileName, true);
				if (cls != null) {
					string typedModifiers = Editor.GetText (firstMod, ctx.TriggerOffset);
					return GetOverridablesCompletionData (pctx, ctx, cls, firstMod, typedModifiers);
				}
			}
			
			// Code completion of classes, members and namespaces
			
			string expression = expressionFinder.FindExpression (Editor.GetText (0, ctx.TriggerOffset), ctx.TriggerOffset - 2).Expression;
			if (expression == null)
				return null;

			IParserContext parserContext = GetParserContext ();
			CodeCompletionDataProvider completionProvider = new CodeCompletionDataProvider (parserContext, GetAmbience ());

			string ns;
			if (IsInUsing (expression, ctx.TriggerOffset, out ns)) {
				if (charTyped == ' ' && ns != String.Empty) {
					// 'using System' and charTyped == ' '
					// subnamespaces show up only on '.'
					return null;
				}
				
				Resolver res = new Resolver (parserContext);
				// Don't show namespaces when "using" is not a namespace directive
				IClass cls = res.GetCallingClass (caretLineNumber, caretColumn, FileName, false);
				if (cls != null)
					return null;
				string[] namespaces = parserContext.GetNamespaceList (ns, true, true);
				completionProvider.AddResolveResults (new ResolveResult(namespaces));
			} else if (charTyped == ' ') {
				if (expression == "is" || expression == "as") {
					string expr = expressionFinder.FindExpression (Editor.GetText (0, ctx.TriggerOffset), ctx.TriggerOffset - 5).Expression;
					Resolver res = new Resolver (parserContext);
					completionProvider.AddResolveResults (res.IsAsResolve (expr, caretLineNumber, caretColumn, FileName, Editor.Text, false));
				}
			} else {
				/* '.' */
				ResolveResult results = parserContext.Resolve (expression, caretLineNumber, caretColumn, FileName, Editor.Text);
				completionProvider.AddResolveResults (results);
			}
			
			if (completionProvider.IsEmpty)
				return null;
			
			return completionProvider;
		}

		/* returns true in case
		 *	using  : ns - ""
		 *	using System. : ns - "System"
		 *	using System.Collections. : ns - "System.Collections"
		 */
		bool IsInUsing (string expr, int triggerOffset, out string ns)
		{
			int len = expr.Length;
			
			ns = String.Empty;
			if (expr == "using" || (expr.EndsWith ("using") && char.IsWhiteSpace (expr[len - 5])))
				return true;
			
			ns = expr;
			int i = triggerOffset - expr.Length - 1;
			
			return (GetPreviousToken (ref i, true) == "using");
		}
		
		bool GetPreviousToken (string token, ref int i, bool allowLineChange)
		{
			return GetPreviousToken (ref i, allowLineChange) == token;
		}
		
		string GetPreviousToken (ref int i, bool allowLineChange)
		{
			char c;
			
			if (i <= 0)
				return null;
			
			do {
				c = Editor.GetCharAt (--i);
			} while (i > 0 && char.IsWhiteSpace (c) && (allowLineChange ? true : c != '\n'));
			
			if (i == 0)
				return null;
			
			if (!char.IsLetterOrDigit (c))
				return new string (c, 1);
			
			int endOffset = i + 1;
			
			do {
				c = Editor.GetCharAt (i - 1);
				if (!(char.IsLetterOrDigit (c) || c == '_'))
					break;
				
				i--;
			} while (i > 0);
			
			return Editor.GetText (i, endOffset);
		}
		
		ICompletionDataProvider GetOverridablesCompletionData (IParserContext pctx, ICodeCompletionContext ctx, IClass cls, int insertPos, string typedModifiers)
		{
			ArrayList classMembers = new ArrayList ();
			ArrayList interfaceMembers = new ArrayList ();
			
			FindOverridables (pctx, cls, classMembers, interfaceMembers);
			foreach (object mem in interfaceMembers)
				if (!classMembers.Contains (mem))
					classMembers.Add (mem);
			
			CSharpAmbience amb = new CSharpAmbience ();
			CodeCompletionDataProvider completionProvider = new CodeCompletionDataProvider (pctx, GetAmbience ());
			foreach (ILanguageItem mem in classMembers) {
				completionProvider.AddCompletionData (new OverrideCompletionData (Editor, mem, insertPos, typedModifiers, amb));
			}
			return completionProvider;
		}
		
		void FindOverridables (IParserContext pctx, IClass cls, ArrayList classMembers, ArrayList interfaceMembers)
		{
			foreach (IReturnType rt in cls.BaseTypes)
			{
				if (rt.FullyQualifiedName == "System.Object" && cls.ClassType == ClassType.Interface)
					continue;

				IClass baseCls = pctx.GetClass (rt.FullyQualifiedName, rt.GenericArguments, true, true);
				if (baseCls == null)
					continue;

				bool isInterface = baseCls.ClassType == ClassType.Interface;
				if (isInterface && interfaceMembers == null)
					continue;
				ArrayList list = isInterface ? interfaceMembers : classMembers;
				
				foreach (IMethod m in baseCls.Methods) {
					if ((isInterface || m.IsVirtual || m.IsAbstract) && !m.IsSealed)
						list.Add (m);
				}
				foreach (IProperty m in baseCls.Properties) {
					if ((isInterface || m.IsVirtual || m.IsAbstract) && !m.IsSealed)
						list.Add (m);
				}
				foreach (IIndexer m in baseCls.Indexer) {
					if ((isInterface || m.IsVirtual || m.IsAbstract) && !m.IsSealed)
						list.Add (m);
				}
				foreach (IEvent m in baseCls.Events) {
					if ((isInterface || m.IsVirtual || m.IsAbstract) && !m.IsSealed)
						list.Add (m);
				}
				
				FindOverridables (pctx, baseCls, classMembers, isInterface ? interfaceMembers : null);
			}
		}
		
		CodeCompletionDataProvider GetDirectiveCompletionData ()
		{
			CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
			cp.AddCompletionData (new CodeCompletionData ("if", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("else", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("elif", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("endif", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("define", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("undef", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("warning", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("error", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("pragma", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("line", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("line hidden", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("line default", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("region", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("endregion", "md-literal"));
			return cp;
		}
		static readonly List<string> commentTags = new List<string> (new string[] { "c", "code", "example", "exception", "include", "list", "listheader", "item", "term", "description", "para", "param", "paramref", "permission", "remarks", "returns", "see", "seealso", "summary", "value" });
		
		CodeCompletionDataProvider GetXmlDocumentationCompletionData ()
		{
			CodeCompletionDataProvider cp = new CodeCompletionDataProvider (null, GetAmbience ());
			cp.AddCompletionData (new CodeCompletionData ("c", "md-literal", GettextCatalog.GetString ("Marks text as code.")));
			cp.AddCompletionData (new CodeCompletionData ("code", "md-literal", GettextCatalog.GetString ("Marks text as code.")));
			cp.AddCompletionData (new CodeCompletionData ("example", "md-literal", GettextCatalog.GetString ("A description of the code sample.\nCommonly, this would involve use of the &lt;code&gt; tag.")));
			cp.AddCompletionData (new CodeCompletionData ("exception cref=\"\"", "md-literal", GettextCatalog.GetString ("This tag lets you specify which exceptions can be thrown.")));
			cp.AddCompletionData (new CodeCompletionData ("include file=\"\" path=\"\"", "md-literal", GettextCatalog.GetString ("The &lt;include&gt; tag lets you refer to comments in another file that describe the types and members in your source code.\nThis is an alternative to placing documentation comments directly in your source code file.")));
			cp.AddCompletionData (new CodeCompletionData ("list type=\"\"", "md-literal", GettextCatalog.GetString ("Defines a list or table.")));
			cp.AddCompletionData (new CodeCompletionData ("listheader", "md-literal", GettextCatalog.GetString ("Defines a header for a list or table.")));
			cp.AddCompletionData (new CodeCompletionData ("item", "md-literal", GettextCatalog.GetString ("Defines an item for a list or table.")));
			cp.AddCompletionData (new CodeCompletionData ("term", "md-literal", GettextCatalog.GetString ("A term to define.")));
			cp.AddCompletionData (new CodeCompletionData ("description", "md-literal", GettextCatalog.GetString ("Describes a term in a list or table.")));
			cp.AddCompletionData (new CodeCompletionData ("para", "md-literal", GettextCatalog.GetString ("A text paragraph.")));

			cp.AddCompletionData (new CodeCompletionData ("param name=\"\"", "md-literal", GettextCatalog.GetString ("Describes a method parameter.")));
			cp.AddCompletionData (new CodeCompletionData ("paramref name=\"\"", "md-literal", GettextCatalog.GetString ("The &lt;paramref&gt; tag gives you a way to indicate that a word is a parameter.")));
			
			cp.AddCompletionData (new CodeCompletionData ("permission cref=\"\"", "md-literal", GettextCatalog.GetString ("The &lt;permission&gt; tag lets you document the access of a member.")));
			cp.AddCompletionData (new CodeCompletionData ("remarks", "md-literal", GettextCatalog.GetString ("The &lt;remarks&gt; tag is used to add information about a type, supplementing the information specified with &lt;summary&gt;.")));
			cp.AddCompletionData (new CodeCompletionData ("returns", "md-literal", GettextCatalog.GetString ("The &lt;returns&gt; tag should be used in the comment for a method declaration to describe the return value.")));
			cp.AddCompletionData (new CodeCompletionData ("see cref=\"\"", "md-literal", GettextCatalog.GetString ("The &lt;see&gt; tag lets you specify a link from within text.")));
			cp.AddCompletionData (new CodeCompletionData ("seealso cref=\"\"", "md-literal", GettextCatalog.GetString ("The &lt;seealso&gt; tag lets you specify the text that you might want to appear in a See Also section.")));
			cp.AddCompletionData (new CodeCompletionData ("summary", "md-literal", GettextCatalog.GetString ("The &lt;summary&gt; tag should be used to describe a type or a type member.")));
			cp.AddCompletionData (new CodeCompletionData ("value", "md-literal", GettextCatalog.GetString ("The &lt;value&gt; tag lets you describe a property.")));
			
			return cp;
		}
	}
	
}
