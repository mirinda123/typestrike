; Send message 7 through the existing DirectPlay COM session.
; Shared addresses come from native/layout.json.

pushad
mov esi, dword ptr [esp+36]
mov dword ptr [${Control.NetworkObject}], esi
movzx eax, word ptr [esi+0x50]
mov dword ptr [${Control.NetworkHost}], eax
movzx eax, word ptr [esi+0x3e]
mov dword ptr [${Control.NetworkConnected}], eax
mov eax, dword ptr [esi+0x4c]
mov dword ptr [${Control.LocalPlayerId}], eax
cmp dword ptr [${Control.Enabled}], 1
jne done
movzx eax, word ptr [esi+0x3e]
test eax, eax
jz done
mov eax, dword ptr [${Control.SendSequence}]
cmp eax, dword ptr [${Control.SendAcknowledged}]
je done
cmp dword ptr [${Control.Sending}], 0
jne done
mov dword ptr [${Control.Sending}], 1
push eax
xor edi, edi
push edi
mov ebx, esp
push ebx
mov ebp, dword ptr [esi+0x40]
test ebp, ebp
jz no_message
mov eax, dword ptr [ebp]
push ebp
call dword ptr [eax+0xc0]
mov dword ptr [${Control.NetworkResult}], eax
test eax, eax
js no_message
mov edi, dword ptr [ebx]
test edi, edi
jz no_message
mov eax, dword ptr [edi]
push 7
push edi
call dword ptr [eax+0x1c]
mov dword ptr [${Control.NetworkResult}], eax
test eax, eax
js release_message
mov eax, dword ptr [edi]
push ${CaveLayout.SendText}
push edi
call dword ptr [eax+0x14]
mov dword ptr [${Control.NetworkResult}], eax
test eax, eax
js release_message
mov eax, dword ptr [ebp]
push edi
push 1
push 0
push dword ptr [esi+0x4c]
push ebp
call dword ptr [eax+0xb4]
mov dword ptr [${Control.NetworkResult}], eax
inc dword ptr [${Control.PacketsSent}]
release_message:
mov eax, dword ptr [edi]
push edi
call dword ptr [eax+8]
no_message:
pop eax
pop eax
mov dword ptr [${Control.SendAcknowledged}], eax
mov dword ptr [${Control.Sending}], 0
done:
popad
push ebp
mov ebp, esp
sub esp, 0x18
jmp ${GameLayout.NetworkPollResume}
