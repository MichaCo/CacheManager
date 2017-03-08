@echo off
cd /d %~dp0
CALL install.cmd
pushd Cluster\7000
@start ..\..\packages\Redis-64\tools\redis-server.exe redis.conf 
popd
pushd Cluster\7001
@start ..\..\packages\Redis-64\tools\redis-server.exe redis.conf 
popd
pushd Cluster\7002
@start ..\..\packages\Redis-64\tools\redis-server.exe redis.conf
popd
pushd Cluster\7003
@start ..\..\packages\Redis-64\tools\redis-server.exe redis.conf 
popd
pushd Cluster\7004
@start ..\..\packages\Redis-64\tools\redis-server.exe redis.conf 
popd
pushd Cluster\7005
@start ..\..\packages\Redis-64\tools\redis-server.exe redis.conf 
popd