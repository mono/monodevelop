using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using MonoDevelop.Ide;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.JavaScript.Parser
{
	public class JavaScriptParser : TypeSystemParser
	{
		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader content, Projects.Project project = null)
		{
			var parseDocument = new JavaScriptParsedDocument (fileName, content);
			parseDocument.Flags |= ParsedDocumentFlags.NonSerializable;
			return parseDocument;
		}
	}
}
