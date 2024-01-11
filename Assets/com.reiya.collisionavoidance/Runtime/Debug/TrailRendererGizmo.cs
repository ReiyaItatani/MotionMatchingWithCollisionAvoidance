using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;


namespace CollisionAvoidance{

public class TrailRendererGizmo : MonoBehaviour
{
    public Color trailColor = Color.red;
    public List<Vector3> points = new List<Vector3>();

    void Update()
    {
        points.Add(transform.position);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = trailColor;

        for (int i = 1; i < points.Count; i++)
        {
            Gizmos.DrawLine(points[i - 1], points[i]);
        }
    }

    public void SavePointsToCSV()
    {
        StringBuilder csv = new StringBuilder();
        foreach (Vector3 point in points)
        {
            csv.AppendLine($"{point.x},{point.y},{point.z}");
        }

        // セッションの開始時刻をファイル名に追加
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{gameObject.name}_TrailPoints_{timestamp}.csv";
        string filePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Users", fileName);

        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(filePath, csv.ToString());
}


}
}