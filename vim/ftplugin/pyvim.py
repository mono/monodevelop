import vim

def jump(f, cur):
    vim.command(':edit ' + f)
    vim.current.window.cursor = cur
