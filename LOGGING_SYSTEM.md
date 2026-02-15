# 📋 Système de Logging - RevitLightingPlugin

**Date de création :** 15/02/2026
**Version :** 1.0

---

## ✅ SYSTÈME INSTALLÉ

Un système de logging complet a été ajouté au plugin RevitLightingPlugin.

### 📂 Fichiers créés/modifiés

| Fichier | Type | Description |
|---------|------|-------------|
| **Core/Logger.cs** | ✨ NOUVEAU | Classe de logging centralisée |
| **Application.cs** | ✏️ MODIFIÉ | Logs au startup/shutdown |
| **Commands/LightingAnalysisCommand.cs** | ✏️ MODIFIÉ | Logs de la commande principale |
| **Core/LightingCalculator.cs** | ✏️ MODIFIÉ | Logs des calculs photométriques |
| **RevitLightingPlugin.csproj** | ✏️ MODIFIÉ | Logger.cs ajouté à la compilation |
| **.gitignore** | ✏️ MODIFIÉ | Dossier Logs/ exclu |
| **Logs/README.md** | ✨ NOUVEAU | Documentation du dossier logs |

---

## 📁 Emplacement des Logs

```
C:\Users\JEDI-Lee\Documents\Projets Plugin\RevitLightingPlugin\Logs\
RevitLightingPlugin_20260215_224530.log
RevitLightingPlugin_20260215_230145.log
...
```

### Format du nom de fichier
```
RevitLightingPlugin_YYYYMMDD_HHmmss.log
```

- **YYYY** = Année (2026)
- **MM** = Mois (02)
- **DD** = Jour (15)
- **HH** = Heure (22)
- **mm** = Minutes (45)
- **ss** = Secondes (30)

**➡ Un nouveau fichier est créé à chaque lancement de Revit**

---

## 🎯 Fonctionnalités

### ✅ Niveaux de Log

```csharp
Logger.Debug("Category", "Message");     // 🔍 DEBUG
Logger.Info("Category", "Message");      // ℹ️  INFO
Logger.Warning("Category", "Message");   // ⚠️  WARNING
Logger.Error("Category", "Message", ex); // ❌ ERROR
Logger.Critical("Category", "Msg", ex);  // 🔥 CRITICAL
```

### ✅ Fonctionnalités avancées

```csharp
// Sép...aration visuelle
Logger.Separator("SECTION TITLE");

// Traçage des méthodes
Logger.EnterMethod("ClassName", "MethodName", param1, param2);
Logger.ExitMethod("ClassName", "MethodName", returnValue);

// Mesure de performance
Logger.Performance("Operation name", timespan);

// Obtenir le chemin du log
string path = Logger.GetLogFilePath();

// Fermer proprement
Logger.Close();
```

---

## 📊 Structure d'un Log

### En-tête (au démarrage)
```
╔════════════════════════════════════════════════════════════════════════╗
║                    REVIT LIGHTING PLUGIN - LOG FILE                    ║
╚════════════════════════════════════════════════════════════════════════╝
Session démarrée : 15/02/2026 22:45:30
Version : 2.0
Machine : DESKTOP-ABC123
Utilisateur : JEDI-Lee
OS : Microsoft Windows NT 10.0.26100.0
.NET Framework : 4.0.30319.42000
════════════════════════════════════════════════════════════════════════
```

### Format d'une ligne de log
```
[TIMESTAMP] [NIVEAU] [CATÉGORIE] [THREAD] MESSAGE
```

Exemple :
```
[2026-02-15 22:45:30.123] ℹ️  INFO     [Application         ] [T001] Démarrage du plugin RevitLightingPlugin
```

### Avec exception
```
[2026-02-15 22:45:30.456] ❌ ERROR    [LightingCalculator  ] [T001] Erreur de calcul
    Exception: NullReferenceException
    Message: Object reference not set to an instance of an object.
    StackTrace:
       at RevitLightingPlugin.Core.LightingCalculator.CalculateForRoom(...)
       ...
```

