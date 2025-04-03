using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Classe pour stocker les connexions entre pi�ces
public class RoomConnection
{
    public int room1Index; // Index premi�re pi�ce
    public int room2Index; // Index seconde pi�ce
    public int direction; // Direction: 0=Nord, 1=Est, 2=Sud, 3=Ouest

    public RoomConnection(int r1, int r2, int dir)
    {
        room1Index = r1;
        room2Index = r2;
        direction = dir;
    }
}

public class ProceduralHouse : MonoBehaviour
{
    [Header("Configuration de la Maison")]
    public int numberOfRoomsPerFloor = 5;
    public int numberOfFloors = 2;
    public Vector2Int roomSizeMin = new Vector2Int(4, 4);
    public Vector2Int roomSizeMax = new Vector2Int(8, 8);
    public float floorHeight = 3f;
    public float wallHeight = 2.8f;
    public float wallThickness = 0.2f;
    public float corridorWidth = 2f;

    [Header("Configuration des Portes")]
    public float doorWidth = 1.2f;
    public float doorHeight = 2.2f;

    [Header("Joueur")]
    public GameObject playerPrefab;
    public float playerHeight = 1.8f;
    private GameObject player;

    // Listes pour stocker les pi�ces par �tage
    private List<List<RectInt>> floorRooms = new List<List<RectInt>>();
    // Position des escaliers
    private List<Vector3> stairPositions = new List<Vector3>();
    // Structure pour stocker les connexions entre pi�ces (pour placer les portes)
    private Dictionary<int, List<RoomConnection>> floorConnections = new Dictionary<int, List<RoomConnection>>();

    void Start()
    {
        GenerateHouse();
        SpawnPlayer();
    }

    void GenerateHouse()
    {
        // Initialiser les listes pour chaque �tage
        for (int floor = 0; floor < numberOfFloors; floor++)
        {
            floorRooms.Add(new List<RectInt>());
            floorConnections[floor] = new List<RoomConnection>();
        }

        // G�n�rer les pi�ces pour chaque �tage
        for (int floor = 0; floor < numberOfFloors; floor++)
        {
            GenerateFloor(floor);
        }

        // Ajouter des escaliers entre les �tages
        ConnectFloors();
    }

    void GenerateFloor(int floorNumber)
    {
        List<RectInt> currentFloorRooms = floorRooms[floorNumber];
        float floorY = floorNumber * floorHeight;

        // Cr�er la premi�re pi�ce (entr�e ou pi�ce principale)
        Vector2Int firstRoomSize = new Vector2Int(
            Random.Range(roomSizeMin.x, roomSizeMax.x + 1),
            Random.Range(roomSizeMin.y, roomSizeMax.y + 1)
        );

        // Placement d�pendant de l'�tage
        Vector2Int firstRoomPos;
        if (floorNumber == 0)
        {
            // Rez-de-chauss�e: entr�e centr�e
            firstRoomPos = new Vector2Int(0, 0);
        }
        else
        {
            // �tages sup�rieurs: aligner avec l'escalier du dessous si possible
            if (stairPositions.Count > 0)
            {
                Vector3 lastStair = stairPositions[stairPositions.Count - 1];
                firstRoomPos = new Vector2Int(Mathf.RoundToInt(lastStair.x), Mathf.RoundToInt(lastStair.z));
            }
            else
            {
                firstRoomPos = new Vector2Int(0, 0);
            }
        }

        RectInt firstRoom = new RectInt(firstRoomPos, firstRoomSize);
        currentFloorRooms.Add(firstRoom);
        CreateRoom(firstRoom, floorY);

        // G�n�rer les autres pi�ces
        for (int i = 1; i < numberOfRoomsPerFloor; i++)
        {
            // Choisir une pi�ce existante pour connecter
            int connectToIndex = Random.Range(0, currentFloorRooms.Count);
            RectInt connectedRoom = currentFloorRooms[connectToIndex];

            // D�terminer une direction al�atoire (0 = nord, 1 = est, 2 = sud, 3 = ouest)
            int direction = Random.Range(0, 4);

            Vector2Int size = new Vector2Int(
                Random.Range(roomSizeMin.x, roomSizeMax.x + 1),
                Random.Range(roomSizeMin.y, roomSizeMax.y + 1)
            );

            Vector2Int position = Vector2Int.zero;

            // Positionner la nouvelle pi�ce en fonction de la direction
            switch (direction)
            {
                case 0: // Nord
                    position = new Vector2Int(
                        connectedRoom.x + Random.Range(0, connectedRoom.width - 2),
                        connectedRoom.y + connectedRoom.height + 2
                    );
                    break;
                case 1: // Est
                    position = new Vector2Int(
                        connectedRoom.x + connectedRoom.width + 2,
                        connectedRoom.y + Random.Range(0, connectedRoom.height - 2)
                    );
                    break;
                case 2: // Sud
                    position = new Vector2Int(
                        connectedRoom.x + Random.Range(0, connectedRoom.width - 2),
                        connectedRoom.y - size.y - 2
                    );
                    break;
                case 3: // Ouest
                    position = new Vector2Int(
                        connectedRoom.x - size.x - 2,
                        connectedRoom.y + Random.Range(0, connectedRoom.height - 2)
                    );
                    break;
            }

            RectInt newRoom = new RectInt(position, size);

            // V�rifier les chevauchements
            bool overlaps = false;
            foreach (var room in currentFloorRooms)
            {
                // Agrandir l�g�rement la zone pour �viter des pi�ces trop proches
                RectInt expandedRoom = new RectInt(
                    room.x - 1, room.y - 1,
                    room.width + 2, room.height + 2
                );

                if (newRoom.Overlaps(expandedRoom))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                int newRoomIndex = currentFloorRooms.Count;
                currentFloorRooms.Add(newRoom);
                CreateRoom(newRoom, floorY);

                // Connecter la nouvelle pi�ce � la pi�ce existante
                Vector2Int from = Vector2Int.RoundToInt(connectedRoom.center);
                Vector2Int to = Vector2Int.RoundToInt(newRoom.center);

                // Stocker la connexion pour placer une porte plus tard
                RoomConnection connection = new RoomConnection(connectToIndex, newRoomIndex, direction);
                floorConnections[floorNumber].Add(connection);

                CreateCorridor(from, to, floorY);
            }
            else
            {
                i--; // R�essayer
            }
        }

        // Maintenant que toutes les pi�ces sont cr��es, ajouter les portes
        CreateDoors(floorNumber);
    }

