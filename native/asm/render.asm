; Borrow a mod sprite for the current player, draw, then restore native fields.
; Shared addresses come from native/layout.json.

pushfd
pushad


call ${CaveLayout.Ime}
call ${CaveLayout.Music}
mov ebx, dword ptr [esp+40]
and ebx, 0xfffffffe
cmp dword ptr [${Control.Enabled}], 1
jne passthrough
cmp word ptr [ebx+0x100], 0
je offline
cmp dword ptr [${Control.PeerCompatible}], 1
jne passthrough
popad
popfd
jmp ${CaveLayout.RemoteRender}
offline:
cmp dword ptr [ebx+0x104], 1
ja passthrough
cmp dword ptr [ebx+0x34], 3
jne passthrough
cmp dword ptr [${Control.Focused}], 1
jne passthrough
cmp dword ptr [${Control.LoadingSprite}], 0
jne passthrough
mov eax, dword ptr [ebx+0x70]
test eax, eax
jz passthrough
mov ecx, 32
check_stock_surfaces:
cmp dword ptr [eax], 0
je passthrough
add eax, 4
dec ecx
jne check_stock_surfaces
mov dword ptr [${Control.LoadingSprite}], 1
mov edx, dword ptr [${Control.PoliceCardinalPixels}]
cmp dword ptr [ebx+0x104], 0
je cardinal_art_selected
mov edx, dword ptr [${Control.ThiefCardinalPixels}]
cardinal_art_selected:
mov dword ptr [${Control.CardinalPixels}], edx
mov eax, dword ptr [ebx+0x70]
mov ecx, dword ptr [ebx+0x104]
mov eax, dword ptr [eax+ecx*4+16]
cmp edx, dword ptr [${Control.CardinalCachedPixels}]
jne load_surface
cmp eax, dword ptr [${Control.CardinalDevice}]
jne load_surface
cmp dword ptr [${Control.CardinalState}], 0
je load_surface
jmp select_surface
load_surface:
mov dword ptr [${Control.CardinalCachedPixels}], edx
mov dword ptr [${Control.CardinalDevice}], eax
mov esi, dword ptr [${Control.CardinalSurface}]
mov dword ptr [${Control.CardinalSurface}], 0
test esi, esi
jz no_old_surface
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
no_old_surface:
mov dword ptr [${Control.CardinalState}], 1
push ${Control.ResourceProtection}
push 4
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
test eax, eax
jz load_failed
cld
mov esi, dword ptr [${Control.CardinalPixels}]
mov edi, dword ptr [${Control.ResourceAddress}]
mov ecx, dword ptr [${Control.SpriteDwords}]
rep movsd dword ptr es:[edi], dword ptr [esi]
push ${Control.ProtectionScratch}
push dword ptr [${Control.ResourceProtection}]
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
push dword ptr [${Control.SpriteName}]
push ${Control.CardinalSurface}
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
mov dword ptr [${Control.CardinalState}], 3
cmp dword ptr [${Control.CardinalSurface}], 0
je select_surface
mov esi, dword ptr [${Control.CardinalSurface}]
mov eax, dword ptr [esi]
push ${Control.SpriteFactoryResult}
push 8
push esi
call dword ptr [eax+0xbc]
mov dword ptr [${Control.SpriteResult}], eax
test eax, eax
js select_surface
mov dword ptr [${Control.CardinalState}], 2
inc dword ptr [${Control.CardinalLoads}]
select_surface:
mov eax, dword ptr [${Control.VisualWeapon}]
mov dword ptr [${Control.RenderWeapon}], eax
; Both roles now have authored unarmed and armed run banks.
load_run:
call ${CaveLayout.RunBank0}
call ${CaveLayout.RunBank1}
load_idle:
call ${CaveLayout.IdleBank}
choose_pose:
mov dword ptr [${Control.BorrowedSlot}], 0
mov eax, dword ptr [ebx+0xc8]
imul ecx, dword ptr [ebx+0x104], 56
add eax, ecx
cmp dword ptr [eax+0x24], 2
jne stock_draw
cmp dword ptr [ebx+0x104], 1
jne police_unarmed
cmp dword ptr [${Control.Armed}], 0
jne choose_weapon
cmp dword ptr [${Control.SkipTypingMove}], 0
jne thief_idle
cmp dword ptr [${Control.RunState0}], 2
jne stock_draw
cmp dword ptr [${Control.RunState1}], 2
jne stock_draw
; Capture frame once so a worker update cannot mix the two banks.
mov ecx, dword ptr [${Control.ThiefRunFrame}]
mov dword ptr [${Control.BorrowedSlot}], 5
jmp thief_baked_frame
thief_idle:
cmp dword ptr [${Control.RunState0}], 2
jne stock_draw
cmp dword ptr [${Control.RunState1}], 2
jne stock_draw
mov ecx, dword ptr [${Control.ThiefIdleFrame}]
mov dword ptr [${Control.BorrowedSlot}], 6
thief_baked_frame:
mov esi, dword ptr [${Control.RunSurface0}]
cmp ecx, 18
jb thief_run_chosen
sub ecx, 18
mov esi, dword ptr [${Control.RunSurface1}]
thief_run_chosen:
jmp custom_chosen
police_unarmed:
cmp dword ptr [${Control.Armed}], 0
jne choose_weapon
cmp dword ptr [${Control.SkipTypingMove}], 0
jne choose_cardinal
cmp dword ptr [${Control.RunState0}], 2
jne stock_draw
cmp dword ptr [${Control.RunState1}], 2
jne stock_draw
mov ecx, dword ptr [${Control.PoliceRunFrame}]
mov dword ptr [${Control.BorrowedSlot}], 13
jmp thief_baked_frame
choose_weapon:
cmp dword ptr [${Control.Armed}], 1
jne choose_cardinal
cmp dword ptr [ebx+0x104], 1
jne police_weapon
cmp dword ptr [${Control.RenderWeapon}], 1
jne choose_thief_shotgun
cmp dword ptr [${Control.RunState0}], 2
jne stock_draw
cmp dword ptr [${Control.RunState1}], 2
jne stock_draw
cmp dword ptr [${Control.SkipTypingMove}], 0
je thief_ak_run
mov ecx, dword ptr [${Control.ThiefAkIdleFrame}]
mov dword ptr [${Control.BorrowedSlot}], 7
jmp thief_baked_frame
thief_ak_run:
mov ecx, dword ptr [${Control.ThiefAkRunFrame}]
mov dword ptr [${Control.BorrowedSlot}], 8
jmp thief_baked_frame
choose_thief_shotgun:
cmp dword ptr [${Control.RenderWeapon}], 2
jne general_weapon
cmp dword ptr [${Control.RunState0}], 2
jne stock_draw
cmp dword ptr [${Control.RunState1}], 2
jne stock_draw
cmp dword ptr [${Control.SkipTypingMove}], 0
je thief_shotgun_run
mov ecx, dword ptr [${Control.ThiefShotgunIdleFrame}]
mov dword ptr [${Control.BorrowedSlot}], 9
jmp thief_baked_frame
thief_shotgun_run:
mov ecx, dword ptr [${Control.ThiefShotgunRunFrame}]
mov dword ptr [${Control.BorrowedSlot}], 10
jmp thief_baked_frame
police_weapon:
cmp dword ptr [${Control.RenderWeapon}], 1
jb general_weapon
cmp dword ptr [${Control.RenderWeapon}], 3
ja general_weapon
cmp dword ptr [${Control.SkipTypingMove}], 0
jne general_weapon
cmp dword ptr [${Control.RunState0}], 2
jne stock_draw
cmp dword ptr [${Control.RunState1}], 2
jne stock_draw
mov ecx, dword ptr [${Control.PoliceAkRunFrame}]
mov dword ptr [${Control.BorrowedSlot}], 11
cmp dword ptr [${Control.RenderWeapon}], 1
je thief_baked_frame
mov ecx, dword ptr [${Control.PoliceShotgunRunFrame}]
mov dword ptr [${Control.BorrowedSlot}], 12
cmp dword ptr [${Control.RenderWeapon}], 2
je thief_baked_frame
mov ecx, dword ptr [${Control.PoliceShieldRunFrame}]
mov dword ptr [${Control.BorrowedSlot}], 14
jmp thief_baked_frame
general_weapon:
cmp dword ptr [${Control.RunState0}], 2
jne choose_cardinal
cmp dword ptr [${Control.RunState1}], 2
jne choose_cardinal
mov ecx, dword ptr [${Control.RenderHeading}]
and ecx, 7
cmp dword ptr [${Control.SkipTypingMove}], 1
jne choose_armed_run
cmp dword ptr [${Control.IdleState}], 2
jne choose_armed_run
mov esi, dword ptr [${Control.IdleSurface}]
mov dword ptr [${Control.BorrowedSlot}], 4
xor ecx, ecx
jmp custom_chosen
choose_armed_run:
mov esi, dword ptr [${Control.RunSurface0}]
mov dword ptr [${Control.BorrowedSlot}], 2
cmp ecx, 4
jb armed_bank_chosen
sub ecx, 4
mov esi, dword ptr [${Control.RunSurface1}]
mov dword ptr [${Control.BorrowedSlot}], 3
armed_bank_chosen:
shl ecx, 2
jmp custom_chosen
choose_cardinal:
cmp dword ptr [${Control.SkipTypingMove}], 1
jne cardinal_run
cmp dword ptr [${Control.IdleState}], 2
jne cardinal_run
mov esi, dword ptr [${Control.IdleSurface}]
mov dword ptr [${Control.BorrowedSlot}], 4
xor ecx, ecx
jmp custom_chosen
cardinal_run:
cmp dword ptr [${Control.CardinalState}], 2
jne stock_draw
mov esi, dword ptr [${Control.CardinalSurface}]
mov dword ptr [${Control.BorrowedSlot}], 1
mov ecx, dword ptr [${Control.RenderHeading}]
and ecx, 6
shl ecx, 1
custom_chosen:


