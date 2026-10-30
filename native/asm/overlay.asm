; Copy one coherent snapshot, draw bullet traces, flash and pixel HUD.
; Shared addresses come from native/layout.json.

pushad
sub esp, 3584
mov eax, dword ptr [${CaveLayout.Snapshot}]
test eax, 1
jnz snapshot_unstable
mov edx, eax
mov esi, ${CaveLayout.Snapshot}
mov edi, esp
mov ecx, 896
cld
rep movsd dword ptr es:[edi], dword ptr [esi]
cmp edx, dword ptr [${CaveLayout.Snapshot}]
jne snapshot_unstable
mov esi, esp
mov edi, ${Render.Sequence}
mov ecx, 896
rep movsd dword ptr es:[edi], dword ptr [esi]
snapshot_unstable:

snapshot_ready:
add esp, 3584
cmp dword ptr [${Render.GameObject}], ebx
jne done
cmp dword ptr [${Render.Focused}], 1
jne done
mov ebp, dword ptr [${GameLayout.GraphicsEngine}]
hud:
push ebp
call ${GameLayout.BeginGdi}
mov edi, dword ptr [ebp+0x40]
test edi, edi
jz done
push edi
call dword ptr [${Control.SaveDC}]
push eax
push 0x00a8faff
push 2
push 0
call dword ptr [${Control.CreatePen}]
push eax
push eax
push edi
call dword ptr [${Control.SelectObject}]
cmp dword ptr [${Render.LineCount}], 0
je draw_hud_text
mov esi, ${Render.ProjectileLines}
mov ecx, dword ptr [${Render.LineCount}]
cmp ecx, 64
ja draw_hud_text
test ecx, ecx
jz flash
draw_bullet:
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
mov dword ptr [${Control.LineResult}], eax
pop ecx
add esi, 16
dec ecx
jne draw_bullet
flash:
cmp dword ptr [${Render.MuzzleFlash}], 1
jne draw_hud_text
mov eax, dword ptr [${Render.MuzzleScreenX}]
mov edx, dword ptr [${Render.MuzzleScreenY}]
cmp eax, 5
jl draw_hud_text
cmp eax, 793
jg draw_hud_text
cmp edx, 5
jl draw_hud_text
cmp edx, 493
jg draw_hud_text
push 0
sub edx, 4
sub eax, 4
push edx
push eax
push edi
call dword ptr [${Control.MoveToEx}]
mov eax, dword ptr [${Render.MuzzleScreenX}]
mov edx, dword ptr [${Render.MuzzleScreenY}]
add edx, 4
add eax, 4
push edx
push eax
push edi
call dword ptr [${Control.LineTo}]
draw_hud_text:
call shotgun_effects
call ${CaveLayout.Hud}
call ${CaveLayout.TypingPower}

pop esi
push edi
call dword ptr [${Control.RestoreDC}]
push esi
call dword ptr [${Control.DeleteObject}]
push ebp
call ${GameLayout.EndGdi}
done:
popad
ret


; Pixel flash and smoke share the game's back-buffer GDI pass. The article
; remains outside the 800x500 effect viewport. SaveDC is held by our caller.
shotgun_effects:
pushad
sub esp, 16
mov ecx, dword ptr [${Render.ShotgunFxCount}]
test ecx, ecx
jz effects_done
; Two independent shotguns can each emit 11 flash/smoke rectangles.
cmp ecx, 22
ja effects_done
push ecx
push 18
call dword ptr [${Control.GetStockObject}]
mov ebp, eax
pop ecx
mov esi, ${Render.ShotgunFxRects}
effects_rect:
mov eax, dword ptr [esi]
mov dword ptr [esp], eax
mov eax, dword ptr [esi+4]
mov dword ptr [esp+4], eax
mov eax, dword ptr [esi+8]
mov dword ptr [esp+8], eax
mov eax, dword ptr [esi+12]
mov dword ptr [esp+12], eax
push ecx
push dword ptr [esi+16]
push edi
call dword ptr [${Control.SetDCBrushColor}]
lea eax, [esp+4]
push ebp
push eax
push edi
call dword ptr [${Control.FillRect}]
pop ecx
add esi, 20
dec ecx
jne effects_rect
effects_done:
add esp, 16
popad
ret
