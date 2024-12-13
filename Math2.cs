
using Godot;

public static class Math2 {

    public static float GetJumpVelocity(float height, float gravity) {

        return Mathf.Sqrt(2*height*gravity);

    }

}