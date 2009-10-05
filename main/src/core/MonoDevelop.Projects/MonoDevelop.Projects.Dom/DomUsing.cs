//
// DomUsing.cs
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
using MonoDevelop.Core.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MonoDevelop.Projects.Dom
{
	public class DomUsing : IUsing
	{
		static readonly Dictionary<string, IReturnType> emptyAliases = new Dictionary<string,IReturnType> ();
		static readonly List<string> emptyNamespaces = new List<string> ();
		
		protected List<string>                    namespaces = emptyNamespaces;
		protected Dictionary<string, IReturnType> aliases    = emptyAliases;
		
		public DomRegion Region {
			get;
			set;
		}
		
		public DomRegion ValidRegion {
			get;
			set;
		}

		public ReadOnlyCollection<string> Namespaces {
			get {
				return namespaces.AsReadOnly ();
			}
		}

		public ReadOnlyDictionary<string, IReturnType> Aliases {
			get {
				return new ReadOnlyDictionary<string, IReturnType> (aliases);
			}
		}

		public bool IsFromNamespace {
			get;
			set;
		}
		
		public DomUsing ()
		{
			IsFromNamespace = false;
			ValidRegion = DomRegion.Empty;
		}
		
		public DomUsing (DomRegion region, string nspace) : this ()
		{
			this.Region = region;
			Add (nspace);
		}
		
		public virtual void Dispose ()
		{
			if (namespaces != null) {
				namespaces.Clear ();
				namespaces = null;
			}
			if (aliases != null) {
				foreach (IReturnType t in aliases.Values) {
					t.Dispose ();
				}
				aliases.Clear ();
				aliases = null;
			}
		}
		
		public void Add (string nspace)
		{
			if (namespaces == null || namespaces == emptyNamespaces)
				namespaces = new List<string> ();
			namespaces.Add (nspace);
		}
		
		public void Add (string nspace, IReturnType alias)
		{
			if (aliases == null || aliases == emptyAliases)
				aliases = new Dictionary<string, IReturnType> ();
			aliases[nspace] = alias;
		}

		public S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
		
		public override string ToString ()
		{
			StringBuilder aliasString = new StringBuilder ();
			foreach (var alias in this.aliases) {
				if (aliasString.Length > 0)
					aliasString.Append (", ");
				aliasString.Append (alias.Key);
				aliasString.Append ("/");
				aliasString.Append (alias.Value.ToInvariantString ());
			}
			return string.Format ("[DomUsing: Region={0}, Namespaces=({1}), Aliases=({3}), IsFromNamespace={2}]", Region, string.Join (",", namespaces.ToArray ()), IsFromNamespace, aliasString);
		}
		
	}
}
