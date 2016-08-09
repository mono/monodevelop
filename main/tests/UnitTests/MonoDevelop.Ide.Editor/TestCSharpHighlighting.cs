//
// SyntaxHighlightingTest.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Microsoft
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
using Mono.TextEditor;
using MonoDevelop.Ide.Editor.Util;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Ide.Editor.Highlighting;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Text;
using System.Threading;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class TestCSharpHighlighting : TestBase
	{
		[Test]
		public void TestComplexHighlighting ()
		{
			string highlighting = @"%YAML 1.2
---
# http://www.sublimetext.com/docs/3/syntax.html
name: ""C#""
file_extensions:
  - cs
  - csx
scope: source.cs
contexts:
  main:
    - match: '^\s*(using)\s+([^ ;]*);'
      scope: meta.keyword.using.source.cs
      captures:
        1: keyword.other.using.source.cs
    - match: '^\s*((namespace)\s+([\w.]+))'
      captures:
        1: meta.namespace.identifier.source.cs
        2: keyword.other.namespace.source.cs
        3: entity.name.type.namespace.source.cs
      push:
        - meta_scope: meta.namespace.source.cs
        - match: ""}""
          scope: punctuation.section.namespace.end.source.cs
          pop: true
        - match: ""{""
          scope: punctuation.section.namespace.begin.source.cs
          push:
            - meta_scope: meta.namespace.body.source.cs
            - match: ""(?=})""
              pop: true
            - include: code
    - include: code
  block:
    - match: ""{""
      scope: punctuation.section.block.begin.source.cs
      push:
        - meta_scope: meta.block.source.cs
        - match: ""}""
          scope: punctuation.section.block.end.source.cs
          pop: true
        - include: code
  builtinTypes:
    - match: \b(bool|byte|sbyte|char|decimal|double|float|int|uint|long|ulong|object|short|ushort|string|void|class|struct|enum|interface)\b
      scope: storage.type.source.cs
  class:
    - match: '(?=\w?[\w\s]*(?:class|struct|interface|enum)\s+\w+)'
      push:
        - meta_scope: meta.class.source.cs
        - match: ""}""
          scope: punctuation.section.class.end.source.cs
          pop: true
        - include: storage-modifiers
        - include: comments
        - match: (class|struct|interface|enum)\s+(\w+)
          scope: meta.class.identifier.source.cs
          captures:
            1: storage.modifier.source.cs
            2: entity.name.type.class.source.cs
        - match: "":""
          push:
            - match: ""(?={)""
              pop: true
            - match: '\s*,?([A-Za-z_]\w*)\b'
              captures:
                1: storage.type.source.cs
        - match: ""{""
          scope: punctuation.section.class.begin.source.cs
          push:
            - meta_scope: meta.class.body.source.cs
            - match: ""(?=})""
              pop: true
            - include: method
            - match: '='
              scope: keyword.operator.assignment.cs
              push:
                - match: ';'
                  pop: true
                - include: code
            - include: code
  code:
    - include: block
    - include: comments
    - include: class
    - include: constants
    - include: storage-modifiers
    - include: keywords
    - include: preprocessor
    - include: method-call
    - include: builtinTypes
  comments:
    - match: ///
      scope: punctuation.definition.comment.source.cs
      push:
        - meta_scope: comment.block.documentation.source.cs
        - match: $\n?
          scope: punctuation.definition.comment.source.cs
          pop: true
        - include: scope:text.xml
    - match: /\*
      scope: punctuation.definition.comment.source.cs
      push:
        - meta_scope: comment.block.source.cs
        - match: \*/\n?
          scope: punctuation.definition.comment.source.cs
          pop: true
    - match: //
      scope: punctuation.definition.comment.source.cs
      push:
        - meta_scope: comment.line.double-slash.source.cs
        - match: $\n?
          pop: true
  constants:
    - match: \b(true|false|null|this|base)\b
      scope: constant.language.source.cs
    - match: '\b((0(x|X)[0-9a-fA-F]*)|(([0-9]+\.?[0-9]*)|(\.[0-9]+))((e|E)(\+|-)?[0-9]+)?)(L|l|UL|ul|u|U|F|f|ll|LL|ull|ULL)?\b'
      scope: constant.numeric.source.cs
    - match: '@""'
      scope: punctuation.definition.string.begin.source.cs
      push:
        - meta_scope: string.quoted.double.literal.source.cs
        - match: '""""'
          scope: constant.character.escape.source.cs
        - match: '""'
          scope: punctuation.definition.string.end.source.cs
          pop: true
    - match: '""'
      scope: punctuation.definition.string.begin.source.cs
      push:
        - meta_scope: string.quoted.double.source.cs
        - match: '""'
          scope: punctuation.definition.string.end.source.cs
          pop: true
        - match: \\.
          scope: constant.character.escape.source.cs
    - match: ""'""
      scope: punctuation.definition.string.begin.source.cs
      push:
        - meta_scope: string.quoted.single.source.cs
        - match: ""'""
          scope: punctuation.definition.string.end.source.cs
          pop: true
        - match: \\.
          scope: constant.character.escape.source.cs
  keywords:
    - match: \b(if|else|while|for|foreach|in|do|return|continue|break|switch|case|default|goto|throw|try|catch|finally|lock|yield)\b
      scope: keyword.control.source.cs
    - match: \b(from|where|select|group|into|orderby|join|let|on|equals|by|ascending|descending)\b
      scope: keyword.linq.source.cs
    - match: \b(new|is|as|using|checked|unchecked|typeof|sizeof|override|readonly|stackalloc)\b
      scope: keyword.operator.source.cs
    - match: \b(var|event|delegate|add|remove|set|get|value)\b
      scope: keyword.other.source.cs
  method:
    - include: attributes
    - match: '(?=\bnew\s+)(?=[\w<].*\s+)(?=[^=]+\()'
      push:
        - meta_scope: meta.new-object.source.cs
        - match: ""(?={|;)""
          pop: true
        - include: code
    - match: '(?!new)(?=[\w<].*\s+)(?=[^=]+\()'
      push:
        - meta_scope: meta.method.source.cs
        - match: ""(})|(?=;)""
          scope: punctuation.section.method.end.source.cs
          pop: true
        - include: storage-modifiers
        - match: '([\w.]+)\s*\('
          captures:
            1: entity.name.function.source.cs
          push:
            - meta_scope: meta.method.identifier.source.cs
            - match: \)
              pop: true
            - include: parameters
        - match: '(?=\w.*\s+[\w.]+\s*\()'
          push:
            - meta_scope: meta.method.return-type.source.cs
            - match: '(?=[\w.]+\s*\()'
              pop: true
            - include: builtinTypes
        - match: ':\s*(this|base)\s*\('
          captures:
            1: constant.language.source.cs
          push:
            - meta_scope: meta.method.base-call.source.cs
            - match: \)
              pop: true
            - include: builtinTypes
        - include: comments
        - match: ""{""
          scope: punctuation.section.method.begin.source.cs
          push:
            - meta_scope: meta.method.body.source.cs
            - match: ""(?=})""
              pop: true
            - include: code
    - match: '(?!new)(?=[\w<].*\s+)(?=[^=]+\{)'
      push:
        - meta_scope: meta.property.source.cs
        - match: ""}""
          scope: punctuation.section.property.end.source.cs
          pop: true
        - include: storage-modifiers
        - match: '([\w.]+)\s*(?={)'
          captures:
            1: entity.name.function.source.cs
          push:
            - meta_scope: meta.method.identifier.source.cs
            - match: ""(?={)""
              pop: true
        - match: '(?=\w.*\s+[\w.]+\s*\{)'
          push:
            - meta_scope: meta.method.return-type.source.cs
            - match: '(?=[\w.]+\s*\{)'
              pop: true
            - include: builtinTypes
        - match: ""{""
          scope: punctuation.section.property.begin.source.cs
          push:
            - meta_scope: meta.method.body.source.cs
            - match: ""(?=})""
              pop: true
            - include: code
  method-call:
    - match: '([\w$]+)(\()'
      captures:
        1: meta.method.source.cs
        2: punctuation.definition.method-parameters.begin.source.cs
      push:
        - meta_scope: meta.method-call.source.cs
        - match: \)
          scope: punctuation.definition.method-parameters.end.source.cs
          pop: true
        - match: "",""
          scope: punctuation.definition.separator.parameter.source.cs
        - include: code
  attributes:
    - match: '\['
      push:
        - meta_scope: meta.method.attribute.source.cs
        - match: '\]'
          pop: true
        - include: constants
        - include: preprocessor
        - include: builtinTypes
  parameters:
    - include: attributes
    - match: '\b(ref|params|out)?\s*\b([\w.\[\]]+)\s+(\w+)\s*(=)?'
      captures:
        1: storage.type.modifier.source.cs
        2: storage.type.generic.source.cs
        3: variable.parameter.function.source.cs
        4: keyword.operator.assignment.source.cs
      push:
        - match: '(,)|(?=[\)])'
          scope: punctuation.definition.separator.parameter.source.cs
          pop: true
        - include: constants
        - include: block
  preprocessor:
    - match: ^\s*#\s*(region)\b(.*)$
      scope: meta.preprocessor.source.cs
      captures:
        2: meta.toc-list.region.source.cs
    - match: ^\s*#\s*(define)\b\s*(\S*)
      scope: meta.preprocessor.source.cs
      captures:
        2: entity.name.function.preprocessor.source.cs
    - match: ^\s*#\s*(if|else|elif|endif|define|undef|warning|error|line|pragma|region|endregion)\b
      scope: meta.preprocessor.source.cs
      captures:
        2: keyword.control.import.source.cs
  storage-modifiers:
    - match: \b(event|delegate|internal|public|protected|private|static|const|new|sealed|abstract|virtual|override|extern|unsafe|readonly|volatile|implicit|explicit|operator|partial)\b
      scope: storage.modifier.source.cs
