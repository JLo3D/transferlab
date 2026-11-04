#!/usr/bin/env sh

set -eu

K3S_VERSION="${K3S_VERSION:-v1.37.1+k3s1}"
SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

if ! mountpoint -q /mnt/nvme; then
    echo "Expected the NVMe drive to be mounted at /mnt/nvme." >&2
    exit 1
fi

if ! grep -qw memory /sys/fs/cgroup/cgroup.controllers; then
    echo "The memory cgroup controller is not enabled. Run enable-memory-cgroup.sh and reboot first." >&2
    exit 1
fi

sudo install -d -m 0755 /etc/rancher/k3s
sudo install -m 0600 "$SCRIPT_DIR/config.yaml" /etc/rancher/k3s/config.yaml

curl --fail --silent --show-error --location \
    --proto '=https' --tlsv1.2 \
    https://get.k3s.io \
    --output /tmp/install-k3s.sh

sudo env INSTALL_K3S_VERSION="$K3S_VERSION" sh /tmp/install-k3s.sh
