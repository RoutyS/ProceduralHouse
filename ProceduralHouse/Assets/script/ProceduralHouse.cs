using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Joueur")]
    public GameObject playerPrefab;
    public float playerHeight = 1.8f;
    private GameObject player;

    // Listes pour stocker les pièces par étage
    private List<List<RectInt>> floorRooms = new List<List<RectInt>>();
    // Position des escaliers
    private List<Vector3> stairPositions = new List<Vector3>();

    void Start()
    {
        GenerateHouse();
        SpawnPlayer();
    }

    void GenerateHouse()
    {
        // Initialiser les listes pour chaque étage
        for (int floor = 0; floor < numberOfFloors; floor++)
        {
            floorRooms.Add(new List<RectInt>());
        }

        // Générer les pièces pour chaque étage
        for (int floor = 0; floor < numberOfFloors; floor++)
        {
            GenerateFloor(floor);
        }

        // Ajouter des escaliers entre les étages
        ConnectFloors();
    }

    void GenerateFloor(int floorNumber)
    {
        List<RectInt> currentFloorRooms = floorRooms[floorNumber];
        float floorY = floorNumber * floorHeight;

        // Créer la première pièce (entrée ou pièce principale)
        Vector2Int firstRoomSize = new Vector2Int(
            Random.Range(roomSizeMin.x, roomSizeMax.x + 1),
            Random.Range(roomSizeMin.y, roomSizeMax.y + 1)
        );

        // Placement dépendant de l'étage
        Vector2Int firstRoomPos;
        if (floorNumber == 0)
        {
            // Rez-de-chaussée: entrée centrée
            firstRoomPos = new Vector2Int(0, 0);
        }
        else
        {
            // Étages supérieurs: aligner avec l'escalier du dessous si possible
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

        // Générer les autres pièces
        for (int i = 1; i < numberOfRoomsPerFloor; i++)
        {
            // Choisir une pièce existante pour connecter
            RectInt connectedRoom = currentFloorRooms[Random.Range(0, currentFloorRooms.Count)];

            // Déterminer une direction aléatoire (0 = nord, 1 = est, 2 = sud, 3 = ouest)
            int direction = Random.Range(0, 4);

            Vector2Int size = new Vector2Int(
                Random.Range(roomSizeMin.x, roomSizeMax.x + 1),
                Random.Range(roomSizeMin.y, roomSizeMax.y + 1)
            );

            Vector2Int position = Vector2Int.zero;

            // Positionner la nouvelle pièce en fonction de la direction
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

            // Vérifier les chevauchements
            bool overlaps = false;
            foreach (var room in currentFloorRooms)
            {
                // Agrandir légèrement la zone pour éviter des pièces trop proches
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
                currentFloorRooms.Add(newRoom);
                CreateRoom(newRoom, floorY);

                // Connecter la nouvelle pièce à la pièce existante
                Vector2Int from = Vector2Int.RoundToInt(connectedRoom.center);
                Vector2Int to = Vector2Int.RoundToInt(newRoom.center);
                CreateCorridor(from, to, floorY);
            }
            else
            {
                i--; // Réessayer
            }
        }
    }

    void CreateRoom(RectInt room, float floorY)
    {
        string floorName = floorY == 0 ? "RDC" : "Étage " + (Mathf.RoundToInt(floorY / floorHeight));

        // Créer un conteneur pour la pièce
        GameObject roomObj = new GameObject("Pièce " + floorName + " [" + room.x + "," + room.y + "]");
        roomObj.transform.parent = this.transform;

        // Crée le sol
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Sol";
        floor.transform.position = new Vector3(room.x + room.width / 2f, floorY, room.y + room.height / 2f);
        floor.transform.localScale = new Vector3(room.width, 0.2f, room.height);
        floor.transform.parent = roomObj.transform;

        // Matériau pour le sol (pour distinguer les pièces)
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        floorRenderer.material.color = new Color(
            Random.Range(0.6f, 0.9f),
            Random.Range(0.6f, 0.9f),
            Random.Range(0.6f, 0.9f)
        );

        // Murs haut/bas
        CreateWall(room.x + room.width / 2f, floorY + wallHeight / 2f, room.y + room.height, room.width, wallHeight, wallThickness, roomObj.transform); // haut
        CreateWall(room.x + room.width / 2f, floorY + wallHeight / 2f, room.y, room.width, wallHeight, wallThickness, roomObj.transform); // bas

        // Murs gauche/droite
        CreateWall(room.x, floorY + wallHeight / 2f, room.y + room.height / 2f, wallThickness, wallHeight, room.height, roomObj.transform); // gauche
        CreateWall(room.x + room.width, floorY + wallHeight / 2f, room.y + room.height / 2f, wallThickness, wallHeight, room.height, roomObj.transform); // droite

        // Plafond (sauf pour le dernier étage)
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

    void CreateCorridor(Vector2Int from, Vector2Int to, float floorY)
    {
        // Créer un conteneur pour le couloir
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

        // Créer les tuiles du couloir
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

        // Plafond du couloir (sauf pour le dernier étage)
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
            // Trouver une pièce appropriée pour placer l'escalier
            if (floorRooms[floor].Count > 0 && floorRooms[floor + 1].Count > 0)
            {
                RectInt lowerRoom = floorRooms[floor][Random.Range(0, floorRooms[floor].Count)];

                // Placer l'escalier au centre de la pièce
                Vector3 stairPosition = new Vector3(
                    lowerRoom.center.x,
                    floor * floorHeight,
                    lowerRoom.center.y
                );

                // Créer l'escalier
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

        // Paramètres de l'escalier
        int numberOfSteps = 10;
        float stepWidth = 1.5f;
        float stepDepth = 0.3f;
        float stepHeight = floorHeight / numberOfSteps;

        // Créer les marches
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

            // Matériau pour l'escalier
            Renderer stepRenderer = step.GetComponent<Renderer>();
            stepRenderer.material.color = new Color(0.8f, 0.6f, 0.4f); // Couleur bois
        }

        // Ajouter des rampes (optionnel)
        CreateRailing(position, numberOfSteps, stepDepth, stepWidth, stepHeight, stairsObj.transform);
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

        // Créer les poteaux de rampe
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

            // Définir le matériau
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

                // Définir le matériau
                Renderer leftHorizRenderer = leftHorizontal.GetComponent<Renderer>();
                leftHorizRenderer.material.color = new Color(0.6f, 0.6f, 0.6f);

                Renderer rightHorizRenderer = rightHorizontal.GetComponent<Renderer>();
                rightHorizRenderer.material.color = new Color(0.6f, 0.6f, 0.6f);
            }
        }
    }

    void CreateWall(float x, float y, float z, float sx, float sy, float sz, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Mur";
        wall.transform.position = new Vector3(x, y, z);
        wall.transform.localScale = new Vector3(sx, sy, sz);
        wall.transform.parent = parent;

        // Ajouter un matériau aux murs
        Renderer wallRenderer = wall.GetComponent<Renderer>();
        wallRenderer.material.color = new Color(0.9f, 0.9f, 0.9f);
    }

    void SpawnPlayer()
    {
        // Utiliser la première pièce du rez-de-chaussée comme point de départ
        if (floorRooms.Count > 0 && floorRooms[0].Count > 0)
        {
            Vector3 spawnPosition = new Vector3(
                floorRooms[0][0].center.x,
                playerHeight / 2f + 0.1f,
                floorRooms[0][0].center.y
            );

            // Si playerPrefab n'est pas assigné, créer une capsule simple
            if (playerPrefab == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.transform.localScale = new Vector3(1, playerHeight / 2f, 1);
                player.transform.position = spawnPosition;

                // Ajouter les composants nécessaires
                player.AddComponent<Rigidbody>();
                player.GetComponent<Rigidbody>().freezeRotation = true;
                player.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
                player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

                // Ajouter le contrôleur de joueur
                player.AddComponent<PlayerController>();

                // Ajouter caméra
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.AddComponent<Camera>();
                cameraObj.transform.position = new Vector3(0, playerHeight / 2f, 0);
                cameraObj.transform.parent = player.transform;
                cameraObj.transform.localPosition = new Vector3(0, 0.7f, 0);
            }
            else
            {
                player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

                // S'assurer que le préfab a un contrôleur
                if (player.GetComponent<PlayerController>() == null)
                {
                    player.AddComponent<PlayerController>();
                }
            }
        }
    }
}

