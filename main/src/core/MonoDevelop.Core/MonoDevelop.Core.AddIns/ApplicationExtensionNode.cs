// ApplicationExtensionNode.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using Mono.Addins;

namespace MonoDevelop.Core.AddIns
{
	[NodeAttribute ("class", typeof(Type), true, Description="Name of the class")]
	[NodeAttribute ("description", typeof(string), false, Description="Description of the tool")]
	internal class ApplicationExtensionNode: ExtensionNode, IApplicationInfo
	{
		string typeName;
		string description;
		
		public string TypeName {
			get { return typeName; }
		}
		
		public string Description {
			get { return description; }
		}
		
		protected override void Read (NodeElement elem)
		{
			typeName = elem.GetAttribute ("class");
			if (typeName.Length == 0)
				typeName = elem.GetAttribute ("type");
			if (typeName.Length == 0)
				throw new InvalidOperationException ("Application type not provided");
			description = elem.GetAttribute ("description");
		}
		
		public object CreateInstance ()
		{
			return Activator.CreateInstance (base.Addin.GetType (typeName, true));
		}
	}
}
