========================================================
==== SpotlightDL v1.4.2 - Par ORelio - Microzoom.fr ====
========================================================

Merci d'avoir téléchargé SpotlightDL!

Ce programme permet de récupérer les images de "Windows à la une" directement depuis l'API de Microsoft.
SpotlightDL peut également définir des images en tant que fond d'écran ou sur l'écran de verouillage global.

Ce programme est utile dans les cas suivants :
 - Télécharger toute la bibliothèque d'images en définition maximale, avec fichiers de métadonnées
 - Définir les images en tant que fond d'écran, non seulement sous Windows 10 mais aussi sur les versions précédentes
 - Définir les images sur l'écran de verouillage global sous Windows 7, 8 et 10, sans les publicités sous Windows 10
 - Utiliser SpotlightDL dans vos propres scripts et programmes en appelant la fonction récupérant les URL des images

=============
 Utilisation
=============

Extraire l'archive si cela n'est pas déjà fait, puis appeler SpotlightDownloader.exe depuis l'invite de commande.
Si vous n'avez pas l'habitude de l'invite de commande, quelques scripts Batch sont fournis pour vous aider :

spotlight-download-archive
  Ce script télécharge autant d'images que possibles depuis l'API Windows à la une, en demandant
  la définition maximale et en sauvegardant les métadonnées. N'étant pas possible de lister toutes
  les images d'un coup, SpotlightDL va effectuer beaucoup d'appels à l'API pour découvrir autant
  d'images que possible, puis s'arrêter lorsqu'aucune nouvelle image n'est découverte.
  Vous pourriez manquer quelques images, mais devriez en obtenir la plupart.

update-archive-and-wallpaper
  Ce script effectue un appel à l'API Windows à la une, et ajoute le résultat à l'archive.
  Il définit également une image de l'archive au hasard en tant que fond d'écran.
  Cela permet d'archiver les images au fur et à mesure sans envoyer beaucoup de requêtes API.

update-archive-and-lockscreen
  Même fonctionnement qu'update-archive-and-wallpaper mais change l'écran de verouillage global.
  Ce script doit être lancé en tant qu'administrateur car il remplace une image dans le dossier Windows
  et vide le cache de l'écran de verouillage afin d'en forcer la mise à jour immédiate.

update-wallpaper
  Ce script maintient un cache de quelques images et en définit une au hasard en tant que fond d'écran.
  Le cache permet d'avoir un peu de changement au niveau du fond d'écran même lorsque vous n'avez pas Internet.
  Les images sont téléchargées pour votre définition d'écran, et les plus anciennes sont supprimées du cache.

update-lockscreen
  Même fonctionnement qu'update-wallpaper mais définit l'image en tant qu'écran de verouillage global.
  Ce script doit être lancé en tant qu'administrateur car il remplace une image dans le dossier Windows
  et vide le cache de l'écran de verouillage afin d'en forcer la mise à jour immédiate.

restore-lockscreen
  Ce script restaure l'écran de verouillage par défaut.
  Il requiert les droits administrateur.

generate-manual
  Ce script sauvegarde le mode d'emploi en ligne de commande dans un fichier texte,
  que vous pouvez utiliser comme référence pour vos scripts Batch ou PowerShell.

hide-console
  Ce script démarre un autre script sans afficher la fenêtre de l'invite de commande.
  Il est utile principalement si vous souhaitez planfier un script à l'ouverture de session.
  Le chemin passé en argument ne devrait pas contenir de caractères spéciaux.

=========================================
 Planifier l'exécution d'un script batch
=========================================

Si vous souhaitez mettre à jour votre fond d'écran ou écran de verouillage périodiquement,
vous pouvez planifier l'exécution d'un script fourni en suivant ces instructions :

= Si vous n'avez pas les droits Administrateur =
= Méthode du raccourci dans le menu Démarrage =

Utilisez le raccourci clavier Win+R et spéciez:
  %appdata%\Microsoft\Windows\Start Menu\Programs\Startup

Faite un clic droit à un endroit vide du dossier Démarrage > Nouveau > Raccourci
  wscript "C:\Chemin\Vers\hide-console.vbs" "C:\Chemin\Vers\votre-script.bat"
  Suivant > Saisir un nom explicite pour le raccourci > Terminer

Le raccourci sera lancé à l'ouverture de session, ce qui exécutera le script.
Note: Les scripts pour l'écran de verouillage ne fonctionneront pas via cette méthode.

= Si vous avez les droits Administrateur =
= Méthode du planificateur de tâches =

Utilisez le raccourci clavier Win+R et spéciez:
  taskschd.msc

Cliquez sur "Créer une tâche..."
  Onglet Général
    - Definir un nom pour la tâche
    - Cocher "Exécuter avec les autorisations maximales" si vous désirez
      lancer le script en tant qu'Administrateur (écran de verouillage...)
  Onglet Déclencheurs
    - Cliquer sur Nouveau et ajouter un déclencheur
      "À l'ouverture de session" ou "À l'heure programmée", par exemple.
  Onglet Actions
    - Cliquer sur Nouveau, choisir Démarrer un programme
    - Programme/Script: wscript
    - Ajouter des arguments: "C:\Chemin\Vers\hide-console.vbs" "C:\Chemin\Vers\votre-script.bat"
  Onglet Conditions
    - À votre convenance, décocher "Ne démarrer la tâche que si l'ordinateur est relié au secteur"
  Onglet Paramètres
    - Si votre tâche a une heure planifiée, par ex 10h tous les jours, mais que votre ordinateur
      est éteint, la tâche ne s'exécutera pas. Vous pouvez activer l'option "Exécuter la
      tâche dès que possible si un démarrage planifié est manqué" pour y remédier.

Cliquez sur OK pour sauvegarder votre tâche.

=====
 FAQ
=====

Q: L'écran de verrouillage n'apparaît pas lorsque j'ai ouvert ma session ?
R: Assurez-vous que l'image est également sélectionnée dans vos paramètres personnels d'écran de verouillage.

Q: Combien d'images sont téléchargées par défaut ? (càd sans les arguments --single ou --many)
R: Par défaut, la liste d'images retournées par un seul appel API: précédemment 6-7, actuellement une seule.

Q: Certaines images n'ont pas de titre ou de copyright dans leur métadonnées?
R: Ces informations ne sont pas fournies pour toutes les images au niveau de l'API Windows à la une.

Q: Je ne veux pas du titre de l'image sur mon fond d'écran ou écran de verrouillage. Comment l'enlever ?
R: Modifiez le fichier batch que vous utilisez pour enlever le paramètre --embed-meta dans la commande associée.

Q: Je souhaite obtenir les métadonnées/images pour une langue spécifique. Comment faire ?
R: Modifiez le fichier batch que vous utilisez pour ajouter le paramètre --locale fr-FR ou autre code de langue.

Q: Lorsque je télécharge les images avec spotlight-download-archive.bat, j'en obtiens assez peu. Pourquoi ?
R: Cela peut être lié à la langue configurée sur votre système. Essayez fr-FR comme décrit ci-dessus.

+--------------------+
| © 2018-2020 ORelio |
+--------------------+