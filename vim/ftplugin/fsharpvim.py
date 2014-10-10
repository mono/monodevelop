from subprocess import Popen, PIPE
from os import path
import string
import tempfile
import unittest
import json
import threading
class Statics:
    fsac = None

class Interaction:
    def __init__(self, proc, timeOut, logfile):
        self.data = None
        self.event = threading.Event()
        self.proc = proc
        self.logfile = logfile
        self._timeOut = timeOut

    def _write(self, txt):
        self.logfile.write("> " + txt)
        self.logfile.flush()
        self.proc.stdin.write(txt)

    def send(self, command):
        self.data = None
        self.event.clear()
        self._write(command)
        self.event.wait(self._timeOut)
        self.logfile.write('msg received %s\n' % self.data)
        return self.data

    #only on worker thread
    def update(self, data):
        self.data = data 
        self.event.set()

class FSAutoComplete:
    def __init__(self, dir):
        self.logfiledir = tempfile.gettempdir() + "/log.txt"
        self.logfile = open(self.logfiledir, "w")
        command = ['mono', dir + '/bin/fsautocomplete.exe']
        opts = { 'stdin': PIPE, 'stdout': PIPE, 'universal_newlines': True }
        try:
            self.p = Popen(command, **opts)
        except WindowsError:
            self.p = Popen(command[1:], **opts)

        self.switch_to_json()


        self.completion = Interaction(self.p, 3, self.logfile)
        self._tooltip = Interaction(self.p, 1, self.logfile)
        self._helptext = Interaction(self.p, 1, self.logfile)
        self._errors = Interaction(self.p, 3, self.logfile)
        self._project = Interaction(self.p, 3, self.logfile)

        self.worker = threading.Thread(target=self.work, args=(self,))
        self.worker.daemon = True
        self.worker.start()

    def send(self, txt):
        self.logfile.write("> " + txt)
        self.logfile.flush()
        self.p.stdin.write(txt)

    def work(self,_):
        self.logfile2 = open(tempfile.gettempdir() + "/log2.txt", "w")
        while True:
            data = self.p.stdout.readline()
            self.logfile2.write("::work read: %s" % data)
            parsed = json.loads(data)
            if parsed['Kind'] == "completion":
                self.completion.update(parsed['Data'])
            elif parsed['Kind'] == "tooltip":
                self._tooltip.update(parsed['Data'])
            elif parsed['Kind'] == "helptext":
                self._helptext.update(parsed['Data'])
            elif parsed['Kind'] == "errors":
                self._errors.update(parsed['Data'])
            elif parsed['Kind'] == "project":
                self._project.update(parsed['Data'])
        
    def help(self):
        self.send("help\n")

    def switch_to_json(self):
        self.send("outputmode json\n")

    def project(self, fn):
        self.send("project \"%s\"\n" % path.abspath(fn))

    def parse(self, fn, full, lines):
        fulltext = "full" if full else ""
        self.send("parse \"%s\" %s\n" % (fn, fulltext))
        for line in lines:
            self.send(line + "\n")
        self.send("<<EOF>>\n")

    def quit(self):
        self.send("quit\n")
        self.p.wait()
        self.logfile.close()

    def complete(self, fn, line, column, base):
        self.logfile.write('complete: base = %s\n' % base)
        msg = self.completion.send('completion "%s" %d %d\n' % (fn, line, column))
        self.logfile.write('msg received %s\n' % msg)
        msg = map(str, msg)
        if base != '':
            msg = filter(lambda(line): 
                    line.lower().find(base.lower()) != -1, msg)

        msg = map(lambda(line): 
                {'word': line, 
                 'info': self.helptext(line),
                 'menu': ""}, msg)

        return msg

    def errors(self, fn, full, lines):
        self.logfile.write('errors: fn = %s\n' % fn)
        fulltext = "full" if full else ""
        self.send("parse \"%s\" %s\n" % (fn, fulltext))
        for line in lines:
            self.send(line + "\n")
        msg = self._errors.send("<<EOF>>\n")
        return msg
    
    def tooltip(self, fn, line, column):
        msg = self._tooltip.send('tooltip "%s" %d %d 500\n' % (fn, line, column))
        if(msg != None):
            return str(msg)
        else:
            return "" 

    def helptext(self, candidate):
        msg = self._helptext.send('helptext %s\n' % candidate) 
        return str(msg[candidate])

class FSharpVimFixture(unittest.TestCase):
    def setUp(self):
        self.fsac = FSAutoComplete('.')
        self.testscript = 'test/TestScript.fsx'
        with open(self.testscript, 'r') as content_file:
            content = map(lambda(line): line.strip('\n'), list(content_file))

        self.fsac.parse(self.testscript, True, content)

    def tearDown(self):
        self.fsac.quit()

    def test_completion(self):
        completions = self.fsac.complete(self.testscript, 8, 16, '')


if __name__ == '__main__':
    unittest.main() 
