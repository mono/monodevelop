# We need the fsac server, which is provided separately. The build process should place the
# required files here.
from FSharp.const import const
from FSharp.fsac.server import Server

_server = None

def get_server():
    global _server
    if _server is None or not _server.proc.stdin:
        _server = Server(const.path_to_fs_ac_binary())
        _server.start()
        pass
    return _server
