#!/bin/sh

# Collect the project name and directory target from the 
# command line or user input.

NAME="$1"
TARGET="$2"

until [ ${#NAME} -gt 0 ] 
do
  echo -n "Enter project name: "
  read NAME
done

if [ ${#TARGET} -lt 1 ];
then
  echo -n "Enter the target directory (leave blank for current): "
  read TARGET
fi

# If the target directory is blank, use the current
# directory.

if [ ${#TARGET} -lt 1 ];
then
  TARGET=$PWD
fi

# Create the project path.  If the project name begins
# with a slash, do not use the separator.  If the target
# directory ends with a slash, drop it.

SEP="/"

if [ ${NAME:0:1} = '/' ] 
then
  SEP=""
fi

TARGET=${TARGET%/}
PROJPATH="$TARGET$SEP$NAME"

# Create the project structure

echo "Creating project structure for [$NAME] at: $PROJPATH"

CURRENTDIR=$PWD

if [ ! -d "$PROJPATH" ];
then
  mkdir $PROJPATH
fi

cd $PROJPATH

if [ ! -d "build" ];
then
  mkdir build
fi 

if [ ! -d "build/tools" ];
then
  mkdir build/tools
fi

if [ ! -d "lib" ];
then
  mkdir lib
fi

if [ ! -d "src" ];
then
  mkdir src
fi

if [ ! -d "src/sandbox" ];
then
  mkdir src/sandbox
fi

if [ ! -d "documents" ];
then
  mkdir documents
fi

if [ ! -d "sql-scripts" ];
then
  mkdir sql-scripts
fi

# Add the empty directory .gitignore placeholder to the empty directories, 
# so that they can be staged to git.  These should be removed when actual 
# content is added.

cp ~/documents/git/.empty-dir-ignore ./build/.gitignore
cp ~/documents/git/.empty-dir-ignore ./build/tools/.gitignore
cp ~/documents/git/.empty-dir-ignore ./lib/.gitignore
cp ~/documents/git/.empty-dir-ignore ./src/sandbox/.gitignore
cp ~/documents/git/.empty-dir-ignore ./documents/.gitignore
cp ~/documents/git/.empty-dir-ignore ./sql-scripts/.gitignore

# Copy the master .gitignore to the root and
# create a simple markdown readme.

cp ~/documents/git/.gitignore ./.gitignore
echo "#$NAME" >> ReadMe.md

cd $CURRENTDIR
echo "Project structure completed."