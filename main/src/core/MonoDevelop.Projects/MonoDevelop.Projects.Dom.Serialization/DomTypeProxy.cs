//
// DomClassProxy.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MonoDevelop.Projects.Dom.Serialization
{
	internal class DomTypeProxy : DomType
	{
		SerializationCodeCompletionDatabase db;
		ClassEntry entry;
		bool isLoaded = false;
		IType wrappedType = null;
		IType WrappedType {
			get {
				if (!isLoaded) {
					isLoaded = true;
					try {
						wrappedType = db.ReadClass (entry);
						entry.AttachClass (wrappedType);
						if (wrappedType != null) {
							base.fieldCount       = wrappedType.FieldCount;
							base.methodCount      = wrappedType.MethodCount;
							base.constructorCount = wrappedType.ConstructorCount;
							base.indexerCount     = wrappedType.IndexerCount;
							base.propertyCount    = wrappedType.PropertyCount;
							base.eventCount       = wrappedType.EventCount;
							base.innerTypeCount   = wrappedType.InnerTypeCount;
						}
					} catch (Exception ex) {
						MonoDevelop.Core.LoggingService.LogError (ex.ToString ());
					}
				}
				return wrappedType;
			}
		}
		
		public DomTypeProxy (SerializationCodeCompletionDatabase db, ClassEntry entry)
		{
			this.db    = db;
			this.entry = entry;
			
			Debug.Assert (entry != null);
			base.Name      = entry.Name;
			base.Namespace = entry.NamespaceRef.FullName;
		}
		
		public override string ToString ()
		{
			return String.Format ("[DomTypeProxy: Entry={0}]", this.entry);
		}
		
		public override string Name {
			get {
				return entry.Name;
			}
		}
		
		public override string Namespace {
			get {
				return entry.NamespaceRef.FullName;
			}
		}
		
		public override string FullName {
			get {
				if (!string.IsNullOrEmpty (Namespace))
					return string.Concat (Namespace, ".", Name);
				else
					return Name;
			}
		}
		
		public override ClassType ClassType {
			get {
				return entry.ClassType;
			}
		}
		
		public override Modifiers Modifiers {
			get {
				return entry.Modifiers;
			}
		}
		
		public override IReturnType BaseType {
			get {
				if (HasContent (ContentFlags.HasBaseTypes))
					return WrappedType.BaseType;
				else
					return null;
			}
		}
		
		public override IEnumerable<IType> InnerTypes {
			get {
				if (HasContent (ContentFlags.HasInnerClasses))
					return WrappedType.InnerTypes;
				else
					return new IType [0];
			}
		}
		
		public override ReadOnlyCollection<IReturnType> ImplementedInterfaces {
			get {
				if (HasContent (ContentFlags.HasBaseTypes))
					return WrappedType.ImplementedInterfaces;
				else
					return new ReadOnlyCollection<IReturnType> (new IReturnType[0]);
			}
		}
		
		public override ReadOnlyCollection<TypeParameter> TypeParameters {
			get {
				if (HasContent (ContentFlags.HasGenericParams))
					return WrappedType.TypeParameters;
				else
					return new ReadOnlyCollection<TypeParameter> (new TypeParameter[0]);
			}
		}
		
		public override IEnumerable<IMember> Members {
			get {
				return WrappedType != null ? WrappedType.Members : new IMember [0];
			}
		}
		
		public override IEnumerable<IField> Fields {
			get {
				if (HasContent (ContentFlags.HasFields))
					return WrappedType.Fields;
				else
					return new IField [0];
			}
		}
		
		public override IEnumerable<IProperty> Properties {
			get {
				if (HasContent (ContentFlags.HasProperties))
					return WrappedType.Properties;
				else
					return new IProperty [0];
			}
		}
		
		public override IEnumerable<IMethod> Methods {
			get {
				if (HasContent (ContentFlags.HasMethods))
					return WrappedType.Methods;
				else
					return new IMethod [0];;
			}
		}
		
		public override IEnumerable<IEvent> Events {
			get {
				if (HasContent (ContentFlags.HasEvents))
					return WrappedType.Events;
				else
					return new IEvent [0];
			}
		}
		
		public override IEnumerable<IType> Parts { 
			get {
				if (HasContent (ContentFlags.HasParts))
					return WrappedType.Parts;
				else
					return base.Parts;
			}
		}
		
		public override bool HasParts {
			get {
				return HasContent (ContentFlags.HasParts);
			}
		}
		
		public override ICompilationUnit CompilationUnit {
			get {
				if (HasContent (ContentFlags.HasCompilationUnit))
					return WrappedType.CompilationUnit;
				else
					return null;
			}
		}
		
		public override DomRegion BodyRegion {
			get {
				if (HasContent (ContentFlags.HasBodyRegion))
					return WrappedType.BodyRegion;
				else
					return DomRegion.Empty;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		public override DomLocation Location {
			get {
				if (HasContent (ContentFlags.HasRegion))
					return WrappedType.Location;
				else
					return DomLocation.Empty;
			}
		}
		
		public override int PropertyCount {
			get {
				if (HasContent (ContentFlags.HasProperties))
					return WrappedType.PropertyCount;
				else
					return 0;
			}
		}
		public override int FieldCount {
			get {
				if (HasContent (ContentFlags.HasFields))
					return WrappedType.FieldCount;
				else
					return 0;
			}
		}
		public override int MethodCount {
			get {
				if (HasContent (ContentFlags.HasMethods))
					return WrappedType.MethodCount;
				else
					return 0;
			}
		}
		public override int ConstructorCount {
			get {
				if (HasContent (ContentFlags.HasConstructors))
					return WrappedType.ConstructorCount;
				else
					return 0;
			}
		}
		public override int IndexerCount {
			get {
				if (HasContent (ContentFlags.HasIndexers))
					return WrappedType.IndexerCount;
				else
					return 0;
			}
		}
		public override int EventCount {
			get {
				if (HasContent (ContentFlags.HasEvents))
					return WrappedType.EventCount;
				else
					return 0;
			}
		}
		public override int InnerTypeCount {
			get {
				if (HasContent (ContentFlags.HasInnerClasses))
					return WrappedType.InnerTypeCount;
				else
					return 0;
			}
		}
		
		public override string Documentation {
			get {
				if (HasContent (ContentFlags.HasDocumentation))
					return WrappedType.Documentation;
				else
					return null;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		public override IEnumerable<IAttribute> Attributes {
			get {
				if (HasContent (ContentFlags.HasAttributes))
					return WrappedType.Attributes;
				else
					return new IAttribute [0];
			}
		}

		public override bool IsObsolete {
			get {
				return HasContent (ContentFlags.IsObsolete);
			}
		}
		
		
		bool HasContent (ContentFlags cf)
		{
			return (entry.ContentFlags & cf) != 0 && WrappedType != null;
		}
	}
}
