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
			return String.Format ("[DomTypeProxy: Entry={0}, WrappedType={1}]", this.entry, this.WrappedType);
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
				return null;
			}
		}
		
		public override IEnumerable<IType> InnerTypes {
			get {
				if (HasContent (ContentFlags.HasInnerClasses))
					return WrappedType.InnerTypes;
				return new IType [0];
			}
		}
		
		public override ReadOnlyCollection<IReturnType> ImplementedInterfaces {
			get {
				if (HasContent (ContentFlags.HasBaseTypes))
					return WrappedType.ImplementedInterfaces;
				return new ReadOnlyCollection<IReturnType> (new IReturnType[0]);
			}
		}
		
		public override IEnumerable<IReturnType> BaseTypes {
			get {
				if (HasContent (ContentFlags.HasBaseTypes))
					return WrappedType.BaseTypes;
				return new ReadOnlyCollection<IReturnType> (new IReturnType[0]);
			}
		}
		
		
		public override ReadOnlyCollection<TypeParameter> TypeParameters {
			get {
				if (HasContent (ContentFlags.HasGenericParams))
					return WrappedType.TypeParameters;
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
				return new IField [0];
			}
		}
		
		public override IEnumerable<IProperty> Properties {
			get {
				if (HasContent (ContentFlags.HasProperties))
					return WrappedType.Properties;
				return new IProperty [0];
			}
		}
		
		public override IEnumerable<IMethod> Methods {
			get {
				if (HasContent (ContentFlags.HasMethods) | HasContent (ContentFlags.HasConstructors))
					return WrappedType.Methods;
				return new IMethod [0];
			}
		}
		
		public override IEnumerable<IEvent> Events {
			get {
				if (HasContent (ContentFlags.HasEvents))
					return WrappedType.Events;
				return new IEvent [0];
			}
		}
		
		public override IEnumerable<IType> Parts { 
			get {
				if (HasContent (ContentFlags.HasParts))
					return WrappedType.Parts;
				return base.Parts;
			}
		}
		
		public override bool HasParts {
			get {
				// Don't use HasContent() since that method loads the wrapped type
				return (entry.ContentFlags & ContentFlags.HasParts) != 0;
			}
		}
		
		public override ICompilationUnit CompilationUnit {
			get {
				if (HasContent (ContentFlags.HasCompilationUnit))
					return WrappedType.CompilationUnit;
				return null;
			}
		}
		
		public override DomRegion BodyRegion {
			get {
				if (HasContent (ContentFlags.HasBodyRegion))
					return WrappedType.BodyRegion;
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
				return DomLocation.Empty;
			}
		}
		
		public override int PropertyCount {
			get {
				if (HasContent (ContentFlags.HasProperties))
					return WrappedType.PropertyCount;
				return 0;
			}
		}
		public override int FieldCount {
			get {
				if (HasContent (ContentFlags.HasFields))
					return WrappedType.FieldCount;
				return 0;
			}
		}
		public override int MethodCount {
			get {
				if (HasContent (ContentFlags.HasMethods))
					return WrappedType.MethodCount;
				return 0;
			}
		}
		public override int ConstructorCount {
			get {
				if (HasContent (ContentFlags.HasConstructors))
					return WrappedType.ConstructorCount;
				return 0;
			}
		}
		public override int IndexerCount {
			get {
				if (HasContent (ContentFlags.HasIndexers))
					return WrappedType.IndexerCount;
				return 0;
			}
		}
		public override int EventCount {
			get {
				if (HasContent (ContentFlags.HasEvents))
					return WrappedType.EventCount;
				return 0;
			}
		}
		public override int InnerTypeCount {
			get {
				if (HasContent (ContentFlags.HasInnerClasses))
					return WrappedType.InnerTypeCount;
				return 0;
			}
		}
		
		public override string Documentation {
			get {
				if (HasContent (ContentFlags.HasDocumentation))
					return WrappedType.Documentation;
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
				return new IAttribute [0];
			}
		}

		public override bool IsObsolete {
			get {
				// Don't use HasContent() since that method loads the wrapped type
				return (entry.ContentFlags & ContentFlags.IsObsolete) != 0;
			}
		}
		
		bool HasContent (ContentFlags cf)
		{
			return (entry.ContentFlags & cf) == cf && WrappedType != null;
		}
	}
}