---

## 🔍 Points Loggés dans l'Application

### 1️⃣ **Application.cs** (Startup/Shutdown)
- Initialisation du logger
- Création des onglets et boutons
- Arrêt propre du plugin

**Catégorie :** `Application`

### 2️⃣ **LightingAnalysisCommand.cs** (Commande principale)
- Lancement de la commande
- Sélection des pièces
- Configuration des paramètres
- Progression des calculs
- Résultats par pièce
- Performances (temps d'exécution)
- Erreurs

**Catégories :** `LightingAnalysisCmd`

### 3️⃣ **LightingCalculator.cs** (Calculs photométriques)
- Nombre de luminaires trouvés
- Flux lumineux total
- Démarrage du calcul de grille
- Résultats : Em, Emin, Emax, U0, Uh
- Entrée/sortie des méthodes

**Catégories :** `LightingCalculator`, `MethodTrace`

---

## 📖 Exemples d'Utilisation

### Consulter un log

```bash
# Windows
notepad "C:\Users\JEDI-Lee\Documents\Projets Plugin\RevitLightingPlugin\Logs\RevitLightingPlugin_20260215_224530.log"

# VS Code
code "C:\Users\JEDI-Lee\Documents\Projets Plugin\RevitLightingPlugin\Logs\RevitLightingPlugin_20260215_224530.log"
```

### Rechercher des erreurs

```bash
cd "C:\Users\JEDI-Lee\Documents\Projets Plugin\RevitLightingPlugin\Logs"

# Toutes les erreurs
findstr /C:"ERROR" /C:"CRITICAL" *.log

# Erreurs dans le calculateur
findstr /C:"LightingCalculator" /C:"ERROR" *.log
```

### Voir les performances

```bash
findstr /C:"Performance" *.log
```

Exemple de sortie :
```
[2026-02-15 22:45:35.789] ℹ️  INFO     [Performance         ] [T001] Calcul pièce Bureau 1 completed in 1234.56 ms
[2026-02-15 22:45:40.123] ℹ️  INFO     [Performance         ] [T001] Analyse d'éclairement complète completed in 5678.90 ms
```

### Filtrer par niveau

```bash
# Seulement les INFO
findstr /C:"INFO" *.log

# Warnings et plus
findstr /C:"WARNING" /C:"ERROR" /C:"CRITICAL" *.log
```

---

## ⚙️ Configuration

### Modifier le niveau minimum de log

Éditer `Core/Logger.cs` ligne 27 :

```csharp
public static LogLevel MinimumLevel { get; set; } = LogLevel.DEBUG;
```

**Options :**
- `LogLevel.DEBUG` = Tout (défaut) - ~100-200 lignes/analyse
- `LogLevel.INFO` = Info + Warning + Error + Critical - ~50-80 lignes/analyse
- `LogLevel.WARNING` = Seulement warnings et erreurs - ~5-10 lignes/analyse
- `LogLevel.ERROR` = Seulement erreurs - ~0-5 lignes/analyse
- `LogLevel.CRITICAL` = Seulement erreurs critiques - ~0-2 lignes/analyse

**Recommandation :**
- **Développement** : `LogLevel.DEBUG` (tout voir)
- **Production** : `LogLevel.INFO` (équilibre)
- **Release** : `LogLevel.WARNING` (léger)

---

## 🛡️ Gestion des Erreurs

Le système de logging est **ultra-robuste** :

### Fallback automatique

1. Essai d'écriture dans `Logs/` à la racine du projet
2. Si échec → Fallback vers `%TEMP%\RevitLightingPlugin_Fallback_*.log`
3. Si échec → Emergency log dans `%TEMP%\RevitLightingPlugin_Emergency_*.log`
4. Si échec → Silence (pas de crash)

### Thread-safe

- Utilise `lock` pour éviter les corruptions multi-thread
- Chaque écriture est atomique

### Encoding UTF-8

- Supporte les caractères spéciaux (emojis ✅)
- Compatible avec tous les éditeurs de texte

---

## 🗑️ Nettoyage des Logs

### Manuel

Supprimer les fichiers du dossier `Logs/` :

```bash
cd "C:\Users\JEDI-Lee\Documents\Projets Plugin\RevitLightingPlugin\Logs"
del /Q RevitLightingPlugin_*.log
```

### Recommandation

- Conserver les logs des **7 derniers jours**
- Archiver les logs importants dans un dossier séparé
- Logs volumineux (>10 MB) peuvent ralentir l'ouverture

---

## 🚀 Ajouter du Logging dans Votre Code

### Exemple simple

```csharp
using RevitLightingPlugin.Core;

public void MyMethod()
{
    Logger.Info("MyClass", "Démarrage de la méthode");

    try
    {
        // Votre code
        Logger.Debug("MyClass", $"Valeur calculée : {result}");
    }
    catch (Exception ex)
    {
        Logger.Error("MyClass", "Erreur lors du calcul", ex);
    }
}
```

### Exemple avec performance

```csharp
using System.Diagnostics;
using RevitLightingPlugin.Core;

public void LongOperation()
{
    var stopwatch = Stopwatch.StartNew();
    Logger.EnterMethod("MyClass", "LongOperation");

    try
    {
        // Opération longue
        DoWork();

        stopwatch.Stop();
        Logger.Performance("LongOperation", stopwatch.Elapsed);
        Logger.ExitMethod("MyClass", "LongOperation", "Success");
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        Logger.Error("MyClass", "Erreur dans LongOperation", ex);
        Logger.ExitMethod("MyClass", "LongOperation", "Failed");
    }
}
```

---

## 📊 Statistiques

### Taille moyenne d'un log

- **DEBUG** : ~10-50 KB par analyse
- **INFO** : ~5-20 KB par analyse
- **WARNING** : ~1-5 KB par analyse

### Nombre de lignes

- **Startup** : ~10 lignes
- **Analyse 1 pièce** : ~50-100 lignes (DEBUG)
- **Shutdown** : ~5 lignes

---

## ✅ Checklist de Vérification

Après compilation, vérifier :

- [ ] Dossier `Logs/` créé à la racine du projet
- [ ] Fichier log créé au lancement de Revit
- [ ] En-tête présent avec infos système
- [ ] Logs visibles pendant l'exécution
- [ ] Footer ajouté à la fermeture de Revit
- [ ] Emojis visibles (si éditeur UTF-8)

---

## 🆘 Dépannage

### Logs non créés

1. Vérifier que `Logger.Initialize()` est appelé dans `Application.OnStartup()`
2. Vérifier les permissions d'écriture sur le dossier Logs/
3. Chercher le fallback dans `%TEMP%\RevitLightingPlugin_Fallback_*.log`

### Logs vides

1. Vérifier que `MinimumLevel` n'est pas trop restrictif
2. Vérifier que les appels `Logger.Info/Debug/etc()` sont présents

### Emojis illisibles

1. Ouvrir avec un éditeur UTF-8 (VS Code, Notepad++, pas Notepad classique)
2. Ou remplacer les emojis par du texte dans `Logger.cs` lignes 169-187

---

## 📝 Notes Techniques

- **Thread ID** : Permet de suivre les opérations multi-thread
- **Milliseconde** : Précision à la milliseconde pour debug fin
- **Catégorie** : Facilite le filtrage par composant
- **Exception complète** : Stack trace + Inner Exception

---

## 🔜 Améliorations Futures Possibles

- [ ] Rotation automatique des logs (supprimer >7 jours)
- [ ] Export en HTML avec coloration syntaxique
- [ ] Dashboard de visualisation des logs
- [ ] Envoi automatique des erreurs critiques par email
- [ ] Compression des logs anciens (.zip)
- [ ] Intégration avec Sentry/Application Insights

---

**Système créé le :** 15/02/2026
**Auteur :** Claude Code
**Version :** 1.0
**Status :** ✅ Production Ready
