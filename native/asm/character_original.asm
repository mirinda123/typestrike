; Continue the original accepted-character handler.
; Shared addresses come from native/layout.json.

push ebp
mov ebp, esp
sub esp, 0xc
jmp ${GameLayout.KeyResume}
