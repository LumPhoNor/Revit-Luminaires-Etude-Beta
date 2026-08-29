using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitLightingPlugin.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class LogoCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            => Result.Succeeded; // Ne fait rien — bouton logo décoratif
    }
}
