//
// TypeReference.cs: Some toolbox node types use this to store a reference to 
//     .NET types in GAC and non-GAC assemblies.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.ComponentModel;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Serializable]
	[TypeConverter (typeof (TypeReferenceTypeConverter))]
	public class TypeReference
	{
		//private serialisable fields
		[ItemProperty ("location")]
		string assemblyLocation = string.Empty;
		[ItemProperty ("assembly")]
		string assemblyName = string.Empty;
		[ItemProperty ("name")]
		string typeName = string.Empty;
		
		//for deserialisation
		public TypeReference ()
		{
		}
		
		#region comparison overrides based on private fields
		
		public override bool Equals (System.Object obj)
		{
			TypeReference other = obj as TypeReference;
			return (other != null)
			    && (this.typeName == other.typeName)
			    && (this.assemblyName == other.assemblyName)
			    && (this.assemblyLocation == other.assemblyLocation);
		}
		
		public override int GetHashCode ()
		{
			return (typeName + assemblyName + assemblyLocation).GetHashCode ();
		}

		
		public bool Equals (TypeReference other)
		{
			return (other != null)
			    && (this.typeName == other.typeName)
			    && (this.assemblyName == other.assemblyName)
			    && (this.assemblyLocation == other.assemblyLocation);
		}
		
		#endregion
		
		#region convenience constructors
		
		public TypeReference (string typeName, string assemblyName)
		{
			this.typeName = typeName;
			this.assemblyName = assemblyName;
		}
		
		public TypeReference (Type type)
			: this (type.FullName, type.Assembly.FullName)
		{
			if (!type.Assembly.GlobalAssemblyCache)
				assemblyLocation = type.Assembly.Location;
		}
		
		public TypeReference (string typeName, string assemblyName, string assemblyLocation)
			: this (typeName, assemblyName)
		{
			this.assemblyLocation = assemblyLocation;
		}
		
		#endregion
		
		#region property accessors for the private fields
		
		[DisplayNameAttribute ("Assembly Name")]
		[DescriptionAttribute ("The assembly name.")]
		[ReadOnly (true)]
		public string AssemblyName {
			get { return assemblyName; }
			set { assemblyName = value; }
		}
		
		[DisplayNameAttribute ("Type Name")]
		[DescriptionAttribute ("The fully-qualified type name.")]
		[ReadOnly (true)]
		public string TypeName {
			get { return typeName; }
			set { typeName = value; }
		}
		
		[DisplayNameAttribute ("Assembly Location")]
		[DescriptionAttribute ("The location of the assembly.")]
		[ReadOnly (true)]
		public string AssemblyLocation {
			get { return assemblyLocation; }
			set { assemblyLocation = value; }
		}
		
		#endregion
		
		//loads the type referenced in the TypeReference
		//FIXME: three likely exceptions in here: need to handle them
		public Type Load ()
		{
			System.Reflection.Assembly assem = null;
			
			if (string.IsNullOrEmpty (assemblyLocation))
				//GAC assembly
				assem = System.Reflection.Assembly.Load (assemblyName);
			else
				//local assembly
				assem = System.Reflection.Assembly.LoadFile (assemblyLocation);
			
			Type type = assem.GetType (typeName, true);
			
			return type;
		}
		
		public MonoDevelop.Projects.ProjectReference GetProjectReference ()
		{
			if (string.IsNullOrEmpty (assemblyLocation)) {
				return new MonoDevelop.Projects.ProjectReference (
				    MonoDevelop.Projects.ReferenceType.Gac,
				    assemblyName);
			} else {
				return new MonoDevelop.Projects.ProjectReference (
				    MonoDevelop.Projects.ReferenceType.Assembly,
				    assemblyLocation);
			}
		}
	}
	
	public class TypeReferenceTypeConverter : ExpandableObjectConverter
	{
		public override object ConvertTo (ITypeDescriptorContext context, 
		    System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string))
				return ((TypeReference)value).TypeName;
			else
				return base.ConvertTo (context, culture, value, destinationType); 
		}
		
		public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string) || base.CanConvertTo (context, destinationType);
		}

	}
}
