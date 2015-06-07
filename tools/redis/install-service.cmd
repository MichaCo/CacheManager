cd /d %~dp0
CALL install.cmd
@packages\Redis-64\redis-server.exe --service-install %~dp0\master.conf
@packages\Redis-64\redis-server.exe --service-start