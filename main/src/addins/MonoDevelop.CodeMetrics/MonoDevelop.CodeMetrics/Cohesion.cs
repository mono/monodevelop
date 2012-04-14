// 
// Cohesion.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2010 Nikhil Sarda
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
using System.Text;
using System.IO;

using Gtk;

using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Mono.TextEditor;
using System.Linq;

namespace MonoDevelop.CodeMetrics
{
	public class Cohesion
	{
		public static void EvaluateCohesion(ClassProperties cls)
		{
			double totalaccess = 0;
			foreach (var field in cls.Fields)
				totalaccess += field.Value.InternalAccessCount;
			
			cls.LCOM = 1 - (double)(totalaccess/(cls.Class.Methods.Count()*cls.Class.Fields.Count()));
			cls.LCOM_HS = (cls.Class.Methods.Count() - totalaccess/cls.Class.Fields.Count())/(cls.Class.Methods.Count()-1);
			
		}
		/*
		 * Pairwise Field Irrelation
			Let:

		    M 	be the set of methods defined by the class
    		F 	be the set of fields defined by the class
    		Mf 	be the subset of M of methods that access field f, where f is a member of F

			Then, the Total Field Irrelation is the mean Jaccard Distance between Mf1 and Mf2, where f1 ≠ f2.
		*/
	}
}

