using System;

namespace MonoDevelop.Projects.Parser
{
	
	public delegate void CommentTasksChangedEventHandler (object sender, CommentTasksChangedEventArgs e);
	
	public class CommentTasksChangedEventArgs : EventArgs
	{
		string filename;
		TagCollection tagComments;
		
		public CommentTasksChangedEventArgs (string filename, TagCollection tagComments)
		{
			this.filename = filename;
			this.tagComments = tagComments;
		}
		
		public string FileName { get { return filename; } }
		
		public TagCollection TagComments { get { return tagComments; } }
	}
}
