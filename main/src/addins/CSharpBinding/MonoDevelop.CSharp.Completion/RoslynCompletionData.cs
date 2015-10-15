//
// RoslynCompletionData.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Microsoft.CodeAnalysis;
using GLib;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor.Extension;
using Xwt;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynCompletionData : ISymbolCompletionData
	{
		List<CompletionData> overloads;
		
		public override bool HasOverloads {
			get {
				return overloads != null;
			}
		}


		public override void AddOverload (CompletionData data)
		{
			if (overloads == null)
				overloads = new List<CompletionData> ();
			overloads.Add ((CompletionData)data);
			sorted = null;
		}

		List<CompletionData> sorted;

		public override IReadOnlyList<CompletionData> OverloadedData {
			get {
				if (overloads == null)
					return new CompletionData[] { this };

				if (sorted == null) {
					sorted = new List<CompletionData> (overloads);
					sorted.Add (this);
					// sorted.Sort (new OverloadSorter ());
				}
				return sorted;
			}
		}


		public RoslynCompletionData (ICompletionDataKeyHandler keyHandler)
		{
			this.KeyHandler = keyHandler;
		}

		public RoslynCompletionData (ICompletionDataKeyHandler keyHandler, string text) : base (text)
		{
			this.KeyHandler = keyHandler;
		}

		public RoslynCompletionData (ICompletionDataKeyHandler keyHandler, string text, IconId icon) : base (text, icon)
		{
			this.KeyHandler = keyHandler;
		}

		public RoslynCompletionData (ICompletionDataKeyHandler keyHandler, string text, IconId icon, string description) : base (text, icon, description)
		{
			this.KeyHandler = keyHandler;
		}
		
		public RoslynCompletionData (ICompletionDataKeyHandler keyHandler, string displayText, IconId icon, string description, string completionText) : base (displayText, icon, description, completionText)
		{
			this.KeyHandler = keyHandler;
		}
		
//		class OverloadSorter : IComparer<ICompletionData>
//		{
//			public OverloadSorter ()
//			{
//			}
//
//			public int Compare (ICompletionData x, ICompletionData y)
//			{
//				var mx = ((RoslynCompletionData)x).Entity as IMember;
//				var my = ((RoslynCompletionData)y).Entity as IMember;
//				int result;
//				
//				if (mx is ITypeDefinition && my is ITypeDefinition) {
//					result = ((((ITypeDefinition)mx).TypeParameters.Count).CompareTo (((ITypeDefinition)my).TypeParameters.Count));
//					if (result != 0)
//						return result;
//				}
//				
//				if (mx is IMethod && my is IMethod) {
//					return MethodParameterDataProvider.MethodComparer ((IMethod)mx, (IMethod)my);
//				}
//				string sx = mx.ReflectionName;// ambience.GetString (mx, flags);
//				string sy = my.ReflectionName;// ambience.GetString (my, flags);
//				result = sx.Length.CompareTo (sy.Length);
//				return result == 0 ? string.Compare (sx, sy) : result;
//			}
//		}

	}

}
