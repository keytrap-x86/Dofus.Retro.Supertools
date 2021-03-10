#include-once
#include <Misc.au3>
#include <WinAPIGdi.au3>
#include <ScreenCapture.au3>
#include <GuiConstantsEx.au3>
#include <WindowsConstants.au3>

; * -----:| Dao Van Trong - TRONG.LIVE

; #INTERNAL_USE_ONLY# ===========================================================================================================
#cs
	; * -----:| Dao Van Trong - TRONG.LIVE
Global Const $tagGDIPENCODERPARAM = "struct;byte GUID[16];ulong NumberOfValues;ulong Type;ptr Values;endstruct"
Global Const $tagGDIPENCODERPARAMS = "uint Count;" & $tagGDIPENCODERPARAM
Global Const $tagGDIPSTARTUPINPUT = "uint Version;ptr Callback;bool NoThread;bool NoCodecs"
Global Const $tagGDIPIMAGECODECINFO = "byte CLSID[16];byte FormatID[16];ptr CodecName;ptr DllName;ptr FormatDesc;ptr FileExt;" & "ptr MimeType;dword Flags;dword Version;dword SigCount;dword SigSize;ptr SigPattern;ptr SigMask"
Global Const $tagGUID = "struct;ulong Data1;ushort Data2;ushort Data3;byte Data4[8];endstruct"
Global Const $STR_NOCASESENSEBASIC = 2
Global Const $tagOSVERSIONINFO = 'struct;dword OSVersionInfoSize;dword MajorVersion;dword MinorVersion;dword BuildNumber;dword PlatformId;wchar CSDVersion[128];endstruct'
Global Const $__tagCURSORINFO = "dword Size;dword Flags;handle hCursor;" & "struct;long X;long Y;endstruct"
Global Const $__WINVER = __WINVER()
Global Const $tagICONINFO = "bool Icon;dword XHotSpot;dword YHotSpot;handle hMask;handle hColor"
Global Const $GDIP_EPGCOLORDEPTH = '{66087055-AD66-4C7C-9A18-38A2310B8337}'
Global Const $GDIP_EPGCOMPRESSION = '{E09D739D-CCD4-44EE-8EBA-3FBF8BE4FC58}'
Global Const $GDIP_EPGQUALITY = '{1D5BE4B5-FA4A-452D-9CDD-5DB35105E7EB}'
Global Const $GDIP_EPTLONG = 4
Global Const $GDIP_EVTCOMPRESSIONLZW = 2
Global Const $GDIP_PXF16RGB555 = 0x00021005
Global Const $GDIP_PXF16RGB565 = 0x00021006
Global Const $GDIP_PXF24RGB = 0x00021808
Global Const $GDIP_PXF32RGB = 0x00022009
Global Const $GDIP_PXF32ARGB = 0x0026200A
Global $__g_hGDIPDll = 0
Global $__g_iGDIPRef = 0
Global $__g_iGDIPToken = 0
Global $__g_bGDIP_V1_0 = True
Global $__g_iBMPFormat = $GDIP_PXF24RGB
Global $__g_iJPGQuality = 100
Global $__g_iTIFColorDepth = 24
Global $__g_iTIFCompression = $GDIP_EVTCOMPRESSIONLZW
Global Const $__SCREENCAPTURECONSTANT_SM_CXSCREEN = 0
Global Const $__SCREENCAPTURECONSTANT_SM_CYSCREEN = 1
Global Const $__SCREENCAPTURECONSTANT_SRCCOPY = 0x00CC0020
Global Const $WS_POPUP = 0x80000000
Global Const $WS_EX_TOOLWINDOW = 0x00000080
Global Const $WS_EX_TOPMOST = 0x00000008

Func _IsPressed($sHexKey, $vDLL = "user32.dll")
	Local $aReturn = DllCall($vDLL, "short", "GetAsyncKeyState", "int", "0x" & $sHexKey)
	If @error Then Return SetError(@error, @extended, False)
	Return BitAND($aReturn[0], 0x8000) <> 0
