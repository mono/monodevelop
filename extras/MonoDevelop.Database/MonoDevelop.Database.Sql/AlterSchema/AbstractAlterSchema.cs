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
	public abstract class AbstractAlterSchema : IAlterSchema
	{
		protected List<IAlteration> alterations;
		
		protected AbstractAlterSchema ()
		{
			alterations = new List<IAlteration> ();
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
		
		public virtual void Rename (string name)
		{
			NameAlteration alteration = GetAlteration<NameAlteration> ();
			if (alteration != null) {
				alteration.Name = name;
			} else {
				alteration = new NameAlteration (name);
				alterations.Add (alteration);
			}
		}
	}
}
