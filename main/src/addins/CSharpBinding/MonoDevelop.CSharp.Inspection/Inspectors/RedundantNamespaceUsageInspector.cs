// 
// RedundantNamespaceUsageInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.Inspection;

namespace MonoDevelop.CSharp.Inspection
{
	public class RedundantNamespaceUsageInspector : CSharpInspector
	{
		public override string Category {
			get {
				return DefaultInspectionCategories.Redundancies;
			}
		}

		public override string Title {
			get {
				return GettextCatalog.GetString ("Remove redundant namespace usages");
			}
		}

		public override string Description {
			get {
				return GettextCatalog.GetString ("Removes namespace usages.");
			}
		}

		protected override void Attach (ObservableAstVisitor<InspectionData, object> visitor)
		{
			visitor.MemberReferenceExpressionVisited += delegate (MemberReferenceExpression mr, InspectionData data) {
				var result = data.GetResolveResult (mr.Target);
				if (!(result is NamespaceResolveResult))
					return;
				var wholeResult = data.GetResolveResult (mr);
				if (!(wholeResult is TypeResolveResult))
					return;
				
				var state = data.GetResolverStateBefore (mr);
				var lookupName = state.LookupSimpleNameOrTypeName (mr.MemberName, new List<IType> (), SimpleNameLookupMode.Expression);
				
				if (lookupName != null && wholeResult.Type.Equals (lookupName.Type)) {
					AddResult (data,
						new DomRegion (mr.StartLocation, mr.MemberNameToken.StartLocation),
						GettextCatalog.GetString ("Remove redundant namespace usage"),
						delegate {
							int offset = data.Document.Editor.LocationToOffset (mr.StartLocation);
							int end = data.Document.Editor.LocationToOffset (mr.MemberNameToken.StartLocation);
							data.Document.Editor.Remove (offset, end - offset);
						}
					);
				}
			};
		}
	}
}
