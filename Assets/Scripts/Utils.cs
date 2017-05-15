using UnityEngine;

public class Utils : MonoBehaviour {

    public static Vector2 NormalL(Vector2 v) {
        return new Vector2(-v.y, v.x).normalized;
    }

    public static Vector2 NormalR(Vector2 v) {
        return new Vector2(v.y, -v.x).normalized;
    }
}
