namespace Easy.Storage.Common.Attributes
{
    using System;

    /// <summary>
    /// Used to mark a given property as the primary key of the model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute { }
}