using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.Desktop.AddIns;
using System.Windows.Forms;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;

namespace Phonatech
{
    public class AddTower : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        public AddTower()
        {
        }

        protected override void OnUpdate()
        {

        }

        protected override void OnMouseUp(MouseEventArgs arg)
        {
            int x = arg.X;
            int y = arg.Y;

            IMxDocument pMxdoc = (IMxDocument)ArcMap.Application.Document;
            IFeatureLayer pfeaturelayer = (IFeatureLayer)pMxdoc.FocusMap.Layer[0];
            IDataset pDS = (IDataset)pfeaturelayer.FeatureClass;
            TowerManager tm = new TowerManager(pDS.Workspace);

            IPoint pPoint = pMxdoc.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);
            Tower t = tm.GetNearestTower(pPoint, 20);

            //Tower t = tm.GetTowerByID("T04");
            if (t != null)
            {
                MessageBox.Show("Tower id " + t.ID + Environment.NewLine + "Type " + t.TowerType + Environment.NewLine + "Networkband " + t.NetworkBand);
            }

            //IPoint pPoint = pMxdoc.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(x, y);

            //MessageBox.Show("Mouse point is x: " + x + Environment.NewLine + "y: " + y + Environment.NewLine + "MapX: " + pPoint.X + Environment.NewLine + "MapY: " + pPoint.Y);

        }
    }

}
