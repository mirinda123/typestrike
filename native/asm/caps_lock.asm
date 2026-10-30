; Preserve the original caps-lock handling branch.
; Shared addresses come from native/layout.json.

pushfd
cmp dword ptr [${Control.Enabled}], 1
jne original
or dword ptr [ebp-0x20], 0x80
original:
popfd
lea edx, [ebp-0x44]
lea eax, [ebp-0x3c]
jmp ${GameLayout.CapsResume}
