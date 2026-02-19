using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitLightingPlugin.Core;

namespace RevitLightingPlugin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class DiagnosticLuminaireCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Logger.Separator("DIAGNOSTIC LUMINAIRE");
            Logger.Info("DiagnosticLuminaire", "🔍 Commande de diagnostic lancée");

            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Demander à l'utilisateur de sélectionner un luminaire
                Reference reference = null;
                try
                {
                    reference = uidoc.Selection.PickObject(
                        ObjectType.Element,
                        new LuminaireSelectionFilter(),
                        "Sélectionnez un luminaire à diagnostiquer"
                    );
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    Logger.Warning("DiagnosticLuminaire", "Sélection annulée par l'utilisateur");
                    return Result.Cancelled;
                }

                if (reference == null)
                {
                    TaskDialog.Show("Erreur", "Aucun luminaire sélectionné.");
                    return Result.Failed;
                }

                // Récupérer le luminaire
                Element element = doc.GetElement(reference);
                FamilyInstance luminaire = element as FamilyInstance;

                if (luminaire == null)
                {
                    TaskDialog.Show("Erreur", "L'élément sélectionné n'est pas un luminaire.");
                    return Result.Failed;
                }

                Logger.Info("DiagnosticLuminaire", $"Luminaire sélectionné : {luminaire.Name} (ID: {luminaire.Id})");

                // Générer le diagnostic complet
                string diagnosticReport = GenerateDiagnosticReport(luminaire, doc);

                // Afficher dans une fenêtre
                var reportWindow = new Window
                {
                    Title = $"Diagnostic Luminaire - {luminaire.Name}",
                    Width = 900,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var scrollViewer = new System.Windows.Controls.ScrollViewer
                {
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    Padding = new Thickness(20)
                };

                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = diagnosticReport,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.NoWrap
                };

                scrollViewer.Content = textBlock;
                reportWindow.Content = scrollViewer;

                reportWindow.ShowDialog();

                // Écrire aussi dans les logs
                Logger.Info("DiagnosticLuminaire", "Rapport généré avec succès");
                Logger.Separator();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("DiagnosticLuminaire", "Erreur lors du diagnostic", ex);
                TaskDialog.Show("Erreur", $"Erreur lors du diagnostic :\n{ex.Message}");
                return Result.Failed;
            }
        }

        private string GenerateDiagnosticReport(FamilyInstance luminaire, Document doc)
        {
            var sb = new StringBuilder();

            sb.AppendLine("╔════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║                    DIAGNOSTIC COMPLET DU LUMINAIRE                     ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Date : {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Document : {doc.Title}");
            sb.AppendLine();

            // ═══════════════════════════════════════════════════════════════════════
            // 1. INFORMATIONS GÉNÉRALES
            // ═══════════════════════════════════════════════════════════════════════
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("1. INFORMATIONS GÉNÉRALES");
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine($"ID Revit           : {luminaire.Id.Value}");
            sb.AppendLine($"Nom instance       : {luminaire.Name}");
            sb.AppendLine($"Catégorie          : {luminaire.Category?.Name}");

            var familySymbol = luminaire.Symbol;
            if (familySymbol != null)
            {
                sb.AppendLine($"Type               : {familySymbol.Name}");
                sb.AppendLine($"Famille            : {familySymbol.FamilyName}");
            }

            sb.AppendLine();

            // ═══════════════════════════════════════════════════════════════════════
            // 2. POSITION ET GÉOMÉTRIE
            // ═══════════════════════════════════════════════════════════════════════
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("2. POSITION ET GÉOMÉTRIE");
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");

            // LocationPoint
            LocationPoint locPoint = luminaire.Location as LocationPoint;
            if (locPoint != null)
            {
                XYZ point = locPoint.Point;
                sb.AppendLine("📍 LocationPoint (Point d'insertion) :");
                sb.AppendLine($"   X = {point.X:F3} ft ({point.X * 0.3048:F3} m)");
                sb.AppendLine($"   Y = {point.Y:F3} ft ({point.Y * 0.3048:F3} m)");
                sb.AppendLine($"   Z = {point.Z:F3} ft ({point.Z * 0.3048:F3} m) ⬅ Point d'insertion");
                sb.AppendLine();
            }

            // BoundingBox
            BoundingBoxXYZ bbox = luminaire.get_BoundingBox(null);
            if (bbox != null)
            {
                sb.AppendLine("📦 BoundingBox (Boîte englobante) :");
                sb.AppendLine($"   Min.X = {bbox.Min.X:F3} ft ({bbox.Min.X * 0.3048:F3} m)");
                sb.AppendLine($"   Min.Y = {bbox.Min.Y:F3} ft ({bbox.Min.Y * 0.3048:F3} m)");
                sb.AppendLine($"   Min.Z = {bbox.Min.Z:F3} ft ({bbox.Min.Z * 0.3048:F3} m) ⬅ BAS du luminaire");
                sb.AppendLine();
                sb.AppendLine($"   Max.X = {bbox.Max.X:F3} ft ({bbox.Max.X * 0.3048:F3} m)");
                sb.AppendLine($"   Max.Y = {bbox.Max.Y:F3} ft ({bbox.Max.Y * 0.3048:F3} m)");
                sb.AppendLine($"   Max.Z = {bbox.Max.Z:F3} ft ({bbox.Max.Z * 0.3048:F3} m) ⬅ HAUT du luminaire");
                sb.AppendLine();

                // Dimensions
                double width = (bbox.Max.X - bbox.Min.X) * 0.3048;
                double depth = (bbox.Max.Y - bbox.Min.Y) * 0.3048;
                double height = (bbox.Max.Z - bbox.Min.Z) * 0.3048;

                sb.AppendLine("📐 Dimensions du luminaire :");
                sb.AppendLine($"   Largeur (X) = {width:F3} m");
                sb.AppendLine($"   Profondeur (Y) = {depth:F3} m");
                sb.AppendLine($"   Hauteur (Z) = {height:F3} m");
                sb.AppendLine();

                // Centre
                double centerZ = (bbox.Min.Z + bbox.Max.Z) / 2.0;
                sb.AppendLine("🎯 POSITIONS CALCULÉES :");
                sb.AppendLine($"   Centre Z (approx) = {centerZ:F3} ft ({centerZ * 0.3048:F3} m) ⬅ SOURCE PROBABLE");
                sb.AppendLine();

                // Comparaison
                sb.AppendLine("⚡ ANALYSE POUR CALCULS PHOTOMÉTRIQUES :");
                if (height > 0.3) // Plus de 30cm
                {
                    sb.AppendLine($"   ✅ Luminaire ÉPAIS ({height:F2}m) → Utiliser CENTRE ({centerZ * 0.3048:F3}m)");
                }
                else
                {
                    sb.AppendLine($"   ℹ️  Luminaire PLAT ({height:F2}m) → Utiliser MAX.Z ({bbox.Max.Z * 0.3048:F3}m)");
                }

                double deltaMaxCenter = Math.Abs(bbox.Max.Z - centerZ) * 0.3048;
                sb.AppendLine($"   Différence Max.Z vs Centre = {deltaMaxCenter:F3} m");
                sb.AppendLine($"   Impact sur éclairement = {Math.Pow((centerZ / bbox.Max.Z), 2):P1}");
                sb.AppendLine();
            }

            // ═══════════════════════════════════════════════════════════════════════
            // 3. PARAMÈTRES DU TYPE
            // ═══════════════════════════════════════════════════════════════════════
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("3. PARAMÈTRES DU TYPE (FamilySymbol)");
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");

            if (familySymbol != null)
            {
                var typeParams = familySymbol.Parameters.Cast<Parameter>()
                    .OrderBy(p => p.Definition.Name);

                foreach (Parameter param in typeParams)
                {
                    string value = GetParameterValueAsString(param);
                    string name = param.Definition.Name;
                    sb.AppendLine($"   {name,-45} = {value}");
                }
            }
            sb.AppendLine();

            // ═══════════════════════════════════════════════════════════════════════
            // 4. PARAMÈTRES DE L'INSTANCE
            // ═══════════════════════════════════════════════════════════════════════
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("4. PARAMÈTRES DE L'INSTANCE");
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");

            var instanceParams = luminaire.Parameters.Cast<Parameter>()
                .Where(p => !p.IsReadOnly)
                .OrderBy(p => p.Definition.Name);

            foreach (Parameter param in instanceParams)
            {
                string value = GetParameterValueAsString(param);
                string name = param.Definition.Name;
                sb.AppendLine($"   {name,-45} = {value}");
            }
            sb.AppendLine();

            // ═══════════════════════════════════════════════════════════════════════
            // 5. DONNÉES PHOTOMÉTRIQUES
            // ═══════════════════════════════════════════════════════════════════════
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("5. DONNÉES PHOTOMÉTRIQUES");
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");

            // Chercher fichier IES - essayer tous les noms possibles
            Parameter iesParam = null;
            string[] possibleIESParamNames = new string[]
            {
                "Fichier de distribution photométrique",  // Revit FR 2026 ✅
                "IES File",
                "Photometric Web File",
                "Light Source Definition File",
                "Fichier IES",
                "Fichier photométrique",
                "Web photométrique",
                "IES",
                "Photometric File",
                "Light Distribution",
                "Distribution lumineuse",
                "Source lumineuse",
                "Type Source",
                "Photometric Distribution File"
            };

            // Essayer tous les noms possibles
            foreach (string paramName in possibleIESParamNames)
            {
                iesParam = familySymbol?.LookupParameter(paramName);
                if (iesParam != null && iesParam.HasValue)
                {
                    sb.AppendLine($"✅ Paramètre IES trouvé : '{paramName}'");
                    break;
                }
            }

            // Si toujours pas trouvé, chercher dans TOUS les paramètres qui contiennent ".ies"
            if (iesParam == null && familySymbol != null)
            {
                sb.AppendLine("🔍 Recherche dans tous les paramètres contenant '.ies' ou 'IES' :");
                foreach (Parameter param in familySymbol.Parameters)
                {
                    if (param.HasValue && param.StorageType == StorageType.String)
                    {
                        string value = param.AsString();
                        if (!string.IsNullOrEmpty(value) &&
                            (value.ToLower().Contains(".ies") ||
                             param.Definition.Name.ToLower().Contains("ies") ||
                             param.Definition.Name.ToLower().Contains("photometric")))
                        {
                            sb.AppendLine($"   • {param.Definition.Name} = {value}");
                            if (iesParam == null && value.ToLower().EndsWith(".ies"))
                            {
                                iesParam = param; // Prendre le premier qui finit par .ies
                            }
                        }
                    }
                }
                sb.AppendLine();
            }

            if (iesParam != null && iesParam.HasValue)
            {
                string iesPath = iesParam.AsString();
                sb.AppendLine($"📄 Fichier IES : {iesPath}");

                if (System.IO.File.Exists(iesPath))
                {
                    sb.AppendLine($"   ✅ Fichier trouvé sur disque");
                    try
                    {
                        var iesData = IESParser.ParseFile(iesPath);
                        sb.AppendLine();
                        sb.AppendLine("📊 Données IES parsées :");
                        sb.AppendLine($"   Fabricant      : {iesData.Manufacturer ?? "(non spécifié)"}");
                        sb.AppendLine($"   Référence      : {iesData.CatalogNumber ?? "(non spécifié)"}");
                        sb.AppendLine($"   Flux lumineux  : {iesData.TotalLumens:F0} lm");
                        sb.AppendLine($"   Puissance      : {iesData.InputWatts:F0} W");
                        if (iesData.InputWatts > 0)
                        {
                            double efficacy = iesData.TotalLumens / iesData.InputWatts;
                            sb.AppendLine($"   Efficacité     : {efficacy:F0} lm/W");
                        }
                        sb.AppendLine($"   Nb lampes      : {iesData.NumberOfLamps}");
                        sb.AppendLine($"   Lm/lampe       : {iesData.LumensPerLamp:F0} lm");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"   ❌ Erreur parsing IES : {ex.Message}");
                    }
                }
                else
                {
                    sb.AppendLine($"   ⚠️  Fichier non trouvé sur disque");
                }
            }
            else
            {
                sb.AppendLine("⚠️  Pas de fichier IES attaché");
            }

            sb.AppendLine();

            // ═══════════════════════════════════════════════════════════════════════
            // 6. RECOMMANDATIONS
            // ═══════════════════════════════════════════════════════════════════════
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("6. RECOMMANDATIONS POUR CALCULS PHOTOMÉTRIQUES");
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");

            if (bbox != null)
            {
                double height = (bbox.Max.Z - bbox.Min.Z) * 0.3048;
                double centerZ = (bbox.Min.Z + bbox.Max.Z) / 2.0;

                sb.AppendLine("Pour le paramètre 'realZ' dans LightingCalculator.cs :");
                sb.AppendLine();

                if (height > 0.3)
                {
                    sb.AppendLine($"✅ RECOMMANDATION : Utiliser le CENTRE de la BoundingBox");
                    sb.AppendLine($"   Code : double realZ = (lumBbox.Min.Z + lumBbox.Max.Z) / 2.0;");
                    sb.AppendLine($"   Valeur : {centerZ:F3} ft = {centerZ * 0.3048:F3} m");
                    sb.AppendLine();
                    sb.AppendLine($"   Raison : Luminaire épais ({height:F2}m), la source est probablement au centre");
                }
                else
                {
                    sb.AppendLine($"ℹ️  RECOMMANDATION : Utiliser Max.Z (luminaire plat/encastré)");
                    sb.AppendLine($"   Code : double realZ = lumBbox.Max.Z;");
                    sb.AppendLine($"   Valeur : {bbox.Max.Z:F3} ft = {bbox.Max.Z * 0.3048:F3} m");
                }

                sb.AppendLine();
                sb.AppendLine("Alternative : Ajouter un paramètre 'Light Source Offset' à la famille");
                sb.AppendLine("pour spécifier la position exacte de la source.");
            }

            sb.AppendLine();
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");
            sb.AppendLine("FIN DU DIAGNOSTIC");
            sb.AppendLine("════════════════════════════════════════════════════════════════════════");

            // Logger le rapport complet
            Logger.Info("DiagnosticLuminaire", "=== RAPPORT COMPLET ===");
            foreach (var line in sb.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                Logger.Debug("DiagnosticLuminaire", line);
            }

            return sb.ToString();
        }

        private string GetParameterValueAsString(Parameter param)
        {
            if (param == null || !param.HasValue)
                return "(vide)";

            try
            {
                switch (param.StorageType)
                {
                    case StorageType.Double:
                        double value = param.AsDouble();
                        // Vérifier le type de paramètre
                        string paramName = param.Definition.Name.ToLower();
                        if (paramName.Contains("height") || paramName.Contains("length") ||
                            paramName.Contains("width") || paramName.Contains("depth") ||
                            paramName.Contains("offset") || paramName.Contains("hauteur"))
                            return $"{value:F3} ft ({value * 0.3048:F3} m)";
                        else if (paramName.Contains("angle"))
                            return $"{value * 180 / Math.PI:F2}°";
                        else
                            return value.ToString("F3");

                    case StorageType.Integer:
                        return param.AsInteger().ToString();

                    case StorageType.String:
                        return param.AsString() ?? "(vide)";

                    case StorageType.ElementId:
                        ElementId id = param.AsElementId();
                        if (id == ElementId.InvalidElementId)
                            return "(aucun)";
                        return id.Value.ToString();

                    default:
                        return param.AsValueString() ?? "(inconnu)";
                }
            }
            catch
            {
                return "(erreur lecture)";
            }
        }
    }

    /// <summary>
    /// Filtre pour ne sélectionner que des luminaires
    /// </summary>
    public class LuminaireSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category?.Id.Value == (long)BuiltInCategory.OST_LightingFixtures;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
