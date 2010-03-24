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
	public class StringTagModelDescription
	{
		List<List<StringTagDescription>> tags = new List<List<StringTagDescription>> ();
		HashSet<string> tagNames = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);
		List<Type> types = new List<Type> ();
		List<StringTagModelDescription> models = new List<StringTagModelDescription> ();
		bool initialized;
		
		public void Add (Type type)
		{
			types.Add (type);
			initialized = false;
		}
		
		public void Add (StringTagModelDescription model)
		{
			models.Add (model);
			initialized = false;
		}
		
		void Initialize ()
		{
			initialized = true;
			tags.Clear ();
			foreach (Type type in types) {
				var localTags = new List<StringTagDescription> ();
				foreach (IStringTagProvider provider in StringParserService.GetProviders ()) {
					foreach (StringTagDescription tag in provider.GetTags (type)) {
						if (tagNames.Add (tag.Name))
							localTags.Add (tag);
					}
					foreach (StringTagDescription tag in provider.GetTags (null)) {
						if (tagNames.Add (tag.Name))
							localTags.Add (tag);
					}
				}
				tags.Add (localTags);
			}
			
			foreach (StringTagModelDescription model in models) {
				foreach (var modelTags in model.GetTagsGrouped ()) {
					var localTags = new List<StringTagDescription> ();
					foreach (var tag in modelTags) {
						if (tagNames.Add (tag.Name))
							localTags.Add (tag);
					}
					tags.Add (localTags);
				}
			}
		}
		
		public IEnumerable<StringTagDescription> GetTags ()
		{
			if (!initialized)
				Initialize ();
			foreach (var list in tags) {
				foreach (var tag in list)
					yield return tag;
			}
		}
		
		public IEnumerable<StringTagDescription[]> GetTagsGrouped ()
		{
			if (!initialized)
				Initialize ();
			foreach (var list in tags)
				yield return list.ToArray ();
		}
	}
}
