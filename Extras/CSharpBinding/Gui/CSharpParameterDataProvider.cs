
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
			int i = editor.CursorPosition;
			if (i < ctx.TriggerOffset)
				return -1;
				
			if (i == ctx.TriggerOffset)
				return 0;
			
			int level = 0;
			int index = 0;
			int realLevel = -1;
			string s = editor.GetText (i-1, i);
			while (i > ctx.TriggerOffset && s.Length > 0) {
				if (s[0] == '(' || s[0] == '[') {
					if (level > 0)
						level--;
					else
						index = 0;
					realLevel--;
				}
				else if (s[0] == ')' || s[0] == ']') {
					level++;
					realLevel++;
				}
				else if (s[0] == ',') {
					index++;
				}
				i--;
				s = editor.GetText (i-1, i);
			}
			
			if (realLevel == 0)
				return -1;
			else
				return index + 1;
		}
	}
}
