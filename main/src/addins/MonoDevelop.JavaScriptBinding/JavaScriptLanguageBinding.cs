using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Projects;

namespace MonoDevelop.JavaScript
{
	class JavaScriptLanguageBinding : ILanguageBinding
	{
		public string Language {
			get { return "JavaScript"; }
		}

		public string SingleLineCommentTag {
			get { return "//"; }
		}

		public string BlockCommentStartTag {
			get { return "/*"; }
		}

		public string BlockCommentEndTag {
			get { return "*/"; }
		}

		public JavaScriptLanguageBinding()
		{
			// JSTypeSystemService.Initialize ();
		}

		public bool IsSourceCodeFile (Core.FilePath fileName)
		{
			return fileName.ToString ().EndsWith (".js", StringComparison.OrdinalIgnoreCase);
		}

		public Core.FilePath GetFileName (Core.FilePath fileNameWithoutExtension)
		{
			return fileNameWithoutExtension + ".js";
		}
	}
}
