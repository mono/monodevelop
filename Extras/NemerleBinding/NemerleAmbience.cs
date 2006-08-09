// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Text;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Projects.Ambience;

namespace NemerleBinding
{
	public class NemerleAmbience :  Ambience
	{
		static string[,] typeConversionList = new string[,] {
			{"System.Void",    "void"},
			{"System.Object",  "object"},
			{"System.Boolean", "bool"},
			{"System.Byte",    "byte"},
			{"System.SByte",   "sbyte"},
			{"System.Char",    "char"},
			{"System.Enum",    "enum"},
			{"System.Int16",   "short"},
			{"System.Int32",   "int"},
			{"System.Int64",   "long"},
			{"System.UInt16",  "ushort"},
			{"System.UInt32",  "uint"},
			{"System.UInt64",  "ulong"},
			{"System.Single",  "float"},
			{"System.Double",  "double"},
			{"System.Decimal", "decimal"},
			{"System.String",  "string"}
		};
		
		static Hashtable typeConversionTable = new Hashtable();
		
		public static Hashtable TypeConversionTable {
			get {
				return typeConversionTable;
			}
		}
		
		static NemerleAmbience()
		{
			for (int i = 0; i < typeConversionList.GetLength(0); ++i) {
				typeConversionTable[typeConversionList[i, 0]] = typeConversionList[i, 1];
			}
		}
		
		bool ModifierIsSet(ModifierEnum modifier, ModifierEnum query)
		{
			return (modifier & query) == query;
		}
		
		public override string Convert(ModifierEnum modifier, ConversionFlags flags)
		{
			if (ShowAccessibility (flags)) {
				if (ModifierIsSet(modifier, ModifierEnum.Public)) {
					return "public ";
				} else if (ModifierIsSet(modifier, ModifierEnum.Private)) {
					return "private ";
				} else if (ModifierIsSet(modifier, ModifierEnum.ProtectedAndInternal)) {
					return "protected internal ";
				} else if (ModifierIsSet(modifier, ModifierEnum.ProtectedOrInternal)) {
					return "internal protected ";
				} else if (ModifierIsSet(modifier, ModifierEnum.Internal)) {
					return "internal ";
				} else if (ModifierIsSet(modifier, ModifierEnum.Protected)) {
					return "protected ";
				}
			}
			
			return string.Empty;
		}
		
		string GetModifier(IDecoration decoration, ConversionFlags flags)
		{	
			string mod;
			
			if (decoration.IsStatic)        mod = "static ";
			else if (decoration.IsFinal)    mod = "final ";
			else if (decoration.IsVirtual)  mod = "virtual ";
			else if (decoration.IsOverride) mod = "override ";
			else if (decoration.IsNew)      mod = "new ";
			else return "";
			
			if (IncludeHTMLMarkup (flags) | IncludePangoMarkup (flags))
				return "<i>" + mod + "</i>";
			else
				return mod;
		}
		
		
		public override string Convert(IClass c, ConversionFlags flags)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append(Convert(c.Modifiers));
			
			if (ShowClassModifiers (flags)) {
				if (c.IsSealed) {
					switch (c.ClassType) {
						case ClassType.Delegate:
						case ClassType.Struct:
						case ClassType.Enum:
							break;
						
						default:
							AppendPangoHtmlTag (builder, "sealed ", "i", flags);
							break;
					}
				} else if (c.IsAbstract && c.ClassType != ClassType.Interface) {
					AppendPangoHtmlTag (builder, "abstract ", "i", flags);
				}
			}
			
			if (ShowClassModifiers (flags)) {
				switch (c.ClassType) {
					case ClassType.Delegate:
						builder.Append("delegate");
						break;
					case ClassType.Class:
						builder.Append("class");
						break;
					case ClassType.Struct:
						builder.Append("struct");
						break;
					case ClassType.Interface:
						builder.Append("interface");
						break;
					case ClassType.Enum:
						builder.Append("enum");
						break;
				}
				builder.Append(' ');
			}
			
			if (c.ClassType == ClassType.Delegate && c.Methods.Count > 0) {
				foreach(IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					builder.Append(Convert(m.ReturnType));
					builder.Append(' ');
				}
			}
			
			if (UseFullyQualifiedMemberNames (flags))
				AppendPangoHtmlTag (builder, c.FullyQualifiedName, "b", flags);
			else
				AppendPangoHtmlTag (builder, c.Name, "b", flags);
			
			
			if (c.ClassType == ClassType.Delegate) {
				builder.Append(" (");
				if (IncludeHTMLMarkup (flags)) builder.Append("<br>");
				
				foreach(IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					for (int i = 0; i < m.Parameters.Count; ++i) {
						if (IncludeHTMLMarkup (flags)) builder.Append("&nbsp;&nbsp;&nbsp;");
						
						builder.Append(Convert(m.Parameters[i]));
						if (i + 1 < m.Parameters.Count) builder.Append(", ");
						
						if (IncludeHTMLMarkup (flags)) builder.Append("<br>");
					}
				}
				builder.Append(')');
				
			} else if (ShowInheritanceList (flags) && c.ClassType != ClassType.Enum) {
				if (c.BaseTypes.Count > 0) {
					builder.Append(" : ");
					for (int i = 0; i < c.BaseTypes.Count; ++i) {
						builder.Append(c.BaseTypes[i]);
						if (i + 1 < c.BaseTypes.Count) {
							builder.Append(", ");
						}
					}
				}
			}
			
