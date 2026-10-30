; Continue the original renderer after the detour.
; Shared addresses come from native/layout.json.

push ebp
mov ebp, esp
sub esp, 8
jmp ${GameLayout.RenderResume}
