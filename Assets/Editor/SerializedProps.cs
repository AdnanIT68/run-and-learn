using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Fluent wrapper for assigning private [SerializeField] fields from editor code,
    /// which is how the generator wires up references that a human would drag in
    /// the Inspector.
    /// </summary>
    internal class SerializedProps
    {
        private readonly SerializedObject _serialized;
        private readonly string _owner;

        public SerializedProps(Object target)
        {
            _serialized = new SerializedObject(target);
            _owner = target != null ? target.GetType().Name : "null";
        }

        public SerializedProps Obj(string field, Object value)
        {
            SerializedProperty property = Find(field);

            if (property != null)
            {
                property.objectReferenceValue = value;
            }

            return this;
        }

        public SerializedProps Num(string field, float value)
        {
            SerializedProperty property = Find(field);

            if (property == null)
            {
                return this;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = Mathf.RoundToInt(value);
            }
            else
            {
                property.floatValue = value;
            }

            return this;
        }

        public SerializedProps Flag(string field, bool value)
        {
            SerializedProperty property = Find(field);

            if (property != null)
            {
                property.boolValue = value;
            }

            return this;
        }

        public SerializedProps Mask(string field, LayerMask value)
        {
            SerializedProperty property = Find(field);

            if (property != null)
            {
                property.intValue = value.value;
            }

            return this;
        }

        public SerializedProps Tint(string field, Color value)
        {
            SerializedProperty property = Find(field);

            if (property != null)
            {
                property.colorValue = value;
            }

            return this;
        }

        public SerializedProps Array(string field, Object[] values)
        {
            SerializedProperty property = Find(field);

            if (property == null)
            {
                return this;
            }

            property.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            return this;
        }

        public void Apply()
        {
            _serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private SerializedProperty Find(string field)
        {
            SerializedProperty property = _serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError($"[SerializedProps] {_owner} has no serialized field '{field}'.");
            }

            return property;
        }
    }
}
