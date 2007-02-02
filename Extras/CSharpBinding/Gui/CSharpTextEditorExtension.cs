
using System;
using System.Text;
using System.Collections;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui.Completion;
using CSharpBinding.Parser;
using MonoDevelop.Projects.Ambience;
using Ambience_ = MonoDevelop.Projects.Ambience.Ambience;

namespace CSharpBinding
{
	public class CSharpTextEditorExtension: TextEditorExtension
	{
		public override bool KeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			if (key == Gdk.Key.numbersign) {
				// Remove all indenting
				int i = Editor.CursorPosition;
				string s = Editor.GetText (i-1, i);
				while (s.Length > 0 && (s[0] == ' ' || s[0] == '\t')) {
					i--;
					s = Editor.GetText (i-1, i);
				}
				if (s.Length == 0 || s[0] == '\n') {
					Editor.DeleteText (i, Editor.CursorPosition - i);
				}
			}
			return base.KeyPress (key, modifier);
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
			if (charTyped == ' ' && GetPreviousToken ("new", ref i)) {
				string token = GetPreviousToken (ref i);
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
			if (charTyped == ' ' && GetPreviousToken ("override", ref i)) {
			
				// Look for modifiers, in order to find the beginning of the declaration
				int firstMod = i;
				bool isSealed;
				for (int n=0; n<3; n++) {
					string mod = GetPreviousToken (ref i);
					if (mod == "public" || mod == "protected" || mod == "private" || mod == "internal") {
						firstMod = i;
					}
					else if (mod == "static") {
						return null;
					}
					else if (mod == "sealed") {
						firstMod = i;
						isSealed = true;
					}
					else
						break;
				}
				int line, column;
				Editor.GetLineColumnFromPosition (Editor.CursorPosition, out line, out column);
				
				IParserContext pctx = GetParserContext ();
				Resolver res = new Resolver (pctx);
				IClass cls = res.GetCallingClass (line, column, FileName, Editor.Text, true);
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
		
		bool GetPreviousToken (string token, ref int i)
		{
			string s = Editor.GetText (i-1, i);
			while (s.Length > 0 && (s[0] == ' ' || s[0] == '\t')) {
				i--;
				s = Editor.GetText (i-1, i);
			}
			if (s.Length == 0)
				return false;
			
			i -= token.Length;
			return Editor.GetText (i, i + token.Length) == token;
		}
		
		string GetPreviousToken (ref int i)
		{
			string s = Editor.GetText (i-1, i);
			while (s.Length > 0 && (s[0] == ' ' || s[0] == '\t')) {
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
			cp.AddCompletionData (new CodeCompletionData ("line", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("region", "md-literal"));
			cp.AddCompletionData (new CodeCompletionData ("endregion", "md-literal"));
			return cp;
		}
	}
	
	class OverrideCompletionData: CodeCompletionData, IActionCompletionData
	{
		ILanguageItem item;
		IEditableTextBuffer editor;
		CSharpAmbience ambience;
		string typedModifiers;
		int insertOffset;
		string indent;
		
		public OverrideCompletionData (IEditableTextBuffer editor, ILanguageItem item, int insertOffset, string typedModifiers, CSharpAmbience amb)
		{
			this.typedModifiers = typedModifiers;
			this.insertOffset = insertOffset;
			this.ambience = amb;
			this.editor = editor;
			this.item = item;
			ConversionFlags flags = ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters;
			
			if (item is IIndexer) {
				IIndexer ind = (IIndexer) item;
				Image = IdeApp.Services.Icons.GetIcon (ind);
				StringBuilder sb = new StringBuilder ("this [");
				ambience.Convert (ind.Parameters, sb, flags);
				sb.Append ("]");
				Text = new string[] { sb.ToString () };
				Description = ambience.Convert(item);
				DescriptionPango = ambience.Convert (item, ConversionFlags.StandardConversionFlags | ConversionFlags.IncludePangoMarkup);
				CompletionString = "this";
				Documentation = item.Documentation;
			}
			else if (item is IMethod) {
				IMethod met = (IMethod) item;
				Image = IdeApp.Services.Icons.GetIcon (met);
				StringBuilder sb = new StringBuilder (met.Name + " (");
				ambience.Convert (met.Parameters, sb, flags);
				sb.Append (")");
				Text = new string[] { sb.ToString () };
				Description = ambience.Convert (met);
				DescriptionPango = ambience.Convert (met, ConversionFlags.StandardConversionFlags | ConversionFlags.IncludePangoMarkup);
				CompletionString = met.Name;
				Documentation = met.Documentation;
			}
			else
				FillCodeCompletionData (item, ambience);
		}
		
		public void InsertAction (ICompletionWidget widget, ICodeCompletionContext context)
		{
			indent = GetIndentString (insertOffset + 1);
			
			// Remove the partially completed word
			editor.DeleteText (insertOffset, editor.CursorPosition - insertOffset);

			// Get the modifiers of the new override
			IMember mem = (IMember) item;
			string modifiers = GetModifiers (mem, typedModifiers);
			if (modifiers.Length > 0) modifiers += " ";
			if ((mem.IsVirtual || mem.IsAbstract) && mem.DeclaringType.ClassType != ClassType.Interface)
				modifiers += "override ";
			
			if (item is IMethod)
				InsertMethod ((IMethod) item, modifiers);
			if (item is IProperty)
				InsertProperty ((IProperty) item, modifiers);
			if (item is IEvent)
				InsertEvent ((IEvent) item, modifiers);
			if (item is IIndexer)
				InsertIndexer ((IIndexer) item, modifiers);
		}
		
		void InsertMethod (IMethod method, string modifiers)
		{
			ConversionFlags flags = ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters;
				
			string text = modifiers + ambience.Convert (method, flags);
			text += "\n" + indent + "{\n" + indent + "\t";
			int cpos = insertOffset + text.Length;
			text += "\n" + indent + "}\n";
			
			editor.InsertText (insertOffset, text);
			editor.CursorPosition = cpos;
			editor.Select (cpos, cpos);
		}
		
		void InsertProperty (IProperty prop, string modifiers)
		{
			ConversionFlags flags = ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters;
			int cpos = -1;
			string text = modifiers + ambience.Convert (prop.ReturnType, flags) + " " + prop.Name + " {\n";
			
			if (prop.CanGet) {
				text += indent + "\tget { ";
				cpos = insertOffset + text.Length;
				text += " }\n";
			}
			if (prop.CanSet) {
				text += indent + "\tset { ";
				if (!prop.CanGet)
					cpos = insertOffset + text.Length;
				text += " }\n";
			}
			
			text += indent + "}\n";
			
			editor.InsertText (insertOffset, text);
			editor.CursorPosition = cpos;
			editor.Select (cpos, cpos);
		}
		
		void InsertEvent (IEvent ev, string modifiers)
		{
			ConversionFlags flags = ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters;
			string text = modifiers + "event " + ambience.Convert (ev.ReturnType, flags) + " " + ev.Name + " {\n";
			
			int cpos;
			text += indent + "\tadd { ";
			cpos = insertOffset + text.Length;
			text += " }\n";
			text += indent + "\tremove { ";
			text += " }\n";
			
			text += indent + "}\n";
			
			editor.InsertText (insertOffset, text);
			editor.CursorPosition = cpos;
			editor.Select (cpos, cpos);
		}
		
		void InsertIndexer (IIndexer indexer, string modifiers)
		{
			ConversionFlags flags = ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters;
			
			int cpos;
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (modifiers).Append (ambience.Convert (indexer.ReturnType)).Append (" this [");
			ambience.Convert (indexer.Parameters, sb, flags);
			sb.Append ("] {\n");
			sb.Append (indent).Append ("\tget { ");
			cpos = insertOffset + sb.Length;
			sb.Append (" }\n");
			sb.Append (indent).Append ("\tset { ");
			sb.Append (" }\n").Append (indent).Append ("}");
			
			editor.InsertText (insertOffset, sb.ToString ());
			editor.CursorPosition = cpos;
			editor.Select (cpos, cpos);
		}
		
		string GetIndentString (int pos)
		{
			string c = editor.GetText (pos - 1, pos);
			int nwpos = pos;
			while (c.Length > 0 && c != "\n") {
				if (c[0] != ' ' && c[0] != '\t')
					nwpos = pos;
				pos--;
				c = editor.GetText (pos - 1, pos);
			}
			return editor.GetText (pos, nwpos - 1);
		}
		
		string GetModifiers (IDecoration dec, string typedModifiers)
		{
			string res = "";
			
			if (dec.IsPublic) {
				res = "public";
			} else if (dec.IsPrivate) {
				res = "";
			} else if (dec.IsProtectedAndInternal) {
				res = "protected internal";
			} else if (dec.IsProtectedOrInternal) {
				res = "internal protected";
			} else if (dec.IsInternal) {
				res = "internal";
			} else if (dec.IsProtected) {
				res = "protected";
			}
			if (typedModifiers.IndexOf ("sealed") != -1) {
				if (res.Length > 0)
					return res + " sealed";
				else
					return "sealed";
			}
			else
				return res;
		}
	}
}
