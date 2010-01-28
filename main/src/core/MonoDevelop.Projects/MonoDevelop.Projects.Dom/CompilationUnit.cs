//
// CompilationUnit.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom
{
	public class CompilationUnit : AbstractNode, ICompilationUnit
	{
		protected FilePath fileName;
		public FilePath FileName {
			get {
				return fileName;
			}
		}
		
		List<IUsing>     usings         = new List<IUsing> ();
		List<IAttribute> attributes     = new List<IAttribute> ();
		List<IType>      types          = new List<IType> ();
		
		public CompilationUnit (string fileName)
		{
			this.fileName = fileName;
		}
		
		#region ICompilationUnit
		public ReadOnlyCollection<IUsing> Usings {
			get {
				return usings.AsReadOnly ();
			}
		}
		
		public IEnumerable<IAttribute> Attributes {
			get {
				return attributes;
			}
		}
		
		public ReadOnlyCollection<IType> Types {
			get {
				return types.AsReadOnly ();
			}
		}
		
		
		
		S INode.AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
		#endregion
		
		
		public IType GetType (string fullName, int genericParameterCount)
		{
			foreach (IType type in types) {
				if (type.FullName == fullName && (genericParameterCount < 0 || type.TypeParameters.Count == genericParameterCount))
					return type;
			}
			return null;
		}
		
		static IType GetTypeAt (IEnumerable<IType> types, int line, int column)
		{
			foreach (IType type in types) {
				if (type.Location.Line == line || type.BodyRegion.Contains (line, column)) {
					IType innerType = GetTypeAt (type.InnerTypes, line, column);
					return innerType ?? type;
				}
			}
			return null;
		}
		
		public IType GetTypeAt (DomLocation location)
		{
			return GetTypeAt (location.Line, location.Column);
		}
		
		public IType GetTypeAt (int line, int column)
		{
			return GetTypeAt (types, line, column);
		}
		
		public IMember GetMemberAt (DomLocation location)
		{
			return GetMemberAt (location.Line, location.Column);
		}
		public IMember GetMemberAt (int line, int column)
		{
			IType type = GetTypeAt (line, column);
			return type != null ? type.GetMemberAt (line, column) : null;
		}
		
		public void Add (IUsing newUsing)
		{
			usings.Add (newUsing);
		}
		
		public void Add (IAttribute newAttribute)
		{
			attributes.Add (newAttribute);
		}
		
		public void Add (IType newType)
		{
			newType.CompilationUnit = this;
			types.Add (newType);
		}
		
		public bool IsNamespaceUsedAt (string name, int line, int column)
		{
			return IsNamespaceUsedAt (name, new DomLocation (line, column));
		}
		
		public bool IsNamespaceUsedAt (string name, DomLocation location)
		{
			return usings.Where (u => ((!u.IsFromNamespace && u.Region.Start < location) || u.Region.Contains (location)) && u.Namespaces.Contains (name)).Any ();
		}
		
		public void GetNamespaceContents (List<IMember> list, string subNamespace, bool caseSensitive)
		{
			foreach (IType type in Types) {
				string fullName = type.FullName;
				if (fullName.StartsWith (subNamespace, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase)) {
					string tmp = subNamespace.Length > 0 ? fullName.Substring (subNamespace.Length + 1) : fullName;
					int idx = tmp.IndexOf('.');
					IMember newMember;
					if (idx > 0) {
						newMember = new Namespace (tmp.Substring (0, idx));
					} else {
						newMember = type;
					}
					if (!list.Contains (newMember))
						list.Add (newMember);
				}
			}
		}
		
		
		public IReturnType ShortenTypeName (IReturnType fullyQualfiedType, int line, int column)
		{
			return ShortenTypeName (fullyQualfiedType, new DomLocation (line, column));
		}
		
		public IReturnType ShortenTypeName (IReturnType fullyQualfiedType, DomLocation location)
		{
			if (fullyQualfiedType == null)
				throw new ArgumentNullException ("fullyQualfiedType");
			return new ShortenTypeNameVistior (this, location).Visit (fullyQualfiedType, null) as IReturnType;
		}
		
		public readonly static HashSet<string> BuiltInTypes = new HashSet<string> (new string[] {
			"System.Void", "System.Object","System.Boolean","System.Byte", "System.SByte",
			"System.Char", "System.Enum", "System.Int16", "System.Int32", "System.Int64", 
			"System.UInt16", "System.UInt32", "System.UInt64", "System.Single", "System.Double", "System.Decimal",
			"System.String"
		});
			
		class ShortenTypeNameVistior : CopyDomVisitor<object>
		{
			ICompilationUnit unit;
			DomLocation location;
			
			public ShortenTypeNameVistior (ICompilationUnit unit, DomLocation location)
			{
				this.unit = unit;
				this.location = location;
			}
			
			public override INode Visit (IReturnType type, object data)
			{
				IReturnType returnType = (IReturnType)base.Visit (type, data);
				if (BuiltInTypes.Contains (returnType.FullName))
					return returnType;
				
				string longest = "";
				string lastAlias = null;
				
				foreach (IUsing u in unit.Usings.Where (u => u.ValidRegion.Contains (location))) {
					foreach (string ns in u.Namespaces.Where (ns => returnType.Namespace == ns || returnType.Namespace.StartsWith (ns + "."))) {
						if (longest.Length < ns.Length)
							longest = ns;
					}
					foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
						if (alias.Value.ToInvariantString() == type.ToInvariantString ())
							lastAlias = alias.Key;
					}
					
				}
				if (lastAlias != null)
					return new DomReturnType (lastAlias);
				if (longest.Length > 0) 
					returnType.Namespace = returnType.Namespace == longest ? "" : returnType.Namespace.Substring (longest.Length + 1);
				return returnType;
			}
		}
		
	}
}
