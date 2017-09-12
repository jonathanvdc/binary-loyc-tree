source-files = $(shell find . -name '*.cs')
proj-files = $(shell find . -name '*.csproj')
sln-file = Loyc.Binary.sln
loyc-binary-dll = Loyc.Binary/bin/Release/Loyc.Binary.dll
loyc-binary-tests-exe = Loyc.Binary.Tests/bin/Release/Loyc.Binary.Tests.exe

all: $(loyc-binary-dll)

$(loyc-binary-dll): $(sln-file) $(proj-files) $(source-files)
	msbuild /p:Configuration=Release /v:quiet /nologo $(sln-file)
	touch $(loyc-binary-dll)

test: $(loyc-binary-dll)
	mono $(loyc-binary-tests-exe)
