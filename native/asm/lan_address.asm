; DirectPlayLobby.CreateInternetAddress receives a directed LAN broadcast BSTR.
; Replaces only push <empty BSTR>; preserve registers, flags and COM argument order.
pushfd
cmp dword ptr [${Control.Enabled}], 1
jne original
cmp dword ptr [${CaveLayout.LanAddressPointer}], 0
je original
inc dword ptr [${CaveLayout.LanAddressCalls}]
popfd
push dword ptr [${CaveLayout.LanAddressPointer}]
jmp ${GameLayout.LanAddressResume}
original:
popfd
push ${GameLayout.EmptyBstr}
jmp ${GameLayout.LanAddressResume}
