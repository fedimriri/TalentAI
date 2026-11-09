#!/usr/bin/env python3
"""Generate the evidence-based French TalentAI report."""
from pathlib import Path
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.style import WD_STYLE_TYPE
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

root = Path(__file__).resolve().parent.parent
out = root / 'labs/talentai-deployment/compte-rendu/compte-rendu.docx'
screenshot = root / 'labs/talentai-deployment/screenshots/Pasted image.png'
d = Document()
sec=d.sections[0]
sec.page_width=Inches(8.5); sec.page_height=Inches(11)
sec.top_margin=Inches(.78);sec.bottom_margin=Inches(.7)
sec.left_margin=Inches(.85);sec.right_margin=Inches(.85)
styles=d.styles
styles['Normal'].font.name='Aptos';styles['Normal'].font.size=Pt(10.5)
styles['Normal'].paragraph_format.space_after=Pt(5)
for name,size in [('Title',21),('Heading 1',14),('Heading 2',11.5)]:
 s=styles[name];s.font.name='Aptos';s.font.size=Pt(size);s.font.color.rgb=RGBColor(0,0,0);s.font.bold=True
 s.paragraph_format.space_before=Pt(12 if name!='Title' else 0)
 s.paragraph_format.space_after=Pt(5)
if 'Caption' not in styles:
 styles.add_style('Caption',WD_STYLE_TYPE.PARAGRAPH)
styles['Caption'].font.name='Aptos';styles['Caption'].font.size=Pt(9);styles['Caption'].font.italic=True

def shade(cell,fill):
 tcPr=cell._tc.get_or_add_tcPr();sh=OxmlElement('w:shd');sh.set(qn('w:fill'),fill);tcPr.append(sh)
def table(headers,rows,widths=None):
 t=d.add_table(rows=1, cols=len(headers));t.alignment=WD_TABLE_ALIGNMENT.CENTER;t.style='Table Grid'
 for i,h in enumerate(headers):
  c=t.rows[0].cells[i];c.text=h;shade(c,'DCE6F1');c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
  for r in c.paragraphs[0].runs:r.bold=True
 for j,row in enumerate(rows):
  cells=t.add_row().cells
  for i,value in enumerate(row):
   cells[i].text=str(value);cells[i].vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
   if j%2:shade(cells[i],'F5F7FA')
 if widths:
  for row in t.rows:
   for i,w in enumerate(widths):row.cells[i].width=Inches(w)
 d.add_paragraph()
 return t

def para(text):d.add_paragraph(text)
def heading(text,level=1):d.add_heading(text,level=level)
def bullet(text):d.add_paragraph(text,style='List Bullet')

p=d.add_paragraph(style='Title');p.add_run('Compte rendu du déploiement DevOps de TalentAI')
p=d.add_paragraph();p.add_run('Projet pratique TEK-UP University').bold=True
para('État observé le 8 novembre 2026 · Environnement local Ubuntu, Docker et Minikube')
para('Ce compte rendu présente les opérations réellement exécutées pour déployer l’application ASP.NET Core .NET 10 avec MongoDB Atlas. Le conteneur Docker, le déploiement Kubernetes à un réplica, l’Ingress et la supervision locale ont été vérifiés. Les actions GitHub Actions et la montée à trois réplicas ne sont pas encore validées.')

heading('Architecture déployée')
table(['Chaîne de livraison','Exécution locale'],[
 ('GitHub → GitHub Actions → image Docker','Workflows définis, exécution distante non vérifiée'),
 ('Minikube → NGINX Ingress → Service ClusterIP','TalentAI 1 réplica, endpoints HTTP vérifiés'),
 ('TalentAI → MongoDB Atlas','Base externe existante, ping authentifié depuis Docker et Kubernetes'),
 ('Prometheus → Grafana','Disponibilité, CPU et mémoire affichés dans un tableau de bord')], [3.1,3.5])
para('Terraform gère uniquement le namespace talentai. Helm gère le Deployment, le Service, la ConfigMap, l’Ingress et le HPA. Le Secret Kubernetes est alimenté depuis un fichier privé et n’est pas stocké dans Git.')

