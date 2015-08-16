#!/bin/sh

# Collect the verb (start/stop) from the command
# line.

VERB="$1"

shopt -s nocasematch

if [ $VERB == "start" ];
then
  net start MySQL
  exit
fi

if [ $VERB == "stop" ];
then
  net stop MySQL
  exit
fi

echo 
echo "Usage: "
echo "    mysql.sh start - To start the MySQL service "
echo "    mysql.sh stop  - To stop the MySQL service "