EndFunc   ;==>_IsPressed
Func _WinAPI_GetCursorInfo()
	Local $tCursor = DllStructCreate($__tagCURSORINFO)
	Local $iCursor = DllStructGetSize($tCursor)
	DllStructSetData($tCursor, "Size", $iCursor)
	Local $aRet = DllCall("user32.dll", "bool", "GetCursorInfo", "struct*", $tCursor)
	If @error Or Not $aRet[0] Then Return SetError(@error + 10, @extended, 0)
	Local $aCursor[5]
	$aCursor[0] = True
	$aCursor[1] = DllStructGetData($tCursor, "Flags") <> 0
	$aCursor[2] = DllStructGetData($tCursor, "hCursor")
	$aCursor[3] = DllStructGetData($tCursor, "X")
	$aCursor[4] = DllStructGetData($tCursor, "Y")
	Return $aCursor
EndFunc   ;==>_WinAPI_GetCursorInfo
Func __WINVER()
	Local $tOSVI = DllStructCreate($tagOSVERSIONINFO)
	DllStructSetData($tOSVI, 1, DllStructGetSize($tOSVI))
	Local $aRet = DllCall('kernel32.dll', 'bool', 'GetVersionExW', 'struct*', $tOSVI)
	If @error Or Not $aRet[0] Then Return SetError(@error, @extended, 0)
	Return BitOR(BitShift(DllStructGetData($tOSVI, 2), -8), DllStructGetData($tOSVI, 3))
EndFunc   ;==>__WINVER
Func _WinAPI_GUIDFromString($sGUID)
	Local $tGUID = DllStructCreate($tagGUID)
	_WinAPI_GUIDFromStringEx($sGUID, $tGUID)
	If @error Then Return SetError(@error + 10, @extended, 0)
	Return $tGUID
EndFunc   ;==>_WinAPI_GUIDFromString
Func _WinAPI_GUIDFromStringEx($sGUID, $tGUID)
	Local $aResult = DllCall("ole32.dll", "long", "CLSIDFromString", "wstr", $sGUID, "struct*", $tGUID)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_GUIDFromStringEx
Func _WinAPI_StringFromGUID($tGUID)
	Local $aResult = DllCall("ole32.dll", "int", "StringFromGUID2", "struct*", $tGUID, "wstr", "", "int", 40)
	If @error Or Not $aResult[0] Then Return SetError(@error, @extended, "")
	Return SetExtended($aResult[0], $aResult[2])
EndFunc   ;==>_WinAPI_StringFromGUID
Func _WinAPI_WideCharToMultiByte($vUnicode, $iCodePage = 0, $bRetNoStruct = True, $bRetBinary = False)
	Local $sUnicodeType = "wstr"
	If Not IsString($vUnicode) Then $sUnicodeType = "struct*"
	Local $aResult = DllCall("kernel32.dll", "int", "WideCharToMultiByte", "uint", $iCodePage, "dword", 0, $sUnicodeType, $vUnicode, "int", -1, "ptr", 0, "int", 0, "ptr", 0, "ptr", 0)
	If @error Or Not $aResult[0] Then Return SetError(@error + 20, @extended, "")
	Local $tMultiByte = DllStructCreate((($bRetBinary) ? ("byte") : ("char")) & "[" & $aResult[0] & "]")
	$aResult = DllCall("kernel32.dll", "int", "WideCharToMultiByte", "uint", $iCodePage, "dword", 0, $sUnicodeType, $vUnicode, "int", -1, "struct*", $tMultiByte, "int", $aResult[0], "ptr", 0, "ptr", 0)
	If @error Or Not $aResult[0] Then Return SetError(@error + 10, @extended, "")
	If $bRetNoStruct Then Return DllStructGetData($tMultiByte, 1)
	Return $tMultiByte
EndFunc   ;==>_WinAPI_WideCharToMultiByte
Func _WinAPI_DeleteObject($hObject)
	Local $aResult = DllCall("gdi32.dll", "bool", "DeleteObject", "handle", $hObject)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_DeleteObject
Func _WinAPI_SelectObject($hDC, $hGDIObj)
	Local $aResult = DllCall("gdi32.dll", "handle", "SelectObject", "handle", $hDC, "handle", $hGDIObj)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_SelectObject
