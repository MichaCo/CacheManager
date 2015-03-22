@ECHO OFF
cmd /C
cmd /C start  memcached_x64 -v -p 11211 -u 0  -t 20 -L -r -m800
cmd /C start  memcached_x64 -v -p 11212 -u 0  -t 20 -L -r -m800
cmd /C start  memcached_x64 -v -p 11213 -u 0  -t 20 -L -r -m400
cmd /C start  memcached_x64 -v -p 11214 -u 0  -t 20 -L -r -m400
@CHOICE /C JN /M "Kill memcached processes?"

IF ERRORLEVEL 2 GOTO END
IF ERRORLEVEL 1 GOTO KILL
GOTO END

:KILL
ECHO killing memcaches
taskkill /F /IM memcached_x64.exe

:END
@PAUSE