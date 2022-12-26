#!/bin/bash

# Get the directory of this script
MY_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

# Downloads and extracts prepackaged archive of native libraries ("moonlibs")
function getLibs() {
  # Downloading
  echo "Downloading latest moonlibs..."
  curl https://moonside.games/files/moonlibs.tar.bz2 >"$MY_DIR/moonlibs.tar.bz2"
  if [ $? -eq 0 ]; then
    echo "Finished downloading!"
  else
    echo >&2 "ERROR: Unable to download successfully."
    exit 1
  fi
  # Decompressing
  echo "Decompressing moonlibs..."
  mkdir -p "$MY_DIR"/moonlibs
  tar -xvC "$MY_DIR"/moonlibs -f "$MY_DIR"/moonlibs.tar.bz2
  if [ $? -eq 0 ]; then
    echo "Finished decompressing!"
    echo ""
    rm "$MY_DIR"/moonlibs.tar.bz2
  else
    echo >&2 "ERROR: Unable to decompress successfully."
    exit 1
  fi
}

rm -rf moonlibs
getLibs
