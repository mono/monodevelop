// 
// T4ParsedDocument.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Mono.TextTemplating;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.Editor;
using System.Linq;

namespace MonoDevelop.TextTemplating.Parser
{
	
	
	public class T4ParsedDocument : DefaultParsedDocument
	{
		IList<MonoDevelop.Ide.TypeSystem.Error> errors;

		public T4ParsedDocument (string fileName, List<ISegment> segments, IList<MonoDevelop.Ide.TypeSystem.Error> errors) : base(fileName)
		{
			this.errors = errors;
			TemplateSegments = segments;
		}

		public override System.Threading.Tasks.Task<IReadOnlyList<MonoDevelop.Ide.TypeSystem.Error>> GetErrorsAsync (System.Threading.CancellationToken cancellationToken)
		{
			return System.Threading.Tasks.Task.FromResult((IReadOnlyList<MonoDevelop.Ide.TypeSystem.Error>)errors);
		}

		public List<ISegment> TemplateSegments { get; private set; }
		
		public IEnumerable<Directive> TemplateDirectives {
			get {
				foreach (ISegment seg in TemplateSegments) {
					Directive dir = seg as Directive;
					if (dir != null)
						yield return dir;
				}
			}
		}
		
		public IEnumerable<TemplateSegment> TemplateContent {
			get {
				foreach (ISegment seg in TemplateSegments) {
					TemplateSegment ts = seg as TemplateSegment;
					if (ts != null)
						yield return ts;
				}
			}
		}

		public override System.Threading.Tasks.Task<IReadOnlyList<FoldingRegion>> GetFoldingsAsync (System.Threading.CancellationToken cancellationToken)
		{
			return System.Threading.Tasks.Task.FromResult((IReadOnlyList<FoldingRegion>)Foldings.ToList ());
		}
		
		public IEnumerable<FoldingRegion> Foldings {
			get {
				foreach (ISegment seg in TemplateSegments) {
					if (seg.EndLocation.Line - seg.TagStartLocation.Line < 1)
						continue;
					
					string name;
					if (seg is TemplateSegment ts) {
						if (ts.Type == SegmentType.Content) {
							continue;
						} else if (ts.Type == SegmentType.Expression) {
							name = "<#=...#>";
						} else if (ts.Type == SegmentType.Helper) {
							name = "<#+...#>";
						} else {
							name = "<#...#>";
						}
					} else {
						var dir = (Directive)seg;
						name = "<#@" + dir.Name + "...#>";
					}

					var region = new DocumentRegion (seg.TagStartLocation.Line, seg.TagStartLocation.Column,
				                                      seg.EndLocation.Line, seg.EndLocation.Column);
					
					yield return new FoldingRegion (name, region, false);
				}
			}
		}
	}
}
