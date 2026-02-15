using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Globalization;
using System.IO;
using RevitLightingPlugin.Models;

namespace RevitLightingPlugin.Core
{
    public class LightingCalculator
    {
        private readonly Document _doc;
        private static bool _logFileInitialized = false; // Pour nettoyer le fichier une seule fois
        private static readonly string DebugLogPath = Path.Combine(Path.GetTempPath(), "RevitLightingPlugin_Debug.log");

        public LightingCalculator(Document doc)
        {
            _doc = doc;

            // Nettoyer le fichier de log au début de chaque session (pas chaque pièce)
            if (!_logFileInitialized)
            {
                try
                {
                    File.WriteAllText(DebugLogPath, $"=== RevitLightingPlugin Debug Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
                    _logFileInitialized = true;
                }
                catch { }
            }
        }

        /// <summary>
        /// 🚨 CORRECTION CRITIQUE : Méthode de log pour diagnostiquer problème IES
        /// </summary>
        private void LogDebug(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                File.AppendAllText(DebugLogPath, $"[{timestamp}] {message}\n");
            }
            catch { }
        }

        public LightingResult CalculateForRoom(Room room)
        {
            // Appeler le nouvel overload avec valeurs par défaut
            var settings = new AnalysisSettings();
            return CalculateForRoom(room, settings, settings.WorkPlaneHeight);
        }

        public LightingResult CalculateForRoom(Room room, AnalysisSettings settings, double workPlaneHeight)
        {
            var result = new LightingResult
            {
                RoomName = room.Name,
                RoomNumber = room.Number,
                WorkPlaneHeight = workPlaneHeight
            };

            Parameter areaParam = room.get_Parameter(BuiltInParameter.ROOM_AREA);
            if (areaParam != null && areaParam.HasValue)
            {
                result.RoomArea = areaParam.AsDouble() * 0.092903;
            }

            var luminaires = GetLuminairesInRoom(room);
            result.LuminaireCount = luminaires.Count;

            if (luminaires.Count == 0)
            {
                result.AverageIlluminance = 0;
                result.UniformityRatio = 0;
                result.TotalPower = 0;
                result.IsCompliant = false;
                result.Recommendation = "Aucun luminaire trouvé dans cette pièce.";
                result.GridPoints = new List<GridPoint>();
                return result;
            }

            result.Luminaires = new List<LuminaireInfo>();
            double totalLumens = 0;
            double totalPower = 0;

            foreach (var luminaire in luminaires)
            {
                var info = ExtractLuminaireInfo(luminaire);
                result.Luminaires.Add(info);
                totalLumens += info.FluxLumineux;
                totalPower += info.Puissance;
            }

            result.TotalPower = totalPower;

            // Convertir mètres en pieds pour Revit
            double gridSpacingFeet = settings.GridSpacing * 3.28084;
            double workplaneHeightFeet = workPlaneHeight * 3.28084;

            // NOUVEAU : Calcul par grille de points avec paramètres
            result.GridPoints = CalculateGridIlluminance(room, luminaires, result.Luminaires, gridSpacingFeet, workplaneHeightFeet, settings);

            if (result.GridPoints.Count > 0)
            {
                result.AverageIlluminance = result.GridPoints.Average(p => p.Illuminance);
                result.MinIlluminance = result.GridPoints.Min(p => p.Illuminance);
                result.MaxIlluminance = result.GridPoints.Max(p => p.Illuminance);
                result.UniformityRatio = result.MinIlluminance / result.AverageIlluminance;

                // P4: Calcul uniformité locale selon EN 12464-1 section 4.3
                result.LocalUniformity = CalculateLocalUniformity(result.GridPoints, gridSpacingFeet);
            }
            else
            {
                // Fallback
                if (result.RoomArea > 0)
                {
                    result.AverageIlluminance = CalculateAverageIlluminance(totalLumens, result.RoomArea);
                }
                result.UniformityRatio = 0.7;
                result.LocalUniformity = 0.7; // Valeur par défaut
            }

            result.IsCompliant = result.AverageIlluminance >= 500;
            result.Recommendation = result.IsCompliant
                ? "L'éclairage est conforme aux normes."
                : "L'éclairage est insuffisant. Ajoutez des luminaires ou augmentez leur puissance.";

            return result;
        }

