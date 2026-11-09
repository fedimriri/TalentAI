# Candidate image upload permission check - 9 November 2026

The published image `fedimriri/talentai:20261108231056` runs as UID 1654. Read-only inspection found `/app/wwwroot` owned by root and no `/app/wwwroot/uploads`; creating the upload directory as the app user failed with `Permission denied`. This is a container reliability defect for CV uploads, independent of the known cross-pod storage limitation.

The candidate Dockerfile now creates `/app/wwwroot/uploads` and assigns that directory to `APP_UID` before switching to the non-root user. `docker build --progress=plain -t talentai:staged-review .` exited 0. In a disposable container, UID 1654 successfully wrote a test file in `/app/wwwroot/uploads`; `.env` and `envirementfile.txt` were absent. `trivy image --scanners vuln --severity HIGH,CRITICAL --exit-code 1 talentai:staged-review` exited 0 with zero reported findings in the scanned Ubuntu and .NET targets.

No running Kubernetes or Docker Compose application was changed. The published image remains the earlier version until a reviewed CI publication and CD deployment. Writable local uploads do not make CV files shared across replicas or persistent across pod replacement.
