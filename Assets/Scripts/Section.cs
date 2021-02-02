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

// Nodes represent a block of a section which has references to all the blocks around it
// Each Node can be found with the Transform to Node dictionary or with the Section's list of nodes
public class Node
{
    public readonly Transform transform;
    
    // TODO: This might not be needed anymore
    private readonly Vector2 transformPosition; // Save a copy of the initial position for equality checks

    // A linked list going to 4 directions
    // A node is guaranteed to always have 4 neighbours but only some of them are active and real
    public Node[] Neighbours { get; private set; }
    
    // Before anything is done with the node, active should obviously be checked first
    // Null checking the transform itself is unneeded and costly for real time operations
    public bool active;

    private static int _maxNeighbours = 4;

    // Constructor for creating ghost Nodes to avoid tedious null checks
    public Node() => active = false;
    
    public Node(Transform centerTransform, Node[] neighbours)
    {
        this.transform = centerTransform;
        this.transformPosition = centerTransform.position;
        
        this.Neighbours = neighbours;
        
        var length = neighbours.Length;
        if(length > _maxNeighbours) // Maximum number of neighbors for a single 2d tile
            Debug.LogWarning("Node has " + length + " members!");
        
        active = true;
    }

    // Neighbours should only be set when raycasting the section
    public void SetNeighbours(Node[] neighbours) => this.Neighbours = neighbours;
    
    public bool Equals(Node other) => transformPosition.Equals(other.transformPosition);

    public override bool Equals(object obj) => obj is Node other && Equals(other);

    public override int GetHashCode() => transformPosition.GetHashCode();
    
    // Determine equality if the nodes' transforms were in the same position at the start of Node's creation
    public static bool operator==(Node left, Node right) => left.transformPosition == right.transformPosition;

    public static bool operator !=(Node left, Node right) => !(left == right);
}

public class Section : MonoBehaviour
{
    private List<Node> nodes;

    private Dictionary<Transform, Node> transformNodeDictionary;

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
    
