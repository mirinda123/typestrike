; Restore every borrowed native sprite field.
; Shared addresses come from native/layout.json.

pushad
mov esi, ${CaveLayout.BorrowedSprites}
mov edi, 2
loop:
cmp dword ptr [esi], 1
jne next
mov eax, dword ptr [esi+4]
mov ecx, dword ptr [esi+8]
mov dword ptr [eax], ecx
mov ecx, dword ptr [esi+12]
mov dword ptr [eax+4], ecx
mov eax, dword ptr [esi+16]
mov ecx, dword ptr [esi+20]
mov dword ptr [eax], ecx
mov eax, dword ptr [esi+24]
mov ecx, dword ptr [esi+28]
mov dword ptr [eax+0x1c], ecx
mov ecx, dword ptr [esi+32]
mov dword ptr [eax+0x24], ecx
mov ecx, dword ptr [esi+36]
mov dword ptr [eax+0x28], ecx
mov dword ptr [esi], 0
next:
add esi, 48
dec edi
jne loop
popad
ret
