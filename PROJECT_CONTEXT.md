# TalentAI deployment context

Architecture: GitHub Actions CI on `talentai-host-runner` restores and builds .NET 10, audits dependencies, builds and scans a Docker image, and publishes a unique full commit SHA tag to Docker Hub repository `fedimriri/talentai`. Manual CD on the same host runner validates the `talentai` context, loads the published image, upgrades Helm and checks HTTP health. CI image publication and GitHub CD execution require verification after the workflows are reviewed and pushed.

Minikube runs TalentAI behind NGINX Ingress and a ClusterIP Service; the app connects to the existing MongoDB Atlas cluster. Ansible validates the local host. Terraform owns the `talentai` namespace. Helm owns the Deployment, Service, ConfigMap, Ingress and HPA. Kubernetes Secret `talentai-runtime` comes from private credentials outside Git. Prometheus, blackbox exporter and Grafana run in `monitoring`.

The HPA has demonstrated 1-to-3 replica scale-out under load and later returned to one replica. Sessions, ASP.NET Data Protection keys and CV files remain local to pods, so functional multi-replica use and high availability are not established. A historical email password in public Git history matches the current private value; the user must rotate it externally. Never print it, reuse it in committed files or rewrite history without approval.

Evidence lives in `results/` and `labs/talentai-deployment/results/`; genuine screenshots are in `labs/talentai-deployment/screenshots/`. The final French compte-rendu requires DOCX and PDF with verified outcomes and explicit gaps.
