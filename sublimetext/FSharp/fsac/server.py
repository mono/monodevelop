import threading
import queue
import os
import json

from .pipe_server import PipeServer

from FSharp.sublime_plugin_lib import PluginLogger


PATH_TO_FSAC = os.path.join(os.path.dirname(__file__),
                            'fsac/fsautocomplete.exe')


requests_queue = queue.Queue()
responses_queue = queue.Queue()
completions_queue = queue.Queue()
_internal_comm = queue.Queue()

STOP_SIGNAL = '__STOP'

_logger = PluginLogger(__name__)


def request_reader(server):
    while True:
        try:
            req = requests_queue.get(block=True, timeout=5)

            try:
                if _internal_comm.get(block=False) == STOP_SIGNAL:
                    print('asked to exit; complying')
                    _internal_comm.put(STOP_SIGNAL)
                    break
            except:
                pass

            if req:
                _logger.debug('reading request: %s', req)
                server.fsac.proc.stdin.write(req)
                server.fsac.proc.stdin.flush ()
        except queue.Empty:
            pass
    print("request reader exiting...")


def response_reader(server):
    while True:
        try:
            data = server.fsac.proc.stdout.readline()
            if not data:
                print ('no data; exiting')
                break

            try:
                if _internal_comm.get(block=False) == STOP_SIGNAL:
                    print('asked to exit; complying')
                    _internal_comm.put(STOP_SIGNAL)
                    break
            except:
                pass

            _logger.debug('reading response: %s', data)
            data_json = json.loads(data.decode('utf-8'))
            if data_json['Kind'] == 'completion':
                completions_queue.put(data)
                continue
            responses_queue.put (data)
        except queue.Empty:
            pass
    print("response reader exiting")


class FsacServer(object):
    def __init__(self, cmd):
        fsac = PipeServer(cmd)
        fsac.start()
        fsac.proc.stdin.write('outputmode json\n'.encode ('ascii'))
        self.fsac = fsac

        threading.Thread (target=request_reader, args=(self,)).start ()
        threading.Thread (target=response_reader, args=(self,)).start ()

    def stop(self):
        self._internal_comm.put(STOP_SIGNAL)
        self.proc.stdin.close()


def start():
    return FsacServer([PATH_TO_FSAC])