Func _WinAPI_BitBlt($hDestDC, $iXDest, $iYDest, $iWidth, $iHeight, $hSrcDC, $iXSrc, $iYSrc, $iROP)
	Local $aResult = DllCall("gdi32.dll", "bool", "BitBlt", "handle", $hDestDC, "int", $iXDest, "int", $iYDest, "int", $iWidth, "int", $iHeight, "handle", $hSrcDC, "int", $iXSrc, "int", $iYSrc, "dword", $iROP)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_BitBlt
Func _WinAPI_CombineRgn($hRgnDest, $hRgnSrc1, $hRgnSrc2, $iCombineMode)
	Local $aResult = DllCall("gdi32.dll", "int", "CombineRgn", "handle", $hRgnDest, "handle", $hRgnSrc1, "handle", $hRgnSrc2, "int", $iCombineMode)
	If @error Then Return SetError(@error, @extended, 0)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_CombineRgn
Func _WinAPI_CreateCompatibleBitmap($hDC, $iWidth, $iHeight)
	Local $aResult = DllCall("gdi32.dll", "handle", "CreateCompatibleBitmap", "handle", $hDC, "int", $iWidth, "int", $iHeight)
	If @error Then Return SetError(@error, @extended, 0)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_CreateCompatibleBitmap
Func _WinAPI_CreateRectRgn($iLeftRect, $iTopRect, $iRightRect, $iBottomRect)
	Local $aResult = DllCall("gdi32.dll", "handle", "CreateRectRgn", "int", $iLeftRect, "int", $iTopRect, "int", $iRightRect, "int", $iBottomRect)
	If @error Then Return SetError(@error, @extended, 0)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_CreateRectRgn
Func _WinAPI_SetWindowRgn($hWnd, $hRgn, $bRedraw = True)
	Local $aResult = DllCall("user32.dll", "int", "SetWindowRgn", "hwnd", $hWnd, "handle", $hRgn, "bool", $bRedraw)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_SetWindowRgn
Func _WinAPI_CreateCompatibleDC($hDC)
	Local $aResult = DllCall("gdi32.dll", "handle", "CreateCompatibleDC", "handle", $hDC)
	If @error Then Return SetError(@error, @extended, 0)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_CreateCompatibleDC
Func _WinAPI_DeleteDC($hDC)
	Local $aResult = DllCall("gdi32.dll", "bool", "DeleteDC", "handle", $hDC)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_DeleteDC
Func _WinAPI_DrawIcon($hDC, $iX, $iY, $hIcon)
	Local $aResult = DllCall("user32.dll", "bool", "DrawIcon", "handle", $hDC, "int", $iX, "int", $iY, "handle", $hIcon)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_DrawIcon
Func _WinAPI_GetDC($hWnd)
	Local $aResult = DllCall("user32.dll", "handle", "GetDC", "hwnd", $hWnd)
	If @error Then Return SetError(@error, @extended, 0)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_GetDC
Func _WinAPI_ReleaseDC($hWnd, $hDC)
	Local $aResult = DllCall("user32.dll", "int", "ReleaseDC", "hwnd", $hWnd, "handle", $hDC)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_ReleaseDC
Func _WinAPI_CopyIcon($hIcon)
	Local $aResult = DllCall("user32.dll", "handle", "CopyIcon", "handle", $hIcon)
	If @error Then Return SetError(@error, @extended, 0)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_CopyIcon
Func _WinAPI_DestroyIcon($hIcon)
	Local $aResult = DllCall("user32.dll", "bool", "DestroyIcon", "handle", $hIcon)
	If @error Then Return SetError(@error, @extended, False)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_DestroyIcon
Func _WinAPI_GetIconInfo($hIcon)
	Local $tInfo = DllStructCreate($tagICONINFO)
	Local $aRet = DllCall("user32.dll", "bool", "GetIconInfo", "handle", $hIcon, "struct*", $tInfo)
	If @error Or Not $aRet[0] Then Return SetError(@error + 10, @extended, 0)
	Local $aIcon[6]
	$aIcon[0] = True
	$aIcon[1] = DllStructGetData($tInfo, "Icon") <> 0
	$aIcon[2] = DllStructGetData($tInfo, "XHotSpot")
	$aIcon[3] = DllStructGetData($tInfo, "YHotSpot")
	$aIcon[4] = DllStructGetData($tInfo, "hMask")
	$aIcon[5] = DllStructGetData($tInfo, "hColor")
	Return $aIcon
