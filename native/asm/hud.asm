; Draw skill availability, shared energy/count/time, typing art, health and weapons.
; Shared addresses come from native/layout.json.

pushad
cmp dword ptr [${Render.SkillVisible}], 1
jne done

mov ecx, dword ptr [${Render.BlinkLineCount}]
test ecx, ecx
jz draw_skill_icon
cmp ecx, 48
ja draw_skill_icon
push 0x00ffe080
push 1
push 0
call dword ptr [${Control.CreatePen}]
test eax, eax
jz draw_skill_icon
mov ebp, eax
push eax
push edi
call dword ptr [${Control.SelectObject}]
push eax
mov esi, ${Render.BlinkLines}
mov ecx, dword ptr [${Render.BlinkLineCount}]
line_loop:
push ecx
push 0
push dword ptr [esi+4]
push dword ptr [esi]
push edi
call dword ptr [${Control.MoveToEx}]
push dword ptr [esi+12]
push dword ptr [esi+8]
push edi
call dword ptr [${Control.LineTo}]
add esi, 16
pop ecx
dec ecx
jne line_loop
pop eax
push eax
push edi
call dword ptr [${Control.SelectObject}]
push ebp
call dword ptr [${Control.DeleteObject}]
draw_skill_icon:

mov eax, dword ptr [${Render.SkillRole}]
cmp eax, 1
ja skill_icon_done
mov dword ptr [${Control.SkillIconRole}], eax
mov esi, dword ptr [${Control.PoliceSkillIcon}]
test eax, eax
jz skill_icon_bank_ready
mov esi, dword ptr [${Control.ThiefSkillIcon}]
skill_icon_bank_ready:
test esi, esi
jz skill_icon_done
mov ecx, dword ptr [${Render.SkillDarkRows}]
cmp ecx, 64
ja skill_icon_done
mov dword ptr [${Control.SkillIconDarkRows}], ecx
imul ecx, ecx, 9216
add esi, ecx
push 0
push dword ptr [${Control.SkillIconBitmapInfo}]
push esi
push 48
push 0
push 0
push 0
push 48
push 48
push 448
push 740
push edi
call dword ptr [${Control.SetDIBitsToDevice}]
mov dword ptr [${Control.SkillIconResult}], eax
inc dword ptr [${Control.SkillIconFrames}]
skill_icon_done:

mov ecx, dword ptr [${Render.SupplyIconState}]
cmp ecx, 4
ja supply_icon_done
mov dword ptr [${Control.DisplayedSupplyState}], ecx
imul ecx, ecx, 9216
mov esi, dword ptr [${Control.SupplyIcon}]
add esi, ecx
push 0
push dword ptr [${Control.SkillIconBitmapInfo}]
push esi
push 48
push 0
push 0
push 0
push 48
push 48
push 448
push 684
push edi
call dword ptr [${Control.SetDIBitsToDevice}]
supply_icon_done:


push 18
call dword ptr [${Control.GetStockObject}]
test eax, eax
jz done
mov ebp, eax

