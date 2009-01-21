// 
// ChangelogPolicy.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.ChangeLogAddIn
{
	
	[DataItem ("ChangeLogPolicy")]
	public sealed class ChangeLogPolicy : IEquatable<ChangeLogPolicy>
	{
		
		public ChangeLogPolicy (ChangeLogUpdateMode mode, VcsIntegration vcsIntegration)
		{
			VcsIntegration = vcsIntegration;
			UpdateMode = mode;
		}
		
		public ChangeLogPolicy ()
		{
			//default to this because it's a useful feature
			VcsIntegration = VcsIntegration.Enabled;
		}
		
		[ItemProperty]
		public ChangeLogUpdateMode UpdateMode { get; private set; }
		
		[ItemProperty]
		public VcsIntegration VcsIntegration { get; private set; }

		public bool Equals (ChangeLogPolicy other)
		{
			return other != null && other.VcsIntegration == VcsIntegration && other.UpdateMode == UpdateMode;
		}
	}
	
	public enum ChangeLogUpdateMode
	{
		None = 0,
		Nearest,
		ProjectRoot,
		Directory
	}
	
	public enum VcsIntegration
	{
		None = 0,
		Enabled,
		RequireEntry
	}
}