heading('Docker et MongoDB Atlas')
para('Un build Docker sans cache a compilé et publié TalentAI pour .NET 10 avec un code de sortie 0 et aucune erreur de compilation. Le build a produit 12 avertissements NuGet, dont plusieurs avis de vulnérabilité élevés. Le contexte Docker exclut les fichiers de secrets locaux; l’image contient un fichier de configuration expurgé avec le nom de la base, sans URI Atlas.')
para('La configuration Compose dédiée à Atlas a démarré talentai:local. Les requêtes HTTP sur /, /health/live et /health/ready ont renvoyé 200. La sonde ready exécute un ping MongoDB; le journal indique « MongoDB connection verified. ». Les tests prouvent l’authentification Atlas depuis le conteneur, sans révéler l’URI.')

heading('Infrastructure avec Ansible et Terraform')
para('Le playbook Ansible de validation locale s’est exécuté sans changement ni erreur : ok=4, changed=0, failed=0. Terraform a été initialisé avec le fournisseur Kubernetes v3.5.0 et sa configuration a été validée. Le plan prévoyait 1 ajout, 0 modification et 0 destruction. L’application a créé le namespace talentai, ensuite observé à l’état Active.')

heading('Kubernetes Helm et Ingress')
para('Helm a déployé TalentAI dans le namespace dédié avec un réplica, des limites CPU et mémoire, une stratégie RollingUpdate et trois sondes. Un délai de réponse initial de 1 seconde pour readiness a produit des échecs intermittents lors du ping Atlas. La révision 2 a augmenté ce délai à 10 secondes; le rollout a réussi et le pod était 1/1 Ready. L’Ingress NGINX, accessible via talentai.local, a renvoyé HTTP 200 pour la page d’accueil et les deux sondes.')
para('Commande d’accès reproductible depuis l’hôte : curl --noproxy "*" --resolve talentai.local:80:192.168.49.2 http://talentai.local/health/ready. L’adresse Minikube peut changer après redémarrage; la relire avec minikube ip -p talentai.')

heading('Supervision Prometheus et Grafana')
para('Une pile légère dans le namespace monitoring utilise trois pods : Prometheus, blackbox exporter et Grafana. Prometheus conserve 24 heures de données dans un volume éphémère. Les trois cibles Prometheus sont Up : Prometheus, le proxy cAdvisor Kubernetes et la sonde de disponibilité de TalentAI. La requête probe_success a renvoyé 1. Les requêtes CPU et mémoire de TalentAI ont renvoyé respectivement environ 0,0104 cœur et 62 382 080 octets au moment du contrôle.')
para('Grafana a répondu HTTP 200, sa base interne était opérationnelle et le tableau de bord « TalentAI Overview » était présent. Les panneaux portent sur la disponibilité, le CPU et la mémoire. L’accès local se fait par port-forward : kubectl -n monitoring port-forward service/prometheus 9090:9090 et kubectl -n monitoring port-forward service/grafana 3000:3000. Le mot de passe Grafana est stocké uniquement dans un fichier local ignoré par Git et dans un Secret Kubernetes.')

heading('CI CD et sécurité')
para('Le workflow CI contient restore, build, découverte des projets de tests, rapport des dépendances vulnérables, build Docker, scan Trivy et publication GHCR. Aucun projet de test dédié n’existe actuellement. Le workflow de déploiement local est manuel, restreint à la branche main et à un runner auto-hébergé étiqueté talentai-local; il vérifie le contexte, charge l’image dans Minikube et attend le rollout Helm. Les workflows ont été analysés statiquement, mais leur exécution sur GitHub, la publication GHCR et l’enregistrement du runner restent non vérifiés. Aucun push n’a été effectué.')
para('Les valeurs sensibles ont été retirées du fichier appsettings.json suivi par Git. Une sauvegarde privée locale a été conservée avec les droits 0600. Les fichiers .env, envirementfile.txt et cette sauvegarde sont ignorés par Git et exclus du contexte Docker. Les journaux sauvegardés ont été expurgés.')

