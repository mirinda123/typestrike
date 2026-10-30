; Draw the other player from the authoritative role/weapon pose.
; Shared addresses come from native/layout.json.

pushfd
pushad
mov ebx, dword ptr [esp+40]
and ebx, 0xfffffffe
cmp dword ptr [${Control.Enabled}], 1
jne passthrough
cmp dword ptr [${Control.PeerCompatible}], 1
jne passthrough
cmp dword ptr [ebx+0x34], 3
jne passthrough
cmp dword ptr [${Control.Focused}], 1
jne passthrough
cmp dword ptr [${Control.LoadingSprite}], 0
jne passthrough
call ${CaveLayout.CopyPoses}
cmp dword ptr [${Cave+0x5104}], ebx
jne passthrough
mov eax, dword ptr [ebx+0x70]
test eax, eax
jz passthrough
mov ecx, 32
check:
cmp dword ptr [eax], 0
je passthrough
add eax, 4
dec ecx
jne check
mov dword ptr [${Control.LoadingSprite}], 1
mov dword ptr [${CaveLayout.BorrowedSprites}], 0
mov dword ptr [${Cave+0x5630}], 0
xor eax, eax
call ${CaveLayout.BorrowSprite}
mov eax, 1
call ${CaveLayout.BorrowSprite}
push ebx
call ${CaveLayout.RenderOriginal}
push eax
call ${CaveLayout.RestoreSprite}
call ${CaveLayout.Overlay}
pop eax
mov dword ptr [esp+28], eax
mov dword ptr [${Control.LoadingSprite}], 0
popad
popfd
ret 4
passthrough:
popad
popfd
jmp ${CaveLayout.RenderOriginal}
