; Continue the original all-scenes renderer after its six-byte detour.
push ebp
mov ebp, esp
sub esp, 0xc
jmp ${GameLayout.ResultRenderResume}
