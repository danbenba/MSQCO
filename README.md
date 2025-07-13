# MySQL Console Client

Une application console légère en C# qui se connecte à une base de données MySQL, exécute des commandes SQL de façon interactive et journalise les sessions dans un fichier. Prend en charge la persistance de la configuration via `config.json` et une option de réinitialisation avec `--reset`.

---

## Fonctionnalités

* Invite de commandes interactive pour exécuter des requêtes SQL (similaire au client `mysql`)
* Sauvegarde automatique de la configuration dans `config.json` (serveur, base, utilisateur, mot de passe)
* Saisie sécurisée du mot de passe (affichage masqué)
* Journalisation des sessions dans `console.log` avec horodatage
* Option `--reset` pour supprimer la configuration et les journaux
* Sortie console colorée :

  * En-tête en magenta
  * Messages d’information en blanc
  * Messages de succès en vert
  * Messages d’erreur en rouge
  * Invite de commande en jaune

---

## Prérequis

* [.NET SDK 9.0+](https://dotnet.microsoft.com/download)
* Serveur MySQL accessible en réseau
* Package NuGet : `MySql.Data` (installation via `dotnet add package MySql.Data`)

---

## Installation

1. **Cloner le dépôt**

   ```bash
   git clone https://github.com/danbenba/MSQCO.git
   cd MSQCO
   ```

2. **Ajouter le package MySql.Data**

   ```bash
   dotnet add package MySql.Data
   ```

3. **Compiler le projet**

   ```bash
   dotnet build
   ```

---

## Utilisation

1. **Première exécution : initialisation de la configuration**

   ```bash
   dotnet run
   ```

   Vous serez invité·e à saisir :

   * Serveur (ex. `141.11.165.10`)
   * Nom de la base (ex. `s1730_Ufinder`)
   * Utilisateur (ex. `u1730_VlfeVrvAbM`)
   * Mot de passe (saisie masquée)

   Ces informations sont enregistrées dans `config.json` pour les prochaines utilisations.

2. **Exécuter des commandes SQL**
   Dans l’invite :

   ```sql
   > SELECT * FROM votre_table;
   > INSERT INTO table (col1, col2) VALUES (val1, val2);
   > exit  -- pour quitter
   ```

3. **Réinitialiser la configuration et les journaux**

   ```bash
   dotnet run -- --reset
   ```

   Supprime `config.json` et `console.log`.

---

## Journaux (`console.log`)

* Chaque session démarre avec :

  ```
  -- Nouvelle session démarrée le YYYY-MM-DD HH:MM:SS
  ```
* Chaque commande est enregistrée avec l’heure
* Les réussites, erreurs de connexion et déconnexion sont également consignées

---

## Contribuer

Les contributions sont les bienvenues : ouvrez une issue ou un pull request pour proposer des améliorations ou signaler des bugs.

---

## Licence

Ce projet est distribué sous licence MIT.
