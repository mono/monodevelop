using System;
using System.Collections;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.Projects.Ambience
{
	public abstract class Ambience
	{		
		public static bool ShowAccessibility(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.ShowAccessibility) == ConversionFlags.ShowAccessibility;
		}

		public static bool ShowParameterNames(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.ShowParameterNames) == ConversionFlags.ShowParameterNames;
		}
		
		public static bool UseFullyQualifiedNames(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.UseFullyQualifiedNames) == ConversionFlags.UseFullyQualifiedNames;
		}
		
		public static bool UseIntrinsicTypeNames(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.UseIntrinsicTypeNames) == ConversionFlags.UseIntrinsicTypeNames;
		}
		
		public static bool ShowMemberModifiers(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.ShowMemberModifiers) == ConversionFlags.ShowMemberModifiers;
		}
		
		public static bool ShowInheritanceList(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.ShowInheritanceList) == ConversionFlags.ShowInheritanceList;
		}
		
		public static bool IncludeHTMLMarkup(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.IncludeHTMLMarkup) == ConversionFlags.IncludeHTMLMarkup;
		}
		
		public static bool IncludePangoMarkup(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.IncludePangoMarkup) == ConversionFlags.IncludePangoMarkup;
		}
		
//		public static bool UseLinkArrayList(ConversionFlags conversionFlags)
//		{
//			return (conversionFlags & ConversionFlags.UseLinkArrayList) == ConversionFlags.UseLinkArrayList;
//		}
		
		public static bool UseFullyQualifiedMemberNames(ConversionFlags conversionFlags)
		{
			return UseFullyQualifiedNames(conversionFlags) && !((conversionFlags & ConversionFlags.QualifiedNamesOnlyForReturnTypes) == ConversionFlags.QualifiedNamesOnlyForReturnTypes);
		}
		
		public static bool IncludeBodies(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.IncludeBodies) == ConversionFlags.IncludeBodies;
		}
		
		public static bool ShowClassModifiers(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.ShowClassModifiers) == ConversionFlags.ShowClassModifiers;
		}
		
		public static bool ShowGenericParameters(ConversionFlags conversionFlags)
		{
			return (conversionFlags & ConversionFlags.ShowGenericParameters) == ConversionFlags.ShowGenericParameters;
		}
		
		public string Convert(ILanguageItem item)
		{
			return Convert (item, ConversionFlags.StandardConversionFlags);
		}
		
		public string Convert(ILanguageItem item, ConversionFlags conversionFlags)
		{
			if (item is IClass)
				return Convert (item as IClass, conversionFlags);
			else if (item is IEvent)
				return Convert (item as IEvent, conversionFlags);
			else if (item is IField)
				return Convert (item as IField, conversionFlags);
			else if (item is IIndexer)
				return Convert (item as IIndexer, conversionFlags);
			else if (item is IMethod)
				return Convert (item as IMethod, conversionFlags);
			else if (item is IProperty)
				return Convert (item as IProperty, conversionFlags);
			else if (item is LocalVariable)
				return Convert (item as LocalVariable, conversionFlags);
			else
				return item.Name;
		}
		
		public string Convert (IClass c)
		{
			return Convert(c, ConversionFlags.StandardConversionFlags, null);
		}
		
		public string Convert (IClass c, ConversionFlags flags)
		{
			return Convert(c, flags, null);
		}
		
		public string ConvertEnd (IClass c)
		{
			return ConvertEnd (c, ConversionFlags.StandardConversionFlags);
		}

		public string Convert (IEvent e)
		{
			return Convert(e, ConversionFlags.StandardConversionFlags, null);
		}
		
		public string Convert (IEvent e, ConversionFlags flags)
		{
			return Convert(e, flags, null);
		}
		
		public string Convert (IField c)
		{
			return Convert(c, ConversionFlags.StandardConversionFlags, null);
		}
		
		public string Convert (IField c, ConversionFlags flags)
		{
			return Convert(c, flags, null);
		}
		
		public string Convert (IIndexer indexer)
		{
			return Convert(indexer, ConversionFlags.StandardConversionFlags, null);
		}
		
		public string Convert (IIndexer indexer, ConversionFlags flags)
		{
			return Convert(indexer, flags, null);
		}
		
		public string Convert (IMethod m)
		{
			return Convert (m, ConversionFlags.StandardConversionFlags, null);
		}
		
		public string Convert (IMethod m, ConversionFlags flags)
		{
			return Convert (m, flags, null);
		}
		
		public string ConvertEnd (IMethod m)
		{
			return ConvertEnd(m, ConversionFlags.StandardConversionFlags);
		}
		
		public string Convert (IProperty property)
		{
			return Convert (property, ConversionFlags.StandardConversionFlags, null);
		}
		
		public string Convert (IProperty property, ConversionFlags flags)
		{
			return Convert (property, flags, null);
		}
		
		public string Convert (IParameter param)
		{
			return Convert (param, ConversionFlags.StandardConversionFlags, null);
		}
		
		public string Convert (IParameter param, ConversionFlags flags)
		{
			return Convert (param, flags, null);
		}
		
		public string Convert (IReturnType returnType)
		{
			return Convert (returnType, ConversionFlags.StandardConversionFlags, null);
		}
		
		public string Convert (IReturnType returnType, ConversionFlags flags)
		{
			return Convert (returnType, flags, null);
		}
		
		public string Convert (ModifierEnum modifier)
		{
			return Convert(modifier, ConversionFlags.StandardConversionFlags);
		}

		public string Convert(LocalVariable localVariable)
		{
			return Convert (localVariable, ConversionFlags.StandardConversionFlags, null);
		}

		public string Convert(LocalVariable localVariable, ConversionFlags flags)
		{
			return Convert (localVariable, flags, null);
		}

		protected string GetResolvedTypeName (string dotNetTypeName, ConversionFlags flags, ITypeNameResolver resolver)
		{
			if (UseIntrinsicTypeNames (flags)) {
				string tn = GetIntrinsicTypeName (dotNetTypeName);
				if (tn != null && tn != dotNetTypeName)
					return tn;
			}
			if (resolver != null) {
				string tn = resolver.ResolveName (dotNetTypeName);
				if (tn != null)
					return tn;
			}
			return dotNetTypeName;
		}
		
		public abstract string Convert (IClass c, ConversionFlags flags, ITypeNameResolver resolver);
		public abstract string ConvertEnd (IClass c, ConversionFlags flags);
		public abstract string Convert (IEvent e, ConversionFlags flags, ITypeNameResolver resolver);
		public abstract string Convert (IField c, ConversionFlags flags, ITypeNameResolver resolver);
		public abstract string Convert (IIndexer indexer, ConversionFlags flags, ITypeNameResolver resolver);
		public abstract string Convert (IMethod m, ConversionFlags flags, ITypeNameResolver resolver);
		public abstract string Convert (IProperty property, ConversionFlags flags, ITypeNameResolver resolver);
		public abstract string ConvertEnd (IMethod m, ConversionFlags flags);
		public abstract string Convert (IParameter param, ConversionFlags flags, ITypeNameResolver resolver);
		public abstract string Convert (IReturnType returnType, ConversionFlags flags, ITypeNameResolver resolver);
		public abstract string Convert (ModifierEnum modifier, ConversionFlags flags);
		public abstract string Convert (LocalVariable localVariable, ConversionFlags flags, ITypeNameResolver resolver);
		
		public abstract string WrapAttribute (string attribute);
		public abstract string WrapComment (string comment);
		public abstract string GetIntrinsicTypeName (string dotNetTypeName);
		
	}
}