EndFunc   ;==>_WinAPI_GetIconInfo
Func _GDIPlus_BitmapCloneArea($hBitmap, $nLeft, $nTop, $nWidth, $nHeight, $iFormat = 0x00021808)
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipCloneBitmapArea", "float", $nLeft, "float", $nTop, "float", $nWidth, "float", $nHeight, "int", $iFormat, "handle", $hBitmap, "handle*", 0)
	If @error Then Return SetError(@error, @extended, 0)
	If $aResult[0] Then Return SetError(10, $aResult[0], 0)
	Return $aResult[7]
EndFunc   ;==>_GDIPlus_BitmapCloneArea
Func _GDIPlus_BitmapCreateFromHBITMAP($hBitmap, $hPal = 0)
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipCreateBitmapFromHBITMAP", "handle", $hBitmap, "handle", $hPal, "handle*", 0)
	If @error Then Return SetError(@error, @extended, 0)
	If $aResult[0] Then Return SetError(10, $aResult[0], 0)
	Return $aResult[3]
EndFunc   ;==>_GDIPlus_BitmapCreateFromHBITMAP
Func _GDIPlus_Encoders()
	Local $iCount = _GDIPlus_EncodersGetCount()
	Local $iSize = _GDIPlus_EncodersGetSize()
	Local $tBuffer = DllStructCreate("byte[" & $iSize & "]")
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipGetImageEncoders", "uint", $iCount, "uint", $iSize, "struct*", $tBuffer)
	If @error Then Return SetError(@error, @extended, 0)
	If $aResult[0] Then Return SetError(10, $aResult[0], 0)
	Local $pBuffer = DllStructGetPtr($tBuffer)
	Local $tCodec, $aInfo[$iCount + 1][14]
	$aInfo[0][0] = $iCount
	For $iI = 1 To $iCount
		$tCodec = DllStructCreate($tagGDIPIMAGECODECINFO, $pBuffer)
		$aInfo[$iI][1] = _WinAPI_StringFromGUID(DllStructGetPtr($tCodec, "CLSID"))
		$aInfo[$iI][2] = _WinAPI_StringFromGUID(DllStructGetPtr($tCodec, "FormatID"))
		$aInfo[$iI][3] = _WinAPI_WideCharToMultiByte(DllStructGetData($tCodec, "CodecName"))
		$aInfo[$iI][4] = _WinAPI_WideCharToMultiByte(DllStructGetData($tCodec, "DllName"))
		$aInfo[$iI][5] = _WinAPI_WideCharToMultiByte(DllStructGetData($tCodec, "FormatDesc"))
		$aInfo[$iI][6] = _WinAPI_WideCharToMultiByte(DllStructGetData($tCodec, "FileExt"))
		$aInfo[$iI][7] = _WinAPI_WideCharToMultiByte(DllStructGetData($tCodec, "MimeType"))
		$aInfo[$iI][8] = DllStructGetData($tCodec, "Flags")
		$aInfo[$iI][9] = DllStructGetData($tCodec, "Version")
		$aInfo[$iI][10] = DllStructGetData($tCodec, "SigCount")
		$aInfo[$iI][11] = DllStructGetData($tCodec, "SigSize")
		$aInfo[$iI][12] = DllStructGetData($tCodec, "SigPattern")
		$aInfo[$iI][13] = DllStructGetData($tCodec, "SigMask")
		$pBuffer += DllStructGetSize($tCodec)
	Next
	Return $aInfo
EndFunc   ;==>_GDIPlus_Encoders
Func _GDIPlus_EncodersGetCLSID($sFileExtension)
	Local $aEncoders = _GDIPlus_Encoders()
	If @error Then Return SetError(@error, 0, "")
	For $iI = 1 To $aEncoders[0][0]
		If StringInStr($aEncoders[$iI][6], "*." & $sFileExtension) > 0 Then Return $aEncoders[$iI][1]
	Next
	Return SetError(-1, -1, "")
EndFunc   ;==>_GDIPlus_EncodersGetCLSID
Func _GDIPlus_EncodersGetCount()
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipGetImageEncodersSize", "uint*", 0, "uint*", 0)
	If @error Then Return SetError(@error, @extended, -1)
	If $aResult[0] Then Return SetError(10, $aResult[0], -1)
	Return $aResult[1]
