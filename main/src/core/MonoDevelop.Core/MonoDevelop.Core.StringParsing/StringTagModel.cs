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
	public class StringTagModel: IStringTagModel
	{
		Dictionary<string,StringTagDescription> tags = new Dictionary<string, StringTagDescription> (StringComparer.InvariantCultureIgnoreCase);
		List<object> objects = new List<object> ();
		bool initialized;
		
		public void Add (object obj)
		{
			objects.Add (obj);
			initialized = false;
		}
		
		public void Add (StringTagModel source)
		{
			objects.Add (source);
			initialized = false;
		}
		
		public void Remove (object obj)
		{
			objects.Remove (obj);
			initialized = false;
		}
		
		void Initialize ()
		{
			initialized = true;
			tags.Clear ();
			foreach (IStringTagProvider provider in StringParserService.GetProviders ()) {
				foreach (object obj in objects) {
					if (obj is StringTagModel)
						continue;
					foreach (StringTagDescription tag in provider.GetTags (obj.GetType ())) {
						if (!tags.ContainsKey (tag.Name)) {
							tag.SetSource (provider, obj);
							tags.Add (tag.Name, tag);
						}
					}
				}
				foreach (StringTagDescription tag in provider.GetTags (null)) {
					if (!tags.ContainsKey (tag.Name)) {
						tag.SetSource (provider, null);
						tags.Add (tag.Name, tag);
					}
				}
			}
			foreach (object obj in objects) {
				StringTagModel source = obj as StringTagModel;
				if (source == null)
					continue;
				foreach (var tag in source.Tags.Values) {
					if (!tags.ContainsKey (tag.Name)) {
						tags.Add (tag.Name, tag);
					}
				}
			}
		}
		
		Dictionary<string,StringTagDescription> Tags {
			get {
				if (!initialized)
					Initialize ();
				return tags;
			}
		}
		
		public object GetValue (string name)
		{
			StringTagDescription td;
			if (Tags.TryGetValue (name, out td))
				return td.GetValue ();
			else
				return null;
		}
	}
}