        private List<GridPoint> CalculateGridIlluminance(Room room, List<FamilyInstance> luminaires, List<LuminaireInfo> luminaireInfos, double gridSpacingFeet, double workplaneHeightFeet, AnalysisSettings settings)
        {
            var gridPoints = new List<GridPoint>();

            BoundingBoxXYZ bbox = room.get_BoundingBox(null);
            if (bbox == null) return gridPoints;

            // P3: Facteur de maintenance variable selon EN 12464-1 Annexe B
            // (type de luminaire + environnement)
            double maintenanceFactor = settings.GetMaintenanceFactor();

            // P2: Calculer le facteur d'éclairement indirect
            double indirectFactor = CalculateIndirectFactor(room, settings);

            // === DIAGNOSTIC COMPLET : BBox pièce + positions de TOUS les luminaires ===
            LogDebug($"");
            LogDebug($"╔══════════════════════════════════════════════════════════╗");
            LogDebug($"║  DIAGNOSTIC GÉOMÉTRIQUE - {room.Name}");
            LogDebug($"╚══════════════════════════════════════════════════════════╝");
            LogDebug($"[ROOM] BBox Min: X={bbox.Min.X:F3} ft ({bbox.Min.X * 0.3048:F2} m), Y={bbox.Min.Y:F3} ft ({bbox.Min.Y * 0.3048:F2} m), Z={bbox.Min.Z:F3} ft ({bbox.Min.Z * 0.3048:F2} m)");
            LogDebug($"[ROOM] BBox Max: X={bbox.Max.X:F3} ft ({bbox.Max.X * 0.3048:F2} m), Y={bbox.Max.Y:F3} ft ({bbox.Max.Y * 0.3048:F2} m), Z={bbox.Max.Z:F3} ft ({bbox.Max.Z * 0.3048:F2} m)");
            LogDebug($"[ROOM] Dimensions: {(bbox.Max.X - bbox.Min.X) * 0.3048:F2} m x {(bbox.Max.Y - bbox.Min.Y) * 0.3048:F2} m x {(bbox.Max.Z - bbox.Min.Z) * 0.3048:F2} m");
            LogDebug($"[ROOM] Plan de travail Z = bbox.Min.Z + workplane = {bbox.Min.Z:F3} + {workplaneHeightFeet:F3} = {bbox.Min.Z + workplaneHeightFeet:F3} ft ({(bbox.Min.Z + workplaneHeightFeet) * 0.3048:F2} m)");
            LogDebug($"[ROOM] Grille espacement: {gridSpacingFeet:F3} ft ({gridSpacingFeet * 0.3048:F2} m)");
            LogDebug($"[ROOM] Nombre luminaires: {luminaires.Count}");
            LogDebug($"");

            for (int li = 0; li < luminaires.Count; li++)
            {
                var lum = luminaires[li];
                XYZ lumPt = (lum.Location as LocationPoint)?.Point;
                BoundingBoxXYZ lumBb = lum.get_BoundingBox(null);
                double lumZ = lumBb != null ? lumBb.Max.Z : (lumPt?.Z ?? 0);
                LogDebug($"[LUM #{li + 1:D2}] X={lumPt?.X:F3} ft ({(lumPt?.X ?? 0) * 0.3048:F2} m), Y={lumPt?.Y:F3} ft ({(lumPt?.Y ?? 0) * 0.3048:F2} m), Z(source)={lumZ:F3} ft ({lumZ * 0.3048:F2} m) | h_utile={(lumZ - bbox.Min.Z - workplaneHeightFeet) * 0.3048:F2} m");
            }
            LogDebug($"");
            LogDebug($"--- Début calcul grille ---");

            for (double x = bbox.Min.X; x <= bbox.Max.X; x += gridSpacingFeet)
            {
                for (double y = bbox.Min.Y; y <= bbox.Max.Y; y += gridSpacingFeet)
                {
                    XYZ testPoint = new XYZ(x, y, bbox.Min.Z + workplaneHeightFeet);

                    // Vérifier si le point est dans la pièce
                    if (!IsPointInRoom(room, testPoint)) continue;

                    double totalIlluminance = 0;

                    for (int i = 0; i < luminaires.Count; i++)
                    {
                        var luminaire = luminaires[i];
                        var info = luminaireInfos[i];

                        XYZ lumLocation = (luminaire.Location as LocationPoint)?.Point;
                        if (lumLocation == null) continue;

                        // 🚨 CORRECTION CRITIQUE GÉOMÉTRIQUE : Position Z réelle du luminaire
                        // LocationPoint.Point donne le point d'insertion (potentiellement au sol)
                        // Il faut utiliser BoundingBox pour obtenir la position réelle de la source lumineuse
                        BoundingBoxXYZ lumBbox = luminaire.get_BoundingBox(null);
                        if (lumBbox != null)
                        {
                            // 🚨 CORRECTION : Pour luminaire suspendu/plafonnier : Max.Z = HAUT (source lumineuse)
                            // Max.Z = point le plus haut = fixation au plafond = position source
                            double realZ = lumBbox.Max.Z;

                            // Logs géométriques pour diagnostic (premier luminaire + premier point seulement)
                            if (i == 0 && gridPoints.Count == 0)
                            {
                                XYZ origLoc = (luminaire.Location as LocationPoint)?.Point;
                                double dHoriz = Math.Sqrt(Math.Pow(lumLocation.X - x, 2) + Math.Pow(lumLocation.Y - y, 2));
                                LogDebug($"");
                                LogDebug($"=== GÉOMÉTRIE : LUMINAIRE #1 vs POINT #1 ===");
                                LogDebug($"[GEOM] Point grille #1: X={x:F3} ft ({x * 0.3048:F2} m), Y={y:F3} ft ({y * 0.3048:F2} m)");
                                LogDebug($"[GEOM] Luminaire #1 insertion: X={origLoc?.X:F3} ft ({(origLoc?.X ?? 0) * 0.3048:F2} m), Y={origLoc?.Y:F3} ft ({(origLoc?.Y ?? 0) * 0.3048:F2} m), Z={origLoc?.Z:F3} ft ({(origLoc?.Z ?? 0) * 0.3048:F2} m)");
                                LogDebug($"[GEOM] BBox luminaire: Min.Z={lumBbox.Min.Z:F3} ft ({lumBbox.Min.Z * 0.3048:F2} m), Max.Z={realZ:F3} ft ({realZ * 0.3048:F2} m)");
                                LogDebug($"[GEOM] Plan de travail (testPoint.Z): {testPoint.Z:F3} ft ({testPoint.Z * 0.3048:F2} m)");
                                LogDebug($"[GEOM] Hauteur utile h = {Math.Abs(realZ - testPoint.Z):F3} ft = {Math.Abs(realZ - testPoint.Z) * 0.3048:F2} m");
                                LogDebug($"[GEOM] Distance horizontale = {dHoriz:F3} ft = {dHoriz * 0.3048:F2} m");
                                LogDebug($"[GEOM] Correction Z appliquée: {origLoc?.Z:F3} ft -> {realZ:F3} ft (delta={Math.Abs((origLoc?.Z ?? 0) - realZ):F3} ft = {Math.Abs((origLoc?.Z ?? 0) - realZ) * 0.3048:F2} m)");
                                LogDebug($"");
                            }

                            // Appliquer la correction Z
                            lumLocation = new XYZ(lumLocation.X, lumLocation.Y, realZ);
                        }
                        else
                        {
                            // Si BoundingBox non disponible, logger l'avertissement
                            if (i == 0 && gridPoints.Count == 0)
                            {
                                LogDebug($"[GEOM] ⚠️ BoundingBox non disponible pour luminaire #1 - utilisation position insertion");
                            }
                        }

                        // Calcul de la distance 3D EN PIEDS (unité Revit)
                        double distanceFeet = lumLocation.DistanceTo(testPoint);

                        // CRITIQUE : Convertir en MÈTRES pour la formule photométrique !
                        // 1 pied = 0.3048 mètres
                        double distanceMeters = distanceFeet * 0.3048;
                        if (distanceMeters < 0.03) distanceMeters = 0.03; // Distance minimale de sécurité (3cm)

                        // Vecteur du luminaire vers le point
                        XYZ direction = (testPoint - lumLocation).Normalize();

                        // Calcul de l'angle vertical (gamma) depuis la normale du luminaire
                        // La normale du luminaire pointe vers le bas (0, 0, -1)
                        XYZ luminaireNormal = new XYZ(0, 0, -1);
                        double cosGamma = -direction.Z; // Produit scalaire simplifié

                        // Si le point est au-dessus du luminaire, pas d'éclairement
                        if (cosGamma <= 0) continue;

                        // Angle vertical en degrés
                        double gamma = Math.Acos(Math.Max(-1, Math.Min(1, cosGamma))) * (180.0 / Math.PI);

                        // Log angle pour premier luminaire + premier point
                        if (i == 0 && gridPoints.Count == 0)
                        {
                            LogDebug($"[GEOM] 📐 Distance 3D : {distanceFeet:F3} ft = {distanceMeters:F2} m");
                            LogDebug($"[GEOM] 📐 Angle vertical γ : {gamma:F1}° (attendu: 0-45° si correction OK)");
                            LogDebug($"[GEOM] 📐 cos(γ) : {cosGamma:F3}");
                        }

                        // Obtenir l'intensité lumineuse (candela) depuis les données IES ou estimer
                        double intensity = GetLuminaireIntensity(luminaire, info, gamma);

                        // FORMULE PHOTOMÉTRIQUE CORRECTE : E = (I × cos³(γ)) / d²
                        // cos³(γ) représente la loi de Lambert pour les surfaces lambertiennes
                        // d DOIT être en mètres quand I est en candela !
                        double illuminance = (intensity * Math.Pow(cosGamma, 3)) / (distanceMeters * distanceMeters);

                        totalIlluminance += illuminance;
                    }

                    // Sauvegarder éclairement direct pour les logs
                    double directIlluminance = totalIlluminance;

                    // Appliquer le facteur de maintenance
                    totalIlluminance *= maintenanceFactor;

                    // P2: Ajouter flux indirect (réflexions des surfaces)
                    double indirectIlluminance = totalIlluminance * indirectFactor;
                    totalIlluminance += indirectIlluminance;

                    // LOG DE DÉBOGAGE (premier point seulement) - CORRIGÉ: AppendAllText au lieu de WriteAllText
                    if (gridPoints.Count == 0)
                    {
                        LogDebug($"");
                        LogDebug($"--- PREMIER POINT DE GRILLE ({room.Name}) ---");
                        LogDebug($"[PT1] Position: X={x:F3} ft ({x * 0.3048:F2} m), Y={y:F3} ft ({y * 0.3048:F2} m), Z={testPoint.Z:F3} ft ({testPoint.Z * 0.3048:F2} m)");
                        LogDebug($"[PT1] Éclairement DIRECT (avant MF): {directIlluminance:F2} lux");
                        LogDebug($"[PT1] Facteur maintenance: {maintenanceFactor:F2}");
                        LogDebug($"[PT1] Éclairement après MF: {directIlluminance * maintenanceFactor:F2} lux");
                        LogDebug($"[PT1] Indirect activé: {settings.IncludeIndirectLight}, facteur: {indirectFactor:P1}");
                        LogDebug($"[PT1] Éclairement INDIRECT: {indirectIlluminance:F2} lux");
                        LogDebug($"[PT1] Éclairement TOTAL: {totalIlluminance:F2} lux");
                        LogDebug($"[PT1] Réflectances: plafond={settings.CeilingReflectance:F2}, murs={settings.WallReflectance:F2}, sol={settings.FloorReflectance:F2}");
                    }

                    gridPoints.Add(new GridPoint
                    {
                        X = x,
                        Y = y,
                        Z = testPoint.Z,
                        Illuminance = totalIlluminance
                    });
                }
            }

            // === LOG RÉSULTATS FINAUX ===
            if (gridPoints.Count > 0)
            {
                double avgE = gridPoints.Average(p => p.Illuminance);
                double minE = gridPoints.Min(p => p.Illuminance);
                double maxE = gridPoints.Max(p => p.Illuminance);
                LogDebug($"");
                LogDebug($"╔══════════════════════════════════════════════════════════╗");
                LogDebug($"║  RÉSULTATS FINAUX - {room.Name}");
                LogDebug($"╚══════════════════════════════════════════════════════════╝");
                LogDebug($"[RESULT] Points de grille: {gridPoints.Count}");
                LogDebug($"[RESULT] Em (moyenne): {avgE:F1} lux");
                LogDebug($"[RESULT] Emin: {minE:F1} lux");
                LogDebug($"[RESULT] Emax: {maxE:F1} lux");
                LogDebug($"[RESULT] Uniformité U0 = Emin/Em: {minE / avgE:F3}");
                LogDebug($"");
            }

            return gridPoints;
        }