heading('HPA fiabilité et limites')
para('Metrics Server fournit des mesures CPU et mémoire. Le HPA CPU à cible 70 % a été créé avec minimum 1 et maximum 1 réplica : il est volontairement empêché de monter à trois. Avant le test d’auto-réparation, il affichait une mesure CPU de 25 % / 70 %. Après remplacement du pod, la métrique HPA est temporairement inconnue pendant la collecte; la vérification finale de son retour reste à confirmer.')
para('Le pod ne contenait aucun fichier CV local avant le test. Sa suppression contrôlée a permis à Kubernetes de créer un nouveau pod 1/1 Ready; l’Ingress a ensuite répondu HTTP 200 sur / et les deux sondes. Un rollback Helm vers la révision 2 a réussi, puis une mise à niveau a restauré la révision 5 avec HPA à un réplica. Il s’agit d’un test de réparation sur un seul nœud, pas d’une preuve de haute disponibilité.')
para('Trois réplicas restent dangereux : sessions en mémoire, clés ASP.NET Data Protection stockées localement, et CV enregistrés dans le système de fichiers du pod. Une solution de session partagée, de clés persistantes partagées et de stockage CV commun doit être implémentée et testée avant tout changement de plafond HPA.')

heading('Vérifications et preuves')
table(['Contrôle','Résultat réel','Preuve dans results'],[
 ('Build .NET 10 sans cache','Succès, 0 erreur','docker-build-uncached.log'),
 ('Atlas depuis Docker','ready HTTP 200','compose-health-ready.json'),
 ('Terraform namespace','1 ajouté, 0 détruit','terraform-apply.txt'),
 ('Helm et Service','1/1 Ready, HTTP 200','helm-deploy.txt, k8s-health-ready.json'),
 ('Ingress','HTTP 200 sur trois routes','ingress-verification.txt'),
 ('Monitoring','3 cibles Up, dashboard présent','monitoring-verification.json'),
 ('Auto-réparation','Nouveau pod 1/1 Ready','self-healing.txt'),
 ('CI CD GitHub','Non exécuté','Workflows locaux seulement')],[2.0,2.0,2.6])

heading('Incidents et travaux restants')
bullet('La préparation initiale de Minikube a subi des lenteurs réseau. Un redémarrage de l’hôte a ensuite arrêté le profil; les ressources Kubernetes ont survécu et Minikube a été récupéré.')
bullet('La sonde readiness initiale était trop courte pour un ping Atlas. Le délai Helm a été porté à 10 secondes et vérifié après upgrade.')
bullet('Le premier contrôle Grafana a échoué en HTTP 401 à cause d’un saut de ligne dans le fichier du mot de passe. Le Secret a été corrigé; Grafana a ensuite répondu et le dashboard a été retrouvé.')
bullet('La première cible blackbox était Down pendant le démarrage du pod. Le contrôle suivant a montré les trois cibles Up.')
bullet('Les avis NuGet et le résultat du scan CI nécessitent une révision des dépendances avant publication. Les captures d’écran des phases finales restent à produire; une capture réelle du nœud Ready est fournie en annexe.')

heading('Conclusion')
para('Le chemin local Docker → Atlas et Minikube → Ingress → TalentAI → Atlas fonctionne à un réplica. La supervision renvoie des données vérifiées et les opérations Terraform, Helm, HPA borné, auto-réparation et rollback ont des traces réelles. La validation complète du CI/CD GitHub et de la montée à trois réplicas reste ouverte. Le présent document distingue ces résultats confirmés des éléments non encore testés.')

heading('Annexe capture disponible')
if screenshot.exists():
 p=d.add_paragraph('Capture réelle fournie : nœud Kubernetes Ready et pods système.');p.style='Caption'
 d.add_picture(str(screenshot),width=Inches(6.7))
else:
 para('Capture non disponible. Ajouter une capture du nœud Ready sans secrets.')

footer=sec.footer.paragraphs[0]
footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
footer.text='TalentAI · Compte rendu DevOps · 8 novembre 2026'
out.parent.mkdir(parents=True,exist_ok=True)
d.save(out)
print(out)
