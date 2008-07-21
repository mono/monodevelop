// WebDeployTarget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

using System;

using MonoDevelop.Core.Serialization;
using MonoDevelop.Deployment;

namespace MonoDevelop.AspNet.Deployment
{
	
	
	public class WebDeployTarget
	{
		[ItemProperty ("Name")]
		internal string name;
		
		[ItemProperty ("FileCopier")]
		FileCopyConfiguration fileCopier;
		
		internal WebDeployTargetCollection parent;
		
		public WebDeployTarget ()
		{
		}
		
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		public string Name {
			get { return name; }
			set {
				name = value;
				//when bound to a parent, check to avoid name collisions
				if (parent != null)
					parent.EnforceUniqueName (this);
			}
		}
		
		//returns GTK markup for lists
		public string GetMarkup ()
		{
			string locationLabel = MonoDevelop.Core.GettextCatalog.GetString ("Location: {0}", LocationName);
			return string.Format ("<b>{0}</b>\n{1}", Name, locationLabel);
		}
		
		public string LocationName {
			get {
				return ValidForDeployment ?
					fileCopier.FriendlyLocation :
					MonoDevelop.Core.GettextCatalog.GetString ("Not set");
			}
		}
		
		public bool ValidForDeployment {
			get {
				return (fileCopier != null && !string.IsNullOrEmpty (fileCopier.FriendlyLocation));
			}
		}
		
		public FileCopyConfiguration FileCopier
		{
			get { return fileCopier; }
			set { fileCopier = value; }
		}
		
		public override bool Equals (object o)
		{
			WebDeployTarget other = o as WebDeployTarget;
			if (other != null)
				return fileCopier == other.fileCopier && name == other.name;
			return false;
		}
		
		public override int GetHashCode ()
		{
			return fileCopier.GetHashCode () + name.GetHashCode ();
		}
	}
}
