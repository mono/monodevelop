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

namespace MonoDevelop.Projects.Dom
{
	/*
	internal class DomTypeProxy : DomType
	{
		CodeCompletionDatabase db;
		ClassEntry entry;
		
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
						LoggingService.LogError (ex.ToString ());
					}
				}
				return wrappedType;
			}
		}
		
		public DomTypeProxy (CodeCompletionDatabase db, ClassEntry entry)
		{
			this.db    = db;
			this.entry = entry;
			
			Debug.Assert (entry != null);
			base.Name      = entry.Name;
			base.Namespace = entry.NamespaceRef.FullName;
		}
		
		bool HasFlags (ContentFlags cf)
		{
			return (entry.ContentFlags & cf) != 0 && WrappedType != null;
		}
		
		public override ClassType ClassType {
			get {
				return WrappedType.ClassType;
			}
		}
		
		public override IReturnType BaseType {
			get {
				return WrappedType.BaseType;
			}
		}
		
		public override IEnumerable<IReturnType> ImplementedInterfaces {
			get {
				return WrappedType.ImplementedInterfaces;
			}
		}
		
		public override ReadOnlyCollection<TypeParameter> TypeParameters {
			get {
				return WrappedType.TypeParameters;
			}
		}
		
		public override IEnumerable<IMember> Members {
			get {
				return WrappedType.Members;
			}
		}
		
		public override IEnumerable<IType> Parts { 
			get {
				return WrappedType.Parts;
			}
		}
	}*/
}
