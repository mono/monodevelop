#!/usr/bin/python -u
#
# Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.

"""
This module provides a simple xml serialization of the file parsed
so that we may use it to fill code completion data within MonoDevelop.

<module>
    <import name="os" from=""/>
    <class name="MyClass">
        <doc>This is a method that does something</doc>
        <attribute name="frobnicate" />
        <function name="do_something">
            <argument name="fork_off" />
            <argument name="*args" />
            <argument name="**kwargs" />
            <local name="tmp" />
        </function>
    </class>parent, node.attrname
    <function name="module_func"/>
    <attribute name="MODULE_ATTR"/>
</module>
"""

try:
    from cStringIO import StringIO
except ImportError:
    from StringIO  import StringIO

from   BaseHTTPServer import BaseHTTPRequestHandler, HTTPServer
import cgi
import compiler
import gc
import sys
from   xml.etree.ElementTree import ElementTree, Element

class XmlASTVisitor(compiler.visitor.ASTVisitor):
    """
    XmlASTVisitor is a simple visitor that will build an xml hierarchy
    representing the code being parsed.  This is intended to be the basic
    IPC so that we might add python code completion to MonoDevelop.
    """
    
    def __init__(self, stream = sys.stdout):
        """
        Initializes the visitor and sets the stream to be used for
        outputing xml data.
        """
        compiler.visitor.ASTVisitor.__init__(self)
        
        self.stream = stream
        self.tree = ElementTree(element = Element('module'))
        self.root = self.tree.getroot()
        
    def append(self, parent, child):
        if parent is None:
            parent = self.root
        parent.append(child)
        
    def walkChildren(self, node, parent = None):
        for child in node.getChildNodes():
            child.parent = node
            self.dispatch(child, parent)
            
    def default(self, node, parent = None):
        self.walkChildren(node, parent)
        
    def _haschildattr(self, element, key, value):
        for child in element.getiterator():
            if child.get(key, None) == value:
                return True
        return False
        
    def visitAssAttr(self, node, parent=None):
        if hasattr(node.expr, 'name'):
            if node.expr.name == 'self' and parent:
                # walk up until we reach the parent class
                while parent:
                    # add attriute child if one does not exist for attrname
                    if parent.tag == 'class' \
                    and not self._haschildattr(parent, 'name', node.attrname):
                        element = Element('attribute')
                        element.set('name', node.attrname)
                        element.set('line', str(node.lineno - 1)) # convert to md base
                        self.append(parent, element)
                        break
                    parent = self.getParent(parent)
        
    def visitAssName(self, node, parent = None):
        element = Element('attribute')
        element.set('name', node.name)
        element.set('line', str(node.lineno - 1))
        self.append(parent, element)
        
    def visitClass(self, node, parent = None):
        # build the class element
        element = Element('class')
        element.set('name', node.name)
        element.set('line', str(node.lineno))

        # get the end of the class
        def walk(n,e):
            for c in n.getChildNodes():
                if c.lineno > e:
                    e = c.lineno
                e = walk(c,e)
            return e
        endline = walk(node, node.lineno)
        element.set('endline', str(endline))

        # add class docs
        if node.doc:
            docElement = Element('doc')
            docElement.text = node.doc
            element.append(docElement)
        
        # add ourselves to the hierarchy
        self.append(parent, element)
        
        # walk our children, now we are the parent
        self.walkChildren(node, element)
        
    def visitFrom(self, node, parent = None):
        for name in node.names:
            element = Element('import')
            element.set('line', str(node.lineno))
            element.set('module', node.modname + '.' + name[0])
            element.set('name', name[1] or name[0])
            self.append(parent, element)
        
    def visitFunction(self, node, parent = None):
        element = Element('function')
        element.set('name', node.name)
        element.set('line', str(node.lineno))
        
        # get the end of the function
        def walk(n,e):
            for c in n.getChildNodes():
                if c.lineno > e:
                    e = c.lineno
                e = walk(c,e)
            return e
        endline = walk(node, node.lineno)
        element.set('endline', str(endline))

        # add our function arguments
        for pos, name in zip(range(len(node.argnames)), node.argnames):
            argElement = Element('argument')
            argElement.set('pos', str(pos))
            argElement.set('name', name)
            element.append(argElement)

        if node.kwargs and node.varargs:
            element[-1].set('name', '**' + element[-1].get('name'))
            element[-2].set('name', '*' + element[-2].get('name'))
        elif node.varargs:
            element[-1].set('name', '*' + element[-1].get('name'))
        elif node.kwargs:
            element[-1].set('name', '**' + element[-1].get('name'))
        
        # add function docs
        if node.doc:
            docElement = Element('doc')
            docElement.text = node.doc
            element.append(docElement)
            
        # add ourselves to the hierarchy
        self.append(parent, element)
        
        # walk our children, now we are the parent
        self.walkChildren(node, element)
        
    def visitImport(self, node, parent = None):
        for name in node.names:
            element = Element('import')
            element.set('line', str(node.lineno))
            element.set('name', name[1] or name[0])
            element.set('module', name[0])
            self.append(parent, element)
    
    def getParent(self, element):
        for parent in self.tree.getiterator():
            for child in parent:
                if child == element:
                    return parent

