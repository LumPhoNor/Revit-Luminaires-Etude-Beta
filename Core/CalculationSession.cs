using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RevitLightingPlugin.Models;
using RevitLightingPlugin.UI;

namespace RevitLightingPlugin.Core
{
    internal static class CalculationSession
    {
        public static List<Room>                               SelectedRooms    { get; set; }
        public static Dictionary<ElementId, RoomActivityType> RoomActivities   { get; set; }
        public static AnalysisSettings                        Settings         { get; set; }
        public static double                                  MaintenanceFactor { get; set; } = 1.0;
        public static Dictionary<ElementId, RoomViewSelection> ViewSelections  { get; set; }

        public static bool IsConfigured =>
            SelectedRooms != null && SelectedRooms.Count > 0 && Settings != null;
    }
}