    void CreateRoom(RectInt room, float floorY)
    {
        string floorName = floorY == 0 ? "RDC" : "�tage " + (Mathf.RoundToInt(floorY / floorHeight));

        // Cr�er un conteneur pour la pi�ce
        GameObject roomObj = new GameObject("Pi�ce " + floorName + " [" + room.x + "," + room.y + "]");
        roomObj.transform.parent = this.transform;

        // Cr�e le sol
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Sol";
        floor.transform.position = new Vector3(room.x + room.width / 2f, floorY, room.y + room.height / 2f);
        floor.transform.localScale = new Vector3(room.width, 0.2f, room.height);
        floor.transform.parent = roomObj.transform;

        // Mat�riau pour le sol (pour distinguer les pi�ces)
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        floorRenderer.material.color = new Color(
            Random.Range(0.6f, 0.9f),
            Random.Range(0.6f, 0.9f),
            Random.Range(0.6f, 0.9f)
        );

        // Murs haut/bas - On ne cr�e pas les portes ici, elles seront ajout�es plus tard
        CreateWall(room.x + room.width / 2f, floorY + wallHeight / 2f, room.y + room.height, room.width, wallHeight, wallThickness, roomObj.transform, "north"); // haut
        CreateWall(room.x + room.width / 2f, floorY + wallHeight / 2f, room.y, room.width, wallHeight, wallThickness, roomObj.transform, "south"); // bas

        // Murs gauche/droite
        CreateWall(room.x, floorY + wallHeight / 2f, room.y + room.height / 2f, wallThickness, wallHeight, room.height, roomObj.transform, "west"); // gauche
        CreateWall(room.x + room.width, floorY + wallHeight / 2f, room.y + room.height / 2f, wallThickness, wallHeight, room.height, roomObj.transform, "east"); // droite

        // Plafond (sauf pour le dernier �tage)
        if (floorY / floorHeight < numberOfFloors - 1)
        {
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Plafond";
            ceiling.transform.position = new Vector3(room.x + room.width / 2f, floorY + wallHeight, room.y + room.height / 2f);
            ceiling.transform.localScale = new Vector3(room.width, 0.2f, room.height);
            ceiling.transform.parent = roomObj.transform;

            Renderer ceilingRenderer = ceiling.GetComponent<Renderer>();
            ceilingRenderer.material.color = new Color(0.95f, 0.95f, 0.95f);
        }
    }

    void CreateWallWithDoorSpace(float x, float y, float z, float sx, float sy, float sz, Transform parent, string wallDirection, bool hasDoor, Vector3? doorLocalPos = null, float doorWidth = 1.2f)
    {
        if (!hasDoor || doorLocalPos == null)
        {
            // Mur normal sans ouverture
            CreateWall(x, y, z, sx, sy, sz, parent, wallDirection);
            return;
        }

        Vector3 doorPos = doorLocalPos.Value;
        float leftWidth = (doorPos.x - doorWidth / 2f) - (x - sx / 2f);
        float rightWidth = (x + sx / 2f) - (doorPos.x + doorWidth / 2f);
        float topHeight = (y + sy / 2f) - (doorPos.y + doorHeight / 2f);

        // Partie gauche du mur
        if (leftWidth > 0.01f)
        {
            Vector3 pos = new Vector3(x - sx / 2f + leftWidth / 2f, y, z);
            Vector3 scale = new Vector3(leftWidth, sy, sz);
            CreateWallSegment(pos, scale, parent, wallDirection + "_Left");
        }

        // Partie droite du mur
        if (rightWidth > 0.01f)
        {
            Vector3 pos = new Vector3(x + sx / 2f - rightWidth / 2f, y, z);
            Vector3 scale = new Vector3(rightWidth, sy, sz);
            CreateWallSegment(pos, scale, parent, wallDirection + "_Right");
        }

        // Partie au-dessus de la porte
        if (topHeight > 0.01f)
        {
            Vector3 pos = new Vector3(doorPos.x, doorPos.y + topHeight / 2f + doorHeight / 2f, z);
            Vector3 scale = new Vector3(doorWidth, topHeight, sz);
            CreateWallSegment(pos, scale, parent, wallDirection + "_Top");
        }
    }


