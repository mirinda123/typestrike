; Apply authoritative remote poses on the native game thread.
; Shared addresses come from native/layout.json.

pushad
mov esi, dword ptr [esp+36]
and esi, 0xfffffffe
cmp dword ptr [${Control.Enabled}], 1
jne original
cmp dword ptr [esi+0x34], 3
jne original
cmp word ptr [esi+0x100], 0
je offline
cmp dword ptr [${Control.PeerCompatible}], 1
jne original
cmp dword ptr [esi+0x34], 3
jne original
call ${CaveLayout.CopyPoses}
cmp dword ptr [${Cave+0x5104}], esi
jne idle
cmp dword ptr [${Cave+0x5108}], 1
jne idle
mov eax, 1
sub eax, dword ptr [esi+0x104]
cmp eax, 1
ja original
imul edx, eax, 56
mov edi, dword ptr [esi+0xc8]
add edi, edx
imul eax, eax, 24
lea ebx, [eax+${Cave+0x5110}]
mov eax, dword ptr [ebx]
mov dword ptr [edi+4], eax
mov eax, dword ptr [ebx+4]
mov dword ptr [edi+8], eax
mov dword ptr [edi+0x10], 0
mov eax, dword ptr [ebx+20]
mov byte ptr [edi+0x1c], al
mov byte ptr [edi+0x1d], 0
mov ecx, dword ptr [ebx+8]
mov edx, ecx
shr edx, 2
xor edx, 1
and edx, 1
shl edx, 1
cmp ecx, 3
jb direction_ready
cmp ecx, 5
ja direction_ready
or edx, 1
direction_ready:
mov dword ptr [edi+0x28], edx
mov eax, dword ptr [ebx+16]
mov dword ptr [edi+0x24], eax
test eax, eax
jnz idle
; Original diagonal idle sheets have 9/11 frames. Their visual clock is local,
; independent of the four running phases carried by the unchanged wire format.
shl edx, 3
add edx, dword ptr [edi+0x20]
lea edx, [edx+edx*2]
mov ecx, dword ptr [esi+0x9c]
movzx ecx, byte ptr [ecx+edx*4+8]
mov eax, dword ptr [${Control.IdleTick}]
xor edx, edx
div ecx
mov byte ptr [edi+0x1c], dl
idle:
popad
xor eax, eax
ret 4
offline:
mov edi, dword ptr [esi+0xc8]
imul eax, dword ptr [esi+0x108], 56
add edi, eax
push dword ptr [edi+8]
push dword ptr [edi+4]
push esi
call native_ai
push dword ptr [esp+4]
push dword ptr [esp+4]
push edi
call ${CaveLayout.MapMove}
add esp, 8
popad
xor eax, eax
ret 4
original:
popad
native_ai:
push ebx
push esi
mov esi, dword ptr [esp+12]
jmp ${GameLayout.RemoteMoveResume}