";
			string test = @"
class X
   ^ storage.modifier
{

	[Usage(""Foo bar"")]
	   ^ meta.method.attribute
    void Run([Usage(""help text"")] int x, int y)
       ^ storage.type
          ^ entity.name.function
                 ^ meta.method.attribute
                       ^ string.quoted.double
                                   ^ storage.type
                                          ^ storage.type
    {
    }
}

string verbatim = @""This is a test """" of a verbatim string literal - C:\User""
                  ^ punctuation.definition.string.begin
                                   ^ constant.character.escape
                                                                       ^ string.quoted.double.literal
                                                                            ^ punctuation.definition.string.end

class A
{
   public A(int x, int y) {}
      ^ storage.modifier
          ^ entity.name.function
}
class B: A
{
   public B(int x, int y): base(x + y, x - y) {}" +
//                            ^ meta.method.base-call
//                                   ^ meta.method.base-call
@"}

public class GenericList<T>
{
    void Add(T input) { }
}
class TestGenericList
{
    private class ExampleClass { }
    static void Main()
    {
        GenericList<int> list1 = new GenericList<int>();
                     ^ storage.type

        GenericList<string> list2 = new GenericList<string>();

        GenericList<ExampleClass> list3 = new GenericList<ExampleClass>();
    }
}

public partial class Employee
       ^ storage.modifier
                     ^ entity.name.type.class
{
    public void DoWork()
    {
    }
}

public class Coo
{
    public Object text = ObjectMaker.MakeSomeText (""In order to publish your text, you need to do some texty things 'Like this' and then say hello."");
                                                                                                                                                  ^ string
    public Vector curves;
    int Zoo()
        ^ entity.name.function
    {}
}
";

			SyntaxHighlightingTest.RunSublimeHighlightingTest (highlighting, test);
		}
	}
	
}
