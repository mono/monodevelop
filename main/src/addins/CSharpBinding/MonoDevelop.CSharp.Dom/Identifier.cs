// 
// Identifier.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Dom
{
	public class Identifier : DomNode
	{
		public static readonly new Identifier Null = new NullIdentifier ();
		class NullIdentifier : Identifier
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
		}
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public string Name {
			get;
			set;
		}
		
		DomLocation startLocation;
		public override DomLocation StartLocation {
			get {
				return startLocation;
			}
		}
		
		public override DomLocation EndLocation {
			get {
				return new DomLocation (StartLocation.Line, StartLocation.Column + (Name ?? "").Length);
			}
		}
		
		/*
		public ISegment Segment {
			get {
				return new Segment (Offset, Name != null ? Name.Length : 0);
			}
		}*/
		
		public Identifier ()
		{
		}
		public Identifier (string name, DomLocation location)
		{
			this.Name = name;
			this.startLocation = location;
		}
		
		public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIdentifier (this, data);
		}
	}
}
