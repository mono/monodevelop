from FSharp.tests.tools import FSharpSyntaxTest


class Test_DoubledQuotedStrings(FSharpSyntaxTest):
    def testCanDetectDoubleQuotedStrings(self):
        self.append('''
let foo = "some string here"
0123456789
          0123456789
                    0123456
''')
        actual = self.getFinestScopeNameAtRowCol(1, 11)
        self.assertEqual(actual, 'string.quoted.double.fsharp')


class Test_TripleQuotedStrings(FSharpSyntaxTest):
    def testCanDetectTripleQuotedStrings(self):
        self.append('''
let foo = """some string here"""
0123456789
          0123456789
                    0123456
''')
        actual = self.getFinestScopeNameAtRowCol(1, 13)
        self.assertEqual(actual, 'string.quoted.triple.fsharp')


class Test_VerbatimStrings(FSharpSyntaxTest):
    def testCanDetectVerbatimStrings(self):
        self.append('''
let foo = @"some string here"
0123456789
          0123456789
                    0123456
''')
        actual = self.getFinestScopeNameAtRowCol(1, 12)
        self.assertEqual(actual, 'string.quoted.double.verbatim.fsharp')