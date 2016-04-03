@ECHO OFF
setlocal

set protoc="C:\Users\Tyler\Documents\Programming\C#\protoc\protoc.exe"

%protoc% --proto_path=SaveAnywhere/ProtoFiles^
	--csharp_out=SaveAnywhere/GeneratedProtoFiles^
	SaveAnywhere/ProtoFiles/Common.proto^
	SaveAnywhere/ProtoFiles/Game.proto