; Record accepted native article progress while movement owns the player.
; Shared addresses come from native/layout.json.

cmp dword ptr [${Control.Enabled}], 1
jne original
cmp dword ptr [esi+0x104], 1
ja original
cmp dword ptr [esi+0x34], 3
jne original
cmp word ptr [esi+0x100], 0
je reset
cmp dword ptr [${Control.PeerCompatible}], 1
jne original
reset:
inc dword ptr [${Control.ArticleLoops}]
jmp ${GameLayout.ArticleAccept}
original:
cmp word ptr [esi+0x100], bx
jmp ${GameLayout.ArticleResume}
