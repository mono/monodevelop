// 
// Reference.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.Ide.FindInFiles
{
	[Flags]
	public enum ReferenceUsageType {
		Unknown = 0,
		Read    = 1,
		Write   = 2,
		ReadWrite = Read | Write
	}

	public class MemberReference : SearchResult
	{
		public override FileProvider FileProvider {
			get {
				return new MonoDevelop.Ide.FindInFiles.FileProvider (FileName);
			}
		}
		
		public override  string FileName {
			get {
				return Region.FileName;
			}
		}

		public ReferenceUsageType ReferenceUsageType { get; set; }
		public object EntityOrVariable { get; private set;}
		public DomRegion Region { get; private set;}
		
		public MemberReference (object entity, DomRegion region, int offset, int length) : base (offset, length)
		{
			if (entity == null)
				throw new System.ArgumentNullException ("entity");
			EntityOrVariable = entity;
			Region = region;
		}

		public string GetName ()
		{
			if (EntityOrVariable is IEntity) {
				return ((IEntity)EntityOrVariable).Name;
			} 
			if (EntityOrVariable is ITypeParameter) {
				return ((ITypeParameter)EntityOrVariable).Name;
			} 
			if (EntityOrVariable is INamespace) {
				return ((INamespace)EntityOrVariable).Name;
			} 
			return ((IVariable)EntityOrVariable).Name;
		}
	}
}

