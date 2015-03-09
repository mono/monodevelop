// 
// ValaDocumentParser.cs
//  
// Author:
//       Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
// 
// Copyright (c) 2009 Levi Bard
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
using System.IO;
using System.Collections.Generic;
using System.Threading;

using MonoDevelop.ValaBinding.Parser.Afrodite;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.ValaBinding.Parser
{
    /// <summary>
    /// Parser for Vala source and vapi files
    /// </summary>
    public class ValaDocumentParser : TypeSystemParser
    {
        public ParsedDocument Parse2(bool storeAst, string fileName, TextReader reader, Project project = null)
        {
            DefaultParsedDocument defaultParsedDocument = new DefaultParsedDocument(fileName);
            defaultParsedDocument.Flags |= ParsedDocumentFlags.NonSerializable;
            ProjectInformation projectInformation = ProjectInformationManager.Instance.Get(project);
            string text = reader.ReadToEnd();
            string[] array = text.Split(new string[]
			{
				Environment.NewLine
			}, StringSplitOptions.None);
            return defaultParsedDocument;
        }
        public override ParsedDocument Parse(bool storeAst, string fileName, TextReader reader, Project project = null)
        {
            ParsedDocument result = new DefaultParsedDocument(fileName);
            ProjectInformation projectInformation = ProjectInformationManager.Instance.Get(project);
            ICollection<Symbol> classesForFile = projectInformation.GetClassesForFile(fileName);
            if (classesForFile == null || classesForFile.Count == 0)
            {
                return result;
            }
            foreach (Symbol current in classesForFile)
            {
                if (current != null)
                {
                    List<IMember> list = new List<IMember>();
                    int val = current.SourceReferences[0].LastLine;
                    foreach (Symbol current2 in current.Children)
                    {
                        if (1 <= current2.SourceReferences.Count && !(current2.SourceReferences[0].File != current.SourceReferences[0].File))
                        {
                            val = Math.Max(val, current2.SourceReferences[0].LastLine + 1);
                            LoggingService.LogWarning("ILAP: " + current2.MemberType.ToLower());
                        }
                    }
                }
            }
            return result;
        }
    }
}
