using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phonatech
{
    public class TowerManager
    {
        private IWorkspace _workspace;

        private DataTable _towerdetails;

        public TowerManager(IWorkspace pWorkspace)
        {
            _workspace = pWorkspace;

            //read the tower details table 
            IFeatureWorkspace pFWorkspace = (IFeatureWorkspace)_workspace;
            ITable pTableTowerDetails = pFWorkspace.OpenTable("TowerDetails");
            ICursor pCursor = pTableTowerDetails.Search(null, false);
            IRow pRow = pCursor.NextRow();

            _towerdetails = new DataTable();
            _towerdetails.Columns.Add("TowerType");
            _towerdetails.Columns.Add("TowerCoverage");
            _towerdetails.Columns.Add("TowerCost");
            _towerdetails.Columns.Add("TowerHeight");
            _towerdetails.Columns.Add("TowerBaseArea");

            while (pRow != null)
            {
                DataRow dtRow = _towerdetails.NewRow();
                dtRow["TowerType"] = pRow.Value[pRow.Fields.FindField("TowerType")].ToString();
                dtRow["TowerCoverage"] = pRow.Value[pRow.Fields.FindField("TowerCoverage")].ToString();
                dtRow["TowerCost"] = pRow.Value[pRow.Fields.FindField("TowerCost")].ToString();
                dtRow["TowerHeight"] = pRow.Value[pRow.Fields.FindField("TowerHeight")].ToString();
                dtRow["TowerBaseArea"] = pRow.Value[pRow.Fields.FindField("TowerBaseArea")].ToString();

                _towerdetails.Rows.Add(dtRow);
                _towerdetails.AcceptChanges();

                pRow = pCursor.NextRow();
            }
        }

        protected Tower GetTower(IFeature pTowerFeature)
        {
            Tower tower = new Tower();
            tower.ID = pTowerFeature.Value[pTowerFeature.Fields.FindField("TOWERID")].ToString(); ;
            tower.NetworkBand = pTowerFeature.Value[pTowerFeature.Fields.FindField("NETWORKBAND")].ToString();
            tower.TowerType = pTowerFeature.Value[pTowerFeature.Fields.FindField("TOWERTYPE")].ToString();
            tower.TowerLocation = (IPoint)pTowerFeature.Shape;

            // Search for the tower details...
            foreach(DataRow r in _towerdetails.Rows)
            {
                if (r["TowerType"].ToString() == tower.TowerType)
                {
                    tower.TowerCoverage = double.Parse(r["TowerCoverage"].ToString());
                    tower.TowerCost = double.Parse(r["TowerCost"].ToString());
                    tower.TowerBaseArea = double.Parse(r["TowerBaseArea"].ToString());
                    tower.TowerHeight = double.Parse(r["TowerHeight"].ToString());

                }
            }

            return tower;
        }

        public Towers GetTowers()
        {
            Towers towers = new Towers();
            IFeatureWorkspace pFWorkspace = (IFeatureWorkspace)_workspace;
            IFeatureClass pTowerFC = pFWorkspace.OpenFeatureClass("Tower");

            IFeatureCursor pFcursor = pTowerFC.Search(null, false);
            IFeature pFeature = pFcursor.NextFeature();
            while(pFeature != null)
            {
                Tower tower = this.GetTower(pFeature);
                towers.Items.Add(tower);
                pFeature = pFcursor.NextFeature();
            }
            return towers;
        }

        public Tower GetTowerByID(string towerid)
        {
            // Query the geodatabase..
            IFeatureWorkspace pFeatureWorkspace = (IFeatureWorkspace)_workspace;
            
            IFeatureClass fcTower = pFeatureWorkspace.OpenFeatureClass("Tower");

            // Get the tower feature by id
            IQueryFilter pQFilter = new QueryFilter();
            pQFilter.WhereClause = "TOWERID = '" + towerid + "'";

            IFeatureCursor pFCursor = fcTower.Search(pQFilter, true);

            IFeature pTowerFeature = pFCursor.NextFeature();

            if (pTowerFeature == null)
            {
                return null;
            }


            return GetTower(pTowerFeature);
            // Get the tower type, and query the tower details table to get the rest of the data
        }

        public void GenerateTowerCoverage(Towers pTowers)
        {
            IWorkspaceEdit pWorkspaceEdit;
            pWorkspaceEdit = (IWorkspaceEdit)this._workspace;
            try
            {
                IFeatureWorkspace pFWorkspace = (IFeatureWorkspace)pWorkspaceEdit;

                IFeatureClass pTowerRangeFC = pFWorkspace.OpenFeatureClass("TowerRange");

                pWorkspaceEdit.StartEditing(true);
                pWorkspaceEdit.StartEditOperation();

                // Delete all ranges, we should later change that to delete only
                IFeatureCursor pcursor = pTowerRangeFC.Update(null, false);
                IFeature pfeaturerange = pcursor.NextFeature();

                while (pfeaturerange != null)
                {
                    pfeaturerange.Delete();
                    pfeaturerange = pcursor.NextFeature();
                }

                foreach (Tower pTower in pTowers.Items)

                {
                    ITopologicalOperator pTopo = (ITopologicalOperator)pTower.TowerLocation;

                    IPolygon range3Bars = (IPolygon)pTopo.Buffer(pTower.TowerCoverage / 3);
                    IPolygon range2BarsWhole = (IPolygon)pTopo.Buffer(pTower.TowerCoverage * 2 / 3);
                    IPolygon range1BarsWhole = (IPolygon)pTopo.Buffer(pTower.TowerCoverage * 3 / 3);

                    ITopologicalOperator pIntTopo = (ITopologicalOperator)range2BarsWhole;
                    IPolygon range2BarsDonut = (IPolygon)pIntTopo.SymmetricDifference(range3Bars);

                    ITopologicalOperator pIntTopo1 = (ITopologicalOperator)range1BarsWhole;
                    IPolygon range1BarsDonut = (IPolygon)pIntTopo1.SymmetricDifference(range2BarsWhole);


                    IFeature pFeature = pTowerRangeFC.CreateFeature();

                    pFeature.Value[pFeature.Fields.FindField("TOWERID")] = "T04";
                    pFeature.Value[pFeature.Fields.FindField("RANGE")] = 3;

                    pFeature.Shape = range3Bars;
                    pFeature.Store();


                    IFeature pFeature2Bar = pTowerRangeFC.CreateFeature();

                    pFeature2Bar.Value[pFeature.Fields.FindField("TOWERID")] = "T04";
                    pFeature2Bar.Value[pFeature.Fields.FindField("RANGE")] = 2;

                    pFeature2Bar.Shape = range2BarsDonut;
                    pFeature2Bar.Store();


                    IFeature pFeature1Bar = pTowerRangeFC.CreateFeature();

                    pFeature1Bar.Value[pFeature.Fields.FindField("TOWERID")] = "T04";
                    pFeature1Bar.Value[pFeature.Fields.FindField("RANGE")] = 1;

                    pFeature1Bar.Shape = range1BarsDonut;
                    pFeature1Bar.Store();

                }


                pWorkspaceEdit.StopEditOperation();
                pWorkspaceEdit.StopEditing(true);
            }

            catch (Exception ex)
            {
                // if anything went wrong, just roll back
                pWorkspaceEdit.AbortEditOperation();
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }

        }
        
        public Tower GetNearestTower(IPoint pPoint, int buffer)
        {
            ITopologicalOperator pTopo = (ITopologicalOperator)pPoint;

            IGeometry pBufferedPoint = pTopo.Buffer(buffer);

            ISpatialFilter pSFilter = new SpatialFilter();
            pSFilter.Geometry = pBufferedPoint;
            pSFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

            // Query the geodatabase..
            IFeatureWorkspace pFeatureWorkspace = (IFeatureWorkspace)_workspace;

            IFeatureClass fcTower = pFeatureWorkspace.OpenFeatureClass("Tower");

            IFeatureCursor pFCursor = fcTower.Search(pSFilter, true);
            IFeature pTowerFeature = pFCursor.NextFeature();

            if (pTowerFeature == null)
            {
                return null;
            }

            return GetTower(pTowerFeature);
        }
    }
}
