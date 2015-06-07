cd /d %~dp0
CALL install.cmd
@packages\Redis-64\redis-server.exe master.conf
