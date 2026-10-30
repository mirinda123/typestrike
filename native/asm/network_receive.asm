; Copy bounded message 7 into the helper receive mailbox.
; Shared addresses come from native/layout.json.

cmp dword ptr [ebp-0x114], 7
jne original
cmp dword ptr [${Control.Enabled}], 1
jne skip
pushad
mov dword ptr [${Control.ReceiveBstr}], 0
mov esi, dword ptr [ebp-0x3c]
mov eax, dword ptr [esi]
push ${Control.ReceiveBstr}
push esi
call dword ptr [eax+0x18]
test eax, eax
js free_string
mov esi, dword ptr [${Control.ReceiveBstr}]
test esi, esi
jz finish
mov ecx, dword ptr [esi-4]
test ecx, 1
jnz free_string
cmp ecx, 8190
ja free_string
inc dword ptr [${Control.ReceiveSequence}]
mov dword ptr [${Control.ReceiveBytes}], ecx
mov eax, dword ptr [ebp-0x48]
mov dword ptr [${Control.PacketSender}], eax
mov edi, ${CaveLayout.ReceiveText}
cld
rep movsb
inc dword ptr [${Control.ReceiveSequence}]
inc dword ptr [${Control.PacketsReceived}]
free_string:
push dword ptr [${Control.ReceiveBstr}]
call dword ptr [${Control.SysFreeString}]
mov dword ptr [${Control.ReceiveBstr}], 0
finish:
popad
skip:
jmp ${GameLayout.NetworkReceiveHandled}
original:
cmp dword ptr [ebp-0x114], 6
jmp ${GameLayout.NetworkReceiveResume}
