# Inventaire des preuves pour le compte-rendu

Dernière revue : 9 novembre 2026. Les résultats indiqués ci-dessous sont des observations réelles; les actions GitHub non exécutées restent explicitement ouvertes.

- [x] Build .NET 10, code de sortie et journaux : `results/40-dotnet-build-fixed.txt`, `labs/talentai-deployment/results/docker-build-uncached.log`.
- [x] Docker Compose et connexion Atlas : `labs/talentai-deployment/results/compose-health-ready.json` et journal expurgé.
- [x] Terraform namespace : `labs/talentai-deployment/results/terraform-plan.txt` et `terraform-apply.txt`.
- [x] Ansible : exécution locale antérieure, syntaxe et prérequis validés.
- [x] Helm et Ingress : pod Ready, HTTP 200 et rollout, preuves dans `results/32-*`, `results/33-deployment-verification.txt` et `labs/talentai-deployment/results/ingress-verification.txt`.
- [x] Prometheus et Grafana : trois cibles Up, disponibilité 1 et tableau de bord présent dans `labs/talentai-deployment/results/monitoring-verification.json`.
- [x] HPA : montée réelle de 1 à 3 pods dans `results/34-hpa-load-test.txt`; retour à 1 pod observé le 9 novembre. La sécurité des sessions et CV entre pods n'est pas démontrée.
- [x] GitHub Actions CI restauration/build/audit : run #3 réussi sur `talentai-host-runner`, avec métadonnées GitHub vérifiées.
- [ ] Nouveau chemin CI build, Trivy et publication Docker Hub exécuté sur GitHub après revue et push.
- [ ] Workflow CD exécuté sur GitHub avec tag SHA, rollout et contrôles HTTP.
- [ ] Mot de passe email exposé historiquement remplacé par l'utilisateur; aucune valeur ne doit apparaître dans le rapport.
- [ ] Captures d'écran finales réelles pour CI publication, CD, HPA 3/3, Prometheus et Grafana. Une capture réelle du nœud Ready existe sous `screenshots/Pasted image.png`.
- [ ] DOCX corrigé selon l'architecture Docker Hub actuelle, rendu visuel vérifié et PDF produit.

Captures manuelles à réaliser sans secrets : page GitHub Actions du nouveau run CI montrant le job et son résultat; page du run CD montrant le tag et les contrôles HTTP; terminal `kubectl -n talentai get deployment,hpa` avec 3/3 durant un nouveau test seulement si nécessaire; page Prometheus Targets montrant les trois cibles Up; tableau de bord Grafana TalentAI Overview. Conserver les sorties déjà enregistrées si un nouveau test n'est pas nécessaire. Ne jamais capturer `.env`, `kubectl get secret -o yaml` ou une configuration de runner contenant un jeton.
