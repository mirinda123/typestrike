; Release all owned remote surfaces.
; Shared addresses come from native/layout.json.

pushad
mov edi, ${CaveLayout.RemoteSurfaces}
mov ebp, 8
loop:
mov esi, dword ptr [edi]
mov dword ptr [edi], 0
mov dword ptr [edi+4], 0
test esi, esi
jz next
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
next:
add edi, 32
dec ebp
jne loop
popad
ret
