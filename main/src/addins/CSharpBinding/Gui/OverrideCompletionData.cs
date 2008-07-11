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
	class OverrideCompletionData : CodeCompletionData, IActionCompletionData
	{
		ILanguageItem item;
		TextEditor editor;
		CSharpAmbience ambience;
		string typedModifiers;
		int insertOffset;
		string indent;
		ITypeNameResolver resolver;
		
		public OverrideCompletionData (TextEditor editor, ILanguageItem item, int insertOffset, string typedModifiers, CSharpAmbience amb, ITypeNameResolver resolver)
		{
			this.typedModifiers = typedModifiers;
			this.insertOffset = insertOffset;
			this.ambience = amb;
			this.editor = editor;
			this.item = item;
			this.resolver = resolver;
			
			ConversionFlags flags = ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters | ConversionFlags.UseIntrinsicTypeNames;
			
			if (item is IIndexer) {
				IIndexer ind = (IIndexer) item;
				Image = IdeApp.Services.Icons.GetIcon (ind);
				StringBuilder sb = new StringBuilder ("this [");
				ambience.Convert (ind.Parameters, sb, flags, resolver);
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
				ambience.Convert (met.Parameters, sb, flags, resolver);
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
			ConversionFlags flags = 
				ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters | 
				ConversionFlags.UseFullyQualifiedNames | ConversionFlags.UseIntrinsicTypeNames | 
				ConversionFlags.QualifiedNamesOnlyForReturnTypes | ConversionFlags.ShowReturnType | 
				ConversionFlags.ShowParameters;
			
			StringBuilder textBuilder = new StringBuilder ();
			
			textBuilder.Append (modifiers);
			textBuilder.Append (ambience.Convert (method, flags, resolver));
			textBuilder.Append ('\n');
			textBuilder.Append (indent);
			textBuilder.Append ("{\n");
			textBuilder.Append (indent);
			if (TextEditorProperties.ConvertTabsToSpaces)
				textBuilder.Append (' ', TextEditorProperties.TabIndent);
			else
				textBuilder.Append ('\t');
			
			// Include call to base when possible
			if (!method.IsAbstract && method.DeclaringType.ClassType != ClassType.Interface) {
				if (method.ReturnType != null && method.ReturnType.FullyQualifiedName != "System.Void")
					textBuilder.Append ("return ");
				
				textBuilder.Append ("base.").Append (method.Name).Append (" (");
				for (int n=0; n<method.Parameters.Count; n++) {
					IParameter par = method.Parameters [n];
					if (n > 0)
						textBuilder.Append (", ");
					if (par.IsOut)
						textBuilder.Append ("out ");
					else if (par.IsRef)
						textBuilder.Append ("ref ");
					textBuilder.Append (method.Parameters [n].Name);
				}
				textBuilder.Append (");");
			} else {
				textBuilder.Append ("throw new ");
				textBuilder.Append (resolver.ResolveName ("System.NotImplementedException"));
				textBuilder.Append (" ();");
			}
			
			int cpos = insertOffset + textBuilder.Length;
			
			textBuilder.Append ('\n');
			textBuilder.Append (indent);
			textBuilder.Append ("}\n");
			
			string text = textBuilder.ToString ();
			
			editor.InsertText (insertOffset, text);
			editor.CursorPosition = cpos;
			editor.Select (cpos, cpos);
		}
		
		void InsertProperty (IProperty prop, string modifiers)
		{
			ConversionFlags flags = 
				ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters | 
				ConversionFlags.UseFullyQualifiedNames | ConversionFlags.UseIntrinsicTypeNames | 
				ConversionFlags.QualifiedNamesOnlyForReturnTypes | ConversionFlags.ShowReturnType | 
				ConversionFlags.ShowParameters;
			
			StringBuilder textBuilder = new StringBuilder ();
			int cpos = -1;
			
			textBuilder.Append (modifiers);
			textBuilder.Append (ambience.Convert (prop.ReturnType, flags, resolver));
			textBuilder.Append (' ');
			textBuilder.Append (prop.Name);
			textBuilder.Append (" {\n");
			
			if (prop.CanGet) {
				textBuilder.Append (indent);
				if (TextEditorProperties.ConvertTabsToSpaces)
					textBuilder.Append (' ', TextEditorProperties.TabIndent);
				else
					textBuilder.Append ('\t');
				
				textBuilder.Append ("get { ");
				
				cpos = insertOffset + textBuilder.Length;
				
				textBuilder.Append (" }\n");
			}
			
			if (prop.CanSet) {
				textBuilder.Append (indent);
				if (TextEditorProperties.ConvertTabsToSpaces)
					textBuilder.Append (' ', TextEditorProperties.TabIndent);
				else
					textBuilder.Append ('\t');
				
				textBuilder.Append ("set { ");
				
				if (!prop.CanGet)
					cpos = insertOffset + textBuilder.Length;
				
				textBuilder.Append (" }\n");
			}
			
			textBuilder.Append (indent);
			textBuilder.Append ("}\n");
			
			string text = textBuilder.ToString ();
			
			editor.InsertText (insertOffset, text);
			editor.CursorPosition = cpos;
			editor.Select (cpos, cpos);
		}
		
		void InsertEvent (IEvent ev, string modifiers)
		{
			ConversionFlags flags = 
				ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters | 
				ConversionFlags.UseFullyQualifiedNames | ConversionFlags.UseIntrinsicTypeNames | 
				ConversionFlags.QualifiedNamesOnlyForReturnTypes | ConversionFlags.ShowReturnType | 
				ConversionFlags.ShowParameters;
			
			StringBuilder textBuilder = new StringBuilder ();
			
			textBuilder.Append (modifiers);
			textBuilder.Append ("event ");
			textBuilder.Append (ambience.Convert (ev.ReturnType, flags, resolver));
			textBuilder.Append (' ');
			textBuilder.Append (ev.Name);
			textBuilder.Append (" {\n");
			
			textBuilder.Append (indent);
			if (TextEditorProperties.ConvertTabsToSpaces)
				textBuilder.Append (' ', TextEditorProperties.TabIndent);
			else
				textBuilder.Append ('\t');
			
			textBuilder.Append ("add { ");
			
			int cpos = insertOffset + textBuilder.Length;
			
			textBuilder.Append (" }\n");
			textBuilder.Append (indent);
			if (TextEditorProperties.ConvertTabsToSpaces)
				textBuilder.Append (' ', TextEditorProperties.TabIndent);
			else
				textBuilder.Append ('\t');
			
			textBuilder.Append ("remove { }\n");
			textBuilder.Append (indent);
			textBuilder.Append ("}\n");
			
			string text = textBuilder.ToString ();
			
			editor.InsertText (insertOffset, text);
			editor.CursorPosition = cpos;
			editor.Select (cpos, cpos);
		}
		
		void InsertIndexer (IIndexer indexer, string modifiers)
		{
			ConversionFlags flags = 
				ConversionFlags.ShowParameterNames | ConversionFlags.ShowGenericParameters | 
				ConversionFlags.UseFullyQualifiedNames | ConversionFlags.UseIntrinsicTypeNames | 
				ConversionFlags.QualifiedNamesOnlyForReturnTypes | ConversionFlags.ShowReturnType | 
				ConversionFlags.ShowParameters;
			
			StringBuilder textBuilder = new StringBuilder ();
			
			textBuilder.Append (modifiers);
			textBuilder.Append (ambience.Convert (indexer.ReturnType, flags, resolver));
			textBuilder.Append (" this [");
			
			ambience.Convert (indexer.Parameters, textBuilder, flags, resolver);
			
			textBuilder.Append ("] {\n");
			
			textBuilder.Append (indent);
			if (TextEditorProperties.ConvertTabsToSpaces)
				textBuilder.Append (' ', TextEditorProperties.TabIndent);
			else
				textBuilder.Append ('\t');
			
			textBuilder.Append ("get { ");
			
			int cpos = insertOffset + textBuilder.Length;
			
			textBuilder.Append (" }\n");
			
			textBuilder.Append (indent);
			if (TextEditorProperties.ConvertTabsToSpaces)
				textBuilder.Append (' ', TextEditorProperties.TabIndent);
			else
				textBuilder.Append ('\t');
			
			textBuilder.Append ("set { }\n");
			textBuilder.Append (indent);
			textBuilder.Append ("}");
			
			string text = textBuilder.ToString ();
			
			editor.InsertText (insertOffset, text);
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
