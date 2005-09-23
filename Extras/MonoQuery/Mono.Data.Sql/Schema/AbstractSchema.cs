//
// Schema/AbstractSchema.cs
//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
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
using System.Collections;

namespace Mono.Data.Sql
{
	public abstract class AbstractSchema : ISchema
	{
		public event EventHandler Changed;
		
		protected string name = String.Empty;
		protected string ownerName = String.Empty;
		protected string comment = String.Empty;
		protected string definition = String.Empty;
		protected string schema = String.Empty;
		protected DbProviderBase provider = null;
		
		protected Hashtable options;
		
		public virtual string Name {
			get {
				return name;
			}
			set {
				name = value;
				OnChanged();
			}
		}
		
		public virtual string FullName {
			get {
				if (schema.Length > 0)
					return schema + "." + name;
				else
					return name;
			}
		}

		public virtual string SchemaName {
			get {
				return schema;
			}
			set {
				schema = value;
				OnChanged();
			}
		}
		
		public virtual SchemaSchema Schema {
			get {
				throw new NotImplementedException();
			}
			set {
				if (value != null)
					schema = value.Name;
				else
					schema = String.Empty;
			}
		}
		
		public virtual string Comment {
			get {
				return comment;
			}
			set {
				comment = value;
				OnChanged();
			}
		}
		
		public virtual string Definition {
			get {
				return definition;
			}
			set {
				definition = value;
				OnChanged();
			}
		}
		
		public DbProviderBase Provider {
			get {
				return provider;
			}
			set {
				provider = value;
				OnChanged();
			}
		}
		
		public virtual string OwnerName {
			get {
				return ownerName;
			}
			set {
				ownerName = value;
				OnChanged();
			}
		}
		
		public virtual UserSchema Owner {
			get {
				throw new NotImplementedException();
			}
		}
		
		public virtual PrivilegeSchema [] Privileges {
			get {
				throw new NotImplementedException();
			}
		}
		
		public virtual Hashtable Options {
			get {
				if (options == null)
					options = new Hashtable ();
				
				return options;
			}
		}
		
		public virtual void OnChanged()
		{
			if (Changed != null) {
				Changed(this, null);
			}
		}
		
		public virtual void Refresh()
		{
		}
	}
}
