from subprocess import Popen, PIPE
from os import path
import string
import threading
import tempfile
import Queue
import uuid
import hidewin

class FSharpInteractive:
    def __init__(self, fsi_path, is_debug = False):
        self._debug = is_debug
        #self.logfiledir = tempfile.gettempdir() + "/log.txt"
        #self.logfile = open(self.logfiledir, "w")
        id = 'vim-' + str(uuid.uuid4())
        command = [fsi_path, '--fsi-server:%s' % id, '--nologo']
        opts = { 'stdin': PIPE, 'stdout': PIPE, 'stderr': PIPE, 'shell': False, 'universal_newlines': True }
        hidewin.addopt(opts)
        self.p = Popen(command, **opts) 

        if is_debug:
            logfiledir = tempfile.gettempdir() + "/fsi-log.txt"
            self.logfile = open(logfiledir, "w")

        self._should_work = True
        self.lines = Queue.Queue()
        self.worker = threading.Thread(target=self._work, args=[])
        self.worker.daemon = True
        self.worker.start()
        self.err_worker = threading.Thread(target=self._err_work, args=[])
        self.err_worker.daemon = True
        self.err_worker.start()
        x = self.purge()
        self._current_path = None

    def _log(self, msg):
        if self._debug:
            self.logfile.write(msg + "\n")
            self.logfile.flush()

    def shutdown(self):
        print "shutting down fsi"
        self._should_work = False
        self.p.kill()

    def set_loc(self, path, line_num):
        self.p.stdin.write("#" + str(line_num) + " @\"" + path + "\"\n")
            
    def send(self, txt):
        self.p.stdin.write(txt + "\n")
        self.p.stdin.write(";;\n")
        self._log(">" + txt + ";;")

    def cd(self, path):
        if self._current_path == path:
            return
        self.p.stdin.write("System.IO.Directory.SetCurrentDirectory(@\"" + path + "\");;\n")
        self.p.stdin.write("#silentCd @\"" + path + "\";;\n")
        self.purge()
        self._current_path = path

    def purge(self):
        items = []
        while(True):
            try:
                l = self.lines.get(False).rstrip()
                if 'SERVER-PROMPT>' not in l:
                    items.append(l)
            except:
                break
        return items

    def read_until_prompt(self, time_out):
        output = []
        try:
            l = self.lines.get(True, time_out) 
            if 'SERVER-PROMPT>' in l:
                return output
            output.append(str(l).rstrip())
            while(True):
                l = self.read_one()
                if 'SERVER-PROMPT>' in l:
                    return output
                output.append(str(l).rstrip())
            return output
        except Exception as ex:
            output.append(".....") #indicate that there may be more lines of output
            return output

    def read_one(self):
        return self.lines.get(True, 0.5)

    def _work(self):
        while(self._should_work):
            try:
                l = self.p.stdout.readline()
                self.lines.put(l, True)
                self._log(l)
            except Exception as ex:
                print ex

    def _err_work(self):
        while(self._should_work):
            l = self.p.stderr.readline()
            self.lines.put(l, True)
            self._log( "err: " + l)