    void CreateDoors(int floorNumber)
    {
        float floorY = floorNumber * floorHeight;
        List<RectInt> rooms = floorRooms[floorNumber];

        foreach (RoomConnection connection in floorConnections[floorNumber])
        {
            RectInt room1 = rooms[connection.room1Index];
            RectInt room2 = rooms[connection.room2Index];

            // D�terminer la position de la porte en fonction de la direction
            Vector3 doorPosition = Vector3.zero;
            Vector3 doorScale = Vector3.zero;

            // Identifier les murs � "couper"
            GameObject wallToCut = null;
            Vector3 cutPosition = Vector3.zero;
            Vector3 cutSize = Vector3.zero;

            switch (connection.direction)
            {
                case 0: // Nord (room1 est au sud de room2)
                        // Trouver un point de passage viable entre les deux pi�ces
                    int doorX = FindValidDoorPosition(room1.x, room1.x + room1.width, room2.x, room2.x + room2.width);

                    doorPosition = new Vector3(
                        doorX,
                        floorY + doorHeight / 2f,
                        room2.y
                    );
                    doorScale = new Vector3(doorWidth, doorHeight, wallThickness);

                    // Trouver le mur � couper
                    cutPosition = new Vector3(doorX, floorY + wallHeight / 2f, room2.y);
                    cutSize = new Vector3(doorWidth, doorHeight, wallThickness);
                    break;

                case 1: // Est (room1 est � l'ouest de room2)
                    int doorZ = FindValidDoorPosition(room1.y, room1.y + room1.height, room2.y, room2.y + room2.height);

                    doorPosition = new Vector3(
                        room2.x,
                        floorY + doorHeight / 2f,
                        doorZ
                    );
                    doorScale = new Vector3(wallThickness, doorHeight, doorWidth);

                    cutPosition = new Vector3(room2.x, floorY + wallHeight / 2f, doorZ);
                    cutSize = new Vector3(wallThickness, doorHeight, doorWidth);
                    break;

                case 2: // Sud (room1 est au nord de room2)
                    int doorX2 = FindValidDoorPosition(room1.x, room1.x + room1.width, room2.x, room2.x + room2.width);

                    doorPosition = new Vector3(
                        doorX2,
                        floorY + doorHeight / 2f,
                        room2.y + room2.height
                    );
                    doorScale = new Vector3(doorWidth, doorHeight, wallThickness);

                    cutPosition = new Vector3(doorX2, floorY + wallHeight / 2f, room2.y + room2.height);
                    cutSize = new Vector3(doorWidth, doorHeight, wallThickness);
                    break;

                case 3: // Ouest (room1 est � l'est de room2)
                    int doorZ2 = FindValidDoorPosition(room1.y, room1.y + room1.height, room2.y, room2.y + room2.height);

                    doorPosition = new Vector3(
                        room2.x + room2.width,
                        floorY + doorHeight / 2f,
                        doorZ2
                    );
                    doorScale = new Vector3(wallThickness, doorHeight, doorWidth);

                    cutPosition = new Vector3(room2.x + room2.width, floorY + wallHeight / 2f, doorZ2);
                    cutSize = new Vector3(wallThickness, doorHeight, doorWidth);
                    break;
            }

            // Cr�er la porte et effectuer la coupe dans le mur
            GameObject door = new GameObject("Porte " + connection.room1Index + " -> " + connection.room2Index);
            door.transform.parent = this.transform;
            door.transform.position = doorPosition;

            // Cr�er l'ouverture dans le mur
            CutWallForDoor(cutPosition, cutSize, floorY, connection.direction);

            // Ajouter un BoxCollider pour cr�er un "trou" dans le mur
            BoxCollider doorCollider = door.AddComponent<BoxCollider>();
            doorCollider.size = doorScale;
            doorCollider.isTrigger = true; // Pour que le joueur puisse traverser

            // Ajouter un cadre de porte visuel
            CreateDoorFrame(doorPosition, doorScale, door.transform);
        }
    }

    int FindValidDoorPosition(int min1, int max1, int min2, int max2)
    {
        // Trouver la zone de chevauchement
        int overlapMin = Mathf.Max(min1, min2);
        int overlapMax = Mathf.Min(max1, max2);

        // Si pas de chevauchement, utiliser le point m�dian entre les deux pi�ces
        if (overlapMax <= overlapMin)
        {
            return (min1 + max1 + min2 + max2) / 4; // Point m�dian
        }

        // Trouver un point au milieu de la zone de chevauchement pour placer la porte
        int doorPosition = overlapMin + (overlapMax - overlapMin) / 2;

        // S'assurer que la porte ne sera pas trop proche du bord
        int doorOffset = Mathf.CeilToInt(doorWidth / 2f) + 1;

        if (doorPosition - doorOffset < overlapMin)
        {
            doorPosition = overlapMin + doorOffset;
        }
        else if (doorPosition + doorOffset > overlapMax)
        {
            doorPosition = overlapMax - doorOffset;
        }

        return doorPosition;
    }

