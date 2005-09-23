// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Text;

using MonoDevelop.Internal.Parser;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Services
{
	public class VBAmbience :  AbstractAmbience
	{
		static string[,] typeConversionList = new string[,] {
			{"System.String",  "String"},
			{"System.Single",  "Single"},
			{"System.Int16",   "Short"},
			{"System.Void",    "Void"},
			{"System.Object",  "Object"},
			{"System.Int64",   "Long"},
			{"System.Int32",   "Integer"},
			{"System.Double",  "Double"},
			{"System.Char",    "Char"},
			{"System.Boolean", "Boolean"},
			{"System.Byte",    "Byte"},
			{"System.Decimal", "Decimal"},
			{"System.DateTime",  "Date"},
		};
		
		static Hashtable typeConversionTable = new Hashtable();
		
		public const bool ShowReturnType=true;
		
		static VBAmbience()
		{
			for (int i = 0; i < typeConversionList.GetLength(0); ++i) {
				typeConversionTable[typeConversionList[i, 0]] = typeConversionList[i, 1];
			}
		}
		
		string GetModifier(IDecoration decoration)
		{
			StringBuilder builder = new StringBuilder();
			
			if (IncludeHTMLMarkup) {
				builder.Append("<i>");
			}
			
			if (decoration.IsStatic) {
				builder.Append("Shared ");
			} 
			if (decoration.IsAbstract) {
				builder.Append("MustOverride ");
			} else if (decoration.IsFinal) {
				builder.Append("NotOverridable ");
			} else if (decoration.IsVirtual) {
				builder.Append("Overridable ");
			} else if (decoration.IsOverride) {
				builder.Append("Overrides ");
			} else if (decoration.IsNew) {
				builder.Append("Shadows ");
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("</i>");
			}
			
			return builder.ToString();
		}
		
		public override string Convert(ModifierEnum modifier)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowAccessibility) {
				if ((modifier & ModifierEnum.Public) == ModifierEnum.Public) {
					builder.Append("Public");
				} else if ((modifier & ModifierEnum.Private) == ModifierEnum.Private) {
					builder.Append("Private");
				} else if ((modifier & (ModifierEnum.Protected | ModifierEnum.Internal)) == (ModifierEnum.Protected | ModifierEnum.Internal)) {
					builder.Append("Protected Friend");
				} else if ((modifier & ModifierEnum.ProtectedOrInternal) == ModifierEnum.ProtectedOrInternal) {
					builder.Append("Protected Friend");
				} else if ((modifier & ModifierEnum.Internal) == ModifierEnum.Internal) {
					builder.Append("Friend");
				} else if ((modifier & ModifierEnum.Protected) == ModifierEnum.Protected) {
					builder.Append("Protected");
				}
				builder.Append(' ');
			}
			return builder.ToString();
		}
		
		public override string Convert(IClass c)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append(Convert(c.Modifiers));
			
			if (IncludeHTMLMarkup) {
				builder.Append("<i>");
			}
			
			if (ShowModifiers) {
				if (c.IsSealed) {
					if (c.ClassType == ClassType.Class) {
						builder.Append("NotInheritable ");
					}
				} else if (c.IsAbstract && c.ClassType != ClassType.Interface) {
					builder.Append("MustInherit ");
				}
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("</i>");
			}
			
			switch (c.ClassType) {
				case ClassType.Delegate:
					builder.Append("Delegate ");
					if (ShowReturnType) {
						foreach (IMethod m in c.Methods) {
							if (m.Name != "Invoke") {
								continue;
							}
							
							if (m.ReturnType == null || m.ReturnType.FullyQualifiedName == "System.Void") {
								builder.Append("Sub");
							} else {
								builder.Append("Function");
							}
						}
					}
					break;
				case ClassType.Class:
					builder.Append("Class");
					break;
				case ClassType.Struct:
					builder.Append("Structure");
					break;
				case ClassType.Interface:
					builder.Append("Interface");
					break;
				case ClassType.Enum:
					builder.Append("Enum");
					break;
			}
			builder.Append(' ');
			
			if (IncludeHTMLMarkup) {
				builder.Append("<b>");
			}
			
			if (UseFullyQualifiedMemberNames) {
				builder.Append(c.FullyQualifiedName);
			} else {
				builder.Append(c.Name);
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("</b>");
			}
			
			if (c.ClassType == ClassType.Delegate) {
				builder.Append("(");
				if (IncludeHTMLMarkup) builder.Append("<br>");
				
				foreach (IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					for (int i = 0; i < m.Parameters.Count; ++i) {
						if (IncludeHTMLMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
						
						builder.Append(Convert(m.Parameters[i]));
						if (i + 1 < m.Parameters.Count) builder.Append(", ");

						if (IncludeHTMLMarkup) builder.Append("<br>");
					}
				}

				builder.Append(")");
				
				foreach (IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					if (m.ReturnType == null || m.ReturnType.FullyQualifiedName == "System.Void") {
					} else {
						if (ShowReturnType) {
							builder.Append(" As ");
							builder.Append(Convert(m.ReturnType));
						}
					}
				}

			} else if (ShowInheritanceList) {
				if (c.BaseTypes.Count > 0) {
					builder.Append(" Inherits ");
					for (int i = 0; i < c.BaseTypes.Count; ++i) {
						builder.Append(c.BaseTypes[i]);
						if (i + 1 < c.BaseTypes.Count) {
							builder.Append(", ");
						}
					}
				}
			}
			
			return builder.ToString();		
		}
		
		public override string ConvertEnd(IClass c)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append("End ");
			
			switch (c.ClassType) {
				case ClassType.Delegate:
					builder.Append("Delegate");
					break;
				case ClassType.Class:
					builder.Append("Class");
					break;
				case ClassType.Struct:
					builder.Append("Structure");
					break;
				case ClassType.Interface:
					builder.Append("Interface");
					break;
				case ClassType.Enum:
					builder.Append("Enum");
					break;
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IField field)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append(Convert(field.Modifiers));
			
			if (IncludeHTMLMarkup) {
				builder.Append("<i>");
			}
			
			if (ShowModifiers) {
				if (field.IsStatic && field.IsLiteral) {
					builder.Append("Const ");
				} else if (field.IsStatic) {
					builder.Append("Shared ");
				}
			}
						
			if (IncludeHTMLMarkup) {
				builder.Append("</i>");
				builder.Append("<b>");
			}
			
			if (UseFullyQualifiedMemberNames) {
				builder.Append(field.FullyQualifiedName);
			} else {
				builder.Append(field.Name);
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("</b>");
			}
			
			if (field.ReturnType != null && ShowReturnType) {
				builder.Append(" As ");
				builder.Append(Convert(field.ReturnType));
			}			
			
			return builder.ToString();			
		}
		
		public override string Convert(IProperty property)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append(Convert(property.Modifiers));
			
			if (ShowModifiers) {
				builder.Append(GetModifier(property));
			}
			
			if (property.CanGet && !property.CanSet) {
				builder.Append("ReadOnly ");
			}
			
			if (property.CanSet && !property.CanGet) {
				builder.Append("WriteOnly ");
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("<b>");
			}
			
			if (UseFullyQualifiedMemberNames) {
				builder.Append(property.FullyQualifiedName);
			} else {
				builder.Append(property.Name);
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("</b>");
			}
			
			if (property.Parameters.Count > 0) {
				builder.Append("(");
				if (IncludeHTMLMarkup) builder.Append("<br>");
				
				for (int i = 0; i < property.Parameters.Count; ++i) {
					if (IncludeHTMLMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
					builder.Append(Convert(property.Parameters[i]));
					if (i + 1 < property.Parameters.Count) {
						builder.Append(", ");
					}
					if (IncludeHTMLMarkup) builder.Append("<br>");
				}
				
				builder.Append(')');
			}
			
			if (property.ReturnType != null && ShowReturnType) {
				builder.Append(" As ");
				builder.Append(Convert(property.ReturnType));
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IEvent e)
		{
			StringBuilder builder = new StringBuilder();
			
			builder.Append(Convert(e.Modifiers));
			
			if (ShowModifiers) {
				builder.Append(GetModifier(e));
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("<b>");
			}
			
			if (UseFullyQualifiedMemberNames) {
				builder.Append(e.FullyQualifiedName);
			} else {
				builder.Append(e.Name);
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("</b>");
			}
			
			if (e.ReturnType != null && ShowReturnType) {
				builder.Append(" As ");
				builder.Append(Convert(e.ReturnType));
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IIndexer m)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(Convert(m.Modifiers));
			
			if (ShowModifiers) {
				if (m.IsStatic) {
					builder.Append("Shared ");
				}
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("<b>");
			}
			
			if (UseFullyQualifiedMemberNames) {
				builder.Append(m.FullyQualifiedName);
			} else {
				builder.Append(m.Name);
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("</b>");
			}
			
			builder.Append("Item(");
			if (IncludeHTMLMarkup) builder.Append("<br>");

			for (int i = 0; i < m.Parameters.Count; ++i) {
				if (IncludeHTMLMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
				builder.Append(Convert(m.Parameters[i]));
				if (i + 1 < m.Parameters.Count) {
					builder.Append(", ");
				}
				if (IncludeHTMLMarkup) builder.Append("<br>");
			}
			
			builder.Append(")");
			
			if (m.ReturnType != null && ShowReturnType) {
				builder.Append(" As ");
				builder.Append(Convert(m.ReturnType));
			}			
			
			return builder.ToString();
		}
		
		public override string Convert(IMethod m)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(Convert(m.Modifiers));
			
			if (ShowModifiers) {
				builder.Append(GetModifier(m));
			}
			if (ShowReturnType) {
				if (m.ReturnType == null || m.ReturnType.FullyQualifiedName == "System.Void") {
					builder.Append("Sub ");
				} else {
					builder.Append("Function ");
				}
			}

			string dispName = UseFullyQualifiedMemberNames ? m.FullyQualifiedName : m.Name;
			if (m.Name == "ctor" || m.Name == "cctor" || m.Name == "#ctor" || m.Name == "#cctor" || m.IsConstructor) {
				dispName = "New";
			}
			
			if (IncludeHTMLMarkup) {
				builder.Append("<b>");
			}
			
			builder.Append(dispName);
			
			if (IncludeHTMLMarkup) {
				builder.Append("</b>");
			}
			
			builder.Append("(");
			if (IncludeHTMLMarkup) builder.Append("<br>");

			for (int i = 0; i < m.Parameters.Count; ++i) {
				if (IncludeHTMLMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
				builder.Append(Convert(m.Parameters[i]));
				if (i + 1 < m.Parameters.Count) {
					builder.Append(", ");
				}
				if (IncludeHTMLMarkup) builder.Append("<br>");
			}
			
			builder.Append(')');
			
			if (ShowReturnType && m.ReturnType != null && m.ReturnType.FullyQualifiedName != "System.Void") {
				builder.Append(" As ");
				builder.Append(Convert(m.ReturnType));
			}
			
			return builder.ToString();
		}
		
		public override string ConvertEnd(IMethod m)
		{
			if (m.ReturnType == null || m.ReturnType.FullyQualifiedName == "System.Void") {
				return "End Sub";
			} else {
				return "End Function";
			}
		}
		
		public override string Convert(IReturnType returnType)
		{
			if (returnType == null) {
				return String.Empty;
			}
			StringBuilder builder = new StringBuilder();
			
			bool linkSet = false;
			
			if (UseLinkArrayList) {
				SharpAssemblyReturnType ret = returnType as SharpAssemblyReturnType;
				if (ret != null) {
					if (ret.UnderlyingClass != null) {
						builder.Append("<a href='as://" + linkArrayList.Add(ret.UnderlyingClass) + "'>");
						linkSet = true;
					}
				}
			}
			
			if (returnType.FullyQualifiedName != null && typeConversionTable[returnType.FullyQualifiedName] != null) {
				builder.Append(typeConversionTable[returnType.FullyQualifiedName].ToString());
			} else {
				builder.Append(UseFullyQualifiedNames ? returnType.FullyQualifiedName : returnType.Name);
			}
			
			if (linkSet) {
				builder.Append("</a>");
			}

			for (int i = 0; i < returnType.PointerNestingLevel; ++i) {
				builder.Append('*');
			}
			
			for (int i = 0; i < returnType.ArrayCount; ++i) {
				builder.Append('(');
				for (int j = 1; j < returnType.ArrayDimensions[i]; ++j) {
					builder.Append(',');
				}
				builder.Append(')');
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IParameter param)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowParameterNames) {
				if (IncludeHTMLMarkup) {
					builder.Append("<i>");
				}
			
				if (param.IsRef || param.IsOut) {
					builder.Append("ByRef ");
				} else if (param.IsParams) {
					builder.Append("ByVal ParamArray ");
				} else  {
					builder.Append("ByVal ");
				}
				if (IncludeHTMLMarkup) {
					builder.Append("</i>");
				}
			
			
				builder.Append(param.Name);
				builder.Append(" As ");
			}

			builder.Append(Convert(param.ReturnType));

			return builder.ToString();
		}

		public override string WrapAttribute(string attribute)
		{
			return "<" + attribute + ">";
		}
		
		public override string WrapComment(string comment)
		{
			return "' " + comment;
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
