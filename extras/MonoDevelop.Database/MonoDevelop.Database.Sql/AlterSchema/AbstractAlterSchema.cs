//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2008 Ben Motmans
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

namespace MonoDevelop.Database.Sql
{
	//TODO: AddUnique method ??
	public abstract class AbstractAlterSchema<T> : IAlterSchema where T : ISchema
	{
		protected List<IAlteration> alterations;
		
		protected T oldSchema;
		protected T newSchema;
		
		protected AbstractAlterSchema ()
		{
			alterations = new List<IAlteration> ();
		}
		
		protected AbstractAlterSchema (T oldSchema, T newSchema):this()
		{
			if (oldSchema == null)
				throw new ArgumentNullException ("oldSchema");
			if (newSchema == null)
				throw new ArgumentNullException ("newSchema");
			
			this.oldSchema = oldSchema;
			this.newSchema = newSchema;
			
			DetermineDifferences (oldSchema, newSchema);
		}
		
		public T OldSchema
		{
			get { return oldSchema; }
			protected internal set { oldSchema = value; }
		}
		
		public T NewSchema
		{
			get { return newSchema; }
			protected internal set { newSchema = value; }
		}
		
		public ICollection<IAlteration> Alterations
		{
			get { return alterations; }
		}
		
		public T GetAlteration<T> () where T : IAlteration
		{
			Type t = typeof (T);
			foreach (IAlteration alteration in alterations)
				if (t == alteration.GetType ())
					return (T)alteration;
			return default(T);
		}

		public bool HasAlteration<T> () where T : IAlteration
		{
			IAlteration alteration = GetAlteration<T> ();
			return alteration != null;
		}

		protected virtual void DetermineDifferences (T oldSchema, T newSchema)
		{
			if (oldSchema.Name != newSchema.Name)
				alterations.Add (new NameAlteration (oldSchema.Name, newSchema.Name));
			if (oldSchema.OwnerName != newSchema.OwnerName)
				alterations.Add (new OwnerNameAlteration (oldSchema.OwnerName, newSchema.OwnerName));
			if (oldSchema.Comment != newSchema.Comment)
				alterations.Add (new CommentAlteration (oldSchema.Comment, newSchema.Comment));
			if (oldSchema.Definition != newSchema.Definition)
				alterations.Add (new DefinitionAlteration (oldSchema.Name, newSchema.Name));
			if (oldSchema.SchemaName != newSchema.SchemaName)
				alterations.Add (new SchemaAlteration (oldSchema.SchemaName, newSchema.SchemaName));
		}
	}
}
