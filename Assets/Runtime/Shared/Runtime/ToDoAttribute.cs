using System;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Method |
    AttributeTargets.Field |
    AttributeTargets.Property,
    AllowMultiple = true,
    Inherited = false)]
public sealed class ToDoAttribute : Attribute
{
    public string Message { get; }

    public ToDoAttribute(string message)
    {
        Message = message;
    }
}