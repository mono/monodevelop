// 
// CSharpInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.CSharp;
using GLib;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Inspection;
using MonoDevelop.Projects.Dom;
using MonoDevelop.AnalysisCore.Fixes;

namespace MonoDevelop.CSharp.Inspection
{
	public abstract class CSharpInspector
	{
		protected InspectorAddinNode node;
		
		protected void AddResult (InspectionData data, DomRegion region, string menuText, Action fix)
		{
			var severity = node.GetSeverity ();
			if (severity == MonoDevelop.SourceEditor.QuickTaskSeverity.None)
				return;
			data.Add (
				new GenericResults (
					region,
					node.Title,
					severity, 
					ResultCertainty.High, 
					ResultImportance.Low,
					new GenericFix (menuText, fix)
				)
			);
		}
		
		public void Attach (InspectorAddinNode node, ObservableAstVisitor<InspectionData, object> visitior)
		{
			if (visitior == null)
				throw new ArgumentNullException ("visitior");
			this.node = node;
			Attach (visitior);
		}
		
		protected abstract void Attach (ObservableAstVisitor<InspectionData, object> visitior);
	}
}
