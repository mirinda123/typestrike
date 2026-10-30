; Capture/close/restore only the game window input context.
; Shared addresses come from native/layout.json.

pushad
cmp dword ptr [${Control.Enabled}], 1
jne restore
mov eax, dword ptr [${Control.GameObject}]
test eax, eax
jz restore
cmp dword ptr [eax+0x34], 3
jb restore
cmp dword ptr [eax+0x34], 5
ja restore
call dword ptr [${Control.GetForegroundWindow}]
cmp eax, dword ptr [${Control.GameWindow}]
jne restore
cmp dword ptr [${Control.ImeCaptured}], 1
je done
push dword ptr [${Control.GameWindow}]
call dword ptr [${Control.ImmGetContext}]
mov esi, eax
test esi, esi
jz done
push esi
call dword ptr [${Control.ImmGetOpenStatus}]
mov dword ptr [${Control.SavedImeOpen}], eax
mov dword ptr [${Control.ImeCaptured}], 1
push 0
push esi
call dword ptr [${Control.ImmSetOpenStatus}]
jmp release
restore:
cmp dword ptr [${Control.ImeCaptured}], 1
jne done
push dword ptr [${Control.GameWindow}]
call dword ptr [${Control.ImmGetContext}]
mov esi, eax
test esi, esi
jz done
mov dword ptr [${Control.ImeCaptured}], 0
push dword ptr [${Control.SavedImeOpen}]
push esi
call dword ptr [${Control.ImmSetOpenStatus}]
release:
push esi
push dword ptr [${Control.GameWindow}]
call dword ptr [${Control.ImmReleaseContext}]
done:
popad
ret
