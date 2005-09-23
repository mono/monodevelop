// MetadataTables.cs
// Copyright (C) 2003 Georg Brandl
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using MonoDevelop.SharpAssembly.Metadata;
using MonoDevelop.SharpAssembly.Metadata.Rows;
using MDRows = MonoDevelop.SharpAssembly.Metadata.Rows;

namespace MonoDevelop.SharpAssembly.Assembly
{
	
	/// <summary>
	/// Contains shortcuts to commonly used tables
	/// </summary>
	public class MetadataTables : object {
		
		AssemblyReader reader;
		
		public MetadataTables(AssemblyReader Reader) {
			reader = Reader;
		}

		// Shortcuts for commonly used tables
		
		public MDRows.Assembly[] Assembly {
			get {
				return (MDRows.Assembly[])reader.MetadataTable.Tables[MDRows.Assembly.TABLE_ID];
			}
		}
		
		public AssemblyRef[] AssemblyRef {
			get {
				return (AssemblyRef[])reader.MetadataTable.Tables[MDRows.AssemblyRef.TABLE_ID];
			}
		}
		
		public ClassLayout[] ClassLayout {
			get {
				return (ClassLayout[])reader.MetadataTable.Tables[MDRows.ClassLayout.TABLE_ID];
			}
		}
		
		public Constant[] Constant {
			get {
				return (Constant[])reader.MetadataTable.Tables[MDRows.Constant.TABLE_ID];
			}
		}
		
		public CustomAttribute[] CustomAttribute {
			get {
				return (CustomAttribute[])reader.MetadataTable.Tables[MDRows.CustomAttribute.TABLE_ID];
			}
		}
		
		public DeclSecurity[] DeclSecurity {
			get {
				return (DeclSecurity[])reader.MetadataTable.Tables[MDRows.DeclSecurity.TABLE_ID];
			}
		}
		
		public Event[] Event {
			get {
				return (Event[])reader.MetadataTable.Tables[MDRows.Event.TABLE_ID];
			}
		}
		
		public EventMap[] EventMap {
			get {
				return (EventMap[])reader.MetadataTable.Tables[MDRows.EventMap.TABLE_ID];
			}
		}
		
		public EventPtr[] EventPtr {
			get {
				return (EventPtr[])reader.MetadataTable.Tables[MDRows.EventPtr.TABLE_ID];
			}
		}
		/*
		public ExportedType[] ExportedType {
			get {
				return (ExportedType[])reader.MetadataTable.Tables[MDRows.ExportedType.TABLE_ID];
			}
		}
		*/
		public Field[] Field {
			get {
				return (Field[])reader.MetadataTable.Tables[MDRows.Field.TABLE_ID];
			}
		}
		
		public FieldLayout[] FieldLayout {
			get {
				return (FieldLayout[])reader.MetadataTable.Tables[MDRows.FieldLayout.TABLE_ID];
			}
		}
		/*
		public FieldMarshal[] FieldMarshal {
			get {
				return (FieldMarshal[])reader.MetadataTable.Tables[MDRows.FieldMarshal.TABLE_ID];
			}
		}
		*/
		public FieldRVA[] FieldRVA {
			get {
				return (FieldRVA[])reader.MetadataTable.Tables[MDRows.FieldRVA.TABLE_ID];
			}
		}
		
		public MDRows.File[] File {
			get {
				return (MDRows.File[])reader.MetadataTable.Tables[MDRows.File.TABLE_ID];
			}
		}
		
		public ImplMap[] ImplMap {
			get {
				return (ImplMap[])reader.MetadataTable.Tables[MDRows.ImplMap.TABLE_ID];
			}
		}
		
		public InterfaceImpl[] InterfaceImpl {
			get {
				return (InterfaceImpl[])reader.MetadataTable.Tables[MDRows.InterfaceImpl.TABLE_ID];
			}
		}
		
		public ManifestResource[] ManifestResource {
			get {
				return (ManifestResource[])reader.MetadataTable.Tables[MDRows.ManifestResource.TABLE_ID];
			}
		}
		
		public MemberRef[] MemberRef {
			get {
				return (MemberRef[])reader.MetadataTable.Tables[MDRows.MemberRef.TABLE_ID];
			}
		}
		
		public Method[] Method {
			get {
				return (Method[])reader.MetadataTable.Tables[MDRows.Method.TABLE_ID];
			}
		}
		
		public MethodImpl[] MethodImpl {
			get {
				return (MethodImpl[])reader.MetadataTable.Tables[MDRows.MethodImpl.TABLE_ID];
			}
		}
		
		public MethodSemantics[] MethodSemantics {
			get {
				return (MethodSemantics[])reader.MetadataTable.Tables[MDRows.MethodSemantics.TABLE_ID];
			}
		}
		/*
		public MDRows.Module[] Module {
			get {
				return (MDRows.Module[])reader.MetadataTable.Tables[MDRows.MDRows.Module.TABLE_ID];
			}
		}
		
		public ModuleRef[] ModuleRefTable {
			get {
				return (ModuleRef[])reader.MetadataTable.Tables[MDRows.ModuleRef.TABLE_ID];
			}
		}
		*/
		public NestedClass[] NestedClass {
			get {
				return (NestedClass[])reader.MetadataTable.Tables[MDRows.NestedClass.TABLE_ID];
			}
		}
		
		public Param[] Param {
			get {
				return (Param[])reader.MetadataTable.Tables[MDRows.Param.TABLE_ID];
			}
		}
		
		public Property[] Property {
			get {
				return (Property[])reader.MetadataTable.Tables[MDRows.Property.TABLE_ID];
			}
		}
		
		public PropertyMap[] PropertyMap {
			get {
				return (PropertyMap[])reader.MetadataTable.Tables[MDRows.PropertyMap.TABLE_ID];
			}
		}
		
		public StandAloneSig[] StandAloneSig {
			get {
				return (StandAloneSig[])reader.MetadataTable.Tables[MDRows.StandAloneSig.TABLE_ID];
			}
		}
		
		public TypeDef[] TypeDef {
			get {
				return (TypeDef[])reader.MetadataTable.Tables[MDRows.TypeDef.TABLE_ID];
			}
		}
		
		public TypeRef[] TypeRef {
			get {
				return (TypeRef[])reader.MetadataTable.Tables[MDRows.TypeRef.TABLE_ID];
			}
		}
		
		public TypeSpec[] TypeSpec {
			get {
				return (TypeSpec[])reader.MetadataTable.Tables[MDRows.TypeSpec.TABLE_ID];
			}
		}

	
	}
}
