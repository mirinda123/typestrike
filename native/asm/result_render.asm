; Draw result controls after the original scene, in the game back buffer.
; The callback receives the tagged PoliceGame pointer, as in RenderPlayers.

push ebp
mov ebp, esp
pushfd
pushad
push dword ptr [ebp+8]
call ${CaveLayout.ResultRenderOriginal}
mov dword ptr [esp+28], eax
mov dword ptr [${Control.ResultHintVisible}], 0
cmp dword ptr [${Control.Enabled}], 1
jne done
mov ebx, dword ptr [ebp+8]
and ebx, 0xfffffffe
mov eax, dword ptr [ebx+0x34]
sub eax, 4
cmp eax, 1
ja done
cmp dword ptr [${Control.Focused}], 1
jne done
mov esi, dword ptr [${Control.ResultHintSpans}]
mov edx, 1
cmp word ptr [ebx+0x100], 0
je bank_ready
add esi, 32768
inc edx
bank_ready:
mov dword ptr [${Control.ResultHintVisible}], edx
mov ebp, dword ptr [${GameLayout.GraphicsEngine}]
push ebp
call ${GameLayout.BeginGdi}
mov edi, dword ptr [ebp+0x40]
test edi, edi
jz done
push edi
call dword ptr [${Control.SaveDC}]
push eax
push 18
call dword ptr [${Control.GetStockObject}]
mov ebx, eax
mov ecx, dword ptr [esi]
add esi, 4
span:
push ecx
push dword ptr [esi+16]
push edi
call dword ptr [${Control.SetDCBrushColor}]
push ebx
push esi
push edi
call dword ptr [${Control.FillRect}]
add esi, 20
pop ecx
dec ecx
jnz span
push edi
call dword ptr [${Control.RestoreDC}]
push ebp
call ${GameLayout.EndGdi}
inc dword ptr [${Control.ResultHintFrames}]
done:
popad
popfd
pop ebp
ret 4
