// 
// XibDocument.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace MonoDevelop.MacDev.InterfaceBuilder
{
	
	public class IBDocument : IBObject
	{
		public IBDocument ()
		{
		}
		
		public static IBDocument Deserialize (XDocument doc)
		{
			return Deserialize (doc,
				new Dictionary<string, Func<IBObject>> () {
					{ "NSMutableArray", () => new NSMutableArray () },
					{ "NSArray", () => new NSArray () },
					{ "NSMutableDictionary", () => new NSMutableDictionary () },
					{ "IBMutableOrderedSet", () => new IBMutableOrderedSet () },
					{ "IBCocoaTouchOutletConnection", () => new IBCocoaTouchOutletConnection () },
					{ "IBCocoaTouchEventConnection", () => new IBCocoaTouchEventConnection () },
					{ "IBActionConnection", () => new IBActionConnection () },
					{ "IBOutletConnection", () => new IBOutletConnection () },
					{ "IBConnectionRecord", () => new IBConnectionRecord () },
					{ "IBProxyObject", () => new IBProxyObject () },
					{ "IBObjectRecord", () => new IBObjectRecord () },
					{ "IBClassDescriptionSource", () => new IBClassDescriptionSource () },
					{ "IBPartialClassDescription", () => new IBPartialClassDescription () },
				}
			);
		}
		
		public static IBDocument Deserialize (XDocument doc, Dictionary<string, Func<IBObject>> constructors)
		{
			var resolver = new ReferencePool ();
			var ibDoc = new IBDocument ();
			var dataEl = doc.Root.Element ("data");
			ibDoc.DeserializeContents (dataEl.Elements (), constructors, resolver);
			resolver.ResolveAll ();
			if (resolver.UnresolvedReferences.Count > 0) {
				var sb = new StringBuilder ("Unresolved references in XIB document: ");
				foreach (var r in resolver.UnresolvedReferences)
					sb.Append (r.Id + " ");
				throw new InvalidOperationException (sb.ToString ());
			}
			return ibDoc;
		}
		
		Dictionary<string, object> properties = new Dictionary<string, object> ();
		
		public Dictionary<string, object> Properties {
			get { return properties; }
		}
		
		public NSMutableArray RootObjects { get; set; }
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			if (name == "IBDocument.RootObjects")
				RootObjects = (NSMutableArray) value;
			else if (name != null)
				Properties [name] = value;
			else
				throw new InvalidOperationException ("XIB data element contains child with no key");
		}
	}
	
	class ReferencePool : IReferenceResolver
	{
		List<IBReference> unresolvedReferences = new List<IBReference> ();
		Dictionary<int,object> identifiableObjects = new Dictionary<int,object> ();
		
		public List<IBReference> UnresolvedReferences {
			get { return unresolvedReferences; }
		}
	
		public Dictionary<int,object> IdentifiableObjects {
			get { return identifiableObjects; }
		}
		
		public void ResolveAll ()
		{
			object identifiable;
			var stillUnresolved = new List<IBReference> ();
			foreach (IBReference reference in unresolvedReferences) {
				if (identifiableObjects.TryGetValue (reference.Id, out identifiable))
					reference.Reference = identifiable;
				else
					stillUnresolved.Add (reference);
			}
			unresolvedReferences = stillUnresolved;
		}
		
		public void Add (IBObject resolveable)
		{
			identifiableObjects.Add (resolveable.Id.Value, resolveable);
		}
		
		public void Add (IBReference reference)
		{
			unresolvedReferences.Add (reference);
		}
		
		public void Add (int id, object primitive)
		{
			identifiableObjects.Add (id, primitive);
		}

	}
}
