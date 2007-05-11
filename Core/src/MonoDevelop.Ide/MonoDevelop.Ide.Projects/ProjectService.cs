//
// ProjectService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Projects
{
	public static class ProjectService
	{
		static Solution solution;
		static IProject activeProject;
		
		public static Solution Solution {
			get {
				return solution;
			}
		}
		
		public static IProject ActiveProject {
			get {
				return activeProject;
			}
			set {
				if (activeProject != value) {
					activeProject = value;
				}
			}
		}
		
		public static void OpenSolution (string fileName)
		{
			solution = Solution.Load (fileName);
			Console.WriteLine ("loaded : " + solution);
			ActiveProject = null;
			OnSolutionOpened (new SolutionEventArgs (solution));
		}
		
		public static void CloseSolution ()
		{
			if (Solution != null) {
				ActiveProject = null;
				OnSolutionClosing (new SolutionEventArgs (solution));
				solution = null;
				OnSolutionClosed (EventArgs.Empty);
			}
		}
		
		public static bool IsSolution (string fileName)
		{
			return Path.GetExtension (fileName) == ".sln";
		}
		
		public static event EventHandler<ProjectEventArgs>  ActiveProjectChanged;
		public static void OnActiveProjectChanged (ProjectEventArgs e)
		{
			if (ActiveProjectChanged != null)
				ActiveProjectChanged (null, e);
		}
		
		
		public static event EventHandler<SolutionEventArgs> SolutionOpened;
		public static void OnSolutionOpened (SolutionEventArgs e)
		{
			if (SolutionOpened != null)
				SolutionOpened (null, e);
		}
		
		public static event EventHandler<SolutionEventArgs> SolutionClosing;
		public static void OnSolutionClosing (SolutionEventArgs e)
		{
			if (SolutionClosing != null)
				SolutionClosing (null, e);
		}
		
		public static event EventHandler SolutionClosed;
		public static void OnSolutionClosed (EventArgs e)
		{
			if (SolutionClosed != null)
				SolutionClosed (null, e);
		}
	}
}