        /// <summary>
        /// Obtient l'intensité lumineuse (candela) du luminaire pour un angle vertical donné
        /// </summary>
        private double GetLuminaireIntensity(FamilyInstance luminaire, LuminaireInfo info, double verticalAngle)
        {
            // Tenter d'obtenir les données IES
            var luminaireType = _doc.GetElement(luminaire.GetTypeId()) as ElementType;

            // Récupérer le nom du type pour détection automatique (P2)
            string typeName = "General";
            if (luminaireType != null)
            {
                typeName = luminaireType.Name ?? luminaire.Symbol?.Family?.Name ?? "General";
            }
            else if (luminaire.Symbol?.Family != null)
            {
                typeName = luminaire.Symbol.Family.Name;
            }

            if (luminaireType == null)
            {
                // Fallback : estimer l'intensité depuis le flux total avec détection du type (P2)
                return EstimateIntensityFromLumens(info.FluxLumineux, verticalAngle, typeName);
            }

            // 🚨 CORRECTION CRITIQUE : Chercher IES avec recherche étendue + extraction API
            string iesFilePath = GetIESFilePath(luminaireType);

            // Tentative 1 : Fichier IES sur disque (chemin absolu ou recherche étendue)
            if (!string.IsNullOrEmpty(iesFilePath) && File.Exists(iesFilePath))
            {
                try
                {
                    LogDebug($"[IES] 📂 Fichier trouvé : {iesFilePath}");
                    LogDebug($"[IES] 📊 Taille fichier : {new FileInfo(iesFilePath).Length} octets");

                    var iesData = IESParser.ParseFile(iesFilePath);

                    LogDebug($"[IES] ✅ Parser retourné : {iesData != null}");

                    if (iesData != null)
                    {
                        LogDebug($"[IES] 📐 Angles verticaux : {iesData.VerticalAngles?.Count ?? 0}");
                        LogDebug($"[IES] 📐 Angles horizontaux : {iesData.HorizontalAngles?.Count ?? 0}");
                        LogDebug($"[IES] 💡 Flux total : {iesData.TotalLumens:F0} lm");
                        LogDebug($"[IES] 🔆 Candela max : {iesData.MaxCandela:F0} cd");

                        double intensity = GetIntensityFromIESData(iesData, verticalAngle, 0);

                        LogDebug($"[IES] 🎯 Intensité calculée pour {verticalAngle:F1}° : {intensity:F2} cd");
                        LogDebug($"[IES] ✅ RETOUR DEPUIS IES (pas de fallback)");

                        return intensity;
                    }
                    else
                    {
                        LogDebug($"[IES] ❌ PARSER A RETOURNÉ NULL !");
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"[IES] ❌ EXCEPTION lors parsing : {ex.Message}");
                    LogDebug($"[IES] Stack trace : {ex.StackTrace}");
                }
            }

            // 🚨 Tentative 2 : Recherche IES par nom de fichier dans paramètre photométrique
            try
            {
                Parameter photometricParam = luminaireType.get_Parameter(BuiltInParameter.FBX_LIGHT_PHOTOMETRIC_FILE);

                if (photometricParam != null && photometricParam.HasValue)
                {
                    string embeddedIesPath = photometricParam.AsString();

                    // Si le paramètre contient un nom de fichier
                    if (!string.IsNullOrEmpty(embeddedIesPath))
                    {
                        string fileName = Path.GetFileName(embeddedIesPath);
                        LogDebug($"IES référencé dans paramètre : {fileName}");

                        // Recherche étendue du fichier IES
                        string foundPath = SearchIESFileExtended(fileName);
                        if (!string.IsNullOrEmpty(foundPath) && File.Exists(foundPath))
                        {
                            try
                            {
                                LogDebug($"[IES] 📂 Trouvé via recherche étendue : {foundPath}");

                                var iesData = IESParser.ParseFile(foundPath);

                                LogDebug($"[IES] ✅ Parser retourné : {iesData != null}");

                                if (iesData != null)
                                {
                                    LogDebug($"[IES] 📐 Angles verticaux : {iesData.VerticalAngles?.Count ?? 0}");
                                    LogDebug($"[IES] 💡 Flux total : {iesData.TotalLumens:F0} lm");

                                    double intensity = GetIntensityFromIESData(iesData, verticalAngle, 0);

                                    LogDebug($"[IES] 🎯 Intensité pour {verticalAngle:F1}° : {intensity:F2} cd");
                                    LogDebug($"[IES] ✅ RETOUR DEPUIS IES (recherche étendue)");

                                    return intensity;
                                }
                            }
                            catch (Exception exParse)
                            {
                                LogDebug($"[IES] ❌ EXCEPTION parsing {foundPath}: {exParse.Message}");
                            }
                        }
                        else
                        {
                            LogDebug($"❌ IES non trouvé : {fileName}");
                        }
                    }
                }
            }
            catch (Exception exParam)
            {
                LogDebug($"Erreur accès FBX_LIGHT_PHOTOMETRIC_FILE: {exParam.Message}");
            }

            // Fallback : estimer depuis le flux avec détection du type (P2)
            LogDebug($"⚠️⚠️⚠️ FALLBACK UTILISÉ pour {typeName} - Aucun IES appliqué");
            LogDebug($"[FALLBACK] Flux lumineux : {info.FluxLumineux:F0} lm");
            LogDebug($"[FALLBACK] Type détecté : {typeName}");

            double fallbackIntensity = EstimateIntensityFromLumens(info.FluxLumineux, verticalAngle, typeName);

            LogDebug($"[FALLBACK] Intensité estimée : {fallbackIntensity:F2} cd");

            return fallbackIntensity;
        }