    void CutWallForDoor(Vector3 position, Vector3 size, float floorY, int direction)
    {
        // Trouver tous les murs qui pourraient �tre affect�s
        Collider[] colliders = Physics.OverlapBox(position, size / 2f);

        foreach (var collider in colliders)
        {
            // V�rifier si c'est un mur
            if (collider.gameObject.name.Contains("Mur"))
            {
                // Diff�rentes approches selon la direction de la porte
                if ((direction == 0 || direction == 2) && collider.gameObject.name.Contains("north") ||
                    collider.gameObject.name.Contains("south"))
                {
                    // Pour les murs nord/sud
                    ModifyWallForDoor(collider.gameObject, position, size);
                }
                else if ((direction == 1 || direction == 3) && collider.gameObject.name.Contains("east") ||
                         collider.gameObject.name.Contains("west"))
                {
                    // Pour les murs est/ouest
                    ModifyWallForDoor(collider.gameObject, position, size);
                }
            }
        }
    }

    void ModifyWallForDoor(GameObject wall, Vector3 doorPosition, Vector3 doorSize)
    {
        Transform originalWallTransform = wall.transform;
        Vector3 wallPos = originalWallTransform.position;
        Vector3 wallScale = originalWallTransform.localScale;

        bool isHorizontal = wallScale.x > wallScale.z;

        wall.SetActive(false); // On d�sactive le mur original

        if (isHorizontal)
        {
            float leftWidth = (doorPosition.x - doorSize.x / 2f) - (wallPos.x - wallScale.x / 2f);
            float rightWidth = (wallPos.x + wallScale.x / 2f) - (doorPosition.x + doorSize.x / 2f);

            // Mur gauche (mur 1)
            if (leftWidth > 0.01f)
            {
                CreateWallSegment(
                    new Vector3(wallPos.x - wallScale.x / 2f + leftWidth / 2f, wallPos.y, wallPos.z),
                    new Vector3(leftWidth, wallScale.y, wallScale.z),
                    wall.transform.parent, wall.name + "_Left");
            }

            // Mur droit (mur 2)
            if (rightWidth > 0.01f)
            {
                CreateWallSegment(
                    new Vector3(wallPos.x + wallScale.x / 2f - rightWidth / 2f, wallPos.y, wallPos.z),
                    new Vector3(rightWidth, wallScale.y, wallScale.z),
                    wall.transform.parent, wall.name + "_Right");
            }

            // La partie au-dessus de la porte est supprim�e
        }
        else
        {
            // M�me logique, mais pour les murs verticaux (axe Z)
            float frontDepth = (doorPosition.z - doorSize.z / 2f) - (wallPos.z - wallScale.z / 2f);
            float backDepth = (wallPos.z + wallScale.z / 2f) - (doorPosition.z + doorSize.z / 2f);

            if (frontDepth > 0.01f)
            {
                CreateWallSegment(
                    new Vector3(wallPos.x, wallPos.y, wallPos.z - wallScale.z / 2f + frontDepth / 2f),
                    new Vector3(wallScale.x, wallScale.y, frontDepth),
                    wall.transform.parent, wall.name + "_Front");
            }

            if (backDepth > 0.01f)
            {
                CreateWallSegment(
                    new Vector3(wallPos.x, wallPos.y, wallPos.z + wallScale.z / 2f - backDepth / 2f),
                    new Vector3(wallScale.x, wallScale.y, backDepth),
                    wall.transform.parent, wall.name + "_Back");
            }

            // La partie au-dessus de la porte est supprim�e
        }
    }


    // Cr�er un segment de mur
    void CreateWallSegment(Vector3 position, Vector3 scale, Transform parent, string name)
    {
        GameObject wallSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallSegment.name = name;
        wallSegment.transform.position = position;
        wallSegment.transform.localScale = scale;
        wallSegment.transform.parent = parent;

        // Appliquer le m�me mat�riau que les autres murs
        Renderer wallRenderer = wallSegment.GetComponent<Renderer>();
        wallRenderer.material.color = new Color(0.9f, 0.9f, 0.9f);
    }

    
    // M�thode pour v�rifier et cr�er une ouverture dans un mur sur le chemin d'un corridor
    void CheckAndCreateOpenings(Vector3 position, float floorY, int dirX, int dirY)
    {
        // D�terminer la taille et la direction de l'ouverture
        Vector3 size;
        int direction;

        if (dirX != 0) // Mouvement horizontal
        {
            size = new Vector3(wallThickness, doorHeight, corridorWidth);
            direction = dirX > 0 ? 1 : 3; // Est ou Ouest
        }
        else // Mouvement vertical
        {
            size = new Vector3(corridorWidth, doorHeight, wallThickness);
            direction = dirY > 0 ? 0 : 2; // Nord ou Sud
        }

        // Rechercher les murs � cette position
        Collider[] colliders = Physics.OverlapBox(position, size / 2f);

        foreach (var collider in colliders)
        {
            if (collider.gameObject.name.Contains("Mur"))
            {
                // Cr�er une ouverture dans ce mur
                CutWallForDoor(position, size, floorY, direction);
                break;
            }
        }
    }

