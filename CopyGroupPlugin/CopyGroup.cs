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
                Group group = grElement as Group; // Exeption-less construction (null if impossible)

                //XYZ sourceGroupPoint = element.Location.Point;

                LocationPoint grElemLP = grElement.Location as LocationPoint;
                XYZ groupPoint = grElemLP.Point as XYZ;

                Room sourceRoom = doc.GetRoomAtPoint(groupPoint);
                if (sourceRoom == null)
                {
                    TaskDialog.Show("Прервано", "Не удалось определить принадлежность выбранной группы какой-либо комнате");
                    return Result.Cancelled;
                }

                Element sourceRoomElement = sourceRoom as Element;

                LocationPoint sourceElemLP = sourceRoomElement.Location as LocationPoint;
                XYZ sourceRoomPoint = sourceElemLP.Point as XYZ;

                double deltaX = groupPoint.X - sourceRoomPoint.X;
                double deltaY = groupPoint.Y - sourceRoomPoint.Y;

                XYZ userPoint = uiDoc.Selection.PickPoint("Выберите точку");

                Room destRoom = doc.GetRoomAtPoint(userPoint);
                if (destRoom == null)
                {
                    TaskDialog.Show("Прервано", "Не удалось определить принадлежность выбранной точки какой-либо комнате");
                    return Result.Cancelled;
                }

                Element destRoomElement = destRoom as Element;

                LocationPoint destElemLP = destRoomElement.Location as LocationPoint;
                XYZ destRoomPoint = destElemLP.Point as XYZ;

                XYZ placePoint = new XYZ(destRoomPoint.X + deltaX, destRoomPoint.Y + deltaY, 0);

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