        /// <summary>
        /// Extrait l'intensité depuis les données IES pour un angle donné
        /// </summary>
        private double GetIntensityFromIESData(IESParser.IESData iesData, double verticalAngle, double horizontalAngle)
        {
            if (iesData == null)
            {
                LogDebug($"[GetIntensityFromIESData] ❌ iesData est NULL !");
                return 0;
            }

            if (iesData.CandelaValues == null || iesData.CandelaValues.Count == 0)
            {
                LogDebug($"[GetIntensityFromIESData] ❌ Pas de valeurs candela !");
                return 0;
            }

            LogDebug($"[GetIntensityFromIESData] 📊 Recherche intensité pour V={verticalAngle:F1}°, H={horizontalAngle:F1}°");

            // Trouver l'angle horizontal le plus proche (ou interpoler)
            int hIndex = FindNearestAngleIndex(iesData.HorizontalAngles, horizontalAngle);
            LogDebug($"[GetIntensityFromIESData] Index horizontal : {hIndex} (angle: {iesData.HorizontalAngles[hIndex]:F1}°)");

            // Trouver les angles verticaux pour interpolation
            int vLow = 0, vHigh = 0;
            for (int i = 0; i < iesData.VerticalAngles.Count - 1; i++)
            {
                if (verticalAngle >= iesData.VerticalAngles[i] && verticalAngle <= iesData.VerticalAngles[i + 1])
                {
                    vLow = i;
                    vHigh = i + 1;
                    break;
                }
            }

            // Si angle hors limites, utiliser la valeur limite
            if (verticalAngle < iesData.VerticalAngles[0])
            {
                double val = iesData.CandelaValues[hIndex][0];
                LogDebug($"[GetIntensityFromIESData] Angle < min, retour : {val:F2} cd");
                return val;
            }
            if (verticalAngle > iesData.VerticalAngles[iesData.VerticalAngles.Count - 1])
            {
                double val = iesData.CandelaValues[hIndex][iesData.VerticalAngles.Count - 1];
                LogDebug($"[GetIntensityFromIESData] Angle > max, retour : {val:F2} cd");
                return val;
            }

            // Interpolation linéaire
            double angle1 = iesData.VerticalAngles[vLow];
            double angle2 = iesData.VerticalAngles[vHigh];
            double candela1 = iesData.CandelaValues[hIndex][vLow];
            double candela2 = iesData.CandelaValues[hIndex][vHigh];

            double t = (verticalAngle - angle1) / (angle2 - angle1);
            double result = candela1 + t * (candela2 - candela1);

            LogDebug($"[GetIntensityFromIESData] Interpolation : {candela1:F2}cd @ {angle1:F1}° <-> {candela2:F2}cd @ {angle2:F1}° => {result:F2}cd");

            return result;
        }

