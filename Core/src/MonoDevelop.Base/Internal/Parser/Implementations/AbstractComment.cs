// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections.Specialized;

namespace MonoDevelop.Internal.Parser {

	[Serializable]
	public abstract class AbstractComment : System.MarshalByRefObject, IComment
	{
		protected bool    isBlockComment;
		protected string  commentTag;
		protected string  commentText;
		protected IRegion region;

		public virtual bool IsBlockComment {
			get {
				return isBlockComment;
			}
		}

		public virtual string CommentTag {
			get {
				return commentTag;
			}
		}

		public virtual string CommentText {
			get {
				return commentText;
			}
		}

		public virtual IRegion Region {
			get {
				return region;
			}
		}
	}
}
