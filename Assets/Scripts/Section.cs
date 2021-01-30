using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Weapons;

// Define object diameters from ship center to end
// ShipDimensions are in this order: up, down, right, left
public readonly struct ObjectDimensions
{
    public readonly float up;
    public readonly float down;
    public readonly float right;
    public readonly float left;

    public ObjectDimensions(float up, float down, float right, float left)
    {
        this.up = up;
        this.down = down;
        this.right = right;
        this.left = left;
    }
}

// Nodes represent a piece of a section. Node's gameobject is active when center value is 1. Otherwise 0.
// Each gameobject's node can be found with a dictionary
// Don't have to deal with horrible nulls when traversing trough nodes.
[Serializable]
public struct Node
{
    public Transform centerTransform;
    private Vector2 centerTransformPosition; // Save a copy of the initial position for equality checks
    
    public Transform[] neighbourTransforms;
    [SerializeField] public bool[] neighbourExists; // Null safety and no cost of comparing UnityEngine.Object to null

    // Use a pre-evaluated null check to determine if neighbour is null or not
    public bool DoesNeighbourExist(int index) => neighbourExists[index];

    public Dictionary<Transform, int> indexOfNeighbour; // Fast lookup to the specific index when deleting neighbours
    
    public Node(Transform centerTransform, Transform[] neighbourTransforms)
    {
        this.centerTransform = centerTransform;
        this.centerTransformPosition = centerTransform.position;
        //this.centerExists = true; // When a node is created, it has to have a valid center transform

        this.neighbourTransforms = neighbourTransforms;
        
        var length = neighbourTransforms.Length;
        if(length > 4) // Maximum number of neighbors for a single 2d tile
            Debug.LogWarning("Node has " + length + " members!");

        // Costly null checks are only ran when a node is created when section starts
        neighbourExists = new bool[4];
        indexOfNeighbour = new Dictionary<Transform, int>();
        for (var i = 0; i < length; i++)
        {
            if (neighbourTransforms[i] == null)
                neighbourExists[i] = false;
            else
            {
                neighbourExists[i] = true;
                indexOfNeighbour.Add(neighbourTransforms[i], i);
            }
        }
    }
    
    public bool Equals(Node other)
    {
        return centerTransformPosition.Equals(other.centerTransformPosition);
    }

    public override bool Equals(object obj)
    {
        return obj is Node other && Equals(other);
    }

    public override int GetHashCode()
    {
        return centerTransformPosition.GetHashCode();
    }
    
    // Determine equality if the nodes' transforms were in the same position at the start of raycasting
    public static bool operator==(Node left, Node right) => left.centerTransformPosition == right.centerTransformPosition;

    public static bool operator !=(Node left, Node right) => !(left == right);
}

public class Section : MonoBehaviour
{
    // Uutta kalustoa
    public List<Node> nodeMap;
    
    private Dictionary<Transform, Node> transformNodeDictionary = new Dictionary<Transform, Node>();

    private GameObject sectionPrefab;
    
    private bool raycastOnStart = false;
    
    [SerializeField] public List<Part> parts; // References removed in Part-script or here in recalculation
    private Dictionary<Part, PolygonCollider2D> partColliders;

    public List<Transform> destroyedParts = new List<Transform>();
    private bool _taskComplete = true;
    
    private Rigidbody2D _rigidbody2D;
    
    [SerializeField] private float shipHp = 0f;

    private float raycastLength = 0.05f;
    float colliderPointSearchLength = 1f;
    private const int MAXAmountOfNeighbours = 4;
    
    public float ShipHp
    {
        get => shipHp;
        set => shipHp = value;
    }

    [SerializeField] private float maxShipHp = 0f;

    public float MaxShipHp
    {
        get => maxShipHp;
        set => maxShipHp = value;
    }
    
    public List<Weapon> weapons; // References removed in Weapon-script or here in recalculation
    
    // Start is called before the first frame update
    public void Start()
    {
        sectionPrefab = Resources.Load<GameObject>("Section");
        
        _rigidbody2D = GetComponent<Rigidbody2D>();
        
        // Initialize the list of parts
        parts = GetComponentsInChildren<Part>().ToList();
        partColliders = new Dictionary<Part, PolygonCollider2D>();
        
        _rigidbody2D.mass = 0f;
        shipHp = 0f;
        
        // If parts exist, define the mass of the section and hp
        foreach (var part in parts)
        {
            _rigidbody2D.mass += part.mass;
            shipHp += part.Hp;
            
            // Assemble RigidBody2D dictionary
            partColliders.Add(part, part.GetComponent<PolygonCollider2D>());
        }
        // Assign the max hp
        MaxShipHp = shipHp;
        
        // If there are weapons onboard, list them
        weapons = new List<Weapon>(GetComponentsInChildren<Weapon>());
        
        raycastOnStart = true;
    }

