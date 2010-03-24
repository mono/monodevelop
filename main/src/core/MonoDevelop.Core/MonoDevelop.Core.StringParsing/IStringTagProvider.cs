// 
// StringTagModel.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Core.StringParsing
{
	[Mono.Addins.TypeExtensionPoint(Name="String providers")]
	public interface IStringTagProvider
	{
		/// <summary>
		/// Returns a list of tags that this provider can extract from objects
		/// of the provided type. If the provided type can be null, in which
		/// case it must return global tags (that is, tags which are not attached
		/// to a particular object).
		/// </summary>
		IEnumerable<StringTagDescription> GetTags (Type type);
		
		/// <summary>
		/// Returns the value of a tag. The instance is an object of a type supported
		/// by the provider, that is, the GetTags method returned tags for the object type.
		/// </summary>
		object GetTagValue (object instance, string tag);
	}
}
