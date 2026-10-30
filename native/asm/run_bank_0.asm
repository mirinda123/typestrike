; Load the selected role/weapon running sheet (bank 0).
; Shared addresses come from native/layout.json.

pushad
cmp dword ptr [ebx+0x104], 1
jne police_art
cmp dword ptr [${Control.RenderWeapon}], 0
je unarmed_select
cmp dword ptr [${Control.RenderWeapon}], 1
jne shotgun_idle_select
mov ecx, dword ptr [${Control.RenderHeading}]
and ecx, 7
mov edx, dword ptr [${Control.ThiefAkRunTable0}]
cmp dword ptr [${Control.SkipTypingMove}], 0
je unarmed_art
mov edx, dword ptr [${Control.ThiefAkPoseTable0}]
jmp unarmed_art
shotgun_idle_select:
cmp dword ptr [${Control.RenderWeapon}], 2
jne weapon_art
mov ecx, dword ptr [${Control.RenderHeading}]
and ecx, 7
mov edx, dword ptr [${Control.ThiefShotgunRunTable0}]
cmp dword ptr [${Control.SkipTypingMove}], 0
je unarmed_art
mov edx, dword ptr [${Control.ThiefShotgunPoseTable0}]
jmp unarmed_art
unarmed_select:
mov ecx, dword ptr [${Control.RenderHeading}]
and ecx, 7
mov edx, dword ptr [${Control.ThiefRunTable0}]
cmp dword ptr [${Control.SkipTypingMove}], 0
je unarmed_art
mov edx, dword ptr [${Control.ThiefIdleTable0}]
unarmed_art:
mov edx, dword ptr [edx+ecx*4]
jmp art_chosen
police_art:
cmp dword ptr [${Control.RenderWeapon}], 0
jne police_armed_art
mov edx, dword ptr [${Control.PoliceRunTable0}]
jmp police_run_heading
police_armed_art:
mov edx, dword ptr [${Control.PoliceAkRunTable0}]
cmp dword ptr [${Control.RenderWeapon}], 1
je police_run_heading
cmp dword ptr [${Control.RenderWeapon}], 2
jne police_shield_art
mov edx, dword ptr [${Control.PoliceShotgunRunTable0}]
jmp police_run_heading
police_shield_art:
cmp dword ptr [${Control.RenderWeapon}], 3
jne weapon_art
mov edx, dword ptr [${Control.PoliceShieldRunTable0}]
police_run_heading:
mov ecx, dword ptr [${Control.RenderHeading}]
and ecx, 7
mov edx, dword ptr [edx+ecx*4]
jmp art_chosen
weapon_art:
mov edx, dword ptr [${Control.PoliceAkRun0}]
cmp dword ptr [ebx+0x104], 1
jne ak_art_selected
mov edx, dword ptr [${Control.ThiefAkRun0}]
ak_art_selected:
cmp dword ptr [${Control.RenderWeapon}], 2
jne art_chosen
mov edx, dword ptr [${Control.PoliceShotgunRun0}]
cmp dword ptr [ebx+0x104], 1
jne art_chosen
mov edx, dword ptr [${Control.ThiefShotgunRun0}]
art_chosen:
mov eax, dword ptr [ebx+0x70]
mov ecx, dword ptr [ebx+0x104]
mov eax, dword ptr [eax+ecx*4+16]
cmp edx, dword ptr [${Control.RunCachedPixels0}]
jne load_surface
cmp eax, dword ptr [${Control.RunDevice0}]
jne load_surface
cmp dword ptr [${Control.RunState0}], 0
jne finished_load
load_surface:
mov dword ptr [${Control.RunCachedPixels0}], edx
mov dword ptr [${Control.RunDevice0}], eax
mov esi, dword ptr [${Control.RunSurface0}]
mov dword ptr [${Control.RunSurface0}], 0
test esi, esi
jz no_old_surface
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
no_old_surface:
mov dword ptr [${Control.RunState0}], 1
push ${Control.ResourceProtection}
push 4
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
test eax, eax
jz load_failed
cld
mov esi, dword ptr [${Control.RunCachedPixels0}]
mov edi, dword ptr [${Control.ResourceAddress}]
mov ecx, dword ptr [${Control.SpriteDwords}]
rep movsd dword ptr es:[edi], dword ptr [esi]
push ${Control.ProtectionScratch}
push dword ptr [${Control.ResourceProtection}]
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
push dword ptr [${Control.SpriteName}]
push ${Control.RunSurface0}
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
mov dword ptr [${Control.RunState0}], 3
cmp dword ptr [${Control.RunSurface0}], 0
je finished_load
mov esi, dword ptr [${Control.RunSurface0}]
mov eax, dword ptr [esi]
push ${Control.SpriteFactoryResult}
push 8
push esi
call dword ptr [eax+0xbc]
mov dword ptr [${Control.RunResult0}], eax
test eax, eax
js finished_load
mov dword ptr [${Control.RunState0}], 2
inc dword ptr [${Control.RunLoads0}]
finished_load:
popad
ret
