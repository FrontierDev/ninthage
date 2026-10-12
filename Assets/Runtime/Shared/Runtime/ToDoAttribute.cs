using System;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Method |
    AttributeTargets.Field |
    AttributeTargets.Property,
    AllowMultiple = true,
    Inherited = false)]
/// <summary>Marks code with an implementation task for later work.</summary>
public sealed class ToDoAttribute : Attribute
{
    public string Message { get; }

    /// <summary>Initializes a task marker with a message.</summary>
    public ToDoAttribute(string message)
    {
        Message = message;
    }
}
