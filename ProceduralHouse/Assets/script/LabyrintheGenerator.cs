using UnityEngine;

public class LabyrintheGenerator : MonoBehaviour
{
    [Header("Préfabriqués")]
    public GameObject solPrefab;
    public GameObject plafondPrefab;
    public GameObject murPrefab;
    public GameObject murAvecPortePrefab;

    [Header("Paramètres")]
    public int largeur = 3;
    public int hauteur = 3;
    public float taillePiece = 2f; // espacement entre pièces (ex : 2 unités)

    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Nord
        new Vector2Int(1, 0),   // Est
        new Vector2Int(0, -1),  // Sud
        new Vector2Int(-1, 0),  // Ouest
    };

    void Start()
    {
        GenererLabyrinthe();
    }

    void GenererLabyrinthe()
    {
        for (int x = 0; x < largeur; x++)
        {
            for (int y = 0; y < hauteur; y++)
            {
                Vector3 centre = new Vector3(x * taillePiece, 0, y * taillePiece);
                GenererPiece(centre, new Vector2Int(x, y));
            }
        }
    }

    void GenererPiece(Vector3 centre, Vector2Int coord)
    {
        // Sol en y = -1
        Instantiate(solPrefab, centre + Vector3.down, Quaternion.Euler(90, 0, 0), transform);

        // Plafond en y = 1
        Instantiate(plafondPrefab, centre + Vector3.up, Quaternion.Euler(90, 0, 0), transform);

        // Murs
        for (int i = 0; i < 4; i++)
        {
            Vector2Int dir = directions[i];
            Vector2Int voisin = coord + dir;

            bool aUneConnexion = (voisin.x >= 0 && voisin.x < largeur && voisin.y >= 0 && voisin.y < hauteur);
            GameObject prefab = aUneConnexion ? murAvecPortePrefab : murPrefab;

            Vector3 offset = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            switch (i)
            {
                case 0: // Nord (Z+)
                    offset = new Vector3(0, 0, 1); // Z = +1
                    rotation = Quaternion.Euler(0, 0, aUneConnexion ? 180 : 0);
                    break;
                case 1: // Est (X+)
                    offset = new Vector3(1, 0, 0); // X = +1
                    rotation = Quaternion.Euler(0, 90, aUneConnexion ? 180 : 0);
                    break;
                case 2: // Sud (Z-)
                    offset = new Vector3(0, 0, -1); // Z = -1
                    rotation = Quaternion.Euler(0, 180, aUneConnexion ? 180 : 0);
                    break;
                case 3: // Ouest (X-)
                    offset = new Vector3(-1, 0, 0); // X = -1
                    rotation = Quaternion.Euler(0, 270, aUneConnexion ? 180 : 0);
                    break;
            }

            Vector3 murPosition = centre + offset;
            Instantiate(prefab, murPosition, rotation, transform);
        }
    }
}
