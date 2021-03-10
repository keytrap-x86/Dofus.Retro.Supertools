If $CmdLine[0] == 0 Then
   Exit 1
EndIf

Global $xArg = $CmdLine[1];
;MsgBox($xArg)
Opt("WinTitleMatchMode", 2) ; 2 = Match any substring in the title
WinActivate($xArg)