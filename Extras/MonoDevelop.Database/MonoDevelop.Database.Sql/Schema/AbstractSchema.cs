//
// Authors:
//	Christian Hergert  <chris@mosaix.net>
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
// Copyright (c) 2007 Ben Motmans
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
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public abstract class AbstractSchema : ISchema
	{
		public event EventHandler Changed;
		
		protected string name;
		protected string ownerName;
		protected string comment;
		protected string definition;
		protected string schema;
		protected ISchemaProvider provider;
		
		protected AbstractSchema (ISchemaProvider schemaProvider)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.provider = schemaProvider;
		}
		
		protected AbstractSchema (AbstractSchema schema)
		{
			if (schema == null)
				throw new ArgumentNullException ("schema");
			
			this.provider = schema.provider;
			this.name = schema.name;
			this.ownerName = schema.ownerName;
			this.comment = schema.comment;
			this.definition = schema.definition;
			this.schema = schema.schema;
		}
		
		public virtual string Name {
			get {
				return name;
			}
			set {
				if (name != value) {
					name = value;
					OnChanged ();
				}
			}
		}
		
		public virtual string FullName {
			get {
				if (!String.IsNullOrEmpty (schema))
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
				if (schema != value) {
					schema = value;
					OnChanged();
				}
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
					schema = null;
			}
		}
		
		public virtual string Comment {
			get {
				return comment;
			}
			set {
				if (comment != value) {
					comment = value;
					OnChanged ();
				}
			}
		}
		
		public virtual string Definition {
			get {
				return definition;
			}
			set {
				if (definition != value) {
					definition = value;
					OnChanged ();
				}
			}
		}
		
		public ISchemaProvider SchemaProvider {
			get { return provider; }
		}
		
		public virtual string OwnerName {
			get {
				return ownerName;
			}
			set {
				if (ownerName != value) {
					ownerName = value;
					OnChanged ();
				}
			}
		}
		
		public virtual UserSchema Owner {
			get {
				throw new NotImplementedException();
			}
		}
		
		public virtual ICollection<PrivilegeSchema> Privileges {
			get {
				throw new NotImplementedException();
			}
		}
		
		public virtual void OnChanged()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public virtual void Refresh()
		{
		}
		
		public abstract object Clone ();
	}
}
