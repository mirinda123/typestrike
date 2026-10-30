; Continue the original graphics callback.
; Shared addresses come from native/layout.json.

push ecx
push esi
mov esi, dword ptr [esp+12]
jmp ${GameLayout.DeviceResume}
