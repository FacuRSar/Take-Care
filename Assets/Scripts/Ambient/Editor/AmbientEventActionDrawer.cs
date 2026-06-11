using UnityEditor;
using UnityEngine;

/* dibuja cada AmbientEventAction en el inspector mostrando solo los campos del tipo elegido.
*  asi no queda una lista infinita de campos que no se usan. cada accion ademas es plegable.
*/
[CustomPropertyDrawer(typeof(AmbientEventAction))]
public class AmbientEventActionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        AmbientActionType type = GetType(property);

        float spacing = EditorGUIUtility.standardVerticalSpacing;
        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // encabezado plegable con el nombre del tipo, para reconocer la accion de un vistazo
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, "Accion: " + type, true);
        line.y += line.height + spacing;

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        DrawField(ref line, property, "type", spacing);
        DrawField(ref line, property, "delay", spacing);

        foreach (string field in FieldsFor(type))
        {
            DrawField(ref line, property, field, spacing);
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float height = EditorGUIUtility.singleLineHeight + spacing;

        if (!property.isExpanded)
        {
            return height;
        }

        height += FieldHeight(property, "type") + spacing;
        height += FieldHeight(property, "delay") + spacing;

        foreach (string field in FieldsFor(GetType(property)))
        {
            height += FieldHeight(property, field) + spacing;
        }

        return height;
    }

    private void DrawField(ref Rect line, SerializedProperty property, string fieldName, float spacing)
    {
        SerializedProperty field = property.FindPropertyRelative(fieldName);

        if (field == null)
        {
            return;
        }

        float height = EditorGUI.GetPropertyHeight(field, true);
        Rect rect = new Rect(line.x, line.y, line.width, height);

        EditorGUI.PropertyField(rect, field, true);
        line.y += height + spacing;
    }

    private float FieldHeight(SerializedProperty property, string fieldName)
    {
        SerializedProperty field = property.FindPropertyRelative(fieldName);
        return field != null ? EditorGUI.GetPropertyHeight(field, true) : 0f;
    }

    private AmbientActionType GetType(SerializedProperty property)
    {
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        return (AmbientActionType)typeProp.enumValueIndex;
    }

    // que campos mostrar segun el tipo de accion
    private string[] FieldsFor(AmbientActionType type)
    {
        switch (type)
        {
            case AmbientActionType.EnablePhysics:
                return new[] { "target", "physicsImpulse" };

            case AmbientActionType.SetActive:
                return new[] { "target", "setActiveValue" };

            case AmbientActionType.MoveTo:
                return new[] { "target", "moveLocal", "moveTarget", "alsoRotate", "rotateTarget", "moveDuration" };

            case AmbientActionType.PlaySfx:
                return new[] { "sfxId", "sfx3D", "target" };

            case AmbientActionType.PlayDialogue:
                return new[] { "dialogueId" };

            case AmbientActionType.ShakeCamera:
                return new[] { "shakeEffectId" };

            case AmbientActionType.ScreenEffect:
                return new[] { "screenEffectId", "stopScreenEffect", "screenEffectAutoStop" };

            case AmbientActionType.SpawnPrefab:
                return new[] { "prefab", "spawnPoint" };

            case AmbientActionType.SetFlag:
                return new[] { "flagName", "flagValue" };

            default:
                return new string[0];
        }
    }
}