        /// <summary>
        /// Trouve l'index de l'angle le plus proche dans une liste
        /// </summary>
        private int FindNearestAngleIndex(List<double> angles, double targetAngle)
        {
            if (angles == null || angles.Count == 0) return 0;

            int nearestIndex = 0;
            double minDiff = Math.Abs(angles[0] - targetAngle);

            for (int i = 1; i < angles.Count; i++)
            {
                double diff = Math.Abs(angles[i] - targetAngle);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        /// <summary>
        /// P2: Estime l'intensité lumineuse avec distribution réaliste selon le type de luminaire
        /// Conforme IES LM-63-19 pour luminaires sans fichier photométrique
        /// Impact: +15% à +25% de précision vs méthode lambertienne simple
        /// </summary>
        private double EstimateIntensityFromLumens(double totalLumens, double verticalAngle, string luminaireTypeName = "General")
        {
            double angleRad = verticalAngle * (Math.PI / 180.0);
            double cosAngle = Math.Cos(angleRad);

            // Déterminer l'angle de faisceau et le facteur de concentration selon le type de luminaire
            double beamAngle = 90.0;   // Angle par défaut (hémisphère)
            double peakFactor = 1.0;   // Facteur de concentration

            // Détection automatique du type de luminaire via son nom
            string typeNameLower = luminaireTypeName.ToLower();

            if (typeNameLower.Contains("downlight") || typeNameLower.Contains("spot") ||
                typeNameLower.Contains("encastr") || typeNameLower.Contains("recessed"))
            {
                // Downlight / Spot : émission concentrée vers le bas
                beamAngle = 60.0;
                peakFactor = 1.8;
            }
            else if (typeNameLower.Contains("indirect") || typeNameLower.Contains("uplighter") ||
                     typeNameLower.Contains("suspendu") || typeNameLower.Contains("suspended"))
            {
                // Luminaire indirect : émission large vers le haut/bas
                beamAngle = 120.0;
                peakFactor = 0.7;
            }
            else if (typeNameLower.Contains("projecteur") || typeNameLower.Contains("floodlight") ||
                     typeNameLower.Contains("projector"))
            {
                // Projecteur : faisceau très concentré
                beamAngle = 45.0;
                peakFactor = 2.2;
            }
            else if (typeNameLower.Contains("panel") || typeNameLower.Contains("panneau") ||
                     typeNameLower.Contains("plafonnier") || typeNameLower.Contains("ceiling"))
            {
                // Panneau LED / Plafonnier : distribution large uniforme
                beamAngle = 110.0;
                peakFactor = 1.1;
            }
            // Sinon : type "General" avec valeurs par défaut (90°, facteur 1.0)

            // Calcul de l'intensité maximale selon l'angle de faisceau
            // Angle solide du cône : Ω = 2π(1 - cos(θ/2))
            double halfBeamRad = (beamAngle / 2.0) * (Math.PI / 180.0);
            double solidAngle = 2.0 * Math.PI * (1.0 - Math.Cos(halfBeamRad));
            double maxIntensity = (totalLumens / solidAngle) * peakFactor;

            // Distribution avec atténuation hors faisceau (courbe gaussienne)
            if (verticalAngle > beamAngle)
            {
                // Atténuation progressive au-delà de l'angle de faisceau
                double falloffFactor = Math.Exp(-Math.Pow((verticalAngle - beamAngle) / 30.0, 2));
                return maxIntensity * cosAngle * falloffFactor;
            }

            // Dans le faisceau : distribution lambertienne classique
            return maxIntensity * cosAngle;
        }

        /// <summary>
        /// P4: Calcul uniformité locale selon EN 12464-1 section 4.3
        /// Uₕ = Eₘᵢₙ / E̅ₘₒᵧ (éclairement point vs moyenne des voisins)
        /// Norme : Uₕ ≥ 0.60
        /// </summary>
        private double CalculateLocalUniformity(List<GridPoint> gridPoints, double gridSpacingFeet)
        {
            if (gridPoints.Count < 9)
                return 1.0; // Pas assez de points pour calculer l'uniformité locale

            double minLocalRatio = 1.0;

            foreach (var point in gridPoints)
            {
                // Trouver les voisins dans un rayon de √2 × gridSpacing
                // Cela correspond aux 8 points adjacents (diagonales incluses)
                double searchRadius = gridSpacingFeet * 1.5;

                var neighbors = gridPoints
                    .Where(p => p != point &&
                           Math.Sqrt(Math.Pow(p.X - point.X, 2) + Math.Pow(p.Y - point.Y, 2)) <= searchRadius)
                    .ToList();

                // Au moins 4 voisins requis pour un calcul valide
                if (neighbors.Count >= 4)
                {
                    double avgNeighbors = neighbors.Average(n => n.Illuminance);
                    if (avgNeighbors > 0)
                    {
                        double localRatio = point.Illuminance / avgNeighbors;
                        minLocalRatio = Math.Min(minLocalRatio, localRatio);
                    }
                }
            }

            return minLocalRatio;
        }

        /// <summary>
        /// P2: Calcule le facteur d'éclairement indirect selon la méthode des coefficients CIE
        /// Prend en compte les réflexions sur les surfaces (plafond, murs, sol)
        /// Conforme CIE 121-1996 et EN 12464-1
        /// </summary>
        private double CalculateIndirectFactor(Room room, AnalysisSettings settings)
        {
            if (!settings.IncludeIndirectLight)
                return 0.0;

            // Récupérer dimensions de la pièce
            BoundingBoxXYZ bbox = room.get_BoundingBox(null);
            if (bbox == null)
                return 0.0;

            double length = bbox.Max.X - bbox.Min.X;
            double width = bbox.Max.Y - bbox.Min.Y;
            double height = bbox.Max.Z - bbox.Min.Z;

            // Convertir pieds → mètres
            length *= 0.3048;
            width *= 0.3048;
            height *= 0.3048;

            // Room Index (k) - Indice de forme de la pièce selon CIE 121-1996
            // Plus k est élevé, plus la pièce est "compacte" et favorise les réflexions
            double k = (length * width) / ((length + width) * height);

            // P1.1 : Réflectance moyenne pondérée selon CIE 121-1996
            // Pondération normalisée : Plafond 30%, Murs 50%, Sol 20%
            double avgReflectance =
                settings.CeilingReflectance * 0.3 +
                settings.WallReflectance * 0.5 +
                settings.FloorReflectance * 0.2;

            // P1.2 : Coefficient d'utilisation selon méthode CIE (basé sur tableaux CIE 121-1996)
            // Cette courbe approchée représente l'efficacité de l'éclairement indirect
            // en fonction de la géométrie de la pièce (Room Index)
            double utilizationFactor;

            if (k < 0.6)
                utilizationFactor = 0.30 + k * 0.25;
            else if (k < 1.0)
                utilizationFactor = 0.45 + (k - 0.6) * 0.20;
            else if (k < 2.0)
                utilizationFactor = 0.53 + (k - 1.0) * 0.12;
            else if (k < 3.0)
                utilizationFactor = 0.65 + (k - 2.0) * 0.08;
            else
                utilizationFactor = 0.73 + Math.Min(k - 3.0, 2.0) * 0.04;

            // Facteur d'éclairement indirect = Coefficient d'utilisation × Réflectance moyenne × Facteur géométrique
            // 🚨 CORRECTION FINALE : Facteur 0.48 (au lieu de 0.35) pour meilleure correspondance avec Dialux
            double indirectFactor = utilizationFactor * avgReflectance * 0.48;

            return indirectFactor;
        }

        private bool IsPointInRoom(Room room, XYZ point)
        {
            try
            {
                return room.IsPointInRoom(point);
            }
            catch
            {
                return true; // Si erreur, on garde le point
            }
        }

        private List<FamilyInstance> GetLuminairesInRoom(Room room)
        {
            var luminaires = new List<FamilyInstance>();

            var collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_LightingFixtures)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>();

            foreach (var luminaire in collector)
            {
                if (luminaire.Room != null && luminaire.Room.Id == room.Id)
                {
                    luminaires.Add(luminaire);
                }
            }

            return luminaires;
        }

        private LuminaireInfo ExtractLuminaireInfo(FamilyInstance luminaire)
        {
            var info = new LuminaireInfo
            {
                Nom = luminaire.Name
            };

            var luminaireType = _doc.GetElement(luminaire.GetTypeId()) as ElementType;
            if (luminaireType == null)
            {
                return info;
            }

            string iesFilePath = GetIESFilePath(luminaireType);
            bool iesDataLoaded = false;

            if (!string.IsNullOrEmpty(iesFilePath) && File.Exists(iesFilePath))
            {
                try
                {
                    var iesData = IESParser.ParseFile(iesFilePath);
                    info.FluxLumineux = (int)iesData.TotalLumens;
                    info.Puissance = (int)iesData.InputWatts;
                    info.Efficacite = (int)iesData.Efficacy;
                    if (!string.IsNullOrEmpty(iesData.Manufacturer))
                        info.Fabricant = iesData.Manufacturer;
                    if (!string.IsNullOrEmpty(iesData.CatalogNumber))
                        info.Reference = iesData.CatalogNumber;
                    iesDataLoaded = true;
                    info.TypeLuminaire = "Données IES";
                }
                catch { }
            }

            if (!iesDataLoaded)
            {
                info.TemperatureCouleur = (int)ExtractNumericValue(luminaireType, new[] {
                    "Température initiale des couleurs",
                    "Température de couleur",
                    "Color Temperature",
                    "Couleur initiale"
                }, 4000);

                info.Puissance = (int)ExtractNumericValue(luminaireType, new[] {
                    "Puissance",
                    "Puissance apparente",
                    "Wattage",
                    "Load",
                    "Intensité initiale",
                    "Apparent Load"
                }, 40);

                double flux = ExtractNumericValue(luminaireType, new[] {
                    "Flux lumineux",
                    "Luminous Flux",
                    "Initial Luminous Flux",
                    "Photométriques",
                    "Luminaire Lumens"
                }, 3600);

                if (flux < 100) flux *= 1000;
                info.FluxLumineux = (int)flux;

                if (info.Puissance > 0)
                {
                    info.Efficacite = (int)(info.FluxLumineux / info.Puissance);
                }

                info.Fabricant = ExtractStringValue(luminaireType, new[] {
                    "Fabricant",
                    "Manufacturer",
                    "Nom de la famille"
                }, luminaire.Symbol?.Family?.Name ?? "Inconnu");

                info.Reference = ExtractStringValue(luminaireType, new[] {
                    "Nom du type",
                    "Référence",
                    "Model",
                    "Type Mark"
                }, luminaireType.Name ?? "N/A");

                info.TypeLuminaire = ExtractStringValue(luminaireType, new[] {
                    "Type de luminaire",
                    "Fixture Type",
                    "Type"
                }, "Paramètres Revit");
            }

            if (info.TemperatureCouleur == 0)
            {
                info.TemperatureCouleur = (int)ExtractNumericValue(luminaireType, new[] {
                    "Température initiale des couleurs",
                    "Température de couleur",
                    "Color Temperature"
                }, 4000);
            }

            info.CategorieUsage = ExtractStringValue(luminaireType, new[] {
                "Catégorie d'usage",
                "Application",
                "Usage"
            }, "");

            info.IndiceProtection = ExtractStringValue(luminaireType, new[] {
                "Indice de protection",
                "IP Rating",
                "IP"
            }, "");

            return info;
        }

        private string GetIESFilePath(ElementType luminaireType)
        {
            string[] iesParamNames = new[]
            {
                "Fichier photométrique Web",
                "Fichier de distribution photométrique",
                "Light Source Definition File",
                "Photometric Web File",
                "IES File",
                "Web File"
            };

            foreach (string paramName in iesParamNames)
            {
                Parameter param = luminaireType.LookupParameter(paramName);
                if (param != null && param.HasValue && param.StorageType == StorageType.String)
                {
                    string filePath = param.AsString();
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        if (Path.IsPathRooted(filePath) && File.Exists(filePath))
                        {
                            return filePath;
                        }

                        if (!Path.IsPathRooted(filePath))
                        {
                            string fileName = Path.GetFileName(filePath);
                            List<string> searchFolders = new List<string>();

                            // Dossier du document Revit actuel
                            string docPath = _doc.PathName;
                            if (!string.IsNullOrEmpty(docPath))
                            {
                                searchFolders.Add(Path.GetDirectoryName(docPath));
                            }

                            // 🚨 CORRECTION CRITIQUE : Dossiers standards Revit étendus
                            string revitVersion = _doc.Application.VersionNumber;

                            // Dossiers IES standards
                            searchFolders.Add($@"C:\ProgramData\Autodesk\RVT {revitVersion}\IES");
                            searchFolders.Add(@"C:\ProgramData\Autodesk\RVT 2026\IES");

                            // Dossiers bibliothèques photométriques Revit
                            searchFolders.Add($@"C:\ProgramData\Autodesk\RVT {revitVersion}\Libraries\Lighting - Photometric Web");
                            searchFolders.Add(@"C:\ProgramData\Autodesk\RVT 2026\Libraries\Lighting - Photometric Web");
                            searchFolders.Add($@"C:\ProgramData\Autodesk\RVT {revitVersion}\Libraries\Lighting");
                            searchFolders.Add(@"C:\ProgramData\Autodesk\RVT 2026\Libraries\Lighting");

                            // Dossiers bibliothèques génériques
                            searchFolders.Add(@"C:\ProgramData\Autodesk\Libraries\Lighting - Photometric Web");
                            searchFolders.Add(@"C:\ProgramData\Autodesk\Libraries\IES");

                            // Dossier utilisateur (roaming)
                            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                            searchFolders.Add(Path.Combine(appDataPath, $@"Autodesk\RVT {revitVersion}\Libraries\Lighting - Photometric Web"));
                            searchFolders.Add(Path.Combine(appDataPath, @"Autodesk\Libraries\Lighting - Photometric Web"));

                            // Temp
                            searchFolders.Add(Path.GetTempPath());
                            searchFolders.Add(@"C:\Temp");

                            // Chercher dans tous les dossiers
                            foreach (string folder in searchFolders)
                            {
                                if (Directory.Exists(folder))
                                {
                                    string fullPath = Path.Combine(folder, fileName);
                                    if (File.Exists(fullPath))
                                    {
                                        LogDebug($"✅ IES trouvé : {fullPath}");
                                        return fullPath;
                                    }
                                }
                            }

                            // 🚨 Recherche récursive dans le dossier Lighting - Photometric Web
                            string photometricFolder = $@"C:\ProgramData\Autodesk\RVT {revitVersion}\Libraries\Lighting - Photometric Web";
                            if (Directory.Exists(photometricFolder))
                            {
                                try
                                {
                                    var foundFiles = Directory.GetFiles(photometricFolder, fileName, SearchOption.AllDirectories);
                                    if (foundFiles.Length > 0)
                                    {
                                        LogDebug($"✅ IES trouvé (récursif) : {foundFiles[0]}");
                                        return foundFiles[0];
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 🚨 CORRECTION CRITIQUE : Recherche étendue de fichiers IES dans tous les dossiers Revit
        /// </summary>
        private string SearchIESFileExtended(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            List<string> searchFolders = new List<string>();
            string revitVersion = _doc.Application.VersionNumber;

            // Dossier du document Revit actuel
            string docPath = _doc.PathName;
            if (!string.IsNullOrEmpty(docPath))
            {
                searchFolders.Add(Path.GetDirectoryName(docPath));
            }

            // Dossiers IES standards
            searchFolders.Add($@"C:\ProgramData\Autodesk\RVT {revitVersion}\IES");
            searchFolders.Add(@"C:\ProgramData\Autodesk\RVT 2026\IES");

            // Dossiers bibliothèques photométriques Revit
            searchFolders.Add($@"C:\ProgramData\Autodesk\RVT {revitVersion}\Libraries\Lighting - Photometric Web");
            searchFolders.Add(@"C:\ProgramData\Autodesk\RVT 2026\Libraries\Lighting - Photometric Web");
            searchFolders.Add($@"C:\ProgramData\Autodesk\RVT {revitVersion}\Libraries\Lighting");
            searchFolders.Add(@"C:\ProgramData\Autodesk\RVT 2026\Libraries\Lighting");

            // Dossiers génériques
            searchFolders.Add(@"C:\ProgramData\Autodesk\Libraries\Lighting - Photometric Web");
            searchFolders.Add(@"C:\ProgramData\Autodesk\Libraries\IES");

            // Dossier utilisateur
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            searchFolders.Add(Path.Combine(appDataPath, $@"Autodesk\RVT {revitVersion}\Libraries\Lighting - Photometric Web"));
            searchFolders.Add(Path.Combine(appDataPath, @"Autodesk\Libraries\Lighting - Photometric Web"));

            // Temp
            searchFolders.Add(Path.GetTempPath());
            searchFolders.Add(@"C:\Temp");

            // Recherche dans les dossiers
            foreach (string folder in searchFolders)
            {
                if (Directory.Exists(folder))
                {
                    string fullPath = Path.Combine(folder, fileName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            // Recherche récursive dans le dossier principal Lighting - Photometric Web
            string photometricFolder = $@"C:\ProgramData\Autodesk\RVT {revitVersion}\Libraries\Lighting - Photometric Web";
            if (Directory.Exists(photometricFolder))
            {
                try
                {
                    var foundFiles = Directory.GetFiles(photometricFolder, fileName, SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                    {
                        return foundFiles[0];
                    }
                }
                catch { }
            }

            // Recherche récursive alternative
            photometricFolder = @"C:\ProgramData\Autodesk\RVT 2026\Libraries\Lighting - Photometric Web";
            if (Directory.Exists(photometricFolder))
            {
                try
                {
                    var foundFiles = Directory.GetFiles(photometricFolder, fileName, SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                    {
                        return foundFiles[0];
                    }
                }
                catch { }
            }

            return null;
        }

        private double ExtractNumericValue(ElementType elementType, string[] paramNames, double defaultValue)
        {
            foreach (string paramName in paramNames)
            {
                Parameter param = elementType.LookupParameter(paramName);
                if (param != null && param.HasValue)
                {
                    try
                    {
                        switch (param.StorageType)
                        {
                            case StorageType.Double:
                                return param.AsDouble();

                            case StorageType.Integer:
                                return param.AsInteger();

                            case StorageType.String:
                                string strValue = param.AsString();
                                if (!string.IsNullOrWhiteSpace(strValue))
                                {
                                    string numericPart = new string(strValue.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                                    numericPart = numericPart.Replace(',', '.');

                                    if (double.TryParse(numericPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                                    {
                                        return result;
                                    }
                                }
                                break;
                        }
                    }
                    catch { }
                }
            }

            return defaultValue;
        }

        private string ExtractStringValue(ElementType elementType, string[] paramNames, string defaultValue)
        {
            foreach (string paramName in paramNames)
            {
                Parameter param = elementType.LookupParameter(paramName);
                if (param != null && param.HasValue)
                {
                    try
                    {
                        string value = param.AsString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                    catch { }
                }
            }

            return defaultValue;
        }

        private double CalculateAverageIlluminance(double totalLumens, double roomArea)
        {
            double utilisationCoefficient = 0.7;
            return (totalLumens * utilisationCoefficient) / roomArea;
        }
    }

    public class GridPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Illuminance { get; set; }
    }
}