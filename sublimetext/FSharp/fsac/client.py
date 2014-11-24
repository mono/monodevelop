import threading
import logging
import queue
import json

from .server import requests_queue
from .server import responses_queue
from .server import _internal_comm


_logger = logging.getLogger(__name__)


def read_responses(responses, messages, resp_proc):
    """Reads responses from server and forwards them to @resp_proc.
    """
    while True:
        try:
            data = responses.get(block=True, timeout=5)
            if not data:
                _logger.warning('no response data to consume; exiting')
                break

            try:
                if messages.get(block=False) == STOP_SIGNAL:
                    _logger.info('asked to stop; complying')
                    break
            except:
                pass

            _logger.debug('response data read: %s', data)
            resp_proc(json.loads(data.decode('utf-8')))
        except queue.Empty:
            pass

    _logger.info('stopping reading responses')


class FsacClient(object):
    """Client for fsac server.
    """
    def __init__(self, server, resp_proc):
        self.requests = requests_queue
        self.server = server

        threading.Thread(target=read_responses, args=(responses_queue,
                                                      _internal_comm,
                                                      resp_proc)).start()

    def stop(self):
        self.server.stop()

    def send_request(self, request):
        _logger.debug('sending request: %s', str(request)[:100])
        self.requests.put(request.encode())
