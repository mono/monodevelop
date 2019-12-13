//
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
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public class TargetEvaluationContext: ProjectOperationContext
	{
		List<MSBuildLogger> loggers = new List<MSBuildLogger> ();

		public TargetEvaluationContext ()
		{
			PropertiesToEvaluate = new HashSet<string> ();
			ItemsToEvaluate = new HashSet<string> ();
			LogVerbosity = MSBuildProjectService.DefaultMSBuildVerbosity;
		}

		public TargetEvaluationContext (OperationContext other): this ()
		{
			if (other != null)
				CopyFrom (other);
		}

		public HashSet<string> PropertiesToEvaluate { get; private set; }

		public HashSet<string> ItemsToEvaluate { get; private set; }

		public MSBuildVerbosity LogVerbosity { get; set; }

		public ICollection<MSBuildLogger> Loggers {
			get { return loggers; }
		}

		public string BinLogFilePath { get; set; }

		/// <summary>
		/// Gets or sets the builder queue to be used to execute the target
		/// </summary>
		/// <remarks>
		/// This property helps the build system decide which builder to use
		/// to execute the target.
		/// </remarks>
		public BuilderQueue BuilderQueue { get; set; } = BuilderQueue.LongOperations;

		/// <summary>
		/// Gets or sets a value indicating whether referenced projects must be
		/// loaded and configured before running the target (true by default)
		/// </summary>
		public bool LoadReferencedProjects { get; set; } = true;

		public override void CopyFrom (OperationContext other)
		{
			base.CopyFrom (other);
			var o = other as TargetEvaluationContext;
			if (o != null) {
				PropertiesToEvaluate = new HashSet<string> (o.PropertiesToEvaluate);
				ItemsToEvaluate = new HashSet<string> (o.ItemsToEvaluate);
				loggers = new List<MSBuildLogger> (o.Loggers);
				LogVerbosity = o.LogVerbosity;
			}
		}
	}

	public enum BuilderQueue
	{
		/// <summary>
		/// This builder queue is used to execute targets that are quick to execute,
		/// for example getting the list of assembly references of a project.
		/// </summary>
		ShortOperations,

		/// <summary>
		/// This builder queue is used to execute targets which may take a long
		/// time to execute, for example building, cleaning or deploying a project.
		/// </summary>
		LongOperations
	}
}