mov edx, dword ptr [eax+0x20]
add edx, 4
lea edi, [edx+edx*2]
mov ebp, dword ptr [ebx+0x9c]
lea ebp, [ebp+edi*4]
push dword ptr [ebp]
push dword ptr [ebp+4]
push ebp
mov dword ptr [ebp], 92
mov dword ptr [ebp+4], 99
mov edi, dword ptr [ebx+0x70]
lea edi, [edi+edx*4]
push dword ptr [edi]
push edi
lea ebp, [eax+0x1c]
push dword ptr [ebp]
push ebp
cmp dword ptr [${Control.BorrowedSlot}], 5
jb native_phase
mov edx, ecx
xor ecx, ecx
jmp add_phase
native_phase:
movzx edx, byte ptr [ebp]
and edx, 3
cmp dword ptr [${Control.BorrowedSlot}], 4
jne add_phase
cmp dword ptr [ebx+0x104], 0
jne legacy_idle_phase
cmp dword ptr [${Control.RenderWeapon}], 3
ja legacy_idle_phase
mov edx, dword ptr [${Control.PoliceIdleFrame}]
xor ecx, ecx
jmp add_phase
legacy_idle_phase:
mov eax, dword ptr [${Control.IdleTick}]
xor edx, edx
mov ecx, 18
div ecx
xor ecx, ecx
add_phase:
add edx, ecx
mov byte ptr [ebp], dl
mov dword ptr [${Control.BorrowedSurface}], edx
mov dword ptr [${Control.BorrowedDescriptor}], esi
mov dword ptr [edi], esi
push ebx
call ${CaveLayout.RenderOriginal}
pop edx
pop ecx
mov dword ptr [edx], ecx
pop edx
pop ecx
mov dword ptr [edx], ecx
pop edx
pop ecx
mov dword ptr [edx+4], ecx
pop ecx
mov dword ptr [edx], ecx
jmp finish
stock_draw:
push ebx
call ${CaveLayout.RenderOriginal}
finish:
push eax
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
