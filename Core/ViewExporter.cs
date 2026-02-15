using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace RevitLightingPlugin.Core
{
    public class ViewExporter
    {
        private readonly Document _doc;
        private readonly string _exportFolder;

        public ViewExporter(Document doc, string exportFolder)
        {
            _doc = doc;
            _exportFolder = exportFolder;

            if (!Directory.Exists(_exportFolder))
            {
                Directory.CreateDirectory(_exportFolder);
            }
        }

        public RoomViewsExport ExportRoomViews(Room room)
        {
            var result = new RoomViewsExport
            {
                RoomId = room.Id,
                RoomName = room.Name,
                Success = false
            };

            try
            {
                string planPath = ExportPlanView(room);
                if (!string.IsNullOrEmpty(planPath) && File.Exists(planPath))
                {
                    result.PlanImagePath = planPath;
                    result.Success = true;
                }

                string view3DPath = Export3DView(room);
                if (!string.IsNullOrEmpty(view3DPath) && File.Exists(view3DPath))
                {
                    result.View3DImagePath = view3DPath;
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private string ExportPlanView(Room room)
        {
            try
            {
                using (Transaction trans = new Transaction(_doc, "Export Plan View"))
                {
                    trans.Start();

                    Level roomLevel = _doc.GetElement(room.LevelId) as Level;
                    if (roomLevel == null)
                    {
                        trans.RollBack();
                        return null;
                    }

                    ViewPlan planView = new FilteredElementCollector(_doc)
                        .OfClass(typeof(ViewPlan))
                        .Cast<ViewPlan>()
                        .FirstOrDefault(v => v.GenLevel != null && v.GenLevel.Id == roomLevel.Id && v.ViewType == ViewType.FloorPlan);

                    if (planView == null)
                    {
                        ViewFamilyType vft = new FilteredElementCollector(_doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(x => x.ViewFamily == ViewFamily.FloorPlan);

                        if (vft != null)
                        {
                            planView = ViewPlan.Create(_doc, vft.Id, roomLevel.Id);
                        }
                    }

                    if (planView == null)
                    {
                        trans.RollBack();
                        return null;
                    }

                    // Cadrer la vue sur la pièce
                    BoundingBoxXYZ bbox = room.get_BoundingBox(planView);
                    if (bbox != null)
                    {
                        double margin = 5.0;
                        bbox.Min = new XYZ(bbox.Min.X - margin, bbox.Min.Y - margin, bbox.Min.Z);
                        bbox.Max = new XYZ(bbox.Max.X + margin, bbox.Max.Y + margin, bbox.Max.Z);

                        planView.CropBox = bbox;
                        planView.CropBoxActive = true;
                        planView.CropBoxVisible = false;
                    }

                    // Afficher les luminaires
                    SetCategoryVisible(planView, BuiltInCategory.OST_LightingFixtures, true);

                    trans.Commit();

                    string fileName = $"Plan_{room.Id.Value}_{DateTime.Now.Ticks}";
                    string imagePath = Path.Combine(_exportFolder, fileName + ".png");

                    ImageExportOptions options = new ImageExportOptions
                    {
                        ZoomType = ZoomFitType.FitToPage,
                        PixelSize = 1920,
                        ImageResolution = ImageResolution.DPI_300,
                        FilePath = Path.Combine(_exportFolder, fileName),
                        FitDirection = FitDirectionType.Horizontal,
                        ExportRange = ExportRange.SetOfViews,
                        HLRandWFViewsFileType = ImageFileType.PNG
                    };

                    options.SetViewsAndSheets(new List<ElementId> { planView.Id });
                    _doc.ExportImage(options);

                    string[] possiblePaths = Directory.GetFiles(_exportFolder, $"*{room.Id.Value}*.png");
                    if (possiblePaths.Length > 0)
                    {
                        return possiblePaths[0];
                    }

                    return File.Exists(imagePath) ? imagePath : null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur export plan : {ex.Message}");
                return null;
            }
        }

        private string Export3DView(Room room)
        {
            try
            {
                using (Transaction trans = new Transaction(_doc, "Export 3D View"))
                {
                    trans.Start();

                    ViewFamilyType vft = new FilteredElementCollector(_doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

                    if (vft == null)
                    {
                        trans.RollBack();
                        return null;
                    }

                    View3D view3D = View3D.CreateIsometric(_doc, vft.Id);
                    view3D.Name = $"TEMP_3D_{room.Id.Value}_{DateTime.Now.Ticks}";

                    // Cadrer la vue 3D sur la pièce
                    BoundingBoxXYZ bbox = room.get_BoundingBox(null);
                    if (bbox != null)
                    {
                        double margin = 5.0;
                        BoundingBoxXYZ newBbox = new BoundingBoxXYZ
                        {
                            Min = new XYZ(bbox.Min.X - margin, bbox.Min.Y - margin, bbox.Min.Z - margin),
                            Max = new XYZ(bbox.Max.X + margin, bbox.Max.Y + margin, bbox.Max.Z + margin)
                        };

                        view3D.SetSectionBox(newBbox);
                    }

                    // MASQUER LE PLAFOND
                    SetCategoryVisible(view3D, BuiltInCategory.OST_Ceilings, false);

                    // Afficher les luminaires
                    SetCategoryVisible(view3D, BuiltInCategory.OST_LightingFixtures, true);

                    ElementId view3DId = view3D.Id;
                    trans.Commit();

                    string fileName = $"3D_{room.Id.Value}_{DateTime.Now.Ticks}";
                    string imagePath = Path.Combine(_exportFolder, fileName + ".png");

                    ImageExportOptions options = new ImageExportOptions
                    {
                        ZoomType = ZoomFitType.FitToPage,
                        PixelSize = 1920,
                        ImageResolution = ImageResolution.DPI_300,
                        FilePath = Path.Combine(_exportFolder, fileName),
                        FitDirection = FitDirectionType.Horizontal,
                        ExportRange = ExportRange.SetOfViews,
                        HLRandWFViewsFileType = ImageFileType.PNG
                    };

                    options.SetViewsAndSheets(new List<ElementId> { view3DId });
                    _doc.ExportImage(options);

                    // Supprimer la vue temporaire
                    using (Transaction delTrans = new Transaction(_doc, "Delete Temp View"))
                    {
                        delTrans.Start();
                        try { _doc.Delete(view3DId); } catch { }
                        delTrans.Commit();
                    }

                    string[] possiblePaths = Directory.GetFiles(_exportFolder, $"*{room.Id.Value}*.png")
                        .OrderByDescending(f => File.GetCreationTime(f)).ToArray();

                    if (possiblePaths.Length > 0)
                    {
                        return possiblePaths[0];
                    }

                    return File.Exists(imagePath) ? imagePath : null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur export 3D : {ex.Message}");
                return null;
            }
        }

        private void SetCategoryVisible(View view, BuiltInCategory category, bool visible)
        {
            try
            {
                Category cat = _doc.Settings.Categories.get_Item(category);
                if (cat != null)
                {
                    view.SetCategoryHidden(cat.Id, !visible);
                }
            }
            catch { }
        }

        public void CleanupTemporaryViews(List<ElementId> viewIds)
        {
            if (viewIds == null || viewIds.Count == 0) return;

            try
            {
                using (Transaction trans = new Transaction(_doc, "Cleanup"))
                {
                    trans.Start();
                    foreach (var viewId in viewIds)
                    {
                        if (viewId != ElementId.InvalidElementId)
                        {
                            try { _doc.Delete(viewId); } catch { }
                        }
                    }
                    trans.Commit();
                }
            }
            catch { }
        }
    }

    public class RoomViewsExport
    {
        public ElementId RoomId { get; set; }
        public string RoomName { get; set; }
        public ElementId PlanViewId { get; set; }
        public ElementId View3DId { get; set; }
        public string PlanImagePath { get; set; }
        public string View3DImagePath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public RoomViewsExport()
        {
            PlanViewId = ElementId.InvalidElementId;
            View3DId = ElementId.InvalidElementId;
        }
    }
}