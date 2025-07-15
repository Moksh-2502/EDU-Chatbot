using UnityEngine;
using UnityEngine.AddressableAssets;
using EducationIntegration.QuestionHandlers;




#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This defines a "piece" of the track. This is attached to the prefab and contains data such as what obstacles can spawn on it.
/// It also defines places on the track where obstacles can spawn. The prefab is placed into a ThemeData list.
/// </summary>
public class TrackSegment : MonoBehaviour
{
    public Transform pathParent;
    public TrackManager manager;

	public Transform objectRoot;
	public Transform collectibleTransform;

    public AssetReference[] possibleObstacles; 

    [HideInInspector]
    public float[] obstaclePositions;

    public float worldLength { get { return m_WorldLength; } }

    protected float m_WorldLength;

    public bool IsReady {get; set;}

    private void Awake()
    {
        UpdateWorldLength();

		GameObject obj = new GameObject("ObjectRoot");
		obj.transform.SetParent(transform);
		objectRoot = obj.transform;

		obj = new GameObject("Collectibles");
		obj.transform.SetParent(objectRoot);
		collectibleTransform = obj.transform;
    }

    // Same as GetPointAt but using an interpolation parameter in world units instead of 0 to 1.
    public void GetPointAtInWorldUnit(float wt, out Vector3 pos, out Quaternion rot)
    {
        float t = wt / m_WorldLength;
        GetPointAt(t, out pos, out rot);
    }


	// Interpolation parameter t is clamped between 0 and 1.
	public void GetPointAt(float t, out Vector3 pos, out Quaternion rot)
    {
        float clampedT = Mathf.Clamp01(t);
        float scaledT = (pathParent.childCount - 1) * clampedT;
        int index = Mathf.FloorToInt(scaledT);
        float segmentT = scaledT - index;

        Transform orig = pathParent.GetChild(index);
        if (index == pathParent.childCount - 1)
        {
            pos = orig.position;
            rot = orig.rotation;
            return;
        }

        Transform target = pathParent.GetChild(index + 1);

        pos = Vector3.Lerp(orig.position, target.position, segmentT);
        rot = Quaternion.Lerp(orig.rotation, target.rotation, segmentT);
    }

    protected void UpdateWorldLength()
    {
        m_WorldLength = 0;

        for (int i = 1; i < pathParent.childCount; ++i)
        {
            Transform orig = pathParent.GetChild(i - 1);
            Transform end = pathParent.GetChild(i);

            Vector3 vec = end.position - orig.position;
            m_WorldLength += vec.magnitude;
        }
    }

	public void Cleanup()
	{
        var answerObjects = this.GetComponentsInChildren<AnswerObject>();
        if(answerObjects != null && answerObjects.Length > 0)
        {
            Debug.Log($"[AnswerObjects] Cleanup: {gameObject.name}");
            // Debug log this segment's position and the player's position and whether it is in front of the player or behind him
            var player = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            Debug.Log($"[AnswerObjects] Cleanup: {gameObject.name}, Position: {transform.position}, Player Position: {player.transform.position}, Is in front of player: {transform.position.z > player.transform.position.z}");
        }

		while(collectibleTransform.childCount > 0)
		{
			Transform t = collectibleTransform.GetChild(0);
			t.SetParent(null);
            Coin.coinPool.Free(t.gameObject);
		}

	    Addressables.ReleaseInstance(gameObject);
	}

    public void CleanupObstacles()
    {
        // Find all obstacle components in the segment
        Obstacle[] obstacles = this.GetComponentsInChildren<Obstacle>();
        foreach (Obstacle obstacle in obstacles)
        {
            if (obstacle.gameObject != null)
            {
                // Proper cleanup using Addressables
                Addressables.ReleaseInstance(obstacle.gameObject);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (pathParent == null)
            return;

        Color c = Gizmos.color;
        Gizmos.color = Color.red;
        for (int i = 1; i < pathParent.childCount; ++i)
        {
            Transform orig = pathParent.GetChild(i - 1);
            Transform end = pathParent.GetChild(i);

            Gizmos.DrawLine(orig.position, end.position);
        }

        Gizmos.color = Color.blue;
        for (int i = 0; i < obstaclePositions.Length; ++i)
        {
            Vector3 pos;
            Quaternion rot;
            GetPointAt(obstaclePositions[i], out pos, out rot);
            Gizmos.DrawSphere(pos, 0.5f);
        }

        Gizmos.color = c;
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TrackSegment))]
class TrackSegmentEditor : Editor
{
    protected TrackSegment m_Segment;

    public void OnEnable()
    {
        m_Segment = target as TrackSegment;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Add obstacles"))
        {
            ArrayUtility.Add(ref m_Segment.obstaclePositions, 0.0f);
        }

        if (m_Segment.obstaclePositions != null)
        {
            int toremove = -1;
            for (int i = 0; i < m_Segment.obstaclePositions.Length; ++i)
            {
                GUILayout.BeginHorizontal();
                m_Segment.obstaclePositions[i] = EditorGUILayout.Slider(m_Segment.obstaclePositions[i], 0.0f, 1.0f);
                if (GUILayout.Button("-", GUILayout.MaxWidth(32)))
                    toremove = i;
                GUILayout.EndHorizontal();
            }

            if (toremove != -1)
                ArrayUtility.RemoveAt(ref m_Segment.obstaclePositions, toremove);
        }
    }
}

#endif