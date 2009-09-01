// PythonResolver.cs
// 
// Copyright (c) 2009 Christian Hergert <chris@dronelabs.com>
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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

using PyBinding;
using PyBinding.Parser.Dom;

namespace PyBinding.Parser
{
	public class PythonResolver: IResolver
	{
		ProjectDom m_dom;
		string m_filename;
		
		public PythonResolver (ProjectDom dom, string filename)
		{
			m_filename = filename;
			m_dom = dom;
		}
		
		ResolveResult IResolver.Resolve (ExpressionResult expressionResult, DomLocation resolvePosition)
		{
			var expr = expressionResult as PythonExpressionResult;
			if (expr == null)
				return null;
			
			var doc = ProjectDomService.GetParsedDocument (m_dom, m_filename) as PythonParsedDocument;
			if (doc == null)
				return null;
			
			var unit = doc.CompilationUnit as PythonCompilationUnit;
			if (unit == null)
				return null;
			
			
			if (expr.Type == "def")
			{
				var type = unit.GetTypeAt (resolvePosition);
				if (type != null)
				{
					// resolving a method
					foreach (var func in type.Methods)
					{
						if (func.Name == expr.Word)
						{
							var l = new List<IMember> ();
							l.Add (func);
							return new MethodResolveResult (l);
						}
					}
				}
			}
			
			else if (expr.Type == "class")
			{
				var type = unit.GetTypeAt (resolvePosition);
				if (type != null && type.Name == expr.Word)
					return new MemberResolveResult (type);
			}
			
			else if (expr.Word == "self")
			{
				var type = unit.GetTypeAt (resolvePosition);
				if (type != null)
					return new MemberResolveResult (type);
			}
			
			else if (expr.Type == "self")
			{
				// looking for a member of self
				var type = unit.GetTypeAt (resolvePosition);
				if (type != null)
				{
					foreach (var attr in type.Fields)
					{
						if (attr.Name == expr.Word)
							return new MemberResolveResult (attr);
					}
					
					foreach (var method in type.Methods)
					{
						if (method.Name == expr.Word)
							return CreateMethodResult (method);
					}
				}
			}
			
			return null;
		}
		
		MethodResolveResult CreateMethodResult (IMethod method)
		{
			var l = new List<IMember> ();
			l.Add (method);
			return new MethodResolveResult (l);
		}
		
//		IBaseMember NearestTo (IEnumerable<IBaseMember> items, DomLocation location)
//		{
//			int lineOffset = -1, columnOffset = -1;
//			IBaseMember nearest = null;
//			
//			foreach (var member in items)
//			{
//				if (location < member.Location)
//					continue;
//				
//				DomRegion region = DomRegion.Empty;
//				
//				if (member is DomType)
//					region = (member as DomType).BodyRegion;
//				else if (member is DomMethod)
//					region = (member as DomMethod).BodyRegion;
//				
//				if (!region.IsEmpty && region.End < location)
//					continue;
//				
//				int curLineOffset = Math.Abs (member.Location.Line - location.Line);
//				int curColumnOffset = Math.Abs (member.Location.Column - location.Column);
//				
//				if (lineOffset == -1 || curLineOffset < lineOffset)
//				{
//					lineOffset = curLineOffset;
//					columnOffset = curColumnOffset;
//					nearest = member;
//				}
//				else if (columnOffset == -1 || (lineOffset == curLineOffset && curColumnOffset < columnOffset))
//				{
//					columnOffset = curColumnOffset;
//					nearest = member;
//				}
//			}
//			
//			return nearest;
//		}
	}
}
