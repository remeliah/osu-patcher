using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace _patcher.Resolver
{
    /// <summary>
    /// SpriteManager class.
    /// </summary>
    internal static class SpriteManager
    {
        private static FieldInfo _spriteManagerWidescreenField;
        private static MethodInfo _addMethod;
        private static Type _spriteManagerType;

        public static void AddToWidescreen(object playerInstance, object drawableInstance)
        {
            if (_spriteManagerWidescreenField == null)
            {
                var playerType = playerInstance.GetType();
                _spriteManagerType = playerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .GroupBy(f => f.FieldType)
                    .Where(g => g.Count() >= 3 && !g.Key.IsPrimitive && !g.Key.IsEnum && g.Key != typeof(string))
                    .Select(g => g.Key)
                    .FirstOrDefault(t => t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Count(lf => lf.FieldType.IsGenericType && lf.FieldType.GetGenericTypeDefinition() == typeof(List<>)) >= 2);

                if (_spriteManagerType == null) return;

                var spriteManagerFields = playerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => f.FieldType == _spriteManagerType)
                    .ToList();

                var boolFields = _spriteManagerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => f.FieldType == typeof(bool)).ToList();

                foreach (var field in spriteManagerFields)
                {
                    var instance = field.GetValue(playerInstance);
                    if (instance == null) continue;

                    int trueCount = boolFields.Count(bf => (bool)bf.GetValue(instance));
                    if (trueCount >= 2)
                    {
                        _spriteManagerWidescreenField = field;
                        break;
                    }
                }

                if (_spriteManagerWidescreenField == null && spriteManagerFields.Count > 1)
                    _spriteManagerWidescreenField = spriteManagerFields[1];

                if (_spriteManagerWidescreenField == null)
                    _spriteManagerWidescreenField = spriteManagerFields.First();

                _addMethod = _spriteManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                    {
                        var p = m.GetParameters();
                        if (p.Length != 1 || m.ReturnType != typeof(void)) return false;

                        return (p[0].ParameterType.IsClass &&
                                !p[0].ParameterType.IsGenericType &&
                                p[0].ParameterType != typeof(object) &&
                                p[0].ParameterType != typeof(string) &&
                                m.Name.Length <= 5);
                    });

                if (_addMethod == null)
                {
                    var lists = _spriteManagerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>)).ToList();
                    Type drawableType = null;
                    if (lists.Count > 0)
                        drawableType = lists[0].FieldType.GetGenericArguments()[0];

                    if (drawableType != null)
                    {
                        _addMethod = _spriteManagerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == drawableType && m.ReturnType == typeof(void));
                    }
                }
            }

            if (_spriteManagerWidescreenField != null && _addMethod != null)
            {
                var managerInstance = _spriteManagerWidescreenField.GetValue(playerInstance);
                if (managerInstance != null)
                {
                    _addMethod.Invoke(managerInstance, new[] { drawableInstance });
                }
            }
        }
    }
}
