#!/bin/sh
printf '\033c\033]0;%s\a' GammaProject
base_path="$(dirname "$(realpath "$0")")"
"$base_path/PotFighterDeluxe05072026.x86_64" "$@"
