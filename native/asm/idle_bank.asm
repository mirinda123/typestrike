; Load the selected role/weapon standing sheet.
; Shared addresses come from native/layout.json.

pushad
cmp dword ptr [${Control.Armed}], 0
jne armed
mov edx, dword ptr [${Control.PoliceCardinalIdle}]
cmp dword ptr [ebx+0x104], 1
jne art_chosen
mov edx, dword ptr [${Control.ThiefCardinalIdle}]
jmp art_chosen
armed:
mov edx, dword ptr [${Control.PoliceAkIdle}]
cmp dword ptr [ebx+0x104], 1
jne ak_art_selected
mov edx, dword ptr [${Control.ThiefAkIdle}]
ak_art_selected:
cmp dword ptr [${Control.RenderWeapon}], 2
jne ak_bank
mov edx, dword ptr [${Control.PoliceShotgunIdle}]
cmp dword ptr [ebx+0x104], 1
jne shotgun_bank
mov edx, dword ptr [${Control.ThiefShotgunIdle}]
shotgun_bank:
ak_bank:
art_chosen:
mov ecx, dword ptr [${Control.RenderHeading}]
and ecx, 7
mov edx, dword ptr [edx+ecx*4]
test edx, edx
jz finished_load
mov eax, dword ptr [ebx+0x70]
mov ecx, dword ptr [ebx+0x104]
mov eax, dword ptr [eax+ecx*4+16]
cmp edx, dword ptr [${Control.IdleCachedPixels}]
jne load_surface
cmp eax, dword ptr [${Control.IdleDevice}]
jne load_surface
cmp dword ptr [${Control.IdleState}], 0
jne finished_load
load_surface:
mov dword ptr [${Control.IdleCachedPixels}], edx
mov dword ptr [${Control.IdleDevice}], eax
mov esi, dword ptr [${Control.IdleSurface}]
mov dword ptr [${Control.IdleSurface}], 0
test esi, esi
jz no_old_surface
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
no_old_surface:
mov dword ptr [${Control.IdleState}], 1
push ${Control.ResourceProtection}
push 4
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
test eax, eax
jz load_failed
cld
mov esi, dword ptr [${Control.IdleCachedPixels}]
mov edi, dword ptr [${Control.ResourceAddress}]
mov ecx, dword ptr [${Control.SpriteDwords}]
rep movsd dword ptr es:[edi], dword ptr [esi]
push ${Control.ProtectionScratch}
push dword ptr [${Control.ResourceProtection}]
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
push dword ptr [${Control.SpriteName}]
push ${Control.IdleSurface}
push dword ptr [${GameLayout.GraphicsEngine}]
call ${GameLayout.CreateSprite}
push ${Control.ProtectionScratch}
push 4
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
cld
mov esi, dword ptr [${Control.ResourceBackup}]
mov edi, dword ptr [${Control.ResourceAddress}]
mov ecx, dword ptr [${Control.SpriteDwords}]
rep movsd dword ptr es:[edi], dword ptr [esi]
push ${Control.ProtectionScratch}
push dword ptr [${Control.ResourceProtection}]
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
load_failed:
mov dword ptr [${Control.IdleState}], 3
cmp dword ptr [${Control.IdleSurface}], 0
je finished_load
mov esi, dword ptr [${Control.IdleSurface}]
mov eax, dword ptr [esi]
push ${Control.SpriteFactoryResult}
push 8
push esi
call dword ptr [eax+0xbc]
mov dword ptr [${Control.IdleResult}], eax
test eax, eax
js finished_load
mov dword ptr [${Control.IdleState}], 2
inc dword ptr [${Control.IdleLoads}]
finished_load:
popad
ret
