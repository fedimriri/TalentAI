# TalentAI DevOps work

Objective: deploy the existing .NET 10 TalentAI app through Docker, Minikube, Terraform, Helm, CI/CD and monitoring. MongoDB Atlas is the existing external database; never create or alter Atlas resources. Preserve all existing code, secrets, volumes and uncommitted changes. Never prune Docker or delete Minikube profiles. Terraform owns only the `talentai` namespace; Helm owns application resources.

The current Docker Hub repository is `fedimriri/talentai`. The host GitHub runner is `talentai-host-runner`; do not recreate it. CI should tag a scanned image with the full commit SHA. CD is manual and must verify the `talentai` context, Helm rollout, and HTTP health. Never push, trigger deployment, or alter infrastructure without the user's approval.

The HPA has demonstrated a real 1-to-3 replica scale-out and has returned to one replica. This is a capacity demonstration, not proof that user sessions or CV files work across replicas. Session state, Data Protection keys, and CV storage are local to pods. Keep these limitations explicit in all claims and reports; do not claim high availability from one Minikube node.

Treat the historical email password match in public Git history as compromised. The user will rotate it outside this repository. Never print or copy credential values, rewrite Git history, or commit private backups. Verify each step, record commands, real outputs, failures and screenshots in `results/` and `labs/talentai-deployment/`. Prepare the French compte-rendu in DOCX and PDF only from verified evidence.
