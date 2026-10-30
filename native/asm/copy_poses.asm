; Copy a stable pose mailbox using its even sequence number.
; Shared addresses come from native/layout.json.

pushad
mov eax, dword ptr [${CaveLayout.PoseMailbox}]
test eax, 1
jnz done
mov edx, eax
sub esp, 64
mov esi, ${CaveLayout.PoseMailbox}
mov edi, esp
mov ecx, 16
cld
rep movsd dword ptr es:[edi], dword ptr [esi]
cmp edx, dword ptr [${CaveLayout.PoseMailbox}]
jne unstable
mov esi, esp
mov edi, ${CaveLayout.PoseCache}
mov ecx, 16
rep movsd dword ptr es:[edi], dword ptr [esi]
unstable:
add esp, 64
done:
popad
ret
