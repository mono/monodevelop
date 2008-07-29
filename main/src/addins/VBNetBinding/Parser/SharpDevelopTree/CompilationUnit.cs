// created on 04.08.2003 at 17:31

using MonoDevelop.Projects.Parser;

namespace VBBinding.Parser.SharpDevelopTree
{
	public class CompilationUnit : DefaultCompilationUnit
	{
		
		public override CommentCollection MiscComments {
			get {
				return null;
			}
		}
		public override CommentCollection DokuComments {
			get {
				return null;
			}
		}
		public override TagCollection TagComments {
			get {
				return null;
			}
		}
	}
}
