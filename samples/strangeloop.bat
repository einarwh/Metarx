@echo off

set ROSIEURL=http://localhost:50935/programs/0

:strange

echo Posting Rosie to %ROSIEURL%

curl -s -XPOST %ROSIEURL% --data-binary @rosie.cs

for /f "tokens=1-2" %%i in ('curl -i -s %ROSIEURL%') do if "%%i"=="Location:" set ROSIEURL=%%j

echo Rosie now lives at %ROSIEURL%

timeout /T 3 /NOBREAK

goto strange

