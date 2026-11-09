# Déploiement vérifié — 8 novembre 2026 (Africa/Tunis)

## Commandes et résultats réels

- Ancien build : aucun processus `docker compose ... build` ni `buildx bake` actif. L'image `talentai:latest` reste distincte du build correct.
- `docker build --progress=plain -t talentai:local .` : code de sortie 0, mais compilation et publication réutilisées depuis le cache. Journal complet : `docker-build-correct.log`; code : `docker-build-correct.exitcode`.
- `docker build --no-cache --progress=plain -t talentai:local .` : code de sortie 0, compilation .NET 10 réelle, 0 erreur et 12 avertissements de dépendances NuGet (dont NU1903). Journal complet : `docker-build-uncached.log`; code : `docker-build-uncached.exitcode`.
- Vérification du contenu de l'image : `appsettings.json` présent; `.env`, `envirementfile.txt`, et la sauvegarde privée absents. Image locale créée avec l'identifiant `sha256:3bde887ad204e238fa8ff05ea7eedbd00eb39554869a105f9ec91d8cb54e28ee` lors du build sans cache.
- `docker compose -f compose.atlas.yaml up --no-build -d talentai` : conteneur démarré. `/health/live` : HTTP 200, `{"status":"live"}`. `/health/ready` : HTTP 200, `{"status":"ready"}`. Le journal indique « MongoDB connection verified. », preuve d'une authentification Atlas depuis Docker. Résultats `compose-health-*.json`, journal expurgé `compose-logs-redacted.txt`.
- Contexte Kubernetes `talentai`, nœud Ready avant déploiement. Plan Terraform : 1 namespace à créer, 0 modification, 0 destruction (`terraform-plan.txt`). Application : 1 ajout, 0 modification, 0 destruction; namespace `talentai` Active (`terraform-apply.txt`).
- `python3 scripts/deploy-from-env.py` : code de sortie 0. Image `talentai:local` chargée dans Minikube; Secret créé sans valeur affichée; Helm release `talentai` deployed, révision 1, rollout réussi (`helm-deploy.txt`).
- Vérification initiale : Deployment 1/1, pod 1/1 Running, Service ClusterIP, objet Ingress créé (`kubernetes-resources.txt`). Port-forward du Service : `/`, `/health/live` et `/health/ready` ont chacun renvoyé HTTP 200. Le journal du pod indique « MongoDB connection verified. » (`kubernetes-logs-redacted.txt`).

## Échec ultérieur et arrêt des opérations dépendantes

La sonde readiness a eu plusieurs délais d'attente avec le timeout Kubernetes par défaut de 1 seconde; le pod est ensuite revenu Ready. Le template Helm a été corrigé à `timeoutSeconds: 10` pour readiness et à 5 secondes pour startup/liveness, et `helm lint` a réussi. **Cette correction n'a pas encore été appliquée au cluster.**

L'addon ingress a été activé (`ingress-addon.txt`). Ses jobs d'admission ont terminé; le contrôleur était en cours de téléchargement. Ensuite `kubectl` a échoué avec `no route to host` vers l'API. Vérification à 19:11 CET : `minikube status -p talentai` indique host/kubelet/apiserver/kubeconfig Stopped; le conteneur Docker `talentai` est exited 255. L'hôte a redémarré à 19:01:40 CET selon `uptime -s`; le conteneur Minikube s'est arrêté vers 19:02:04 CET. La cause précise n'est pas établie. Le conteneur Compose est toujours Up. Aucun redémarrage Minikube, aucune mise à jour Helm et aucun test Ingress n'ont été tentés après cet échec, conformément à la consigne de pause.

## Captures et compte-rendu

Aucune capture d'écran n'a été fabriquée. Capturer manuellement les sorties réelles des journaux et commandes ci-dessus sans afficher `.env` ni les Secrets. La capture Ingress doit attendre un contrôleur fonctionnel; celle du timeout readiness doit être conservée comme incident. Les rapports DOCX/PDF ne sont pas encore finalisés. Les limites de sessions en mémoire, clés Data Protection locales, et CV locaux empêchent encore trois replicas.