EndFunc   ;==>_GDIPlus_EncodersGetCount
Func _GDIPlus_EncodersGetSize()
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipGetImageEncodersSize", "uint*", 0, "uint*", 0)
	If @error Then Return SetError(@error, @extended, -1)
	If $aResult[0] Then Return SetError(10, $aResult[0], -1)
	Return $aResult[2]
EndFunc   ;==>_GDIPlus_EncodersGetSize
Func _GDIPlus_ImageDispose($hImage)
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipDisposeImage", "handle", $hImage)
	If @error Then Return SetError(@error, @extended, False)
	If $aResult[0] Then Return SetError(10, $aResult[0], False)
	Return True
EndFunc   ;==>_GDIPlus_ImageDispose
Func _GDIPlus_ImageGetHeight($hImage)
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipGetImageHeight", "handle", $hImage, "uint*", 0)
	If @error Then Return SetError(@error, @extended, -1)
	If $aResult[0] Then Return SetError(10, $aResult[0], -1)
	Return $aResult[2]
EndFunc   ;==>_GDIPlus_ImageGetHeight
Func _GDIPlus_ImageGetWidth($hImage)
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipGetImageWidth", "handle", $hImage, "uint*", -1)
	If @error Then Return SetError(@error, @extended, -1)
	If $aResult[0] Then Return SetError(10, $aResult[0], -1)
	Return $aResult[2]
EndFunc   ;==>_GDIPlus_ImageGetWidth
Func _GDIPlus_ImageSaveToFileEx($hImage, $sFileName, $sEncoder, $tParams = 0)
	Local $tGUID = _WinAPI_GUIDFromString($sEncoder)
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdipSaveImageToFile", "handle", $hImage, "wstr", $sFileName, "struct*", $tGUID, "struct*", $tParams)
	If @error Then Return SetError(@error, @extended, False)
	If $aResult[0] Then Return SetError(10, $aResult[0], False)
	Return True
EndFunc   ;==>_GDIPlus_ImageSaveToFileEx
Func _GDIPlus_ParamAdd(ByRef $tParams, $sGUID, $iNbOfValues, $iType, $pValues)
	Local $iCount = DllStructGetData($tParams, "Count")
	Local $pGUID = DllStructGetPtr($tParams, "GUID") + ($iCount * _GDIPlus_ParamSize())
	Local $tParam = DllStructCreate($tagGDIPENCODERPARAM, $pGUID)
	_WinAPI_GUIDFromStringEx($sGUID, $pGUID)
	DllStructSetData($tParam, "Type", $iType)
	DllStructSetData($tParam, "NumberOfValues", $iNbOfValues)
	DllStructSetData($tParam, "Values", $pValues)
	DllStructSetData($tParams, "Count", $iCount + 1)
EndFunc   ;==>_GDIPlus_ParamAdd
Func _GDIPlus_ParamInit($iCount)
	Local $sStruct = $tagGDIPENCODERPARAMS
	For $i = 2 To $iCount
		$sStruct &= ";struct;byte[16];ulong;ulong;ptr;endstruct"
	Next
	Return DllStructCreate($sStruct)
EndFunc   ;==>_GDIPlus_ParamInit
Func _GDIPlus_ParamSize()
	Local $tParam = DllStructCreate($tagGDIPENCODERPARAM)
	Return DllStructGetSize($tParam)
EndFunc   ;==>_GDIPlus_ParamSize
Func _GDIPlus_Shutdown()
	If $__g_hGDIPDll = 0 Then Return SetError(-1, -1, False)
	$__g_iGDIPRef -= 1
	If $__g_iGDIPRef = 0 Then
		DllCall($__g_hGDIPDll, "none", "GdiplusShutdown", "ulong_ptr", $__g_iGDIPToken)
		DllClose($__g_hGDIPDll)
		$__g_hGDIPDll = 0
	EndIf
	Return True
