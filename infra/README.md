# Deploying Beam to Azure Container Apps

```
browser ──https──▶ beam-web (Next.js, Container App) ──/api/* → http://beam-api──┐
   │                                                                             ▼
   └──https (overlays/control pages, SignalR)──────────────▶ beam-api (ASP.NET, Container App, 1 replica)
                                                                 │            │
                                              Azure Postgres Flexible (B1ms)  Supabase Storage (S3)
```

| Resource | What it is | Cost notes |
|---|---|---|
| `beam-api` | Container App, 0.5 vCPU / 1 GiB, **exactly 1 replica** | Always on. Live state and SignalR groups are in-process, so it must not scale out or to zero |
| `beam-web` | Container App, 0.5 vCPU / 1 GiB, 0–2 replicas | `WEB_MIN_REPLICAS=0` scales to zero when idle |
| Postgres Flexible Server | Burstable `Standard_B1ms`, 32 GB, PG 16 | Covered by the Azure free account's 12-month allowance (check current terms) |
| Container Registry | Basic | Small monthly charge, comes out of trial credit |
| Log Analytics | 30-day retention | Pay per GB ingested; tiny at this scale |

Files stay in **Supabase Storage**. Azure Blob Storage doesn't speak the S3 protocol the API uses.
Data-protection keys (auth cookies, encrypted Twitch/YouTube tokens) live in the
`DataProtectionKeys` Postgres table, so the API needs no disk.

## Prerequisites

1. **Azure CLI**, then `az login`. Docker is not needed locally. Images are built in Azure with `az acr build`.
2. **Supabase project.** In Storage, create a **private** bucket named `beam-assets`, then under
   Storage → S3 Connection enable the S3 protocol and create an access key. Note the endpoint
   (`https://<project-ref>.supabase.co/storage/v1/s3`) and the region shown there.
3. **SixLabors license key.** Release builds of the API (ImageSharp) fail without one.

## Deploy

```powershell
Copy-Item infra/azure.env.example infra/azure.env   # then fill it in (git-ignored)
./infra/deploy.ps1
```

The first run takes about 10–15 minutes, mostly Postgres provisioning. It prints the web and API URLs at
the end. On first start the API applies EF migrations automatically (`Startup__ApplyMigrations=true`),
which is safe because there is only ever one replica.

**To redeploy after code changes,** run `./infra/deploy.ps1` again. It builds new images with a fresh tag and
rolls out a new revision. Expect a brief disconnect for live overlays while the API revision swaps.

If Postgres fails with a SKU or region error, set `POSTGRES_LOCATION` to another region (e.g. `northeurope`,
`eastus`) and rerun.

## Check it

- `https://<api>/api/health/ready` should return `Healthy`.
- Sign up in the web app, upload an asset (exercises Supabase Storage), and open an overlay URL.
- Logs: Azure Portal → `beam-api` → Log stream, or
  `az containerapp logs show -g <rg> -n beam-api --follow`.

## Custom domain (optional)

Container Apps → `beam-web` → Custom domains adds a free managed certificate. Do the same for
`beam-api`, then rerun the deploy. `BEAM_API_PUBLIC_URL` is derived from the API's default FQDN, so
change it in `main.bicep` to your domain if overlays should use it.

## Notes and limits

- **Rate limiting / client IPs.** The API trusts `X-Forwarded-*` only from private address ranges
  (`trustedProxyCidrs` in `main.bicep`), which is where Container Apps ingress connects from.
- **Postgres firewall.** It allows Azure-hosted callers (`0.0.0.0` rule) because Consumption Container
  Apps have no fixed outbound IP. Access still requires TLS and the admin password. To connect from your
  machine (e.g. `psql`), add your IP under the server's Networking blade.
- **Back up Postgres.** It now also holds the data-protection keys. Losing that table logs everyone out
  and makes stored OAuth tokens unreadable.
- **After the trial,** the always-on API replica and the registry are billed. Delete everything with
  `az group delete -n <resource group>`.
