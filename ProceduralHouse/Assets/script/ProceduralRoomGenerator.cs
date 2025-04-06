using UnityEngine;
using System.Collections.Generic;

public class ProceduralRoomGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject wallWithDoorPrefab;
    public GameObject floorPrefab;
    public GameObject ceilingPrefab;
    
    [Header("Generation Settings")]
    public int roomCount = 10;
    public int minRoomSize = 3;
    public int maxRoomSize = 8;
    public float corridorWidth = 2f;
    public bool addColliders = true;
    
    // Structure repr�sentant une pi�ce
    private class Room
    {
        public Vector2Int position;
        public Vector2Int size;
        public List<Vector2Int> doorPositions = new List<Vector2Int>();
        
        public bool Overlaps(Room other)
        {
            return !(position.x + size.x < other.position.x || 
                    position.x > other.position.x + other.size.x || 
                    position.y + size.y < other.position.y || 
                    position.y > other.position.y + other.size.y);
        }
    }
    
    private List<Room> rooms = new List<Room>();
    private List<GameObject> generatedObjects = new List<GameObject>();
    
    void Start()
    {
        GenerateLevel();
    }
    
    public void GenerateLevel()
    {
        // Nettoyer les objets pr�c�dents si on r�g�n�re
        CleanupGeneratedObjects();
        
        // G�n�rer les pi�ces
        GenerateRooms();
        
        // Connecter les pi�ces avec des couloirs
        ConnectRooms();
        
        // Placer les �l�ments dans la sc�ne
        BuildLevel();
    }
    
    private void CleanupGeneratedObjects()
    {
        foreach (GameObject obj in generatedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        
        generatedObjects.Clear();
        rooms.Clear();
    }
    
    private void GenerateRooms()
    {
        for (int i = 0; i < roomCount; i++)
        {
            Room newRoom = new Room
            {
                size = new Vector2Int(
                    Random.Range(minRoomSize, maxRoomSize + 1),
                    Random.Range(minRoomSize, maxRoomSize + 1)
                ),
                position = new Vector2Int(
                    Random.Range(-20, 20),
                    Random.Range(-20, 20)
                )
            };
            
            // V�rifier si la pi�ce chevauche une autre pi�ce existante
            bool overlaps = false;
            for (int j = 0; j < rooms.Count; j++)
            {
                if (newRoom.Overlaps(rooms[j]))
                {
                    overlaps = true;
                    break;
                }
            }
            
            // Si elle ne chevauche pas, l'ajouter � la liste
            if (!overlaps)
                rooms.Add(newRoom);
            else
                i--; // R�essayer
        }
    }
    
    private void ConnectRooms()
    {
        // Connecter chaque pi�ce � la pi�ce la plus proche
        for (int i = 0; i < rooms.Count; i++)
        {
            int closestRoomIndex = -1;
            float closestDistance = float.MaxValue;
            
            // Trouver la pi�ce la plus proche
            for (int j = 0; j < rooms.Count; j++)
            {
                if (i == j) continue;
                
                Vector2 centerA = new Vector2(
                    rooms[i].position.x + rooms[i].size.x / 2,
                    rooms[i].position.y + rooms[i].size.y / 2
                );
                
                Vector2 centerB = new Vector2(
                    rooms[j].position.x + rooms[j].size.x / 2,
                    rooms[j].position.y + rooms[j].size.y / 2
                );
                
                float distance = Vector2.Distance(centerA, centerB);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestRoomIndex = j;
                }
            }
            
            if (closestRoomIndex >= 0)
            {
                // D�cider des points de connexion (portes)
                Room roomA = rooms[i];
                Room roomB = rooms[closestRoomIndex];
                
                // C�t� X ou Y pour la connexion
                bool connectOnX = Random.value > 0.5f;
                
                if (connectOnX)
                {
                    // Connexion horizontale
                    int doorY = Random.Range(0, roomA.size.y);
                    
                    // Position de la porte pour la pi�ce A
                    Vector2Int doorA;
                    if (roomA.position.x < roomB.position.x)
                        doorA = new Vector2Int(roomA.size.x - 1, doorY);
                    else
                        doorA = new Vector2Int(0, doorY);
                        
                    roomA.doorPositions.Add(doorA);
                    
                    // Position de la porte pour la pi�ce B
                    Vector2Int doorB;
                    if (roomB.position.x < roomA.position.x)
                        doorB = new Vector2Int(roomB.size.x - 1, doorY);
                    else
                        doorB = new Vector2Int(0, doorY);
                        
                    roomB.doorPositions.Add(doorB);
                }
                else
                {
                    // Connexion verticale
                    int doorX = Random.Range(0, roomA.size.x);
                    
                    // Position de la porte pour la pi�ce A
                    Vector2Int doorA;
                    if (roomA.position.y < roomB.position.y)
                        doorA = new Vector2Int(doorX, roomA.size.y - 1);
                    else
                        doorA = new Vector2Int(doorX, 0);
                        
                    roomA.doorPositions.Add(doorA);
                    
                    // Position de la porte pour la pi�ce B
                    Vector2Int doorB;
                    if (roomB.position.y < roomA.position.y)
                        doorB = new Vector2Int(doorX, roomB.size.y - 1);
                    else
                        doorB = new Vector2Int(doorX, 0);
                        
                    roomB.doorPositions.Add(doorB);
                }
            }
        }
    }
    
    private void BuildLevel()
    {
        foreach (Room room in rooms)
        {
            BuildRoom(room);
        }
    }
    
    private void BuildRoom(Room room)
    {
        // Cr�er le sol
        GameObject floor = CreateFloor(room);
        generatedObjects.Add(floor);
        
        // Cr�er le plafond
        GameObject ceiling = CreateCeiling(room);
        generatedObjects.Add(ceiling);
        
        // Cr�er les murs
        for (int x = 0; x < room.size.x; x++)
        {
            for (int y = 0; y < room.size.y; y++)
            {
                // V�rifier si c'est un mur (bord de la pi�ce)
                if (x == 0 || x == room.size.x - 1 || y == 0 || y == room.size.y - 1)
                {
                    // V�rifier si c'est une position de porte
                    bool isDoor = false;
                    foreach (Vector2Int doorPos in room.doorPositions)
                    {
                        if (doorPos.x == x && doorPos.y == y)
                        {
                            isDoor = true;
                            break;
                        }
                    }
                    
                    // D�terminer l'orientation du mur
                    float rotation = 0f;
                    if (x == 0) rotation = 90f;
                    else if (x == room.size.x - 1) rotation = 270f;
                    else if (y == 0) rotation = 180f;
                    // y == room.size.y - 1 reste � 0f
                    
                    // Cr�er le mur ou la porte
                    GameObject wallObj;
                    if (isDoor)
                        wallObj = CreateWallWithDoor(room, x, y, rotation);
                    else
                        wallObj = CreateWall(room, x, y, rotation);
                        
                    generatedObjects.Add(wallObj);
                }
            }
        }
    }
    
    private GameObject CreateFloor(Room room)
    {
        GameObject floor = Instantiate(floorPrefab, transform);
        floor.transform.localScale = new Vector3(room.size.x, 0.1f, room.size.y);
        floor.transform.position = new Vector3(
            room.position.x + room.size.x / 2f,
            0f,
            room.position.y + room.size.y / 2f
        );
        
        if (addColliders && floor.GetComponent<Collider>() == null)
            floor.AddComponent<BoxCollider>();
            
        return floor;
    }
    
    private GameObject CreateCeiling(Room room)
    {
        GameObject ceiling = Instantiate(ceilingPrefab, transform);
        ceiling.transform.localScale = new Vector3(room.size.x, 0.1f, room.size.y);
        ceiling.transform.position = new Vector3(
            room.position.x + room.size.x / 2f,
            3f, // Hauteur du plafond
            room.position.y + room.size.y / 2f
        );
        
        if (addColliders && ceiling.GetComponent<Collider>() == null)
            ceiling.AddComponent<BoxCollider>();
            
        return ceiling;
    }
    
    private GameObject CreateWall(Room room, int x, int y, float rotation)
    {
        GameObject wall = Instantiate(wallPrefab, transform);
        wall.transform.position = new Vector3(
            room.position.x + x,
            1.5f, // Hauteur du mur (centr�e)
            room.position.y + y
        );
        wall.transform.localScale = new Vector3(1f, 3f, 1f); // Hauteur du mur � 3 unit�s
        wall.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        
        if (addColliders && wall.GetComponent<Collider>() == null)
            wall.AddComponent<BoxCollider>();
            
        return wall;
    }
    
    private GameObject CreateWallWithDoor(Room room, int x, int y, float rotation)
    {
        GameObject wallWithDoor = Instantiate(wallWithDoorPrefab, transform);
        wallWithDoor.transform.position = new Vector3(
            room.position.x + x,
            1.5f, // Hauteur du mur (centr�e)
            room.position.y + y
        );
        wallWithDoor.transform.localScale = new Vector3(1f, 3f, 1f); // Hauteur du mur � 3 unit�s
        wallWithDoor.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        
        if (addColliders && wallWithDoor.GetComponent<Collider>() == null)
            wallWithDoor.AddComponent<BoxCollider>();
            
        return wallWithDoor;
    }
    
    // M�thode pour r�g�n�rer le niveau via l'inspecteur
    public void RegenerateLevel()
    {
        GenerateLevel();
    }
}