    // This does the same thing as start but maxHp is remembered
    // This is only done for the initial "core" section
    private void ReCalculateStats()
    {
        // Don't do any recalculations before all queued checks are completed
        //if (DestroyedParts.Count > 0) return;
        
        // Clear all lists so no missing parts are remembered
        weapons.Clear();

        // Reset hp and mass but leave maxHP intact
        shipHp = 0;
        _rigidbody2D.mass = 0;
        
        // (re)Initialize the list of parts before using them
        parts = GetComponentsInChildren<Part>().ToList();
        partColliders = new Dictionary<Part, PolygonCollider2D>();
        
        // If parts exist, define the mass of the section
        foreach (var part in parts)
        {
            _rigidbody2D.mass += part.mass;
            shipHp += part.Hp;
            
            partColliders.Add(part, part.GetComponent<PolygonCollider2D>());
        }
        
        // If there are weapons onboard, list them
        weapons = new List<Weapon>(GetComponentsInChildren<Weapon>());

        //raycastOnStart = true;
    }
    
    private void FixedUpdate()
    {
        //if (gameObject.name != "Debris") return;
        
        if (raycastOnStart)
        {
            nodeMap = RaycastSection();
            raycastOnStart = false;
        }
    }

    List<Node> RaycastSection()
    {
        var nodes = new List<Node>();
        
        foreach (var part in parts)
        {
            //if (part != parts[1]) continue;
            
            var polygonCollider2D = partColliders[part];
            
            //var collisionPoints = polygonCollider2D.points;
            //RaycastHit2D hit = Physics2D.Linecast(transform.position, Vector2.up);
            //var closestPoint = polygonCollider2D.ClosestPoint(part.transform.position + Vector3.up);

            var partTransform = part.transform;
            var up = partTransform.up;
            var right = partTransform.right;
            var partPosition = partTransform.position;
            
            var colliderPointUp = polygonCollider2D.ClosestPoint(partPosition + up * colliderPointSearchLength);
            var colliderPointDown = polygonCollider2D.ClosestPoint(partPosition - up * colliderPointSearchLength);
            var colliderPointRight = polygonCollider2D.ClosestPoint(partPosition + right * colliderPointSearchLength);
            var colliderPointLeft = polygonCollider2D.ClosestPoint(partPosition - right * colliderPointSearchLength);
            
            // Temporarily disable this part's collider for raycast for false positives
            polygonCollider2D.enabled = false;
            
            int layerMask = LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));

            var hits = new List<RaycastHit2D>();

            // Draw rays for all 4 directions until collision point reached
            // An exception for this is a triangle. Only raycast right and up
            if (partTransform.CompareTag("Triangle")) 
            {
                Debug.DrawRay(colliderPointUp, up * raycastLength, Color.green, 10f);
                Debug.DrawRay(colliderPointRight, right * raycastLength, Color.green, 10f);
                
                hits.Add(Physics2D.Raycast(colliderPointUp, up, raycastLength, layerMask));
                hits.Add(Physics2D.Raycast(colliderPointRight, right, raycastLength, layerMask));
            }
            else
            {
                Debug.DrawRay(colliderPointUp, up * raycastLength, Color.green, 10f);
                Debug.DrawRay(colliderPointDown, -up * raycastLength, Color.green, 10f);
                Debug.DrawRay(colliderPointRight, right * raycastLength, Color.green, 10f);
                Debug.DrawRay(colliderPointLeft, -right * raycastLength, Color.green, 10f);
                
                hits.Add(Physics2D.Raycast(colliderPointUp, up, raycastLength, layerMask));
                hits.Add(Physics2D.Raycast(colliderPointDown, -up, raycastLength, layerMask));
                hits.Add(Physics2D.Raycast(colliderPointRight, right, raycastLength, layerMask));
                hits.Add(Physics2D.Raycast(colliderPointLeft, -right, raycastLength, layerMask));
            }
            
            polygonCollider2D.enabled = true; // Reenable collider
            
            Transform[] neighbours = new Transform[4]; // One block has only 4 raycasts

            for (var i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                // Projectiles can be on the same layer so check if the hit root is the same
                //if (hit.collider != null && hit.collider.transform.root == gameObject.transform.root)
                if (hit.collider != null && hit.collider.gameObject.layer == gameObject.layer
                                         && hit.collider.transform.root == gameObject.transform.root)
                {
                    //Debug.Log("Hit: " + hit.transform.gameObject.transform.position);
                    //Debug.DrawLine(Vector3.zero, hit.collider.transform.position, Color.red, 10f);
                    //gameObjects.Add(hit.collider.transform.gameObject);
                    
                    neighbours[i] = hits[i].collider.transform;
                }
            }
            
