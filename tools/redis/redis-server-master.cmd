cd /d %~dp0
CALL install.cmd
@packages\Redis-64\tools\redis-server.exe master.conf
