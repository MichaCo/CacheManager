sudo service memcached stop

status=$(service memcached status | grep -c "is running")
if [[ $status == 0 ]]; then
  echo "Service stopped"
fi