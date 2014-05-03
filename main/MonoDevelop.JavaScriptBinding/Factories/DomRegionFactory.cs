using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoDevelop.JavaScript.Factories
{
    public static class DomRegionFactory
    {
        public static DomRegion CreateDomRegion(string filename, Jurassic.Compiler.SourceCodeSpan position)
        {
            return CreateDomRegion(filename, position.StartLine, position.StartColumn, position.EndLine, position.EndColumn);
        }

        public static DomRegion CreateDomRegion(string filename, int startLine, int startColumn, int endLine, int endColumn)
        {
            var region = new DomRegion(filename,
                startLine,
                startColumn,
                endLine,
                endColumn);

            return region;
        }
    }
}
