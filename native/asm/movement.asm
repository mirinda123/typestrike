; Local WASD integration, blink mailbox and animation state.
; Shared addresses come from native/layout.json.

pushad
mov esi, dword ptr [esp+36]
and esi, 0xfffffffe
cmp dword ptr [${Control.Enabled}], 1
jne original
cmp word ptr [esi+0x100], 0
je network_ok
cmp dword ptr [${Control.PeerCompatible}], 1
jne original
network_ok:
cmp dword ptr [esi+0x104], 1
ja original
cmp dword ptr [esi+0x34], 3
jne original
mov dword ptr [${Control.GameObject}], esi
call dword ptr [${Control.GetTickCount}]
mov edx, eax
sub edx, dword ptr [${Control.Heartbeat}]
cmp edx, 500
jbe alive
mov dword ptr [${Control.Enabled}], 0
jmp original
alive:
mov edx, dword ptr [${Control.LastMovementTick}]
mov dword ptr [${Control.LastMovementTick}], eax
sub eax, edx
cmp eax, 50
jbe delta_ok
mov eax, 0
delta_ok:
mov dword ptr [${Control.FrameMilliseconds}], eax
; The input worker publishes the thief AK frame from real shot events.
; Do not advance it here: idle breathing must never play the firing cells.
mov edi, dword ptr [esi+0xc8]
imul eax, dword ptr [esi+0x104], 56
add edi, eax
mov dword ptr [${Control.SkipTypingMove}], 0
mov eax, dword ptr [${Control.BlinkSequence}]
cmp eax, dword ptr [${Control.BlinkAcknowledged}]
je blink_done
cmp dword ptr [${Control.BlinkObject}], esi
jne blink_ack
mov ecx, dword ptr [esi+0x104]
cmp ecx, dword ptr [${Control.BlinkRole}]
jne blink_ack
mov ecx, dword ptr [${Control.BlinkX}]
mov dword ptr [edi+4], ecx
mov ecx, dword ptr [${Control.BlinkY}]
mov dword ptr [edi+8], ecx
mov word ptr [esi+0x102], 0xffff
blink_ack:
mov dword ptr [${Control.BlinkAcknowledged}], eax
blink_done:
cmp dword ptr [${Control.SkillActive}], 1
jne skill_not_started
mov word ptr [esi+0x102], 0xffff
skill_not_started:
mov eax, dword ptr [${Control.Armed}]
cmp eax, dword ptr [${Control.LastArmed}]
je equipment_unchanged
mov dword ptr [${Control.LastArmed}], eax
mov word ptr [edi+0x1c], 0
equipment_unchanged:
cmp dword ptr [${Control.Moving}], 0
je idle
mov word ptr [esi+0x102], 0xffff
push dword ptr [edi+8]
push dword ptr [edi+4]
fild dword ptr [${Control.FrameMilliseconds}]
fmul dword ptr [${Control.SecondsPerMillisecond}]
fmul dword ptr [${Control.VelocityX}]
fadd dword ptr [edi+4]
fstp dword ptr [edi+4]
fild dword ptr [${Control.FrameMilliseconds}]
fmul dword ptr [${Control.SecondsPerMillisecond}]
fmul dword ptr [${Control.VelocityY}]
fadd dword ptr [edi+8]
fstp dword ptr [edi+8]
fld dword ptr [edi+4]
fcomp dword ptr [${Control.WorldMinX}]
fnstsw ax
test ah, 1
jz x_high
mov eax, dword ptr [${Control.WorldMinX}]
mov dword ptr [edi+4], eax
x_high:
fld dword ptr [edi+4]
fcomp dword ptr [${Control.WorldMaxX}]
fnstsw ax
test ah, 0x41
jnz y_low
mov eax, dword ptr [${Control.WorldMaxX}]
mov dword ptr [edi+4], eax
y_low:
fld dword ptr [edi+8]
fcomp dword ptr [${Control.WorldMinY}]
fnstsw ax
test ah, 1
jz y_high
mov eax, dword ptr [${Control.WorldMinY}]
mov dword ptr [edi+8], eax
y_high:
fld dword ptr [edi+8]
fcomp dword ptr [${Control.WorldMaxY}]
fnstsw ax
test ah, 0x41
jnz animate
mov eax, dword ptr [${Control.WorldMaxY}]
mov dword ptr [edi+8], eax
animate:
push dword ptr [esp+4]
push dword ptr [esp+4]
push edi
call ${CaveLayout.MapMove}
add esp, 8
mov eax, dword ptr [${Control.StockDirection}]
mov ebx, 1
mov ecx, dword ptr [${Control.Heading}]
; Both unarmed roles have complete eight-heading, 24-pose visual loops.
cmp dword ptr [${Control.Armed}], 0
jne armed_direction
cmp dword ptr [${Control.RunState0}], 2
jne unarmed_direction
cmp dword ptr [${Control.RunState1}], 2
je moving_custom
armed_direction:
cmp dword ptr [${Control.Armed}], 1
jne unarmed_direction
cmp dword ptr [${Control.RunState0}], 2
jne unarmed_direction
cmp dword ptr [${Control.RunState1}], 2
je moving_custom
unarmed_direction:
bt dword ptr [${Control.CardinalHeadingMask}], ecx
jnc select_direction
cmp dword ptr [${Control.CardinalState}], 2
jne select_direction
moving_custom:
cmp dword ptr [${Control.RenderHeading}], ecx
je same_heading
mov word ptr [edi+0x1c], 0
same_heading:
mov dword ptr [${Control.RenderHeading}], ecx
xor eax, eax
mov ebx, 2
select_direction:
cmp dword ptr [edi+0x28], eax
je same_direction
mov word ptr [edi+0x1c], 0
same_direction:
mov dword ptr [edi+0x28], eax
cmp dword ptr [edi+0x24], ebx
je same_action
mov word ptr [edi+0x1c], 0
same_action:
mov dword ptr [edi+0x24], ebx
cmp ebx, 2
je custom_animation
shl eax, 2
inc eax
shl eax, 1
add eax, dword ptr [edi+0x20]
native_animation:
lea eax, [eax+eax*2]
mov edx, dword ptr [esi+0x9c]
lea edx, [edx+eax*4]
mov al, byte ptr [edi+0x1d]
cmp al, byte ptr [edx+9]
jb tick_animation
mov byte ptr [edi+0x1d], 0
mov al, byte ptr [edi+0x1c]
inc al
cmp al, byte ptr [edx+8]
jb keep_frame
xor al, al
keep_frame:
mov byte ptr [edi+0x1c], al
jmp capture
tick_animation:
inc byte ptr [edi+0x1d]
jmp capture
custom_animation:
cmp byte ptr [edi+0x1d], 3
jb tick_animation
mov byte ptr [edi+0x1d], 0
mov al, byte ptr [edi+0x1c]
inc al
and al, 3
mov byte ptr [edi+0x1c], al
jmp capture
idle:
mov ecx, dword ptr [${Control.Heading}]
; The approved police idle has complete art for all eight headings.
cmp dword ptr [esi+0x104], 0
jne thief_idle_check
cmp dword ptr [${Control.Armed}], 0
jne armed_idle
cmp dword ptr [${Control.IdleState}], 2
je idle_custom
jmp stock_idle
thief_idle_check:
cmp dword ptr [esi+0x104], 1
jne armed_idle
cmp dword ptr [${Control.Armed}], 0
jne armed_idle
cmp dword ptr [${Control.RunState0}], 2
jne unarmed_idle
cmp dword ptr [${Control.RunState1}], 2
je idle_custom
armed_idle:
cmp dword ptr [${Control.Armed}], 1
jne unarmed_idle
cmp dword ptr [${Control.RunState0}], 2
jne unarmed_idle
cmp dword ptr [${Control.RunState1}], 2
je idle_custom
unarmed_idle:
bt dword ptr [${Control.CardinalHeadingMask}], ecx
jnc stock_idle
cmp dword ptr [${Control.CardinalState}], 2
jne stock_idle
idle_custom:
mov dword ptr [${Control.SkipTypingMove}], 1
mov dword ptr [${Control.RenderHeading}], ecx
mov dword ptr [edi+0x24], 2
mov dword ptr [edi+0x28], 0
mov word ptr [edi+0x1c], 1
jmp capture
stock_idle:
mov dword ptr [edi+0x24], 0
mov eax, dword ptr [edi+0x28]
shl eax, 3
add eax, dword ptr [edi+0x20]
jmp native_animation
capture:
mov dword ptr [edi+0x10], 0
cmp dword ptr [${Control.PeerCompatible}], 1
je done
cmp dword ptr [${Control.SuppressPhysicalCatch}], 1
jne check_bullet_hit
mov word ptr [esi+0x102], 0xffff
check_bullet_hit:
cmp dword ptr [${Control.VictoryRequested}], 1
jne done
cmp dword ptr [${Control.VictoryObject}], esi
jne done
mov dword ptr [${Control.VictoryRequested}], 0
inc dword ptr [${Control.Victories}]
xor edx, edx
jmp player_victory
player_victory:
mov eax, dword ptr [esi]
add edx, 4
push edx
push esi
call dword ptr [eax+0x2c]
done:
popad
xor eax, eax
ret 4
original:
popad
mov edx, dword ptr [esp+4]
push ebx
jmp ${GameLayout.MoveResume}
