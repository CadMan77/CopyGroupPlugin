// Плагин для выбора и копирования в выбранную точку группы объектов

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {        
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            try
            {
                //Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов");
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new GroupFilter(), "Выберите группу объектов");
                Element grElement = doc.GetElement(reference);

                //Group group = (Group)element;
                Group group = grElement as Group; // Exception-less construction (null if impossible)

                //XYZ sourceGroupPoint = element.Location.Point;

                //LocationPoint grElemLP = grElement.Location as LocationPoint;
                //XYZ groupLP = grElemLP.Point as XYZ; // Group Location Point

                BoundingBoxXYZ groupBB = grElement.get_BoundingBox(null);
                XYZ groupCP = (groupBB.Min + groupBB.Max) / 2; // Group Center Point

                //TaskDialog.Show("LP - CP", $"{groupLP.X} - {groupCP.X}{Environment.NewLine}{groupLP.Y} - {groupCP.Y}{Environment.NewLine}{groupLP.Z} - {groupCP.Z}");

                Room sourceRoom = doc.GetRoomAtPoint(groupCP);
                if (sourceRoom == null)
                {
                    TaskDialog.Show("Прервано", "Не удалось определить принадлежность выбранной группы к какой-либо комнате");
                    return Result.Cancelled;
                }

                Element sourceRoomElement = sourceRoom as Element;

                LocationPoint sourceElemLP = sourceRoomElement.Location as LocationPoint;
                XYZ sourceRoomLP = sourceElemLP.Point as XYZ;

                double deltaX = groupCP.X - sourceRoomLP.X;
                double deltaY = groupCP.Y - sourceRoomLP.Y;

                XYZ userPoint = uiDoc.Selection.PickPoint("Выберите точку");

                Room destRoom = doc.GetRoomAtPoint(userPoint);
                if (destRoom == null)
                {
                    TaskDialog.Show("Прервано", "Не удалось определить принадлежность выбранной точки к какой-либо комнате");
                    return Result.Cancelled;
                }

                Element destRoomElement = destRoom as Element;

                //LocationPoint destElemLP = destRoomElement.Location as LocationPoint;
                //XYZ destRoomLP = destElemLP.Point as XYZ; // Room Location Point

                BoundingBoxXYZ destRoomBB = destRoomElement.get_BoundingBox(null);
                XYZ destRoomCP = (destRoomBB.Min + destRoomBB.Max) / 2; // Room Center Point

                //TaskDialog.Show("LP - CP", $"{destRoomLP.X} - {destRoomCP.X}{Environment.NewLine}{destRoomLP.Y} - {destRoomCP.Y}{Environment.NewLine}{destRoomLP.Z} - {destRoomCP.Z}");

                XYZ placePoint = new XYZ(destRoomCP.X + deltaX, destRoomCP.Y + deltaY, 0);

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");

                doc.Create.PlaceGroup(placePoint, group.GroupType);

                transaction.Commit();
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                //TaskDialog.Show("Прервано",ex.Message);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
    public class GroupFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            #region Определение разрешенных элементов по внутреннему названию категории (?не работает?)
            //if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSGroups)
            //    return true;
            //else
            //    return false;
            #endregion

            return elem is Group;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}