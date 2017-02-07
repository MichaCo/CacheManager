exists=$(whereis memcached | grep -c "bin/memcached")
if [[ $exists == 0 ]]; then
	echo "installing memcached..."
	sudo apt-get install memcached -y
fi

status=$(service memcached status | grep -c "is running")
if [[ $status == 0 ]]; then
  sudo service memcached start
else
  echo "Memcached already running!"
fi

service memcached status