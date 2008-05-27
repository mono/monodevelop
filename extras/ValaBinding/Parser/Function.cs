//
// Function.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;

using MonoDevelop.Projects;

namespace MonoDevelop.ValaBinding.Parser
{
	public class Function : LanguageItem
	{
		private string[] parameters;
		private string signature;
		private bool is_const = false;
		
		public Function (Tag tag, Project project, string ctags_output) : base (tag, project)
		{
			signature = tag.Signature;
			ParseSignature (tag.Signature);
			
			if (tag.Kind == TagKind.Prototype) {
				Access = tag.Access;
				if (GetNamespace (tag, ctags_output)) return;
				if (GetClass (tag, ctags_output)) return;
				if (GetStructure (tag, ctags_output)) return;
				if (GetUnion (tag, ctags_output)) return;
			} else {
				// If it is not a prototype tag, we attempt to get the prototype tag
				// we need the prototype tag because the implementation tag
				// marks the belonging namespace as a if it were a class
				// and it does not have the access field.
				Tag prototypeTag = TagDatabaseManager.Instance.FindTag (Name, TagKind.Prototype, ctags_output);
				
				if (prototypeTag == null) {
					// It does not have a prototype tag which means it is inline
					// and when it is inline it does have all the info we need
					
					if (GetNamespace (tag, ctags_output)) return;
					if (GetClass (tag, ctags_output)) return;
					if (GetStructure (tag, ctags_output)) return;
					if (GetUnion (tag, ctags_output)) return;
					
					return;
				}
				
				// we need to re-get the access
				Access = prototypeTag.Access;
				
				if (GetNamespace (prototypeTag, ctags_output)) return;
				if (GetClass (prototypeTag, ctags_output)) return;
				if (GetStructure (prototypeTag, ctags_output)) return;
				if (GetUnion (prototypeTag, ctags_output)) return;
			}
		}
		
		private void ParseSignature (string signature)
		{
			if (null == signature) return;
			
			string sig = signature;
			
			if (signature.EndsWith ("const")) {
				is_const = true;
				sig = signature.Substring (0, signature.Length - 6);
				sig = sig.Substring (1, sig.Length - 2);
			} else {
				sig = signature.Substring (1, signature.Length - 2);
			}
			
			parameters = sig.Split (',');
			
			for (int i = 0; i < parameters.Length; i++)
				parameters[i] = parameters[i].Trim ();
		}
		
		public string[] Parameters {
			get { return parameters; }
		}
		
		public string Signature {
			get { return signature; }
		}
		
		public bool IsConst {
			get { return is_const; }
		}
		
		public override bool Equals (object o)
		{
			Function other = o as Function;
			
			return (other != null &&
			    FullName == other.FullName &&
			    LanguageItem.Equals(Parent, other.Parent) &&
			    Project.Equals(other.Project) &&
				Signature == other.Signature);
		}
		
		public override int GetHashCode ()
		{
			return base.GetHashCode () + parameters.GetHashCode ();
		}
	}
}
