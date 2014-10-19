import vim

def jump(f, cur):
    found = False
    for tp in vim.tabpages:
        if tp.window.buffer.name == f:
            tp.window.cursor = cur
            vim.command('normal %igt' % tp.number)
            found = True
            break
    if not found:
        vim.command('tabnew %s' % f)
        last_tp = vim.tabpages[len(vim.tabpages)-1]
        last_tp.window.cursor = cur