    void CreateDoorFrame(Vector3 position, Vector3 scale, Transform parent)
    {
        // Couleur du cadre de porte
        Color doorFrameColor = new Color(0.5f, 0.35f, 0.2f); // Marron pour simuler du bois

        // Calculer l'�paisseur du cadre
        float frameThickness = 0.1f;

        // Cr�er les montants verticaux du cadre
        if (scale.x > scale.z) // Porte orient�e le long de l'axe X
        {
            // Montant gauche
            GameObject leftPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPost.name = "MontantGauche";
            leftPost.transform.position = new Vector3(position.x - scale.x / 2 + frameThickness / 2, position.y, position.z);
            leftPost.transform.localScale = new Vector3(frameThickness, scale.y, scale.z);
            leftPost.transform.parent = parent;
            leftPost.GetComponent<Renderer>().material.color = doorFrameColor;

            // Montant droit
            GameObject rightPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPost.name = "MontantDroit";
            rightPost.transform.position = new Vector3(position.x + scale.x / 2 - frameThickness / 2, position.y, position.z);
            rightPost.transform.localScale = new Vector3(frameThickness, scale.y, scale.z);
            rightPost.transform.parent = parent;
            rightPost.GetComponent<Renderer>().material.color = doorFrameColor;

            // Traverse sup�rieure
            GameObject topBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBeam.name = "TraverseHaute";
            topBeam.transform.position = new Vector3(position.x, position.y + scale.y / 2 - frameThickness / 2, position.z);
            topBeam.transform.localScale = new Vector3(scale.x - 2 * frameThickness, frameThickness, scale.z);
            topBeam.transform.parent = parent;
            topBeam.GetComponent<Renderer>().material.color = doorFrameColor;
        }
        else // Porte orient�e le long de l'axe Z
        {
            // Montant avant
            GameObject frontPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontPost.name = "MontantAvant";
            frontPost.transform.position = new Vector3(position.x, position.y, position.z - scale.z / 2 + frameThickness / 2);
            frontPost.transform.localScale = new Vector3(scale.x, scale.y, frameThickness);
            frontPost.transform.parent = parent;
            frontPost.GetComponent<Renderer>().material.color = doorFrameColor;

            // Montant arri�re
            GameObject backPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backPost.name = "MontantArriere";
            backPost.transform.position = new Vector3(position.x, position.y, position.z + scale.z / 2 - frameThickness / 2);
            backPost.transform.localScale = new Vector3(scale.x, scale.y, frameThickness);
            backPost.transform.parent = parent;
            backPost.GetComponent<Renderer>().material.color = doorFrameColor;

            // Traverse sup�rieure
            GameObject topBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBeam.name = "TraverseHaute";
            topBeam.transform.position = new Vector3(position.x, position.y + scale.y / 2 - frameThickness / 2, position.z);
            topBeam.transform.localScale = new Vector3(scale.x, frameThickness, scale.z - 2 * frameThickness);
            topBeam.transform.parent = parent;
            topBeam.GetComponent<Renderer>().material.color = doorFrameColor;
        }
    }

    void CreateCorridor(Vector2Int from, Vector2Int to, float floorY)
    {
        // Cr�er un conteneur pour le couloir
        GameObject corridorObj = new GameObject("Couloir [" + from.x + "," + from.y + "] to [" + to.x + "," + to.y + "]");
        corridorObj.transform.parent = this.transform;

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = from;
        path.Add(current);

        // D'abord en X
        while (current.x != to.x)
        {
            int direction = (to.x > current.x) ? 1 : -1;
            current.x += direction;
            path.Add(current);
        }

        // Puis en Y
        while (current.y != to.y)
        {
            int direction = (to.y > current.y) ? 1 : -1;
            current.y += direction;
            path.Add(current);
        }

        // Cr�er les tuiles du couloir
        for (int i = 0; i < path.Count; i++)
        {
            CreateCorridorTile(path[i], floorY, wallHeight, wallThickness, corridorWidth, corridorObj.transform);
        }
    }

    void CreateCorridorTile(Vector2Int position, float floorY, float wallHeight, float wallThickness, float corridorWidth, Transform parent)
    {
        // Sol du couloir
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Sol Couloir";
        floor.transform.position = new Vector3(position.x, floorY, position.y);
        floor.transform.localScale = new Vector3(corridorWidth, 0.2f, corridorWidth);
        floor.transform.parent = parent;

        // Couleur du couloir
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        floorRenderer.material.color = new Color(0.4f, 0.4f, 0.4f);

        // Plafond du couloir (sauf pour le dernier �tage)
        if (floorY / floorHeight < numberOfFloors - 1)
        {
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Plafond Couloir";
            ceiling.transform.position = new Vector3(position.x, floorY + wallHeight, position.y);
            ceiling.transform.localScale = new Vector3(corridorWidth, 0.2f, corridorWidth);
            ceiling.transform.parent = parent;

            Renderer ceilingRenderer = ceiling.GetComponent<Renderer>();
            ceilingRenderer.material.color = new Color(0.95f, 0.95f, 0.95f);
        }
    }

