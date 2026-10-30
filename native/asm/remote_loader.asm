; Create a remote role/weapon surface with native ownership.
; Shared addresses come from native/layout.json.

pushad
mov ebp, ecx
mov esi, dword ptr [ebx+0x70]
mov esi, dword ptr [esi+eax*4+16]
cmp dword ptr [ebp+8], edx
jne load
cmp dword ptr [ebp+12], esi
jne load
cmp dword ptr [ebp+4], 0
jne done
load:
mov dword ptr [ebp+8], edx
mov dword ptr [ebp+12], esi
mov esi, dword ptr [ebp]
mov dword ptr [ebp], 0
test esi, esi
jz no_surface
mov eax, dword ptr [esi]
push esi
call dword ptr [eax+8]
no_surface:
mov dword ptr [ebp+4], 1
push ${Control.ResourceProtection}
push 4
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
test eax, eax
jz failed
cld
mov esi, dword ptr [ebp+8]
mov edi, dword ptr [${Control.ResourceAddress}]
mov ecx, dword ptr [${Control.SpriteDwords}]
rep movsd dword ptr es:[edi], dword ptr [esi]
push ${Control.ProtectionScratch}
push dword ptr [${Control.ResourceProtection}]
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
push dword ptr [${Control.SpriteName}]
push ebp
push dword ptr [${GameLayout.GraphicsEngine}]
call ${GameLayout.CreateSprite}
push ${Control.ProtectionScratch}
push 4
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
cld
mov esi, dword ptr [${Control.ResourceBackup}]
mov edi, dword ptr [${Control.ResourceAddress}]
mov ecx, dword ptr [${Control.SpriteDwords}]
rep movsd dword ptr es:[edi], dword ptr [esi]
push ${Control.ProtectionScratch}
push dword ptr [${Control.ResourceProtection}]
push 165008
push dword ptr [${Control.ResourceAddress}]
call dword ptr [${Control.VirtualProtect}]
failed:
mov dword ptr [ebp+4], 3
mov esi, dword ptr [ebp]
test esi, esi
jz done
mov eax, dword ptr [esi]
push ${Control.SpriteFactoryResult}
push 8
push esi
call dword ptr [eax+0xbc]
mov dword ptr [ebp+20], eax
test eax, eax
js done
mov dword ptr [ebp+4], 2
inc dword ptr [ebp+16]
done:
popad
ret