    // Start is only called for new sections
    // Already created and damaged sections must preserve valuable data such as max hp
    public void Start()
    {
        nodes = new List<Node>();
        transformNodeDictionary = new Dictionary<Transform, Node>();
        
        sectionPrefab = Resources.Load<GameObject>("Section");
        
        _rigidbody2D = GetComponent<Rigidbody2D>();
        
        // Initialize the list of parts
        parts = GetComponentsInChildren<Part>().ToList();
        partColliders = new Dictionary<Part, PolygonCollider2D>();
        
        _rigidbody2D.mass = 0f;
        shipHp = 0f;
        
        // If parts exist, define the mass of the section and hp
        // Also assemble the list of nodes
        foreach (var part in parts)
        {
            _rigidbody2D.mass += part.mass;
            shipHp += part.Hp;
            
            // Assemble RigidBody2D dictionary
            partColliders.Add(part, part.GetComponent<PolygonCollider2D>());
            
            // Initialize list and dictionary with nodes. They will be all replaced later in the raycast
            nodes.Add(new Node(part.transform, new Node[4]));
            transformNodeDictionary.Add(part.transform, nodes.Last());
        }
        
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
            RaycastSection();
            raycastOnStart = false;
        }
    }

    // Defines legal structures and assembles the neighbours of every Node
    void RaycastSection()
    {
        foreach (var part in parts)
        {
            //if (part != parts[1]) continue;
            
            var polygonCollider2D = partColliders[part];
            
            var partTransform = part.transform;
            var up = partTransform.up;
            var right = partTransform.right;
            var partPosition = partTransform.position;
            
            var colliderPointUp = polygonCollider2D.ClosestPoint(partPosition + up * colliderPointSearchLength);
            var colliderPointDown = polygonCollider2D.ClosestPoint(partPosition - up * colliderPointSearchLength);
            var colliderPointRight = polygonCollider2D.ClosestPoint(partPosition + right * colliderPointSearchLength);
            var colliderPointLeft = polygonCollider2D.ClosestPoint(partPosition - right * colliderPointSearchLength);
            
            // Temporarily disable this part's collider for raycasting
            polygonCollider2D.enabled = false;
            
            int layerMask = LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));

            var hits = new List<RaycastHit2D>();

            // Draw rays for all 4 directions until collision point reached
            // An exception for this is a triangle. Only raycast non hypotenuse-sides
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
            
            polygonCollider2D.enabled = true;
            
            Transform[] neighbourTransforms = new Transform[4]; // One block has only 4 raycasts
            
            // Every node has 4 neighbours even if there really isn't that much. Initialize array with ghost neighbours
            Node[] neighbourNodes = new Node[4] {new Node(), new Node(), new Node(), new Node()};

            for (var i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                if (hit.collider != null && hit.collider.gameObject.layer == gameObject.layer
                                         && hit.collider.transform.root == gameObject.transform.root)
                {
                    //Debug.Log("Hit: " + hit.transform.gameObject.transform.position);
                    //Debug.DrawLine(Vector3.zero, hit.collider.transform.position, Color.red, 10f);
                    //gameObjects.Add(hit.collider.transform.gameObject);
                    
                    var hitColliderTransform = hits[i].collider.transform;
                    
                    neighbourTransforms[i] = hitColliderTransform;
                    neighbourNodes[i] = transformNodeDictionary[hitColliderTransform]; // All transforms are linked at start()
                }
            }
            
            // Find the current part's Node and replace it's neighbours
            transformNodeDictionary[partTransform].SetNeighbours(neighbourNodes);
        }
        
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
        Debug.Log("Running integrity check for section " + gameObject.name + " of size " + parts.Count + " owning: " + destroyedPart);
        
        if (!transformNodeDictionary.ContainsKey(destroyedPart))
        {
            Debug.LogWarning("No " + destroyedPart.name + " part in dictionary for " + gameObject.name);
            return;
        }
        
        var destroyedNode = transformNodeDictionary[destroyedPart]; // Get a temporary copy before removal
        destroyedNode.active = false;
        
        // Find all active neighbours
        var activeNeighbourNodes = new List<Node>();
        for (int i = 0; i < MAXAmountOfNeighbours; i++)
        {
            var currentNeighbour = destroyedNode.Neighbours[i];
            
            if(currentNeighbour.active)
                activeNeighbourNodes.Add(currentNeighbour);
        }

        CleanupPartReferences(destroyedPart);
        
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
                            Debug.Log("Deduced path to be true for " + activeNeighbourNodes[i].transform.name + " and " + activeNeighbourNodes[j].transform.name);
                            foundPath = true;
                            goto skipPathFinding;
                        }
                        case 0: // Connection can be deduced to be false
                        {
                            Debug.Log("Deduced path to be false for " + activeNeighbourNodes[i].transform.name + " and " + activeNeighbourNodes[j].transform.name);
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
                    
                    Debug.Log("Found path between " + activeNeighbourNodes[i].transform.name + " and " 
                              + activeNeighbourNodes[j].transform.name);
                    Debug.DrawLine(Vector3.zero, activeNeighbourNodes[i].transform.position, Color.magenta);
                    Debug.DrawLine(Vector3.zero, activeNeighbourNodes[j].transform.position, Color.magenta);
                }
                else
                {
                    compromisedConnections.Add((activeNeighbourNodes[i], activeNeighbourNodes[j]));

                    Debug.Log("No path found between " + activeNeighbourNodes[i].transform.name + " and " 
                              + activeNeighbourNodes[j].transform.name);
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
    
    // Remove the node, disable the transform and other references
    void CleanupPartReferences(Transform partTransform, bool changeOwnership = false)
    {
        // If has a child and that child has a Weapon component
        if (partTransform.childCount != 0 && partTransform.GetChild(0).TryGetComponent<Weapon>(out var weapon))
        {
            Debug.Log("Weapon found!");
            weapons.Remove(weapon);
        }
        
        Part part = partTransform.GetComponent<Part>();
        shipHp -= part.MaxHP; // Remove hp value from mother section
        
        if(changeOwnership)
            part.Start();
        
        parts.Remove(partTransform.GetComponent<Part>());
        
        // Finally disable the game object carrying the transform
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
                if (!node1.Neighbours[i].active) continue; // Skip if not an active node

                nextNode = node1.Neighbours[i];

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
                    if (!node.Neighbours[i].active) continue; // Skip if not an active node

                    nextNode = node.Neighbours[i];

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
        // Find out the biggest group. That is the "core" of the ship that has player/ai in control
        var maxCount = newSections.Max(list => list.Count());
        var coreSection = newSections.First(list => list.Count() == maxCount);

        foreach (var section in newSections)
        {
            if (section == coreSection) continue;

            var transformCache = transform;
            var newSection = Instantiate(sectionPrefab, transformCache.position, transformCache.rotation);
            
            foreach (var node in section)
            {
                // Change parent to a new section to simulate it's own physics
                node.transform.SetParent(newSection.transform);
                
                // Remove references to core section dictionaries
                CleanupPartReferences(node.transform, changeOwnership:true);
                
                // Change layer to default to enable collisions with the new detached section
                node.transform.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            
            // When group is reassigned, reassemble it's hp, weapons and raycast again for legal structure
            section[0].transform.GetComponentInParent<Section>().Start();
            
            // Group detached from parent and currently it doesn't have any velocity
            // So match the values from parent RigidBody2D
            section[0].transform.GetComponentInParent<Rigidbody2D>().velocity = _rigidbody2D.velocity;
            section[0].transform.GetComponentInParent<Rigidbody2D>().angularVelocity = _rigidbody2D.angularVelocity;
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
    
}