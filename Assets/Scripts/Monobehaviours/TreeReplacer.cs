// Tree Replacer created by Seta
// https://www.youtube.com/@SetaLevelDesign
// https://www.youtube.com/watch?v=ZCo-Htm3WXs
// Licence: Creative Commons
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides functionality to replace trees on a terrain with specified prefabs during runtime.
/// </summary>
/// <remarks>This class allows for dynamic replacement of trees on a Unity terrain based on user-defined mappings
/// between tree prototypes and replacement prefabs. It supports interaction-based replacement, grid-based optimization
/// for performance, and optional restoration of trees when exiting Play Mode.</remarks>
public class TreeReplacer : MonoBehaviour {
    [Header("References")]
    public Terrain terrain; //reference to the terrain
    public Camera playerCamera; //reference to the player camera

    [Header("Interaction")]
    public KeyCode replaceKey = KeyCode.G; //key used to trigger tree replacement
    public float interactionDistance = 1f; //max distance the raycast can check for trees
    public float radiusFromHitPoint = 3f; // radius for search tree pivot

    [Header("Performance")]
    public float cellSize = 10f; //size of grid cells
    public float despawnDistance = 50f; //distance at which replaced trees despawn

    [Header("PlayMode Options")]
    public bool restoreTrees = true; //option to restore trees when exiting Play Mode

    [System.Serializable]
    public class TreeReplacement {
        public string treeName; //name of the tree prefab in the terrain
        public GameObject replacementPrefab; //prefab that will replace this tree
    }

    [Header("Replacements")]
    public TreeReplacement[] replacements; //array of tree replacements defined in the inspector

    private TerrainData tData; //reference to terrain data (tree instances, heightmap)
    private Dictionary<string, GameObject> replacementDict; //maps tree names to replacement prefabs
    private Dictionary<Vector3Int, List<TreeRef>> treeGrid; //grid of trees for efficient lookup
    private readonly List<TreeRef> activeSpawned = new List<TreeRef>(); //list of currently spawned replacements
    private const float POS_EPS = 0.01f; //position epsilon for comparing trees

    public class TreeRef {
        public TreeInstance original; //original tree instance data
        public Vector3 worldPos; //world position of the tree
        public bool isReplaced; //whether the tree has been replaced
        public GameObject spawnedGO; //reference to the spawned replacement prefab
    }

    void Start() {
        if (terrain == null || terrain.terrainData == null) //check if terrain and data exist
        {
            enabled = false; //disable script if not valid
            return;
        }

        tData = terrain.terrainData; //get terrain data reference

        if (restoreTrees) //use if restore option is enabled
        {
            tData = Instantiate(terrain.terrainData); //clone terrain data
            terrain.terrainData = tData; //assign cloned data to terrain
        }

        replacementDict = new Dictionary<string, GameObject>(); //initialize replacement dictionary
        foreach (var r in replacements) //loop through defined replacements
        {
            if (!replacementDict.ContainsKey(r.treeName) && r.replacementPrefab != null) //avoid duplicates
                replacementDict.Add(r.treeName, r.replacementPrefab); //add mapping
        }
        BuildTreeGrid(); //build the tree grid
    }

    void Update() {
        if (Input.GetKeyDown(replaceKey)) //if replacement key is pressed
        {
            TryReplaceTree(); //attempt to replace a tree
        }
        CheckForDespawn(); //check if any replaced trees should be despawned
    }

    private void BuildTreeGrid() {
        treeGrid = new Dictionary<Vector3Int, List<TreeRef>>(); //initialize tree grid
        var trees = tData.treeInstances; //get all tree instances from terrain

        for (int i = 0; i < trees.Length; i++) //loop through all trees
        {
            var tr = new TreeRef //create new tree reference
            {
                original = trees[i], //save original instance
                worldPos = NormalizedToWorld(trees[i].position), //convert normalized pos to world pos
                isReplaced = false, //initially not replaced
                spawnedGO = null //no prefab spawned yet
            };

            Vector3Int cell = WorldToCell(tr.worldPos); //determine which grid cell this tree belongs to
            if (!treeGrid.TryGetValue(cell, out var list)) //check if cell exists in dictionary
            {
                list = new List<TreeRef>(); //create new list if cell does not exist
                treeGrid[cell] = list; //add it to the dictionary
            }
            list.Add(tr); //add tree to its cell list
        }
    }

    private Vector3 NormalizedToWorld(Vector3 normalizedPos) => Vector3.Scale(normalizedPos, tData.size) + terrain.transform.position; //convert normalized tree position to world position

    private Vector3Int WorldToCell(Vector3 pos) => new Vector3Int(Mathf.FloorToInt(pos.x / cellSize), 0, Mathf.FloorToInt(pos.z / cellSize)); //convert world pos to grid cell index

