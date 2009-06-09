// 
// OldChangeLogData.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.ChangeLogAddIn
{
	
	[DataItem ("ChangeLogInfo")]
	class OldChangeLogData
	{
		[ItemProperty]
		ChangeLogPolicyEnum policy = ChangeLogPolicyEnum.UseParentPolicy;
		
		OldChangeLogData ()
		{
		}
		
		enum ChangeLogPolicyEnum
		{
			NoChangeLog,
			UseParentPolicy,
			UpdateNearestChangeLog,
			OneChangeLogInProjectRootDirectory,
			OneChangeLogInEachDirectory		
		}
		
		public static void Migrate (SolutionItem entry)
		{
			if (entry.ParentFolder != null)
				Migrate (entry.ParentFolder);
			
			OldChangeLogData data = entry.ExtendedProperties ["MonoDevelop.ChangeLogAddIn.ChangeLogInfo"] as OldChangeLogData;
			if (data == null)
				return;
			
			entry.ExtendedProperties.Remove ("MonoDevelop.ChangeLogAddIn.ChangeLogInfo");
			
			MonoDevelop.Projects.Policies.IPolicyContainer pc = entry.Policies;
			if (pc.DirectGet<ChangeLogPolicy> () != null)
				return;
			
			ChangeLogPolicyEnum policy = data.policy;
			
			if ((entry is SolutionFolder) && ((SolutionFolder)entry).IsRoot) {
				if (policy == ChangeLogPolicyEnum.UseParentPolicy)
					policy = ChangeLogPolicyEnum.UpdateNearestChangeLog;
			}
			
			ChangeLogUpdateMode mode;
			VcsIntegration intEnabled = VcsIntegration.Enabled;
			
			switch (policy) {
			case ChangeLogPolicyEnum.UseParentPolicy:
				return;
			case ChangeLogPolicyEnum.NoChangeLog:
				mode = ChangeLogUpdateMode.None;
				intEnabled = VcsIntegration.None;
				break;
			case ChangeLogPolicyEnum.OneChangeLogInEachDirectory:
				mode = ChangeLogUpdateMode.Directory;
				break;
			case ChangeLogPolicyEnum.OneChangeLogInProjectRootDirectory:
				mode = ChangeLogUpdateMode.ProjectRoot;
				break;
			case ChangeLogPolicyEnum.UpdateNearestChangeLog:
				mode = ChangeLogUpdateMode.Nearest;
				break;
			default:
				throw new InvalidOperationException ("Unknown value '" + policy + "'");
			}
			
			entry.Policies.Set (new ChangeLogPolicy (mode, intEnabled, null));
		}
	}
}
