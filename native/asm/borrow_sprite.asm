; Temporarily substitute a sprite surface and descriptor.
; Shared addresses come from native/layout.json.

pushad

mov esi, eax
imul edi, eax, 24
add edi, ${Cave+0x5110}
imul ebp, eax, 48
add ebp, ${CaveLayout.BorrowedSprites}
mov edx, dword ptr [edi+8]
cmp dword ptr [edi+16], 0
jne moving
imul ecx, eax, 128
add ecx, ${Cave+0x5460}
cmp dword ptr [edi+12], 0
jne armed_idle
test eax, eax
jnz thief_unarmed_idle
mov edx, dword ptr [${Control.PoliceCardinalIdle}]
test eax, eax
jz cardinal_idle
mov edx, dword ptr [${Control.ThiefCardinalIdle}]
cardinal_idle:
jmp load_idle
armed_idle:
test eax, eax
jz legacy_armed_idle
cmp dword ptr [edi+12], 1
je thief_ak_idle
cmp dword ptr [edi+12], 2
je thief_shotgun_idle
legacy_armed_idle:
mov edx, dword ptr [${Control.PoliceAkIdle}]
test eax, eax
jz idle_ak_role
mov edx, dword ptr [${Control.ThiefAkIdle}]
idle_ak_role:
cmp dword ptr [edi+12], 2
jne load_idle
mov edx, dword ptr [${Control.PoliceShotgunIdle}]
test eax, eax
jz load_idle
mov edx, dword ptr [${Control.ThiefShotgunIdle}]
load_idle:
push eax
mov eax, dword ptr [edi+8]
mov edx, dword ptr [edx+eax*4]
pop eax
call ${CaveLayout.RemoteLoader}
; Police unarmed, AK, shotgun and shield idle share the authored nine-pose clock.
test eax, eax
jnz legacy_idle_clock
cmp dword ptr [edi+12], 3
ja legacy_idle_clock
mov edx, dword ptr [${Control.PoliceIdleFrame}]
jmp selected
legacy_idle_clock:
mov eax, dword ptr [${Control.IdleTick}]
xor edx, edx
push ecx
mov ecx, 18
div ecx
pop ecx
jmp selected
moving:
cmp dword ptr [edi+12], 0
jne armed
test eax, eax
jnz thief_unarmed_run
mov ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.PoliceRunFrame}]
mov esi, dword ptr [${Control.PoliceRunTable0}]
mov ebp, dword ptr [${Control.PoliceRunTable1}]
jmp thief_baked_bank
thief_unarmed_run:
; Reuse the existing two per-role run caches. The 24-frame visual clock is
; local; the four running phases carried by network packets stay unchanged.
imul ecx, eax, 128
add ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.ThiefRunFrame}]
mov esi, dword ptr [${Control.ThiefRunTable0}]
mov ebp, dword ptr [${Control.ThiefRunTable1}]
jmp thief_baked_bank
thief_unarmed_idle:
; Idle and run reuse both per-role caches; no allocation occurs while flipping.
imul ecx, eax, 128
add ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.ThiefIdleFrame}]
mov esi, dword ptr [${Control.ThiefIdleTable0}]
mov ebp, dword ptr [${Control.ThiefIdleTable1}]
jmp thief_baked_bank
thief_ak_idle:
imul ecx, eax, 128
add ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.ThiefAkIdleFrame}]
mov esi, dword ptr [${Control.ThiefAkPoseTable0}]
mov ebp, dword ptr [${Control.ThiefAkPoseTable1}]
jmp thief_baked_bank
thief_shotgun_idle:
imul ecx, eax, 128
add ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.ThiefShotgunIdleFrame}]
mov esi, dword ptr [${Control.ThiefShotgunPoseTable0}]
mov ebp, dword ptr [${Control.ThiefShotgunPoseTable1}]
thief_baked_bank:
cmp edx, 18
jb thief_first_bank
sub edx, 18
add ecx, 32
push edx
mov edx, ebp
jmp thief_bank_selected
thief_first_bank:
push edx
mov edx, esi
thief_bank_selected:
push eax
mov eax, dword ptr [edi+8]
mov edx, dword ptr [edx+eax*4]
pop eax
call ${CaveLayout.RemoteLoader}
pop edx
; Restore the role/borrow-record registers used by the common draw path.
mov esi, eax
imul ebp, eax, 48
add ebp, ${CaveLayout.BorrowedSprites}
jmp selected
armed:
test eax, eax
jz police_armed_run
cmp dword ptr [edi+12], 2
je thief_shotgun_run
cmp dword ptr [edi+12], 1
jne legacy_armed_run
imul ecx, eax, 128
add ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.ThiefAkRunFrame}]
mov esi, dword ptr [${Control.ThiefAkRunTable0}]
mov ebp, dword ptr [${Control.ThiefAkRunTable1}]
jmp thief_baked_bank
police_armed_run:
cmp dword ptr [edi+12], 3
je police_shield_run
cmp dword ptr [edi+12], 2
je police_shotgun_run
cmp dword ptr [edi+12], 1
jne legacy_armed_run
mov ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.PoliceAkRunFrame}]
mov esi, dword ptr [${Control.PoliceAkRunTable0}]
mov ebp, dword ptr [${Control.PoliceAkRunTable1}]
jmp thief_baked_bank
police_shield_run:
mov ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.PoliceShieldRunFrame}]
mov esi, dword ptr [${Control.PoliceShieldRunTable0}]
mov ebp, dword ptr [${Control.PoliceShieldRunTable1}]
jmp thief_baked_bank
police_shotgun_run:
mov ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.PoliceShotgunRunFrame}]
mov esi, dword ptr [${Control.PoliceShotgunRunTable0}]
mov ebp, dword ptr [${Control.PoliceShotgunRunTable1}]
jmp thief_baked_bank
thief_shotgun_run:
imul ecx, eax, 128
add ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.ThiefShotgunRunFrame}]
mov esi, dword ptr [${Control.ThiefShotgunRunTable0}]
mov ebp, dword ptr [${Control.ThiefShotgunRunTable1}]
jmp thief_baked_bank
legacy_armed_run:
imul ecx, eax, 128
add ecx, ${Cave+0x5420}
mov edx, dword ptr [${Control.PoliceAkRun0}]
test eax, eax
jz gun0_role
mov edx, dword ptr [${Control.ThiefAkRun0}]
gun0_role:
cmp dword ptr [edi+12], 2
jne gun0
mov edx, dword ptr [${Control.PoliceShotgunRun0}]
test eax, eax
jz gun0
mov edx, dword ptr [${Control.ThiefShotgunRun0}]
gun0:
call ${CaveLayout.RemoteLoader}
add ecx, 32
mov edx, dword ptr [${Control.PoliceAkRun1}]
test eax, eax
jz gun1_role
mov edx, dword ptr [${Control.ThiefAkRun1}]
gun1_role:
cmp dword ptr [edi+12], 2
jne gun1
mov edx, dword ptr [${Control.PoliceShotgunRun1}]
test eax, eax
jz gun1
mov edx, dword ptr [${Control.ThiefShotgunRun1}]
gun1:
call ${CaveLayout.RemoteLoader}
mov edx, dword ptr [edi+8]
sub ecx, 32
cmp edx, 4
jb run
sub edx, 4
add ecx, 32
run:
shl edx, 2
selected:
cmp dword ptr [ecx+4], 2
jne done
mov eax, dword ptr [ecx]
mov dword ptr [ebp+44], eax
cmp dword ptr [edi+16], 0
je phase
; Authored shield motion, like other police runs, already supplies its phase.
cmp dword ptr [edi+12], 2
jbe phase
cmp dword ptr [edi+12], 3
jne add_native_phase
test esi, esi
jz phase
add_native_phase:
add edx, dword ptr [edi+20]
phase:
mov dword ptr [ebp+40], edx
mov eax, dword ptr [ebx+0xc8]
imul esi, esi, 56
add eax, esi
mov dword ptr [ebp+24], eax
mov ecx, dword ptr [eax+0x1c]
mov dword ptr [ebp+28], ecx
mov ecx, dword ptr [eax+0x24]
mov dword ptr [ebp+32], ecx
mov ecx, dword ptr [eax+0x28]
mov dword ptr [ebp+36], ecx
mov dword ptr [eax+0x24], 2
mov dword ptr [eax+0x28], 0
mov byte ptr [eax+0x1c], dl
mov edx, dword ptr [eax+0x20]
add edx, 4
lea ecx, [edx+edx*2]
mov esi, dword ptr [ebx+0x9c]
lea esi, [esi+ecx*4]
mov dword ptr [ebp+4], esi
mov ecx, dword ptr [esi]
mov dword ptr [ebp+8], ecx
mov ecx, dword ptr [esi+4]
mov dword ptr [ebp+12], ecx
mov dword ptr [esi], 92
mov dword ptr [esi+4], 99
mov esi, dword ptr [ebx+0x70]
lea esi, [esi+edx*4]
mov dword ptr [ebp+16], esi
mov ecx, dword ptr [esi]
mov dword ptr [ebp+20], ecx
mov ecx, dword ptr [ebp+44]
mov dword ptr [esi], ecx
mov dword ptr [ebp], 1
done:
popad
ret
