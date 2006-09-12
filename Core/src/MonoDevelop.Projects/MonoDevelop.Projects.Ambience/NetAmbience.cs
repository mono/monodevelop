// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Ambience
{
	public class NetAmbience : Ambience
	{		
		public override string Convert(ModifierEnum modifier, ConversionFlags conversionFlags)
		{
			return "";
		}
		
		public override string Convert(IClass c, ConversionFlags conversionFlags)
		{
			StringBuilder builder = new StringBuilder();
			
			if (ShowClassModifiers(conversionFlags)) {
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
						builder.Append("Enumeration");
						break;
				}
				builder.Append(' ');
			}
			
			if (UseFullyQualifiedNames(conversionFlags))
				builder.Append (c.FullyQualifiedName);
			else
				builder.Append (c.Name);
			
			if (c.GenericParameters != null && c.GenericParameters.Count > 0)
 			{
 				builder.Append("&lt;");
 				for (int i = 0; i < c.GenericParameters.Count; i++)
 				{
 					builder.Append(c.GenericParameters[i].Name);
 					if (i + 1 < c.GenericParameters.Count) builder.Append(", ");
 				}
 				builder.Append("&gt;");
 			}
				
			if (c.ClassType == ClassType.Delegate) {
				builder.Append('(');
				
				foreach (IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					for (int i = 0; i < m.Parameters.Count; ++i) {
						builder.Append(Convert(m.Parameters[i]));
						if (i + 1 < m.Parameters.Count) {
							builder.Append(", ");
						}
					}					
				}
				
				builder.Append(')');
				if (c.Methods.Count > 0) {
					builder.Append(" : ");
					builder.Append(Convert(c.Methods[0].ReturnType));
				}
			} else if (ShowInheritanceList(conversionFlags)) {
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
			
			if (IncludeBodies(conversionFlags)) {
				builder.Append("\n{");
			}
			
			return builder.ToString();		
		}
		
		public override string ConvertEnd(IClass c, ConversionFlags conversionFlags)
		{
			return "}";
		}
		
		public override string Convert(IField field, ConversionFlags conversionFlags)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowMemberModifiers(conversionFlags)) {
				builder.Append("Field");
				builder.Append(' ');
			}
			
			if (UseFullyQualifiedNames(conversionFlags)) {
				builder.Append(field.FullyQualifiedName);
			} else {
				builder.Append(field.Name);
			}
			
			if (field.ReturnType != null) {
				builder.Append(" : ");
				builder.Append(Convert(field.ReturnType));
			}
			
			return builder.ToString();			
		}
		
		public override string Convert(IProperty property, ConversionFlags conversionFlags)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowMemberModifiers(conversionFlags)) {
				builder.Append("Property");
				builder.Append(' ');
			}
			
			if (UseFullyQualifiedNames(conversionFlags)) {
				builder.Append(property.FullyQualifiedName);
			} else {
				builder.Append(property.Name);
			}

			if (property.Parameters.Count > 0) builder.Append('(');
			
			for (int i = 0; i < property.Parameters.Count; ++i) {
				builder.Append(Convert(property.Parameters[i]));
				if (i + 1 < property.Parameters.Count) {
					builder.Append(", ");
				}
			}
			
			if (property.Parameters.Count > 0) builder.Append(')');
			
			
			if (property.ReturnType != null) {
				builder.Append(" : ");
				builder.Append(Convert(property.ReturnType));
			}
			return builder.ToString();
		}
		
		public override string Convert(IEvent e, ConversionFlags conversionFlags)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowMemberModifiers(conversionFlags)) {
				builder.Append("Event ");
			}
			
			if (UseFullyQualifiedNames(conversionFlags)) {
				builder.Append(e.FullyQualifiedName);
			} else {
				builder.Append(e.Name);
			}
			if (e.ReturnType != null) {
				builder.Append(" : ");
				builder.Append(Convert(e.ReturnType));
			}
			return builder.ToString();
		}
		
		public override string Convert(IIndexer m, ConversionFlags conversionFlags)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowMemberModifiers(conversionFlags)) {
				builder.Append("Indexer ");
			}
			
			if (UseFullyQualifiedNames(conversionFlags)) {
				builder.Append(m.FullyQualifiedName);
			} else {
				builder.Append(m.Name);
			}
			builder.Append('[');
			for (int i = 0; i < m.Parameters.Count; ++i) {
				builder.Append(Convert(m.Parameters[i]));
				if (i + 1 < m.Parameters.Count) {
					builder.Append(", ");
				}
			}
			
			builder.Append("]");
			if (m.ReturnType != null) {
				builder.Append(" : ");
				builder.Append(Convert(m.ReturnType));
			}
			return builder.ToString();
		}
		
		public override string Convert(IMethod m, ConversionFlags conversionFlags)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowMemberModifiers(conversionFlags)) {
				builder.Append("Method ");
			}
			
			if (UseFullyQualifiedNames(conversionFlags)) {
				builder.Append(m.FullyQualifiedName);
			} else {
				builder.Append(m.Name);
			}
			builder.Append('(');
			for (int i = 0; i < m.Parameters.Count; ++i) {
				builder.Append(Convert(m.Parameters[i]));
				if (i + 1 < m.Parameters.Count) {
					builder.Append(", ");
				}
			}
			
			builder.Append(")");
			if (m.ReturnType != null) {
				builder.Append(" : ");
				builder.Append(Convert(m.ReturnType));
			}
			
			if (IncludeBodies(conversionFlags)) {
				builder.Append(" {");
			}
			
			return builder.ToString();
		}
		
		public override string ConvertEnd(IMethod m, ConversionFlags conversionFlags)
		{
			return "}";
		}	
		
		public override string Convert(IReturnType returnType, ConversionFlags conversionFlags)
		{
			if (returnType == null) {
				return String.Empty;
			}
			StringBuilder builder = new StringBuilder();
			
			bool linkSet = false;
			
			//if (UseLinkArrayList(conversionFlags)) {
				//SharpAssemblyReturnType ret = returnType as SharpAssemblyReturnType;
				//if (ret != null) {
				//	if (ret.UnderlyingClass != null) {
				//		builder.Append("<a href='as://" + linkArrayList.Add(ret.UnderlyingClass) + "'>");
				//		linkSet = true;
				//	}
				//}
			//}
			
			if (UseFullyQualifiedNames(conversionFlags)) {
				builder.Append(returnType.FullyQualifiedName);
			} else {
				builder.Append(returnType.Name);
			}
			
			if (linkSet) {
				builder.Append("</a>");
			}
			
			for (int i = 0; i < returnType.PointerNestingLevel; ++i) {
				builder.Append('*');
			}
			
			for (int i = 0; i < returnType.ArrayCount; ++i) {
				builder.Append('[');
				for (int j = 1; j < returnType.ArrayDimensions[i]; ++j) {
					builder.Append(',');
				}
				builder.Append(']');
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IParameter param, ConversionFlags conversionFlags)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowParameterNames(conversionFlags)) {
				builder.Append(param.Name);
				builder.Append(" : ");
			}
			builder.Append(Convert(param.ReturnType));
			if (param.IsRef) {
				builder.Append("&amp;");
			}
			return builder.ToString();
		}

		public override string Convert(LocalVariable localVariable, ConversionFlags conversionFlags)
		{
			StringBuilder builder = new StringBuilder();

			builder.Append(localVariable.Name);
			builder.Append(" : ");
			builder.Append(Convert(localVariable.ReturnType));

			return builder.ToString();
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
			return dotNetTypeName;
		}
	}
}
