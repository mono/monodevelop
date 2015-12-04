﻿//
// TargetEvaluationContext.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.Projects
{
	public class TargetEvaluationContext: ProjectOperationContext
	{
		public TargetEvaluationContext ()
		{
			PropertiesToEvaluate = new HashSet<string> ();
			ItemsToEvaluate = new HashSet<string> ();
		}

		public TargetEvaluationContext (OperationContext other): this ()
		{
			if (other != null)
				CopyFrom (other);
		}

		public HashSet<string> PropertiesToEvaluate { get; private set; }

		public HashSet<string> ItemsToEvaluate { get; private set; }

		public override void CopyFrom (OperationContext other)
		{
			base.CopyFrom (other);
			var o = other as TargetEvaluationContext;
			if (o != null) {
				PropertiesToEvaluate = new HashSet<string> (o.PropertiesToEvaluate);
				o.ItemsToEvaluate = new HashSet<string> (o.ItemsToEvaluate);
			}
		}
	}
}

