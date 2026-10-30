; Release owned surfaces and coordinate music on display changes.
; Shared addresses come from native/layout.json.

push ebp
mov ebp, esp
pushfd
pushad
push dword ptr [ebp+12]
push dword ptr [ebp+8]
call ${CaveLayout.DeviceOriginal}
cmp dword ptr [${Control.ResourceSwapActive}], 0
jne done
mov dword ptr [${Control.ResourceSwapActive}], 1
; This UI callback runs in offline results as well as DirectPlay matches.
cmp dword ptr [${Control.Enabled}], 1
jne scene_done
call ${CaveLayout.Scene}
scene_done:
call ${CaveLayout.Music}
call ${CaveLayout.Ime}
cmp dword ptr [${Control.LoadingSprite}], 0
jne finish
cmp dword ptr [${Control.ReleaseRequested}], 1
je release_surface
call dword ptr [${Control.GetForegroundWindow}]
cmp eax, dword ptr [${Control.GameWindow}]
jne release_surface
mov edx, dword ptr [ebp+12]
cmp word ptr [edx], 0
je finish
release_surface:
mov esi, dword ptr [${Control.CardinalSurface}]
mov dword ptr [${Control.CardinalSurface}], 0
mov dword ptr [${Control.CardinalState}], 0
test esi, esi
jz release_gun
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
release_gun:
mov esi, dword ptr [${Control.RunSurface0}]
mov dword ptr [${Control.RunSurface0}], 0
mov dword ptr [${Control.RunState0}], 0
test esi, esi
jz release_armed_second
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
release_armed_second:
mov esi, dword ptr [${Control.RunSurface1}]
mov dword ptr [${Control.RunSurface1}], 0
mov dword ptr [${Control.RunState1}], 0
test esi, esi
jz release_armed_idle
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
release_armed_idle:
mov esi, dword ptr [${Control.IdleSurface}]
mov dword ptr [${Control.IdleSurface}], 0
mov dword ptr [${Control.IdleState}], 0
test esi, esi
jz released
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
released:
call ${CaveLayout.ReleaseRemote}
mov dword ptr [${Control.ReleaseRequested}], 0
finish:
mov dword ptr [${Control.ResourceSwapActive}], 0
done:
popad
popfd
pop ebp
xor eax, eax
ret 8
