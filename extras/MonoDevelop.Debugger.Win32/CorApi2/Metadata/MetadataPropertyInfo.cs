using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorMetadata.NativeApi;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorMetadata
{
	public class MetadataPropertyInfo: PropertyInfo
	{
		private IMetadataImport m_importer;
		private int m_propertyToken;
		private MetadataType m_declaringType;

		private string m_name;
		private PropertyAttributes m_propAttributes;

		int m_pmdSetter;
		int m_pmdGetter;

		MetadataMethodInfo m_setter;
		MetadataMethodInfo m_getter;

		internal MetadataPropertyInfo (IMetadataImport importer, int propertyToken, MetadataType declaringType)
		{
			m_importer = importer;
			m_propertyToken = propertyToken;
			m_declaringType = declaringType;

			int mdTypeDef;
			int pchProperty;
			int pdwPropFlags;
			IntPtr ppvSig;
			int pbSig;
			int pdwCPlusTypeFlag;
			IntPtr ppDefaultValue;
			int pcchDefaultValue;
			int rmdOtherMethod;
			int pcOtherMethod;

			m_importer.GetPropertyProps (
				m_propertyToken,
				out mdTypeDef,
				null,
				0,
				out pchProperty,
				out pdwPropFlags,
				out ppvSig,
				out pbSig,
				out pdwCPlusTypeFlag,
				out ppDefaultValue,
				out pcchDefaultValue,
				out m_pmdSetter,
				out m_pmdGetter,
				out rmdOtherMethod,
				0,
				out pcOtherMethod);

			StringBuilder szProperty = new StringBuilder (pchProperty);
			m_importer.GetPropertyProps (
				m_propertyToken,
				out mdTypeDef,
				szProperty,
				pchProperty,
				out pchProperty,
				out pdwPropFlags,
				out ppvSig,
				out pbSig,
				out pdwCPlusTypeFlag,
				out ppDefaultValue,
				out pcchDefaultValue,
				out m_pmdSetter,
				out m_pmdGetter,
				out rmdOtherMethod,
				0,
				out pcOtherMethod);

			m_propAttributes = (PropertyAttributes) pdwPropFlags;
			m_name = szProperty.ToString ();
		}

		public override PropertyAttributes Attributes
		{
			get { return m_propAttributes; }
		}

		public override bool CanRead
		{
			get { return m_pmdGetter != 0; }
		}

		public override bool CanWrite
		{
			get { return m_pmdSetter != 0; }
		}

		public override MethodInfo[] GetAccessors (bool nonPublic)
		{
			throw new NotImplementedException ();
		}

		public override MethodInfo GetGetMethod (bool nonPublic)
		{
			if (m_getter == null) {
				if (m_pmdGetter != 0)
					m_getter = new MetadataMethodInfo (m_importer, m_pmdGetter);
			}
			return m_getter;
		}

		public override ParameterInfo[] GetIndexParameters ( )
		{
			MethodInfo mi = GetGetMethod ();
			if (mi == null)
				return new ParameterInfo[0];
			return mi.GetParameters ();
		}

		public override MethodInfo GetSetMethod (bool nonPublic)
		{
			if (m_setter == null) {
				if (m_pmdSetter != 0)
					m_setter = new MetadataMethodInfo (m_importer, m_pmdSetter);
			}
			return m_setter;
		}

		public override object GetValue (object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}

		public override Type PropertyType
		{
			get { throw new NotImplementedException (); }
		}

		public override void SetValue (object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException ();
		}

		public override Type DeclaringType
		{
			get { throw new NotImplementedException (); }
		}

		public override object[] GetCustomAttributes (Type attributeType, bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override object[] GetCustomAttributes (bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override bool IsDefined (Type attributeType, bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override string Name
		{
			get { return m_name; }
		}

		public override Type ReflectedType
		{
			get { throw new NotImplementedException (); }
		}
	}
}
