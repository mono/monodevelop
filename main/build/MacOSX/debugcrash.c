void debug_trigger_sigsegv()
{
	void *p = (void*)0x12345;
	*(int *)p = 0;
}

