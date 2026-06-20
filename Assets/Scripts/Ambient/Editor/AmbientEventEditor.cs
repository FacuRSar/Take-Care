using UnityEditor;
using UnityEngine;

/* Inspector ordenado para AmbientEvent: muestra solo los campos que aplican
*  segun el TriggerMode seleccionado.
*/
[CustomEditor(typeof(AmbientEvent))]
public class AmbientEventEditor : Editor
{
    private SerializedProperty eventId;
    private SerializedProperty triggerMode;

    private SerializedProperty requiredFlag;
    private SerializedProperty additionalRequiredFlags;
    private SerializedProperty startDelay;
    private SerializedProperty onlyPlayer;

    private SerializedProperty lookTarget;
    private SerializedProperty lookDistance;
    private SerializedProperty lookAngle;
    private SerializedProperty lookHoldTime;
    private SerializedProperty requireLineOfSight;
    private SerializedProperty lookMask;

    private SerializedProperty interactPrompt;

    private SerializedProperty triggerOnce;
    private SerializedProperty debugLog;
    private SerializedProperty actions;

    private void OnEnable()
    {
        eventId = serializedObject.FindProperty("eventId");
        triggerMode = serializedObject.FindProperty("triggerMode");

        requiredFlag = serializedObject.FindProperty("requiredFlag");
        additionalRequiredFlags = serializedObject.FindProperty("additionalRequiredFlags");
        startDelay = serializedObject.FindProperty("startDelay");
        onlyPlayer = serializedObject.FindProperty("onlyPlayer");

        lookTarget = serializedObject.FindProperty("lookTarget");
        lookDistance = serializedObject.FindProperty("lookDistance");
        lookAngle = serializedObject.FindProperty("lookAngle");
        lookHoldTime = serializedObject.FindProperty("lookHoldTime");
        requireLineOfSight = serializedObject.FindProperty("requireLineOfSight");
        lookMask = serializedObject.FindProperty("lookMask");

        interactPrompt = serializedObject.FindProperty("interactPrompt");

        triggerOnce = serializedObject.FindProperty("triggerOnce");
        debugLog = serializedObject.FindProperty("debugLog");
        actions = serializedObject.FindProperty("actions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Identificacion
        EditorGUILayout.PropertyField(eventId);

        EditorGUILayout.Space();

        // Disparo
        EditorGUILayout.LabelField("Disparo", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(triggerMode);

        AmbientEvent.TriggerMode mode = (AmbientEvent.TriggerMode)triggerMode.enumValueIndex;

        EditorGUI.indentLevel++;
        switch (mode)
        {
            case AmbientEvent.TriggerMode.PlayerEnter:
                EditorGUILayout.PropertyField(onlyPlayer);
                EditorGUILayout.HelpBox("Dispara cuando algo entra al trigger. Necesita un Collider con Is Trigger.", MessageType.None);
                break;

            case AmbientEvent.TriggerMode.OnFlag:
                EditorGUILayout.PropertyField(requiredFlag);
                EditorGUILayout.PropertyField(additionalRequiredFlags, true);
                EditorGUILayout.HelpBox(
                    "Dispara cuando todas las flags listadas estan en true. " +
                    "Required Flag sigue funcionando sola como antes; Additional Required Flags es opcional.",
                    MessageType.None);
                break;

            case AmbientEvent.TriggerMode.Timed:
                EditorGUILayout.PropertyField(startDelay);
                EditorGUILayout.HelpBox("Dispara solo, esperando este tiempo al iniciar la escena.", MessageType.None);
                break;

            case AmbientEvent.TriggerMode.Manual:
                EditorGUILayout.HelpBox("Se dispara a mano desde el AmbientEventManager usando el Event Id.", MessageType.None);
                break;

            case AmbientEvent.TriggerMode.PlayerLook:
                EditorGUILayout.PropertyField(lookTarget);
                EditorGUILayout.PropertyField(lookDistance);
                EditorGUILayout.PropertyField(lookAngle);
                EditorGUILayout.PropertyField(lookHoldTime);
                EditorGUILayout.PropertyField(requireLineOfSight);
                if (requireLineOfSight.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(lookMask);
                    EditorGUI.indentLevel--;
                }
                break;

            case AmbientEvent.TriggerMode.OnInteract:
                EditorGUILayout.PropertyField(interactPrompt);
                EditorGUILayout.HelpBox("Dispara cuando el jugador interactua (E) con el objeto. Necesita un Collider en la layer de interaccion.", MessageType.None);
                break;
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // Configuracion
        EditorGUILayout.LabelField("Configuracion", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(triggerOnce);
        EditorGUILayout.PropertyField(debugLog);

        EditorGUILayout.Space();

        // Acciones
        EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(actions, true);

        serializedObject.ApplyModifiedProperties();
    }
}