EndFunc   ;==>_GDIPlus_Shutdown
Func _GDIPlus_Startup($sGDIPDLL = Default, $bRetDllHandle = False)
	$__g_iGDIPRef += 1
	If $__g_iGDIPRef > 1 Then Return True
	If $sGDIPDLL = Default Then $sGDIPDLL = "gdiplus.dll"
	$__g_hGDIPDll = DllOpen($sGDIPDLL)
	If $__g_hGDIPDll = -1 Then
		$__g_iGDIPRef = 0
		Return SetError(1, 2, False)
	EndIf
	Local $sVer = FileGetVersion($sGDIPDLL)
	$sVer = StringSplit($sVer, ".")
	If $sVer[1] > 5 Then $__g_bGDIP_V1_0 = False
	Local $tInput = DllStructCreate($tagGDIPSTARTUPINPUT)
	Local $tToken = DllStructCreate("ulong_ptr Data")
	DllStructSetData($tInput, "Version", 1)
	Local $aResult = DllCall($__g_hGDIPDll, "int", "GdiplusStartup", "struct*", $tToken, "struct*", $tInput, "ptr", 0)
	If @error Then Return SetError(@error, @extended, False)
	If $aResult[0] Then Return SetError(10, $aResult[0], False)
	$__g_iGDIPToken = DllStructGetData($tToken, "Data")
	If $bRetDllHandle Then Return $__g_hGDIPDll
	Return SetExtended($sVer[1], True)
EndFunc   ;==>_GDIPlus_Startup
Func __GDIPlus_ExtractFileExt($sFileName, $bNoDot = True)
	Local $iIndex = __GDIPlus_LastDelimiter(".\:", $sFileName)
	If ($iIndex > 0) And (StringMid($sFileName, $iIndex, 1) = '.') Then
		If $bNoDot Then
			Return StringMid($sFileName, $iIndex + 1)
		Else
			Return StringMid($sFileName, $iIndex)
		EndIf
	Else
		Return ""
	EndIf
EndFunc   ;==>__GDIPlus_ExtractFileExt
Func __GDIPlus_LastDelimiter($sDelimiters, $sString)
	Local $sDelimiter, $iN
	For $iI = 1 To StringLen($sDelimiters)
		$sDelimiter = StringMid($sDelimiters, $iI, 1)
		$iN = StringInStr($sString, $sDelimiter, $STR_NOCASESENSEBASIC, -1)
		If $iN > 0 Then Return $iN
	Next
EndFunc   ;==>__GDIPlus_LastDelimiter
Func _WinAPI_GetDesktopWindow()
	Local $aResult = DllCall("user32.dll", "hwnd", "GetDesktopWindow")
	If @error Then Return SetError(@error, @extended, 0)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_GetDesktopWindow
Func _WinAPI_GetSystemMetrics($iIndex)
	Local $aResult = DllCall("user32.dll", "int", "GetSystemMetrics", "int", $iIndex)
	If @error Then Return SetError(@error, @extended, 0)
	Return $aResult[0]
EndFunc   ;==>_WinAPI_GetSystemMetrics
Func _ScreenCapture_Capture($sFileName = "", $iLeft = 0, $iTop = 0, $iRight = -1, $iBottom = -1, $bCursor = True)
	Local $bRet = False
	If $iRight = -1 Then $iRight = _WinAPI_GetSystemMetrics($__SCREENCAPTURECONSTANT_SM_CXSCREEN) - 1
	If $iBottom = -1 Then $iBottom = _WinAPI_GetSystemMetrics($__SCREENCAPTURECONSTANT_SM_CYSCREEN) - 1
	If $iRight < $iLeft Then Return SetError(-1, 0, $bRet)
	If $iBottom < $iTop Then Return SetError(-2, 0, $bRet)
	Local $iW = ($iRight - $iLeft) + 1
	Local $iH = ($iBottom - $iTop) + 1
	Local $hWnd = _WinAPI_GetDesktopWindow()
	Local $hDDC = _WinAPI_GetDC($hWnd)
	Local $hCDC = _WinAPI_CreateCompatibleDC($hDDC)
	Local $hBMP = _WinAPI_CreateCompatibleBitmap($hDDC, $iW, $iH)
	_WinAPI_SelectObject($hCDC, $hBMP)
	_WinAPI_BitBlt($hCDC, 0, 0, $iW, $iH, $hDDC, $iLeft, $iTop, $__SCREENCAPTURECONSTANT_SRCCOPY)
	If $bCursor Then
		Local $aCursor = _WinAPI_GetCursorInfo()
		If Not @error And $aCursor[1] Then
			$bCursor = True
			Local $hIcon = _WinAPI_CopyIcon($aCursor[2])
			Local $aIcon = _WinAPI_GetIconInfo($hIcon)
			If Not @error Then
				_WinAPI_DeleteObject($aIcon[4])
				If $aIcon[5] <> 0 Then _WinAPI_DeleteObject($aIcon[5])
				_WinAPI_DrawIcon($hCDC, $aCursor[3] - $aIcon[2] - $iLeft, $aCursor[4] - $aIcon[3] - $iTop, $hIcon)
			EndIf
			_WinAPI_DestroyIcon($hIcon)
		EndIf
	EndIf
	_WinAPI_ReleaseDC($hWnd, $hDDC)
	_WinAPI_DeleteDC($hCDC)
	If $sFileName = "" Then Return $hBMP
	$bRet = _ScreenCapture_SaveImage($sFileName, $hBMP, True)
	Return SetError(@error, @extended, $bRet)
