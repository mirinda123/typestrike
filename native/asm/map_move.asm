; stdcall MapMove(player record, previous X bits, previous Y bits).
; Exact counterpart of MovementMap.Move: <=1px steps, X then Y, wall sliding.
; Uses only stack scratch, preserves all registers and the x87 control word.
push ebp
mov ebp, esp
pushad
sub esp, 48
mov edi, dword ptr [ebp+8]
mov esi, dword ptr [${Control.MovementMap}]
mov eax, dword ptr [ebp+12]
mov dword ptr [ebp-36], eax
mov eax, dword ptr [ebp+16]
mov dword ptr [ebp-40], eax
mov eax, dword ptr [edi+4]
mov dword ptr [ebp-44], eax
mov eax, dword ptr [edi+8]
mov dword ptr [ebp-48], eax
fld dword ptr [ebp-44]
fsub dword ptr [ebp-36]
fst dword ptr [ebp-52]
fabs
fisttp dword ptr [ebp-60]
fld dword ptr [ebp-48]
fsub dword ptr [ebp-40]
fst dword ptr [ebp-56]
fabs
fisttp dword ptr [ebp-64]
mov eax, dword ptr [ebp-64]
cmp eax, dword ptr [ebp-60]
jbe count_ready
mov dword ptr [ebp-60], eax
count_ready:
inc dword ptr [ebp-60]
mov eax, dword ptr [ebp-60]
mov dword ptr [ebp-64], eax
fld dword ptr [ebp-52]
fidiv dword ptr [ebp-60]
fstp dword ptr [ebp-52]
fld dword ptr [ebp-56]
fidiv dword ptr [ebp-60]
fstp dword ptr [ebp-56]
mov dword ptr [ebp-68], 0
step:
fld dword ptr [ebp-36]
fadd dword ptr [ebp-52]
fstp dword ptr [ebp-72]
mov eax, dword ptr [ebp-72]
mov edx, dword ptr [ebp-40]
call solid
test eax, eax
jnz blocked_x
mov eax, dword ptr [ebp-72]
mov dword ptr [ebp-36], eax
jmp try_y
blocked_x:
or dword ptr [ebp-68], 1
try_y:
fld dword ptr [ebp-40]
fadd dword ptr [ebp-56]
fstp dword ptr [ebp-72]
mov eax, dword ptr [ebp-36]
mov edx, dword ptr [ebp-72]
call solid
test eax, eax
jnz blocked_y
mov eax, dword ptr [ebp-72]
mov dword ptr [ebp-40], eax
jmp next_step
blocked_y:
or dword ptr [ebp-68], 2
next_step:
dec dword ptr [ebp-64]
jnz step
; Preserve the exact requested coordinate on an unobstructed axis.
test dword ptr [ebp-68], 1
jnz finish_y
mov eax, dword ptr [ebp-44]
mov edx, dword ptr [ebp-40]
call solid
test eax, eax
jnz finish_y
mov eax, dword ptr [ebp-44]
mov dword ptr [ebp-36], eax
finish_y:
test dword ptr [ebp-68], 2
jnz finish
mov eax, dword ptr [ebp-36]
mov edx, dword ptr [ebp-48]
call solid
test eax, eax
jnz finish
mov eax, dword ptr [ebp-48]
mov dword ptr [ebp-40], eax
finish:
mov eax, dword ptr [ebp-36]
mov dword ptr [edi+4], eax
mov eax, dword ptr [ebp-40]
mov dword ptr [edi+8], eax
add esp, 48
popad
pop ebp
ret 12

; EAX/EDX contain candidate float bits. Return EAX=1 for an occupied foot.
solid:
push eax
push edx
fld dword ptr [esp+4]
fisttp dword ptr [ebp-76]
fld dword ptr [esp]
fisttp dword ptr [ebp-80]
add esp, 8
mov eax, dword ptr [ebp-76]
mov edx, dword ptr [ebp-80]
add eax, 40
add edx, 85
cmp eax, 64
jb outside
cmp edx, 117
jb outside
cmp eax, 1600
jae outside
cmp edx, 1200
jae outside
imul edx, edx, 1600
add edx, eax
mov ecx, edx
and ecx, 7
shr edx, 3
movzx eax, byte ptr [esi+edx]
mov ebx, 128
shr ebx, cl
test eax, ebx
setnz al
movzx eax, al
ret
outside:
mov eax, 1
ret
