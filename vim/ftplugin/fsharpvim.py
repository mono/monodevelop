from subprocess import Popen, PIPE
from os import path
import string
import tempfile
import unittest
import json
import threading
import hidewin

class Statics:
    fsac = None
    fsi = None
    locations = []
    projects = {} 

class Interaction:
    def __init__(self, proc, timeOut, logfile = None):
        self.data = None
        self.event = threading.Event()
        self.proc = proc
        self._timeOut = timeOut
        self.logfile = logfile
        self.debug = not logfile is None

    def _write(self, txt):
        self.proc.stdin.write(txt)

        if self.debug:
            self.logfile.write("> " + txt)
            self.logfile.flush()

    def read(self):
        self.event.wait(self._timeOut)
        if self.debug:
            self.logfile.write('msg received %s\n' % self.data)
        return self.data

    def send(self, command):
        self.data = None
        self.event.clear()
        self._write(command)
        return self.read()

    def send_async(self, command):
        self.data = None
        self.event.clear()
        self._write(command)

    # only on worker thread
    def update(self, data):
        self.data = data
        self.event.set()

class FSAutoComplete:
    def __init__(self, dir, debug = False):
        if debug:
            self.logfiledir = tempfile.gettempdir() + "/log.txt"
            self.logfile = open(self.logfiledir, "w")
        else:
            self.logfile = None

        command = ['mono', dir + '/bin/fsautocomplete.exe']
        opts = { 'stdin': PIPE, 'stdout': PIPE, 'stderr': PIPE, 'universal_newlines': True }
        hidewin.addopt(opts)
        try:
            self.p = Popen(command, **opts)
        except WindowsError:
            self.p = Popen(command[1:], **opts)

        self.debug = debug
        self.switch_to_json()

        self.completion = Interaction(self.p, 3, self.logfile)
        self._finddecl = Interaction(self.p, 1, self.logfile)
        self._tooltip = Interaction(self.p, 1, self.logfile)
        self._helptext = Interaction(self.p, 1, self.logfile)
        self._errors = Interaction(self.p, 3, self.logfile)
        self._project = Interaction(self.p, 3, self.logfile)

        self.worker = threading.Thread(target=self.work, args=(self,))
        self.worker.daemon = True
        self.worker.start()

    def __log(self, msg):
        if self.debug:
            self.logfile.write(msg)

    def send(self, txt):
        if self.debug:
            self.logfile.write("> " + txt)
            self.logfile.flush()

        self.p.stdin.write(txt)

    def work(self,_):
        if self.debug:
            self.logfile2 = open(tempfile.gettempdir() + "/log2.txt", "w")

        while True:
            data = self.p.stdout.readline()

            if self.debug:
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
                data = parsed['Data']
                Statics.projects[data['Project']] = data
                self._project.update(data)
            elif parsed['Kind'] == "finddecl":
                self._finddecl.update(parsed['Data'])

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

        if self.debug:
            self.logfile.close()

    def complete(self, fn, line, column, base):
        self.__log('complete: base = %s\n' % base)

        msg = self.completion.send('completion "%s" %d %d\n' % (fn, line, column))

        self.__log('msg received %s\n' % msg)

        if msg is None:
            return []

        msg = map(str, msg)

        if base != '':
            msg = filter(lambda(line):
                    line.lower().find(base.lower()) != -1, msg)

        msg.sort(key=lambda x: x.startswith(base), reverse=True)
        msg = map(lambda(line):
                {'word': line,
                 'info': self.helptext(line), 
                 'menu': ""}, msg)

        return msg

    def finddecl(self, fn, line, column):
        msg = self._finddecl.send('finddecl "%s" %d %d\n' % (fn, line, column))
        if msg != None:
            return str(msg['File']), (int(str(msg['Line'])), int(str(msg['Column'])))
        else:
            return None

    def errors(self, fn, full, lines):
        self.__log('errors: fn = %s\n' % fn)

        fulltext = "full" if full else ""
        self.send("parse \"%s\" %s\n" % (fn, fulltext))

        for line in lines:
            self.send(line + "\n")

        msg = self._errors.send("<<EOF>>\n")
        self.__log('msg received: %s\n' % msg)

        return msg

    def errors_current(self):
        msg = self._errors.read()
        if msg == None:
            return []
        else:
            return msg

    def tooltip(self, fn, line, column):
        msg = self._tooltip.send('tooltip "%s" %d %d 500\n' % (fn, line, column))
        if msg != None:
            return str(msg)
        else:
            return ""

    def helptext(self, candidate):
        msg = self._helptext.send('helptext %s\n' % candidate)
        msg = str(msg[candidate])

        if "'" in msg:
            return msg
        else:
            return msg + " '" #HACK: - the ' appears to ensure that newlines are interpreted properly in the preview window

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