			if (IncludeBodies (flags)) {
				builder.Append("\n{");
			}
			
			return builder.ToString();		
		}
		
		public override string ConvertEnd(IClass c, ConversionFlags flags)
		{
			return "}";
		}
		
		public override string Convert(IField field, ConversionFlags flags)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append(Convert(field.Modifiers));
			
			if (ShowMemberModifiers (flags)) {
				if (field.IsStatic && field.IsLiteral)
					AppendPangoHtmlTag (builder, "const ", "i", flags);
				else if (field.IsStatic)
					AppendPangoHtmlTag (builder, "static ", "i", flags);
				
				if (!field.IsReadonly) {
					AppendPangoHtmlTag (builder, "mutable ", "i", flags);
				}
			}
			
			if (UseFullyQualifiedMemberNames (flags))
				AppendPangoHtmlTag (builder, field.FullyQualifiedName, "b", flags);
			else
				AppendPangoHtmlTag (builder, field.Name, "b", flags);
				
		    if (field.ReturnType != null) {
				builder.Append(" : " + Convert(field.ReturnType));
				builder.Append(' ');
			}
			
			if (IncludeBodies (flags)) builder.Append(";");
			
			return builder.ToString();			
		}
		
		public override string Convert(IProperty property, ConversionFlags flags)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append(Convert(property.Modifiers));
			
			if (ShowMemberModifiers (flags)) {
				builder.Append(GetModifier(property, flags));
			}
			
			if (UseFullyQualifiedMemberNames (flags))
				AppendPangoHtmlTag (builder, property.FullyQualifiedName, "b", flags);
			else
				AppendPangoHtmlTag (builder, property.Name, "b", flags);
				
		    if (property.ReturnType != null) {
				builder.Append(" : " + Convert(property.ReturnType));
				builder.Append(' ');
			}
			
			if (property.Parameters.Count > 0) {
				builder.Append(" (");
				if (IncludeHTMLMarkup (flags)) builder.Append("<br>");
			
				for (int i = 0; i < property.Parameters.Count; ++i) {
					if (IncludeHTMLMarkup (flags)) builder.Append("&nbsp;&nbsp;&nbsp;");
					builder.Append(Convert(property.Parameters[i]));
					if (i + 1 < property.Parameters.Count) {
						builder.Append(", ");
					}
					if (IncludeHTMLMarkup (flags)) builder.Append("<br>");
				}
				
				builder.Append(')');
			}
			
			builder.Append(" { ");
			
			if (property.CanGet) {
				builder.Append("get; ");
			}
			if (property.CanSet) {
				builder.Append("set; ");
			}
			
			builder.Append(" } ");
			
			return builder.ToString();
		}
		
		public override string Convert(IEvent e, ConversionFlags flags)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append(Convert(e.Modifiers));
			
			if (ShowMemberModifiers (flags)) {
				builder.Append(GetModifier(e, flags));
			}
			
			if (UseFullyQualifiedMemberNames (flags))
				AppendPangoHtmlTag (builder, e.FullyQualifiedName, "b", flags);
			else
				AppendPangoHtmlTag (builder, e.Name, "b", flags);
							
			if (e.ReturnType != null) {
				builder.Append(" : " + Convert(e.ReturnType));
				builder.Append(' ');
			}
			
			if (IncludeBodies (flags)) builder.Append(";");
			
			return builder.ToString();
		}
		
		public override string Convert(IIndexer m, ConversionFlags flags)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(Convert(m.Modifiers));
			
			if (ShowMemberModifiers (flags) && m.IsStatic)
				AppendPangoHtmlTag (builder, "static", "i", flags);
			
			if (UseFullyQualifiedMemberNames (flags))
				AppendPangoHtmlTag (builder, m.FullyQualifiedName, "b", flags);
			else
				AppendPangoHtmlTag (builder, m.Name, "b", flags);
			
			builder.Append(" [");
			if (IncludeHTMLMarkup (flags)) builder.Append("<br>");

			for (int i = 0; i < m.Parameters.Count; ++i) {
				if (IncludeHTMLMarkup (flags)) builder.Append("&nbsp;&nbsp;&nbsp;");
				builder.Append(Convert(m.Parameters[i]));
				if (i + 1 < m.Parameters.Count) {
					builder.Append(", ");
				}
				if (IncludeHTMLMarkup (flags)) builder.Append("<br>");
			}
			
			builder.Append(']');
			
			if (m.ReturnType != null) {
				builder.Append(" : " + Convert(m.ReturnType));
				builder.Append(' ');
			}
			
			if (IncludeBodies (flags)) builder.Append(";");
			
			return builder.ToString();
		}
		
		public override string Convert(IMethod m, ConversionFlags flags)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(Convert(m.Modifiers));
			
			if (ShowMemberModifiers (flags)) {
				builder.Append(GetModifier(m, flags));
			}
			
			if (m.IsConstructor) {
				AppendPangoHtmlTag (builder, "this", "b", flags);
			} else {
				if (UseFullyQualifiedMemberNames (flags))
					AppendPangoHtmlTag (builder, m.FullyQualifiedName, "b", flags);
				else
					AppendPangoHtmlTag (builder, m.Name, "b", flags);
			}
			
			builder.Append(" (");
			if (IncludeHTMLMarkup (flags)) builder.Append("<br>");
			
			for (int i = 0; i < m.Parameters.Count; ++i) {
				if (IncludeHTMLMarkup (flags)) builder.Append("&nbsp;&nbsp;&nbsp;");
				builder.Append(Convert(m.Parameters[i]));
				if (i + 1 < m.Parameters.Count) {
					builder.Append(", ");
				}
				if (IncludeHTMLMarkup (flags)) builder.Append("<br>");
			}
			
			builder.Append(')');
			
			if (m.ReturnType != null) {
				builder.Append(" : " + Convert(m.ReturnType));
				builder.Append(' ');
			}
			
			if (IncludeBodies (flags)) {
				if (m.DeclaringType != null) {
					if (m.DeclaringType.ClassType == ClassType.Interface) {
						builder.Append(";");
					} else {
						builder.Append(" {");
					}
				} else {
					builder.Append(" {");
				}
			}
			
			return builder.ToString();
		}
		
		public override string ConvertEnd(IMethod m, ConversionFlags flags)
		{
			return "}";
		}
		
		public override string Convert(IReturnType returnType, ConversionFlags flags)
		{
			if (returnType == null) {
				return String.Empty;
			}
			StringBuilder builder = new StringBuilder();
			
			bool linkSet = false;
			
			//if (UseLinkArrayList) {
				//SharpAssemblyReturnType ret = returnType as SharpAssemblyReturnType;
				//if (ret != null) {
				//	if (ret.UnderlyingClass != null) {
				//		builder.Append("<a href='as://" + linkArrayList.Add(ret.UnderlyingClass) + "'>");
				//		linkSet = true;
				//	}
				//}
			//}
			
			if (typeConversionTable[returnType.FullyQualifiedName] != null) {
				builder.Append(typeConversionTable[returnType.FullyQualifiedName].ToString());
			} else {
				if (UseFullyQualifiedNames (flags)) {
					builder.Append(returnType.FullyQualifiedName);
				} else {
					builder.Append(returnType.Name);
				}
			}
			
			if (linkSet) {
				builder.Append("</a>");
			}
			
			for (int i = 0; i < returnType.PointerNestingLevel; ++i) {
				builder.Append('*');
			}
			
			if (returnType.ArrayCount > 0)
			    return "array [" + builder.ToString () + "]";
			else
			    return builder.ToString ();
		}
		
		public override string Convert(IParameter param, ConversionFlags flags)
		{
			StringBuilder builder = new StringBuilder();
			
			if (param.IsRef)
				AppendPangoHtmlTag (builder, "ref ", "i", flags);
			else if (param.IsOut)
				AppendPangoHtmlTag (builder, "out ", "i", flags);
			else if (param.IsParams)
				AppendPangoHtmlTag (builder, "params ", "i", flags);
			
			if (ShowParameterNames (flags)) {
				builder.Append(param.Name);
			}
			
			builder.Append(" : " + Convert(param.ReturnType));

			return builder.ToString();
		}
		
		// pango has some problems with
		// <i>static </i>bool <b>Equals</b> (<i></i>object a, <i></i>object b)
		// it will make "object a" italics. so rather tan appending a markup
		// tag if there might be a modifier, we only do it if there is.
		void AppendPangoHtmlTag (StringBuilder sb, string str, string tag, ConversionFlags flags)
		{
			if (IncludeHTMLMarkup (flags) | IncludePangoMarkup (flags)) sb.Append ('<').Append (tag).Append ('>');
			
			sb.Append (str);
			
			if (IncludeHTMLMarkup (flags) | IncludePangoMarkup (flags)) sb.Append ("</").Append (tag).Append ('>');
		}
		
		public override string WrapAttribute(string attribute)
		{
			return "[" + attribute + "]";
		}
		
		public override string WrapComment(string comment)
		{
			return "// " + comment;
		}

		public override string GetIntrinsicTypeName(string dotNetTypeName)
		{
			if (typeConversionTable[dotNetTypeName] != null) {
				return (string)typeConversionTable[dotNetTypeName];
			}
			return dotNetTypeName;
		}
		
	}
}
