#!/usr/bin/env sh

set -eu

CMDLINE=/boot/firmware/cmdline.txt
BACKUP=${CMDLINE}.transferlab-backup

if [ ! -f "$CMDLINE" ]; then
    echo "Could not find $CMDLINE." >&2
    exit 1
fi

if [ ! -f "$BACKUP" ]; then
    sudo cp --preserve=mode,ownership,timestamps "$CMDLINE" "$BACKUP"
fi

updated="$(
    sed -E 's/(^|[[:space:]])cgroup_disable=memory([[:space:]]|$)/ /g' "$CMDLINE" |
        tr -s '[:space:]' ' ' |
        sed -E 's/^ //; s/ $//'
)"

case " $updated " in
    *" cgroup_enable=memory "*) ;;
    *) updated="$updated cgroup_enable=memory" ;;
esac

case " $updated " in
    *" cgroup_memory=1 "*) ;;
    *) updated="$updated cgroup_memory=1" ;;
esac

case "$updated" in
    *"root="*) ;;
    *)
        echo "Refusing to write a boot line without a root filesystem argument." >&2
        exit 1
        ;;
esac

printf '%s\n' "$updated" | sudo tee "$CMDLINE" >/dev/null

echo "Updated $CMDLINE. The original is saved at $BACKUP."
echo "Reboot the Pi to activate the memory cgroup controller."