// Classe pour contrôler le joueur
public class PlayerController : MonoBehaviour
{
    [Header("Mouvement")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float mouseSensitivity = 2f;
    public float gravity = 20f;

    private Rigidbody rb;
    private Camera playerCamera;
    private float rotationX = 0f;
    private bool isGrounded = true;
    private float verticalVelocity = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = GetComponentInChildren<Camera>();

        // Cacher et verrouiller le curseur
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Rotation de la caméra avec la souris
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // Saut
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity = jumpForce;
            isGrounded = false;
        }

        // Appliquer la gravité
        if (!isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Libérer la souris avec Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    void FixedUpdate()
    {
        // Déplacement horizontal
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Calculer le vecteur de mouvement relatif à la direction regardée
        Vector3 movement = transform.right * moveHorizontal + transform.forward * moveVertical;
        movement = movement.normalized * moveSpeed * Time.fixedDeltaTime;

        // Appliquer le mouvement vertical
        Vector3 verticalMovement = new Vector3(0, verticalVelocity * Time.fixedDeltaTime, 0);

        // Combiner les mouvements
        Vector3 finalMovement = movement + verticalMovement;
        rb.MovePosition(rb.position + finalMovement);
    }

    void OnCollisionStay(Collision collision)
    {
        // Vérifier si le joueur touche le sol
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.7f)
            {
                isGrounded = true;
                verticalVelocity = 0;
                break;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Vérifier si le joueur quitte le sol
        isGrounded = false;
    }

    void ToggleCursorLock()
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