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
			return String.Format ("[DomTypeProxy: WrappedType={0}]", this.WrappedType);
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
				return WrappedType != null ? WrappedType.BaseType : null;
			}
		}
		
		public override ReadOnlyCollection<IReturnType> ImplementedInterfaces {
			get {
				return WrappedType != null ? WrappedType.ImplementedInterfaces : null;
			}
		}
		
		public override ReadOnlyCollection<TypeParameter> TypeParameters {
			get {
				return WrappedType != null ? WrappedType.TypeParameters : null;
			}
		}
		
		public override IEnumerable<IMember> Members {
			get {
				return WrappedType != null ? WrappedType.Members : null;
			}
		}
		
		public override IEnumerable<IType> Parts { 
			get {
				return WrappedType != null ? WrappedType.Parts : null;
			}
		}
		
		public override bool HasParts {
			get {
				return WrappedType != null ? WrappedType.HasParts : false;
			}
		}
		
		public override ICompilationUnit CompilationUnit {
			get {
				return WrappedType != null ? WrappedType.CompilationUnit : null;
			}
		}
		
		public override DomLocation Location {
			get {
				return WrappedType != null ? WrappedType.Location : DomLocation.Empty;
			}
		}
	}
}
