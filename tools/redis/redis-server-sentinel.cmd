cd /d %~dp0
CALL install.cmd
cmd /C start packages\Redis-64\tools\redis-server.exe sentinel1.conf --sentinel
cmd /C start packages\Redis-64\tools\redis-server.exe sentinel2.conf --sentinel
cmd /C start packages\Redis-64\tools\redis-server.exe sentinel3.conf --sentinel
