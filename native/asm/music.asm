; Mute the current native BGM buffer; restore volume and COM references.
; Shared addresses come from native/layout.json.

pushad
cmp dword ptr [${Control.Enabled}], 1
jne restore
cmp dword ptr [${Control.MusicWanted}], 1
jne restore
call dword ptr [${Control.GetTickCount}]
sub eax, dword ptr [${Control.Heartbeat}]
cmp eax, 600
ja restore


mov esi, dword ptr [${GameLayout.SoundEngine}]
test esi, esi
jz no_native_music
cmp word ptr [esi+0x34], 0
je no_native_music
mov edi, dword ptr [esi+0x40]
test edi, edi
jz no_native_music
mov esi, dword ptr [${Control.MusicBuffer}]
test esi, esi
jz acquire
cmp esi, edi
je set_mute
call release_cached
jmp acquire
set_mute:
mov eax, dword ptr [esi]
push -10000
push esi
call dword ptr [eax+0x50]
mov dword ptr [${Control.MusicResult}], eax
test eax, eax
js restore
mov dword ptr [${Control.MusicState}], 1
jmp done
acquire:
mov eax, dword ptr [edi]
push ${Control.MusicOriginalVolume}
push edi
call dword ptr [eax+0x28]
mov dword ptr [${Control.MusicResult}], eax
test eax, eax
js unavailable
mov eax, dword ptr [edi]
push edi
call dword ptr [eax+4]
mov esi, edi
mov dword ptr [${Control.MusicBuffer}], esi
inc dword ptr [${Control.MusicAcquisitions}]
jmp set_mute
unavailable:
mov dword ptr [${Control.MusicState}], 2
jmp done
no_native_music:
call release_cached

mov dword ptr [${Control.MusicState}], 1
jmp done
restore:
mov dword ptr [${Control.MusicState}], 0
call release_cached
jmp done
release_cached:
mov esi, dword ptr [${Control.MusicBuffer}]
test esi, esi
jz released
mov eax, dword ptr [esi]
push dword ptr [${Control.MusicOriginalVolume}]
push esi
call dword ptr [eax+0x50]
mov dword ptr [${Control.MusicResult}], eax
inc dword ptr [${Control.MusicRestores}]
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
mov dword ptr [${Control.MusicBuffer}], 0
released:
ret
done:
popad
ret
