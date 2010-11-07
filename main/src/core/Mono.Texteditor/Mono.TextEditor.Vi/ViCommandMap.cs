// 
// ViCommandMap.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using Mono.TextEditor;

namespace Mono.TextEditor.Vi
{
	class ViCommandMap : IEnumerable<KeyValuePair<ViKey,Action<ViEditor>>>
	{
		Dictionary<ViKey,BuilderAction> builders = new Dictionary<ViKey, BuilderAction> ();
		Dictionary<ViKey,Action<ViEditor>> actions  = new Dictionary<ViKey,Action<ViEditor>> ();
		
		public bool Builder (ViBuilderContext ctx)
		{
			Action<ViEditor> a;
			if (actions.TryGetValue (ctx.LastKey, out a)) {
				ctx.RunAction (a);
				return true;
			} else {
				BuilderAction b;
				if (builders.TryGetValue (ctx.LastKey, out b)) {
					ctx.Builder = b.Builder;
					if (b.RunInstantly)
						ctx.Builder (ctx);
					return true;
				}
			}
			return false;
		}
		
		public void Add (ViKey key, ViCommandMap map)
		{
			Add (key, map.Builder);
		}
		
		public void Add (ViKey key, Action<ViEditor> action)
		{
			this.actions[key] = action;
		}
		
		public void Add (ViKey key, Action<TextEditorData> action)
		{
			this.actions[key] = (ViEditor ed) => action (ed.Data);
		}
		
		public void Add (ViKey key, ViBuilder builder)
		{
			Add (key, builder, false);
		}
		
		public void Add (ViKey key, ViBuilder builder, bool runInstantly)
		{
			this.builders[key] = new BuilderAction (builder, runInstantly);
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return builders.GetEnumerator ();
		}
		
		public IEnumerator<KeyValuePair<ViKey,Action<ViEditor>>> GetEnumerator ()
		{
			return actions.GetEnumerator ();
		}
		
		struct BuilderAction
		{
			public BuilderAction (ViBuilder builder, bool runInstantly)
			{
				this.Builder = builder;
				this.RunInstantly = runInstantly;
			}
			
			public readonly ViBuilder Builder;
			public readonly bool RunInstantly;
		}
	}
}