EndFunc   ;==>_ScreenCapture_Capture
Func _ScreenCapture_SaveImage($sFileName, $hBitmap, $bFreeBmp = True)
	_GDIPlus_Startup()
	If @error Then Return SetError(-1, -1, False)
	Local $sExt = StringUpper(__GDIPlus_ExtractFileExt($sFileName))
	Local $sCLSID = _GDIPlus_EncodersGetCLSID($sExt)
	If $sCLSID = "" Then Return SetError(-2, -2, False)
	Local $hImage = _GDIPlus_BitmapCreateFromHBITMAP($hBitmap)
	If @error Then Return SetError(-3, -3, False)
	Local $tData, $tParams
	Switch $sExt
		Case "BMP"
			Local $iX = _GDIPlus_ImageGetWidth($hImage)
			Local $iY = _GDIPlus_ImageGetHeight($hImage)
			Local $hClone = _GDIPlus_BitmapCloneArea($hImage, 0, 0, $iX, $iY, $__g_iBMPFormat)
			_GDIPlus_ImageDispose($hImage)
			$hImage = $hClone
		Case "JPG", "JPEG"
			$tParams = _GDIPlus_ParamInit(1)
			$tData = DllStructCreate("int Quality")
			DllStructSetData($tData, "Quality", $__g_iJPGQuality)
			_GDIPlus_ParamAdd($tParams, $GDIP_EPGQUALITY, 1, $GDIP_EPTLONG, DllStructGetPtr($tData))
		Case "TIF", "TIFF"
			$tParams = _GDIPlus_ParamInit(2)
			$tData = DllStructCreate("int ColorDepth;int Compression")
			DllStructSetData($tData, "ColorDepth", $__g_iTIFColorDepth)
			DllStructSetData($tData, "Compression", $__g_iTIFCompression)
			_GDIPlus_ParamAdd($tParams, $GDIP_EPGCOLORDEPTH, 1, $GDIP_EPTLONG, DllStructGetPtr($tData, "ColorDepth"))
			_GDIPlus_ParamAdd($tParams, $GDIP_EPGCOMPRESSION, 1, $GDIP_EPTLONG, DllStructGetPtr($tData, "Compression"))
	EndSwitch
	Local $pParams = 0
	If IsDllStruct($tParams) Then $pParams = $tParams
	Local $bRet = _GDIPlus_ImageSaveToFileEx($hImage, $sFileName, $sCLSID, $pParams)
	_GDIPlus_ImageDispose($hImage)
	If $bFreeBmp Then _WinAPI_DeleteObject($hBitmap)
	_GDIPlus_Shutdown()
	Return SetError($bRet = False, 0, $bRet)
EndFunc   ;==>_ScreenCapture_SaveImage
Func _ScreenCapture_SetBMPFormat($iFormat)
	Switch $iFormat
		Case 0
			$__g_iBMPFormat = $GDIP_PXF16RGB555
		Case 1
			$__g_iBMPFormat = $GDIP_PXF16RGB565
		Case 2
			$__g_iBMPFormat = $GDIP_PXF24RGB
		Case 3
			$__g_iBMPFormat = $GDIP_PXF32RGB
		Case 4
			$__g_iBMPFormat = $GDIP_PXF32ARGB
		Case Else
			$__g_iBMPFormat = $GDIP_PXF24RGB
	EndSwitch