    void ConnectFloors()
    {
        for (int floor = 0; floor < numberOfFloors - 1; floor++)
        {
            // Trouver une pi�ce appropri�e pour placer l'escalier
            if (floorRooms[floor].Count > 0 && floorRooms[floor + 1].Count > 0)
            {
                RectInt lowerRoom = floorRooms[floor][Random.Range(0, floorRooms[floor].Count)];

                // Placer l'escalier au centre de la pi�ce
                Vector3 stairPosition = new Vector3(
                    lowerRoom.center.x,
                    floor * floorHeight,
                    lowerRoom.center.y
                );

                // Cr�er l'escalier
                CreateStairs(stairPosition, floor);

                // Enregistrer la position de l'escalier
                stairPositions.Add(stairPosition);
            }
        }
    }

    void CreateStairs(Vector3 position, int fromFloor)
    {
        GameObject stairsObj = new GameObject("Escalier " + fromFloor + " -> " + (fromFloor + 1));
        stairsObj.transform.parent = this.transform;

        // Param�tres de l'escalier
        int numberOfSteps = 10;
        float stepWidth = 1.5f;
        float stepDepth = 0.3f;
        float stepHeight = floorHeight / numberOfSteps;

        // Cr�er un trou dans le plafond pour l'escalier
        CreateStairOpening(position, fromFloor, stepWidth + 0.5f, numberOfSteps * stepDepth + 0.5f);

        // Cr�er les marches
        for (int i = 0; i < numberOfSteps; i++)
        {
            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = "Marche " + i;

            float xPos = position.x;
            float yPos = position.y + i * stepHeight + stepHeight / 2;
            float zPos = position.z - i * stepDepth;

            step.transform.position = new Vector3(xPos, yPos, zPos);
            step.transform.localScale = new Vector3(stepWidth, stepHeight, stepDepth);
            step.transform.parent = stairsObj.transform;

            // Mat�riau pour l'escalier
            Renderer stepRenderer = step.GetComponent<Renderer>();
            stepRenderer.material.color = new Color(0.8f, 0.6f, 0.4f); // Couleur bois
        }

        // Ajouter des rampes
        CreateRailing(position, numberOfSteps, stepDepth, stepWidth, stepHeight, stairsObj.transform);
    }

    void CreateStairOpening(Vector3 position, int floor, float width, float depth)
    {
        // Cr�er un trou dans le plafond pour permettre au joueur de monter
        GameObject stairOpening = new GameObject("Ouverture Escalier");
        stairOpening.transform.parent = this.transform;
        stairOpening.transform.position = new Vector3(
            position.x,
            floor * floorHeight + floorHeight - 0.1f, // Juste sous le plafond
            position.z - depth / 2f
        );

        // Ajouter un BoxCollider pour cr�er un "trou" dans le plafond
        BoxCollider openingCollider = stairOpening.AddComponent<BoxCollider>();
        openingCollider.size = new Vector3(width, 0.3f, depth);
        openingCollider.isTrigger = true; // Pour que le joueur puisse traverser
    }

