; Apply a requested existing native scene (lobby/play/win/loss).
; Shared addresses come from native/layout.json.

pushad
mov ecx, dword ptr [${Control.SceneRequested}]
test ecx, ecx
jz done
dec ecx
cmp ecx, 5
ja clear
mov esi, dword ptr [${Control.SceneObject}]
test esi, esi
jz clear
mov eax, dword ptr [${GameLayout.ActiveCallback}]
and eax, 0xfffffffe
lea edx, [esi+0x160]
cmp eax, edx
jne clear
cmp dword ptr [esi], ${GameLayout.PoliceGameVtable}
jne clear
cmp word ptr [esi+0x100], 0
jne scene_ready
; Offline requests only restart a result screen. Esc may have already left it.
cmp ecx, 3
jne clear
mov eax, dword ptr [esi+0x34]
sub eax, 4
cmp eax, 1
ja clear
scene_ready:
cmp dword ptr [esi+0x34], ecx
je clear
mov eax, dword ptr [esi]
push ecx
push esi
call dword ptr [eax+0x2c]
clear:
mov dword ptr [${Control.SceneRequested}], 0
inc dword ptr [${Control.SceneChanges}]
done:
popad
ret