EndFunc   ;==>_ScreenCapture_SetBMPFormat

#ce
; * -----:| Dao Van Trong - TRONG.LIVE



Func _ImageSearch_Create_BMP($imgFilename = "Search.bmp")
	Local $iX1, $iY1, $iX2, $iY2, $aPos, $sMsg
	Local $aMouse_Pos, $hMask, $hMaster_Mask, $iTemp
	Local $UserDLL = DllOpen("user32.dll")
	$hCross_GUI = GUICreate("Test", @DesktopWidth, @DesktopHeight - 20, 0, 0, $WS_POPUP, $WS_EX_TOPMOST)
	WinSetTrans($hCross_GUI, "", 8)
	GUISetState(@SW_SHOW, $hCross_GUI)
	GUISetCursor(3, 1, $hCross_GUI)
	Global $hRectangle_GUI = GUICreate("", @DesktopWidth, @DesktopHeight, 0, 0, $WS_POPUP, $WS_EX_TOOLWINDOW + $WS_EX_TOPMOST)
	GUISetBkColor(0x000000)
	While Not _IsPressed("01", $UserDLL)
		Sleep(10)
	WEnd
	$aMouse_Pos = MouseGetPos()
	$iX1 = $aMouse_Pos[0]
	$iY1 = $aMouse_Pos[1]
	While _IsPressed("01", $UserDLL)
		$aMouse_Pos = MouseGetPos()
		$hMaster_Mask = _WinAPI_CreateRectRgn(0, 0, 0, 0)
		$hMask = _WinAPI_CreateRectRgn($iX1, $aMouse_Pos[1], $aMouse_Pos[0], $aMouse_Pos[1] + 1)
		_WinAPI_CombineRgn($hMaster_Mask, $hMask, $hMaster_Mask, 2)
		_WinAPI_DeleteObject($hMask)
		$hMask = _WinAPI_CreateRectRgn($iX1, $iY1, $iX1 + 1, $aMouse_Pos[1])
		_WinAPI_CombineRgn($hMaster_Mask, $hMask, $hMaster_Mask, 2)
		_WinAPI_DeleteObject($hMask)
		$hMask = _WinAPI_CreateRectRgn($iX1 + 1, $iY1 + 1, $aMouse_Pos[0], $iY1)
		_WinAPI_CombineRgn($hMaster_Mask, $hMask, $hMaster_Mask, 2)
		_WinAPI_DeleteObject($hMask)
		$hMask = _WinAPI_CreateRectRgn($aMouse_Pos[0], $iY1, $aMouse_Pos[0] + 1, $aMouse_Pos[1])
		_WinAPI_CombineRgn($hMaster_Mask, $hMask, $hMaster_Mask, 2)
		_WinAPI_DeleteObject($hMask)
		_WinAPI_SetWindowRgn($hRectangle_GUI, $hMaster_Mask)
		If WinGetState($hRectangle_GUI) < 15 Then GUISetState()
		Sleep(10)
	WEnd
	$iX2 = $aMouse_Pos[0]
	$iY2 = $aMouse_Pos[1]
	If $iX2 < $iX1 Then
		$iTemp = $iX1
		$iX1 = $iX2
		$iX2 = $iTemp
	EndIf
	If $iY2 < $iY1 Then
		$iTemp = $iY1
		$iY1 = $iY2
		$iY2 = $iTemp
	EndIf
	GUIDelete($hRectangle_GUI)
	GUIDelete($hCross_GUI)
	DllClose($UserDLL)
	;ConsoleWrite("-- ScreenCapture   : X1=" & $iX1 & " Y1=" & $iY1 & " X2=" & $iX2 & " Y2=" & $iY2 & @CRLF)
	_ScreenCapture_SetBMPFormat(4)
	_ScreenCapture_Capture($imgFilename, $iX1, $iY1, $iX2, $iY2, False)
EndFunc   ;==>_ImageSearch_Create_BMP


;~ _ImageSearch_Create_BMP(@ScriptDir & "\Search.bmp")
; * -----:| Dao Van Trong - TRONG.LIVE


