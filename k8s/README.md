# TransferLab on peepi

This deployment runs TransferLab's four ARM64 application images on the remote
K3s cluster hosted by the Raspberry Pi.

```text
Browser -> Pi Traefik -> Web -> API ----+
                         Worker --------+-> SQL Server on the Mac
                         Migration Job -+
```

SQL Server remains on the Mac because Microsoft's SQL Server Linux image is
x86-64 only. Port 1433 is used only over the private LAN and must not be exposed
through the router.

## One-time cluster setup

The checked-in peepi configuration places K3s state and local volumes on the
NVMe drive mounted at `/mnt/nvme`:

```bash
scp -r k8s/cluster/peepi peepi-gemma:/tmp/transferlab-k3s-bootstrap
ssh peepi-gemma /tmp/transferlab-k3s-bootstrap/install.sh
```

Configure the Mac's `kubectl` client:

```bash
./k8s/scripts/configure-kubectl-peepi.sh
```

## Deploy

Keep SQL Server running on the Mac and stop the duplicate Compose application
processes:

```bash
docker compose --env-file .env -f docker/docker-compose.yml up -d sqlserver
docker compose --env-file .env -f docker/docker-compose.yml stop api worker web
```

Build ARM64 images and load them into the Pi's K3s image store:

```bash
./k8s/scripts/load-images-peepi.sh
```

Create the Kubernetes secret, run migrations, and deploy the three long-running
services:

```bash
./k8s/scripts/deploy-peepi.sh
```

The deployment script discovers the Mac's Wi-Fi address. Override it when
necessary:

```bash
TRANSFERLAB_SQL_HOST=192.168.1.83 ./k8s/scripts/deploy-peepi.sh
```

Open `http://192.168.1.179`.

Run the end-to-end smoke test:

```bash
./k8s/scripts/smoke-test-peepi.sh
```

The test creates two accounts, completes a transfer through the remote Worker,
checks both final balances, and verifies the idempotency replay.

## Kubernetes dashboard

The deployment also installs Headlamp, the Kubernetes SIG UI dashboard. Open:

```text
http://192.168.1.179:30446
```

Generate an eight-hour login token and paste it into the login screen:

```bash
./k8s/scripts/headlamp-token.sh
```

The token has cluster-administrator access. Do not share it or expose port
`30446` through the internet router. Generate a new token when the old one
expires.

## Inspect

```bash
kubectl --context peepi get all,ingress --namespace transferlab
kubectl --context peepi logs --namespace transferlab deployment/transferlab-api
kubectl --context peepi logs --namespace transferlab deployment/transferlab-worker
```

The database password is read from the repository's ignored `.env` file and is
stored as a Kubernetes Secret. It is never written to a checked-in manifest.

## Off-LAN testing

The K3s API certificate includes the Pi's existing WireGuard address,
`10.0.0.1`. When the Mac is on another internet connection, connect WireGuard
and refresh the kubeconfig with:

```bash
PEEPI_ADDRESS=10.0.0.1 ./k8s/scripts/configure-kubectl-peepi.sh
```

TransferLab and Headlamp are then reachable through the same private tunnel.
The database connection must also use the Mac's WireGuard address:

```bash
TRANSFERLAB_SQL_HOST=MAC_WIREGUARD_ADDRESS ./k8s/scripts/deploy-peepi.sh
```

Do not forward Kubernetes API port `6443`, Headlamp port `30446`, or SQL Server
port `1433` directly through either internet router.
