; Typing mode and character queue; the native article accepts characters.
; Shared addresses come from native/layout.json.

pushad
cmp dword ptr [${Control.Enabled}], 1
jne original
mov esi, dword ptr [esp+36]
and esi, 0xfffffffe
cmp word ptr [esi+0x100], 0
je network_ok
cmp dword ptr [${Control.PeerCompatible}], 1
jne original
network_ok:
cmp dword ptr [esi+0x104], 1
ja original
cmp dword ptr [esi+0x34], 3
je record
; Both offline and LAN results use the worker's release/press confirmation.
; Letting WM_CHAR Return reach VB6 would restart on a held firing key.
mov eax, dword ptr [esp+40]
cmp eax, 13
jne original
cmp dword ptr [esi+0x34], 4
je swallow
cmp dword ptr [esi+0x34], 5
je swallow
jmp original
record:
mov eax, dword ptr [esp+40]
cmp eax, 27
je original
cmp eax, 127
ja swallow


push eax
cmp eax, 9
je native_character_done
cmp dword ptr [${Control.TypingMode}], 1
jne native_character_done
push eax
push esi
call ${CaveLayout.CharacterOriginal}
native_character_done:
pop eax
mov ecx, dword ptr [${Control.KeyboardWrite}]
mov edx, ecx
sub edx, dword ptr [${Control.KeyboardRead}]
cmp edx, 128
jae swallow
and ecx, 127
mov dword ptr [ecx*8+${CaveLayout.KeyboardQueue}], eax
mov edi, dword ptr [esi+0xc8]
imul edx, dword ptr [esi+0x104], 56
mov edx, dword ptr [edi+edx+0x34]
mov dword ptr [ecx*8+${Cave+0x5804}], edx
inc dword ptr [${Control.KeyboardWrite}]
swallow:
popad
xor eax, eax
ret 8
original:
popad
push ebp
mov ebp, esp
sub esp, 0xc
jmp ${GameLayout.KeyResume}
