from subprocess import Popen
from subprocess import PIPE
import logging
import json


logging.basicConfig(level=logging.DEBUG)


_CRLF = bytes('\r\n', 'ascii')
_EOF = bytes('<<EOF>>', 'ascii')


def to_str(utf8_enc):
    return utf8_enc.decode('utf-8')


def to_utf8(s):
    return s.encode('utf-8')


class Server(object):
    """
    Wraps fsautocomplete.exe daemon.
    """
    def __init__(self, path):
        self.path = path
        self.proc = None
    def start(self):

        logging.debug('Starting fsautocomplete...')
        try:
            self.proc = Popen([self.path], stdin=PIPE, stdout=PIPE)
        except Exception as e:
            logging.error("Exception occurred during fsautocomplete's startup")
            raise IOError("Could not open fsautocomplete.exe.")
        else:
            logging.debug('Switching output to json')
            self._send("outputmode json")

    def stop(self):
        logging.debug('Stopping fsautocomplete...')
        try:
            if self.proc:
                self._send('quit')
        except Exception as e:
            logging.error("Exception during fsautocomplete's shutdown")
            raise

    def help(self):
        self._send('help')

    def project(self, path):
        self._send('project "{0}"'.format(path))

    def errors(self):
        self._send('errors')

    def declarations(self, path):
        self._send('declarations "{0}"'.format(path))

    def completions(self, path, row, col, timeout=0):
        cmd = 'completion "{0}" {1} {2}'.format(path, row, col)
        if timeout > 0:
            cmd += ' ' + timeout
        self._send(cmd)

    def tooltip(self, path, row, col, timeout=0):
        cmd = 'tooltip "{0}" {1} {2}'.format(path, row, col)
        if timeout > 0:
            cmd += ' ' + timeout
        self._send(cmd)

    def find_declaration(self, fname, row, col, timeout=None):
        cmd = 'finddecl "{0}" {1} {2}'.format(fname, row, col)
        if timeout:
           cmd += ' ' + timeout
        self._send(cmd)

    def parse(self, fname, full=False):
        # request parsing
        cmd = 'parse "{0}"'.format(fname)
        if full:
            cmd += ' full'
        self._send(cmd)

        # now send the file's content
        try:
            with open(fname, 'r') as f:
                text = f.read()
        except Exception as e:
            logging.error('Could not read file for parsing')
            raise
        else:
            self._send(text + '\n<<EOF>>')

    def _send(self, cmd):
        cmd += '\n'
        logging.debug('sending command ' + repr(cmd))
        self.proc.stdin.write(to_utf8(cmd))
        self.proc.stdin.flush()
        self.proc.stdout.flush()

    def _unmarshal(self, s):
        try:
            val = json.loads(s)
            logging.debug('returning ' + repr(val))
            return val
        except:
            # Unmarshalling will fail when calling .help(), because we don't
            # get Json back from the daemon. Simply return the text.
            logging.debug('returning ' + repr(s))
            return s

    def _read(self):
        line = self.proc.stdout.readline()
        logging.debug('line read as utf-8: ' + repr(to_str(line)))
        return self._unmarshal(to_str(line))

    def _read_all(self):
        lines = b''
        while True:
            line = self.proc.stdout.readline()
            logging.debug('line read as utf-8: ' + repr(to_str(line)))
            if line == _EOF or line.endswith(_CRLF):
                break
            lines += line

        return self._unmarshal(to_str(lines))
