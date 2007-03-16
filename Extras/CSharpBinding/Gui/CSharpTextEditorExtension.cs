
using System;
using System.Text;
using System.Collections;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Properties;
using MonoDevelop.SourceEditor.FormattingStrategy;
using CSharpBinding.Parser;
using CSharpBinding.FormattingStrategy;
using MonoDevelop.Projects.Ambience;
using Ambience_ = MonoDevelop.Projects.Ambience.Ambience;

namespace CSharpBinding
{
	public class CSharpTextEditorExtension: TextEditorExtension
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
		
		public override bool KeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			bool reindent = false, insert = true;
			int nInserted, cursor;
			string newIndent;
			char ch, c;
			
			// This code is for Smart Indent, no-op for any other indent style
			if (TextEditorProperties.IndentStyle != IndentStyle.Smart)
				return base.KeyPress (key, modifier);
			
			switch (key) {
			case Gdk.Key.KP_Enter:
			case Gdk.Key.Return:
				reindent = true;
				insert = false;
				c = '\n';
				break;
			case Gdk.Key.Tab:
				c = '\t';
				break;
			default:
				if ((c = (char) Gdk.Keyval.ToUnicode ((uint) key)) == 0)
					return base.KeyPress (key, modifier);
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
			
			if (c == '\t') {
				// Tab is a special case... depending on the context, the user may be
				// requesting a re-indent, tab-completing, or may just be wanting to
				// insert a literal tab.
				
				if (Editor.SelectionEndPosition > Editor.SelectionStartPosition) {
					// FIXME: replace with an appropriate indent for the line
					// containing the cursor position Editor.SelectionStartPosition?
					return base.KeyPress (key, modifier);
				}
				
				if (!engine.IsInsideVerbatimString) {
					int bufLen = Editor.TextLength;
					
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
			
			if (insert)
				engine.Push (c);
			
			// engine.Debug ();
			
			if (!(reindent || engine.NeedsReindent)) {
				if (insert)
					return base.KeyPress (key, modifier);
				
				return true;
			}
			
			if (c == '\n') {
				if (Editor.SelectionEndPosition > Editor.SelectionStartPosition)
					return base.KeyPress (key, modifier);
				
				// Pass off to base.KeyPress() so the '\n' gets added to the Undo stack, etc
				nInserted = 0;
				if (base.KeyPress (key, modifier)) {
					// if the char inserted is not '\n', then it means the user used
					// <Enter> to accept an auto-completion choice.
					nInserted = Editor.CursorPosition - cursor;
					if (nInserted <= 0 || Editor.GetCharAt (cursor) != '\n')
						return true;
				}
				
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
					
					// we handled the Return
					return true;
				}
				
				// need more context... fall thru
			}
			
			// Get more context but w/o changing our IndentEngine state
			CSharpIndentEngine ctx = (CSharpIndentEngine) engine.Clone ();
			string line = Editor.GetLineText (ctx.LineNumber);
			for (int i = ctx.LineOffset; i < line.Length; i++) {
				ctx.Push (line[i]);
				if (ctx.NeedsReindent)
					break;
			}
			
			// ok, we should have enough context now
			
			// measure the current indent
			int nlwsp = 0;
			while (nlwsp < line.Length && Char.IsWhiteSpace (line[nlwsp]))
				nlwsp++;
			
			int pos = Editor.GetPositionFromLineColumn (ctx.LineNumber, 1);
			string curIndent = line.Substring (0, nlwsp);
			
			// Note: We always replace the leading white space.
			//       If the cursor is within the leading white
			//       space, then it will be pushed to the end,
			//       otherwise nothing will change (in the end).
			
			Editor.DeleteText (pos, nlwsp);
			
			if (!engine.IsInsideMultiLineComment ||
			    (nlwsp < line.Length && line[nlwsp] == '*')) {
				// Replace the indent
				newIndent = ctx.ThisLineIndent;
				Editor.InsertText (pos, newIndent);
				
				if (newIndent != curIndent) {
					// engine state is now invalid
					engine.Reset ();
				}
			} else {
				// Reinsert current indent
				Editor.InsertText (pos, curIndent);
			}
			
			if (insert)
				return base.KeyPress (key, modifier);
			
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


		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext ctx, char charTyped)
		{
			if (charTyped == '#') {
				int lin, col;
				Editor.GetLineColumnFromPosition (Editor.CursorPosition, out lin, out col);
				if (col == 1)
					return GetDirectiveCompletionData ();
			}
			
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
			
			if (charTyped == ' ') {
				if (expression == "is" || expression == "as") {
					string expr = expressionFinder.FindExpression (Editor.GetText (0, ctx.TriggerOffset), ctx.TriggerOffset - 5).Expression;
					Resolver res = new Resolver (parserContext);
					completionProvider.AddResolveResults (res.IsAsResolve (expr, caretLineNumber, caretColumn, FileName, Editor.Text, false));
				}
				else if (expression == "using" || expression.EndsWith(" using") || expression.EndsWith("\tusing")|| expression.EndsWith("\nusing")|| expression.EndsWith("\rusing")) {
					Resolver res = new Resolver (parserContext);
					// Don't show namespaces when "using" is not a namespace directive
					IClass cls = res.GetCallingClass (caretLineNumber, caretColumn, FileName, false);
					if (cls != null)
						return null;
					string[] namespaces = parserContext.GetNamespaceList ("", true, true);
					completionProvider.AddResolveResults (new ResolveResult(namespaces));
				}
			} else {
				ResolveResult results = parserContext.Resolve (expression, caretLineNumber, caretColumn, FileName, Editor.Text);
				completionProvider.AddResolveResults (results);
			}
			
			if (completionProvider.IsEmpty)
				return null;
			
			return completionProvider;
		}
		
		bool GetPreviousToken (string token, ref int i, bool allowLineChange)
		{
			string s = Editor.GetText (i-1, i);
			while (s.Length > 0 && (s[0] == ' ' || s[0] == '\t' || (allowLineChange && s[0] == '\n'))) {
				i--;
				s = Editor.GetText (i-1, i);
			}
			if (s.Length == 0)
				return false;
			
			i -= token.Length;
			return Editor.GetText (i, i + token.Length) == token;
		}
		
		string GetPreviousToken (ref int i, bool allowLineChange)
		{
			string s = Editor.GetText (i-1, i);
			while (s.Length > 0 && (s[0] == ' ' || s[0] == '\t' || (allowLineChange && s[0] == '\n'))) {
				i--;
				s = Editor.GetText (i-1, i);
			}
			if (s.Length == 0)
				return null;
			if (!char.IsLetterOrDigit (s[0]))
				return s;

			int endp = i;
			while (s.Length > 0 && (char.IsLetterOrDigit (s[0]) || s[0] == '_')) {
				i--;
				s = Editor.GetText (i-1, i);
			}
			
			return Editor.GetText (i, endp);
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
	}
}
