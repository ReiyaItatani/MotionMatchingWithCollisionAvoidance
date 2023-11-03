using MotionMatching;
using UnityEngine;
using Drawing;

namespace CollisionAvoidance{
public class NormalVector : MonoBehaviour
{
    private Vector3 perpendicularVector = Vector3.zero;
    /// <summary>
    /// Calculates the normal vector from the wall (represented by the forward vector of this transform) towards a given point in the xz plane.
    /// </summary>
    /// <param name="currentPosition">The position of the point in the xz plane from which the normal vector should be directed.</param>
    /// <returns>The normalized vector directed from the wall's forward direction towards the given point on the xz plane.</returns>
    public Vector3 CalculateNormalVectorFromWall(Vector3 currentPosition)
    {
        // The direction the wall (or line) is facing.
        Vector3 wallDirection = this.transform.forward;
        
        // The direction pointing from the wall's center position to the given point.
        Vector3 directionToCurrentPosition = currentPosition - this.transform.position;

        // Base vector for xz plane. We'll use this to find a vector perpendicular to wallDirection.
        Vector3 xzBaseVector = new Vector3(1, 0, 0);
        
        // If the wall direction is nearly parallel to the x-axis, switch the base vector to the z-axis.
        if (Mathf.Abs(Vector3.Dot(wallDirection.normalized, xzBaseVector)) > 0.9f)
        {
            xzBaseVector = new Vector3(0, 0, 1);
        }

        // Compute a vector in the xz plane that's perpendicular to the wall direction.
        Vector3 xzPerpendicular = Vector3.Cross(wallDirection, xzBaseVector).normalized;

        // Subtracting the projection of directionToCurrentPosition onto wallDirection from directionToCurrentPosition 
        // gives us the desired normal vector.
        Vector3 normalVector = directionToCurrentPosition - Vector3.Dot(directionToCurrentPosition, wallDirection) * wallDirection;
        
        // Normalize the resulting vector.
        normalVector = normalVector.normalized;

        // Scale the normalized vector based on the inverse of the distance to the wall.
        float distance = directionToCurrentPosition.magnitude;

        // To prevent division by zero or extremely high values, we can clamp the minimum distance.
        const float minDistance = 0.01f;
        if (distance < minDistance)
        {
            distance = minDistance;
        }

        return normalVector / distance;
    }

    
    void Update(){
        Draw.ArrowheadArc(this.transform.position, perpendicularVector, 0.55f, Color.blue);
    }
}
}