    void CreateRailing(Vector3 stairPosition, int steps, float stepDepth, float stepWidth, float stepHeight, Transform parent)
    {
        float railingHeight = 1.0f;
        float postWidth = 0.1f;

        // Rampe de gauche
        GameObject leftRailing = new GameObject("Rampe Gauche");
        leftRailing.transform.parent = parent;

        // Rampe de droite
        GameObject rightRailing = new GameObject("Rampe Droite");
        rightRailing.transform.parent = parent;

        // Cr�er les poteaux de rampe
        for (int i = 0; i < steps + 1; i++)
        {
            // Poteau gauche
            GameObject leftPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPost.name = "Poteau Gauche " + i;
            leftPost.transform.position = new Vector3(
                stairPosition.x - stepWidth / 2 + postWidth / 2,
                stairPosition.y + i * stepHeight + railingHeight / 2,
                stairPosition.z - i * stepDepth
            );
            leftPost.transform.localScale = new Vector3(postWidth, railingHeight, postWidth);
            leftPost.transform.parent = leftRailing.transform;

            // Poteau droit
            GameObject rightPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPost.name = "Poteau Droit " + i;
            rightPost.transform.position = new Vector3(
                stairPosition.x + stepWidth / 2 - postWidth / 2,
                stairPosition.y + i * stepHeight + railingHeight / 2,
                stairPosition.z - i * stepDepth
            );
            rightPost.transform.localScale = new Vector3(postWidth, railingHeight, postWidth);
            rightPost.transform.parent = rightRailing.transform;

            // D�finir le mat�riau
            Renderer leftRenderer = leftPost.GetComponent<Renderer>();
            leftRenderer.material.color = new Color(0.6f, 0.6f, 0.6f);

            Renderer rightRenderer = rightPost.GetComponent<Renderer>();
            rightRenderer.material.color = new Color(0.6f, 0.6f, 0.6f);

            // Ajouter une main courante horizontale
            if (i < steps)
            {
                // Main courante gauche
                GameObject leftHorizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftHorizontal.name = "Main Courante Gauche " + i;
                leftHorizontal.transform.position = new Vector3(
                    stairPosition.x - stepWidth / 2 + postWidth / 2,
                    stairPosition.y + i * stepHeight + railingHeight,
                    stairPosition.z - i * stepDepth - stepDepth / 2
                );
                leftHorizontal.transform.localScale = new Vector3(postWidth, postWidth, stepDepth);
                leftHorizontal.transform.parent = leftRailing.transform;

                // Main courante droite
                GameObject rightHorizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightHorizontal.name = "Main Courante Droite " + i;
                rightHorizontal.transform.position = new Vector3(
                    stairPosition.x + stepWidth / 2 - postWidth / 2,
                    stairPosition.y + i * stepHeight + railingHeight,
                    stairPosition.z - i * stepDepth - stepDepth / 2
                );
                rightHorizontal.transform.localScale = new Vector3(postWidth, postWidth, stepDepth);
                rightHorizontal.transform.parent = rightRailing.transform;

                // D�finir le mat�riau
                Renderer leftHorizRenderer = leftHorizontal.GetComponent<Renderer>();
                leftHorizRenderer.material.color = new Color(0.6f, 0.6f, 0.6f);

                Renderer rightHorizRenderer = rightHorizontal.GetComponent<Renderer>();
                rightHorizRenderer.material.color = new Color(0.6f, 0.6f, 0.6f);
            }
        }
    }

    void CreateWall(float x, float y, float z, float sx, float sy, float sz, Transform parent, string wallDirection)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Mur " + wallDirection;
        wall.transform.position = new Vector3(x, y, z);
        wall.transform.localScale = new Vector3(sx, sy, sz);
        wall.transform.parent = parent;