class ParseHandler(BaseHTTPRequestHandler):
    """
    This handler will take in an HTTP request containing the body
    of a python program and return an HTTP response with an xml
    encoding of the AST.
    """
    def do_POST(self):
        length, _ = cgi.parse_header(self.headers.getheader('content-length'))
        inContent = self.rfile.read(int(length))
        outContent = ""
        
        try:
            tmpStream = StringIO()
            self.parse(inContent, tmpStream)
            tmpStream.seek(0)

            self.send_response(200)
            outContent = tmpStream.read()
        except Exception, ex:
            self.send_response(200)
            outContent = '<error>%s</error>' % str(ex)

        self.send_header('Content-type', 'application/xml')
        self.end_headers()
        self.wfile.write(outContent)

    def parse(self, content, outStream):
        parse(content, outStream)

def parse(content, outStream):
    visitor = XmlASTVisitor(sys.stdout)
    
    try:
        # get our AST for the file
        mod = compiler.parse(content)

        # build our xml heirarchy of data
        visitor.preorder(mod, visitor, None)

        # add our total line count. we should have a better
        # way to do this. but oh well, we are a subprocess
        # so it shouldnt hurt us too bad
        root = visitor.tree.getroot()
        root.set('line', '0')
        root.set('endline', str(len(content.split('\n'))))

        # use pyflakes for warnings
        try:
            from pyflakes import checker
            pyChecker = checker.Checker(mod)
            root = visitor.tree.getroot()
            for warning in pyChecker.messages:
                element = Element('warning')
                element.set('line', str(warning.lineno))
                element.text = warning.message % warning.message_args
                root.append(element)
        except ImportError:
            pass

        visitor.tree.write(file = outStream)
    except SyntaxError, ex:
        outStream.write('<error line="%d" column="%d">%s</error>' % (ex.lineno, ex.offset, str(ex)))
    finally:
        gc.collect()

if __name__ == '__main__':
    if len(sys.argv[1:]) and sys.argv[1] == "MAGIC_TEST":
        if len(sys.argv[2:]):
            fName = sys.argv[2]
        else:
            fName = 'completion.py'
        parse(file(fName).read(), sys.stdout)
        print
        sys.exit(0)

    # generate a random port by opening a socket, and then closing it
    import socket
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.bind(('127.0.0.1', 0))
    _, port = s.getsockname()
    s.close()
    
    server = HTTPServer(('', port), ParseHandler)
    print >> sys.stdout, 'Listening on port %d' % port
    server.serve_forever()
