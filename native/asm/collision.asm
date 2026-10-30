; Physical catch and pending victory on the original UI thread.
; Shared addresses come from native/layout.json.

cmp dword ptr [${Control.Enabled}], 1
jne original
cmp word ptr [esi+0x100], 0
je network_ok
cmp dword ptr [${Control.PeerCompatible}], 1
jne original
network_ok:
cmp dword ptr [esi+0x104], 1
ja original
jmp ${GameLayout.CollisionHandled}
original:
mov eax, dword ptr [ebp-0x34]
test eax, eax
jmp ${GameLayout.CollisionResume}
