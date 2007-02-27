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
	class OverrideCompletionData: CodeCompletionData, IActionCompletionData
	{
		ILanguageItem item;
		TextEditor editor;
		CSharpAmbience ambience;
		string typedModifiers;
		int insertOffset;
		string indent;
		
		public OverrideCompletionData (TextEditor editor, ILanguageItem item, int insertOffset, string typedModifiers, CSharpAmbience amb)
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
