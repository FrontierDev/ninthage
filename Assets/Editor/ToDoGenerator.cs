using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class ToDoGenerator
{
    [DidReloadScripts]
    private static void GenerateTodoFile()
    {
        try
        {
            List<string> lines = new List<string>();
            lines.Add("TODO REPORT");
            lines.Add($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            lines.Add(string.Empty);

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                if (assembly.IsDynamic)
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    CollectTypeTodos(type, lines);
                    CollectFieldTodos(type, lines);
                    CollectPropertyTodos(type, lines);
                    CollectMethodTodos(type, lines);
                }
            }

            if (lines.Count == 3)
            {
                lines.Add("No [ToDo] attributes found.");
            }

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string outputPath = Path.Combine(projectRoot, "todo.txt");

            File.WriteAllLines(outputPath, lines);

            Debug.Log($"Generated todo.txt at: {outputPath}");
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to generate todo.txt: {ex}");
        }
    }

    private static void CollectTypeTodos(Type type, List<string> lines)
    {
        object[] attributes = type.GetCustomAttributes(typeof(ToDoAttribute), false);

        foreach (ToDoAttribute todo in attributes)
        {
            lines.Add($"[TYPE] {type.FullName}");
            lines.Add($"  - {todo.Message}");
            lines.Add(string.Empty);
        }
    }

    private static void CollectFieldTodos(Type type, List<string> lines)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        FieldInfo[] fields = type.GetFields(flags);

        foreach (FieldInfo field in fields)
        {
            object[] attributes = field.GetCustomAttributes(typeof(ToDoAttribute), false);

            foreach (ToDoAttribute todo in attributes)
            {
                lines.Add($"[FIELD] {type.FullName}.{field.Name}");
                lines.Add($"  - {todo.Message}");
                lines.Add(string.Empty);
            }
        }
    }

    private static void CollectPropertyTodos(Type type, List<string> lines)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        PropertyInfo[] properties = type.GetProperties(flags);

        foreach (PropertyInfo property in properties)
        {
            object[] attributes = property.GetCustomAttributes(typeof(ToDoAttribute), false);

            foreach (ToDoAttribute todo in attributes)
            {
                lines.Add($"[PROPERTY] {type.FullName}.{property.Name}");
                lines.Add($"  - {todo.Message}");
                lines.Add(string.Empty);
            }
        }
    }

    private static void CollectMethodTodos(Type type, List<string> lines)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        MethodInfo[] methods = type.GetMethods(flags);

        foreach (MethodInfo method in methods)
        {
            object[] attributes = method.GetCustomAttributes(typeof(ToDoAttribute), false);

            foreach (ToDoAttribute todo in attributes)
            {
                lines.Add($"[METHOD] {type.FullName}.{method.Name}()");
                lines.Add($"  - {todo.Message}");
                lines.Add(string.Empty);
            }
        }
    }
}