    private void TryReplaceTree() {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward); //create a ray starting from the player camera position pointing forward

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance)) return; //cast the ray into the world up to "interactionDistance", stop if nothing was hit

        var target = FindNearestTree(hit.point, radiusFromHitPoint); //find the nearest tree around the hit point within the radiusFromHitPoint value
        if (target == null) return; //stop if no tree found

        string treeName = tData.treePrototypes[target.original.prototypeIndex].prefab.name; //get the tree prototype name
        if (!replacementDict.TryGetValue(treeName, out var prefab) || prefab == null) return; //check if there is a replacement prefab defined for this tree type on list

        ReplaceTreeWithPrefab(target, prefab); //replace tree with the replacement prefab
    }
    private TreeRef FindNearestTree(Vector3 point, float maxDistance) {
        TreeRef closest = null; //this will hold the closest tree found
        float minDist = maxDistance; //start with the maximum allowed distance

        Vector3Int centerCell = WorldToCell(point);  //convert the hit point into grid cell coordinates
        for (int x = -1; x <= 1; x++) //search in the center cell and in surrounding cells
        {
            for (int z = -1; z <= 1; z++) {
                Vector3Int cell = new Vector3Int(centerCell.x + x, 0, centerCell.z + z); //get the current cell to check
                if (!treeGrid.TryGetValue(cell, out var list)) continue; //skip if the cell does not exist

                foreach (var tr in list) //loop through all trees in this cell
                {
                    if (tr.isReplaced) continue;//skip if this tree has already been replaced

                    float dist = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(tr.worldPos.x, tr.worldPos.z)); //calculate horizontal distance from point to tree pivot

                    if (dist < minDist) //save it if this tree is closer than any previously found
                    {
                        minDist = dist;
                        closest = tr;
                    }
                }
            }
        }

        return closest; //return the closest tree found
    }


    private void ReplaceTreeWithPrefab(TreeRef tr, GameObject prefab) {
        if (tr.isReplaced) return; //skip if already replaced

        if (!RemoveTreeInstanceFromTerrain(tr.original)) //try removing from terrain
            return; //if failed, exit

        Quaternion rot = Quaternion.Euler(0f, tr.original.rotation * Mathf.Rad2Deg, 0f); //convert terrain rotation to Quaternion
        Vector3 scale = new Vector3(tr.original.widthScale, tr.original.heightScale, tr.original.widthScale); //get tree scaling

        tr.isReplaced = true; //mark as replaced
        GameObject obj = Instantiate(prefab, tr.worldPos, rot); //spawn replacement prefab
        obj.transform.localScale = Vector3.Scale(obj.transform.localScale, scale); // apply terrain scaling to prefab
        tr.spawnedGO = obj; //save reference to spawned prefab
        activeSpawned.Add(tr); //add to active list
    }

    private bool RemoveTreeInstanceFromTerrain(TreeInstance target) {
        var src = tData.treeInstances; //get all current tree instances
        var list = new List<TreeInstance>(src.Length - 1); //create a new list with reduced size
        bool removed = false; //track if removal happened

        foreach (var t in src) //loop through all trees
        {
            if (!removed && SameTree(t, target)) { removed = true; continue; } //skip target tree once
            list.Add(t); //add all others
        }

        if (removed)
            tData.treeInstances = list.ToArray(); //update terrain data with new tree list

        return removed; //return true if tree was removed
    }

    private bool SameTree(TreeInstance a, TreeInstance b) {
        if (a.prototypeIndex != b.prototypeIndex) return false; //different type of tree
        return (a.position - b.position).sqrMagnitude <= POS_EPS * POS_EPS; //check position difference within epsilon
    }

    private void CheckForDespawn() {
        for (int i = activeSpawned.Count - 1; i >= 0; i--) //loop backwards through active replacements
        {
            var tr = activeSpawned[i]; //get tree reference
            if (!tr.isReplaced || tr.spawnedGO == null) { activeSpawned.RemoveAt(i); continue; } //skip invalid entries

            float d = Vector3.Distance(playerCamera.transform.position, tr.worldPos); //distance to player
            if (d > despawnDistance) //if too far away
            {
                Destroy(tr.spawnedGO); //destroy prefab
                tr.spawnedGO = null; //clear reference
                tr.isReplaced = false; //mark as not replaced
                var list = new List<TreeInstance>(tData.treeInstances) { tr.original }; //add tree back to terrain
                tData.treeInstances = list.ToArray(); //update terrain data
                activeSpawned.RemoveAt(i); //remove from active list
            }
        }
    }
}
