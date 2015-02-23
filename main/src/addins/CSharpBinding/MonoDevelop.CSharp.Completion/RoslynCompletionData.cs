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
	class RoslynCompletionData : CompletionData, ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData
	{
		List<CompletionData> overloads;
		
		public override bool HasOverloads {
			get {
				return overloads != null;
			}
		}
		
		void ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.AddOverload (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData data)
		{
			if (overloads == null)
				overloads = new List<CompletionData> ();
			overloads.Add ((CompletionData)data);
			sorted = null;
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionCategory ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.CompletionCategory { 
			get {
				return (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionCategory)base.CompletionCategory;
			} 
			set {
				base.CompletionCategory = (CompletionCategory)value;
			} 
		}

		ICSharpCode.NRefactory6.CSharp.Completion.DisplayFlags ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.DisplayFlags { 
			get {
				return (ICSharpCode.NRefactory6.CSharp.Completion.DisplayFlags)base.DisplayFlags;
			}
			set {
				base.DisplayFlags = (DisplayFlags)value;
			}
		}

		List<CompletionData> sorted;

		IEnumerable<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.OverloadedData {
			get {
				return (IEnumerable<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData>)OverloadedData;
			}
		}

		public override IEnumerable<CompletionData> OverloadedData {
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

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler;

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.KeyHandler {
			get {
				return keyHandler;
			}
		}

		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler)
		{
			this.keyHandler = keyHandler;
		}

		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, string text) : base (text)
		{
			this.keyHandler = keyHandler;
		}

		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, string text, IconId icon) : base (text, icon)
		{
			this.keyHandler = keyHandler;
		}

		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, string text, IconId icon, string description) : base (text, icon, description)
		{
			this.keyHandler = keyHandler;
		}
		
		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, string displayText, IconId icon, string description, string completionText) : base (displayText, icon, description, completionText)
		{
			this.keyHandler = keyHandler;
		}
		
//		class OverloadSorter : IComparer<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData>
//		{
//			public OverloadSorter ()
//			{
//			}
//
//			public int Compare (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData x, ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData y)
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