        // Ajouter un mat�riau aux murs
        Renderer wallRenderer = wall.GetComponent<Renderer>();
        wallRenderer.material.color = new Color(0.9f, 0.9f, 0.9f);
    }

    void SpawnPlayer()
    {
        // Utiliser la premi�re pi�ce du rez-de-chauss�e comme point de d�part
        if (floorRooms.Count > 0 && floorRooms[0].Count > 0)
        {
            Vector3 spawnPosition = new Vector3(
                floorRooms[0][0].center.x,
                playerHeight / 2f + 0.1f,
                floorRooms[0][0].center.y
            );

            // Si playerPrefab n'est pas assign�, cr�er une capsule simple
            if (playerPrefab == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.transform.localScale = new Vector3(1, playerHeight / 2f, 1);
                player.transform.position = spawnPosition;

                // Ajouter les composants n�cessaires
                player.AddComponent<Rigidbody>();
                player.GetComponent<Rigidbody>().freezeRotation = true;
                player.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
                player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

                // Ajouter le contr�leur de joueur
                player.AddComponent<PlayerController>();

                // Ajouter cam�ra
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.AddComponent<Camera>();
                cameraObj.transform.position = new Vector3(0, playerHeight / 2f, 0);
                cameraObj.transform.parent = player.transform;
                cameraObj.transform.localPosition = new Vector3(0, 0.7f, 0);
            }
            else
            {
                // Continuation de la m�thode SpawnPlayer()
                player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                player.name = "Player";

                // Si le prefab n'a pas de contr�leur, en ajouter un
                if (!player.GetComponent<PlayerController>())
                {
                    player.AddComponent<PlayerController>();
                }

                // V�rifier si le prefab a une cam�ra, sinon en ajouter une
                Camera playerCamera = player.GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    GameObject cameraObj = new GameObject("PlayerCamera");
                    cameraObj.AddComponent<Camera>();
                    cameraObj.transform.parent = player.transform;
                    cameraObj.transform.localPosition = new Vector3(0, 0.7f, 0);
                }
            }

            // Ajouter un composant mouvement si n�cessaire
            if (!player.GetComponent<PlayerController>())
            {
                player.AddComponent<PlayerController>();
            }
        }
        else
        {
            Debug.LogError("Impossible de faire appara�tre le joueur : aucune pi�ce g�n�r�e.");
        }
    }

    // Classe pour contr�ler le mouvement du joueur
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float lookSpeed = 3f;
        public float jumpForce = 5f;

        private Camera playerCamera;
        private Rigidbody rb;
        private bool isGrounded;
        private float rotationX = 0f;

        void Start()
        {
            // Obtenir les r�f�rences des composants
            playerCamera = GetComponentInChildren<Camera>();
            rb = GetComponent<Rigidbody>();

            // Verrouiller et cacher le curseur
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            // Rotation de la cam�ra (regarder autour)
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);

            // Sauter
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }

            // D�verrouiller/verrouiller le curseur avec la touche Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        void FixedUpdate()
        {
            // Mouvement du joueur
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = transform.right * horizontal + transform.forward * vertical;
            movement.Normalize();

            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

            // V�rifier si le joueur est au sol
            isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }

        void OnCollisionEnter(Collision collision)
        {
            // V�rifier si le joueur touche le sol
            if (collision.contacts[0].normal.y > 0.5f)
            {
                isGrounded = true;
            }
        }

        void OnCollisionExit(Collision collision)
        {
            // V�rifier si le joueur ne touche plus le sol
            isGrounded = false;
        }
    }

    // M�thode pour sauvegarder la configuration de la maison (pourrait �tre utilis�e pour un mode �diteur)
    public void SaveHouseConfiguration()
    {
        // Cr�er un objet de donn�es pour stocker la configuration
        HouseData data = new HouseData
        {
            numberOfRoomsPerFloor = this.numberOfRoomsPerFloor,
            numberOfFloors = this.numberOfFloors,
            roomSizeMin = this.roomSizeMin,
            roomSizeMax = this.roomSizeMax,
            floorHeight = this.floorHeight,
            wallHeight = this.wallHeight,
            wallThickness = this.wallThickness,
            corridorWidth = this.corridorWidth,
            doorWidth = this.doorWidth,
            doorHeight = this.doorHeight
        };

        // Sauvegarder les donn�es en JSON
        string json = JsonUtility.ToJson(data, true);

        // Dans un contexte r�el, on pourrait sauvegarder le fichier
        // Pour l'exemple, on affiche juste le JSON dans la console
        Debug.Log("Configuration sauvegard�e : " + json);

        // Ou sauvegarder dans PlayerPrefs
        PlayerPrefs.SetString("HouseConfig", json);
        PlayerPrefs.Save();
    }

    // M�thode pour charger la configuration de la maison
    public void LoadHouseConfiguration()
    {
        // V�rifier si une configuration existe
        if (PlayerPrefs.HasKey("HouseConfig"))
        {
            string json = PlayerPrefs.GetString("HouseConfig");
            HouseData data = JsonUtility.FromJson<HouseData>(json);

            // Appliquer les donn�es charg�es
            this.numberOfRoomsPerFloor = data.numberOfRoomsPerFloor;
            this.numberOfFloors = data.numberOfFloors;
            this.roomSizeMin = data.roomSizeMin;
            this.roomSizeMax = data.roomSizeMax;
            this.floorHeight = data.floorHeight;
            this.wallHeight = data.wallHeight;
            this.wallThickness = data.wallThickness;
            this.corridorWidth = data.corridorWidth;
            this.doorWidth = data.doorWidth;
            this.doorHeight = data.doorHeight;

            Debug.Log("Configuration charg�e avec succ�s.");
        }
        else
        {
            Debug.LogWarning("Aucune configuration sauvegard�e trouv�e.");
        }
    }

    // Classe pour stocker les donn�es de configuration de la maison
    [System.Serializable]
    public class HouseData
    {
        public int numberOfRoomsPerFloor;
        public int numberOfFloors;
        public Vector2Int roomSizeMin;
        public Vector2Int roomSizeMax;
        public float floorHeight;
        public float wallHeight;
        public float wallThickness;
        public float corridorWidth;
        public float doorWidth;
        public float doorHeight;
    }

    // M�thode pour r�g�n�rer la maison (utile pour l'�diteur Unity ou pour g�n�rer de nouvelles maisons)
    public void RegenerateHouse()
    {
        // Supprimer tous les enfants actuels
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // R�initialiser les listes
        floorRooms.Clear();
        stairPositions.Clear();
        floorConnections.Clear();

        // R�g�n�rer la maison
        GenerateHouse();

        // Replacer le joueur si n�cessaire
        if (player != null)
        {
            Destroy(player);
        }
        SpawnPlayer();
    }

    // Editor-only: M�thode pour visualiser la structure des pi�ces (utile pour le d�bogage)
    void OnDrawGizmos()
    {
        // Dessiner les pi�ces en mode �diteur
        for (int floor = 0; floor < floorRooms.Count; floor++)
        {
            float floorY = floor * floorHeight;

            // Couleur diff�rente pour chaque �tage
            Gizmos.color = new Color(
                0.2f + (float)floor / numberOfFloors * 0.8f,
                0.8f - (float)floor / numberOfFloors * 0.5f,
                0.2f,
                0.3f
            );

            // Dessiner chaque pi�ce
            foreach (RectInt room in floorRooms[floor])
            {
                Vector3 center = new Vector3(
                    room.x + room.width / 2f,
                    floorY + 0.5f,
                    room.y + room.height / 2f
                );

                Vector3 size = new Vector3(room.width, 1f, room.height);

                Gizmos.DrawCube(center, size);
                Gizmos.DrawWireCube(center, size);
            }
        }

        // Dessiner les positions des escaliers
        Gizmos.color = Color.blue;
        foreach (Vector3 stairPos in stairPositions)
        {
            Gizmos.DrawSphere(stairPos, 0.5f);
        }
    }
}