using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phonatech
{
    public class Device
    {
        IWorkspace _workspace;
        public Device(IWorkspace pWorkspace)
        {
            _workspace = pWorkspace;
        }

        /// <summary>
        /// Signal range
        /// </summary>
        public int Bars { get; set; }

        /// <summary>
        /// The Connected Tower
        /// </summary>
        public Tower connectedTower { get; set; }

        /// <summary>
        /// The device id
        /// </summary>
        public string DeviceID { get; set; }

        /// <summary>
        /// Returns the last known location fo the device
        /// </summary>
        public IPoint DeviceLocation { get; set; }

        /// <summary>
        /// find the strongest signal
        /// </summary>
        public void reCalculateSignal()
        {
            IFeatureWorkspace pFWorkspace = (IFeatureWorkspace)_workspace;
            IFeatureClass pTowerRangeFC = pFWorkspace.OpenFeatureClass("TowerRange");

            ISpatialFilter pSFilter = new SpatialFilter();
            pSFilter.Geometry = DeviceLocation;
            pSFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            //pSFilter.SubFields = "TOWERID,MAX(RANGE)";
            IFeatureCursor pFCursor = pTowerRangeFC.Search(pSFilter, true);
            IFeature pFeature = pFCursor.NextFeature();
            while (pFeature != null)
            {
                int bars = int.Parse(pFeature.Value[pFeature.Fields.FindField("RANGE")].ToString());
                string tid = pFeature.Value[pFeature.Fields.FindField("TOWERID")].ToString();

                if (bars > Bars)
                {
                    Tower t = new Tower();
                    t.ID = tid;
                    connectedTower = t;
                    Bars = bars;
                }

                pFeature = pFCursor.NextFeature();                
            }
        }

        /// <summary>
        /// Updates the device information
        /// </summary>
        public void Store()
        {
            IFeatureWorkspace pFWorkspace = (IFeatureWorkspace)_workspace;
            IWorkspaceEdit pWorkspaceEdit = (IWorkspaceEdit)pFWorkspace;
            pWorkspaceEdit.StartEditing(true);
            pWorkspaceEdit.StartEditOperation();


            IFeatureClass pDeviceFC = pFWorkspace.OpenFeatureClass("Device");

            IQueryFilter pQFilter = new QueryFilter();
            pQFilter.WhereClause = "DEVICEID = '" + DeviceID + "'";
            IFeatureCursor pFCursor = pDeviceFC.Search(pQFilter, false);
            IFeature pDeviceFeature = pFCursor.NextFeature();

            if (pDeviceFeature != null)
            {
                if (connectedTower != null)
                {
                    pDeviceFeature.Value[pDeviceFeature.Fields.FindField("CONNECTEDTOWERID")] = connectedTower.ID;
                }
                else
                {
                    pDeviceFeature.Value[pDeviceFeature.Fields.FindField("CONNECTEDTOWERID")] = DBNull.Value;

                }
                pDeviceFeature.Value[pDeviceFeature.Fields.FindField("BARS")] = Bars;
                pDeviceFeature.Shape = DeviceLocation;
                pDeviceFeature.Store();

            }
            else
            {
                IFeature pNewFeature = pDeviceFC.CreateFeature();
                pNewFeature.Value[pNewFeature.Fields.FindField("DEVICEID")] = DeviceID;
                pNewFeature.Value[pNewFeature.Fields.FindField("CONNECTEDTOWERID")] = connectedTower.ID;
                pNewFeature.Value[pNewFeature.Fields.FindField("BARS")] = Bars;
                pNewFeature.Shape = DeviceLocation;
                pNewFeature.Store();

            }

            pWorkspaceEdit.StopEditOperation();
            pWorkspaceEdit.StopEditing(true);
        }
    }
}
