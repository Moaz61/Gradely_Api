namespace Gradely.Domain.Enums
{
    /// <summary>
    /// Defines the roles a user can have in the Gradely system.
    /// These values are also seeded as ASP.NET Identity Roles in the database.
    /// </summary>
    public enum UserRole
    {
        Student = 0,
        Teacher = 1,
        Admin = 2
    }
}
