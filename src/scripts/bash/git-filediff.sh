#!/bin/sh

FILE="$1"
BRANCH1="$2"
BRANCH2="$3"

# Force the file to be specified.

until [ ${#FILE} -gt 0 ] 
do
  echo -n "Enter the name of a file to diff: "
  read FILE
done

# Force at least one branch to be specified.

until [ ${#BRANCH1} -gt 0 ] 
do
  echo -n "Enter the name of a branch for the file: "
  read BRANCH1
done

# If a second branch was not provided, assume the master branch.

if [ ${#BRANCH2} -lt 1 ] 
then
  BRANCH2="master"
fi

# Diff the file.

git diff $BRANCH1:$FILE $BRANCH2:$FILE