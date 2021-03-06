﻿using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if !UNITY_WEBGL
//using UnityEditor.Uti;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InterativeErosionProject
{
    public enum MaterialsForEditing
    {
        stone,
        cobble, clay, sand,
        water, watersource, waterdrain, sediment
        , ocean, lavatest, volcanotest
    }
    public class ControlPanel : Window
    {
        public Text text;
        public Dropdown actionDD, materialChoiseDD, overlayDD;

        public GameObject mapPointer;
        public DragPanel infoWindow;
        public ErosionSim sim;

        [SerializeField]
        private Plane referencePlane = new Plane(Vector3.up, Vector3.zero);

        static public Vector2 selectedPoint;
        static public Action selectedAction = Action.Add;
        internal static MaterialsForEditing selectedMaterial = MaterialsForEditing.volcanotest;
        private Vector3 lastClick;

        public override void Refresh()
        {

        }

        // Use this for initialization
        void Start()
        {
            rebuildActionDD();
            RebuildMaterialDD();
            RebuildOverlayDD();
        }

        // Update is called once per frame
        void Update()
        {
            Refresh();
            if (selectedAction != Action.Nothing && Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                // currently. works as for flat plane                

                if (RaycastToPlain() < 0)
                    mapPointer.SetActive(false);
                else
                {
                    mapPointer.SetActive(true);
                    mapPointer.transform.position = lastClick;

                    // lift pointer at terrain height
                    var height = sim.getTerrainLevel(selectedPoint);
                    height += sim.getLavaLevel(selectedPoint);
                    //height *= (float)ErosionSim.TOTAL_GRID_SIZE / (float)ErosionSim.TEX_SIZE;
                    height *= ErosionSim.terrainAmountScale;
                    height += 12f;
                    mapPointer.transform.position = new Vector3(mapPointer.transform.position.x, mapPointer.transform.position.y + height, mapPointer.transform.position.z);
                    if (Input.GetMouseButton(0))
                    {
                        if (selectedAction == Action.Add)
                        {
                            if (selectedMaterial == MaterialsForEditing.water)
                                sim.AddWater(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.watersource)
                                sim.MoveWaterSource(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.volcanotest)
                                sim.MoveLavaSource(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.waterdrain)
                                sim.MoveWaterDrainage(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.ocean)
                                sim.AddOcean(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.sediment)
                                sim.AddSediment(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.lavatest)
                                sim.AddLava(selectedPoint);
                            else //rest of materials
                                sim.AddToTerrainLayer(selectedMaterial, selectedPoint);
                        }
                        if (selectedAction == Action.Remove)
                        {
                            if (selectedMaterial == MaterialsForEditing.water)
                                sim.RemoveWater(selectedPoint);
                            if (selectedMaterial == MaterialsForEditing.lavatest)
                                sim.RemoveLava(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.watersource)
                                sim.RemoveWaterSource();
                            else if (selectedMaterial == MaterialsForEditing.volcanotest)
                                sim.RemoveLavaSource();
                            else if (selectedMaterial == MaterialsForEditing.waterdrain)
                                sim.RemoveWaterDrainage();
                            else if (selectedMaterial == MaterialsForEditing.ocean)
                                sim.RemoveOcean(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.sediment)
                                sim.RemoveSediment(selectedPoint);
                            else //rest of materials
                                sim.RemoveFromTerrainLayer(selectedMaterial, selectedPoint);
                        }
                        else if (selectedAction == Action.Info)
                            infoWindow.Show();
                    }
                }
            }
        }

        private int RaycastToMesh()
        {
            // Bit shift the index of the layer (8) to get a bit mask
            //var layerMask = 1 << 5;
            // This would cast rays only against colliders in layer 8.
            // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.            
            //layerMask = ~layerMask;
            RaycastHit hit;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity))//, layerMask
            {
                {
                    //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                    //selectedPoint = default(Vector2);
                    //Debug.Log("Missed");
                    return -1;
                }
            }
            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            MeshCollider meshCollider = hit.collider as MeshCollider;

            if (meshCollider == null || meshCollider.sharedMesh == null)
            {
                //selectedPoint = default(Vector2);
                //Debug.Log("Missed");
                return -2;
            }
            selectedPoint = hit.textureCoord;
            lastClick = hit.point;
            return 1;
        }
        void RebuildOverlayDD()
        {
            //actionDD.interactable = true;
            overlayDD.ClearOptions();

            foreach (var next in Overlay.getAllPossible())
            {
                overlayDD.options.Add(new Dropdown.OptionData() { text = next.ToString() });
            }
            overlayDD.RefreshShownValue();
        }
        void rebuildActionDD()
        {
            //actionDD.interactable = true;
            actionDD.ClearOptions();

            foreach (var next in Action.getAllPossible())
            {
                actionDD.options.Add(new Dropdown.OptionData() { text = next.ToString() });
            }
            actionDD.RefreshShownValue();
            //onActionDDChanged();
        }
        void RebuildMaterialDD()
        {
            materialChoiseDD.ClearOptions();
            foreach (var next in Enum.GetValues(typeof(MaterialsForEditing)))
            {
                materialChoiseDD.options.Add(new Dropdown.OptionData() { text = next.ToString() });
            }
            materialChoiseDD.RefreshShownValue();
        }
        public void onActionDDChanged()
        {
            selectedAction = Action.getById(actionDD.value);
            if (selectedAction == Action.Nothing)
                mapPointer.SetActive(false);
        }
        public void onMaterialChoiseDDChanged()
        {
            selectedMaterial = (MaterialsForEditing)materialChoiseDD.value;
        }
        public void onOverlayDDChanged()
        {
            sim.SetOverlay(Overlay.getById(overlayDD.value));
        }
        public void onLoadPlates()
        {
            // Open file with filter
            var extensions = new[] {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg" )
            };
            var selected = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            if (selected[0] != "")
            {
                //var tex = RTUtility.Load(Path.GetFileNameWithoutExtension(selected));
                var tex = RTUtility.Load(selected[0]);
                if (tex != null)
                    sim.SetMagmaVelocity(tex);
            }

        }
        public void onLoadTerrain()
        {

            // Open file with filter
            var extensions = new[] {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg" )
            };
            var selected = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            if (selected[0] != "")
            {
                //var tex = RTUtility.Load(Path.GetFileNameWithoutExtension(selected));
                var tex = RTUtility.Load(selected[0]);
                if (tex != null)
                    sim.SetTerrain(tex);
            }

        }
        private int RaycastToPlain()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float rayDistance;
            if (referencePlane.Raycast(ray, out rayDistance))
            {
                // convert this to texture UV
                lastClick = ray.GetPoint(rayDistance);

                //
                //float xInTexture = lastClick.x * ErosionSim.TEX_SIZE / ErosionSim.TOTAL_GRID_SIZE + ErosionSim.TOTAL_GRID_SIZE / 2;
                float xInTexture = (lastClick.x + ErosionSim.TOTAL_GRID_SIZE / 2) * ErosionSim.TEX_SIZE / ErosionSim.TOTAL_GRID_SIZE;
                float yInTexture = (lastClick.z + ErosionSim.TOTAL_GRID_SIZE / 2) * ErosionSim.TEX_SIZE / ErosionSim.TOTAL_GRID_SIZE;

                if (xInTexture >= 0 && xInTexture <= ErosionSim.MAX_TEX_INDEX
                    && yInTexture >= 0 && yInTexture <= ErosionSim.MAX_TEX_INDEX)
                    selectedPoint = new Vector2(xInTexture / (float)ErosionSim.MAX_TEX_INDEX, yInTexture / (float)ErosionSim.MAX_TEX_INDEX);
                else
                    return -10;

            }
            return 1;
        }
    }

}