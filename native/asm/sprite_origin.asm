; Compensate packed weapon-cell padding only at the player's destination X.
; World/camera state and the original native crop remain untouched.
pushfd
pushad
cmp dword ptr [${Control.Enabled}], 1
jne draw
cmp dword ptr [${Control.LoadingSprite}], 1
jne draw
cmp dword ptr [ebp-0x14], 1
ja draw
mov ebx, dword ptr [ebp+8]
cmp dword ptr [ebp-0x14], 0
je police
cmp word ptr [ebx+0x100], 0
jne network
mov eax, dword ptr [${Control.RenderHeading}]
cmp dword ptr [${Control.BorrowedSlot}], 7
je ak_idle
cmp dword ptr [${Control.BorrowedSlot}], 8
je ak_run
cmp dword ptr [${Control.BorrowedSlot}], 9
je shotgun_heading
cmp dword ptr [${Control.BorrowedSlot}], 10
je shotgun_run
jmp draw
network:
cmp dword ptr [${Control.PeerCompatible}], 1
jne draw
cmp dword ptr [${Cave+0x5630}], 1
jne draw
mov eax, dword ptr [${Cave+0x5130}]
cmp dword ptr [${Cave+0x5134}], 1
jne shotgun
cmp dword ptr [${Cave+0x5138}], 0
je ak_idle
jmp ak_run
shotgun:
cmp dword ptr [${Cave+0x5134}], 2
jne draw
cmp dword ptr [${Cave+0x5138}], 0
jne shotgun_run
shotgun_heading:
cmp eax, 2
je east
cmp eax, 5
je west
cmp eax, 6
jne draw
west:
; The detour replaces CALL, so the original argument block is at esp+36.
sub dword ptr [esp+40], 13
jmp draw
east:
add dword ptr [esp+40], 2
jmp draw
ak_idle:
mov ecx, dword ptr [${Control.ThiefAkPoseOrigins}]
jmp ak_heading
ak_run:
mov ecx, dword ptr [${Control.ThiefAkRunOrigins}]
jmp ak_heading
shotgun_run:
mov ecx, dword ptr [${Control.ThiefShotgunRunOrigins}]
ak_heading:
mov eax, dword ptr [ecx+eax*4]
add dword ptr [esp+40], eax
jmp draw
police:
cmp word ptr [ebx+0x100], 0
jne police_network
mov ecx, dword ptr [${Control.RenderWeapon}]
mov eax, dword ptr [${Control.RenderHeading}]
xor edx, edx
cmp dword ptr [${Control.BorrowedSlot}], 4
je police_table
mov edx, 1
cmp dword ptr [${Control.BorrowedSlot}], 11
je police_table
cmp dword ptr [${Control.BorrowedSlot}], 12
jne draw
jmp police_table
police_network:
cmp dword ptr [${Control.PeerCompatible}], 1
jne draw
cmp dword ptr [${CaveLayout.BorrowedSprites}], 1
jne draw
mov ecx, dword ptr [${Cave+0x511c}]
mov edx, dword ptr [${Cave+0x5120}]
mov eax, dword ptr [${Cave+0x5118}]
police_table:
cmp ecx, 1
jb draw
cmp ecx, 2
ja draw
; (weapon-1)*2 + moving selects one fixed eight-heading offset block.
dec ecx
shl ecx, 1
add ecx, edx
shl ecx, 5
mov edx, dword ptr [${Control.PoliceWeaponOrigins}]
add edx, ecx
mov eax, dword ptr [edx+eax*4]
add dword ptr [esp+40], eax
draw:
popad
popfd
call ${GameLayout.SpriteBlit}
jmp ${GameLayout.SpriteBlitResume}
