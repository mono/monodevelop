# We need the fsac server, which is provided separately. The build process should place the
# required files here.
from FSharp.lib import const
from FSharp.lib.fsac.server import Server

import sublime

_server = None

def get_server():
    global _server
    if _server is None or not _server.proc.stdin:
        if sublime.platform() in ('osx', 'linux'):
            _server = Server('mono', const.path_to_fs_ac_binary())
        else:
            assert sublime.platform() == 'windows'
            _server = Server(const.path_to_fs_ac_binary())
        _server.start()
        pass
    return _server
