def is_fsharp_file(fname):
    return any((is_fsharp_code(fname),
                is_fsharp_project(fname)))


def is_fsharp_code(fname):
    return fname.endswith(('.fs', '.fsx', '.fsi'))


def is_fsharp_script(fname):
    return fname.endswith(('.fsscript', '.fsx'))


def is_fsharp_project(fname):
    return fname.endswith('.fsproj')
