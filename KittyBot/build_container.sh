#!/bin/zsh

d=$(date +%Y-%m-%d)
docker build -t docker.io/kisakitty/kitty-bot -t docker.io/kisakitty/kitty-bot:$d -f Containerfile . 
