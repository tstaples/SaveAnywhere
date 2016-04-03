@ECHO OFF
setlocal

set protoc="C:\Users\Tyler\Documents\Programming\C#\protoc\protoc.exe"

%protoc% --proto_path=ProtoFiles^
	--csharp_out=GeneratedProtoFiles^
	ProtoFiles/Common.proto^
	ProtoFiles/Game.proto