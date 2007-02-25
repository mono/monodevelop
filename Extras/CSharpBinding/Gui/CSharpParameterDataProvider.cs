
using System;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Ambience;

namespace CSharpBinding
{
	public class CSharpParameterDataProvider: MethodParameterDataProvider
	{
		IEditableTextBuffer editor;
		CSharpAmbience ambience = new CSharpAmbience ();
		
		public CSharpParameterDataProvider (IEditableTextBuffer editor, IClass cls, string methodName): base (cls, methodName)
		{
			this.editor = editor;
		}
		
		public CSharpParameterDataProvider (IEditableTextBuffer editor, IClass cls): base (cls)
		{
			this.editor = editor;
		}
		
		public override string GetParameterMarkup (int overload, int paramIndex)
		{
			IParameter p = GetParameter (overload, paramIndex);
			return ambience.Convert (p, ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters | ConversionFlags.IncludePangoMarkup);
		}
		
		public override string GetMethodMarkup (int overload, string[] parameters)
		{
			IMethod met = GetMethod (overload);
			
			string paramTxt = string.Join (", ", parameters);
			if (met.IsConstructor)
				return "<b>" + GLib.Markup.EscapeText (ambience.Convert (met.DeclaringType, ConversionFlags.None)) + "</b> (" + paramTxt + ")";
			else
				return GLib.Markup.EscapeText (ambience.Convert (met.ReturnType, ConversionFlags.ShowGenericParameters)) + " <b>" + met.Name + "</b> (" + paramTxt + ")";
		}
		
		public override int GetCurrentParameterIndex (ICodeCompletionContext ctx)
		{
			int i = ctx.TriggerOffset;
			
			if (i > editor.CursorPosition)
				return -1;
				
			if (i == editor.CursorPosition)
				return 0;
			
			bool inString = false;
			bool inVerbatimString = false;
			bool inComment = false;
			
			int level = 0;
			int index = 0;
			char prevChar = '\0';
			string s = editor.GetText (i-1, i);
			
			while (i <= editor.CursorPosition && s.Length > 0) {
				switch (s[0]) {
				case '(':
				case '[':
					if (!inString && !inComment) {
						level++;
					}
					break;
				case ')':
				case ']':
					if (!inString && !inComment) {
						level--;
					}
					break;
				case '*':
					if (prevChar == '/' && !inString)
						inComment = true;
					break;
				case '/':
					if (prevChar == '*' && inComment)
						inComment = false;
					break;
				case '"':
					if ((inString && prevChar != '\\') ||
					    inVerbatimString) {
						inString = false;
						inVerbatimString = false;
					}
					else if (!inString && !inComment) {
						inString = true;
						if (prevChar == '@')
							inVerbatimString = true;
					}
					break;
				case ',':
					if (!inString && !inComment)
						index++;
					break;
				}
				
				i++;
				prevChar = s[0];
				s = editor.GetText (i-1, i);
			}
			
			if (level == 0)
				return -1;
			else
				return index + 1;
		}
	}
}
