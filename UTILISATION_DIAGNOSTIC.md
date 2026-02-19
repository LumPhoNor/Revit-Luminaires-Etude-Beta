# 🔍 Utilisation de la Commande "Diagnostic Luminaire"

**Date** : 15/02/2026 23:48
**Version** : 1.0

---

## 🎯 Objectif

Cette commande vous permet d'analyser COMPLÈTEMENT un luminaire pour comprendre :
- ❓ Où est la **vraie source lumineuse** (1.75m vs 2.30m)
- ❓ Quelle **hauteur utiliser** dans les calculs (Centre vs Max.Z)
- ❓ Quel est l'**impact** sur l'éclairement calculé

---

## 🚀 Procédure d'Utilisation

### Étape 1 : Fermer Revit
```
CTRL + Q (quitter Revit)
```

### Étape 2 : Recompiler
```bash
cd "C:\Users\JEDI-Lee\Documents\Projets Plugin\RevitLightingPlugin"
dotnet build
```

**Résultat attendu** : `Build succeeded. 0 Error(s)`

### Étape 3 : Lancer Revit
- Ouvrir Revit 2026
- Ouvrir votre projet avec le luminaire R924.01

### Étape 4 : Lancer la Commande
1. Onglet **"Éclairage"** (en haut)
2. Panneau **"Analyse"**
3. Bouton **"Diagnostic Luminaire"** (nouveau bouton)
4. **Cliquer sur votre luminaire R924.01** dans la vue

### Étape 5 : Lire le Rapport
Une fenêtre s'ouvre avec le rapport détaillé.

---

## 📊 Exemple de Rapport pour R924.01

```
╔════════════════════════════════════════════════════════════════════════╗
║                    DIAGNOSTIC COMPLET DU LUMINAIRE                     ║
╚════════════════════════════════════════════════════════════════════════╝

Date : 15/02/2026 23:50:00
Document : Projet_Test.rvt

════════════════════════════════════════════════════════════════════════
1. INFORMATIONS GÉNÉRALES
════════════════════════════════════════════════════════════════════════
ID Revit           : 123456
Nom instance       : R924.01 : iGuzzini R924.01 - 1
Catégorie          : Luminaires
Type               : iGuzzini R924.01
Famille            : iGuzzini R924.01

════════════════════════════════════════════════════════════════════════
2. POSITION ET GÉOMÉTRIE
════════════════════════════════════════════════════════════════════════
📍 LocationPoint (Point d'insertion) :
   X = 10.500 ft (3.200 m)
   Y = 15.250 ft (4.650 m)
   Z = 5.450 ft (1.661 m) ⬅ Point d'insertion

📦 BoundingBox (Boîte englobante) :
   Min.X = 9.800 ft (2.987 m)
   Min.Y = 14.550 ft (4.436 m)
   Min.Z = 3.940 ft (1.201 m) ⬅ BAS du luminaire

   Max.X = 11.200 ft (3.414 m)
   Max.Y = 15.950 ft (4.862 m)
   Max.Z = 7.550 ft (2.302 m) ⬅ HAUT du luminaire

📐 Dimensions du luminaire :
   Largeur (X) = 0.427 m
   Profondeur (Y) = 0.426 m
   Hauteur (Z) = 1.101 m

🎯 POSITIONS CALCULÉES :
   Centre Z (approx) = 5.745 ft (1.751 m) ⬅ SOURCE PROBABLE

⚡ ANALYSE POUR CALCULS PHOTOMÉTRIQUES :
   ✅ Luminaire ÉPAIS (1.10m) → Utiliser CENTRE (1.751m)
   Différence Max.Z vs Centre = 0.551 m
   Impact sur éclairement = 157.28%

════════════════════════════════════════════════════════════════════════
3. PARAMÈTRES DU TYPE (FamilySymbol)
════════════════════════════════════════════════════════════════════════
   Fabricant                                     = iGuzzini
   Référence                                     = R924.01
   Description                                   = Suspension LED 35W
   Flux lumineux                                 = 2368.000 lm
   Puissance                                     = 35.000 W
   Efficacité                                    = 67.657 lm/W
   Température de couleur                        = 3000 K
   IRC                                           = 90
   IES File                                      = C:\...\R924.G1_D55Y.ies

════════════════════════════════════════════════════════════════════════
5. DONNÉES PHOTOMÉTRIQUES
════════════════════════════════════════════════════════════════════════
📄 Fichier IES : C:\Users\...\iGuzzini\R924.G1_D55Y.ies
   ✅ Fichier trouvé sur disque

📊 Données IES parsées :
   Fabricant      : iGuzzini illuminazione
   Référence      : R924.G1_D55Y
   Flux lumineux  : 2368 lm
   Puissance      : 35 W
   Efficacité     : 68 lm/W
   Nb lampes      : 1

════════════════════════════════════════════════════════════════════════
6. RECOMMANDATIONS POUR CALCULS PHOTOMÉTRIQUES
════════════════════════════════════════════════════════════════════════
Pour le paramètre 'realZ' dans LightingCalculator.cs :

✅ RECOMMANDATION : Utiliser le CENTRE de la BoundingBox
   Code : double realZ = (lumBbox.Min.Z + lumBbox.Max.Z) / 2.0;
   Valeur : 5.745 ft = 1.751 m

   Raison : Luminaire épais (1.10m), la source est probablement au centre

Alternative : Ajouter un paramètre 'Light Source Offset' à la famille
pour spécifier la position exacte de la source.

════════════════════════════════════════════════════════════════════════
```

---

## 🎯 Interprétation des Résultats

### Votre cas (R924.01) :

| Donnée | Valeur | Signification |
|--------|--------|---------------|
| **Min.Z** | 1.20m | Bas du luminaire |
| **Max.Z** | 2.30m | Haut/fixation au plafond ❌ |
| **Centre Z** | **1.75m** | **Position source lumineuse** ✅ |
| **LocationPoint.Z** | 1.66m | Point d'insertion (arbitraire) |

### Code actuel (FAUX) :
```csharp
double realZ = lumBbox.Max.Z;  // = 2.30m ❌
```

### Code corrigé (BON) :
```csharp
double realZ = (lumBbox.Min.Z + lumBbox.Max.Z) / 2.0;  // = 1.75m ✅
```

### Impact sur l'éclairement :
```
AVANT (Max.Z = 2.30m) :
E = I / (2.30)² = I / 5.29

APRÈS (Centre = 1.75m) :
E = I / (1.75)² = I / 3.06

Amélioration = 5.29 / 3.06 = 1.73
➜ +73% d'éclairement calculé ! 🚀
```

---

## 📝 Prochaine Action

Après avoir lancé le diagnostic sur votre R924.01, vous saurez :

1. ✅ La **position exacte** de la source (devrait être ~1.75m)
2. ✅ Si utiliser **Centre** est la bonne approche
3. ✅ L'**écart** avec le calcul actuel

Ensuite, on pourra :
- Modifier `LightingCalculator.cs` ligne 209
- Utiliser `(lumBbox.Min.Z + lumBbox.Max.Z) / 2.0` au lieu de `lumBbox.Max.Z`
- Recompiler et tester
- Comparer avec Dialux

---

## 🔍 Logs

Le rapport complet est aussi écrit dans les logs :
```
C:\Users\JEDI-Lee\Documents\Projets Plugin\RevitLightingPlugin\Logs\RevitLightingPlugin_YYYYMMDD_HHmmss.log
```

Chercher la section `[DiagnosticLuminaire]`

---

**Créé le** : 15/02/2026 23:48
**Compilé** : ✅ 0 erreurs
**Prêt à utiliser** : ✅ OUI
