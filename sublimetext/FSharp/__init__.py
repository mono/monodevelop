import os
import sys
import logging
import logging.handlers
import logging.config

import sublime

from FSharp.lib.editor import Editor
from FSharp.lib.response_processor import process_resp


LOGGER_PATH = os.path.join(os.path.dirname(__file__), 'fsharp.log')


logger = logging.getLogger('FSharp')
logger.setLevel(logging.ERROR)

handler = logging.handlers.RotatingFileHandler(LOGGER_PATH, 'a', 2**13)
handler.setLevel(logging.DEBUG)
formatter = logging.Formatter(fmt='%(asctime)s - %(name)s:%(levelname)s: %(message)s', datefmt='%m/%d/%Y %I:%M:%S %p')
handler.setFormatter(formatter)

# ST may reload this file multiple times and `logging` keeps adding handlers.
logger.handlers = []
logger.addHandler(handler)

logger.debug('top-level logger initialized')

logger.debug('starting editor context...')
editor_context = Editor(process_resp)


def plugin_unloaded():
    editor_context.fsac.stop()
