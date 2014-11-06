from subprocess import Popen, PIPE
from os import path
import string
import threading
import Queue
import uuid

class FSharpInteractive:
    def __init__(self, fsi_path):
        #self.logfiledir = tempfile.gettempdir() + "/log.txt"
        #self.logfile = open(self.logfiledir, "w")
        id = 'vim-' + str(uuid.uuid4())
        command = [fsi_path, '--fsi-server:%s' % id, '--nologo']
        opts = { 'stdin': PIPE, 'stdout': PIPE, 'stderr': PIPE, 'shell': False, 'universal_newlines': True }
        self.p = Popen(command, **opts) 
        #try:
         #   self.p = Popen(command, **opts) 
        #except WindowsError:
         #   self.p = Popen(command[1:], **opts)

        self.should_work = True
        self.lines = Queue.Queue()
        self.worker = threading.Thread(target=self._work, args=[])
        self.worker.daemon = True
        self.worker.start()
        self.err_worker = threading.Thread(target=self._err_work, args=[])
        self.err_worker.daemon = True
        self.err_worker.start()
        x = self.purge()

    def shutdown(self):
        print "shutting down fsi"
        self.should_work = False
        self.p.kill()

    def set_loc(self, path, line_num):
        self.p.stdin.write("#" + str(line_num) + " @\"" + path + "\"\n")
            
    def send(self, txt):
        self.p.stdin.write(txt + ";;\n")

    def cd(self, path):
        self.p.stdin.write("System.IO.Directory.SetCurrentDirectory(@\"" + path + "\");;\n")
        self.p.stdin.write("#silentCd @\"" + path + "\";;\n")
        items = self.purge()
        return

    def purge(self):
        items = []
        while(True):
            try:
                l = self.read_one().rstrip()
                if 'SERVER-PROMPT>' not in l:
                    items.append(l)
            except:
                break
        return items

    def read_until_prompt(self):
        output = []
        try:
            l = self.lines.get(True, 10) #is one minute enough?
            if 'SERVER-PROMPT>' in l:
                return output
            output.append(str(l).rstrip())
            while(True):
                l = self.read_one()
                if 'SERVER-PROMPT>' in l:
                    return output
                output.append(str(l.rstrip()))
        except:
            return output

    def read_one(self):
        return self.lines.get(True, 0.5)

    def _work(self):
        while(self.should_work):
            l = self.p.stdout.readline()
            self.lines.put(l, True)

    def _err_work(self):
        while(self.should_work):
            l = self.p.stderr.readline()
            self.lines.put(l, True)
