; EDI is the game's back-buffer HDC. BeginGdi selected the original article font.
; Measure the currently displayed accepted substring (game+F8), not the whole article.
; All GDI calls run on the game's owning render thread, with SaveDC held by overlay.
pushad
sub esp, 24
cmp dword ptr [${Render.SkillVisible}], 1
jne done
mov ecx, dword ptr [${Render.FireCount}]
test ecx, ecx
jz shake
cmp ecx, 32
ja shake
mov eax, dword ptr [${Render.GameObject}]
mov esi, dword ptr [eax+0xf8]
test esi, esi
jz shake
mov ecx, dword ptr [esi-4]
shr ecx, 1
cmp ecx, 2048
ja shake
mov eax, esp
push eax
push ecx
push esi
push edi
call dword ptr [${Control.GetTextExtentPoint32W}]
test eax, eax
jz shake
mov ebx, dword ptr [esp]
add ebx, 60
cmp ebx, 740
jle caret_ok
mov ebx, 740
caret_ok:
mov dword ptr [${Control.TypingCursorX}], ebx
push 18
call dword ptr [${Control.GetStockObject}]
mov ebp, eax
mov esi, ${Render.FireRects}
mov ecx, dword ptr [${Render.FireCount}]
particle:
push ecx
mov eax, dword ptr [esi]
add eax, ebx
mov dword ptr [esp+12], eax
mov eax, dword ptr [esi+4]
mov dword ptr [esp+16], eax
mov eax, dword ptr [esi+8]
add eax, ebx
mov dword ptr [esp+20], eax
mov eax, dword ptr [esi+12]
mov dword ptr [esp+24], eax
push dword ptr [esi+16]
push edi
call dword ptr [${Control.SetDCBrushColor}]
lea eax, [esp+12]
push ebp
push eax
push edi
call dword ptr [${Control.FillRect}]
inc dword ptr [${Control.FireRectsDrawn}]
add esi, 20
pop ecx
dec ecx
jne particle
shake:
mov eax, dword ptr [${Render.ShakeX}]
mov ecx, dword ptr [${Render.ShakeY}]
mov dword ptr [${Control.AppliedShakeX}], eax
mov dword ptr [${Control.AppliedShakeY}], ecx
test eax, eax
jnz apply_shake
test ecx, ecx
jz done
apply_shake:
; Move only pixels inside the 800x600 game surface. No OS window/display changes.
push 0x00cc0020
push 0
push 0
push edi
push 600
push 800
push ecx
push eax
push edi
call dword ptr [${Control.BitBlt}]
inc dword ptr [${Control.PowerFrames}]
done:
add esp, 24
popad
ret
