using UnityEditor;
using UnityEngine;

/* Inspector ordenado para ProximityFeedback: muestra solo los campos que aplican
*  segun el modo de activacion y los toggles de vignette / pistas.
*/
[CustomEditor(typeof(ProximityFeedback))]
public class ProximityFeedbackEditor : Editor
{
    private SerializedProperty activation;
    private SerializedProperty activeFlagName;

    private SerializedProperty source;
    private SerializedProperty target;
    private SerializedProperty targetTag;

    private SerializedProperty startDistance;
    private SerializedProperty fullDistance;

    private SerializedProperty useVignette;
    private SerializedProperty vignetteEffectId;
    private SerializedProperty minIntensity;
    private SerializedProperty maxIntensity;
    private SerializedProperty changeSpeed;

    private SerializedProperty useDistanceHints;
    private SerializedProperty hintInterval;
    private SerializedProperty hintDuration;
    private SerializedProperty hintPriority;
    private SerializedProperty hintMaxDistance;
    private SerializedProperty hints;

    private void OnEnable()
    {
        activation = serializedObject.FindProperty("activation");
        activeFlagName = serializedObject.FindProperty("activeFlagName");

        source = serializedObject.FindProperty("source");
        target = serializedObject.FindProperty("target");
        targetTag = serializedObject.FindProperty("targetTag");

        startDistance = serializedObject.FindProperty("startDistance");
        fullDistance = serializedObject.FindProperty("fullDistance");

        useVignette = serializedObject.FindProperty("useVignette");
        vignetteEffectId = serializedObject.FindProperty("vignetteEffectId");
        minIntensity = serializedObject.FindProperty("minIntensity");
        maxIntensity = serializedObject.FindProperty("maxIntensity");
        changeSpeed = serializedObject.FindProperty("changeSpeed");

        useDistanceHints = serializedObject.FindProperty("useDistanceHints");
        hintInterval = serializedObject.FindProperty("hintInterval");
        hintDuration = serializedObject.FindProperty("hintDuration");
        hintPriority = serializedObject.FindProperty("hintPriority");
        hintMaxDistance = serializedObject.FindProperty("hintMaxDistance");
        hints = serializedObject.FindProperty("hints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Activacion
        EditorGUILayout.LabelField("Activacion", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(activation);

        if (activation.enumValueIndex == (int)ProximityFeedback.ActivationMode.WhileFlagOn)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(activeFlagName, new GUIContent("Active Flag Name"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Medicion
        EditorGUILayout.LabelField("Medicion", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(source);
        EditorGUILayout.PropertyField(target);
        EditorGUILayout.PropertyField(targetTag);

        EditorGUILayout.Space();

        // Rango
        EditorGUILayout.LabelField("Rango", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startDistance);
        EditorGUILayout.PropertyField(fullDistance);

        EditorGUILayout.Space();

        // Vignette
        EditorGUILayout.LabelField("Vignette", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useVignette, new GUIContent("Use Vignette"));

        if (useVignette.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(vignetteEffectId);
            EditorGUILayout.PropertyField(minIntensity);
            EditorGUILayout.PropertyField(maxIntensity);
            EditorGUILayout.PropertyField(changeSpeed);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Pistas por distancia
        EditorGUILayout.LabelField("Pistas por distancia", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useDistanceHints, new GUIContent("Use Distance Hints"));

        if (useDistanceHints.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(hintInterval);
            EditorGUILayout.PropertyField(hintDuration);
            EditorGUILayout.PropertyField(hintPriority);
            EditorGUILayout.PropertyField(hintMaxDistance, new GUIContent("Hint Max Distance (0 = sin limite)"));
            EditorGUILayout.PropertyField(hints, true);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