; One six-cell energy meter below both icons; article pixels remain untouched.
mov ecx, dword ptr [${Render.EnergySteps}]
cmp ecx, 60
ja energy_hud_done
xor eax, eax
cmp dword ptr [${Render.Typing}], 1
sete al
mov dword ptr [${Control.TypingPromptVisible}], eax
mov eax, dword ptr [${Render.SkillRole}]
cmp eax, 1
ja energy_hud_done
imul eax, eax, 61
add eax, ecx
shl eax, 12
mov esi, dword ptr [${Control.EnergyHudSpans}]
test esi, esi
jz energy_hud_done
add esi, eax
mov ecx, dword ptr [esi]
cmp ecx, 204
ja energy_hud_done
test ecx, ecx
jz energy_hud_done
add esi, 4
energy_span:
push ecx
push dword ptr [esi+16]
push edi
call dword ptr [${Control.SetDCBrushColor}]
push ebp
push esi
push edi
call dword ptr [${Control.FillRect}]
inc dword ptr [${Control.EnergyHudRects}]
add esi, 20
pop ecx
dec ecx
jne energy_span
energy_hud_done:
; A small prebuilt highlight is clipped to the filled cells. Frame 20 is quiet.
mov eax, dword ptr [${Render.EnergyShineFrame}]
cmp eax, 20
jae energy_shine_done
mov ecx, dword ptr [${Render.EnergySteps}]
cmp ecx, 60
ja energy_shine_done
imul ecx, ecx, 20
add eax, ecx
shl eax, 7
mov esi, dword ptr [${Control.EnergyShineSpans}]
test esi, esi
jz energy_shine_done
add esi, eax
mov ecx, dword ptr [esi]
cmp ecx, 6
ja energy_shine_done
test ecx, ecx
jz energy_shine_done
add esi, 4
energy_shine_span:
push ecx
push dword ptr [esi+16]
push edi
call dword ptr [${Control.SetDCBrushColor}]
push ebp
push esi
push edi
call dword ptr [${Control.FillRect}]
inc dword ptr [${Control.EnergyHudRects}]
add esi, 20
pop ecx
dec ecx
jne energy_shine_span
energy_shine_done:
; Composite the mask and color into the existing back-buffer DC.
; Two bitmap passes avoid thousands of individual pixel-rectangle calls.
cmp dword ptr [${Render.Typing}], 1
jne typing_art_done
mov eax, dword ptr [${Render.TypingArtFrame}]
cmp eax, 41
jae typing_art_done
mov dword ptr [${Control.TypingArtFrame}], eax
imul eax, eax, 100800
mov esi, dword ptr [${Control.TypingArtPixels}]
test esi, esi
jz typing_art_done
add esi, eax
mov ebx, 0x008800c6
call typing_art_blit
add esi, 50400
mov ebx, 0x00ee0086
call typing_art_blit
inc dword ptr [${Control.TypingArtDraws}]
typing_art_done:
xor ebx, ebx
health_row:
mov eax, dword ptr [${Render.PoliceHearts}+ebx*4]
cmp eax, 3
ja health_next
shl eax, 1
mov ecx, dword ptr [${Render.PoliceInvulnerable}+ebx*4]
cmp ecx, 1
ja health_next
add eax, ecx
mov ecx, ebx
shl ecx, 3
add eax, ecx
shl eax, 12
mov esi, dword ptr [${Control.HeartSpans}]
test esi, esi
jz done
add esi, eax
mov ecx, dword ptr [esi]
cmp ecx, 200
ja health_next
test ecx, ecx
jz health_next
add esi, 4
health_span:
push ecx
push dword ptr [esi+16]
push edi
call dword ptr [${Control.SetDCBrushColor}]
push ebp
push esi
push edi
call dword ptr [${Control.FillRect}]
inc dword ptr [${Control.HeartRects}]
add esi, 20
pop ecx
dec ecx
jne health_span
health_next:
inc ebx
cmp ebx, 2
jb health_row

mov eax, dword ptr [${Render.Weapon}]
cmp eax, 2
ja done
mov dword ptr [${Control.WeaponState}], eax
shl eax, 17
mov esi, dword ptr [${Control.WeaponSpans}]
test esi, esi
jz done
add esi, eax
mov ecx, dword ptr [esi]
cmp ecx, 6000
ja done
test ecx, ecx
jz done
add esi, 4
weapon_span:
push ecx
push dword ptr [esi+16]
push edi
call dword ptr [${Control.SetDCBrushColor}]
push ebp
push esi
push edi
call dword ptr [${Control.FillRect}]
inc dword ptr [${Control.WeaponRects}]
add esi, 20
pop ecx
dec ecx
jne weapon_span

mov eax, dword ptr [${Render.Weapon}]
cmp eax, 1
jb done
cmp eax, 2
ja done
dec eax
imul eax, eax, 31
mov ecx, dword ptr [${Render.Ammo}]
cmp ecx, 30
ja done
mov dword ptr [${Control.DisplayedAmmo}], ecx
add eax, ecx
shl eax, 12
mov esi, dword ptr [${Control.AmmoSpans}]
test esi, esi
jz done
add esi, eax
call ammo_spans
mov eax, dword ptr [${Render.Reserve}]
cmp eax, 90
ja done
shl eax, 12
mov esi, dword ptr [${Control.ReserveSpans}]
add esi, eax
call ammo_spans
mov eax, dword ptr [${Render.ReloadProgress}]
mov dword ptr [${Control.ReloadProgress}], eax
test eax, eax
jz done
cmp eax, 64
ja done
shl eax, 10
mov esi, dword ptr [${Control.ReloadSpans}]
test esi, esi
jz done
add esi, eax
call ammo_spans
jmp done
ammo_spans:
mov ecx, dword ptr [esi]
cmp ecx, 200
ja ammo_return
test ecx, ecx
jz ammo_return
add esi, 4
ammo_span:
push ecx
push dword ptr [esi+16]
push edi
call dword ptr [${Control.SetDCBrushColor}]
push ebp
push esi
push edi
call dword ptr [${Control.FillRect}]
inc dword ptr [${Control.AmmoRects}]
add esi, 20
pop ecx
dec ecx
jne ammo_span
ammo_return:
ret

typing_art_blit:
push ebx
push 0
push dword ptr [${Control.TypingArtInfo}]
push esi
push 70
push 180
push 0
push 0
push 70
push 180
push 440
push 48
push edi
call dword ptr [${Control.StretchDIBits}]
ret
done:
popad
ret