            // Create a new node
            // Also assemble the dictionary
            Node node = new Node(partTransform, neighbours);
            transformNodeDictionary.Add(partTransform, node);
            
            nodes.Add(node);
        }
        
        return nodes;
    }

    private void LateUpdate()
    {
        IntegrityCheckCaller();
    }

    public void IntegrityCheckCaller()
    {
        if (!_taskComplete) return;

        if (destroyedParts.Count <= 0) return;

        var destroyedPart = destroyedParts.First();
        
        _taskComplete = false;

        IntegrityCheck(destroyedPart);

        _taskComplete = true;
            
        destroyedParts.Remove(destroyedPart);

    }
    
    public async void IntegrityCheck(Transform destroyedPart)
    {
        if(raycastOnStart)
            Debug.LogWarning("Destroying node before raycast is complete!");
        
        Debug.Log("Running integrity check for section " + gameObject.name + " of size " + parts.Count + " owning: " + destroyedPart);
        
        if (!transformNodeDictionary.ContainsKey(destroyedPart))
        {
            Debug.LogWarning("No " + destroyedPart.name + " part in dictionary for " + gameObject.name);
            return;
        }
        var destroyedNode = transformNodeDictionary[destroyedPart]; // Get a temporary copy before removal
        
        // Remove the the node for the destroyedPart and references to it from other nodes
        CleanupPartReferences(destroyedPart);
        
        // Find all active neighbours
        var activeNeighbourNodes = new List<Node>();
        for (int i = 0; i < MAXAmountOfNeighbours; i++)
        {
            if (destroyedNode.DoesNeighbourExist(i))
                activeNeighbourNodes.Add(transformNodeDictionary[destroyedNode.neighbourTransforms[i]]);
        }

        switch (activeNeighbourNodes.Count)
        {
            // If there are no neighbours, there is nothing left so no integrity to check
            case 0:
                Debug.Log("Node has no neighbours left. No integrity.");
                return;
            // If there is only one neighbour, only node can't be responsible to compromising integrity
            case 1:
                Debug.Log("Integrity OK");
                return;
        }

        List<Node> nodesToRecalculate = FindPathBetweenParts(activeNeighbourNodes);

        if (nodesToRecalculate.Count == 0)
        {
            Debug.Log("Integrity OK");
            // TODO: Remove references to weapons, parts etc.
            return;
        }

        Debug.Log("Integrity compromised. Recalculating sections.");
        
        List<List<Node>> newSections = await RecalculateSections(nodesToRecalculate);

        foreach (var section in newSections)
        {
            Debug.Log("Section size: " + section.Count);
        }
        
        ConfigureNewSections(newSections);
    }

    // Test connection to every neighbour node by the power of the flood fill
    // A connection is rigid if even one way is found to each other
    // Returns a list of nodes that are compromised
    List<Node> FindPathBetweenParts(List<Node> activeNeighbourNodes)
    {
        Dictionary<(int, int), bool> triedCombinations = new Dictionary<(int, int), bool>();
        
        List<(Node, Node)> connections = new List<(Node, Node)>();
        List<(Node, Node)> compromisedConnections = new List<(Node, Node)>();
        
        for (int i = 0; i < activeNeighbourNodes.Count; i++)
        {
            for (int j = 0; j < activeNeighbourNodes.Count; j++)
            {
                // If the same node or tried them already
                if (i == j || triedCombinations.ContainsKey((i,j)) || triedCombinations.ContainsKey((j,i))) continue;
                
                var foundPath = false;
                
                // Now try to deduce the connection status to the last node. A huge performance gain for big structures
                // Only do this if there are 2 verified connections broken or valid or both
                if (connections.Count + compromisedConnections.Count >= 2)
                {
                    var deductionStatus = DeduceIfConnectionTroughOtherNode((activeNeighbourNodes[i], activeNeighbourNodes[j]), in connections, in compromisedConnections);

                    switch (deductionStatus)
                    {
                        case 1: // Connection can be deduced to be true
                        {
                            Debug.Log("Deduced path to be true for " + activeNeighbourNodes[i].centerTransform.name + " and " + activeNeighbourNodes[j].centerTransform.name);
                            foundPath = true;
                            goto skipPathFinding;
                        }
                        case 0: // Connection can be deduced to be false
                        {
                            Debug.Log("Deduced path to be false for " + activeNeighbourNodes[i].centerTransform.name + " and " + activeNeighbourNodes[j].centerTransform.name);
                            foundPath = false;
                            goto skipPathFinding;
                        }
                        case -1: // Can't deduce connection
                        {
                            break;
                        }
                    }
                }
                
                // Do a (path finding) flood fill to find each other // TODO: Use a more efficient pathfinding algorithm
                ConditionalFloodFill(activeNeighbourNodes[i], activeNeighbourNodes[j], ref foundPath,
                    new Dictionary<Node, bool>());

                skipPathFinding:
                
                if (foundPath)
                {
                    var nodeTuple = (activeNeighbourNodes[i], activeNeighbourNodes[j]);
                    connections.Add(nodeTuple);
                    
                    Debug.Log("Found path between " + activeNeighbourNodes[i].centerTransform.name + " and " 
                              + activeNeighbourNodes[j].centerTransform.name);
                    Debug.DrawLine(Vector3.zero, activeNeighbourNodes[i].centerTransform.position, Color.magenta);
                    Debug.DrawLine(Vector3.zero, activeNeighbourNodes[j].centerTransform.position, Color.magenta);
                }
                else
                {
                    compromisedConnections.Add((activeNeighbourNodes[i], activeNeighbourNodes[j]));

                    Debug.Log("No path found between " + activeNeighbourNodes[i].centerTransform.name + " and " 
                              + activeNeighbourNodes[j].centerTransform.name);
                }
                
                // Add combinations both ways because a connection can be made both ways
                triedCombinations.Add((i,j), true); 
                triedCombinations.Add((j,i), true);
            }
        }

        // If there are no compromised nodes, just return an empty list
        if (compromisedConnections.Count == 0)
            return new List<Node>();
        
        // If there are compromised nodes, we want to make sure that we only send the nodes
        // that are absolutely necessary. Don't send nodes that are surely in the same section for example.
        return DeduceImportantNodes(in connections, in compromisedConnections);;
    }

    // In all natural cases this deduction logic is rigid and provable
    // If a block is destroyed where it shouldn't be possible (in example in a center covered by 4 neighbours) this will probably not work well
    int DeduceIfConnectionTroughOtherNode((Node, Node) nodeTuple, in List<(Node, Node)> connectedNodes, in List<(Node, Node)> compromisedNodes)
    {
        var amountOfValidConnections = connectedNodes.Count;
        var amountOfCompromisedConnections = compromisedNodes.Count;

        // If 1 -> 2 && 1 -> 3 so 2 -> 3
        if (amountOfValidConnections >= 2)
            return 1;

        // If 1 -> 2 && not 1 -> 3 so not 2 -> 3
        // If not 1 -> 2 && 1 -> 3 so not 2 -> 3
        if (amountOfCompromisedConnections >= 1)
            return 0;
        
        return -1; // Can't deduce connection
    }

    List<Node> DeduceImportantNodes(in List<(Node, Node)> connectedNodes, in List<(Node, Node)> compromisedNodes)
    {
        var amountOfValidConnections = connectedNodes.Count;
        var amountOfCompromisedConnections = compromisedNodes.Count;
        
        var importantNodeList = new List<Node>();

        // No rigid connections, send every compromised node
        if (amountOfValidConnections == 0) 
        {
            foreach (var nodeTuple in compromisedNodes)
            {
                importantNodeList.Add(nodeTuple.Item1);
                importantNodeList.Add(nodeTuple.Item2);
            }

            importantNodeList = importantNodeList.Distinct().ToList(); // Remove duplicates

            return importantNodeList;
        }

        // At least one connection is valid, send only one compromised connection
        // One of the nodes of the compromised connections also belongs to the connected connection
        // The one compromised connection can happily be the first one
        importantNodeList.Add(compromisedNodes[0].Item1);
        importantNodeList.Add(compromisedNodes[0].Item2);

        return importantNodeList;
    }

    void CleanupPartReferences(Transform partTransform, bool changeOwnership = false)
    {
        // If has a child and that child has a Weapon component
        if (partTransform.childCount != 0 && partTransform.GetChild(0).TryGetComponent<Weapon>(out var weapon))
        {
            weapons.Remove(weapon);
        }
        
        Part part = partTransform.GetComponent<Part>();
        shipHp -= part.MaxHP; // Remove hp value from mother section
        
        if(changeOwnership) // Reassign parent section
            part.Start();
        
        parts.Remove(partTransform.GetComponent<Part>());

        // Remove all neighbours' reference to this part
        // After this, all neighbours still technically have a reference to the transform
        // Only the null-safe array is updated to contain only safe values
        var node = transformNodeDictionary[partTransform];
        for (int i = 0; i < MAXAmountOfNeighbours; i++)
        {
            if (!node.DoesNeighbourExist(i)) continue;
            
            var neighbourNode = transformNodeDictionary[node.neighbourTransforms[i]];
            
            var indexOfDestroyedPartInNeighbourNode = neighbourNode.indexOfNeighbour[partTransform]; // Lookup the index instead of looping
            neighbourNode.neighbourExists[indexOfDestroyedPartInNeighbourNode] = false;
        }
        
        nodeMap.Remove(transformNodeDictionary[partTransform]);
        
        // Remove the transform's key to the node dictionary
        transformNodeDictionary.Remove(partTransform);
        
        // Finally disable the object
        if(!changeOwnership)
            partTransform.gameObject.SetActive(false);
    }

    // Use flood fill to determine which parts belong to the new section
    async Task<List<List<Node>>> RecalculateSections(List<Node> nodes)
    {
        // Perform an asynchronous flood fill to all the nodes
        var tasks = new List<Task<List<Node>>>();
        foreach (var node in nodes)
        {
            tasks.Add(NonConditionalFloodFillAsync(node, new Dictionary<Node, bool>()));
        }

        var sections = new List<List<Node>>();
        foreach (var task in await Task.WhenAll(tasks))
        {
            sections.Add(task);
        }

        return sections;
    }
    
    void ConditionalFloodFill(Node node1, Node node2, ref bool foundPathToNode, Dictionary<Node, bool> hasNodeBeenVisitedDic)
    {
        //Stack<Node> nodeQueue = new Stack<Node>();
        Node nextNode; 
            
        List<Node> nodeQueue = new List<Node>();
        nodeQueue.Add(node1);
        hasNodeBeenVisitedDic.Add(node1, true);
            
        while (nodeQueue.Count > 0)
        {
            // Flag this group as visited, add it to returned nodes and remove it from queue
            node1 = nodeQueue[0];
            nodeQueue.RemoveAt(0);

            for (var i = 0; i < MAXAmountOfNeighbours; i++)
            {
                if (!node1.neighbourExists[i]) continue; // Skip if not a valid node

                nextNode = transformNodeDictionary[node1.neighbourTransforms[i]];

                // It this node happens to be the node we are looking for
                if (nextNode == node2)
                {
                    foundPathToNode = true;
                    return;
                }
                
                if (!hasNodeBeenVisitedDic.ContainsKey(nextNode))
                {
                    hasNodeBeenVisitedDic.Add(nextNode, true);
                    nodeQueue.Add(nextNode);
                }
            }
        }
        
        // Flag this group as visited
        // Also add it to returned GameObjects
        //if (!hasNodeBeenVisitedDic.ContainsKey(node1))
        //{
        //    hasNodeBeenVisitedDic.Add(node1, true);
        //}
        //
        //for (var i = 0; i < MAXAmountOfNeighbours; i++)
        //{
        //    if (!node1.neighbourExists[i]) continue; // Skip if not a valid node
        //
        //    var nextNode = transformNodeDictionary[node1.neighbourTransforms[i]];
        //    
        //    // It this node happens to be the node we are looking for
        //    if (nextNode == node2)
        //    {
        //        foundPathToNode = true;
        //        return;
        //    }
        //    
        //    if (!hasNodeBeenVisitedDic.ContainsKey(nextNode))
        //    {
        //        ConditionalFloodFill(nextNode, node2, ref foundPathToNode, hasNodeBeenVisitedDic);
        //    }
        //}
    }
    
    void PaintNewSections(List<(Node, Node)> compromisedNodes)
    {
        // All the compromised parts will be new parts of individual sections
        // Run a non conditional flood fill for each group
        
        var newSections = new List<List<Transform>>();
        
        foreach (var node in compromisedNodes)
        {
            var newSection = new List<Transform>();
            //NonConditionalFloodFill(node, ref newSection, new Dictionary<Node, bool>());
            
            newSections.Add(newSection);
        }

        foreach (var section in newSections)
        {
            Debug.Log("Section size: " + section.Count);
        }
        
        //ConfigureNewSections(newSections);

    }
    
    async Task<List<Node>> NonConditionalFloodFillAsync(Node node, Dictionary<Node, bool> hasNodeBeenVisitedDic)
    {
        List<Node> foundNodes = new List<Node>();

        foundNodes = await Task.Run(() =>
        {
            //Stack<Node> nodeQueue = new Stack<Node>();
            Node nextNode; 
            
            List<Node> nodeQueue = new List<Node>();
            nodeQueue.Add(node);
            hasNodeBeenVisitedDic.Add(node, true);
            foundNodes.Add(node);
            
            while (nodeQueue.Count > 0)
            {
                // Flag this group as visited, add it to returned nodes and remove it from queue
                node = nodeQueue[0];
                nodeQueue.RemoveAt(0);

                for (var i = 0; i < MAXAmountOfNeighbours; i++)
                {
                    if (!node.neighbourExists[i]) continue; // Skip if not a valid node

                    nextNode = transformNodeDictionary[node.neighbourTransforms[i]];

                    if (!hasNodeBeenVisitedDic.ContainsKey(nextNode))
                    {
                        hasNodeBeenVisitedDic.Add(nextNode, true);
                        nodeQueue.Add(nextNode);
                        foundNodes.Add(nextNode);
                    }
                }
            }
            
            return foundNodes;
        });
        
        return foundNodes;
    }
    
    void ConfigureNewSections(List<List<Node>> newSections)
    {
        // Find out the biggest group. That is the "core" of the ship that as most components
        var maxCount = newSections.Max(list => list.Count());
        var coreSection = newSections.First(list => list.Count() == maxCount);

        foreach (var section in newSections)
        {
            if (section == coreSection) continue;
                
            var newSection = Instantiate(sectionPrefab, transform.position, transform.rotation);
            
            foreach (var node in section)
            {
                // Change parent to a new section to simulate it's own physics
                node.centerTransform.SetParent(newSection.transform);
                
                // Remove references to core section dictionaries
                CleanupPartReferences(node.centerTransform, changeOwnership:true);
                
                // Change layer to default to enable collisions
                node.centerTransform.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            
            // When group is reassigned, recalculate mass, health, weapons for the new section
            section[0].centerTransform.GetComponentInParent<Section>().Start();
            
            // Group detached from parent and currently it doesn't have any velocity
            // So match the values from parent RigidBody2D
            section[0].centerTransform.GetComponentInParent<Rigidbody2D>().velocity = _rigidbody2D.velocity;
            section[0].centerTransform.GetComponentInParent<Rigidbody2D>().angularVelocity = _rigidbody2D.angularVelocity;
        }
        
        // Recalculate the biggest "core" section's mass, health, weapons
        // If we did did this before, there could still be missing weapons from other sections
        //coreSection[0].centerTransform.GetComponentInParent<Section>().ReCalculateStats();
    }
    
    // Count all objects and figure dimensions based on their positions
    public ObjectDimensions CalculateShipDimensions()
    {
        var up = 0f;
        var down = 0f;
        var right = 0f;
        var left = 0f;
        
        var childX = new List<float>();
        var childY = new List<float>();
        for (var i = 0; i < transform.childCount; i++)
        {
            childX.Add(transform.GetChild(i).transform.localPosition.x);
            childY.Add(transform.GetChild(i).transform.localPosition.y);
        }

        up = childY.Max();
        down = childY.Min();
        right = childX.Max();
        left = childX.Min();
        
        return new ObjectDimensions(up,down,right,left);
    }
    
    // VANHOJA FUNKTIOITA
    
    // Check if section has broken into pieces
    /*
    void OldIntegrityCheck(GameObject destroyedPart)
    {
        
        //if (!masterConnectionDictionary.ContainsKey(destroyedPart))
        //{
        //    //Debug.LogWarning("No PartGroup found for part:" + destroyedPart.name);
        //    Destroy(destroyedPart);
        //    DestroyedParts.Remove(destroyedPart);
        //    return;
        //}
        
        // Lookup the right ConnectionGroup where the destroyed part was the master
        var destroyedPartGroup = masterConnectionDictionary[destroyedPart];
        
        // After getting a copy of the part, destroy the real part along the group
        // The next three lines can have cause errors when multiple objects are destroyed in a very short timespan
        //connectionMap.Remove(masterConnectionDictionary[destroyedPart]);
        if (masterConnectionDictionary.ContainsKey(destroyedPart)) masterConnectionDictionary.Remove(destroyedPart);
        if (subordinateConnectionListDictionary.ContainsKey(destroyedPart)) subordinateConnectionListDictionary.Remove(destroyedPart);
        DestroyedParts.Remove(destroyedPart);
        Destroy(destroyedPart);
        
        // Check if destroyed partGroups subordinates have other masters
        // If any of those subordinates don't have any subordinates or their own, that part is for sure detached
        // If partgroub's subordinates happen to have their own subordinates, try to find a path to each other

        Debug.Log("Subordinate count: " + subordinateConnectionListDictionary.Count);

        var aliveSubordinates = new List<GameObject>();
        for (var i = 0; i < destroyedPartGroup.subordinates.Length; i++)
        {
            if (destroyedPartGroup.subordinates[i] == null) continue;
            aliveSubordinates.Add(destroyedPartGroup.subordinates[i]);
        }

        if (aliveSubordinates.Count == 0)
        {
            Debug.Log("No other nodes left.");
            return;
        }
        
        // A list to store all the valid paths
        var connectedGroups = new List<List<PartGroup>>();
        var partPathDictionary = new Dictionary<PartGroup, List<PartGroup>>(); // Each group knows which path it belongs in

        var compromisedIntegrityGroups = new List<PartGroup>(); // Store a list of the groups that must be re-evaluated for integrity
        
        // Check if there is a valid path for the alive subordinates
        for (var i = 0; i < aliveSubordinates.Count; i++)
        {
            for (var j = 0; j < aliveSubordinates.Count; j++)
            {
                if (i == j) continue; // Don't evaluate connections to the same group
                   
                var subordinateMasterGroup = masterConnectionDictionary[aliveSubordinates[i]];
                var subordinateMasterGroup2 = masterConnectionDictionary[aliveSubordinates[j]];

                // Before tracing the path, check if the path has been already found (one way or another)
                bool pathAlreadyFound = false;
                
                // If groups point to the same list, they already belong to the same path
                if(partPathDictionary.ContainsKey(subordinateMasterGroup) && partPathDictionary.ContainsKey(subordinateMasterGroup2)) 
                    pathAlreadyFound = partPathDictionary[subordinateMasterGroup] == partPathDictionary[subordinateMasterGroup2];

                if(pathAlreadyFound) continue; // Skip path finding if path already exists
                
                var didFindPath = FloodFillManager(ref subordinateMasterGroup, ref subordinateMasterGroup2);
                
                if (didFindPath)
                {
                    Debug.Log(subordinateMasterGroup.master.name + " and " + subordinateMasterGroup2.master.name + " are connected!");
                    Debug.DrawLine(Vector3.zero, subordinateMasterGroup.master.transform.position, Color.magenta, 5f);
                    Debug.DrawLine(Vector3.zero, subordinateMasterGroup2.master.transform.position, Color.magenta, 5f);
                    
                    // If one of the groups belong to a path, assign them both to the path
                    if (partPathDictionary.ContainsKey(subordinateMasterGroup))
                    {
                        partPathDictionary[subordinateMasterGroup].Add(subordinateMasterGroup2);
                        
                        // Update the pointed list for group2
                        partPathDictionary[subordinateMasterGroup2] = partPathDictionary[subordinateMasterGroup];
                    }
                    else if (partPathDictionary.ContainsKey(subordinateMasterGroup2))
                    {
                        partPathDictionary[subordinateMasterGroup2].Add(subordinateMasterGroup);
                        
                        // Update the pointed list for group1
                        partPathDictionary[subordinateMasterGroup] = partPathDictionary[subordinateMasterGroup2];
                    }
                    else // Create a new path, add groups to the path and create entries for the dictionary
                    {
                        var newList = new List<PartGroup>() {subordinateMasterGroup, subordinateMasterGroup2};
                        connectedGroups.Add(newList);
                        
                        // Create dictionary entries for both the groups
                        partPathDictionary.Add(subordinateMasterGroup, newList);
                        partPathDictionary.Add(subordinateMasterGroup2, newList);
                    }
                    
                    
                }
                else
                {
                    Debug.Log(subordinateMasterGroup.master.name + " and " + subordinateMasterGroup2.master.name + " path not found!");
                    // Add both of the groups to the volatilegroups because their integrity is compromised
                    compromisedIntegrityGroups.Add(subordinateMasterGroup);
                    compromisedIntegrityGroups.Add(subordinateMasterGroup2);
                }
            }
        }

        compromisedIntegrityGroups = compromisedIntegrityGroups.Distinct().ToList(); // List might contain duplicates
        
        if (compromisedIntegrityGroups.Count > 0)
        {
            Debug.Log("Found groups whichs integrity is compromised!");
            PaintNewSections(compromisedIntegrityGroups);
        }
        else
        {
            Debug.Log("Integrity is OK");
        }

    }
    */
    /*
    private Dictionary<Transform, List<Node>> AssembleTransformAsNeighborDictionary()
    {
        var dictionary = new Dictionary<Transform, List<Node>>();
        
        for (var i = 0; i < nodeMap.Count; i++)
        {
            var node1 = nodeMap[i];

            foreach (var neighbourTransform in node1.neighbourTransforms)
            {
                if(!node1.activeNeighbourTransforms.TryGetValue(neighbourTransform, out var value)) continue;
                
                //for (var j = 0; j < nodeMap.Count; j++)
                //{
                //    if (i == j) continue; // Don't hassle with the same node
                //
                //    var node2 = nodeMap[j];
                //
                //    if (!node2.neighbourTransforms.Contains(neighbourTransform)) continue;
                //
                //    // Group 2 has the same subordinate as Group 1
                //    // So add it as an entry if it isn't already there
                //    // Otherwise just add a new value
                //
                //    if (dictionary.ContainsKey(neighbourTransform))
                //    {
                //        // Update value list
                //        dictionary[neighbourTransform].Add(nodeMap[i]);
                //        dictionary[neighbourTransform].Add(nodeMap[j]);
                //        
                //        // In case of duplicates, delete them
                //        dictionary[neighbourTransform] =
                //            dictionary[neighbourTransform].Distinct().ToList();
                //    }
                //    else
                //    {
                //        // Create a new entry and set both map1 and map2 as values
                //        var list = new List<Node>() {nodeMap[i], nodeMap[j]};
                //        dictionary.Add(neighbourTransform, list);
                //    }
                //}
            }
        }

        return dictionary;
    }
    */
    /*
    List<GameObject> ReturnElementsThatOverlap(List<List<GameObject>> groups)
    {
        var overlappingElements = new List<GameObject>();
        
        foreach (var group in groups)
        {
            foreach (var gameObject in group)
            {
                foreach (var group2 in groups)
                {
                    if(group == group2) continue;
                    
                    var contains = group2.Any(val => val == gameObject);
       
                    if (contains)
                    {
                        //Debug.DrawLine(Vector3.zero, gameObject.transform.position, Color.magenta, 10f);
                        overlappingElements.Add(gameObject);
                    }
                }
            }
        }
        
        overlappingElements = overlappingElements.Distinct().ToList();
        return overlappingElements;
    }

    List<List<GameObject>> MergeGroupsWithOverlappingElements(List<List<GameObject>> groups)
    {
        var tempGroups = new List<List<GameObject>>(groups);
        
        for (var i = 0; i < groups.Count; i++)
        {
            // Go trough every element in group
            foreach (var gameObject in groups[i])
            {
                for (var j = 0; j < groups.Count; j++)
                {
                    if (groups[i] == groups[j]) continue;
                    
                    if (groups[j].Contains(gameObject))
                    {
                        var mergedGroup = new List<GameObject>();
                        mergedGroup.AddRange(groups[i]);
                        mergedGroup.AddRange(groups[j]);
                        mergedGroup = mergedGroup.Distinct().ToList();

                        tempGroups[i] = mergedGroup; // Replace first one
                        tempGroups.Remove(tempGroups[j]); // Delete second one
                        //groups.Add(mergedGroup); // Add the merged group

                        return tempGroups;

                    }
                }
            }
        }
        
        //var sharedOptions =
        //    from option in groups.First( ).Distinct( )
        //    where groups.Skip( 1 ).All( l => l.Contains( option ) )
        //    select option;
        
        

        /*
        foreach (var group in groups)
        {
            foreach (var gameObject in group)
            {
                //foreach (var group2 in groups)
                //{
                //    //if(group == group2) continue;
                //    //
                //    //var mergedGroup = new List<GameObject>();
                //    //
                //    //var contains = group2.Any(val => val == gameObject);
                //    //
                //    //if (contains)
                //    //{
                //    //    mergedGroup.AddRange(group);
                //    //    mergedGroup.AddRange(group2);
                //    //    mergedGroup = mergedGroup.Distinct().ToList(); // Remove duplicates
                //    //    
                //    //    mergedGroups.Add(mergedGroup);
                //    //}
                //}
            }
        }
        
        return tempGroups;
    }
    
    void ReAssignParts(List<List<GameObject>> groups)
    {
        // Find out the biggest group. That is the "core" of the ship that as most components
        var maxCount = groups.Max(list => list.Count());
        var coreSection = groups.First(list => list.Count() == maxCount);
        
        foreach (var group in groups)
        {
            // Skip the biggest one from reassigning parts
            if (group == coreSection)
                continue;
            
            // Create a section at the first element of the group
            // var newSection = Instantiate(sectionPrefab, group[0].transform.position, group[0].transform.rotation);
            var newSection = Instantiate(sectionPrefab, transform.position, transform.rotation);
            
            foreach (var gameObject in group)
            {
                // Remove object from parent section's part list
                // So parent doesn't have any reference to detached parts

                // Also remove from raycastGroups so we don't have to raycast again
                //for (int i = 0; i < rayCastGroups.Count; i++)
                //{
                //    if (rayCastGroups[i].Contains(gameObject))
                //        rayCastGroups.RemoveAt(i);
                //}

                //Debug.Log(rayCastGroups.Count);
                
                parts.Remove(gameObject.GetComponent<Part>());
                
                // Change layer to default to enable collisions
                gameObject.layer = LayerMask.NameToLayer("Default");
                
                // Change parent to a new section to simulate it's own physics
                gameObject.transform.SetParent(newSection.transform);
            }
            
            // When group is reassigned, recalculate mass, health, weapons for the new section
            group[0].GetComponentInParent<Section>().Start();
            
            // Group detached from parent and currently it doesn't have any velocity
            // So match the values from parent RigidBody2D
            group[0].GetComponentInParent<Rigidbody2D>().velocity = _rigidbody2D.velocity;
            group[0].GetComponentInParent<Rigidbody2D>().angularVelocity = _rigidbody2D.angularVelocity;
        }
        
        // Recalculate the biggest "core" section's mass, health, weapons
        // If we did did this before, there could still be missing weapons from other sections
        coreSection[0].GetComponentInParent<Section>().ReCalculateStats();
    }
    
    */
    
    
}